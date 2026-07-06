# Production Compose Hygiene

ED-24OPS.CLEANUP documents two operator-only cleanup cases for the production checkout at
`/opt/assistantengineer`. It does not stop, rename, remove, or recreate any container, volume, database, or
environment backup.

## Orphan PostgreSQL warning

The tracked `deploy/docker-compose.yml` fixes the Compose project name as `assistantengineer`, but it does not
define a PostgreSQL service. Repository history shows that the tracked production Compose scaffold has never
defined one. A running container named `assistantengineer-postgres-1` is therefore reported as an orphan when
the current file manages the same Compose project: the container belongs to an earlier or VPS-local Compose
definition whose `postgres` service is absent from the tracked file. This is not evidence that the database is
unused.

Treat `assistantengineer-postgres-1` as production data-bearing infrastructure. Do not add or run
`--remove-orphans`, and do not delete the container or its volumes to silence the warning.

From `/opt/assistantengineer/deploy`, inspect the current Compose services and database infrastructure:

```sh
docker compose --env-file .env -f docker-compose.yml ps
docker ps --filter "name=postgres"
docker inspect assistantengineer-postgres-1
docker volume ls
```

`docker inspect` confirms the container's Compose labels, mounts, image, and runtime configuration. Its output
may include environment values, so keep the output private and do not paste it into tickets, chats, or logs.
Record the container labels and mounted volume names in the private operator record before any separately
reviewed infrastructure change. The safe outcome for this stage is to leave the healthy PostgreSQL container
and its data in place.

## Environment backup files

The repository deployment and operations scripts do not create `deploy/.env.before-*` files. They were created
by an external/manual VPS procedure. The exact pattern is ignored by `deploy/.gitignore` so backup copies no
longer pollute `git status`; the active `deploy/.env` remains ignored separately.

Future operator procedures should place environment backups outside the Git working tree by default:

```sh
mkdir -p /opt/assistantengineer-runtime-backups/env
```

For existing files, first confirm each matched file is a backup copy and is not the active
`/opt/assistantengineer/deploy/.env`. Then move only the timestamped backup copies:

```sh
mkdir -p /opt/assistantengineer-runtime-backups/env
mv /opt/assistantengineer/deploy/.env.before-* /opt/assistantengineer-runtime-backups/env/
```

The move is an explicit operator action, not an automatic deployment step. Do not delete the backups and do
not add the backup directory or any `.env` content to Git. Operators may select another protected path outside
the repository when required by host policy.
