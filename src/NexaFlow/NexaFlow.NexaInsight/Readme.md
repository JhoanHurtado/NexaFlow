# NexaFlow.NexaInsight

Microservicio serverless de analítica para la plataforma NexaFlow. Consume datos de NexaPOS y NexaBook para generar inteligencia de negocio en tiempo real.

---

## Responsabilidades

- Calcular el **ticket promedio** de ventas por rango de fechas
- Analizar la **tasa de cancelación** de reservas
- Generar **resumen diario** de ventas (revenue, cantidad, promedio)

Este servicio es de **solo lectura** — no escribe en ninguna tabla operativa. Consulta directamente `sales` y `reservations` con RLS activado.

---

## Arquitectura

```
NexaFlow.NexaInsight                  → Lambda entry point (Handlers, Startup)
NexaFlow.NexaInsight.Application      → Servicios, interfaces, DTOs
NexaFlow.NexaInsight.Domain           → Entidades de resultado (records), excepciones
NexaFlow.NexaInsight.Infrastructura   → Repositorios de consulta (Dapper + PostgreSQL)
```

### Patrones

- **Clean Architecture** — dominio sin dependencias externas
- **Read-only Repository** — solo consultas SQL analíticas, sin escrituras
- **RLS** — cada query aplica `SET app.tenant_id` para aislamiento multi-tenant
- **Window functions** — SQL eficiente con `COUNT`, `SUM`, `AVG`, `FILTER`

---

## Endpoints

Todos requieren `x-tenant-id: <guid>` y parámetros `from` / `to` en formato `YYYY-MM-DD`.

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/insights/average-ticket` | Ticket promedio en el rango |
| `GET` | `/insights/cancellation-rate` | Tasa de cancelación de reservas |
| `GET` | `/insights/daily-summary` | Resumen diario de ventas (máx. 90 días) |

### Ejemplos

```bash
# Ticket promedio de enero 2024
curl "https://<api>/insights/average-ticket?from=2024-01-01&to=2024-01-31" \
  -H "x-tenant-id: <tenant-uuid>"

# Tasa de cancelación
curl "https://<api>/insights/cancellation-rate?from=2024-01-01&to=2024-01-31" \
  -H "x-tenant-id: <tenant-uuid>"

# Resumen diario (máx. 90 días)
curl "https://<api>/insights/daily-summary?from=2024-01-01&to=2024-01-31" \
  -H "x-tenant-id: <tenant-uuid>"
```

### Respuestas

**GET /insights/average-ticket**
```json
{
  "tenantId": "...",
  "average": 45.50,
  "totalRevenue": 4550.00,
  "saleCount": 100,
  "from": "2024-01-01",
  "to": "2024-01-31"
}
```

**GET /insights/cancellation-rate**
```json
{
  "tenantId": "...",
  "totalReservations": 80,
  "cancelledReservations": 12,
  "ratePercent": 15.00,
  "from": "2024-01-01",
  "to": "2024-01-31"
}
```

---

## Variables de entorno

| Variable | Descripción |
|----------|-------------|
| `DB_CONNECTION` | Cadena de conexión PostgreSQL |

---

## Validaciones de negocio

- `from` no puede ser posterior a `to` → `400 Bad Request`
- Rango máximo para `/daily-summary`: 90 días → `400 Bad Request`

---

## Tests

```bash
cd NexaFlow.NexaInsight.Tests
dotnet test
```

### Qué se prueba

- Que `InsightService` mapea correctamente los resultados del repositorio a DTOs
- Que se lanza `DomainException` cuando `from > to`
- Que se lanza `DomainException` cuando el rango supera 90 días en daily-summary
- Entidades de dominio (records) con valores límite (0 ventas, 100% cancelación)

---

## Fase 2 — Evolución prevista

En la Fase 2 este servicio incorporará:

- Procesamiento asíncrono via **SQS consumer** (Lambda)
- Tablas precalculadas: `daily_metrics`, `hourly_metrics`, `product_metrics`
- Idempotencia en el procesamiento de eventos
- Ranking de productos más vendidos
- Conversión reserva → venta

En la Fase 3 se integrará con el microservicio Python (NexaML) para predicciones y detección de anomalías.

---

## Deploy

```bash
sam build --template serverless.template --use-container
sam deploy --guided \
  --parameter-overrides DbConnection="Host=...;Database=NexosNexaFlow;..."
```
