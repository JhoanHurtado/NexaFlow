-- =========================
-- DATABASE
-- =========================
CREATE DATABASE nexaflow;

-- =========================
-- EXTENSIONS
-- =========================
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =========================
-- PLANS
-- =========================
CREATE TABLE plans (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    max_users INT NOT NULL,
    stripe_price_id TEXT UNIQUE,
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
-- USERS
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

ALTER TABLE customers ADD CONSTRAINT unique_customer_per_tenant UNIQUE (tenant_id, email);
ALTER TABLE customers ALTER COLUMN email SET NOT NULL;

CREATE UNIQUE INDEX idx_customers_tenant_email ON customers (tenant_id, email);

-- =========================
-- RESERVATIONS
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

CREATE INDEX idx_reservations_date_time ON reservations (reservation_date, time_slot);
CREATE INDEX idx_reservations_tenant_date ON reservations (tenant_id, reservation_date);

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
-- PRODUCT STOCK
-- Separado de products para aislar responsabilidades:
-- products = catálogo, product_stock = inventario
-- Relación: products.id → product_stock.product_id (1 a 1)
-- =========================
CREATE TABLE product_stock (
    product_id UUID PRIMARY KEY REFERENCES products(id) ON DELETE CASCADE,
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    quantity INT NOT NULL DEFAULT 0 CHECK (quantity >= 0),
    low_stock_threshold INT NOT NULL DEFAULT 5,
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_product_stock_tenant ON product_stock(tenant_id);

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

CREATE INDEX idx_sales_tenant_date ON sales (tenant_id, created_at);

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
-- POS EVENTS (Outbox Pattern)
-- Registra todos los eventos de negocio del POS antes de enviarlos a SQS/EventBridge.
-- published = FALSE: pendiente de enviar
-- published = TRUE:  ya enviado a SQS/EventBridge
-- Un proceso separado (Lambda scheduler) lee los no publicados y los despacha.
-- =========================
CREATE TABLE pos_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    event_type TEXT NOT NULL,
    -- Valores: 'sale.created' | 'product.created' | 'product.deactivated'
    --          'stock.updated' | 'stock.low' | 'stock.depleted' | 'customer.created'
    aggregate_id UUID NOT NULL,
    aggregate_type TEXT NOT NULL,   -- 'Sale' | 'Product' | 'Customer'
    payload JSONB NOT NULL,
    published BOOLEAN NOT NULL DEFAULT FALSE,
    published_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_pos_events_tenant ON pos_events(tenant_id);
CREATE INDEX idx_pos_events_type ON pos_events(event_type);
CREATE INDEX idx_pos_events_aggregate ON pos_events(aggregate_id);
-- Índice parcial para el publisher: solo lee los no publicados
CREATE INDEX idx_pos_events_unpublished ON pos_events(tenant_id, created_at)
    WHERE published = FALSE;

-- =========================
-- RLS
-- =========================
ALTER TABLE customers ENABLE ROW LEVEL SECURITY;
ALTER TABLE reservations ENABLE ROW LEVEL SECURITY;
ALTER TABLE sales ENABLE ROW LEVEL SECURITY;
ALTER TABLE products ENABLE ROW LEVEL SECURITY;
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE product_stock ENABLE ROW LEVEL SECURITY;
ALTER TABLE pos_events ENABLE ROW LEVEL SECURITY;

-- =========================
-- FLEXIBLE RLS POLICIES
-- =========================

CREATE POLICY tenant_isolation_customers ON customers
USING (
    current_setting('app.tenant_id', true) IS NULL
    OR tenant_id = current_setting('app.tenant_id', true)::UUID
);

CREATE POLICY tenant_isolation_reservations ON reservations
USING (
    current_setting('app.tenant_id', true) IS NULL
    OR tenant_id = current_setting('app.tenant_id', true)::UUID
);

CREATE POLICY tenant_isolation_sales ON sales
USING (
    current_setting('app.tenant_id', true) IS NULL
    OR tenant_id = current_setting('app.tenant_id', true)::UUID
);

CREATE POLICY tenant_isolation_products ON products
USING (
    current_setting('app.tenant_id', true) IS NULL
    OR tenant_id = current_setting('app.tenant_id', true)::UUID
);

CREATE POLICY tenant_isolation_users ON users
USING (
    current_setting('app.tenant_id', true) IS NULL
    OR tenant_id = current_setting('app.tenant_id', true)::UUID
);

CREATE POLICY tenant_isolation_product_stock ON product_stock
USING (
    current_setting('app.tenant_id', true) IS NULL
    OR tenant_id = current_setting('app.tenant_id', true)::UUID
);

CREATE POLICY tenant_isolation_pos_events ON pos_events
USING (
    current_setting('app.tenant_id', true) IS NULL
    OR tenant_id = current_setting('app.tenant_id', true)::UUID
);
