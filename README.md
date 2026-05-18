# NexaFlow

Plataforma SaaS multi-tenant para gestión de restaurantes y negocios de hospitalidad.

## Microservicios

| Servicio | Tecnología | Responsabilidad | Puerto local |
|---|---|---|---|
| **NexaAuth** | .NET 10 | Autenticación, tenants, suscripciones y billing | 8081 |
| **NexaPOS** | .NET 10 | Punto de venta: productos, ventas, stock | 8082 |
| **NexaBook** | .NET 10 | Reservas y gestión de clientes | 8083 |
| **NexaInsight** | .NET 10 | Reportes y analítica de negocio | 8084 |
| **NexaML** | Python / FastAPI | Predicciones y detección de anomalías con ML | 8085 |
| **NexaWeb** | React / Vite | Frontend SPA | 80 |

---

## Requisitos previos

| Herramienta | Versión mínima | Para qué |
|---|---|---|
| Docker Desktop | 4.x | Construir imágenes |
| Kubernetes (Docker Desktop) | 1.28+ | Despliegue K8s local |
| kubectl | 1.28+ | Gestionar el cluster |
| Python | 3.10+ | Generar dataset Spark |
| PySpark | 3.5+ | Ejecutar análisis Spark |

---

## Opción A — Kubernetes local (recomendado para la demo)

### 1. Habilitar Kubernetes en Docker Desktop

Docker Desktop → Settings → Kubernetes → **Enable Kubernetes** → Apply & Restart.

Verificar:
```powershell
kubectl get nodes
# NAME             STATUS   ROLES           AGE
# docker-desktop   Ready    control-plane   ...
```

### 2. Construir las imágenes Docker

Ejecutar desde la raíz del repositorio:

```powershell
# Microservicios .NET (contexto: src/NexaFlow/)
docker build -f src/NexaFlow/NexaFlow.NexaAuth_Billing.API/Dockerfile -t nexaflow/nexaauth:latest src/NexaFlow
docker build -f src/NexaFlow/NexaFlow.NexaPOS.API/Dockerfile           -t nexaflow/nexapos:latest   src/NexaFlow
docker build -f src/NexaFlow/NexaFlow.NexaBook.API/Dockerfile           -t nexaflow/nexabook:latest  src/NexaFlow
docker build -f src/NexaFlow/NexaFlow.NexaInsight.API/Dockerfile        -t nexaflow/nexainsight:latest src/NexaFlow

# Microservicio Python
docker build -f src/NexaML/Dockerfile.k8s -t nexaflow/nexaml:latest src/NexaML

# Frontend React
docker build `
  -f src/NexaFlow/NexaFlow-web/Dockerfile `
  --build-arg VITE_AUTH_API_URL=http://nexaflow.local/auth `
  --build-arg VITE_POS_API_URL=http://nexaflow.local/pos `
  --build-arg VITE_BOOK_API_URL=http://nexaflow.local/book `
  --build-arg VITE_INSIGHT_API_URL=http://nexaflow.local/insight `
  --build-arg VITE_ML_API_URL=http://nexaflow.local/ml `
  -t nexaflow/nexaweb:latest `
  src/NexaFlow/NexaFlow-web
```

Verificar que las 6 imágenes existen:
```powershell
docker images | findstr nexaflow
```

### 3. Desplegar en Kubernetes

```powershell
# Namespace, configuración y secretos
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secret.yaml

# Base de datos — esperar que esté lista antes de continuar
kubectl apply -f k8s/postgres.yaml
kubectl wait --for=condition=ready pod -l app=nexaflow-postgres -n nexaflow --timeout=120s

# Inicializar esquema (solo la primera vez)
kubectl cp NexosNexaFlow-db-structure.sql nexaflow/$(kubectl get pod -n nexaflow -l app=nexaflow-postgres -o jsonpath='{.items[0].metadata.name}'):/tmp/init.sql
kubectl exec -n nexaflow deploy/nexaflow-postgres -- psql -U post_usr -d NexosNexaFlow -f /tmp/init.sql

# Microservicios
kubectl apply -f k8s/nexaauth-deployment.yaml
kubectl apply -f k8s/nexapos-deployment.yaml
kubectl apply -f k8s/nexabook-deployment.yaml
kubectl apply -f k8s/nexainsight-deployment.yaml
kubectl apply -f k8s/nexaml-deployment.yaml
kubectl apply -f k8s/nexaweb-deployment.yaml

# Escalabilidad automática
kubectl apply -f k8s/hpa.yaml

# Ingress
kubectl apply -f k8s/ingress.yaml

# Monitoreo
kubectl apply -f k8s/monitoring/prometheus-config.yaml
kubectl apply -f k8s/monitoring/prometheus.yaml
kubectl apply -f k8s/monitoring/grafana.yaml
```

