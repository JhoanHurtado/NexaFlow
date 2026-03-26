# Guía de Configuración — CI/CD NexaFlow NexaPOS

Todo lo que debes crear y configurar en AWS y GitHub para que el despliegue automático funcione.

---

## Cuándo se despliega

| Trigger | Entorno | Stack CloudFormation | Requiere aprobación |
|---------|---------|----------------------|---------------------|
| Push a rama `staging` | Staging | `NexaFlow-NexaPOS-staging` | No |
| Tag `nexapos/v*` | Production | `NexaFlow-NexaPOS-prod` | Sí |
| `workflow_dispatch` manual | El que elijas | Según selección | Según entorno |

---

## Parte 1 — Configuración en AWS

### 1.1 Crear el S3 bucket para artefactos SAM

El bucket almacena el ZIP compilado antes de desplegarlo. Se crea **una sola vez**.

```bash
aws s3 mb s3://nexaflow-sam-artifacts --region us-east-1

# Habilitar versionado (permite rollback)
aws s3api put-bucket-versioning \
  --bucket nexaflow-sam-artifacts \
  --versioning-configuration Status=Enabled

# Bloquear acceso público
aws s3api put-public-access-block \
  --bucket nexaflow-sam-artifacts \
  --public-access-block-configuration \
    BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true
```

> El nombre del bucket debe ser único globalmente. Si ya existe, usa `nexaflow-sam-artifacts-<tu-account-id>`.

---

### 1.2 Crear el IAM Role OIDC para GitHub Actions

Permite que GitHub Actions asuma un rol en AWS **sin guardar access keys**.
Se crea **una sola vez** con el template `.github/oidc-role.yml`.

```bash
aws cloudformation deploy \
  --template-file .github/oidc-role.yml \
  --stack-name NexaFlow-GitHubActions-OIDC \
  --capabilities CAPABILITY_NAMED_IAM \
  --parameter-overrides \
    GitHubOrg=TU_USUARIO_O_ORG \
    GitHubRepo=NexaFlow \
    S3BucketName=nexaflow-sam-artifacts
```

Obtén el ARN del role (lo necesitas en el paso 2.2):

```bash
aws cloudformation describe-stacks \
  --stack-name NexaFlow-GitHubActions-OIDC \
  --query "Stacks[0].Outputs[?OutputKey=='RoleArn'].OutputValue" \
  --output text
```

---

## Parte 2 — Configuración en GitHub

### 2.1 Crear los entornos

Ve a **Settings → Environments → New environment** y crea dos:

**`staging`**
- Sin restricciones adicionales

**`production`**
- Activar **"Required reviewers"** → agrega tu usuario o equipo
- Esto hace que cada deploy a producción requiera aprobación manual antes de ejecutarse

---

### 2.2 Agregar los Secrets

#### Secrets por entorno

Los secrets de entorno se configuran en **Settings → Environments → [nombre] → Add secret**.
Esto permite que staging y producción tengan distintas credenciales de BD.

**Entorno `staging`:**

| Secret | Descripción | Ejemplo |
|--------|-------------|---------|
| `AWS_DEPLOY_ROLE_ARN` | ARN del role del paso 1.2 | `arn:aws:iam::123456789012:role/NexaFlow-GitHubActions-NexaPOS` |
| `NEXAPOS_DB_CONNECTION` | Connection string BD staging | `Host=staging.rds.amazonaws.com;Database=nexapos_staging;Username=...;Password=...` |

**Entorno `production`:**

| Secret | Descripción |
|--------|-------------|
| `AWS_DEPLOY_ROLE_ARN` | El mismo ARN (o uno diferente si usas cuentas AWS separadas) |
| `NEXAPOS_DB_CONNECTION` | Connection string BD producción |

#### Secret de repositorio (compartido)

Ve a **Settings → Secrets and variables → Actions → New repository secret**:

| Secret | Valor |
|--------|-------|
| `NEXAPOS_S3_BUCKET` | `nexaflow-sam-artifacts` |

---

### 2.3 Crear la rama `staging`

```bash
git checkout -b staging
git push origin staging
```

Cualquier push a esta rama con cambios en los proyectos NexaPOS dispara el deploy a staging automáticamente.

---

## Parte 3 — Cómo crear un Release para desplegar a producción

Un release en este proyecto se hace mediante un **Git tag** con el formato `nexapos/v<semver>`.
El workflow detecta el tag y despliega al stack de producción.

### Convención de versiones

