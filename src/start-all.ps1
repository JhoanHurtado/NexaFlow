# start-all.ps1 — NexaFlow Suite
# Levanta todos los microservicios en paralelo para pruebas locales.
#
# Puertos:
#   5050 → NexaPOS          (productos, ventas, clientes)
#   5051 → NexaBook         (reservas, agenda)
#   5052 → NexaAuth_Billing (auth, usuarios, suscripciones)
#   5053 → NexaInsight      (analytics)
#   5054 → NexaML           (forecast, anomalías, insights LLM)
#
# Uso:    .\start-all.ps1
# Detener: Ctrl+C

# ─────────────────────────────────────────
# CONFIGURACIÓN — edita aquí tus credenciales
# ─────────────────────────────────────────
$DB         = "Host=localhost;Port=5432;Database=NexosNexaFlow;Username=post_usr;Password=P3assW0e"
$JWT_SECRET = "nexaflow-dev-secret-min32chars!!"
$JWT_ISSUER = "nexaflow"
$ML_DB      = "postgresql+asyncpg://post_usr:P3assW0e@localhost:5432/NexosNexaFlow"

# Rutas base
$ROOT    = Split-Path $PSScriptRoot -Parent   # src/NexaFlow
$DOTNET  = $ROOT
$ML_ROOT = Split-Path $ROOT -Parent | Join-Path -ChildPath "NexaML"   # src/NexaML

# ─────────────────────────────────────────
# BANNER
# ─────────────────────────────────────────
Clear-Host
Write-Host ""
Write-Host "  ███╗   ██╗███████╗██╗  ██╗ █████╗ ███████╗██╗      ██████╗ ██╗    ██╗" -ForegroundColor Cyan
Write-Host "  ████╗  ██║██╔════╝╚██╗██╔╝██╔══██╗██╔════╝██║     ██╔═══██╗██║    ██║" -ForegroundColor Cyan
Write-Host "  ██╔██╗ ██║█████╗   ╚███╔╝ ███████║█████╗  ██║     ██║   ██║██║ █╗ ██║" -ForegroundColor Cyan
Write-Host "  ██║╚██╗██║██╔══╝   ██╔██╗ ██╔══██║██╔══╝  ██║     ██║   ██║██║███╗██║" -ForegroundColor Cyan
Write-Host "  ██║ ╚████║███████╗██╔╝ ██╗██║  ██║██║     ███████╗╚██████╔╝╚███╔███╔╝" -ForegroundColor Cyan
Write-Host "  ╚═╝  ╚═══╝╚══════╝╚═╝  ╚═╝╚═╝  ╚═╝╚═╝     ╚══════╝ ╚═════╝  ╚══╝╚══╝ " -ForegroundColor Cyan
Write-Host ""
Write-Host "  Local Dev Suite — todos los servicios" -ForegroundColor DarkCyan
Write-Host ""

# ─────────────────────────────────────────
# VERIFICAR PREREQUISITOS
# ─────────────────────────────────────────
Write-Host "Verificando prerequisitos..." -ForegroundColor DarkGray

$missing = @()
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue))                    { $missing += "dotnet SDK" }
if (-not (Get-Command dotnet-lambda-test-tool-10.0 -ErrorAction SilentlyContinue)) { $missing += "dotnet-lambda-test-tool-10.0" }
if (-not (Get-Command python3 -ErrorAction SilentlyContinue) -and
    -not (Get-Command python -ErrorAction SilentlyContinue))                    { $missing += "Python 3" }

if ($missing.Count -gt 0) {
    Write-Host ""
    Write-Host "Faltan los siguientes prerequisitos:" -ForegroundColor Red
    $missing | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    Write-Host ""
    Write-Host "Instala el test tool con:" -ForegroundColor Yellow
    Write-Host "  dotnet tool install -g Amazon.Lambda.TestTool-10.0"
    exit 1
}
Write-Host "  OK" -ForegroundColor Green

# ─────────────────────────────────────────
# COMPILAR SERVICIOS .NET
# ─────────────────────────────────────────
Write-Host ""
Write-Host "Compilando servicios .NET..." -ForegroundColor DarkGray

$services = @("NexaFlow.NexaPOS", "NexaFlow.NexaBook", "NexaFlow.NexaAuth_Billing", "NexaFlow.NexaInsight")
foreach ($svc in $services) {
    $path = "$DOTNET\$svc"
    Write-Host "  $svc..." -NoNewline
    dotnet build $path -v q 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host " FALLIDO" -ForegroundColor Red
        Write-Host "Ejecuta 'dotnet build $path' para ver el error." -ForegroundColor Yellow
        exit 1
    }
    Write-Host " OK" -ForegroundColor Green
}

# ─────────────────────────────────────────
# VERIFICAR / PREPARAR ENTORNO PYTHON
# ─────────────────────────────────────────
Write-Host ""
Write-Host "Verificando entorno Python (NexaML)..." -ForegroundColor DarkGray

