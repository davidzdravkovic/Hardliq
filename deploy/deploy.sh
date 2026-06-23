#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
cd "$ROOT"

if [[ ! -f .env ]]; then
  echo "Missing .env file. Copy .env.example to .env and edit it first."
  exit 1
fi

docker compose --env-file .env up -d --build

echo ""
echo "Deployment started."
echo "Open the URL set in PUBLIC_ORIGIN (default port ${HTTP_PORT:-80})."
echo "Logs: docker compose --env-file .env logs -f"
