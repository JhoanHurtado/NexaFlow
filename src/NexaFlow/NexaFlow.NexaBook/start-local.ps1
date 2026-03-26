# start-local.ps1 — NexaBook
# Levanta el API Gateway emulator + todos los handlers en paralelo.
# Uso: .\start-local.ps1
# Detener: Ctrl+C

$DB       = "Host=localhost;Port=5432;Database=NexosNexaFlow;Username=post_usr;Password=P3assW0e"
$PROJECT  = $PSScriptRoot
$EXE      = "$PROJECT\bin\Debug\net10.0\NexaFlow.NexaBook.exe"
$TEMPLATE = "$PROJECT\serverless.template"
$PORT     = 5051
$API      = "localhost:$PORT"

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkCyan
Write-Host "  NexaBook — Local Dev" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkCyan

Write-Host "Compilando..." -ForegroundColor Cyan
dotnet build $PROJECT -v q
if ($LASTEXITCODE -ne 0) { Write-Host "Build fallido." -ForegroundColor Red; exit 1 }

if (-not (Test-Path $EXE)) {
    Write-Host "No se encontro el ejecutable: $EXE" -ForegroundColor Red; exit 1
}

Write-Host "Iniciando API Gateway emulator en http://localhost:$PORT ..." -ForegroundColor Cyan
$toolJob = Start-Job -ScriptBlock {
    param($proj, $tmpl, $port)
    Set-Location $proj
    dotnet-lambda-test-tool-10.0 --port $port --template $tmpl --api-gateway-emulator-mode Rest
} -ArgumentList $PROJECT, $TEMPLATE, $PORT

Start-Sleep -Seconds 3

$handlers = @(
    "RegisterCustomer", "UpdateCustomer", "GetCustomerById", "ListCustomers",
    "CreateReservation", "ConfirmReservation", "CancelReservation",
    "MarkArrived", "CompleteReservation", "RescheduleReservation",
    "GetReservationById", "ListReservations", "GetReservationsByCustomer",
    "GetAvailability", "GetAgenda", "GetSummary"
)

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
Write-Host "  POST   /customers"
Write-Host "  PUT    /customers/{id}"
Write-Host "  GET    /customers/{id}"
Write-Host "  GET    /customers"
Write-Host "  POST   /reservations"
Write-Host "  POST   /reservations/{id}/confirm"
Write-Host "  POST   /reservations/{id}/cancel"
Write-Host "  POST   /reservations/{id}/arrived"
Write-Host "  POST   /reservations/{id}/complete"
Write-Host "  POST   /reservations/{id}/reschedule"
Write-Host "  GET    /reservations/{id}"
Write-Host "  GET    /reservations"
Write-Host "  GET    /customers/{customerId}/reservations"
Write-Host "  GET    /reservations/availability"
Write-Host "  GET    /agenda"
Write-Host "  GET    /reservations/summary"
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
