import os
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    db_connection: str = ""
    aws_region: str = "us-east-1"
    bedrock_model_id: str = "anthropic.claude-3-haiku-20240307-v1:0"
    forecast_horizon_days: int = 7
    anomaly_zscore_threshold: float = 2.5


settings = Settings()

# Fail fast at cold start if DB_CONNECTION is missing
if not settings.db_connection or not settings.db_connection.startswith(("postgresql", "postgres")):
    raise RuntimeError(
        f"DB_CONNECTION env var is missing or invalid. "
        f"Expected a postgresql:// or postgresql+asyncpg:// DSN, got: '{settings.db_connection}'"
    )
