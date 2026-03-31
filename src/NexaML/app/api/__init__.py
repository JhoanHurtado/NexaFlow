from fastapi import APIRouter
from .routes import forecast, anomalies, insights

router = APIRouter()
router.include_router(forecast.router)
router.include_router(anomalies.router)
router.include_router(insights.router)
