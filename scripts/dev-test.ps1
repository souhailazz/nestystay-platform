param([switch]$SkipBrowser)

. "$PSScriptRoot/dev-common.ps1"
Assert-NestyStayCommand "dotnet"
Assert-NestyStayCommand "npm"
Initialize-NestyStayDevState
$servicesWereStarted = $false
try {
    try {
        Wait-NestyStayUrl "$script:NestyStayApiUrl/api/health/ready" 5 | Out-Null
        $servicesWereStarted = $true
    } catch {
        & "$PSScriptRoot/dev-up.ps1" -SkipBuild
        if ($LASTEXITCODE -ne 0) { throw "Could not start the local services." }
        $servicesWereStarted = $true
    }
    # Release builds overwrite the assemblies used by the API. Stop only the
    # repository-owned app trees before compiling so Windows file locks cannot
    # turn a valid test run into a false build failure.
    & "$PSScriptRoot/dev-down.ps1"

    $snapshot = Save-NestyStayEnvironment
    $locationPushed = $false
    try {
    $testRoot = Join-Path $script:NestyStayStateRoot "test-results"
    New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
    Push-Location $script:NestyStayBackendRoot
    $locationPushed = $true
    & dotnet restore NestyStay.sln
    if ($LASTEXITCODE -ne 0) { throw "Backend restore failed." }
    & dotnet build NestyStay.sln -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw "Backend build failed." }
    & dotnet test NestyStay.sln -c Release --no-build --nologo
    if ($LASTEXITCODE -ne 0) { throw "Backend tests failed." }
    Pop-Location
    $locationPushed = $false

    & "$PSScriptRoot/dev-up.ps1" -SkipBuild
    if ($LASTEXITCODE -ne 0) { throw "Could not restart the local services after backend verification." }

    Push-Location $script:NestyStayFrontendRoot
    $locationPushed = $true
    & npm test
    if ($LASTEXITCODE -ne 0) { throw "Frontend unit tests failed." }
    & npm run typecheck
    if ($LASTEXITCODE -ne 0) { throw "Frontend typecheck failed." }
    & npm run build
    if ($LASTEXITCODE -ne 0) { throw "Frontend build failed." }
    & npm run lint
    if ($LASTEXITCODE -ne 0) { throw "Frontend lint failed." }
    if (-not $SkipBrowser) {
        $env:PLAYWRIGHT_API_URL = "$script:NestyStayApiUrl/api/health"
        $env:PLAYWRIGHT_BASE_URL = $script:NestyStayFrontendUrl
        $env:NESTYSTAY_E2E_ADMIN_TOKEN = "test-admin-token"
        & npm run test:e2e
        if ($LASTEXITCODE -ne 0) { throw "Playwright browser suite failed." }
    }
    Pop-Location
    $locationPushed = $false
    } finally {
        if ($locationPushed) { Pop-Location -ErrorAction SilentlyContinue }
        Restore-NestyStayEnvironment $snapshot
    }
} finally {
    # Keep the historical behavior of leaving a usable local stack running
    # after dev-test, including when it had to be started by this script.
}
Write-Output "Local backend/frontend verification completed successfully."
