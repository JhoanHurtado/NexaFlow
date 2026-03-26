# NexaFlow.NexaPOS

Microservicio serverless de Punto de Venta (POS) para la plataforma NexaFlow. Gestiona productos, inventario, clientes y ventas por tenant, desplegado como funciones AWS Lambda con .NET 10 Native AOT.

---

## Descripción

NexaFlow es una plataforma multi-tenant SaaS. Este microservicio es responsable de:

- Gestión de productos con control de inventario (stock)
- Registro de ventas con ítems, asociación opcional a clientes y reservas
- Gestión de clientes por tenant
- Registro de eventos de negocio con patrón Outbox hacia SQS/EventBridge
- Aislamiento de datos por tenant mediante PostgreSQL Row Level Security (RLS)

Cada request requiere el header `x-tenant-id: <UUID>` para identificar el tenant.

---

## Arquitectura

Sigue **Clean Architecture** con cuatro capas:

```
NexaFlow.NexaPOS                  → Lambda entry point (Handlers, Startup, Serializer)
NexaFlow.NexaPOS.Application      → Servicios, interfaces, DTOs, Records
NexaFlow.NexaPOS.Domain           → Entidades, eventos de dominio, excepciones
NexaFlow.NexaPOS.Infrastructure   → Repositorios, UnitOfWork, Logger (PostgreSQL + Dapper)
```

### Patrones utilizados

- **Clean Architecture** — dependencias apuntan hacia adentro, dominio sin dependencias externas
- **Repository Pattern** — `IProductRepository`, `ISaleRepository`, `ICustomerRepository`, `IStockRepository`
- **Unit of Work** — `IUnitOfWork` agrupa operaciones en una sola transacción DB (producto + stock + evento)
- **Outbox Pattern** — eventos de negocio se persisten en `pos_events` dentro de la misma transacción antes de enviarse a SQS
- **Domain Events** — `SaleCreatedEvent`, `ProductCreatedEvent`, `StockLowEvent`, `StockDepletedEvent`, `CustomerCreatedEvent`
- **Dependency Injection** — registrado en `Startup.cs` con `IServiceCollection`
- **DTO / Record Pattern** — requests tipados, respuestas envueltas en `ApiResponse<T>` con paginación
- **Lambda Annotations** — `[LambdaFunction]`, `[RestApi]`, `[FromBody]`, `[FromHeader]`, `[FromQuery]`
- **Source Generator Serialization** — `LambdaFunctionJsonSerializerContext` para AOT sin reflection
- **Multi-tenant con RLS** — cada query aplica `SET app.tenant_id` para activar las políticas de PostgreSQL

---

## Endpoints

Todos los endpoints requieren el header `x-tenant-id: <guid>`.

### Productos

| Método | Ruta | Handler | Descripción |
|--------|------|---------|-------------|
| `POST` | `/products` | `Create` | Crear producto con stock inicial |
| `GET` | `/products` | `List` | Listar productos activos paginados |

### Clientes

| Método | Ruta | Handler | Descripción |
|--------|------|---------|-------------|
| `POST` | `/customers` | `CreateCustomer` | Crear cliente |
| `GET` | `/customers` | `ListCustomers` | Listar clientes paginados |

### Ventas

| Método | Ruta | Handler | Descripción |
|--------|------|---------|-------------|
| `POST` | `/sales` | `CreateSale` | Crear venta (deduce stock, registra eventos) |
| `GET` | `/sales` | `ListSales` | Listar ventas paginadas |
| `GET` | `/sales/{id}` | `GetSaleById` | Obtener venta por ID |

---

## Flujo de una venta

```
POST /sales
  │
  ├── Validaciones de entrada (items, cantidades, duplicados)
  ├── Por cada ítem:
  │   ├── Verifica producto activo
  │   └── Verifica stock disponible (lanza error si no hay)
  │
  ├── BEGIN TRANSACTION
  │   ├── INSERT INTO sales
  │   ├── INSERT INTO sale_items (x N)
  │   ├── UPDATE product_stock (x N)
  │   └── INSERT INTO pos_events (published = FALSE)
  │       ├── SaleCreatedEvent
  │       ├── StockLowEvent     (si stock ≤ threshold)
  │       └── StockDepletedEvent (si stock = 0)
  └── COMMIT
```

Si cualquier paso falla → `ROLLBACK` completo. Nunca queda una venta sin sus eventos ni un stock desactualizado.

---

## Eventos de dominio (Outbox)

