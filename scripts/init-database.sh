#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

if [[ -f .env ]]; then
  # shellcheck disable=SC1091
  source .env
fi

SA_PASSWORD="${SA_PASSWORD:-EnigmaChamber_StrongPass123!}"
DB_NAME="${DB_NAME:-EnigmaChamber}"
CONTAINER="${SQL_CONTAINER:-enigmachamber-sql}"

sqlcmd() {
  docker exec "$CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" -C "$@"
}

echo "Waiting for SQL Server..."
until sqlcmd -Q "SELECT 1" &>/dev/null; do
  sleep 2
done

echo "Creating database if missing..."
sqlcmd -Q "IF DB_ID('$DB_NAME') IS NULL CREATE DATABASE [$DB_NAME];"

for script in sql/03_procedures.sql sql/04_functions.sql sql/05_triggers.sql sql/06_cursors.sql; do
  echo "Running $script..."
  docker exec -i "$CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" -C -d "$DB_NAME" \
    < "$script"
done

echo "Database init complete."
