#!/usr/bin/env bash
set -euo pipefail

# ─── Uso ──────────────────────────────────────────────────────────────────────
# ./release.sh 1.0.0
# ./release.sh 1.0.0 nexaauth nexapos        ← solo servicios específicos
# ─────────────────────────────────────────────────────────────────────────────

VERSION="${1:-}"
if [[ -z "$VERSION" ]]; then
  echo "Uso: $0 <version> [servicio...]"
  echo "Ejemplo: $0 1.0.0"
  echo "Ejemplo: $0 1.0.0 nexaauth nexapos"
  exit 1
fi

ALL_SERVICES=(nexaauth nexabook nexapos nexainsight nexaml nexaflow-web)

msg_nexaauth="NexaAuth & Billing v${VERSION}

Microservicio de identidad, control de acceso y facturación.

Endpoints:
  POST   /auth/register        — Registro de nuevo tenant con plan inicial
  POST   /auth/login           — Autenticación JWT con credenciales de tenant
  GET    /tenants/:id          — Info pública del tenant (nombre, plan activo)
  GET    /plans                — Listado de planes disponibles (público)
  GET    /subscriptions/status — Estado de suscripción activa del tenant
  POST   /webhooks/stripe      — Webhook de Stripe para eventos de pago
  POST   /users                — Crear usuario con rol dentro del tenant
  GET    /users                — Listar usuarios del tenant
  DELETE /users/:id            — Desactivar usuario

Stack: .NET 10 · Lambda (Zip) · PostgreSQL · JWT · Stripe · AWS SAM"

msg_nexabook="NexaBook v${VERSION}

Microservicio de reservas con agenda diaria y flujo completo de estados.

Endpoints:
  POST   /customers                   — Registrar cliente
  PUT    /customers/:id               — Actualizar datos de cliente
  GET    /customers/:id               — Obtener cliente por ID
  GET    /customers                   — Listar clientes del tenant
  POST   /customers/find-or-create    — Buscar o crear cliente por teléfono/email
  GET    /availability                — Slots disponibles para una fecha
  POST   /reservations                — Crear reserva (pending)
  GET    /reservations/:id            — Detalle de reserva
  GET    /reservations/customer/:id   — Historial de reservas de un cliente
  GET    /reservations                — Listar todas las reservas del tenant
  GET    /agenda                      — Vista de agenda diaria con reservas
  PATCH  /reservations/:id/confirm    — Confirmar reserva (pending → confirmed)
  PATCH  /reservations/:id/arrived    — Marcar llegada (confirmed → arrived)
  PATCH  /reservations/:id/complete   — Completar reserva (arrived → completed)
  PATCH  /reservations/:id/cancel     — Cancelar reserva
  PATCH  /reservations/:id/reschedule — Reprogramar reserva
  GET    /reservations/summary        — Resumen de reservas por período

Stack: .NET 10 · Lambda (Zip) · PostgreSQL · AWS SAM"

msg_nexapos="NexaPOS v${VERSION}

Microservicio de punto de venta: productos, clientes, ventas y configuración.

Endpoints:
  POST  /products         — Crear producto con precio y stock
  GET   /products         — Listar productos del tenant (hasta 200)
  POST  /customers        — Registrar cliente POS
  GET   /customers        — Listar clientes del tenant
  POST  /sales            — Crear venta con líneas de detalle e IVA
  GET   /sales            — Listar ventas con paginación (hasta 200)
  GET   /sales/:id        — Detalle de venta con líneas
  PATCH /sales/:id/status — Cambiar estado de factura (pending/paid/cancelled)
  GET   /config           — Obtener configuración de horarios y slot
  PUT   /config           — Actualizar horario de apertura/cierre y duración de slot

Stack: .NET 10 · Lambda (Zip) · PostgreSQL · AWS SAM"

msg_nexainsight="NexaInsight v${VERSION}

Microservicio de analítica de negocio con indicadores clave en tiempo real.