Los eventos se guardan en `pos_events` con `published = FALSE`. Un proceso separado (Lambda scheduler) los lee y los envía a SQS/EventBridge, luego marca `published = TRUE`.

| Evento | Cuándo ocurre |
|--------|---------------|
| `sale.created` | Al completar una venta |
| `product.created` | Al crear un producto |
| `product.deactivated` | Al desactivar un producto |
| `stock.updated` | Al modificar stock |
| `stock.low` | Cuando el stock cae por debajo del umbral |
| `stock.depleted` | Cuando el stock llega a 0 |
| `customer.created` | Al crear un cliente |

---

## Variables de entorno

| Variable | Descripción | Ejemplo |
|----------|-------------|---------|
| `DB_CONNECTION` | Cadena de conexión PostgreSQL | `Host=...;Database=nexapos;Username=...;Password=...` |
| `ANNOTATIONS_HANDLER` | Handler activo — lo setea Lambda automáticamente | `Create`, `CreateSale`, `ListCustomers` |

---

## Estructura del proyecto

```
NexaFlow.NexaPOS/
├── Handlers/
│   ├── ProductHandler.cs          # POST /products, GET /products
│   ├── CustomerHandler.cs         # POST /customers, GET /customers
│   └── SaleHandler.cs             # POST /sales, GET /sales, GET /sales/{id}
├── migrations/
│   └── schema.sql                 # Schema completo de la BD
├── Properties/
│   └── launchSettings.json        # Perfiles de ejecución local
├── Functions.cs                   # Assembly attributes (GenerateMain, LambdaSerializer)
├── LambdaFunctionJsonSerializerContext.cs  # Tipos registrados para AOT
├── Startup.cs                     # Registro de servicios DI
├── aws-lambda-tools-defaults.json # Config para deploy manual local
└── serverless.template            # CloudFormation SAM template

NexaFlow.NexaPOS.Application/
├── Dto/                           # ProductDTO, SaleDTO, CustomerDTO, ApiResponse<T>
├── Interfaces/
│   ├── Events/                    # IEventRepository, IPosLogger
│   ├── Repositories/              # IProductRepository, ISaleRepository, ICustomerRepository, IStockRepository
│   ├── Services/                  # IProductService, ISaleService, ICustomerService
│   └── UnitOfWork/                # IUnitOfWork
├── Records/Create/                # CreateProductRequest, CreateSaleRequest, CreateCustomerRequest
└── Services/                      # ProductService, SaleService, CustomerService

NexaFlow.NexaPOS.Domain/
├── Entities/                      # Product, ProductStock, Sale, SaleItem, Customer
├── Events/                        # DomainEvents (SaleCreated, StockLow, etc.)
└── Exceptions/                    # DomainException

NexaFlow.NexaPOS.Infrastructure/
├── DBRepository/
│   ├── Events/                    # EventRepository
│   ├── ProductRepository.cs
│   ├── CustomerRepository.cs
│   ├── SaleRepository.cs
│   └── StockRepository.cs
├── Logging/                       # LambdaPosLogger (Console → CloudWatch)
└── UnitOfWork/                    # UnitOfWork (transacción atómica)
```

---

## Pruebas unitarias

El proyecto `NexaFlow.NexaPOS.Tests` contiene 76 tests organizados en dos capas.

### Stack de testing

- **xUnit** — framework de tests
- **Moq** — mocks para aislar dependencias en tests de aplicación
- Sin base de datos — los tests de aplicación usan mocks, los de dominio son puramente en memoria

### Estructura

```
NexaFlow.NexaPOS.Tests/
├── Helpers/
│   └── Build.cs                   # Builders reutilizables para construir entidades en tests
├── Domain/                        # Tests de reglas de negocio puras (sin mocks)
│   ├── ProductTests.cs            # Constructor, UpdatePrice, Deactivate
│   ├── ProductStockTests.cs       # Deduct, Add, IsLow, IsDepleted
│   ├── SaleTests.cs               # AddItem, ValidateForCheckout
│   └── CustomerTests.cs          # Validaciones de nombre, email, teléfono
└── Application/                   # Tests de servicios con mocks (Moq)
    ├── ProductServiceTests.cs     # Create, GetPaged, rollback en fallo de DB
    ├── SaleServiceTests.cs        # Flujo completo, eventos de stock, rollback
    └── CustomerServiceTests.cs    # Create, List, validaciones
```

### Correr los tests

```powershell
cd NexaFlow.NexaPOS.Tests
dotnet test
```

Con detalle de cada test:
```powershell
dotnet test -v normal
```

### Reporte de cobertura

