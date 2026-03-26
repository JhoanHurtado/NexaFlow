from datetime import date
import pytest

from app.domain.entities import SaleRecord
from app.services.ml.forecast import ProphetForecastService


def make_records(n_days: int = 60, base_revenue: float = 100.0) -> list[SaleRecord]:
    """Genera n_days de datos sintéticos con variación semanal."""
    import math
    records = []
    for i in range(n_days):
        d = date(2024, 1, 1)
        from datetime import timedelta
        d = d + timedelta(days=i)
        # Patrón semanal: fines de semana +30%
        multiplier = 1.3 if d.weekday() >= 5 else 1.0
        revenue = base_revenue * multiplier + (i % 7) * 2
        records.append(SaleRecord(sale_date=d, total_revenue=revenue, sale_count=5))
    return records


class TestProphetForecastService:
    def setup_method(self):
        self.svc = ProphetForecastService()

    def test_returns_correct_horizon(self):
        records = make_records(60)
        result = self.svc.predict(records, horizon_days=7)
        assert len(result) == 7

    def test_predictions_are_positive(self):
        records = make_records(60)
        result = self.svc.predict(records, horizon_days=7)
        assert all(p.yhat > 0 for p in result)

    def test_confidence_interval_valid(self):
        records = make_records(60)
        result = self.svc.predict(records, horizon_days=7)
        for p in result:
            assert p.yhat_lower <= p.yhat <= p.yhat_upper

    def test_insufficient_data_raises(self):
        records = make_records(1)
        with pytest.raises(ValueError, match="al menos 2"):
            self.svc.predict(records, horizon_days=7)

    def test_forecast_dates_are_future(self):
        records = make_records(60)
        last_date = records[-1].sale_date
        result = self.svc.predict(records, horizon_days=7)
        assert all(p.ds > last_date for p in result)
