from datetime import date
from unittest.mock import AsyncMock, MagicMock

import pytest
from fastapi.testclient import TestClient

from app.domain.entities import SaleRecord, ForecastPoint, AnomalyPoint
from app.main import app

TENANT_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"


def make_records(n: int = 30) -> list[SaleRecord]:
    from datetime import timedelta
    base = date(2024, 1, 1)
    return [SaleRecord(base + timedelta(days=i), 100.0 + i, 5) for i in range(n)]


def test_forecast_endpoint_returns_predictions():
    from app.api.routes.forecast import get_sales_repo, get_forecast_service

    mock_repo = AsyncMock()
    mock_repo.get_daily_sales.return_value = make_records(60)

    mock_svc = MagicMock()
    mock_svc.predict.return_value = [
        ForecastPoint(date(2024, 3, 1), 120.0, 100.0, 140.0)
    ]

    app.dependency_overrides[get_sales_repo] = lambda: mock_repo
    app.dependency_overrides[get_forecast_service] = lambda: mock_svc

    try:
        client = TestClient(app)
        response = client.get("/ml/forecast", headers={"x-tenant-id": TENANT_ID})
    finally:
        app.dependency_overrides.clear()

    assert response.status_code == 200
    body = response.json()
    assert body["success"] is True
    data = body["data"]
    assert data["tenant_id"] == TENANT_ID
    assert len(data["predictions"]) == 1


def test_anomaly_endpoint_returns_results():
    from app.api.routes.anomalies import get_sales_repo, get_anomaly_service

    mock_repo = AsyncMock()
    mock_repo.get_daily_sales.return_value = make_records(30)

    mock_svc = MagicMock()
    mock_svc.detect.return_value = [
        AnomalyPoint(date(2024, 1, 1), 100.0, 0.5, False),
        AnomalyPoint(date(2024, 1, 2), 500.0, 3.1, True),
    ]

    app.dependency_overrides[get_sales_repo] = lambda: mock_repo
    app.dependency_overrides[get_anomaly_service] = lambda: mock_svc

    try:
        client = TestClient(app)
        response = client.get("/ml/anomalies", headers={"x-tenant-id": TENANT_ID})
    finally:
        app.dependency_overrides.clear()

    assert response.status_code == 200
    body = response.json()
    assert body["success"] is True
    data = body["data"]
    assert data["anomaly_count"] == 1
    assert data["total_days"] == 2


def test_forecast_insufficient_data_returns_422():
    from app.api.routes.forecast import get_sales_repo

    mock_repo = AsyncMock()
    mock_repo.get_daily_sales.return_value = make_records(1)

    app.dependency_overrides[get_sales_repo] = lambda: mock_repo

    try:
        client = TestClient(app)
        response = client.get("/ml/forecast", headers={"x-tenant-id": TENANT_ID})
    finally:
        app.dependency_overrides.clear()

    assert response.status_code == 422
    body = response.json()
    assert body["success"] is False
    assert body["error_code"] == "INSUFFICIENT_DATA"


def test_health_endpoint():
    client = TestClient(app)
    response = client.get("/health")
    assert response.status_code == 200
    body = response.json()
    assert body["success"] is True
    assert body["data"]["status"] == "ok"