Requiere `coverlet.collector` (ya incluido en el proyecto) y `reportgenerator`:

```powershell
# Instalar reportgenerator (una sola vez)
dotnet tool install -g dotnet-reportgenerator-globaltool
```

Generar y abrir el reporte en un solo comando:

```powershell
dotnet test --collect:"XPlat Code Coverage" && reportgenerator -reports:"TestResults\**\coverage.cobertura.xml" -targetdir:"TestResults\CoverageReport" -reporttypes:Html && start TestResults\CoverageReport\index.html
```

El reporte HTML queda en:
```
NexaFlow.NexaPOS.Tests\TestResults\CoverageReport\index.html
```

Resultado actual: **76 tests — 0 fallidos — duración ~1.9s**

### Qué se prueba

**Capa Domain** — reglas de negocio sin dependencias externas:
- Validaciones del constructor (nombre vacío, precio negativo, tenant vacío, email inválido)
- Comportamiento de `ProductStock.Deduct` con stock insuficiente
- Detección correcta de `IsLow` e `IsDepleted` según threshold
- Cálculo automático del total en `Sale.AddItem`
- Rechazo de productos inactivos en una venta

**Capa Application** — orquestación de servicios con mocks:
- Que `ProductService.CreateAsync` llama a `UnitOfWork` en orden correcto (Begin → Save → Commit)
- Que se hace `RollbackAsync` si la DB falla en cualquier punto
- Que `SaleService` encola `StockLowEvent` cuando el stock cae al umbral
- Que `SaleService` encola `StockDepletedEvent` cuando el stock llega a 0
- Que se rechaza una venta con productos duplicados, cantidades cero o stock insuficiente
- Que `CustomerService` publica `CustomerCreatedEvent` al crear un cliente
- Validaciones de paginación en todos los servicios

---

## Pruebas locales — Mock Lambda Test Tool

### Requisitos

- .NET 10 SDK
- PostgreSQL corriendo localmente con el schema de `migrations/schema.sql`
- Mock Lambda Test Tool:

```powershell
dotnet tool install -g Amazon.Lambda.TestTool-10.0
```

### Opción A — Script automático (recomendado)

Levanta el emulator y todos los handlers en paralelo con un solo comando:

```powershell
.\start-local.ps1
```

El script compila, arranca el API Gateway emulator en `http://localhost:5050` y los 7 handlers simultáneamente. Presiona `Ctrl+C` para detener todo.

Si PowerShell bloquea la ejecución de scripts:
```powershell
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
```

### Opción B — Manual (función individual)

**1. Compilar:**
```powershell
dotnet build
```

**2. Iniciar el test tool** (terminal 1):
```powershell
dotnet-lambda-test-tool-10.0
```
Abre `http://localhost:5050` → tab **"Executable Assembly"** → encola el evento → click **"Queue Event"**.

**3. Ejecutar** (terminal 2):
```powershell
$env:AWS_LAMBDA_RUNTIME_API="localhost:5050"
$env:DB_CONNECTION="Host=localhost;Port=5432;Database=nexapos_db;Username=...;Password=..."
$env:ANNOTATIONS_HANDLER="CreateSale"
.\bin\Debug\net10.0\NexaFlow.NexaPOS.exe
```

### Opción C — API Gateway emulator manual (todas las funciones a la vez)

**1. Iniciar el test tool con emulador** (terminal 1):
```powershell
dotnet-lambda-test-tool-10.0 --port 5050 --template serverless.template --api-gateway-emulator-mode Rest
```

**2. Un proceso por función** (una terminal por cada una):
```powershell
# Products
$env:AWS_LAMBDA_RUNTIME_API="localhost:5050"; $env:ANNOTATIONS_HANDLER="Create"; $env:DB_CONNECTION="..."; .\bin\Debug\net10.0\NexaFlow.NexaPOS.exe
$env:AWS_LAMBDA_RUNTIME_API="localhost:5050"; $env:ANNOTATIONS_HANDLER="List"; $env:DB_CONNECTION="..."; .\bin\Debug\net10.0\NexaFlow.NexaPOS.exe

# Customers
$env:AWS_LAMBDA_RUNTIME_API="localhost:5050"; $env:ANNOTATIONS_HANDLER="CreateCustomer"; $env:DB_CONNECTION="..."; .\bin\Debug\net10.0\NexaFlow.NexaPOS.exe
$env:AWS_LAMBDA_RUNTIME_API="localhost:5050"; $env:ANNOTATIONS_HANDLER="ListCustomers"; $env:DB_CONNECTION="..."; .\bin\Debug\net10.0\NexaFlow.NexaPOS.exe

# Sales
$env:AWS_LAMBDA_RUNTIME_API="localhost:5050"; $env:ANNOTATIONS_HANDLER="CreateSale"; $env:DB_CONNECTION="..."; .\bin\Debug\net10.0\NexaFlow.NexaPOS.exe
$env:AWS_LAMBDA_RUNTIME_API="localhost:5050"; $env:ANNOTATIONS_HANDLER="ListSales"; $env:DB_CONNECTION="..."; .\bin\Debug\net10.0\NexaFlow.NexaPOS.exe
$env:AWS_LAMBDA_RUNTIME_API="localhost:5050"; $env:ANNOTATIONS_HANDLER="GetSaleById"; $env:DB_CONNECTION="..."; .\bin\Debug\net10.0\NexaFlow.NexaPOS.exe
```