$VENV = "$ML_ROOT\.venv"
$PY   = if (Get-Command python3 -ErrorAction SilentlyContinue) { "python3" } else { "python" }

if (-not (Test-Path "$VENV\Scripts\uvicorn.exe")) {
    Write-Host "  Creando entorno virtual..." -ForegroundColor Yellow
    & $PY -m venv $VENV
    & "$VENV\Scripts\pip" install -r "$ML_ROOT\requirements.txt" -q
}
Write-Host "  OK" -ForegroundColor Green

# ─────────────────────────────────────────
# FUNCIÓN HELPER — lanzar test tool
# ─────────────────────────────────────────
function Start-LambdaTool {
    param([string]$ProjectPath, [string]$Template, [int]$Port)
    return Start-Job -ScriptBlock {
        param($proj, $tmpl, $port)
        Set-Location $proj
        dotnet-lambda-test-tool-10.0 --port $port --template $tmpl --api-gateway-emulator-mode Rest
    } -ArgumentList $ProjectPath, $Template, $Port
}

# ─────────────────────────────────────────
# FUNCIÓN HELPER — lanzar handler Lambda
# ─────────────────────────────────────────
function Start-Handler {
    param([string]$Exe, [string]$Api, [hashtable]$EnvVars, [string]$Handler)
    return Start-Job -ScriptBlock {
        param($exe, $api, $envVars, $h)
        $env:AWS_LAMBDA_RUNTIME_API = $api
        $env:ANNOTATIONS_HANDLER    = $h
        foreach ($key in $envVars.Keys) {
            [System.Environment]::SetEnvironmentVariable($key, $envVars[$key])
        }
        & $exe
    } -ArgumentList $Exe, $Api, $EnvVars, $Handler
}

$allJobs = @()

# ─────────────────────────────────────────
# NexaPOS — puerto 5050
# ─────────────────────────────────────────
Write-Host ""
Write-Host "  [5050] NexaPOS" -ForegroundColor White -NoNewline
$posProject  = "$DOTNET\NexaFlow.NexaPOS"
$posExe      = "$posProject\bin\Debug\net10.0\NexaFlow.NexaPOS.exe"
$posTemplate = "$posProject\serverless.template"
$posEnv      = @{ DB_CONNECTION = $DB }
$posHandlers = @("Create","List","CreateCustomer","ListCustomers","CreateSale","ListSales","GetSaleById")

$allJobs += Start-LambdaTool $posProject $posTemplate 5050
Start-Sleep -Seconds 2
foreach ($h in $posHandlers) { $allJobs += Start-Handler $posExe "localhost:5050" $posEnv $h }
Write-Host " — $($posHandlers.Count) handlers" -ForegroundColor Green

# ─────────────────────────────────────────
# NexaBook — puerto 5051
# ─────────────────────────────────────────
Write-Host "  [5051] NexaBook" -ForegroundColor White -NoNewline
$bookProject  = "$DOTNET\NexaFlow.NexaBook"
$bookExe      = "$bookProject\bin\Debug\net10.0\NexaFlow.NexaBook.exe"
$bookTemplate = "$bookProject\serverless.template"
$bookEnv      = @{ DB_CONNECTION = $DB }
$bookHandlers = @(
    "RegisterCustomer","UpdateCustomer","GetCustomerById","ListCustomers",
    "CreateReservation","ConfirmReservation","CancelReservation",
    "MarkArrived","CompleteReservation","RescheduleReservation",
    "GetReservationById","ListReservations","GetReservationsByCustomer",
    "GetAvailability","GetAgenda","GetSummary"
)

$allJobs += Start-LambdaTool $bookProject $bookTemplate 5051
Start-Sleep -Seconds 2
foreach ($h in $bookHandlers) { $allJobs += Start-Handler $bookExe "localhost:5051" $bookEnv $h }
Write-Host " — $($bookHandlers.Count) handlers" -ForegroundColor Green

# ─────────────────────────────────────────
# NexaAuth_Billing — puerto 5052
# ─────────────────────────────────────────
Write-Host "  [5052] NexaAuth_Billing" -ForegroundColor White -NoNewline
$authProject  = "$DOTNET\NexaFlow.NexaAuth_Billing"
$authExe      = "$authProject\bin\Debug\net10.0\NexaFlow.NexaAuth_Billing.exe"
$authTemplate = "$authProject\serverless.template"
$authEnv      = @{ DB_CONNECTION = $DB; JWT_SECRET = $JWT_SECRET; JWT_ISSUER = $JWT_ISSUER }
$authHandlers = @("Register","Login","CreateUser","ListUsers","DeactivateUser","GetStatus","StripeWebhook")

$allJobs += Start-LambdaTool $authProject $authTemplate 5052
Start-Sleep -Seconds 2
foreach ($h in $authHandlers) { $allJobs += Start-Handler $authExe "localhost:5052" $authEnv $h }
Write-Host " — $($authHandlers.Count) handlers" -ForegroundColor Green

