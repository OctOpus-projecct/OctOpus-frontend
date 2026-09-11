param(
    [string]$ServerPath = "$PSScriptRoot/../../OctOpus-backend/Builds/WindowsServer/OctOpusServer.exe",
    [string]$ClientPath = "$PSScriptRoot/../Builds/WindowsClient/OctOpus.exe",
    [switch]$ExternalServer,
    [string]$ServerAddress = '127.0.0.1'
)
$ErrorActionPreference = 'Stop'
if (-not $ExternalServer -and $ServerAddress -ne '127.0.0.1') {
    throw 'Use -ExternalServer for a different server address.'
}
$requiredFiles = @($ClientPath)
if (-not $ExternalServer) { $requiredFiles += $ServerPath }
foreach ($file in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Build missing: $file" }
}
$output = Join-Path $PSScriptRoot ('../TestResults/strike-session-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $output
$output = (Resolve-Path -LiteralPath $output).Path
$ownedProcesses = [Collections.Generic.List[Diagnostics.Process]]::new()
$clientLogs = [Collections.Generic.List[string]]::new()
$sessionDeadline = [DateTime]::UtcNow.AddSeconds(180)
function Start-Game([string]$file, [string]$log, [string[]]$extra = @()) {
    $start = [Diagnostics.ProcessStartInfo]::new((Resolve-Path -LiteralPath $file).Path)
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    foreach ($argument in (@('-batchmode', '-nographics', '-logFile', $log) + $extra)) {
        $start.ArgumentList.Add($argument)
    }
    $start.Environment['OCTOPUS_SERVER_ADDRESS'] = $ServerAddress
    $null = $start.Environment.Remove('OCTOPUS_BIND_ADDRESS')
    $process = [Diagnostics.Process]::Start($start)
    $ownedProcesses.Add($process)
    return $process
}
function Wait-Marker([string]$log, [string]$marker, [Diagnostics.Process]$process, [int]$seconds = 30) {
    $deadline = [DateTime]::UtcNow.AddSeconds($seconds)
    while ([DateTime]::UtcNow -lt $deadline -and [DateTime]::UtcNow -lt $sessionDeadline) {
        foreach ($owned in $ownedProcesses) {
            if ($owned.HasExited) { throw "Owned process exited ($($owned.ExitCode)); logs: $output" }
        }
        foreach ($clientLog in $clientLogs) {
            if ((Test-Path -LiteralPath $clientLog) -and
                (Select-String -LiteralPath $clientLog -Pattern 'StrikeTest=Failed|Exception:|Exception\b' -Quiet)) {
                throw "Strike harness failed or raised an exception; see $clientLog"
            }
        }
        if ((Test-Path -LiteralPath $log) -and (Select-String -LiteralPath $log -SimpleMatch $marker -Quiet)) { return }
        Start-Sleep -Milliseconds 200
    }
    throw "Timed out waiting for $marker; logs: $output"
}
try {
    if (-not $ExternalServer) {
        $server = Start-Game $ServerPath "$output/server.log" @('-octopus-stamina-baseline-test')
        Wait-Marker "$output/server.log" '[OctOpus] Server=Started' $server
    }
    $workerLog = "$output/worker.log"
    $observerLog = "$output/observer.log"
    $clientLogs.Add($workerLog)
    $worker = Start-Game $ClientPath $workerLog @('-octopus-connect', '-octopus-strike-test')
    Wait-Marker $workerLog '[OctOpus] StrikeStage=DuplicateCooldown' $worker
    $clientLogs.Add($observerLog)
    $observer = Start-Game $ClientPath $observerLog @('-octopus-connect', '-octopus-strike-observer')
    Wait-Marker $workerLog '[OctOpus] StrikeTest=Passed' $worker 130
    Wait-Marker $observerLog '[OctOpus] StrikeTest=Passed' $observer 5
    foreach ($stage in @('DuplicateCooldown', 'ObserverRejectionObserved', 'CancelRestoredHealth',
        'IdleRegeneration', 'InsufficientStaminaPreservedProgress', 'WorkingRegeneration', 'Depleted',
        'ObserverDepletedRejectionObserved', 'DepletedRequestsRejected', 'RespawnIdentityPreserved',
        'TimeoutRestoredHealth', 'TimeoutPreservedStamina')) {
        Wait-Marker $workerLog "[OctOpus] StrikeStage=$stage" $worker 1
    }
    foreach ($stage in @('LateJoinDamageObserved', 'NonWorkerStrikeRejected',
        'ObserverDepletionObserved', 'ObserverDepletedRequestRejected', 'ObserverRespawnObserved')) {
        Wait-Marker $observerLog "[OctOpus] StrikeStage=$stage" $observer 1
    }
    Write-Output "Two-client strike, cooldown, stamina rejection/regeneration, cancellation, depletion, respawn and timeout passed. Logs: $output"
} finally {
    foreach ($process in $ownedProcesses) {
        if (-not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
        $process.Dispose()
    }
}
