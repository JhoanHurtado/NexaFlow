-- =========================
-- SALE STATUS
-- =========================
ALTER TABLE sales ADD COLUMN IF NOT EXISTS status TEXT NOT NULL DEFAULT 'pending'
    CHECK (status IN ('pending', 'completed', 'cancelled'));

-- =========================
-- IVA / TAX
-- =========================
ALTER TABLE sales ADD COLUMN IF NOT EXISTS tax_rate  NUMERIC(5,2) NOT NULL DEFAULT 0;
ALTER TABLE sales ADD COLUMN IF NOT EXISTS tax_amount NUMERIC(10,2) NOT NULL DEFAULT 0;

-- Recalcular total para incluir IVA en ventas existentes (sin cambio — ya están sin IVA)
-- Las nuevas ventas usarán tax_rate y tax_amount del config del tenant

-- =========================
-- TENANT CONFIG (parametrización por tenant)
-- =========================
CREATE TABLE IF NOT EXISTS tenant_config (
    tenant_id   UUID PRIMARY KEY REFERENCES tenants(id) ON DELETE CASCADE,
    tax_rate    NUMERIC(5,2)  NOT NULL DEFAULT 19.00,  -- IVA %
    currency    TEXT          NOT NULL DEFAULT 'COP',
    slot_duration_minutes INT NOT NULL DEFAULT 60,
    open_time   TIME          NOT NULL DEFAULT '08:00',
    close_time  TIME          NOT NULL DEFAULT '20:00',
    updated_at  TIMESTAMP     NOT NULL DEFAULT NOW()
);

-- RLS
ALTER TABLE tenant_config ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_config ON tenant_config
USING (
    current_setting('app.tenant_id', true) IS NULL
    OR tenant_id = current_setting('app.tenant_id', true)::UUID
);

-- Índice
CREATE INDEX IF NOT EXISTS idx_tenant_config_tenant ON tenant_config(tenant_id);
