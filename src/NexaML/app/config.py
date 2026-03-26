from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    db_connection: str = "postgresql+asyncpg://post_usr:P3assW0e@localhost/NexosNexaFlow"

    # AWS Bedrock (LLM)
    aws_region: str = "us-east-1"
    bedrock_model_id: str = "anthropic.claude-3-haiku-20240307-v1:0"

    # Forecast
    forecast_horizon_days: int = 7
    anomaly_zscore_threshold: float = 2.5


settings = Settings()
