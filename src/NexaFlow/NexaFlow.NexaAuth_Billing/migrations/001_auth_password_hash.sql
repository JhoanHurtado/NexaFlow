-- =========================
-- MIGRATION: NexaAuth_Billing
-- Agrega password_hash a users para autenticación local.
-- La tabla users ya existe en schema.sql (gestionada por NexaPOS/NexaBook).
-- Este microservicio es el único que escribe password_hash.
-- =========================

ALTER TABLE users ADD COLUMN IF NOT EXISTS password_hash TEXT NOT NULL DEFAULT '';

-- Índice para login rápido por email dentro del tenant
CREATE INDEX IF NOT EXISTS idx_users_tenant_email ON users(tenant_id, email);
