import os

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from mangum import Mangum

from app.api import router

app = FastAPI(
    title="NexaML",
    description="Microservicio de ML/LLM para NexaFlow — predicción, anomalías e insights",
    version="1.0.0",
)

_cors_origin = os.getenv("CORS_ORIGIN", "*")
_origins = [_cors_origin] if _cors_origin != "*" else ["*"]

app.add_middleware(
    CORSMiddleware,
    allow_origins=_origins,
    allow_credentials=_cors_origin != "*",
    allow_methods=["*"],
    allow_headers=["Content-Type", "Authorization", "x-tenant-id"],
)

app.include_router(router)


@app.get("/health")
def health():
    return {"status": "ok", "service": "NexaML"}


handler = Mangum(app, lifespan="off")
