#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
LITELLM_DIR="$ROOT_DIR/infra/litellm"
ENV_FILE="$LITELLM_DIR/.env.local"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing $ENV_FILE"
  echo "Create it from infra/litellm/.env.example and fill local secrets."
  exit 1
fi

cd "$LITELLM_DIR"
set -a
source "$ENV_FILE"
set +a

: "${LITELLM_MASTER_KEY:?LITELLM_MASTER_KEY is required in .env.local}"
: "${DEEPSEEK_API_KEY:?DEEPSEEK_API_KEY is required in .env.local}"
: "${DASHSCOPE_API_KEY:?DASHSCOPE_API_KEY is required in .env.local}"

exec litellm --config config.yaml --port 4000
