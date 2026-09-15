param([string]$BackupPath)

. "$PSScriptRoot/dev-common.ps1"
Initialize-NestyStayDevState
$snapshot = Save-NestyStayEnvironment
$restoreDatabase = $null
try {
    Set-NestyStayLocalEnvironment
    $connection = Get-NestyStayDevConnectionString
    $database = Get-NestyStayConnectionPart $connection "Database" "nestystay_dev"
    $hostName = Get-NestyStayConnectionPart $connection "Host" "127.0.0.1"
    $port = Get-NestyStayConnectionPart $connection "Port" "5432"
    $user = Get-NestyStayConnectionPart $connection "Username" "nestystay"
    $adminUser = "postgres"
    if (-not $database.Equals("nestystay_dev", [StringComparison]::OrdinalIgnoreCase) -or $hostName -notin @("127.0.0.1", "localhost", "::1")) {
        throw "Refusing restore test: only the local nestystay_dev connection is accepted."
    }
    if ([string]::IsNullOrWhiteSpace($BackupPath)) {
        $BackupPath = Get-ChildItem -LiteralPath (Join-Path $script:NestyStayStateRoot "backups") -Filter "*.dump" -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName
    }
    if ([string]::IsNullOrWhiteSpace($BackupPath) -or -not (Test-Path -LiteralPath $BackupPath)) { throw "No local backup was supplied or found." }
    $pgRestore = Get-NestyStayPgTool "pg_restore"
    $createdb = Get-NestyStayPgTool "createdb"
    $dropdb = Get-NestyStayPgTool "dropdb"
    $restoreDatabase = "nestystay_restore_test"
    $pgPassword = Get-NestyStayConnectionPart $connection "Password" ""
    if (-not [string]::IsNullOrWhiteSpace($pgPassword)) { $env:PGPASSWORD = $pgPassword }
    & $dropdb --if-exists --host $hostName --port $port --username $adminUser $restoreDatabase | Out-Null
    & $createdb --host $hostName --port $port --username $adminUser $restoreDatabase
    if ($LASTEXITCODE -ne 0) { throw "Could not create the isolated restore-test database." }
    & $pgRestore --dbname $restoreDatabase --host $hostName --port $port --username $adminUser --no-owner --no-privileges $BackupPath
    if ($LASTEXITCODE -ne 0) { throw "Restore into the isolated local database failed." }
    Write-Output "Restore test passed in isolated database $restoreDatabase."
} finally {
    if ($restoreDatabase) {
        $cleanupConnection = Get-NestyStayDevConnectionString
        $cleanupHost = Get-NestyStayConnectionPart $cleanupConnection "Host" "127.0.0.1"
        $cleanupPort = Get-NestyStayConnectionPart $cleanupConnection "Port" "5432"
        & $dropdb --if-exists --host $cleanupHost --port $cleanupPort --username postgres $restoreDatabase | Out-Null
    }
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    Restore-NestyStayEnvironment $snapshot
}
