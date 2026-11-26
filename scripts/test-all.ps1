# Runs every automated check locally: backend tests (needs Docker for the SQL Server Testcontainer),
# then frontend install / lint / build / tests. Stops with a non-zero exit code on the first failure.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Invoke-Step([string]$Name, [scriptblock]$Action) {
  Write-Host "`n=== $Name ===" -ForegroundColor Cyan
  & $Action
  if ($LASTEXITCODE -ne 0) { Write-Host "FAILED: $Name" -ForegroundColor Red; exit $LASTEXITCODE }
}

Push-Location $root
try {
  Invoke-Step 'Backend tests (dotnet test)' { dotnet test Agilium.Be.Tests }
  Push-Location Agilium.Fe
  try {
    Invoke-Step 'Frontend install (npm ci)' { npm ci }
    Invoke-Step 'Frontend lint' { npm run lint }
    Invoke-Step 'Frontend build' { npm run build }
    Invoke-Step 'Frontend tests' { npm test }
  }
  finally { Pop-Location }
}
finally { Pop-Location }

Write-Host "`nAll checks passed." -ForegroundColor Green
