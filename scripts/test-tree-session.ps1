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
$output = Join-Path $PSScriptRoot ('../TestResults/tree-session-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $output
$output = (Resolve-Path -LiteralPath $output).Path
$ownedProcesses = [Collections.Generic.List[Diagnostics.Process]]::new()
$clientLogs = [Collections.Generic.List[string]]::new()
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
    while ([DateTime]::UtcNow -lt $deadline) {
        if ($process.HasExited) { throw "Process exited ($($process.ExitCode)); see $log" }
        foreach ($clientLog in $clientLogs) {
            if ((Test-Path -LiteralPath $clientLog) -and
                (Select-String -LiteralPath $clientLog -SimpleMatch '[OctOpus] TreeTest=Failed' -Quiet)) {
                throw "Tree harness failed; see $clientLog"
            }
        }
        if ((Test-Path -LiteralPath $log) -and (Select-String -LiteralPath $log -SimpleMatch $marker -Quiet)) { return }
        Start-Sleep -Milliseconds 200
    }
    throw "Timed out waiting for $marker; logs: $output"
}
try {
    if (-not $ExternalServer) {
        $server = Start-Game $ServerPath "$output/server.log"
        Wait-Marker "$output/server.log" '[OctOpus] Server=Started' $server
    }
    $workerLog = "$output/worker.log"
    $contenderLog = "$output/contender.log"
    $observerLog = "$output/late-join.log"
    $disconnectMarker = "$output/disconnect.signal"
    $clientLogs.Add($workerLog)
    $worker = Start-Game $ClientPath $workerLog @('-octopus-connect', '-octopus-tree-test',
        '-octopus-tree-disconnect-marker', $disconnectMarker)
    Wait-Marker $workerLog '[OctOpus] TreeStage=Working' $worker
    $clientLogs.Add($contenderLog)
    $contender = Start-Game $ClientPath $contenderLog @('-octopus-connect', '-octopus-tree-contender')
    Wait-Marker $contenderLog '[OctOpus] TreeStage=Busy' $contender
    Wait-Marker $workerLog '[OctOpus] TreeStage=ReadyForDisconnect' $worker 75
    foreach ($stage in @('ApproachingUnclaimed', 'ArrivedInRange', 'ContenderBusyObserved', 'RepeatedRequestPreservedDeadline',
        'TimedOut', 'Restarted', 'MoveCancelled', 'CancelMoveArrived')) {
        Wait-Marker $workerLog "[OctOpus] TreeStage=$stage" $worker 1
    }
    $disconnectStart = [DateTime]::UtcNow
    Set-Content -LiteralPath $disconnectMarker -Value 'disconnect'
    Wait-Marker $workerLog '[OctOpus] TreeStage=Disconnected' $worker 10
    Wait-Marker $contenderLog '[OctOpus] TreeStage=Reclaimed' $contender 20
    if (([DateTime]::UtcNow - $disconnectStart).TotalSeconds -ge 25) {
        throw 'Disconnect release was too slow to distinguish it from the 30-second timeout.'
    }
    Wait-Marker $contenderLog '[OctOpus] TreeTest=Passed' $contender 1
    $worker.Kill()
    $worker.WaitForExit()
    $clientLogs.Add($observerLog)
    $observer = Start-Game $ClientPath $observerLog @('-octopus-connect', '-octopus-tree-observer')
    Wait-Marker $observerLog '[OctOpus] TreeStage=LateJoinObserved' $observer 20
    Wait-Marker $observerLog '[OctOpus] TreeStage=SingleTreeObserved' $observer 3
    Wait-Marker $observerLog '[OctOpus] TreeTest=Passed' $observer 2
    Write-Output "Tree approach, contention, deadline, timeout, move cancellation, disconnect release and late join passed. Logs: $output"
} finally {
    foreach ($process in $ownedProcesses) {
        if (-not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
        $process.Dispose()
    }
}
