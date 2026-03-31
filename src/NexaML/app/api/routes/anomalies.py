from datetime import date, timedelta

from fastapi import APIRouter, Depends, Header
from fastapi.responses import JSONResponse

from app.api.response import ApiResponse
from app.config import settings
from app.domain.interfaces import ISalesRepository, IAnomalyService
from app.infrastructure.db.sales_repository import PostgresSalesRepository
from app.services.ml.anomaly import ZScoreAnomalyService

router = APIRouter(prefix="/ml/anomalies", tags=["anomalies"])


def get_sales_repo() -> ISalesRepository:
    return PostgresSalesRepository(settings.get_db_dsn())


def get_anomaly_service() -> IAnomalyService:
    return ZScoreAnomalyService()


@router.get("", response_model=ApiResponse)
async def detect_anomalies(
    x_tenant_id: str = Header(..., alias="x-tenant-id"),
    days: int = 30,
    repo: ISalesRepository = Depends(get_sales_repo),
    svc: IAnomalyService = Depends(get_anomaly_service),
):
    to_date = date.today()
    from_date = to_date - timedelta(days=days)

    try:
        records = await repo.get_daily_sales(x_tenant_id, from_date, to_date)
    except Exception as e:
        return JSONResponse(
            status_code=500,
            content=ApiResponse.fail("DB_ERROR", f"Error al consultar ventas: {str(e)}").model_dump(),
        )

    try:
        results = svc.detect(records, settings.anomaly_zscore_threshold)
    except Exception as e:
        return JSONResponse(
            status_code=500,
            content=ApiResponse.fail("ANOMALY_ERROR", f"Error al detectar anomalías: {str(e)}").model_dump(),
        )

    anomalies = [r for r in results if r.is_anomaly]
    return ApiResponse.ok({
        "tenant_id": x_tenant_id,
        "total_days": len(results),
        "anomaly_count": len(anomalies),
        "anomalies": [
            {"date": str(r.sale_date), "revenue": r.total_revenue,
             "zscore": r.zscore, "is_anomaly": r.is_anomaly}
            for r in results
        ],
    })
