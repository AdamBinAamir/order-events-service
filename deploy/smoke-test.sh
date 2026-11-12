#!/usr/bin/env bash
# Places an order through a port-forward and waits until the worker logs it.
# Usage (after `kubectl apply -f deploy/k8s`):  bash deploy/smoke-test.sh
set -euo pipefail

kubectl port-forward svc/orders-api 8080:80 >/dev/null 2>&1 &
PF_PID=$!
trap 'kill $PF_PID 2>/dev/null || true' EXIT

for _ in $(seq 1 30); do
  curl -sf http://localhost:8080/health/ready >/dev/null && break
  sleep 2
done

RESPONSE=$(curl -sf -X POST http://localhost:8080/orders \
  -H 'Content-Type: application/json' \
  -d '{"customerId":"3f2504e0-4f89-11d3-9a0c-0305e82c3301","lines":[{"productId":"6f9619ff-8b86-d011-b42d-00c04fc964ff","quantity":2,"unitPrice":19.99,"currency":"EUR"}]}')
ORDER_ID=$(echo "$RESPONSE" | sed -E 's/.*"id":"([^"]+)".*/\1/')
echo "Placed order $ORDER_ID"

for _ in $(seq 1 30); do
  if kubectl logs deployment/orders-worker | grep -q "Order $ORDER_ID placed"; then
    echo "Worker handled order $ORDER_ID"
    exit 0
  fi
  sleep 2
done

echo "The worker did not log order $ORDER_ID" >&2
kubectl logs deployment/orders-worker --tail=50 >&2
exit 1
