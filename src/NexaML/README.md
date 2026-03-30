# NexaML

Microservicio Python de ML/LLM para la plataforma NexaFlow (Fase 3). Genera predicciones de ventas, detecta anomalías y produce insights en lenguaje natural usando AWS Bedrock.

---

## Por qué Python aquí y no antes

- **Prophet** (Meta) y **scikit-learn** son el estándar de facto para series temporales y ML en Python. No tienen equivalente maduro en .NET.
- **AWS Bedrock** tiene SDK nativo en Python (`boto3`). La integración es directa.
- Los servicios .NET (NexaPOS, NexaBook, NexaInsight) ya manejan la lógica transaccional y analítica básica. Este servicio solo agrega la capa de inteligencia.
- Separar ML en su propio servicio permite escalar, actualizar modelos y cambiar de LLM sin tocar el core.

---

## Responsabilidades

| Endpoint | Qué hace |
|----------|----------|
| `GET /ml/forecast` | Predice ventas de los próximos N días (Prophet) |
| `GET /ml/anomalies` | Detecta días con ventas anómalas (Z-score) |
| `GET /ml/insights` | Genera insight en lenguaje natural (AWS Bedrock / Claude) |
| `GET /health` | Health check |

---

## Arquitectura

```
app/
├── main.py                        → FastAPI app + Mangum handler (Lambda)
├── config.py                      → Settings desde variables de entorno
├── domain/
│   ├── entities.py                → SaleRecord, ForecastPoint, AnomalyPoint, InsightResult
│   └── interfaces.py              → ISalesRepository, IForecastService, IAnomalyService, ILLMService
├── services/
│   ├── ml/
│   │   ├── forecast.py            → ProphetForecastService (Facebook Prophet)
│   │   └── anomaly.py             → ZScoreAnomalyService (numpy Z-score)
│   └── llm/
│       └── bedrock.py             → BedrockLLMService (Claude via boto3)
├── infrastructure/
│   └── db/
│       └── sales_repository.py    → PostgresSalesRepository (asyncpg)
└── api/
    └── routes/
        ├── forecast.py            → GET /ml/forecast
        ├── anomalies.py           → GET /ml/anomalies
        └── insights.py            → GET /ml/insights
```

### Patrones

- **Clean Architecture** — dominio sin dependencias externas (solo dataclasses)
- **Dependency Injection** — FastAPI `Depends()` para repos y servicios
- **Interfaces abstractas** — `ISalesRepository`, `IForecastService`, etc. → fácil de mockear en tests
- **Mangum** — adapta FastAPI a AWS Lambda (API Gateway proxy)
- **RLS** — `SET app.tenant_id` antes de cada query (mismo patrón que .NET)

---

## Flujo de predicción

```
GET /ml/forecast
  │
  ├── Obtener últimos 90 días de ventas (PostgreSQL)
  ├── Entrenar modelo Prophet (weekly seasonality)
  ├── Predecir próximos 7 días con intervalo de confianza 80%
  └── Retornar [{date, predicted, lower, upper}]
```

## Flujo de insight LLM

```
GET /ml/insights
  │
  ├── Calcular ticket promedio (30 días)
  ├── Detectar anomalías (30 días, Z-score)
  ├── Predecir próximos 7 días (Prophet, 90 días histórico)
  ├── Obtener top producto (30 días)
  ├── Construir prompt con contexto numérico
  └── Invocar Claude via AWS Bedrock → texto en español
```

---

## Variables de entorno

| Variable | Descripción | Default |
|----------|-------------|---------|
| `DB_CONNECTION` | PostgreSQL asyncpg DSN | `postgresql+asyncpg://...` |
| `AWS_REGION` | Región AWS para Bedrock | `us-east-1` |
| `NEXAML_BEDROCK_MODEL_ID` | Modelo LLM en Bedrock | `google.gemma-3-4b-it` |
| `FORECAST_HORIZON_DAYS` | Días a predecir | `7` |
| `ANOMALY_ZSCORE_THRESHOLD` | Umbral Z-score para anomalías | `2.5` |

---

## Desarrollo local

```bash
cd src/NexaML

# Crear entorno virtual
python -m venv .venv
source .venv/bin/activate

# Instalar dependencias
pip install -r requirements.txt -r requirements-dev.txt

# Variables de entorno
cp .env.example .env   # editar con tus credenciales

# Levantar servidor local
uvicorn app.main:app --reload --port 8001
```

Endpoints disponibles en `http://localhost:8001/docs` (Swagger UI automático).

---

## Tests

```bash
# Solo tests unitarios (sin DB ni Bedrock — usan mocks)
pytest tests/unit/ -v

# Con cobertura
pytest tests/unit/ --cov=app --cov-report=html
```

Los tests unitarios no requieren PostgreSQL ni AWS. Todo se mockea con `pytest-mock`.

---

## Deploy

### Prerrequisitos

1. Crear repositorio ECR:
```bash
aws ecr create-repository --repository-name nexaml --region us-east-1
```

2. El IAM Role OIDC (ya creado para NexaPOS) necesita permisos adicionales:
```json
{
  "Effect": "Allow",
  "Action": ["ecr:GetAuthorizationToken", "ecr:BatchCheckLayerAvailability",
             "ecr:PutImage", "ecr:InitiateLayerUpload", "ecr:UploadLayerPart",
             "ecr:CompleteLayerUpload"],
  "Resource": "*"
}
```

### Manual

```bash
# Build imagen
docker build -t nexaml .

# Push a ECR
aws ecr get-login-password | docker login --username AWS --password-stdin <account>.dkr.ecr.us-east-1.amazonaws.com
docker tag nexaml:latest <account>.dkr.ecr.us-east-1.amazonaws.com/nexaml:latest
docker push <account>.dkr.ecr.us-east-1.amazonaws.com/nexaml:latest

# Deploy SAM
sam build && sam deploy --guided
```

### CI/CD automático

| Trigger | Entorno | Stack |
|---------|---------|-------|
| Push a `staging` | Staging | `NexaFlow-NexaML-staging` |
| Tag `nexaml/v*` | Production | `NexaFlow-NexaML-prod` |

El workflow:
1. Corre tests unitarios
2. Build imagen Docker y push a ECR
3. SAM deploy con la nueva imagen

---

## Secrets requeridos en GitHub

| Secret | Descripción |
|--------|-------------|
| `AWS_DEPLOY_ROLE_ARN` | ARN del role OIDC (mismo que otros servicios) |
| `NEXAFLOW_S3_BUCKET` | Bucket S3 para artefactos SAM |
| `NEXAML_DB_CONNECTION` | Connection string PostgreSQL |
| `NEXAML_BEDROCK_MODEL_ID` | Opcional — override del modelo Bedrock |

---

## Evolución prevista (Fase 3+)

- **Neo4j AuraDB** — grafo de relaciones producto-producto para recomendaciones
- **Reentrenamiento automático** — Lambda scheduler que reentrena Prophet semanalmente
- **Alertas** — SNS/SES cuando se detecta anomalía crítica
- **Caché de predicciones** — ElastiCache para no recalcular en cada request
