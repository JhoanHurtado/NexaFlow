from datetime import date, timedelta

from fastapi import APIRouter, Depends, HTTPException, Header
from pydantic import BaseModel

from app.config import settings
from app.domain.interfaces import ISalesRepository, IForecastService
from app.infrastructure.db.sales_repository import PostgresSalesRepository
from app.services.ml.forecast import ProphetForecastService

router = APIRouter(prefix="/ml/forecast", tags=["forecast"])


class ForecastResponse(BaseModel):
    tenant_id: str
    horizon_days: int
    predictions: list[dict]


def get_sales_repo() -> ISalesRepository:
    return PostgresSalesRepository(settings.db_connection)


def get_forecast_service() -> IForecastService:
    return ProphetForecastService()


@router.get("", response_model=ForecastResponse)
async def forecast_sales(
    x_tenant_id: str = Header(..., alias="x-tenant-id"),
    days_history: int = 90,
    horizon_days: int = 7,
    repo: ISalesRepository = Depends(get_sales_repo),
    svc: IForecastService = Depends(get_forecast_service),
):
    """
    Predice las ventas de los próximos `horizon_days` días usando Prophet.
    Usa los últimos `days_history` días como datos de entrenamiento.
    """
    to_date = date.today()
    from_date = to_date - timedelta(days=days_history)

    records = await repo.get_daily_sales(x_tenant_id, from_date, to_date)
    if len(records) < 2:
        raise HTTPException(422, "Datos insuficientes para generar predicción (mínimo 2 días).")

    try:
        predictions = svc.predict(records, horizon_days)
    except ValueError as e:
        raise HTTPException(422, str(e))

    return ForecastResponse(
        tenant_id=x_tenant_id,
        horizon_days=horizon_days,
        predictions=[
            {"date": str(p.ds), "predicted": p.yhat,
             "lower": p.yhat_lower, "upper": p.yhat_upper}
            for p in predictions
        ],
    )
