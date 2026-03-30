import os
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    db_connection: str = ""
    aws_region: str = "us-east-1"
    # Lee NEXAML_BEDROCK_MODEL_ID primero; si no existe usa el fallback de Gemma
    bedrock_model_id: str = os.getenv(
        "NEXAML_BEDROCK_MODEL_ID",
        "google.gemma-3-4b-it",
    )
    forecast_horizon_days: int = 7
    anomaly_zscore_threshold: float = 2.5

    def get_db_dsn(self) -> str:
        if not self.db_connection or not self.db_connection.startswith(("postgresql", "postgres")):
            raise RuntimeError(
                f"DB_CONNECTION env var is missing or invalid. "
                f"Expected a postgresql:// DSN, got: '{self.db_connection}'"
            )
        return self.db_connection.replace("postgresql+asyncpg://", "postgresql://")


settings = Settings()
