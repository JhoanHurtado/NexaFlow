"""
ForecastService — predicción de ventas sin Prophet.

Reemplaza Prophet (que falla en Lambda por incompatibilidad con pystan/cmdstan)
con una regresión lineal + estacionalidad semanal usando numpy y scikit-learn,
que ya están disponibles en el entorno Lambda sin dependencias nativas problemáticas.

Modelo:
  y = trend + weekly_seasonality + noise
  - trend: regresión lineal sobre el índice de día
  - weekly_seasonality: dummies de día de semana (sin/cos Fourier de orden 1)
  - predicción con intervalo de confianza basado en el RMSE del entrenamiento
"""
from __future__ import annotations

from datetime import date, timedelta

import numpy as np

from app.domain.entities import SaleRecord, ForecastPoint
from app.domain.interfaces import IForecastService


class LinearForecastService(IForecastService):
    """
    Predicción de ventas usando regresión lineal con estacionalidad semanal.
    Compatible con AWS Lambda — solo depende de numpy y scikit-learn.
    """

    def predict(self, records: list[SaleRecord], horizon_days: int) -> list[ForecastPoint]:
        if len(records) < 2:
            raise ValueError("Se necesitan al menos 2 días de datos para predecir.")

        from sklearn.linear_model import Ridge  # lazy — not needed in unit tests

        dates = [r.sale_date for r in records]
        revenues = np.array([r.total_revenue for r in records], dtype=float)

        # Índice numérico de día (0, 1, 2, ...)
        base = dates[0]
        day_idx = np.array([(d - base).days for d in dates], dtype=float)

        X_train = self._build_features(day_idx)
        model = Ridge(alpha=1.0)
        model.fit(X_train, revenues)

        # RMSE para intervalo de confianza (±1.28σ ≈ 80%)
        residuals = revenues - model.predict(X_train)
        rmse = float(np.sqrt(np.mean(residuals ** 2)))
        ci = 1.28 * rmse

        # Predecir días futuros
        last_idx = int(day_idx[-1])
        future_idx = np.array([last_idx + i + 1 for i in range(horizon_days)], dtype=float)
        X_future = self._build_features(future_idx)
        preds = model.predict(X_future)

        result = []
        for i, yhat in enumerate(preds):
            future_date = base + timedelta(days=int(future_idx[i]))
            result.append(ForecastPoint(
                ds=future_date,
                yhat=round(max(yhat, 0.0), 2),
                yhat_lower=round(max(yhat - ci, 0.0), 2),
                yhat_upper=round(max(yhat + ci, 0.0), 2),
            ))
        return result

    @staticmethod
    def _build_features(day_idx: np.ndarray) -> np.ndarray:
        """Trend + Fourier features para estacionalidad semanal (período 7)."""
        trend = day_idx.reshape(-1, 1)
        period = 7.0
        sin1 = np.sin(2 * np.pi * day_idx / period).reshape(-1, 1)
        cos1 = np.cos(2 * np.pi * day_idx / period).reshape(-1, 1)
        sin2 = np.sin(4 * np.pi * day_idx / period).reshape(-1, 1)
        cos2 = np.cos(4 * np.pi * day_idx / period).reshape(-1, 1)
        return np.hstack([trend, sin1, cos1, sin2, cos2])
