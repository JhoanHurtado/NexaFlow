#!/bin/bash
# start-local.sh — NexaFlow desarrollo local sin Docker ni Kubernetes
# Levanta los microservicios .NET con dotnet run y el frontend con pnpm dev
#
# Puertos:
#   5050 → NexaPOS
#   5051 → NexaBook
#   5052 → NexaAuth
#   5053 → NexaInsight
#   5054 → NexaML
#   5173 → NexaWeb (Vite dev server)
#
# Uso: ./start-local.sh
# Detener: Ctrl+C

ROOT="$(cd "$(dirname "$0")" && pwd)"
DOTNET="$ROOT/NexaFlow"
ML_ROOT="$ROOT/NexaML"
WEB_ROOT="$DOTNET/NexaFlow-web"

DB="Host=localhost;Port=5432;Database=NexosNexaFlow;Username=post_usr;Password=P3assW0e"
JWT_SECRET="nexaflow-dev-secret-min32chars!!"
JWT_ISSUER="nexaflow"
ML_DB="postgresql+asyncpg://post_usr:P3assW0e@localhost:5432/NexosNexaFlow"

PIDS=()

cleanup() {
  echo ""
  echo "Deteniendo servicios..."
  for pid in "${PIDS[@]}"; do kill "$pid" 2>/dev/null; done
  wait
  echo "Listo."
  exit 0
}
trap cleanup INT TERM

echo ""
echo "Iniciando NexaFlow en modo local..."
echo ""

# NexaPOS — 5050
DB_CONNECTION="$DB" \
  dotnet run --project "$DOTNET/NexaFlow.NexaPOS.API" \
  --urls "http://localhost:5050" &
PIDS+=($!)
echo "  [5050] NexaPOS iniciando..."

# NexaBook — 5051
DB_CONNECTION="$DB" \
  dotnet run --project "$DOTNET/NexaFlow.NexaBook.API" \
  --urls "http://localhost:5051" &
PIDS+=($!)
echo "  [5051] NexaBook iniciando..."

# NexaAuth — 5052
DB_CONNECTION="$DB" JWT_SECRET="$JWT_SECRET" JWT_ISSUER="$JWT_ISSUER" \
  dotnet run --project "$DOTNET/NexaFlow.NexaAuth_Billing.API" \
  --urls "http://localhost:5052" &
PIDS+=($!)
echo "  [5052] NexaAuth iniciando..."

# NexaInsight — 5053
DB_CONNECTION="$DB" \
  dotnet run --project "$DOTNET/NexaFlow.NexaInsight.API" \
  --urls "http://localhost:5053" &
PIDS+=($!)
echo "  [5053] NexaInsight iniciando..."

# NexaML — 5054
if [ -d "$ML_ROOT/.venv" ]; then
  DB_DSN_PYTHON="$ML_DB" \
    "$ML_ROOT/.venv/bin/uvicorn" app.main:app \
    --host 0.0.0.0 --port 5054 --app-dir "$ML_ROOT" &
  PIDS+=($!)
  echo "  [5054] NexaML iniciando..."
else
  echo "  [5054] NexaML omitido — crea el venv con: python3 -m venv src/NexaML/.venv && src/NexaML/.venv/bin/pip install -r src/NexaML/requirements.txt"
fi

# NexaWeb — 5173 (Vite usa .env.local automáticamente)
cd "$WEB_ROOT" && pnpm dev &
PIDS+=($!)
echo "  [5173] NexaWeb (Vite) iniciando..."

echo ""
echo "Servicios disponibles:"
echo "  Frontend  → http://localhost:5173"
echo "  NexaAuth  → http://localhost:5052/auth/swagger"
echo "  NexaPOS   → http://localhost:5050/pos/swagger"
echo "  NexaBook  → http://localhost:5051/book/swagger"
echo "  NexaInsight → http://localhost:5053/insight/swagger"
echo "  NexaML    → http://localhost:5054/docs"
echo ""
echo "Presiona Ctrl+C para detener todo."
echo ""

wait