# ─────────────────────────────────────────
# NexaInsight — puerto 5053
# ─────────────────────────────────────────
Write-Host "  [5053] NexaInsight" -ForegroundColor White -NoNewline
$insightProject  = "$DOTNET\NexaFlow.NexaInsight"
$insightExe      = "$insightProject\bin\Debug\net10.0\NexaFlow.NexaInsight.exe"
$insightTemplate = "$insightProject\serverless.template"
$insightEnv      = @{ DB_CONNECTION = $DB }
$insightHandlers = @("GetAverageTicket","GetCancellationRate","GetDailySummary")

$allJobs += Start-LambdaTool $insightProject $insightTemplate 5053
Start-Sleep -Seconds 2
foreach ($h in $insightHandlers) { $allJobs += Start-Handler $insightExe "localhost:5053" $insightEnv $h }
Write-Host " — $($insightHandlers.Count) handlers" -ForegroundColor Green

# ─────────────────────────────────────────
# NexaML — puerto 5054 (FastAPI / uvicorn)
# ─────────────────────────────────────────
Write-Host "  [5054] NexaML (FastAPI)" -ForegroundColor White -NoNewline
$mlJob = Start-Job -ScriptBlock {
    param($mlRoot, $venv, $mlDb, $region, $modelId, $port)
    $env:DB_CONNECTION    = $mlDb
    $env:AWS_REGION       = $region
    $env:BEDROCK_MODEL_ID = $modelId
    Set-Location $mlRoot
    & "$venv\Scripts\uvicorn" app.main:app --host 0.0.0.0 --port $port
} -ArgumentList $ML_ROOT, $VENV, $ML_DB, "us-east-1", "anthropic.claude-3-haiku-20240307-v1:0", 5054
$allJobs += $mlJob
Write-Host " — uvicorn" -ForegroundColor Green

# ─────────────────────────────────────────
# RESUMEN
# ─────────────────────────────────────────
Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkCyan
Write-Host "  Todos los servicios levantados. Endpoints disponibles:" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkCyan
Write-Host ""
Write-Host "  NexaPOS          http://localhost:5050" -ForegroundColor White
Write-Host "    POST  /products          POST  /customers"
Write-Host "    GET   /products          GET   /customers"
Write-Host "    POST  /sales             GET   /sales"
Write-Host "    GET   /sales/{id}"
Write-Host ""
Write-Host "  NexaBook         http://localhost:5051" -ForegroundColor White
Write-Host "    POST  /customers         GET   /customers"
Write-Host "    POST  /reservations      GET   /reservations"
Write-Host "    POST  /reservations/{id}/confirm|cancel|arrived|complete|reschedule"
Write-Host "    GET   /reservations/availability    GET   /agenda"
Write-Host ""
Write-Host "  NexaAuth_Billing http://localhost:5052" -ForegroundColor White
Write-Host "    POST  /auth/register     POST  /auth/login"
Write-Host "    POST  /users             GET   /users"
Write-Host "    DELETE /users/{id}       GET   /subscriptions/status"
Write-Host "    POST  /webhooks/stripe"
Write-Host ""
Write-Host "  NexaInsight      http://localhost:5053" -ForegroundColor White
Write-Host "    GET   /insights/average-ticket?from=&to="
Write-Host "    GET   /insights/cancellation-rate?from=&to="
Write-Host "    GET   /insights/daily-summary?from=&to="
Write-Host ""
Write-Host "  NexaML           http://localhost:5054" -ForegroundColor White
Write-Host "    GET   /health"
Write-Host "    GET   /ml/forecast       GET   /ml/anomalies"
Write-Host "    GET   /ml/insights       GET   /docs  (Swagger UI)"
Write-Host ""
Write-Host "  Archivos .http por servicio para probar con VS Code REST Client:" -ForegroundColor DarkGray
Write-Host "    src/NexaFlow/NexaFlow.NexaPOS/local.http"
Write-Host "    src/NexaFlow/NexaFlow.NexaBook/local.http"
Write-Host "    src/NexaFlow/NexaFlow.NexaAuth_Billing/local.http"
Write-Host "    src/NexaFlow/NexaFlow.NexaInsight/local.http"
Write-Host "    src/NexaML/local.http"
Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkCyan
Write-Host "  Presiona Ctrl+C para detener todos los servicios." -ForegroundColor Red
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkCyan
Write-Host ""

# ─────────────────────────────────────────
# LOOP — mantener vivo y mostrar logs
# ─────────────────────────────────────────
try {
    while ($true) {
        Start-Sleep -Seconds 5
        $allJobs | Receive-Job 2>&1 | Where-Object { $_ -match "\[ERROR\]|\[WARN\]|Exception|error" } |
            ForEach-Object { Write-Host "  $_" -ForegroundColor DarkYellow }
    }
} finally {
    Write-Host ""
    Write-Host "Deteniendo todos los servicios..." -ForegroundColor Red
    $allJobs | Stop-Job
    $allJobs | Remove-Job -Force
    Write-Host "Listo." -ForegroundColor Green
}
