# NexaFlow.NexaAuth_Billing

Microservicio serverless de identidad, permisos y facturación para la plataforma NexaFlow. Es el **SaaS Manager**: gestiona tenants, usuarios con RBAC, JWT y sincronización con Stripe.

---

## Responsabilidades

- Registro de nuevos negocios (Tenants) con su usuario `owner`
- Login y emisión de JWT con claims de tenant, rol y usuario
- Gestión de usuarios con roles (`owner`, `admin`, `staff`)
- Sincronización con Stripe via Webhooks (idempotente)
- Validación de suscripción activa para otros microservicios

---

## Arquitectura

```
NexaFlow.NexaAuth_Billing                → Lambda entry point (Handlers, Startup)
NexaFlow.NexaAuth_Billing.Application    → Servicios, interfaces, DTOs, Records
NexaFlow.NexaAuth_Billing.Domain         → Entidades, eventos, excepciones
NexaFlow.NexaAuth_Billing.Infrastructure → Repositorios, JWT, BCrypt, Logger
```

### Patrones

- **Clean Architecture** — dominio sin dependencias externas
- **Repository Pattern** — `ITenantRepository`, `IUserRepository`, `ISubscriptionRepository`, `IWebhookEventRepository`
- **Idempotencia en Webhooks** — `stripe_webhook_events` previene procesamiento duplicado
- **RBAC** — roles validados en dominio: `owner`, `admin`, `staff`
- **JWT** — tokens firmados con HMAC-SHA256, claims: `sub`, `tenant_id`, `email`, `role`
- **BCrypt** — hash de contraseñas con work factor 12

---

## Tablas gestionadas

| Tabla | Responsabilidad |
|-------|----------------|
| `tenants` | Registro de negocios |
| `users` | Usuarios con roles por tenant |
| `subscriptions` | Estado de suscripción Stripe |
| `payments` | Historial de pagos |
| `stripe_products` | Catálogo de productos Stripe |
| `stripe_prices` | Precios de Stripe |
| `stripe_webhook_events` | Eventos recibidos (idempotencia) |
| `plans` | Planes disponibles |

---

## Endpoints

Todos los endpoints de gestión requieren `x-tenant-id: <guid>`.

### Autenticación

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/auth/register` | Registra negocio + usuario owner |
| `POST` | `/auth/login` | Login, retorna JWT |

### Usuarios

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/users` | Crear usuario en el tenant |
| `GET` | `/users` | Listar usuarios del tenant |
| `DELETE` | `/users/{id}` | Desactivar usuario |

### Suscripciones

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/subscriptions/status` | Estado de suscripción del tenant |
| `POST` | `/webhooks/stripe` | Recibir eventos de Stripe |

---

## Variables de entorno

| Variable | Descripción | Ejemplo |
|----------|-------------|---------|
| `DB_CONNECTION` | Cadena de conexión PostgreSQL | `Host=...;Database=NexosNexaFlow;...` |
| `JWT_SECRET` | Secreto de firma JWT (mín. 32 chars) | `my-super-secret-key-32chars!!` |
| `JWT_ISSUER` | Issuer del JWT | `nexaflow` |

---

## Flujo de registro

```
POST /auth/register
  │
  ├── Validar contraseña (mín. 8 chars)
  ├── Crear Tenant (dominio)
  ├── INSERT INTO tenants
  ├── Hash contraseña (BCrypt work=12)
  ├── Crear User con rol=owner (dominio)
  └── INSERT INTO users
      └── Retorna tenantId
```

## Flujo de webhook Stripe

```
POST /webhooks/stripe
  │
  ├── Verificar firma Stripe (stripe-signature header)
  ├── Verificar idempotencia (stripe_webhook_events)
  ├── INSERT INTO stripe_webhook_events (processed=FALSE)
  ├── Procesar evento:
  │   ├── customer.subscription.created → INSERT subscriptions
  │   ├── customer.subscription.updated → UPDATE subscriptions
  │   └── customer.subscription.deleted → UPDATE status=canceled
  └── UPDATE stripe_webhook_events SET processed=TRUE
```

---

## Migraciones

```
migrations/
└── 001_auth_password_hash.sql   # Agrega password_hash a users
```

Ejecutar antes del primer deploy:
```bash
psql $DB_CONNECTION -f migrations/001_auth_password_hash.sql
```

---

## Tests

```bash
cd NexaFlow.NexaAuth_Billing.Tests
dotnet test
```

### Cobertura

```bash
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"TestResults/CoverageReport" -reporttypes:Html
```

---

## Deploy

```bash
sam build --template serverless.template --use-container
sam deploy --guided \
  --parameter-overrides \
    DbConnection="Host=...;Database=NexosNexaFlow;..." \
    JwtSecret="your-secret-min-32-chars" \
    JwtIssuer="nexaflow"
```

### CI/CD

| Trigger | Entorno | Stack |
|---------|---------|-------|
| Push a `staging` | Staging | `NexaFlow-NexaAuth-staging` |
| Tag `nexaauth/v*` | Production | `NexaFlow-NexaAuth-prod` |
