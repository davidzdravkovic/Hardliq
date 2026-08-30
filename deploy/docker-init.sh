#!/bin/sh
set -e
export PGPASSWORD="$POSTGRES_PASSWORD"

until pg_isready -h postgres -U "$POSTGRES_USER"; do
  echo "waiting for postgres..."
  sleep 2
done

if psql -h postgres -U "$POSTGRES_USER" -d postgres -tAc \
  "SELECT 1 FROM pg_database WHERE datname = '${POSTGRES_DB}'" | grep -q 1; then
  echo "database ${POSTGRES_DB} already exists"
else
  psql -h postgres -U "$POSTGRES_USER" -d postgres -v ON_ERROR_STOP=1 \
    -c "CREATE DATABASE ${POSTGRES_DB} OWNER ${POSTGRES_USER}"
  echo "database ${POSTGRES_DB} created"
fi

psql -h postgres -U "$POSTGRES_USER" -d "${POSTGRES_DB}" -v ON_ERROR_STOP=1 \
  -c "CREATE EXTENSION IF NOT EXISTS vector;"
echo "pgvector extension ensured on ${POSTGRES_DB}"
