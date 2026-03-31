from datetime import date
import pytest

from app.domain.entities import SaleRecord
from app.services.ml.anomaly import ZScoreAnomalyService


def make_records(revenues: list[float]) -> list[SaleRecord]:
    base = date(2024, 1, 1)
    return [
        SaleRecord(sale_date=date(2024, 1, i + 1), total_revenue=v, sale_count=5)
        for i, v in enumerate(revenues)
    ]


class TestZScoreAnomalyService:
    def setup_method(self):
        self.svc = ZScoreAnomalyService()

    def test_normal_data_no_anomalies(self):
        records = make_records([100, 105, 98, 102, 99, 103, 101])
        results = self.svc.detect(records, threshold=2.5)
        assert not any(r.is_anomaly for r in results)

    def test_spike_detected_as_anomaly(self):
        # Con threshold=2.0 el spike es claramente anómalo (zscore ~2.45)
        records = make_records([100, 100, 100, 100, 100, 100, 1000])
        results = self.svc.detect(records, threshold=2.0)
        assert results[-1].is_anomaly
        assert results[-1].zscore > 2.0

    def test_drop_detected_as_anomaly(self):
        records = make_records([500, 500, 500, 500, 500, 500, 1])
        results = self.svc.detect(records, threshold=2.0)
        assert results[-1].is_anomaly

    def test_insufficient_data_returns_no_anomalies(self):
        records = make_records([100, 200])
        results = self.svc.detect(records, threshold=2.5)
        assert not any(r.is_anomaly for r in results)

    def test_all_same_revenue_no_anomalies(self):
        records = make_records([50.0] * 10)
        results = self.svc.detect(records, threshold=2.5)
        # std=0, zscore=0 para todos
        assert all(r.zscore == 0.0 for r in results)
        assert not any(r.is_anomaly for r in results)

    def test_returns_same_count_as_input(self):
        records = make_records([10, 20, 30, 40, 50])
        results = self.svc.detect(records, threshold=2.5)
        assert len(results) == len(records)

    def test_custom_threshold_more_sensitive(self):
        records = make_records([100, 100, 100, 100, 150])
        results_strict = self.svc.detect(records, threshold=0.5)
        results_loose = self.svc.detect(records, threshold=3.0)
        # Con threshold bajo, más anomalías
        assert sum(r.is_anomaly for r in results_strict) >= sum(r.is_anomaly for r in results_loose)
