#!/usr/bin/env bash
# Runs every automated check locally: backend tests (needs Docker for the SQL Server Testcontainer),
# then frontend install / lint / build / tests. Stops with a non-zero exit code on the first failure.
set -euo pipefail
cd "$(dirname "$0")/.."

echo "=== Backend tests (dotnet test) ==="
dotnet test Agilium.Be.Tests

cd Agilium.Fe
echo "=== Frontend install (npm ci) ==="
npm ci
echo "=== Frontend lint ==="
npm run lint
echo "=== Frontend build ==="
npm run build
echo "=== Frontend tests ==="
npm test

echo "All checks passed."
