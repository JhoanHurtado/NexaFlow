# CI/CD Setup — NexaFlow NexaPOS

Despliegue automático a AWS Lambda via GitHub Actions usando OIDC (sin access keys almacenadas).

---

## Prerequisitos en AWS (una sola vez)

### 1. Crear el S3 bucket para artefactos SAM

```bash
aws s3 mb s3://nexaflow-sam-artifacts --region us-east-1
aws s3api put-bucket-versioning \
  --bucket nexaflow-sam-artifacts \
  --versioning-configuration Status=Enabled
```

### 2. Crear el IAM Role OIDC para GitHub Actions

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

Obtén el ARN del role creado:

```bash
aws cloudformation describe-stacks \
  --stack-name NexaFlow-GitHubActions-OIDC \
  --query "Stacks[0].Outputs[?OutputKey=='RoleArn'].OutputValue" \
  --output text
```

---

## Secrets en GitHub

Ve a **Settings → Secrets and variables → Actions** y agrega:

| Secret | Valor |
|--------|-------|
| `AWS_DEPLOY_ROLE_ARN` | ARN del role del paso anterior |
| `NEXAPOS_S3_BUCKET` | `nexaflow-sam-artifacts` |
| `NEXAPOS_DB_CONNECTION` | `Host=...;Database=...;Username=...;Password=...` |

---

## Flujo del workflow

```
push a main (cambios en NexaPOS)
  │
  ├── Configure AWS credentials (OIDC — sin access keys)
  ├── Setup .NET 10 + Amazon.Lambda.Tools + SAM CLI
  ├── sam build --use-container   ← compila Native AOT en Linux/Docker
  └── sam deploy                  ← despliega el stack CloudFormation
```

El workflow solo se dispara cuando hay cambios en los proyectos de NexaPOS o en el propio workflow.

---

## Costos

- **Lambda**: free tier 1M invocaciones/mes + 400,000 GB-segundos
- **API Gateway**: free tier 1M llamadas/mes (primeros 12 meses)
- **S3**: ~$0.023/GB almacenado — los artefactos SAM son <10MB
- **CloudFormation**: sin costo
- **OIDC / IAM**: sin costo

El despliegue en sí **no genera costo adicional**.

---

## Deploy manual (sin GitHub Actions)

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
