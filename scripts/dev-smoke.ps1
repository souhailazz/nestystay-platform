. "$PSScriptRoot/dev-common.ps1"
Initialize-NestyStayDevState
try { Wait-NestyStayUrl "$script:NestyStayApiUrl/api/health/ready" 5 | Out-Null }
catch { & "$PSScriptRoot/dev-up.ps1" -SkipBuild }

$urls = @(
    "$script:NestyStayApiUrl/api/health/live",
    "$script:NestyStayApiUrl/api/health/ready",
    "$script:NestyStayApiUrl/api/properties",
    "$script:NestyStayApiUrl/api/spec/public/pages",
    "$script:NestyStayApiUrl/api/spec/experiences",
    "$script:NestyStayApiUrl/api/directories/providers",
    "$script:NestyStayFrontendUrl/",
    "$script:NestyStayFrontendUrl/explore"
)
foreach ($url in $urls) {
    $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 30
    if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 300) { throw "$url returned HTTP $($response.StatusCode)." }
    Write-Output ("PASS {0} HTTP {1}" -f $url, $response.StatusCode)
}
Write-Output "Local smoke checks passed."
