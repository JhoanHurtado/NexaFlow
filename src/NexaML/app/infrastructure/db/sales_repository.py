from __future__ import annotations

from datetime import date

from app.domain.entities import SaleRecord
from app.domain.interfaces import ISalesRepository


class PostgresSalesRepository(ISalesRepository):
    def __init__(self, dsn: str):
        self._dsn = dsn.replace("postgresql+asyncpg://", "postgresql://")

    async def get_daily_sales(
        self, tenant_id: str, from_date: date, to_date: date
    ) -> list[SaleRecord]:
        import asyncpg  # lazy import — not needed in unit tests
        conn = await asyncpg.connect(self._dsn)
        try:
            await conn.execute(f"SET app.tenant_id = '{tenant_id}'")
            rows = await conn.fetch(
                """
                SELECT
                    created_at::date        AS sale_date,
                    SUM(total)::float       AS total_revenue,
                    COUNT(*)::int           AS sale_count
                FROM sales
                WHERE tenant_id = $1
                  AND created_at::date BETWEEN $2 AND $3
                GROUP BY created_at::date
                ORDER BY created_at::date
                """,
                tenant_id, from_date, to_date,
            )
            return [
                SaleRecord(r["sale_date"], r["total_revenue"], r["sale_count"])
                for r in rows
            ]
        finally:
            await conn.close()

    async def get_top_products(
        self, tenant_id: str, from_date: date, to_date: date, limit: int = 5
    ) -> list[dict]:
        import asyncpg  # lazy import
        conn = await asyncpg.connect(self._dsn)
        try:
            await conn.execute(f"SET app.tenant_id = '{tenant_id}'")
            rows = await conn.fetch(
                """
                SELECT
                    p.name,
                    SUM(si.quantity)::int       AS total_units,
                    SUM(si.quantity * si.unit_price)::float AS total_revenue
                FROM sale_items si
                JOIN products p ON p.id = si.product_id
                JOIN sales s ON s.id = si.sale_id
                WHERE s.tenant_id = $1
                  AND s.created_at::date BETWEEN $2 AND $3
                GROUP BY p.name
                ORDER BY total_revenue DESC
                LIMIT $4
                """,
                tenant_id, from_date, to_date, limit,
            )
            return [dict(r) for r in rows]
        finally:
            await conn.close()
