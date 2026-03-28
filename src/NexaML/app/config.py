import os
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    db_connection: str = ""
    aws_region: str = "us-east-1"
    bedrock_model_id: str = "anthropic.claude-3-haiku-20240307-v1:0"
    forecast_horizon_days: int = 7
    anomaly_zscore_threshold: float = 2.5

    def get_db_dsn(self) -> str:
        """Returns the DB DSN, raising a clear error if not configured."""
        if not self.db_connection or not self.db_connection.startswith(("postgresql", "postgres")):
            raise RuntimeError(
                f"DB_CONNECTION env var is missing or invalid. "
                f"Expected a postgresql:// DSN, got: '{self.db_connection}'"
            )
        return self.db_connection.replace("postgresql+asyncpg://", "postgresql://")


settings = Settings()
