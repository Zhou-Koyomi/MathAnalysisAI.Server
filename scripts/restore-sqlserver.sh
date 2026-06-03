#!/usr/bin/env bash
set -euo pipefail

SQL_CONTAINER_NAME="${SQL_CONTAINER_NAME:-mathanalysis-sqlserver}"
SQL_DATABASE_NAME="${SQL_DATABASE_NAME:-MathAnalysisAI}"
SERVER_CONTAINER_NAME="${SERVER_CONTAINER_NAME:-mathanalysis-server}"
MSSQL_SA_PASSWORD="${MSSQL_SA_PASSWORD:-}"
BACKUP_FILE="${BACKUP_FILE:-}"
ALLOW_OVERWRITE="${ALLOW_OVERWRITE:-false}"
STOP_SERVER_BEFORE_RESTORE="${STOP_SERVER_BEFORE_RESTORE:-false}"
TIMESTAMP="$(date +%Y%m%d_%H%M%S)"
RESTORE_DATABASE_NAME="${RESTORE_DATABASE_NAME:-${SQL_DATABASE_NAME}_RestoreTest_${TIMESTAMP}}"
CONTAINER_BACKUP_DIR="/var/opt/mssql/backups"
CONTAINER_BACKUP_BASENAME="restore_${RESTORE_DATABASE_NAME}_${TIMESTAMP}.bak"
CONTAINER_BACKUP_PATH="${CONTAINER_BACKUP_DIR}/${CONTAINER_BACKUP_BASENAME}"
DATA_FILE_PATH="/var/opt/mssql/data/${RESTORE_DATABASE_NAME}.mdf"
LOG_FILE_PATH="/var/opt/mssql/data/${RESTORE_DATABASE_NAME}_log.ldf"

cleanup() {
  if docker container inspect "${SQL_CONTAINER_NAME}" >/dev/null 2>&1; then
    docker exec "${SQL_CONTAINER_NAME}" rm -f "${CONTAINER_BACKUP_PATH}" >/dev/null 2>&1 || true
  fi
}

trap cleanup EXIT

log() {
  printf '%s\n' "$*"
}

warn() {
  printf 'WARNING: %s\n' "$*" >&2
}

fail() {
  printf 'ERROR: %s\n' "$*" >&2
  exit 1
}

require_command() {
  command -v "$1" >/dev/null 2>&1 || fail "Required command not found: $1"
}

resolve_path() {
  local input_path="$1"
  local dir_name base_name
  dir_name="$(cd "$(dirname "${input_path}")" 2>/dev/null && pwd -P)" || return 1
  base_name="$(basename "${input_path}")"
  printf '%s/%s\n' "${dir_name}" "${base_name}"
}

is_true() {
  case "${1}" in
    true|TRUE|True|1|yes|YES|on|ON)
      return 0
      ;;
    *)
      return 1
      ;;
  esac
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
    -h -1 -W -Q "${query}" 2>/dev/null | tr -d '\r' | sed '/^[[:space:]]*$/d' | tail -n 1
}

query_sql_rows() {
  local sqlcmd_path="$1"
  local query="$2"

  docker exec "${SQL_CONTAINER_NAME}" "${sqlcmd_path}" \
    -C -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" \
    -h -1 -W -s "|" -Q "${query}" 2>/dev/null | tr -d '\r' | sed '/^[[:space:]]*$/d'
}

validate_database_name() {
  local name="$1"
  [[ "${name}" =~ ^[A-Za-z0-9_]+$ ]] || fail "Invalid database name: ${name}. Only letters, numbers, and underscores are allowed."
}

