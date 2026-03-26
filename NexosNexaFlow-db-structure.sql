CREATE DATABASE NexosNexaFlow;

-- =========================
-- EXTENSIONS
-- =========================
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =========================
-- PLANS (ENFOCADOS EN USUARIOS)
-- =========================
CREATE TABLE plans (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    max_users INT NOT NULL,
    stripe_price_id TEXT UNIQUE, -- 🔥 mapping directo con Stripe
    price NUMERIC(10,2) NOT NULL CHECK (price >= 0)
);

-- =========================
-- TENANTS
-- =========================
CREATE TABLE tenants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL,
    stripe_customer_id TEXT UNIQUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- =========================
-- USERS (NUEVO)
-- =========================
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    name TEXT NOT NULL,
    email TEXT NOT NULL,
    role TEXT NOT NULL CHECK (role IN ('owner','admin','staff')),
    active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE (tenant_id, email)
);

CREATE INDEX idx_users_tenant ON users(tenant_id);

-- =========================
-- STRIPE PRODUCTS
-- =========================
CREATE TABLE stripe_products (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL
);

-- =========================
-- STRIPE PRICES
-- =========================
CREATE TABLE stripe_prices (
    id TEXT PRIMARY KEY,
    product_id TEXT REFERENCES stripe_products(id),
    unit_amount BIGINT NOT NULL,
    currency TEXT NOT NULL,
    interval TEXT
);

-- =========================
-- SUBSCRIPTIONS
-- =========================
CREATE TABLE subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    stripe_subscription_id TEXT UNIQUE NOT NULL,
    stripe_price_id TEXT REFERENCES stripe_prices(id),
    status TEXT NOT NULL CHECK (
        status IN ('trialing','active','past_due','canceled','incomplete')
    ),
    current_period_start TIMESTAMP NOT NULL,
    current_period_end TIMESTAMP NOT NULL,
    cancel_at_period_end BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_subscriptions_tenant ON subscriptions(tenant_id);

-- =========================
-- PAYMENTS
-- =========================
CREATE TABLE payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    stripe_payment_intent_id TEXT UNIQUE,
    stripe_invoice_id TEXT,
    amount NUMERIC(10,2) NOT NULL,
    currency TEXT NOT NULL,
    status TEXT NOT NULL CHECK (
        status IN ('pending','succeeded','failed')
    ),
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_payments_tenant ON payments(tenant_id);

-- =========================
-- STRIPE WEBHOOK EVENTS
-- =========================
CREATE TABLE stripe_webhook_events (
    id TEXT PRIMARY KEY,
    type TEXT NOT NULL,
    payload JSONB NOT NULL,
    processed BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- =========================
-- CUSTOMERS
-- =========================
CREATE TABLE customers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    name TEXT NOT NULL,
    phone TEXT,
    email TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_customers_tenant ON customers(tenant_id);

-- =========================
-- RESERVATIONS (SIN LÍMITES)
-- =========================
CREATE TABLE reservations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    customer_id UUID NOT NULL REFERENCES customers(id) ON DELETE CASCADE,
    reservation_date DATE NOT NULL,
    time_slot TIME NOT NULL,
    status TEXT NOT NULL CHECK (
        status IN ('pending','confirmed','cancelled','arrived','completed')
    ),
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_reservations_date_time 
ON reservations (reservation_date, time_slot);

CREATE INDEX idx_reservations_tenant_date 
ON reservations (tenant_id, reservation_date);

-- =========================
-- PRODUCTS
-- =========================
CREATE TABLE products (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    name TEXT NOT NULL,
    price NUMERIC(10,2) NOT NULL CHECK (price >= 0),
    active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_products_tenant ON products(tenant_id);
CREATE INDEX idx_products_active ON products(active);

-- =========================
-- SALES
-- =========================
CREATE TABLE sales (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    customer_id UUID REFERENCES customers(id) ON DELETE SET NULL,
    reservation_id UUID REFERENCES reservations(id) ON DELETE SET NULL,
    total NUMERIC(10,2) NOT NULL CHECK (total >= 0),
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_sales_tenant_date 
ON sales (tenant_id, created_at);

-- =========================
-- SALE ITEMS
-- =========================
CREATE TABLE sale_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sale_id UUID NOT NULL REFERENCES sales(id) ON DELETE CASCADE,
    product_id UUID NOT NULL REFERENCES products(id),
    quantity INT NOT NULL CHECK (quantity > 0),
    unit_price NUMERIC(10,2) NOT NULL CHECK (unit_price >= 0)
);

CREATE INDEX idx_sale_items_sale ON sale_items(sale_id);

-- =========================
-- RLS
-- =========================
ALTER TABLE customers ENABLE ROW LEVEL SECURITY;
ALTER TABLE reservations ENABLE ROW LEVEL SECURITY;
ALTER TABLE sales ENABLE ROW LEVEL SECURITY;
ALTER TABLE products ENABLE ROW LEVEL SECURITY;
ALTER TABLE users ENABLE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation_customers ON customers
USING (tenant_id = current_setting('app.tenant_id')::UUID);

CREATE POLICY tenant_isolation_reservations ON reservations
USING (tenant_id = current_setting('app.tenant_id')::UUID);

CREATE POLICY tenant_isolation_sales ON sales
USING (tenant_id = current_setting('app.tenant_id')::UUID);

CREATE POLICY tenant_isolation_products ON products
USING (tenant_id = current_setting('app.tenant_id')::UUID);

CREATE POLICY tenant_isolation_users ON users
USING (tenant_id = current_setting('app.tenant_id')::UUID);