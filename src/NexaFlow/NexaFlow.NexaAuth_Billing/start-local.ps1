# start-local.ps1 — NexaAuth_Billing
# Levanta el API Gateway emulator + todos los handlers en paralelo.
# Uso: .\start-local.ps1
# Detener: Ctrl+C

$DB         = "Host=localhost;Database=NexosNexaFlow;Username=post_usr;Password=P3assW0e"
$JWT_SECRET = "nexaflow-dev-secret-min32chars!!"
$JWT_ISSUER = "nexaflow"
$PROJECT    = $PSScriptRoot
$EXE        = "$PROJECT\bin\Debug\net10.0\NexaFlow.NexaAuth_Billing.exe"
$TEMPLATE   = "$PROJECT\serverless.template"
$PORT       = 5052
$API        = "localhost:$PORT"

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkMagenta
Write-Host "  NexaAuth_Billing — Local Dev" -ForegroundColor Magenta
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkMagenta

Write-Host "Compilando..." -ForegroundColor Magenta
dotnet build $PROJECT -v q
if ($LASTEXITCODE -ne 0) { Write-Host "Build fallido." -ForegroundColor Red; exit 1 }

if (-not (Test-Path $EXE)) {
    Write-Host "No se encontro el ejecutable: $EXE" -ForegroundColor Red; exit 1
}

Write-Host "Iniciando API Gateway emulator en http://localhost:$PORT ..." -ForegroundColor Magenta
$toolJob = Start-Job -ScriptBlock {
    param($proj, $tmpl, $port)
    Set-Location $proj
    dotnet-lambda-test-tool-10.0 --port $port --template $tmpl --api-gateway-emulator-mode Rest
} -ArgumentList $PROJECT, $TEMPLATE, $PORT

Start-Sleep -Seconds 3

$handlers = @(
    "Register", "Login",
    "CreateUser", "ListUsers", "DeactivateUser",
    "GetStatus", "StripeWebhook"
)

$jobs = @()
foreach ($handler in $handlers) {
    Write-Host "  Iniciando handler: $handler" -ForegroundColor Green
    $jobs += Start-Job -ScriptBlock {
        param($exe, $api, $db, $secret, $issuer, $h)
        $env:AWS_LAMBDA_RUNTIME_API = $api
        $env:DB_CONNECTION          = $db
        $env:JWT_SECRET             = $secret
        $env:JWT_ISSUER             = $issuer
        $env:ANNOTATIONS_HANDLER    = $h
        & $exe
    } -ArgumentList $EXE, $API, $DB, $JWT_SECRET, $JWT_ISSUER, $handler
}

Write-Host ""
Write-Host "Endpoints disponibles en http://localhost:$PORT :" -ForegroundColor Yellow
Write-Host "  POST   /auth/register"
Write-Host "  POST   /auth/login"
Write-Host "  POST   /users"
Write-Host "  GET    /users"
Write-Host "  DELETE /users/{id}"
Write-Host "  GET    /subscriptions/status"
Write-Host "  POST   /webhooks/stripe"
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
