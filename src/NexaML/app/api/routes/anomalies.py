from datetime import date, timedelta

from fastapi import APIRouter, Depends, Header
from pydantic import BaseModel

from app.config import settings
from app.domain.interfaces import ISalesRepository, IAnomalyService
from app.infrastructure.db.sales_repository import PostgresSalesRepository
from app.services.ml.anomaly import ZScoreAnomalyService

router = APIRouter(prefix="/ml/anomalies", tags=["anomalies"])


class AnomalyResponse(BaseModel):
    tenant_id: str
    total_days: int
    anomaly_count: int
    anomalies: list[dict]


def get_sales_repo() -> ISalesRepository:
    return PostgresSalesRepository(settings.db_connection)


def get_anomaly_service() -> IAnomalyService:
    return ZScoreAnomalyService()


@router.get("", response_model=AnomalyResponse)
async def detect_anomalies(
    x_tenant_id: str = Header(..., alias="x-tenant-id"),
    days: int = 30,
    repo: ISalesRepository = Depends(get_sales_repo),
    svc: IAnomalyService = Depends(get_anomaly_service),
):
    """
    Detecta días con ventas anómalas (Z-score) en los últimos `days` días.
    Retorna todos los días con su Z-score y flag is_anomaly.
    """
    to_date = date.today()
    from_date = to_date - timedelta(days=days)

    records = await repo.get_daily_sales(x_tenant_id, from_date, to_date)
    results = svc.detect(records, settings.anomaly_zscore_threshold)

    anomalies = [r for r in results if r.is_anomaly]
    return AnomalyResponse(
        tenant_id=x_tenant_id,
        total_days=len(results),
        anomaly_count=len(anomalies),
        anomalies=[
            {"date": str(r.sale_date), "revenue": r.total_revenue,
             "zscore": r.zscore, "is_anomaly": r.is_anomaly}
            for r in results
        ],
    )
