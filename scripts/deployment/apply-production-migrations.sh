#!/usr/bin/env bash
set -euo pipefail

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repo_root=$(CDPATH= cd -- "$script_dir/../.." && pwd)
deploy_dir="$repo_root/deploy"
compose_file="$deploy_dir/docker-compose.yml"

if [ ! -f "$compose_file" ]; then
  echo "Missing deployment Compose file: $compose_file" >&2
  exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
  echo "docker is required to run production migrations." >&2
  exit 1
fi

cd "$deploy_dir"

echo "Running AssistantEngineer PostgreSQL migrations from $deploy_dir"
echo "This command uses the assistantengineer-api image/container environment and does not print connection strings."

docker compose run --rm assistantengineer-api dotnet AssistantEngineer.Api.dll --migrate-database

cat <<'EOF'
Migration runner completed.

Optional production verification, without printing secrets:
  docker compose run --rm assistantengineer-api dotnet AssistantEngineer.Api.dll --migrate-database

If a PostgreSQL service/container is available in this Compose project, verify the latest __EFMigrationsHistory row
with a production-safe psql command that reads credentials from the server environment or an operator-owned secret store.
EOF
