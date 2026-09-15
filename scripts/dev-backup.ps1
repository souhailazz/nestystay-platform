param([string]$OutputPath)

. "$PSScriptRoot/dev-common.ps1"
Initialize-NestyStayDevState
$snapshot = Save-NestyStayEnvironment
try {
    Set-NestyStayLocalEnvironment
    $connection = Get-NestyStayDevConnectionString
    $database = Get-NestyStayConnectionPart $connection "Database" "nestystay_dev"
    $hostName = Get-NestyStayConnectionPart $connection "Host" "127.0.0.1"
    $port = Get-NestyStayConnectionPart $connection "Port" "5432"
    $user = Get-NestyStayConnectionPart $connection "Username" "nestystay"
    if (-not $database.Equals("nestystay_dev", [StringComparison]::OrdinalIgnoreCase) -or $hostName -notin @("127.0.0.1", "localhost", "::1")) {
        throw "Refusing backup: only the local nestystay_dev database is supported by this script."
    }
    $dump = Get-NestyStayPgTool "pg_dump"
    if ([string]::IsNullOrWhiteSpace($OutputPath)) {
        $OutputPath = Join-Path $script:NestyStayStateRoot ("backups\nestystay_dev_{0:yyyyMMdd_HHmmss}.dump" -f (Get-Date))
    }
    New-Item -ItemType Directory -Path (Split-Path -Parent $OutputPath) -Force | Out-Null
    $pgPassword = Get-NestyStayConnectionPart $connection "Password" ""
    if (-not [string]::IsNullOrWhiteSpace($pgPassword)) { $env:PGPASSWORD = $pgPassword }
    & $dump --format=custom --file=$OutputPath --host=$hostName --port=$port --username=$user --dbname=$database --no-owner --no-privileges
    if ($LASTEXITCODE -ne 0) { throw "Local database backup failed." }
    Write-Output "Created local development backup: $OutputPath"
} finally {
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    Restore-NestyStayEnvironment $snapshot
}
