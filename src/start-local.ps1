# start-local.ps1 - NexaFlow desarrollo local sin Docker ni Kubernetes
# Uso: .\start-local.ps1
# Detener: Ctrl+C

$DOTNET  = "$PSScriptRoot\NexaFlow"
$ML_ROOT = "$PSScriptRoot\NexaML"
$WEB     = "$DOTNET\NexaFlow-web"

$DB         = "Host=localhost;Port=5432;Database=NexosNexaFlow;Username=post_usr;Password=P3assW0e"
$JWT_SECRET = "nexaflow-dev-secret-min32chars!!"
$JWT_ISSUER = "nexaflow"
$ML_DB      = "postgresql+asyncpg://post_usr:P3assW0e@localhost:5432/NexosNexaFlow"

$procs = @()

Write-Host ""
Write-Host "Iniciando NexaFlow en modo local..." -ForegroundColor Cyan
Write-Host ""

# NexaPOS - 5050
$env:DB_CONNECTION = $DB
$p1 = Start-Process "dotnet" -ArgumentList "run --project `"$DOTNET\NexaFlow.NexaPOS.API`" --urls http://localhost:5050" -PassThru -NoNewWindow
$procs += $p1
Write-Host "  [5050] NexaPOS iniciando (PID $($p1.Id))..." -ForegroundColor Green

# NexaBook - 5051
$p2 = Start-Process "dotnet" -ArgumentList "run --project `"$DOTNET\NexaFlow.NexaBook.API`" --urls http://localhost:5051" -PassThru -NoNewWindow
$procs += $p2
Write-Host "  [5051] NexaBook iniciando (PID $($p2.Id))..." -ForegroundColor Green

# NexaAuth - 5052
$env:JWT_SECRET = $JWT_SECRET
$env:JWT_ISSUER = $JWT_ISSUER
$p3 = Start-Process "dotnet" -ArgumentList "run --project `"$DOTNET\NexaFlow.NexaAuth_Billing.API`" --urls http://localhost:5052" -PassThru -NoNewWindow
$procs += $p3
Write-Host "  [5052] NexaAuth iniciando (PID $($p3.Id))..." -ForegroundColor Green

# NexaInsight - 5053
$p4 = Start-Process "dotnet" -ArgumentList "run --project `"$DOTNET\NexaFlow.NexaInsight.API`" --urls http://localhost:5053" -PassThru -NoNewWindow
$procs += $p4
Write-Host "  [5053] NexaInsight iniciando (PID $($p4.Id))..." -ForegroundColor Green

# NexaML - 5054
$venvUvicorn = "$ML_ROOT\.venv\Scripts\uvicorn.exe"
if (Test-Path $venvUvicorn) {
    $env:DB_DSN_PYTHON = $ML_DB
    $p5 = Start-Process $venvUvicorn -ArgumentList "app.main:app --host 0.0.0.0 --port 5054 --app-dir `"$ML_ROOT`"" -PassThru -NoNewWindow
    $procs += $p5
    Write-Host "  [5054] NexaML iniciando (PID $($p5.Id))..." -ForegroundColor Green
} else {
    Write-Host "  [5054] NexaML omitido - crea el venv con:" -ForegroundColor Yellow
    Write-Host "         python -m venv src\NexaML\.venv" -ForegroundColor Yellow
    Write-Host "         src\NexaML\.venv\Scripts\pip install -r src\NexaML\requirements.txt" -ForegroundColor Yellow
}

# NexaWeb - 5173
$p6 = Start-Process "pnpm" -ArgumentList "dev" -WorkingDirectory $WEB -PassThru -NoNewWindow
$procs += $p6
Write-Host "  [5173] NexaWeb (Vite) iniciando (PID $($p6.Id))..." -ForegroundColor Green

Write-Host ""
Write-Host "Servicios disponibles:" -ForegroundColor Cyan
Write-Host "  Frontend    -> http://localhost:5173"
Write-Host "  NexaAuth    -> http://localhost:5052/auth/swagger"
Write-Host "  NexaPOS     -> http://localhost:5050/pos/swagger"
Write-Host "  NexaBook    -> http://localhost:5051/book/swagger"
Write-Host "  NexaInsight -> http://localhost:5053/insight/swagger"
Write-Host "  NexaML      -> http://localhost:5054/docs"
Write-Host ""
Write-Host "Presiona Ctrl+C para detener todo." -ForegroundColor Red
Write-Host ""

try {
    while ($true) { Start-Sleep -Seconds 5 }
} finally {
    Write-Host "Deteniendo servicios..." -ForegroundColor Red
    $procs | ForEach-Object { if (-not $_.HasExited) { $_.Kill() } }
    Write-Host "Listo." -ForegroundColor Green
}