### Probar los endpoints

Una vez levantado el entorno con cualquiera de las opciones anteriores, usa el archivo `local.http` incluido en el proyecto.

**Con VS Code** — instala la extensión [REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client), abre `local.http` y haz click en **"Send Request"** sobre cada bloque.

**Con curl** — flujo completo de prueba:

```powershell
# 1. Crear un producto (guarda el id retornado)
curl -X POST http://localhost:5050/products `
  -H "x-tenant-id: 3fa85f64-5717-4562-b3fc-2c963f66afa6" `
  -H "Content-Type: application/json" `
  -d '{"name":"Coca Cola","price":1.50,"initialStock":100,"lowStockThreshold":10}'

# 2. Listar productos para verificar
curl http://localhost:5050/products `
  -H "x-tenant-id: 3fa85f64-5717-4562-b3fc-2c963f66afa6"

# 3. Crear un cliente (guarda el id retornado)
curl -X POST http://localhost:5050/customers `
  -H "x-tenant-id: 3fa85f64-5717-4562-b3fc-2c963f66afa6" `
  -H "Content-Type: application/json" `
  -d '{"name":"Juan Perez","phone":"3001234567","email":"juan@test.com"}'

# 4. Crear una venta (reemplaza <product-id> con el id del paso 1)
curl -X POST http://localhost:5050/sales `
  -H "x-tenant-id: 3fa85f64-5717-4562-b3fc-2c963f66afa6" `
  -H "Content-Type: application/json" `
  -d '{"items":[{"productId":"<product-id>","quantity":2}]}'

# 5. Listar ventas
curl "http://localhost:5050/sales?page=1&pageSize=10" `
  -H "x-tenant-id: 3fa85f64-5717-4562-b3fc-2c963f66afa6"

# 6. Obtener venta por id (reemplaza <sale-id> con el id del paso 4)
curl http://localhost:5050/sales/<sale-id> `
  -H "x-tenant-id: 3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

### Valores válidos para ANNOTATIONS_HANDLER

| Valor | Endpoint |
|-------|----------|
| `Create` | POST /products |
| `List` | GET /products |
| `CreateCustomer` | POST /customers |
| `ListCustomers` | GET /customers |
| `CreateSale` | POST /sales |
| `ListSales` | GET /sales |
| `GetSaleById` | GET /sales/{id} |

---

## CI/CD — Despliegue automático

El despliegue se realiza via GitHub Actions con OIDC (sin access keys almacenadas).

| Trigger | Entorno | Stack |
|---------|---------|-------|
| Push a `staging` | Staging | `NexaFlow-NexaPOS-staging` |
| Tag `nexapos/v*` | Production | `NexaFlow-NexaPOS-prod` |

Para la guía completa de configuración ver `.github/DEPLOYMENT-SETUP.md`.

### Deploy manual

```bash
sam build --template serverless.template --use-container
sam deploy --s3-bucket <tu-bucket> --guided
```

---

## Base de datos

El schema completo está en `migrations/schema.sql`. Incluye:

- `products` + `product_stock` — catálogo e inventario (relación 1 a 1)
- `sales` + `sale_items` — ventas e ítems
- `customers` — clientes por tenant
- `pos_events` — Outbox de eventos de negocio (`published = FALSE` → pendiente de enviar a SQS)
- RLS habilitado en todas las tablas con política `app.tenant_id`

---

## Native AOT

En `Release` el proyecto compila con Native AOT para reducir el cold start de Lambda. En `Debug` AOT está desactivado para permitir desarrollo local sin Docker.

La compilación AOT requiere Docker en Windows/Mac (target: Amazon Linux 2023). El workflow de GitHub Actions lo maneja automáticamente con `sam build --use-container`.
