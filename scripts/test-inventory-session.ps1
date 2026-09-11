param(
    [string]$ServerPath = "$PSScriptRoot/../../OctOpus-backend/Builds/WindowsServer/OctOpusServer.exe",
    [string]$ClientPath = "$PSScriptRoot/../Builds/WindowsClient/OctOpus.exe",
    [ValidateSet('normal', 'overweight')][string]$Scenario = 'normal',
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
$output = Join-Path $PSScriptRoot ('../TestResults/inventory-' + $Scenario + '-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $output
$output = (Resolve-Path -LiteralPath $output).Path
$ownedProcesses = [Collections.Generic.List[Diagnostics.Process]]::new()
$logs = [Collections.Generic.List[string]]::new()
$sessionDeadline = [DateTime]::UtcNow.AddSeconds(110)
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
    $logs.Add($log)
    return $process
}
function Assert-Healthy {
    foreach ($owned in $ownedProcesses) {
        if ($owned.HasExited) { throw "Owned process exited ($($owned.ExitCode)); logs: $output" }
    }
    foreach ($log in $logs) {
        if ((Test-Path -LiteralPath $log) -and
            (Select-String -LiteralPath $log -Pattern 'InventoryTest=Failed|Exception:|Exception\b' -Quiet)) {
            throw "Inventory harness failed or raised an exception; see $log"
        }
    }
}
function Wait-Marker([string]$log, [string]$marker, [int]$seconds = 30) {
    $deadline = [DateTime]::UtcNow.AddSeconds($seconds)
    while ([DateTime]::UtcNow -lt $deadline -and [DateTime]::UtcNow -lt $sessionDeadline) {
        Assert-Healthy
        if ((Test-Path -LiteralPath $log) -and (Select-String -LiteralPath $log -SimpleMatch $marker -Quiet)) { return }
        Start-Sleep -Milliseconds 200
    }
    throw "Timed out waiting for $marker; logs: $output"
}
try {
    if (-not $ExternalServer) {
        $serverArgs = @()
        if ($Scenario -eq 'overweight') { $serverArgs += '-octopus-inventory-test-overweight' }
        $server = Start-Game $ServerPath "$output/server.log" $serverArgs
        Wait-Marker "$output/server.log" '[OctOpus] Server=Started'
    }
    $workerLog = "$output/worker.log"
    $observerLog = "$output/observer.log"
    $workerArgs = @('-octopus-connect', '-octopus-inventory-test')
    if ($Scenario -eq 'overweight') { $workerArgs += '-octopus-inventory-overweight' }
    $worker = Start-Game $ClientPath $workerLog $workerArgs
    # The fixture applies to the first connection; start the observer only after this snapshot.
    Wait-Marker $workerLog '[OctOpus] InventoryStage=InitialInventory'
    $observer = Start-Game $ClientPath $observerLog @('-octopus-connect', '-octopus-inventory-observer')
    Wait-Marker $workerLog '[OctOpus] InventoryTest=Passed' 95
    Wait-Marker $observerLog '[OctOpus] InventoryTest=Passed' 5
    foreach ($log in @($workerLog, $observerLog)) {
        foreach ($stage in @('InitialInventory', 'TwoPlayers')) {
            Wait-Marker $log "[OctOpus] InventoryStage=$stage" 1
        }
    }
    foreach ($stage in @('Working', 'FirstHit', 'PartialNoReward', 'ObserverRejectionsObserved',
        'Cancelled', 'CancellationNoReward', 'Reacquired', 'DamageReplicated', 'DepletionReplicated',
        'FullRewardOnce', 'ObserverDepletionAcknowledged', 'LateStrikesRejected', 'NoDuplicateReward',
        'MovementStarted', 'MovementSpeedVerified', 'MovementArrived', 'FinalInventoryPreserved')) {
        Wait-Marker $workerLog "[OctOpus] InventoryStage=$stage" 1
    }
    foreach ($stage in @('PartialWorkObserved', 'BusyRejected', 'NonWorkerStrikeRejected',
        'DepletionObserved', 'ObserverNoReward', 'OwnerInventoryPrivate', 'DepletedRequestRejected')) {
        Wait-Marker $observerLog "[OctOpus] InventoryStage=$stage" 1
    }
    $scenarioStages = if ($Scenario -eq 'overweight') {
        @('WeightRestricted', 'RestrictedWorkPreservedState')
    } else {
        @('NormalNextWorkAllowed', 'NextWorkCancelled')
    }
    foreach ($stage in $scenarioStages) { Wait-Marker $workerLog "[OctOpus] InventoryStage=$stage" 1 }
    $expectedInitial = if ($Scenario -eq 'overweight') { 94 } else { 0 }
    $expectedFinal = $expectedInitial + 12
    Wait-Marker $workerLog "InventorySnapshot=Initial;Wood=$expectedInitial;Weight=$($expectedInitial * 1000);Capacity=100000;" 1
    Wait-Marker $workerLog "InventorySnapshot=Reward;Wood=$expectedFinal;Weight=$($expectedFinal * 1000);Capacity=100000;" 1
    Wait-Marker $observerLog 'InventorySnapshot=Initial;Wood=0;Weight=0;Capacity=100000;' 1
    Wait-Marker $workerLog '[OctOpus] InventorySpeed=' 1
    Assert-Healthy
    Write-Output "Two-client inventory $Scenario passed: server reward, cancellation, duplicate rejection, owner privacy and movement speed. Logs: $output"
} finally {
    foreach ($process in $ownedProcesses) {
        if (-not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
        $process.Dispose()
    }
}