Endpoints:
  GET /insights/average-ticket    — Ticket promedio de ventas por período
  GET /insights/cancellation-rate — Tasa de cancelación de reservas
  GET /insights/daily-summary     — Resumen diario: ventas, reservas e ingresos
  GET /insights/top-products      — Productos más vendidos por cantidad e ingreso
  GET /insights/low-stock         — Alertas de productos con stock bajo umbral

Stack: .NET 10 · Lambda (Zip) · PostgreSQL · AWS SAM"

msg_nexaml="NexaML v${VERSION}

Microservicio de inteligencia artificial: predicción, anomalías e insights con LLM.

Endpoints:
  GET /ml/forecast  — Predicción de ingresos para los próximos 7 días (Prophet)
  GET /ml/anomalies — Detección de anomalías en ventas históricas (Z-score)
  GET /ml/insights  — Resumen ejecutivo generado con Amazon Bedrock (Claude)

Stack: Python 3.12 · FastAPI · Lambda (Docker/ECR) · Prophet · Amazon Bedrock · PostgreSQL · AWS SAM"

msg_nexaflow_web="NexaFlow Web v${VERSION}

SPA React/Vite multi-tenant desplegada en S3 con routing del lado del cliente.

Módulos:
  POS           — Punto de venta con historial de facturas, cambio de estado y búsqueda
  Inventario    — Gestión de productos y stock
  Reservas      — Agenda diaria, listado y gestión de estados de reservas
  Analytics     — Dashboard con métricas de NexaInsight e insights de IA (NexaML)
  Configuración — Horarios, duración de slot y ajustes del tenant
  Portal público (/book/:tenantId)      — Stepper de reserva en 3 pasos (calendario → datos → confirmación)
  Portal público (/book/menu/:tenantId) — Menú digital con filtros por categoría y modal de detalle

Stack: React 18 · TypeScript · Vite · pnpm · S3 Static Hosting"

get_message() {
  case "$1" in
    nexaauth)     echo "$msg_nexaauth" ;;
    nexabook)     echo "$msg_nexabook" ;;
    nexapos)      echo "$msg_nexapos" ;;
    nexainsight)  echo "$msg_nexainsight" ;;
    nexaml)       echo "$msg_nexaml" ;;
    nexaflow-web) echo "$msg_nexaflow_web" ;;
    *) echo ""; return 1 ;;
  esac
}

# Si se pasaron servicios específicos, usarlos; si no, todos
if [[ $# -gt 1 ]]; then
  SERVICES=("${@:2}")
else
  SERVICES=("${ALL_SERVICES[@]}")
fi

# Validar que los servicios existen
for svc in "${SERVICES[@]}"; do
  if ! get_message "$svc" >/dev/null 2>&1; then
    echo "Error: servicio desconocido '$svc'"
    echo "Servicios válidos: ${ALL_SERVICES[*]}"
    exit 1
  fi
done

# Verificar que no existan los tags ya
echo "Verificando tags existentes..."
for svc in "${SERVICES[@]}"; do
  TAG="${svc}/v${VERSION}"
  if git rev-parse "$TAG" >/dev/null 2>&1; then
    echo "Error: el tag '$TAG' ya existe. Elimínalo primero con:"
    echo "  git tag -d $TAG && git push origin :refs/tags/$TAG"
    exit 1
  fi
done

# Crear los tags
echo ""
echo "Creando tags para v${VERSION}..."
for svc in "${SERVICES[@]}"; do
  TAG="${svc}/v${VERSION}"
  git tag -a "$TAG" -m "$(get_message "$svc")"
  echo "  ✓ $TAG"
done

# Confirmar antes de hacer push
echo ""
read -r -p "¿Publicar ${#SERVICES[@]} tag(s) en origin? [s/N] " confirm
if [[ ! "$confirm" =~ ^[sS]$ ]]; then
  echo "Push cancelado. Los tags locales fueron creados pero no publicados."
  echo "Para publicarlos manualmente: git push origin --tags"
  exit 0
fi

# Push
echo ""
echo "Publicando tags..."
for svc in "${SERVICES[@]}"; do
  TAG="${svc}/v${VERSION}"
  git push origin "$TAG"
  echo "  ✓ pushed $TAG"
done

echo ""
echo "Release v${VERSION} publicado para: ${SERVICES[*]}"
