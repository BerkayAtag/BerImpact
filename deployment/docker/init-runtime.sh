#!/bin/sh
set -eu
runtime_password=$(cat /run/secrets/db_runtime_password)
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --set=runtime_password="$runtime_password" <<'SQL'
SELECT format('CREATE ROLE berimpact_runtime LOGIN PASSWORD %L', :'runtime_password') \gexec
GRANT CONNECT ON DATABASE berimpact TO berimpact_runtime;
GRANT USAGE ON SCHEMA public TO berimpact_runtime;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO berimpact_runtime;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO berimpact_runtime;
SQL
