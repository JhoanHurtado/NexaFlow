#!/usr/bin/env bash
set -euo pipefail

# ─── Uso ──────────────────────────────────────────────────────────────────────
# ./release.sh 1.0.2
# ./release.sh 1.0.2 nexaauth nexapos        ← solo servicios específicos
# ─────────────────────────────────────────────────────────────────────────────

VERSION="${1:-}"
if [[ -z "$VERSION" ]]; then
  echo "Uso: $0 <version> [servicio...]"
  echo "Ejemplo: $0 1.0.2"
  echo "Ejemplo: $0 1.0.2 nexaauth nexapos"
  exit 1
fi

declare -A MESSAGES=(
  [nexaauth]="NexaAuth & Billing v${VERSION} — Microservicio de identidad, RBAC y facturación."
  [nexabook]="NexaBook v${VERSION} — Microservicio de reservas y disponibilidad de slots."
  [nexapos]="NexaPOS v${VERSION} — Microservicio de punto de venta y gestión de productos."
  [nexainsight]="NexaInsight v${VERSION} — Microservicio de analítica e indicadores de negocio."
  [nexaml]="NexaML v${VERSION} — Microservicio de IA con predicción y Amazon Bedrock."
  [nexaflow-web]="NexaFlow Web v${VERSION} — SPA React/Vite desplegada en S3."
)

ALL_SERVICES=(nexaauth nexabook nexapos nexainsight nexaml nexaflow-web)

# Si se pasaron servicios específicos, usarlos; si no, todos
if [[ $# -gt 1 ]]; then
  SERVICES=("${@:2}")
else
  SERVICES=("${ALL_SERVICES[@]}")
fi

# Validar que los servicios existen
for svc in "${SERVICES[@]}"; do
  if [[ -z "${MESSAGES[$svc]+_}" ]]; then
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
    echo "Error: el tag '$TAG' ya existe. Elimínalo primero con: git tag -d $TAG && git push origin :refs/tags/$TAG"
    exit 1
  fi
done

# Crear los tags
echo ""
echo "Creando tags para v${VERSION}..."
for svc in "${SERVICES[@]}"; do
  TAG="${svc}/v${VERSION}"
  git tag -a "$TAG" -m "${MESSAGES[$svc]}"
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
