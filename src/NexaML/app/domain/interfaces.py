from abc import ABC, abstractmethod
from datetime import date

from app.domain.entities import SaleRecord, AnomalyPoint, ForecastPoint


class ISalesRepository(ABC):
    @abstractmethod
    async def get_daily_sales(
        self, tenant_id: str, from_date: date, to_date: date
    ) -> list[SaleRecord]: ...

    @abstractmethod
    async def get_top_products(
        self, tenant_id: str, from_date: date, to_date: date, limit: int = 5
    ) -> list[dict]: ...


class IForecastService(ABC):
    @abstractmethod
    def predict(self, records: list[SaleRecord], horizon_days: int) -> list[ForecastPoint]: ...


class IAnomalyService(ABC):
    @abstractmethod
    def detect(self, records: list[SaleRecord], threshold: float) -> list[AnomalyPoint]: ...


class ILLMService(ABC):
    @abstractmethod
    async def generate_insight(self, context: dict) -> str: ...
