from dataclasses import dataclass
from datetime import date


@dataclass(frozen=True)
class SaleRecord:
    sale_date: date
    total_revenue: float
    sale_count: int


@dataclass(frozen=True)
class ForecastPoint:
    ds: date
    yhat: float
    yhat_lower: float
    yhat_upper: float


@dataclass(frozen=True)
class AnomalyPoint:
    sale_date: date
    total_revenue: float
    zscore: float
    is_anomaly: bool


@dataclass(frozen=True)
class InsightResult:
    tenant_id: str
    insight_type: str   # 'forecast' | 'anomaly' | 'recommendation'
    summary: str
    details: dict
