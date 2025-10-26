#!/usr/bin/env bash
set -euo pipefail

# Deploys the ApiDemo application into a local MicroK8s cluster.

IMAGE_NAME="${IMAGE_NAME:-api-demo}"
IMAGE_TAG="${IMAGE_TAG:-local}"
NAMESPACE="${NAMESPACE:-api-demo}"
APP_NAME="${APP_NAME:-api-demo}"
CONTAINER_PORT="${CONTAINER_PORT:-8080}"
SERVICE_PORT="${SERVICE_PORT:-80}"

function require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Required command '$1' not found in PATH." >&2
    exit 1
  fi
}

require_cmd docker
require_cmd microk8s

if ! microk8s status --wait-ready >/dev/null 2>&1; then
  echo "MicroK8s is not ready. Start it and try again." >&2
  exit 1
fi

IMAGE_REF="${IMAGE_NAME}:${IMAGE_TAG}"
echo "Building container image ${IMAGE_REF}..."
docker build -t "${IMAGE_REF}" .

echo "Pushing image into MicroK8s containerd..."
docker save "${IMAGE_REF}" | microk8s ctr image import -

echo "Ensuring namespace ${NAMESPACE} exists..."
microk8s kubectl get ns "${NAMESPACE}" >/dev/null 2>&1 || microk8s kubectl create ns "${NAMESPACE}"

echo "Applying Deployment and Service..."
microk8s kubectl -n "${NAMESPACE}" apply -f - <<EOF
apiVersion: apps/v1
kind: Deployment
metadata:
  name: ${APP_NAME}
spec:
  replicas: 1
  selector:
    matchLabels:
      app: ${APP_NAME}
  template:
    metadata:
      labels:
        app: ${APP_NAME}
    spec:
      containers:
        - name: ${APP_NAME}
          image: ${IMAGE_REF}
          imagePullPolicy: IfNotPresent
          ports:
            - containerPort: ${CONTAINER_PORT}
          env:
            - name: ASPNETCORE_URLS
              value: http://+:${CONTAINER_PORT}
          readinessProbe:
            httpGet:
              path: /
              port: ${CONTAINER_PORT}
            initialDelaySeconds: 5
            periodSeconds: 10
          livenessProbe:
            httpGet:
              path: /
              port: ${CONTAINER_PORT}
            initialDelaySeconds: 15
            periodSeconds: 20
---
apiVersion: v1
kind: Service
metadata:
  name: ${APP_NAME}
spec:
  selector:
    app: ${APP_NAME}
  ports:
    - protocol: TCP
      port: ${SERVICE_PORT}
      targetPort: ${CONTAINER_PORT}
EOF

echo "Deployment applied. Current pod status:"
microk8s kubectl -n "${NAMESPACE}" get pods

cat <<NOTE

Next steps:
  - To stream logs: microk8s kubectl -n ${NAMESPACE} logs -f deployment/${APP_NAME}
  - To access the service: microk8s kubectl -n ${NAMESPACE} port-forward deployment/${APP_NAME} ${CONTAINER_PORT}:${CONTAINER_PORT}
  - Adjust image tag/env by exporting IMAGE_TAG=... before running this script.
NOTE