check_backup_in_repo() {
  local abs_backup_file="$1"
  local git_root

  if git_root="$(cd /Users/night_creek/开发/数学分析智能体/MathAnalysisAI.Server && git rev-parse --show-toplevel 2>/dev/null)"; then
    case "${abs_backup_file}" in
      "${git_root}"/*)
        warn "BACKUP_FILE is inside the Git repository. Backup files should stay outside the repo: ${abs_backup_file}"
        ;;
    esac
  fi
}

restore_database() {
  local sqlcmd_path="$1"
  local data_logical_name="$2"
  local log_logical_name="$3"
  local query

  query="RESTORE DATABASE [${RESTORE_DATABASE_NAME}] FROM DISK = N'${CONTAINER_BACKUP_PATH}' WITH MOVE N'${data_logical_name}' TO N'${DATA_FILE_PATH}', MOVE N'${log_logical_name}' TO N'${LOG_FILE_PATH}', RECOVERY, STATS = 10;"

  docker exec "${SQL_CONTAINER_NAME}" "${sqlcmd_path}" \
    -C -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" \
    -Q "${query}" >/dev/null
}

verify_table_exists() {
  local sqlcmd_path="$1"
  local table_name="$2"
  local result

  result="$(query_sql_scalar "${sqlcmd_path}" "SET NOCOUNT ON; SELECT COUNT(*) FROM [${RESTORE_DATABASE_NAME}].sys.tables WHERE name = N'${table_name}';")"
  if [[ "${result}" == "1" ]]; then
    printf 'OK'
  else
    printf 'MISSING'
  fi
}

main() {
  require_command docker

  [[ -n "${MSSQL_SA_PASSWORD}" ]] || fail "MSSQL_SA_PASSWORD is required. Example: MSSQL_SA_PASSWORD='***' BACKUP_FILE='/path/to/file.bak' ./scripts/restore-sqlserver.sh"
  [[ -n "${BACKUP_FILE}" ]] || fail "BACKUP_FILE is required. Example: BACKUP_FILE='/path/to/file.bak'"

  if is_true "${ALLOW_OVERWRITE}"; then
    fail "Phase 1 restore does not support overwriting ${SQL_DATABASE_NAME}. ALLOW_OVERWRITE=true is not allowed yet."
  fi

  if is_true "${STOP_SERVER_BEFORE_RESTORE}"; then
    fail "Phase 1 restore does not stop ${SERVER_CONTAINER_NAME}. STOP_SERVER_BEFORE_RESTORE=true is not supported yet."
  fi

  docker info >/dev/null 2>&1 || fail "Docker daemon is not running or not accessible."
  docker container inspect "${SQL_CONTAINER_NAME}" >/dev/null 2>&1 || fail "Container not found: ${SQL_CONTAINER_NAME}"

  local container_status
  container_status="$(docker inspect -f '{{.State.Status}}' "${SQL_CONTAINER_NAME}" 2>/dev/null || true)"
  [[ "${container_status}" == "running" ]] || fail "Container is not running: ${SQL_CONTAINER_NAME} (status=${container_status:-unknown})"

  [[ -f "${BACKUP_FILE}" ]] || fail "Backup file not found: ${BACKUP_FILE}"
  [[ "${BACKUP_FILE}" == *.bak ]] || fail "BACKUP_FILE must end with .bak: ${BACKUP_FILE}"

  local abs_backup_file
  abs_backup_file="$(resolve_path "${BACKUP_FILE}")" || fail "Cannot resolve BACKUP_FILE path: ${BACKUP_FILE}"
  check_backup_in_repo "${abs_backup_file}"

  validate_database_name "${RESTORE_DATABASE_NAME}"
  [[ "${RESTORE_DATABASE_NAME}" != "${SQL_DATABASE_NAME}" ]] || fail "Phase 1 restore refuses to target the primary database: ${SQL_DATABASE_NAME}"

  local sqlcmd_path
  sqlcmd_path="$(detect_sqlcmd_path)" || fail "sqlcmd not found in container. Checked /opt/mssql-tools18/bin/sqlcmd and /opt/mssql-tools/bin/sqlcmd"

  local source_db_exists target_db_exists
  source_db_exists="$(query_sql_scalar "${sqlcmd_path}" "SET NOCOUNT ON; SELECT DB_ID(N'${SQL_DATABASE_NAME}');")"
  [[ -n "${source_db_exists}" && "${source_db_exists}" != "NULL" ]] || fail "Primary database does not exist: ${SQL_DATABASE_NAME}"

  target_db_exists="$(query_sql_scalar "${sqlcmd_path}" "SET NOCOUNT ON; SELECT DB_ID(N'${RESTORE_DATABASE_NAME}');")"
  if [[ -n "${target_db_exists}" && "${target_db_exists}" != "NULL" ]]; then
    fail "Target restore database already exists and will not be overwritten: ${RESTORE_DATABASE_NAME}"
  fi

  docker exec "${SQL_CONTAINER_NAME}" mkdir -p "${CONTAINER_BACKUP_DIR}" >/dev/null 2>&1 || fail "Cannot create container backup directory: ${CONTAINER_BACKUP_DIR}"
  docker cp "${abs_backup_file}" "${SQL_CONTAINER_NAME}:${CONTAINER_BACKUP_PATH}" >/dev/null 2>&1 || fail "docker cp failed when copying backup file into container: ${abs_backup_file}"
  docker exec "${SQL_CONTAINER_NAME}" test -f "${CONTAINER_BACKUP_PATH}" >/dev/null 2>&1 || fail "Backup file missing inside container after copy: ${CONTAINER_BACKUP_PATH}"
  docker exec -u 0 "${SQL_CONTAINER_NAME}" chown mssql:mssql "${CONTAINER_BACKUP_PATH}" >/dev/null 2>&1 || fail "Failed to adjust backup file owner inside container: ${CONTAINER_BACKUP_PATH}"
  docker exec -u 0 "${SQL_CONTAINER_NAME}" chmod 600 "${CONTAINER_BACKUP_PATH}" >/dev/null 2>&1 || fail "Failed to adjust backup file permissions inside container: ${CONTAINER_BACKUP_PATH}"

  local filelist_output data_logical_name log_logical_name
  filelist_output="$(query_sql_rows "${sqlcmd_path}" "RESTORE FILELISTONLY FROM DISK = N'${CONTAINER_BACKUP_PATH}';")"
  [[ -n "${filelist_output}" ]] || fail "Failed to read logical file names from backup. Check backup validity and SA password."

  data_logical_name="$(printf '%s\n' "${filelist_output}" | awk -F'|' '$3 ~ /^[[:space:]]*D[[:space:]]*$/ {gsub(/^[[:space:]]+|[[:space:]]+$/, "", $1); print $1; exit}')"
  log_logical_name="$(printf '%s\n' "${filelist_output}" | awk -F'|' '$3 ~ /^[[:space:]]*L[[:space:]]*$/ {gsub(/^[[:space:]]+|[[:space:]]+$/, "", $1); print $1; exit}')"

  [[ -n "${data_logical_name}" ]] || fail "Could not determine data logical file name from backup."
  [[ -n "${log_logical_name}" ]] || fail "Could not determine log logical file name from backup."

  log "Starting restore to temporary database: ${RESTORE_DATABASE_NAME}"
  restore_database "${sqlcmd_path}" "${data_logical_name}" "${log_logical_name}" || fail "RESTORE DATABASE failed. Check backup compatibility, logical file names, SQL Server disk space, or SQL Server logs."

  local restored_db_id
  restored_db_id="$(query_sql_scalar "${sqlcmd_path}" "SET NOCOUNT ON; SELECT DB_ID(N'${RESTORE_DATABASE_NAME}');")"
  [[ -n "${restored_db_id}" && "${restored_db_id}" != "NULL" ]] || fail "Restore completed without a visible target database: ${RESTORE_DATABASE_NAME}"

  local appusers_count analysisresults_count migrations_count
  appusers_count="$(query_sql_scalar "${sqlcmd_path}" "SET NOCOUNT ON; SELECT COUNT(*) FROM [${RESTORE_DATABASE_NAME}].dbo.[AppUsers];")"
  analysisresults_count="$(query_sql_scalar "${sqlcmd_path}" "SET NOCOUNT ON; SELECT COUNT(*) FROM [${RESTORE_DATABASE_NAME}].dbo.[AnalysisResults];")"
  migrations_count="$(query_sql_scalar "${sqlcmd_path}" "SET NOCOUNT ON; SELECT COUNT(*) FROM [${RESTORE_DATABASE_NAME}].dbo.[__EFMigrationsHistory];")"

  log "Restore completed successfully."
  log "Temporary restore database: ${RESTORE_DATABASE_NAME}"
  log "Core table checks:"
  log "  AppUsers: $(verify_table_exists "${sqlcmd_path}" "AppUsers")"
  log "  Courses: $(verify_table_exists "${sqlcmd_path}" "Courses")"
  log "  Chapters: $(verify_table_exists "${sqlcmd_path}" "Chapters")"
  log "  KnowledgePoints: $(verify_table_exists "${sqlcmd_path}" "KnowledgePoints")"
  log "  Problems: $(verify_table_exists "${sqlcmd_path}" "Problems")"
  log "  StudentSolutions: $(verify_table_exists "${sqlcmd_path}" "StudentSolutions")"
  log "  AnalysisResults: $(verify_table_exists "${sqlcmd_path}" "AnalysisResults")"
  log "  UserCourseStats: $(verify_table_exists "${sqlcmd_path}" "UserCourseStats")"
  log "Summary counts:"
  log "  AppUsers count: ${appusers_count:-unknown}"
  log "  AnalysisResults count: ${analysisresults_count:-unknown}"
  log "  __EFMigrationsHistory count: ${migrations_count:-unknown}"
  log "Cleanup hint:"
  log "  docker exec ${SQL_CONTAINER_NAME} ${sqlcmd_path} -C -S localhost -U sa -P '***' -Q \"DROP DATABASE [${RESTORE_DATABASE_NAME}]\""
}

main "$@"
