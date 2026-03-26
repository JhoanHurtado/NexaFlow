# start-local.ps1
# Levanta el API Gateway emulator + todos los handlers en paralelo.
# Uso: .\start-local.ps1
# Detener: Ctrl+C

$DB       = "Host=localhost;Port=5432;Database=nexapos_db;Username=jhoan_admin;Password=nexapassword123"
$PROJECT  = $PSScriptRoot
$EXE      = "$PROJECT\bin\Debug\net10.0\NexaFlow.NexaPOS.exe"
$TEMPLATE = "$PROJECT\serverless.template"
$API      = "localhost:5050"

# Compilar
Write-Host "Compilando..." -ForegroundColor Cyan
dotnet build $PROJECT -v q
if ($LASTEXITCODE -ne 0) { Write-Host "Build fallido." -ForegroundColor Red; exit 1 }

if (-not (Test-Path $EXE)) {
    Write-Host "No se encontro el ejecutable: $EXE" -ForegroundColor Red
    exit 1
}

# Arrancar el test tool con API Gateway emulator
Write-Host "Iniciando API Gateway emulator en http://localhost:5050 ..." -ForegroundColor Cyan
$toolJob = Start-Job -ScriptBlock {
    param($proj, $tmpl)
    Set-Location $proj
    dotnet-lambda-test-tool-10.0 --port 5050 --template $tmpl --api-gateway-emulator-mode Rest
} -ArgumentList $PROJECT, $TEMPLATE

Start-Sleep -Seconds 3

# Handlers
$handlers = @("Create","List","CreateCustomer","ListCustomers","CreateSale","ListSales","GetSaleById")

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
Write-Host "Endpoints disponibles en http://localhost:5050 :" -ForegroundColor Yellow
Write-Host "  POST   /products"
Write-Host "  GET    /products"
Write-Host "  POST   /customers"
Write-Host "  GET    /customers"
Write-Host "  POST   /sales"
Write-Host "  GET    /sales"
Write-Host "  GET    /sales/{id}"
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
