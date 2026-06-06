#!/usr/bin/env bash
set -euo pipefail

SQL_CONTAINER_NAME="${SQL_CONTAINER_NAME:-mathanalysis-sqlserver}"
SQL_DATABASE_NAME="${SQL_DATABASE_NAME:-MathAnalysisAI}"
BACKUP_HOST_DIR="${BACKUP_HOST_DIR:-$HOME/Backups/mathanalysis-ai}"
CONTAINER_BACKUP_DIR="/var/opt/mssql/backups"
TIMESTAMP="$(date +%Y%m%d_%H%M%S)"
BACKUP_BASENAME="${SQL_DATABASE_NAME}_${TIMESTAMP}.bak"
CONTAINER_BACKUP_PATH="${CONTAINER_BACKUP_DIR}/${BACKUP_BASENAME}"
HOST_BACKUP_PATH="${BACKUP_HOST_DIR}/${BACKUP_BASENAME}"
MSSQL_SA_PASSWORD="${MSSQL_SA_PASSWORD:-}"

log() {
  printf '%s\n' "$*"
}

fail() {
  printf 'ERROR: %s\n' "$*" >&2
  exit 1
}

require_command() {
  command -v "$1" >/dev/null 2>&1 || fail "Required command not found: $1"
}

detect_sqlcmd_path() {
  local path
  for path in /opt/mssql-tools18/bin/sqlcmd /opt/mssql-tools/bin/sqlcmd; do
    if docker exec "${SQL_CONTAINER_NAME}" test -x "${path}" >/dev/null 2>&1; then
      printf '%s\n' "${path}"
      return 0
    fi
  done

  return 1
}

query_sql_scalar() {
  local sqlcmd_path="$1"
  local query="$2"

  docker exec "${SQL_CONTAINER_NAME}" "${sqlcmd_path}" \
    -C -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" \
    -h -1 -W -Q "${query}" 2>/dev/null | tr -d '\r' | sed '/^\s*$/d' | tail -n 1
}

backup_sql() {
  local sqlcmd_path="$1"
  local query
  query="BACKUP DATABASE [${SQL_DATABASE_NAME}] TO DISK = N'${CONTAINER_BACKUP_PATH}' WITH INIT, COPY_ONLY, COMPRESSION, STATS = 10;"

  docker exec "${SQL_CONTAINER_NAME}" "${sqlcmd_path}" \
    -C -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" \
    -Q "${query}" >/dev/null
}

main() {
  require_command docker

  [[ -n "${MSSQL_SA_PASSWORD}" ]] || fail "MSSQL_SA_PASSWORD is required. Example: MSSQL_SA_PASSWORD='***' ./scripts/backup-sqlserver.sh"

  docker info >/dev/null 2>&1 || fail "Docker daemon is not running or not accessible."

  docker container inspect "${SQL_CONTAINER_NAME}" >/dev/null 2>&1 || fail "Container not found: ${SQL_CONTAINER_NAME}"

  local container_status
  container_status="$(docker inspect -f '{{.State.Status}}' "${SQL_CONTAINER_NAME}" 2>/dev/null || true)"
  [[ "${container_status}" == "running" ]] || fail "Container is not running: ${SQL_CONTAINER_NAME} (status=${container_status:-unknown})"

  mkdir -p "${BACKUP_HOST_DIR}" 2>/dev/null || fail "Cannot create backup host directory: ${BACKUP_HOST_DIR}"
  [[ -w "${BACKUP_HOST_DIR}" ]] || fail "Backup host directory is not writable: ${BACKUP_HOST_DIR}"

  local sqlcmd_path
  sqlcmd_path="$(detect_sqlcmd_path)" || fail "sqlcmd not found in container. Checked /opt/mssql-tools18/bin/sqlcmd and /opt/mssql-tools/bin/sqlcmd"

  local db_exists
  db_exists="$(query_sql_scalar "${sqlcmd_path}" "SET NOCOUNT ON; SELECT DB_ID(N'${SQL_DATABASE_NAME}');")"
  [[ -n "${db_exists}" && "${db_exists}" != "NULL" ]] || fail "Database does not exist: ${SQL_DATABASE_NAME}"

  docker exec "${SQL_CONTAINER_NAME}" mkdir -p "${CONTAINER_BACKUP_DIR}" >/dev/null 2>&1 || fail "Cannot create container backup directory: ${CONTAINER_BACKUP_DIR}"

  log "Starting backup for database: ${SQL_DATABASE_NAME}"
  backup_sql "${sqlcmd_path}" || fail "BACKUP DATABASE failed. Check SQL Server logs, database name, or SA password."

  docker exec "${SQL_CONTAINER_NAME}" test -f "${CONTAINER_BACKUP_PATH}" >/dev/null 2>&1 || fail "Backup file was not created in container: ${CONTAINER_BACKUP_PATH}"

  docker cp "${SQL_CONTAINER_NAME}:${CONTAINER_BACKUP_PATH}" "${HOST_BACKUP_PATH}" >/dev/null 2>&1 || fail "docker cp failed when copying backup to host: ${HOST_BACKUP_PATH}"

  [[ -f "${HOST_BACKUP_PATH}" ]] || fail "Host backup file missing after copy: ${HOST_BACKUP_PATH}"

  local file_size
  file_size="$(wc -c < "${HOST_BACKUP_PATH}" | tr -d ' ')"

  docker exec "${SQL_CONTAINER_NAME}" rm -f "${CONTAINER_BACKUP_PATH}" >/dev/null 2>&1 || log "Warning: failed to delete container backup file: ${CONTAINER_BACKUP_PATH}"

  log "Backup completed successfully."
  log "Host backup file: ${HOST_BACKUP_PATH}"
  log "Backup file size: ${file_size} bytes"
}

main "$@"
