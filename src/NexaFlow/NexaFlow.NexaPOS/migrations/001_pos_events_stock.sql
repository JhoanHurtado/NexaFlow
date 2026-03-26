-- =========================
-- POS EVENTS (registro de eventos de negocio)
-- =========================
CREATE TABLE pos_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    event_type TEXT NOT NULL,         -- 'sale.created', 'product.created', 'product.deactivated', 'customer.created', 'stock.low', 'stock.depleted'
    aggregate_id UUID NOT NULL,       -- id de la entidad que originó el evento
    aggregate_type TEXT NOT NULL,     -- 'Sale', 'Product', 'Customer'
    payload JSONB NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_pos_events_tenant ON pos_events(tenant_id);
CREATE INDEX idx_pos_events_type ON pos_events(event_type);
CREATE INDEX idx_pos_events_aggregate ON pos_events(aggregate_id);

-- =========================
-- PRODUCT STOCK (inventario)
-- =========================
CREATE TABLE product_stock (
    product_id UUID PRIMARY KEY REFERENCES products(id) ON DELETE CASCADE,
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    quantity INT NOT NULL DEFAULT 0 CHECK (quantity >= 0),
    low_stock_threshold INT NOT NULL DEFAULT 5,
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_product_stock_tenant ON product_stock(tenant_id);

-- RLS
ALTER TABLE pos_events ENABLE ROW LEVEL SECURITY;
ALTER TABLE product_stock ENABLE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation_pos_events ON pos_events
USING (tenant_id = current_setting('app.tenant_id')::UUID);

CREATE POLICY tenant_isolation_product_stock ON product_stock
USING (tenant_id = current_setting('app.tenant_id')::UUID);