### 4. Agregar entrada en hosts (una sola vez)

Editar `C:\Windows\System32\drivers\etc\hosts` como administrador y agregar:
```
127.0.0.1  nexaflow.local
```

### 5. Instalar nginx Ingress Controller (si no está instalado)

```powershell
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.10.0/deploy/static/provider/cloud/deploy.yaml
kubectl wait --namespace ingress-nginx --for=condition=ready pod --selector=app.kubernetes.io/component=controller --timeout=120s
```

### 6. Verificar el despliegue

```powershell
# Estado de todos los pods
kubectl get pods -n nexaflow

# Todos deben mostrar Running y READY 1/1 (o 2/2 para los que tienen 2 réplicas)
```

### 7. Acceder a los servicios

Con Ingress activo:

| Servicio | URL |
|---|---|
| **Frontend** | http://nexaflow.local |
| NexaAuth Swagger | http://nexaflow.local/auth/swagger |
| NexaPOS Swagger | http://nexaflow.local/pos/swagger |
| NexaBook Swagger | http://nexaflow.local/book/swagger |
| NexaInsight Swagger | http://nexaflow.local/insight/swagger |
| NexaML Docs | http://nexaflow.local/ml/docs |
| **Prometheus** | http://localhost:30090 |
| **Grafana** | http://localhost:30030 — usuario: `admin` / contraseña: `nexaflow123` |

Sin Ingress (port-forward directo):
```powershell
kubectl port-forward -n nexaflow svc/nexaweb-svc      3000:80  &
kubectl port-forward -n nexaflow svc/nexaauth-svc     8081:80  &
kubectl port-forward -n nexaflow svc/nexapos-svc      8082:80  &
kubectl port-forward -n nexaflow svc/nexabook-svc     8083:80  &
kubectl port-forward -n nexaflow svc/nexainsight-svc  8084:80  &
kubectl port-forward -n nexaflow svc/nexaml-svc       8085:80  &
```

---

## Opción B — Docker Compose local (desarrollo rápido)

### 1. Crear archivo `.env` en la raíz del repositorio

```env
IMAGE_TAG=latest
IMAGE_PREFIX=nexaflow
DB_CONNECTION=Host=postgres;Database=NexosNexaFlow;Username=post_usr;Password=P3assW0e
JWT_SECRET=nexaflow-dev-secret-min32chars!!
JWT_ISSUER=nexaflow
POSTGRES_PASSWORD=P3assW0e
```

### 2. Construir y levantar

```powershell
docker compose -f docker-compose.lightsail.yml up -d --build
```

### 3. Inicializar esquema (solo la primera vez)

```powershell
docker compose -f docker-compose.lightsail.yml exec postgres `
  psql -U post_usr -d NexosNexaFlow -f /tmp/init.sql
```

O copiar primero el archivo:
```powershell
docker cp NexosNexaFlow-db-structure.sql $(docker compose -f docker-compose.lightsail.yml ps -q postgres):/tmp/init.sql
docker compose -f docker-compose.lightsail.yml exec postgres psql -U post_usr -d NexosNexaFlow -f /tmp/init.sql
```

### 4. Acceder

| Servicio | URL |
|---|---|
| Frontend | http://localhost |
| NexaAuth | http://localhost:8081/swagger |
| NexaPOS | http://localhost:8082/swagger |
| NexaBook | http://localhost:8083/swagger |
| NexaInsight | http://localhost:8084/swagger |
| NexaML | http://localhost:8085/docs |

---

## Comandos útiles de Kubernetes

```powershell
# Ver todos los recursos del namespace
kubectl get all -n nexaflow

# Logs de un servicio
kubectl logs -n nexaflow deploy/nexapos -f

# Escalar manualmente
kubectl scale deployment nexapos -n nexaflow --replicas=3

# Ver HPA en acción
kubectl get hpa -n nexaflow

# Reiniciar un deployment (rolling restart)
kubectl rollout restart deployment/nexapos -n nexaflow

