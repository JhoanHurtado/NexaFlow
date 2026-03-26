import numpy as np

from app.domain.entities import SaleRecord, AnomalyPoint
from app.domain.interfaces import IAnomalyService


class ZScoreAnomalyService(IAnomalyService):
    """
    Detección de anomalías en ventas diarias usando Z-score.
    Un día es anómalo si su revenue se desvía más de `threshold` desviaciones estándar.
    """

    def detect(self, records: list[SaleRecord], threshold: float = 2.5) -> list[AnomalyPoint]:
        if len(records) < 3:
            # Sin suficientes datos, ningún punto es anómalo
            return [
                AnomalyPoint(r.sale_date, r.total_revenue, 0.0, False)
                for r in records
            ]

        revenues = np.array([r.total_revenue for r in records], dtype=float)
        mean = revenues.mean()
        std = revenues.std()

        results = []
        for record in records:
            zscore = abs(record.total_revenue - mean) / std if std > 0 else 0.0
            results.append(AnomalyPoint(
                sale_date=record.sale_date,
                total_revenue=record.total_revenue,
                zscore=round(zscore, 3),
                is_anomaly=zscore > threshold,
            ))
        return results