Usa [Semantic Versioning](https://semver.org/):

| Tipo | Cuándo usarlo | Ejemplo |
|------|---------------|---------|
| `v1.0.0` | Primera versión estable | `nexapos/v1.0.0` |
| `v1.1.0` | Nueva funcionalidad sin breaking changes | `nexapos/v1.1.0` |
| `v1.1.1` | Bugfix | `nexapos/v1.1.1` |
| `v2.0.0` | Breaking change | `nexapos/v2.0.0` |

---

### Paso a paso para crear un release

**1. Asegúrate de estar en `main` y tener todo actualizado:**
```bash
git checkout main
git pull origin main
```

**2. Verifica que staging fue probado y aprobado:**
```bash
# Opcional: merge de staging a main si los cambios vienen de ahí
git merge staging
git push origin main
```

**3. Crea el tag localmente:**
```bash
git tag -a nexapos/v1.0.0 -m "Release NexaPOS v1.0.0 - POS inicial con productos, ventas y clientes"
```

> Usa `-a` para un tag anotado (incluye mensaje, autor y fecha). Es mejor práctica que un tag ligero.

**4. Sube el tag a GitHub:**
```bash
git push origin nexapos/v1.0.0
```

**5. El workflow se dispara automáticamente.**
Ve a **Actions → Deploy NexaPOS** para ver el progreso.

**6. Aprueba el deploy a producción:**
GitHub pausará el workflow y enviará una notificación al reviewer configurado en el entorno `production`.
Ve a **Actions → [el run activo] → Review deployments → Approve**.

---

### Crear el Release en la UI de GitHub (opcional pero recomendado)

Además del tag, puedes crear un Release formal en GitHub para documentar los cambios:

1. Ve a **Releases → Draft a new release**
2. En "Choose a tag" selecciona el tag que acabas de crear (`nexapos/v1.0.0`)
3. Título: `NexaPOS v1.0.0`
4. Descripción: lista los cambios incluidos en esta versión
5. Click **"Publish release"**

Esto no dispara el workflow nuevamente (el tag ya lo hizo), pero deja un historial visible en GitHub.

---

### Cómo hacer rollback

Si el deploy a producción falla o hay un problema, puedes volver a la versión anterior redespliegando el tag anterior:

```bash
# Opción 1: redesplegar el tag anterior manualmente desde GitHub Actions
# Ve a Actions → Deploy NexaPOS → Run workflow → selecciona el tag anterior

# Opción 2: desde tu máquina
git checkout nexapos/v0.9.0
sam build --template serverless.template --use-container
sam deploy \
  --stack-name NexaFlow-NexaPOS-prod \
  --s3-bucket nexaflow-sam-artifacts \
  --parameter-overrides DbConnection="..."
```

---

## Parte 4 — Sobre `aws-lambda-tools-defaults.json`

Este archivo es **solo para uso local** (Visual Studio / CLI manual). El workflow de GitHub Actions no lo usa.

```json
{
  "profile": "default",
  "region": "us-east-1",
  "configuration": "Release",
  "template": "serverless.template",
  "stack-name": "NexaFlow-NexaPOS-staging",
  "msbuild-parameters": "--self-contained true"
}
```

`s3-bucket` **no está en este archivo** porque:
- En el workflow viene del secret `NEXAPOS_S3_BUCKET`
- Para deploy manual, SAM lo pide con `--guided` o se pasa como argumento
- No conviene hardcodear nombres de buckets en el repositorio

**Deploy manual desde tu máquina:**
```bash
sam build --template serverless.template --use-container
sam deploy --s3-bucket nexaflow-sam-artifacts --guided
```

---

## Resumen visual del flujo

```
Developer
    │
    ├── push → staging ─────────────────────────────────────────────────────────┐
    │                                                                            │
    └── git tag nexapos/v1.0.0 + push ──────────────────────────────────────────┤
                                                                                 │
                                                                                 ▼
                                                                        GitHub Actions
                                                                             │
                                                                             ├── OIDC → AssumeRole AWS (sin keys)
                                                                             ├── sam build (Docker / Amazon Linux 2023)
                                                                             ├── sam deploy → S3 + CloudFormation
                                                                             │
                                                                   staging ──┴── Stack: NexaFlow-NexaPOS-staging (auto)
                                                                             │
                                                                 production ─┴── Aprobación requerida
                                                                                 Stack: NexaFlow-NexaPOS-prod
```

---

## Costos estimados

| Servicio | Costo |
|----------|-------|
| Lambda | Free tier: 1M invocaciones + 400K GB-s/mes |
| API Gateway | Free tier: 1M llamadas/mes (primeros 12 meses) |
| S3 artefactos | ~$0.001/mes (archivos de ~5MB) |
| CloudFormation | Sin costo |
| OIDC / IAM | Sin costo |
| GitHub Actions | Free: 2,000 min/mes repos públicos · 500 min repos privados |

**El despliegue en sí no genera costo adicional.**
