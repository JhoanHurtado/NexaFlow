from datetime import date, timedelta

from fastapi import APIRouter, Depends, Header
from fastapi.responses import JSONResponse

from app.api.response import ApiResponse
from app.config import settings
from app.domain.interfaces import ISalesRepository, IForecastService
from app.infrastructure.db.sales_repository import PostgresSalesRepository
from app.services.ml.forecast import LinearForecastService

router = APIRouter(prefix="/ml/forecast", tags=["forecast"])


def get_sales_repo() -> ISalesRepository:
    return PostgresSalesRepository(settings.get_db_dsn())


def get_forecast_service() -> IForecastService:
    return LinearForecastService()


@router.get("", response_model=ApiResponse)
async def forecast_sales(
    x_tenant_id: str = Header(..., alias="x-tenant-id"),
    days_history: int = 90,
    horizon_days: int = 7,
    repo: ISalesRepository = Depends(get_sales_repo),
    svc: IForecastService = Depends(get_forecast_service),
):
    to_date = date.today()
    from_date = to_date - timedelta(days=days_history)

    try:
        records = await repo.get_daily_sales(x_tenant_id, from_date, to_date)
    except Exception as e:
        return JSONResponse(
            status_code=500,
            content=ApiResponse.fail("DB_ERROR", f"Error al consultar ventas: {str(e)}").model_dump(),
        )

    if len(records) < 2:
        return JSONResponse(
            status_code=422,
            content=ApiResponse.fail(
                "INSUFFICIENT_DATA",
                "Datos insuficientes para generar predicción (mínimo 2 días).",
            ).model_dump(),
        )

    try:
        predictions = svc.predict(records, horizon_days)
    except ValueError as e:
        return JSONResponse(
            status_code=422,
            content=ApiResponse.fail("FORECAST_ERROR", str(e)).model_dump(),
        )
    except Exception as e:
        return JSONResponse(
            status_code=500,
            content=ApiResponse.fail("FORECAST_ERROR", f"Error al generar predicción: {str(e)}").model_dump(),
        )

    return ApiResponse.ok({
        "tenant_id": x_tenant_id,
        "horizon_days": horizon_days,
        "predictions": [
            {"date": str(p.ds), "predicted": p.yhat,
             "lower": p.yhat_lower, "upper": p.yhat_upper}
            for p in predictions
        ],
    })