# Eliminar todo y empezar de cero
kubectl delete namespace nexaflow
```

---

## Análisis con Apache Spark

### 1. Generar el dataset

```powershell
cd k8s/spark
python generate_dataset.py
# Genera: sales.csv (2000 registros) y reservations.csv (1000 registros)
```

### 2. Ejecutar el análisis

Abrir el notebook en VS Code o Jupyter:

```powershell
cd k8s/spark
python -m jupyter notebook nexaflow_spark_analysis.ipynb
```

O abrir `k8s/spark/nexaflow_spark_analysis.ipynb` directamente en VS Code y ejecutar **Run All**.
La celda 0 instala `pyspark` automáticamente si no está disponible.

### Métricas generadas

| # | Métrica | Valor para el negocio |
|---|---|---|
| 1 | Revenue y ventas por tenant | Identifica los tenants más rentables |
| 2 | Top productos por cantidad vendida | Informa decisiones de inventario |
| 3 | Tasa de cancelación de reservas por tenant | Detecta problemas operativos |
| 4 | Reservas por franja horaria | Optimiza staffing y capacidad |
| 5 | Ventas completadas por mes | Tendencia temporal del negocio |

---

## Monitoreo con Prometheus y Grafana

Prometheus recolecta métricas automáticamente de todos los pods con las anotaciones:
```yaml
prometheus.io/scrape: "true"
prometheus.io/port: "8080"
prometheus.io/path: "/metrics"
```

Métricas disponibles:
- `http_requests_total` — total de requests por endpoint y código HTTP
- `http_request_duration_seconds` — latencia de requests
- `dotnet_gc_collections_total` — garbage collection de .NET

En Grafana (http://localhost:30030) el datasource de Prometheus ya está preconfigurado.
Dashboards recomendados para importar desde grafana.com:
- **ID 10427** — ASP.NET Core
- **ID 1860** — Node Exporter Full

---

## Alta disponibilidad y tolerancia a fallos

| Mecanismo | Descripción |
|---|---|
| Réplicas múltiples | NexaAuth y NexaPOS corren con 2 réplicas mínimas |
| HPA | Escala automáticamente hasta 5 réplicas cuando CPU > 70% |
| Liveness probe | Kubernetes reinicia pods que no responden en `/health` |
| Readiness probe | El tráfico solo llega a pods listos para servir |
| Rolling updates | Actualizaciones sin downtime (estrategia por defecto) |
| PVC | Los datos de PostgreSQL persisten ante reinicios del pod |

---

## Despliegue automático (CI/CD)

Ver [`.github/CICD-SETUP.md`](.github/CICD-SETUP.md) para instrucciones completas de cada modalidad:

| Workflow | Destino | Trigger |
|---|---|---|
| `deploy-nexaauth/pos/book/insight/ml.yml` | AWS Lambda (SAM) | push a `staging` / tag por servicio |
| `deploy-lightsail.yml` | Lightsail via SSH | push a `staging` / tag `lightsail/v*` |
| `deploy-k8s-local.yml` | Kubernetes local | `workflow_dispatch` manual |

---

## Estructura del repositorio

```
NexaFlow/
├── src/
│   ├── NexaFlow/
│   │   ├── NexaFlow-web/              # Frontend React/Vite
│   │   │   ├── Dockerfile
│   │   │   └── nginx.conf
│   │   ├── NexaFlow.NexaAuth_Billing.API/
│   │   ├── NexaFlow.NexaPOS.API/
│   │   ├── NexaFlow.NexaBook.API/
│   │   └── NexaFlow.NexaInsight.API/
│   └── NexaML/
│       ├── Dockerfile          # Para AWS Lambda
│       └── Dockerfile.k8s      # Para Kubernetes / Lightsail
├── k8s/
│   ├── namespace.yaml
│   ├── configmap.yaml
│   ├── secret.yaml
│   ├── postgres.yaml
│   ├── nexaauth-deployment.yaml
│   ├── nexapos-deployment.yaml
│   ├── nexabook-deployment.yaml
│   ├── nexainsight-deployment.yaml
│   ├── nexaml-deployment.yaml
│   ├── nexaweb-deployment.yaml
│   ├── hpa.yaml
│   ├── ingress.yaml
│   ├── monitoring/
│   │   ├── prometheus-config.yaml
│   │   ├── prometheus.yaml
│   │   └── grafana.yaml
│   └── spark/
│       ├── generate_dataset.py
│       ├── sales.csv
│       ├── reservations.csv
│       └── nexaflow_spark_analysis.ipynb
├── docker-compose.lightsail.yml
├── NexosNexaFlow-db-structure.sql
└── README.md
```
