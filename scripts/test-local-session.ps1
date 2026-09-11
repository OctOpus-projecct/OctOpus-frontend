param(
    [string]$ServerPath = "$PSScriptRoot/../../OctOpus-backend/Builds/WindowsServer/OctOpusServer.exe",
    [string]$ClientPath = "$PSScriptRoot/../Builds/WindowsClient/OctOpus.exe",
    [switch]$ExternalServer,
    [string]$ServerAddress = '127.0.0.1',
    [switch]$Movement
)
$ErrorActionPreference = 'Stop'
$requiredFiles = @($ClientPath)
if (-not $ExternalServer) { $requiredFiles += $ServerPath }
if (-not $ExternalServer -and $ServerAddress -ne '127.0.0.1') {
    throw 'Use -ExternalServer when specifying a different server address.'
}
foreach ($file in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Build missing: $file" }
}
$output = Join-Path $PSScriptRoot ('../TestResults/session-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $output
$output = (Resolve-Path $output).Path
$ownedProcesses = [System.Collections.Generic.List[System.Diagnostics.Process]]::new()
function Start-Game([string]$file, [string]$log, [string]$extra = '') {
    $start = [Diagnostics.ProcessStartInfo]::new((Resolve-Path $file).Path)
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $start.Arguments = "-batchmode -nographics -octopus-anonymous-regression -logFile `"$log`" $extra"
    $start.Environment['OCTOPUS_SERVER_ADDRESS'] = $ServerAddress
    # The local regression run must retain its loopback server even if the caller configured WSL.
    $null = $start.Environment.Remove('OCTOPUS_BIND_ADDRESS')
    $process = [Diagnostics.Process]::Start($start)
    $ownedProcesses.Add($process)
    return $process
}
function Wait-Roster([string]$log, [int]$count, [System.Diagnostics.Process]$process) {
    $deadline = [DateTime]::UtcNow.AddSeconds(90)
    while ([DateTime]::UtcNow -lt $deadline) {
        if ($process.HasExited) { throw "Client exited ($($process.ExitCode)); see $log" }
        if (Test-Path $log) {
            $lines = @(Get-Content $log | Select-String '^\[OctOpus\] Roster=(.*)$')
            if ($lines.Count -gt 0) {
                $ids = $lines[-1].Matches[0].Groups[1].Value
                $actual = if ($ids -eq '') { 0 } else { ($ids -split ',').Count }
                if ($actual -eq $count) { return $ids }
            }
        }
        Start-Sleep -Milliseconds 250
    }
    throw "Timed out waiting for $count players; see $log"
}
try {
    if (-not $ExternalServer) {
    $server = Start-Game $ServerPath "$output/server.log"
    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    do {
        if ($server.HasExited) { throw 'Server exited before listening.' }
        $ready = (Test-Path "$output/server.log") -and (Select-String -Path "$output/server.log" -SimpleMatch '[OctOpus] Server=Started' -Quiet)
        if ($ready) { break }
        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)
    if (-not $ready) { throw "Server not listening; see $output/server.log" }
    $conflict = Start-Game $ServerPath "$output/server-conflict.log"
    if (-not $conflict.WaitForExit(15000)) { throw 'Port-conflicting server did not exit.' }
    if ($conflict.ExitCode -eq 0) { throw 'Port-conflicting server reported success.' }
    Write-Output 'Port conflict produces a nonzero exit.'
    }
    $firstArguments = '-octopus-connect'
    if ($Movement) { $firstArguments += ' -octopus-movement-test' }
    $first = Start-Game $ClientPath "$output/client1.log" $firstArguments
    $firstOwner = Wait-Roster "$output/client1.log" 1 $first
    $second = Start-Game $ClientPath "$output/client2.log" '-octopus-connect'
    $one = Wait-Roster "$output/client1.log" 2 $first
    $two = Wait-Roster "$output/client2.log" 2 $second
    if ($one -ne $two) { throw "Clients disagree on roster: $one / $two" }
    Write-Output "Two clients connected: $one"
    if ($Movement) {
        $deadline = [DateTime]::UtcNow.AddSeconds(60)
        $movementPassed = $false
        do {
            if ($first.HasExited -or $second.HasExited) { throw 'Client exited during movement.' }
            $body = Get-Content "$output/client1.log" -Raw
            if ($body.Contains('[OctOpus] MovementTest=Failed')) { throw "Movement harness failed: $output" }
            $arrival = "[OctOpus] Position=${firstOwner}:3.000,1.000,-2.000"
            $renderArrival = "[OctOpus] Render=${firstOwner}:3.000,1.000,-2.000"
            if ($body.Contains('[OctOpus] MovementTest=Passed') -and $body.Contains($arrival) -and
                $body.Contains('[OctOpus] MovementStage=InitialProgress') -and $body.Contains($renderArrival) -and
                (Select-String -LiteralPath "$output/client2.log" -SimpleMatch -Pattern $arrival -Quiet) -and
                (Select-String -LiteralPath "$output/client2.log" -SimpleMatch -Pattern $renderArrival -Quiet)) {
                $movementPassed = $true
                break
            }
            Start-Sleep -Milliseconds 250
        } while ([DateTime]::UtcNow -lt $deadline)
        if (-not $movementPassed) { throw "Movement did not converge on both clients: $output" }
        if (-not $ExternalServer) {
            $rejected = @(Select-String -LiteralPath "$output/server.log" -Pattern '^\[OctOpus\] MoveRejected=')
            if ($rejected.Count -lt 2) { throw 'Invalid destination requests did not reach server validation.' }
        }
        Write-Output 'Movement, target replacement, invalid requests and ownership checks passed; both clients agree on arrival.'
    }
    $second.Kill()
    $second.WaitForExit()
    $null = Wait-Roster "$output/client1.log" 1 $first
    Write-Output 'Disconnect propagated to remaining client.'
    $rejoined = Start-Game $ClientPath "$output/client2-rejoined.log" '-octopus-connect'
    $one = Wait-Roster "$output/client1.log" 2 $first
    $two = Wait-Roster "$output/client2-rejoined.log" 2 $rejoined
    if ($one -ne $two) { throw 'Clients disagree after reconnect.' }
    Write-Output "Reconnect passed: $one; logs: $output"
} finally {
    foreach ($process in $ownedProcesses) {
        if (-not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
        $process.Dispose()
    }
}
