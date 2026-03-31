# start-local.ps1 — NexaInsight
# Levanta el API Gateway emulator + todos los handlers en paralelo.
# Uso: .\start-local.ps1
# Detener: Ctrl+C

$DB       = "Host=localhost;Database=NexosNexaFlow;Username=post_usr;Password=P3assW0e"
$PROJECT  = $PSScriptRoot
$EXE      = "$PROJECT\bin\Debug\net10.0\NexaFlow.NexaInsight.exe"
$TEMPLATE = "$PROJECT\serverless.template"
$PORT     = 5053
$API      = "localhost:$PORT"

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkYellow
Write-Host "  NexaInsight — Local Dev" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkYellow

Write-Host "Compilando..." -ForegroundColor Yellow
dotnet build $PROJECT -v q
if ($LASTEXITCODE -ne 0) { Write-Host "Build fallido." -ForegroundColor Red; exit 1 }

if (-not (Test-Path $EXE)) {
    Write-Host "No se encontro el ejecutable: $EXE" -ForegroundColor Red; exit 1
}

Write-Host "Iniciando API Gateway emulator en http://localhost:$PORT ..." -ForegroundColor Yellow
$toolJob = Start-Job -ScriptBlock {
    param($proj, $tmpl, $port)
    Set-Location $proj
    dotnet-lambda-test-tool-10.0 --port $port --template $tmpl --api-gateway-emulator-mode Rest
} -ArgumentList $PROJECT, $TEMPLATE, $PORT

Start-Sleep -Seconds 3

$handlers = @("GetAverageTicket", "GetCancellationRate", "GetDailySummary")

$jobs = @()
foreach ($handler in $handlers) {
    Write-Host "  Iniciando handler: $handler" -ForegroundColor Green
    $jobs += Start-Job -ScriptBlock {
        param($exe, $api, $db, $h)
        $env:AWS_LAMBDA_RUNTIME_API = $api
        $env:DB_CONNECTION          = $db
        $env:ANNOTATIONS_HANDLER    = $h
        & $exe
    } -ArgumentList $EXE, $API, $DB, $handler
}

Write-Host ""
Write-Host "Endpoints disponibles en http://localhost:$PORT :" -ForegroundColor Yellow
Write-Host "  GET    /insights/average-ticket?from=YYYY-MM-DD&to=YYYY-MM-DD"
Write-Host "  GET    /insights/cancellation-rate?from=YYYY-MM-DD&to=YYYY-MM-DD"
Write-Host "  GET    /insights/daily-summary?from=YYYY-MM-DD&to=YYYY-MM-DD"
Write-Host ""
Write-Host "Presiona Ctrl+C para detener todo." -ForegroundColor Red

try {
    while ($true) {
        Start-Sleep -Seconds 3
        $jobs + $toolJob | Receive-Job
    }
} finally {
    Write-Host "Deteniendo procesos..." -ForegroundColor Red
    $jobs + $toolJob | Stop-Job
    $jobs + $toolJob | Remove-Job
}
