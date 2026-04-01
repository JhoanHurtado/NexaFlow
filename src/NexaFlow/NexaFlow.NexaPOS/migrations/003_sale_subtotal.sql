-- =========================
-- SALE SUBTOTAL
-- =========================
-- El campo 'subtotal' (base antes de IVA) faltaba en la tabla sales.
-- Se agrega con DEFAULT = total para que las ventas existentes sean consistentes
-- (ventas anteriores no tenían IVA, por lo que subtotal == total).
ALTER TABLE sales ADD COLUMN IF NOT EXISTS subtotal NUMERIC(10,2) NOT NULL DEFAULT 0;

-- Backfill: para ventas existentes donde tax_amount = 0, subtotal = total
UPDATE sales SET subtotal = total WHERE subtotal = 0;
