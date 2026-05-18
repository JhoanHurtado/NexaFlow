# CI/CD Setup — NexaFlow

Despliegue automático via GitHub Actions. Tres modalidades disponibles:

| Workflow | Destino | Trigger |
|---|---|---|
| `deploy-nexaauth/pos/book/insight/ml.yml` | AWS Lambda (SAM) | push a `staging` / tag por servicio |
| `deploy-lightsail.yml` | Lightsail (Docker Compose + SSH) | push a `staging` / tag `lightsail/v*` |
| `deploy-k8s-local.yml` | Kubernetes local | `workflow_dispatch` manual |

---

## 1. AWS Lambda (SAM) — configuración existente

### Prerequisitos en AWS (una sola vez)

```bash
aws s3 mb s3://nexaflow-sam-artifacts --region us-east-1
aws s3api put-bucket-versioning \
  --bucket nexaflow-sam-artifacts \
  --versioning-configuration Status=Enabled
```

```bash
aws cloudformation deploy \
  --template-file .github/oidc-role.yml \
  --stack-name NexaFlow-GitHubActions-OIDC \
  --capabilities CAPABILITY_NAMED_IAM \
  --parameter-overrides \
    GitHubOrg=TU_ORG_O_USUARIO \
    GitHubRepo=NexaFlow \
    S3BucketName=nexaflow-sam-artifacts
```

### Secrets requeridos

| Secret | Valor |
|--------|-------|
| `AWS_DEPLOY_ROLE_ARN` | ARN del role OIDC |
| `NEXAPOS_S3_BUCKET` | `nexaflow-sam-artifacts` |
| `NEXAPOS_DB_CONNECTION` | Connection string de BD |
| `NEXAAUTH_DB_CONNECTION` | Connection string de BD |
| `NEXAAUTH_JWT_SECRET` | Secret JWT (mín. 32 chars) |
| `NEXABOOK_DB_CONNECTION` | Connection string de BD |
| `NEXAINSIGHT_DB_CONNECTION` | Connection string de BD |
| `NEXAML_DB_CONNECTION` | Connection string de BD |

### Deploy manual

```bash
cd src/NexaFlow/NexaFlow.NexaPOS
sam build --template serverless.template --use-container
sam deploy \
  --stack-name NexaFlow-NexaPOS \
  --s3-bucket nexaflow-sam-artifacts \
  --s3-prefix NexaFlow.NexaPOS \
  --region us-east-1 \
  --capabilities CAPABILITY_IAM \
  --parameter-overrides DbConnection="Host=...;..."
```

---

## 2. Lightsail (SSH + Docker Compose)

### Prerequisitos en el servidor Lightsail (una sola vez)

```bash
# Instalar Docker
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER

# Crear directorio de trabajo
mkdir -p ~/nexaflow
```

Abrir puertos en Lightsail → Networking → IPv4 Firewall: `8081–8085`.

### Secrets requeridos

| Secret | Valor |
|--------|-------|
| `DOCKERHUB_USERNAME` | Usuario de Docker Hub |
| `DOCKERHUB_TOKEN` | Access token de Docker Hub (no la contraseña) |
| `LIGHTSAIL_HOST` | IP pública de la instancia |
| `LIGHTSAIL_USER` | Usuario SSH (`ubuntu` o `bitnami`) |
| `LIGHTSAIL_SSH_KEY` | Contenido completo de la clave privada `.pem` |
| `LIGHTSAIL_DB_CONNECTION` | `Host=postgres;Database=NexosNexaFlow;Username=post_usr;Password=...` |
| `LIGHTSAIL_POSTGRES_PASSWORD` | Contraseña de PostgreSQL |
| `NEXAAUTH_JWT_SECRET` | Secret JWT (mín. 32 chars) |

### Flujo

```
push a staging (o tag lightsail/v*)
  │
  ├── test        → tests .NET × 3 + pytest
  ├── build-push  → docker build × 5 → push a Docker Hub (con caché GHA)
  └── deploy
        ├── scp docker-compose.lightsail.yml al servidor
        ├── escribe .env con secrets
        ├── docker compose pull + up -d --remove-orphans
        ├── docker image prune -f
        └── health check HTTP en puertos 8081–8085
```

### Puertos expuestos

| Servicio    | Puerto |
|-------------|--------|
| NexaAuth    | 8081   |
| NexaPOS     | 8082   |
| NexaBook    | 8083   |
| NexaInsight | 8084   |
| NexaML      | 8085   |

---

## 3. Kubernetes Local (self-hosted runner)

### Prerequisitos en la máquina local

- Docker Desktop con Kubernetes habilitado **o** Minikube corriendo
- `kubectl` configurado apuntando al cluster local
- GitHub Actions self-hosted runner registrado con etiquetas `local,kubernetes`

### Registrar el runner

```
GitHub repo → Settings → Actions → Runners → New self-hosted runner
```

Seguir las instrucciones del asistente. Al llegar al paso de configuración agregar las etiquetas:

```
local,kubernetes
```

### Disparar el workflow

Solo se dispara manualmente:

```
GitHub repo → Actions → Deploy to Kubernetes (Local) → Run workflow
```

La opción `reset=true` elimina el namespace completo y redesplega desde cero.

### Flujo

```
workflow_dispatch
  │
  ├── build-images
  │     └── docker build × 5 (imágenes locales, sin push a registry)
  │
  └── deploy-k8s
        ├── kubectl apply namespace / configmap / secret
        ├── kubectl apply postgres → wait ready (90s)
        ├── inicializar esquema SQL (idempotente)
        ├── kubectl apply × 5 microservicios
        ├── kubectl apply hpa / ingress / monitoring
        ├── kubectl rollout status × 5 (wait 120s c/u)
        ├── resumen: pods / services / ingress / hpa
        └── health check HTTP interno por cada servicio
```
