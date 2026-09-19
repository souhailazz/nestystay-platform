Set-StrictMode -Version Latest

$script:NestyStayRoot = Split-Path -Parent $PSScriptRoot
$script:NestyStayBackendRoot = Join-Path $script:NestyStayRoot "backend"
$script:NestyStayFrontendRoot = Join-Path $script:NestyStayRoot "frontend"
$script:NestyStayStateRoot = Join-Path $script:NestyStayRoot ".local\dev"
$script:NestyStayApiUrl = "http://127.0.0.1:5019"
$script:NestyStayFrontendUrl = "http://127.0.0.1:5173"

function Initialize-NestyStayDevState {
    New-Item -ItemType Directory -Path $script:NestyStayStateRoot -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $script:NestyStayStateRoot "logs") -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $script:NestyStayStateRoot "storage") -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $script:NestyStayStateRoot "email-outbox") -Force | Out-Null
}

function Get-NestyStayDevConnectionString {
    if (-not [string]::IsNullOrWhiteSpace($env:NESTYSTAY_DEV_POSTGRES_CONNECTION)) {
        return $env:NESTYSTAY_DEV_POSTGRES_CONNECTION
    }

    return "Host=127.0.0.1;Port=55432;Database=nestystay_dev;Username=nestystay"
}

function Get-NestyStayDevEnvironmentNames {
    return @(
        "ASPNETCORE_ENVIRONMENT",
        "ASPNETCORE_URLS",
        "ConnectionStrings__Postgres",
        "ConnectionStrings__Redis",
        "Security__SessionTokenSecret",
        "Security__TotpSecretProtectionKey",
        "NESTYSTAY_SESSION_TOKEN_SECRET",
        "NESTYSTAY_TOTP_SECRET_PROTECTION_KEY",
        "NESTYSTAY_ADMIN_TOKEN_SHA256",
        "NESTYSTAY_OPERATOR_TOKEN_SHA256",
        "NESTYSTAY_WEBHOOK_SHARED_SECRET",
        "STRIPE_WEBHOOK_SECRET",
        "STRIPE_SECRET_KEY",
        "Integrations__StripeSecretKey",
        "STRIPE_PUBLISHABLE_KEY",
        "Integrations__StripePublishableKey",
        "Integrations__EkycProvider",
        "EKYC_PROVIDER",
        "Integrations__PaymentProvider",
        "PAYMENT_PROVIDER",
        "Payout__Mode",
        "PAYOUT_MODE",
        "PAYOUT_LOCAL_SCENARIO",
        "Integrations__StorageProvider",
        "OBJECT_STORAGE_PROVIDER",
        "NESTYSTAY_STORAGE_PROVIDER",
        "NESTYSTAY_STORAGE_LOCAL_ROOT",
        "NESTYSTAY_EMAIL_OUTBOX_ROOT",
        "Email__Provider",
        "EMAIL_PROVIDER",
        "NESTYSTAY_EMAIL_PROVIDER",
        "Email__Brevo__Enabled",
        "BREVO_ENABLED",
        "STRIPE_IDENTITY_LOCAL_RESULT",
        "PublicAppUrl",
        "PUBLIC_APP_URL",
        "NESTYSTAY_CORS_ALLOWED_ORIGINS",
        "BackgroundJobs__Enabled",
        "Worker__Enabled",
        "Security__EnableHttpsRedirection",
        "NESTYSTAY_MINIO_E2E"
    )
}

function Save-NestyStayEnvironment {
    $snapshot = @{}
    foreach ($name in Get-NestyStayDevEnvironmentNames) {
        $snapshot[$name] = [Environment]::GetEnvironmentVariable($name, "Process")
    }
    return ,$snapshot
}

function Restore-NestyStayEnvironment([hashtable]$Snapshot) {
    foreach ($name in Get-NestyStayDevEnvironmentNames) {
        $value = $Snapshot[$name]
        if ($null -eq $value) {
            Remove-Item ("Env:{0}" -f $name) -ErrorAction SilentlyContinue
        } else {
            [Environment]::SetEnvironmentVariable($name, $value, "Process")
        }
    }
}

function Get-Sha256Hex([string]$Value) {
    $bytes = [Text.Encoding]::UTF8.GetBytes($Value)
    return ([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))).ToLowerInvariant()
}

