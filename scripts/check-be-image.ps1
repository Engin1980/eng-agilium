# Verifies that secret / local-only files never end up in the backend Docker image
# (Agilium.Be\.dockerignore must exclude them). Needs Docker. Exit code != 0 when a file leaks.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$be = Join-Path $root 'Agilium.Be'
$tag = 'eng-agilium-be-leakcheck'
$decoys = @('appsettings.Development.json', 'appsettings.Production.json', '.env')
$created = @()

try {
  # plant decoy files (when the developer does not have the real ones) so the check works on any machine
  foreach ($name in $decoys) {
    $path = Join-Path $be $name
    if (-not (Test-Path $path)) { Set-Content -Path $path -Value '{}' ; $created += $path }
  }

  docker build -q -t $tag $be | Out-Null
  if ($LASTEXITCODE -ne 0) { throw 'docker build failed' }

  $listing = docker run --rm --entrypoint ls $tag -a /app
  $leaked = $decoys | Where-Object { $listing -contains $_ }
  if ($leaked) {
    Write-Host "LEAK: the image contains: $($leaked -join ', ')" -ForegroundColor Red
    exit 1
  }
  Write-Host 'OK: no local/secret configuration files in the backend image.' -ForegroundColor Green
}
finally {
  $created | ForEach-Object { Remove-Item $_ -Force -ErrorAction SilentlyContinue }
  docker rmi $tag 2>$null | Out-Null
}
