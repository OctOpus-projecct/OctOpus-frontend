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
$output = Join-Path $PSScriptRoot ('../TestResults/map-session-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $output
$output = (Resolve-Path -LiteralPath $output).Path
$ownedProcesses = [Collections.Generic.List[Diagnostics.Process]]::new()
$clientLogs = [Collections.Generic.List[string]]::new()
$sessionDeadline = [DateTime]::UtcNow.AddSeconds(160)
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
                (Select-String -LiteralPath $clientLog -Pattern 'MapTest=Failed|Exception:|Exception\b' -Quiet)) {
                throw "Map harness failed or raised an exception; see $clientLog"
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
    $leadLog = "$output/lead.log"
    $partnerLog = "$output/partner.log"
    $clientLogs.Add($leadLog)
    $lead = Start-Game $ClientPath $leadLog @('-octopus-connect', '-octopus-map-test')
    Wait-Marker $leadLog '[OctOpus] MapStage=LeadWorking' $lead 60
    $clientLogs.Add($partnerLog)
    $partner = Start-Game $ClientPath $partnerLog @('-octopus-connect', '-octopus-map-partner')
    Wait-Marker $leadLog '[OctOpus] MapTest=Passed' $lead 95
    Wait-Marker $partnerLog '[OctOpus] MapTest=Passed' $partner 10
    foreach ($log in @($leadLog, $partnerLog)) {
        foreach ($stage in @('LayoutAndSpawn', 'IndependentDamageObserved',
            'IndependentCancellationObserved', 'IndependentDepletionObserved',
            'LeadProgressRestored', 'IndependentRespawnObserved', 'AllTreeIdentitiesPreserved')) {
            Wait-Marker $log "[OctOpus] MapStage=$stage" $lead 1
        }
    }
    foreach ($stage in @('BoundaryInteriorReached', 'BoundaryOutsideRejected', 'LeadWorking',
        'PartnerWorkingObserved', 'LeadReacquired')) {
        Wait-Marker $leadLog "[OctOpus] MapStage=$stage" $lead 1
    }
    foreach ($stage in @('LeadWorkingObserved', 'PartnerWorking', 'DepletionReplicated')) {
        Wait-Marker $partnerLog "[OctOpus] MapStage=$stage" $partner 1
    }
    $leadLayout = @(Select-String -LiteralPath $leadLog -Pattern '^\[OctOpus\] MapLayout=(.+)$')
    $partnerLayout = @(Select-String -LiteralPath $partnerLog -Pattern '^\[OctOpus\] MapLayout=(.+)$')
    if ($leadLayout.Count -ne 1 -or $partnerLayout.Count -ne 1 -or
        $leadLayout[0].Matches[0].Groups[1].Value -cne $partnerLayout[0].Matches[0].Groups[1].Value) {
        throw "Client tree identities or positions differ; logs: $output"
    }
    Write-Output "Two-client map layout, spawn, movement boundary, independent tree work/cancellation/depletion and respawn identity passed. Logs: $output"
} finally {
    foreach ($process in $ownedProcesses) {
        if (-not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
        $process.Dispose()
    }
}
