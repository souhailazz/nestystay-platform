. "$PSScriptRoot/dev-common.ps1"
Initialize-NestyStayDevState
foreach ($name in @("frontend", "backend")) {
    $id = Get-NestyStayProcessId $name
    if ($id) {
        $process = Get-Process -Id $id -ErrorAction SilentlyContinue
        if ($process) {
            & taskkill.exe /PID $id /T /F | Out-Null
            Write-Output "Stopped $name process tree ($id)."
        }
    }
    Remove-Item -LiteralPath (Join-Path $script:NestyStayStateRoot ("{0}.pid" -f $name)) -Force -ErrorAction SilentlyContinue
}
Write-Output "PostgreSQL was not touched."
