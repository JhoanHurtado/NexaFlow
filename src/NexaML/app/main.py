from fastapi import FastAPI
from mangum import Mangum

from app.api import router

app = FastAPI(
    title="NexaML",
    description="Microservicio de ML/LLM para NexaFlow — predicción, anomalías e insights",
    version="1.0.0",
)

app.include_router(router)


@app.get("/health")
def health():
    return {"status": "ok", "service": "NexaML"}


# Handler para AWS Lambda (API Gateway proxy)
handler = Mangum(app, lifespan="off")
