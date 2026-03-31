from datetime import date, timedelta

from fastapi import APIRouter, Depends, Header
from fastapi.responses import JSONResponse

from app.api.response import ApiResponse
from app.config import settings
from app.domain.interfaces import ISalesRepository
from app.infrastructure.db.sales_repository import PostgresSalesRepository
from app.services.ml.anomaly import ZScoreAnomalyService
from app.services.ml.forecast import LinearForecastService
from app.services.llm.bedrock import BedrockLLMService

router = APIRouter(prefix="/ml/insights", tags=["insights"])


def get_sales_repo() -> ISalesRepository:
    return PostgresSalesRepository(settings.get_db_dsn())


@router.get("", response_model=ApiResponse)
async def get_insight(
    x_tenant_id: str = Header(..., alias="x-tenant-id"),
    repo: ISalesRepository = Depends(get_sales_repo),
):
    to_date = date.today()
    from_30 = to_date - timedelta(days=30)
    from_90 = to_date - timedelta(days=90)

    try:
        records_30 = await repo.get_daily_sales(x_tenant_id, from_30, to_date)
        records_90 = await repo.get_daily_sales(x_tenant_id, from_90, to_date)
        top_products = await repo.get_top_products(x_tenant_id, from_30, to_date, limit=1)
    except Exception as e:
        return JSONResponse(
            status_code=500,
            content=ApiResponse.fail("DB_ERROR", f"Error al consultar datos: {str(e)}").model_dump(),
        )

    total_revenue_30d = sum(r.total_revenue for r in records_30)
    sale_count_30d = sum(r.sale_count for r in records_30)
    avg_ticket = total_revenue_30d / sale_count_30d if sale_count_30d > 0 else 0.0

    try:
        anomaly_svc = ZScoreAnomalyService()
        anomalies = [
            a for a in anomaly_svc.detect(records_30, settings.anomaly_zscore_threshold)
            if a.is_anomaly
        ]
    except Exception:
        anomalies = []

    forecast_7d_total = 0.0
    if len(records_90) >= 2:
        try:
            forecast_svc = LinearForecastService()
            forecast = forecast_svc.predict(records_90, 7)
            forecast_7d_total = sum(p.yhat for p in forecast)
        except Exception:
            pass

    context = {
        "avg_ticket": avg_ticket,
        "total_revenue_30d": total_revenue_30d,
        "anomaly_count": len(anomalies),
        "forecast_7d_total": forecast_7d_total,
        "top_product": top_products[0]["name"] if top_products else "N/A",
    }

    try:
        llm = BedrockLLMService()
        insight_text = await llm.generate_insight(context)
    except Exception as e:
        return JSONResponse(
            status_code=500,
            content=ApiResponse.fail("LLM_ERROR", f"Error al generar insight: {str(e)}").model_dump(),
        )

    return ApiResponse.ok({
        "tenant_id": x_tenant_id,
        "insight": insight_text,
        "context": context,
    })