function Set-NestyStayLocalEnvironment {
    Initialize-NestyStayDevState
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ASPNETCORE_URLS = $script:NestyStayApiUrl
    $env:ConnectionStrings__Postgres = Get-NestyStayDevConnectionString
    $env:Security__SessionTokenSecret = "nesty-local-session-secret-change-me-2026"
    $env:Security__TotpSecretProtectionKey = "nesty-local-totp-protection-key-2026"
    $env:NESTYSTAY_SESSION_TOKEN_SECRET = $env:Security__SessionTokenSecret
    $env:NESTYSTAY_TOTP_SECRET_PROTECTION_KEY = $env:Security__TotpSecretProtectionKey
    $env:NESTYSTAY_ADMIN_TOKEN_SHA256 = Get-Sha256Hex "test-admin-token"
    $env:NESTYSTAY_OPERATOR_TOKEN_SHA256 = Get-Sha256Hex "test-operator-token"
    $env:NESTYSTAY_WEBHOOK_SHARED_SECRET = "nesty-local-webhook-shared-secret"
    $env:STRIPE_WEBHOOK_SECRET = "whsec_nesty_local"

    # Never let a developer's real Stripe key leak into a local run. The
    # backend selects its deterministic Stripe/Identity adapters when these are
    # absent, while the production adapters remain in the codebase.
    Remove-Item Env:STRIPE_SECRET_KEY -ErrorAction SilentlyContinue
    Remove-Item Env:Integrations__StripeSecretKey -ErrorAction SilentlyContinue
    $env:STRIPE_PUBLISHABLE_KEY = "pk_test_local"
    $env:Integrations__StripePublishableKey = "pk_test_local"
    $env:Integrations__EkycProvider = "stripe_identity"
    $env:EKYC_PROVIDER = "stripe_identity"
    $env:Integrations__PaymentProvider = "stripe"
    $env:PAYMENT_PROVIDER = "stripe"
    $env:Payout__Mode = "local"
    $env:PAYOUT_MODE = "local"
    if ([string]::IsNullOrWhiteSpace($env:PAYOUT_LOCAL_SCENARIO)) { $env:PAYOUT_LOCAL_SCENARIO = "paid" }
    $env:Integrations__StorageProvider = "local"
    $env:OBJECT_STORAGE_PROVIDER = "local"
    $env:NESTYSTAY_STORAGE_PROVIDER = "local"
    $env:NESTYSTAY_STORAGE_LOCAL_ROOT = Join-Path $script:NestyStayStateRoot "storage"
    # Keep the documented/tested development sink location so browser tests
    # and operators can inspect the same files without any provider access.
    $env:NESTYSTAY_EMAIL_OUTBOX_ROOT = Join-Path ([IO.Path]::GetTempPath()) "nestystay-email-outbox"
    $env:Email__Provider = "file"
    $env:EMAIL_PROVIDER = "file"
    $env:NESTYSTAY_EMAIL_PROVIDER = "file"
    $env:Email__Brevo__Enabled = "false"
    $env:BREVO_ENABLED = "false"
    $env:STRIPE_IDENTITY_LOCAL_RESULT = "verified"
    $env:PublicAppUrl = $script:NestyStayFrontendUrl
    $env:PUBLIC_APP_URL = $script:NestyStayFrontendUrl
    $env:NESTYSTAY_CORS_ALLOWED_ORIGINS = $script:NestyStayFrontendUrl
    $env:BackgroundJobs__Enabled = "true"
    $env:Worker__Enabled = "false"
    $env:Security__EnableHttpsRedirection = "false"
    Remove-Item Env:NESTYSTAY_MINIO_E2E -ErrorAction SilentlyContinue
}

function Assert-NestyStayCommand([string]$Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found in PATH."
    }
}

function Wait-NestyStayUrl([string]$Url, [int]$TimeoutSeconds = 60) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) { return $response }
        } catch { }
        Start-Sleep -Milliseconds 500
    } while ((Get-Date) -lt $deadline)
    throw "Timed out waiting for $Url"
}

function Get-NestyStayPgTool([string]$Name) {
    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    $candidates = @(
        (Join-Path ${env:ProgramFiles} "PostgreSQL\18\bin\$Name.exe"),
        (Join-Path ${env:ProgramFiles} "PostgreSQL\17\bin\$Name.exe"),
        (Join-Path ${env:ProgramFiles} "PostgreSQL\16\bin\$Name.exe")
    )
    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) { return $candidate }
    }
    throw "PostgreSQL tool '$Name' was not found. Install PostgreSQL client tools or add them to PATH."
}

function Ensure-NestyStayLocalPostgres {
    $dataRoot = Join-Path $script:NestyStayRoot ".postgres-data"
    if (-not (Test-Path -LiteralPath (Join-Path $dataRoot "PG_VERSION"))) {
        throw "The repository-owned local PostgreSQL cluster is missing at $dataRoot. Start PostgreSQL separately or set NESTYSTAY_DEV_POSTGRES_CONNECTION to an isolated development database."
    }

    $listener = Get-NetTCPConnection -LocalPort 55432 -State Listen -ErrorAction SilentlyContinue
    if ($listener) { return }
    $pgCtl = Get-NestyStayPgTool "pg_ctl"
    $log = Join-Path $script:NestyStayStateRoot "logs\postgres.log"
    & $pgCtl status -D $dataRoot *> $null
    if ($LASTEXITCODE -eq 0) { return }
    & $pgCtl start -D $dataRoot -o "-p 55432" -l $log -w
    if ($LASTEXITCODE -ne 0) { throw "Could not start the repository-owned local PostgreSQL cluster." }
}

function Get-NestyStayConnectionPart([string]$ConnectionString, [string]$Key, [string]$Default) {
    $match = [regex]::Match($ConnectionString, "(?i)(?:^|;)\s*" + [regex]::Escape($Key) + "\s*=\s*([^;]*)")
    if ($match.Success -and -not [string]::IsNullOrWhiteSpace($match.Groups[1].Value)) { return $match.Groups[1].Value.Trim() }
    return $Default
}

function Get-NestyStayProcessId([string]$Name) {
    $path = Join-Path $script:NestyStayStateRoot ("{0}.pid" -f $Name)
    if (-not (Test-Path -LiteralPath $path)) { return $null }
    $raw = Get-Content -LiteralPath $path -Raw
    $id = 0
    if ([int]::TryParse($raw.Trim(), [ref]$id)) { return $id }
    return $null
}
