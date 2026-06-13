# ─── Uso ──────────────────────────────────────────────────────────────────────
# .\release.ps1 1.0.0
# .\release.ps1 1.0.0 nexaauth,nexapos        ← solo servicios específicos
# ─────────────────────────────────────────────────────────────────────────────
param(
    [Parameter(Mandatory)][string]$Version,
    [string[]]$Services
)

$ALL_SERVICES = @("nexaauth","nexabook","nexapos","nexainsight","nexaml","nexaflow-web")

$MESSAGES = @{
    "nexaauth" = @"
NexaAuth & Billing v$Version

Microservicio de identidad, control de acceso y facturacion.

Endpoints:
  POST   /auth/register        - Registro de nuevo tenant con plan inicial
  POST   /auth/login           - Autenticacion JWT con credenciales de tenant
  GET    /tenants/:id          - Info publica del tenant (nombre, plan activo)
  GET    /plans                - Listado de planes disponibles (publico)
  GET    /subscriptions/status - Estado de suscripcion activa del tenant
  POST   /webhooks/stripe      - Webhook de Stripe para eventos de pago
  POST   /users                - Crear usuario con rol dentro del tenant
  GET    /users                - Listar usuarios del tenant
  DELETE /users/:id            - Desactivar usuario

Stack: .NET 10 · Lambda (Zip) · PostgreSQL · JWT · Stripe · AWS SAM
"@
    "nexabook" = @"
NexaBook v$Version

Microservicio de reservas con agenda diaria y flujo completo de estados.

Endpoints:
  POST   /customers                   - Registrar cliente
  PUT    /customers/:id               - Actualizar datos de cliente
  GET    /customers/:id               - Obtener cliente por ID
  GET    /customers                   - Listar clientes del tenant
  POST   /customers/find-or-create    - Buscar o crear cliente por telefono/email
  GET    /availability                - Slots disponibles para una fecha
  POST   /reservations                - Crear reserva (pending)
  GET    /reservations/:id            - Detalle de reserva
  GET    /reservations/customer/:id   - Historial de reservas de un cliente
  GET    /reservations                - Listar todas las reservas del tenant
  GET    /agenda                      - Vista de agenda diaria con reservas
  PATCH  /reservations/:id/confirm    - Confirmar reserva (pending -> confirmed)
  PATCH  /reservations/:id/arrived    - Marcar llegada (confirmed -> arrived)
  PATCH  /reservations/:id/complete   - Completar reserva (arrived -> completed)
  PATCH  /reservations/:id/cancel     - Cancelar reserva
  PATCH  /reservations/:id/reschedule - Reprogramar reserva
  GET    /reservations/summary        - Resumen de reservas por periodo

Stack: .NET 10 · Lambda (Zip) · PostgreSQL · AWS SAM
"@
    "nexapos" = @"
NexaPOS v$Version

Microservicio de punto de venta: productos, clientes, ventas y configuracion.

Endpoints:
  POST  /products         - Crear producto con precio y stock
  GET   /products         - Listar productos del tenant (hasta 200)
  POST  /customers        - Registrar cliente POS
  GET   /customers        - Listar clientes del tenant
  POST  /sales            - Crear venta con lineas de detalle e IVA
  GET   /sales            - Listar ventas con paginacion (hasta 200)
  GET   /sales/:id        - Detalle de venta con lineas
  PATCH /sales/:id/status - Cambiar estado de factura (pending/paid/cancelled)
  GET   /config           - Obtener configuracion de horarios y slot
  PUT   /config           - Actualizar horario de apertura/cierre y duracion de slot

Stack: .NET 10 · Lambda (Zip) · PostgreSQL · AWS SAM
"@
    "nexainsight" = @"
NexaInsight v$Version

Microservicio de analitica de negocio con indicadores clave en tiempo real.

Endpoints:
  GET /insights/average-ticket    - Ticket promedio de ventas por periodo
  GET /insights/cancellation-rate - Tasa de cancelacion de reservas
  GET /insights/daily-summary     - Resumen diario: ventas, reservas e ingresos
  GET /insights/top-products      - Productos mas vendidos por cantidad e ingreso
  GET /insights/low-stock         - Alertas de productos con stock bajo umbral

Stack: .NET 10 · Lambda (Zip) · PostgreSQL · AWS SAM
"@
    "nexaml" = @"
NexaML v$Version

Microservicio de inteligencia artificial: prediccion, anomalias e insights con LLM.

Endpoints:
  GET /ml/forecast  - Prediccion de ingresos para los proximos 7 dias (Prophet)
  GET /ml/anomalies - Deteccion de anomalias en ventas historicas (Z-score)
  GET /ml/insights  - Resumen ejecutivo generado con Amazon Bedrock (Claude)

Stack: Python 3.12 · FastAPI · Lambda (Docker/ECR) · Prophet · Amazon Bedrock · PostgreSQL · AWS SAM
"@
    "nexaflow-web" = @"
NexaFlow Web v$Version

SPA React/Vite multi-tenant desplegada en S3 con routing del lado del cliente.

Modulos:
  POS           - Punto de venta con historial de facturas, cambio de estado y busqueda
  Inventario    - Gestion de productos y stock
  Reservas      - Agenda diaria, listado y gestion de estados de reservas
  Analytics     - Dashboard con metricas de NexaInsight e insights de IA (NexaML)
  Configuracion - Horarios, duracion de slot y ajustes del tenant
  Portal publico (/book/:tenantId)      - Stepper de reserva en 3 pasos (calendario -> datos -> confirmacion)
  Portal publico (/book/menu/:tenantId) - Menu digital con filtros por categoria y modal de detalle

Stack: React 18 · TypeScript · Vite · pnpm · S3 Static Hosting
"@
}

# Si no se pasaron servicios, usar todos
if (-not $Services -or $Services.Count -eq 0) {
    $Services = $ALL_SERVICES
}

# Validar servicios
foreach ($svc in $Services) {
    if (-not $MESSAGES.ContainsKey($svc)) {
        Write-Error "Servicio desconocido '$svc'. Validos: $($ALL_SERVICES -join ', ')"
        exit 1
    }
}

# Verificar que los tags no existan
Write-Host "Verificando tags existentes..."
foreach ($svc in $Services) {
    $tag = "$svc/v$Version"
    $exists = git rev-parse $tag 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Error "El tag '$tag' ya existe. Eliminalo con: git tag -d $tag; git push origin :refs/tags/$tag"
        exit 1
    }
}

# Crear tags
Write-Host ""
Write-Host "Creando tags para v$Version..."
foreach ($svc in $Services) {
    $tag = "$svc/v$Version"
    git tag -a $tag -m $MESSAGES[$svc]
    Write-Host "  v $tag"
}

# Confirmar push
Write-Host ""
$confirm = Read-Host "Publicar $($Services.Count) tag(s) en origin? [s/N]"
if ($confirm -notmatch '^[sS]$') {
    Write-Host "Push cancelado. Tags locales creados. Para publicar: git push origin --tags"
    exit 0
}

# Push
Write-Host ""
Write-Host "Publicando tags..."
foreach ($svc in $Services) {
    $tag = "$svc/v$Version"
    git push origin $tag
    Write-Host "  v pushed $tag"
}

Write-Host ""
Write-Host "Release v$Version publicado para: $($Services -join ', ')"
