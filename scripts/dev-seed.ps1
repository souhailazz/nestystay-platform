. "$PSScriptRoot/dev-common.ps1"
Initialize-NestyStayDevState
try { Wait-NestyStayUrl "$script:NestyStayApiUrl/api/health/ready" 5 | Out-Null }
catch {
    & "$PSScriptRoot/dev-up.ps1" -SkipBuild
}

$seed = Invoke-RestMethod -Method Post -Uri "$script:NestyStayApiUrl/api/spec/seed" -TimeoutSec 30
$checks = [ordered]@{
    properties = "$script:NestyStayApiUrl/api/properties"
    publicPages = "$script:NestyStayApiUrl/api/spec/public/pages"
    experiences = "$script:NestyStayApiUrl/api/spec/experiences"
    journal = "$script:NestyStayApiUrl/api/spec/journal"
    hostProfiles = "$script:NestyStayApiUrl/api/spec/host-profiles"
    directories = "$script:NestyStayApiUrl/api/directories/providers"
}
$counts = [ordered]@{}
foreach ($entry in $checks.GetEnumerator()) {
    $value = Invoke-RestMethod -Method Get -Uri $entry.Value -TimeoutSec 30
    $counts[$entry.Key] = if ($value -is [System.Collections.ICollection]) { $value.Count } else { 1 }
}
Write-Output ("Seed status: " + ($seed | ConvertTo-Json -Compress))
Write-Output ("Local deterministic data counts: " + ($counts | ConvertTo-Json -Compress))
Write-Output "M3/M5 operational rows are created by their authenticated stores on first real workflow use; the browser suite exercises those paths without production data."
