# start-local.ps1 — NexaML
# Levanta el servidor FastAPI con uvicorn en modo reload.
# Uso: .\start-local.ps1
# Detener: Ctrl+C

$PROJECT = $PSScriptRoot
$PORT    = 5054
$VENV    = "$PROJECT\.venv"

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGreen
Write-Host "  NexaML — Local Dev (FastAPI)" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGreen

# Verificar entorno virtual
if (-not (Test-Path "$VENV\Scripts\uvicorn.exe")) {
    Write-Host "Entorno virtual no encontrado. Creando..." -ForegroundColor Yellow
    python -m venv $VENV
    & "$VENV\Scripts\pip" install -r "$PROJECT\requirements.txt" -q
    Write-Host "Dependencias instaladas." -ForegroundColor Green
}

# Variables de entorno para desarrollo local
$env:DB_CONNECTION    = "postgresql+asyncpg://post_usr:P3assW0e@localhost:5432/NexosNexaFlow"
$env:AWS_REGION       = "us-east-1"
$env:BEDROCK_MODEL_ID = "anthropic.claude-3-haiku-20240307-v1:0"

Write-Host ""
Write-Host "Iniciando FastAPI en http://localhost:$PORT ..." -ForegroundColor Green
Write-Host ""
Write-Host "Endpoints disponibles:" -ForegroundColor Yellow
Write-Host "  GET    http://localhost:$PORT/health"
Write-Host "  GET    http://localhost:$PORT/docs          (Swagger UI)"
Write-Host "  GET    http://localhost:$PORT/ml/forecast"
Write-Host "  GET    http://localhost:$PORT/ml/anomalies"
Write-Host "  GET    http://localhost:$PORT/ml/insights"
Write-Host ""
Write-Host "Presiona Ctrl+C para detener." -ForegroundColor Red
Write-Host ""

Set-Location $PROJECT
$UVICORN_PATH = Join-Path $VENV "Scripts\uvicorn.exe"
& $UVICORN_PATH app.main:app --host 0.0.0.0 --port $PORT --reload



