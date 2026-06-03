#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
ENV_FILE="$ROOT_DIR/.env.local"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing $ENV_FILE"
  echo "Create it with local server settings (ConnectionStrings__DefaultConnection, LLMGateway__Mode, LiteLLM__ApiKey...)."
  exit 1
fi

cd "$ROOT_DIR"
set -a
source "$ENV_FILE"
set +a

: "${ConnectionStrings__DefaultConnection:?ConnectionStrings__DefaultConnection is required in .env.local}"
: "${LLMGateway__Mode:?LLMGateway__Mode is required in .env.local}"
: "${LiteLLM__ApiKey:?LiteLLM__ApiKey is required in .env.local}"

exec dotnet run --no-launch-profile --urls "${ASPNETCORE_URLS:-http://127.0.0.1:5131}"
