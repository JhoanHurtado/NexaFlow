import os

from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
from mangum import Mangum

from app.api import router
from app.api.response import ApiResponse

app = FastAPI(
    title="NexaML",
    description="Microservicio de ML/LLM para NexaFlow — predicción, anomalías e insights",
    version="1.0.0",
)

_cors_origin = os.getenv("CORS_ORIGIN", "*")
_origins = [_cors_origin] if _cors_origin != "*" else ["*"]

# CORS — mismo patrón que NexaPOS (Api.cs)
app.add_middleware(
    CORSMiddleware,
    allow_origins=_origins,
    allow_credentials=_cors_origin != "*",
    allow_methods=["GET", "POST", "PUT", "DELETE", "OPTIONS"],
    allow_headers=["Content-Type", "Authorization", "x-tenant-id"],
)


@app.exception_handler(Exception)
async def global_exception_handler(request: Request, exc: Exception) -> JSONResponse:
    """Garantiza que cualquier error no capturado responda con ApiResponse estándar."""
    return JSONResponse(
    status_code=500,
    content=ApiResponse.fail(
        "INTERNAL_ERROR",
        str(exc) or "Error interno del servidor"
    ).model_dump(),
    headers={
        "Access-Control-Allow-Origin": os.getenv("CORS_ORIGIN", "*"),
        "Access-Control-Allow-Headers": "*",
        "Access-Control-Allow-Methods": "*",
    },
)

app.include_router(router)


@app.get("/health")
def health():
    return ApiResponse.ok({"status": "ok", "service": "NexaML"}).model_dump()


handler = Mangum(app, lifespan="off")
