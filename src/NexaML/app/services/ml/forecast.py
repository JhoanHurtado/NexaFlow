from datetime import date

from app.domain.entities import SaleRecord, ForecastPoint
from app.domain.interfaces import IForecastService


class ProphetForecastService(IForecastService):
    """
    Predicción de ventas usando Facebook Prophet.
    Requiere al menos 2 puntos históricos para entrenar.
    pandas y prophet se importan de forma lazy para no penalizar tests unitarios.
    """

    def predict(self, records: list[SaleRecord], horizon_days: int) -> list[ForecastPoint]:
        if len(records) < 2:
            raise ValueError("Se necesitan al menos 2 días de datos para predecir.")

        import pandas as pd          # lazy — heavy dep, not needed in unit tests
        from prophet import Prophet  # type: ignore

        df = pd.DataFrame({
            "ds": [r.sale_date for r in records],
            "y":  [r.total_revenue for r in records],
        })

        model = Prophet(
            daily_seasonality=False,
            weekly_seasonality=True,
            yearly_seasonality=False,
            interval_width=0.80,
        )
        model.fit(df)

        future = model.make_future_dataframe(periods=horizon_days)
        forecast = model.predict(future)

        future_rows = forecast.tail(horizon_days)
        return [
            ForecastPoint(
                ds=row["ds"].date(),
                yhat=round(row["yhat"], 2),
                yhat_lower=round(row["yhat_lower"], 2),
                yhat_upper=round(row["yhat_upper"], 2),
            )
            for _, row in future_rows.iterrows()
        ]
