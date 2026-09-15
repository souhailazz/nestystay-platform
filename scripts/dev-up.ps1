param([switch]$SkipBuild)

. "$PSScriptRoot/dev-common.ps1"
Assert-NestyStayCommand "dotnet"
Assert-NestyStayCommand "npm"
Initialize-NestyStayDevState
$snapshot = Save-NestyStayEnvironment
try {
    Set-NestyStayLocalEnvironment
    if ([string]::IsNullOrWhiteSpace($env:NESTYSTAY_DEV_POSTGRES_CONNECTION)) { Ensure-NestyStayLocalPostgres }
    $apiProject = Join-Path $script:NestyStayBackendRoot "src/NestyStay.Api"
    $apiDll = Join-Path $apiProject "bin\Release\net10.0\NestyStay.Api.dll"
    if (-not $SkipBuild -and -not (Test-Path -LiteralPath $apiDll)) {
        & dotnet build (Join-Path $script:NestyStayBackendRoot "NestyStay.sln") -c Release --nologo
        if ($LASTEXITCODE -ne 0) { throw "Backend build failed." }
    }

    $env:NESTYSTAY_MIGRATE_ONLY = "true"
    & dotnet run --project $apiProject --configuration Release --no-build --no-restore -- --urls $script:NestyStayApiUrl
    if ($LASTEXITCODE -ne 0) { throw "Local database migration failed. Check PostgreSQL and ConnectionStrings__Postgres." }
    Remove-Item Env:NESTYSTAY_MIGRATE_ONLY -ErrorAction SilentlyContinue

    $backendLog = Join-Path $script:NestyStayStateRoot "logs\backend.log"
    $backendError = Join-Path $script:NestyStayStateRoot "logs\backend.err.log"
    $frontendLog = Join-Path $script:NestyStayStateRoot "logs\frontend.log"
    $frontendError = Join-Path $script:NestyStayStateRoot "logs\frontend.err.log"
    $backend = Start-Process -FilePath (Get-Command dotnet).Source -ArgumentList @(
        "run", "--project", $apiProject, "--configuration", "Release", "--no-build", "--no-restore", "--", "--urls", $script:NestyStayApiUrl
    ) -WorkingDirectory $script:NestyStayBackendRoot -RedirectStandardOutput $backendLog -RedirectStandardError $backendError -PassThru
    $frontend = Start-Process -FilePath (Get-Command npm.cmd).Source -ArgumentList @(
        "run", "dev", "--", "--host", "127.0.0.1", "--port", "5173"
    ) -WorkingDirectory $script:NestyStayFrontendRoot -RedirectStandardOutput $frontendLog -RedirectStandardError $frontendError -PassThru
    Set-Content -LiteralPath (Join-Path $script:NestyStayStateRoot "backend.pid") -Value $backend.Id -NoNewline
    Set-Content -LiteralPath (Join-Path $script:NestyStayStateRoot "frontend.pid") -Value $frontend.Id -NoNewline
} finally {
    Remove-Item Env:NESTYSTAY_MIGRATE_ONLY -ErrorAction SilentlyContinue
    Restore-NestyStayEnvironment $snapshot
}

Wait-NestyStayUrl "$script:NestyStayApiUrl/api/health/live" 90 | Out-Null
Wait-NestyStayUrl "$script:NestyStayApiUrl/api/health/ready" 90 | Out-Null
Wait-NestyStayUrl $script:NestyStayFrontendUrl 90 | Out-Null
Write-Output "NestyStay local services are up."
Write-Output "Frontend: $script:NestyStayFrontendUrl"
Write-Output "API:      $script:NestyStayApiUrl"
Write-Output "Admin test bearer token: test-admin-token (local only)"
