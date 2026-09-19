param([switch]$SkipStart)

. "$PSScriptRoot/dev-common.ps1"
Assert-NestyStayCommand "dotnet"
& "$PSScriptRoot/dev-down.ps1"
$snapshot = Save-NestyStayEnvironment
try {
    Set-NestyStayLocalEnvironment
    $connection = Get-NestyStayDevConnectionString
    $database = Get-NestyStayConnectionPart $connection "Database" "nestystay_dev"
    if (-not $database.Equals("nestystay_dev", [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing reset: database '$database' is not the development database nestystay_dev."
    }
    $project = Join-Path $script:NestyStayBackendRoot "src\NestyStay.Infrastructure"
    $startup = Join-Path $script:NestyStayBackendRoot "src\NestyStay.Api"
    & dotnet ef database drop --force --project $project --startup-project $startup --context NestyStayDbContext --no-build
    if ($LASTEXITCODE -ne 0) { throw "Development database drop failed." }
    & dotnet ef database update --project $project --startup-project $startup --context NestyStayDbContext --no-build
    if ($LASTEXITCODE -ne 0) { throw "Development database migration failed." }
} finally {
    Restore-NestyStayEnvironment $snapshot
}
if (-not $SkipStart) {
    & "$PSScriptRoot/dev-up.ps1" -SkipBuild
    & "$PSScriptRoot/dev-seed.ps1"
}
Write-Output "Development database reset and migrated. Production/staging databases were not touched."
