param(
    [string]$ServerPath = "$PSScriptRoot/../../OctOpus-backend/Builds/WindowsServer/OctOpusServer.exe",
    [string]$ClientPath = "$PSScriptRoot/../Builds/WindowsClient/OctOpus.exe",
    [string]$Distribution = 'Ubuntu',
    [switch]$LinuxServer,
    [string]$LinuxServerPath = "$PSScriptRoot/../../OctOpus-backend/Builds/LinuxServer/OctOpusServer"
)
$ErrorActionPreference = 'Stop'
$backendScripts = Join-Path $PSScriptRoot '../../OctOpus-backend/scripts'
foreach ($file in @($ServerPath, $ClientPath, "$backendScripts/initialize-account-data.ps1", "$backendScripts/create-account.ps1")) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Required file missing: $file" }
}
if ($LinuxServer -and -not (Test-Path -LiteralPath $LinuxServerPath -PathType Leaf)) { throw 'Linux server build missing.' }
$ServerPath = (Resolve-Path -LiteralPath $ServerPath).Path
$ClientPath = (Resolve-Path -LiteralPath $ClientPath).Path
$output = Join-Path $PSScriptRoot ('../TestResults/accounts-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $output
$output = (Resolve-Path -LiteralPath $output).Path
$dataDirectory = Join-Path $output 'ServerData'
$ownedProcesses = [Collections.Generic.List[Diagnostics.Process]]::new()
$linuxServers = @{}
$ownedClients = @{}
$serverAddress = '127.0.0.1'
$firstName = 'test_' + [guid]::NewGuid().ToString('N').Substring(0, 20)
$secondName = 'test_' + [guid]::NewGuid().ToString('N').Substring(0, 20)
$firstPassword = [guid]::NewGuid().ToString('N') + [guid]::NewGuid().ToString('N')
$secondPassword = [guid]::NewGuid().ToString('N') + [guid]::NewGuid().ToString('N')

function Start-OwnedGame([string]$file, [string]$log, [string[]]$extraArguments, [hashtable]$environment) {
    $info = [Diagnostics.ProcessStartInfo]::new($file)
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    foreach ($argument in @('-batchmode', '-nographics', '-logFile', $log) + $extraArguments) { $info.ArgumentList.Add($argument) }
    foreach ($name in @('OCTOPUS_BIND_ADDRESS', 'OCTOPUS_SERVER_ADDRESS', 'OCTOPUS_DATA_DIR', 'OCTOPUS_ADMIN_LOGIN',
                       'OCTOPUS_ADMIN_PASSWORD', 'OCTOPUS_LOGIN_NAME', 'OCTOPUS_LOGIN_PASSWORD', 'OCTOPUS_AUTH_PIN', 'OCTOPUS_EXPECTED_WOOD', 'OCTOPUS_TEST_STOP_FILE')) {
        $null = $info.Environment.Remove($name)
    }
    foreach ($name in $environment.Keys) { $info.Environment[$name] = [string]$environment[$name] }
    try {
        $process = [Diagnostics.Process]::Start($info)
        $ownedProcesses.Add($process)
        return $process
    } finally {
        $null = $info.Environment.Remove('OCTOPUS_LOGIN_PASSWORD')
    }
}
function Stop-OwnedGame([Diagnostics.Process]$process) {
    if ($null -eq $process) { return }
    if ($ownedClients.ContainsKey($process)) {
        $client = $ownedClients[$process]
        if ($client.Stopped) { return }
        if ($process.HasExited) { throw "Account client exited without confirmed disconnect; see $($client.Log)" }
        $disconnected = $false
        try {
            [IO.File]::WriteAllText($client.StopFile, 'stop')
            $deadline = [DateTime]::UtcNow.AddSeconds(15)
            while ([DateTime]::UtcNow -lt $deadline -and -not $process.HasExited) {
                $body = Read-TestLog $client.Log
                $states = [regex]::Matches($body, '(?m)^\[OctOpus\] Client=(\w+)')
                if ($body.Contains('[OctOpus] AccountStage=GracefulDisconnected') -and
                    $states.Count -gt 0 -and $states[$states.Count - 1].Groups[1].Value -eq 'Stopped') {
                    $disconnected = $true
                    break
                }
                Start-Sleep -Milliseconds 100
            }
        } finally {
            # Always clean up this owned process even when graceful shutdown fails.
            if (-not $process.HasExited) {
                $process.Kill()
                if (-not $process.WaitForExit(15000)) { throw 'An owned account client did not terminate.' }
            }
        }
        if (-not $disconnected) { throw "Client graceful disconnect was not confirmed within 15 seconds; see $($client.Log)" }
        $client.Stopped = $true
        return
    }
    if ($linuxServers.ContainsKey($process)) {
        # The supervisor signals only its own Popen child, never a PID/name search result.
        [IO.File]::WriteAllText($linuxServers[$process].StopFile, 'stop')
        if (-not $process.HasExited -and -not $process.WaitForExit(20000)) {
            throw 'The owned Linux server supervisor did not acknowledge shutdown.'
        }
        return
    }
    if (-not $process.HasExited) {
        $process.Kill()
        if (-not $process.WaitForExit(15000)) { throw 'An owned test process did not terminate.' }
    }
}
function ConvertTo-LinuxPath([string]$windowsPath) {
    $portablePath = $windowsPath.Replace('\', '/')
    $converted = & wsl.exe -d $Distribution -- wslpath -a $portablePath
    if ($LASTEXITCODE -ne 0) { throw 'Could not resolve a test path in WSL.' }
    return ($converted -join "`n").Trim()
}
function Start-OwnedLinuxServer([string]$log) {
    $stopFile = $log + '.stop'
    $pidFile = $log + '.pid'
    $info = [Diagnostics.ProcessStartInfo]::new('wsl.exe')
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $supervisor = @'
import os, pathlib, subprocess, sys, time
executable, data, address, log, stop_file, pid_file = sys.argv[1:]
env = os.environ.copy()
for key in ('OCTOPUS_ADMIN_LOGIN', 'OCTOPUS_ADMIN_PASSWORD', 'OCTOPUS_LOGIN_NAME', 'OCTOPUS_LOGIN_PASSWORD', 'OCTOPUS_AUTH_PIN'):
    env.pop(key, None)
env['OCTOPUS_DATA_DIR'] = data
env['OCTOPUS_BIND_ADDRESS'] = address
with open(log + '.stdout', 'wb') as output:
    child = subprocess.Popen([executable, '-batchmode', '-nographics', '-logFile', log], cwd=str(pathlib.Path(executable).parent), env=env, stdout=output, stderr=subprocess.STDOUT)
    try:
        pathlib.Path(pid_file).write_text(str(child.pid), encoding='ascii')
        deadline = time.monotonic() + 1200
        while child.poll() is None and not pathlib.Path(stop_file).exists() and time.monotonic() < deadline:
            time.sleep(0.1)
    finally:
        if child.poll() is None:
            child.terminate()
            try:
                child.wait(timeout=5)
            except subprocess.TimeoutExpired:
                child.kill()
                child.wait()
'@
    $linuxExecutable = ConvertTo-LinuxPath (Resolve-Path -LiteralPath $LinuxServerPath).Path
    $linuxData = ConvertTo-LinuxPath $dataDirectory
    $linuxLog = ConvertTo-LinuxPath $log
    $linuxStop = ConvertTo-LinuxPath $stopFile
    $linuxPid = ConvertTo-LinuxPath $pidFile
    foreach ($argument in @('-d', $Distribution, '--', 'python3', '-c', $supervisor, $linuxExecutable, $linuxData, $serverAddress, $linuxLog, $linuxStop, $linuxPid)) {
        $info.ArgumentList.Add($argument)
    }
    $process = [Diagnostics.Process]::Start($info)
    $ownedProcesses.Add($process)
    $linuxServers[$process] = @{ StopFile = $stopFile; PidFile = $pidFile }
    return $process
}
function Read-TestLog([string]$log) {
    if (Test-Path -LiteralPath $log) {
        $stream = [IO.File]::Open($log, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
        $reader = [IO.StreamReader]::new($stream)
        try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
    }
    return ''
}
function Wait-AccountTest([Diagnostics.Process]$process, [string]$log) {
    $deadline = [DateTime]::UtcNow.AddSeconds(150)
    while ([DateTime]::UtcNow -lt $deadline) {
        $body = Read-TestLog $log
        if ($body.Contains('[OctOpus] AccountTest=Failed')) { throw "Account harness failed; see $log" }
        if ($process.HasExited) { throw "Account client exited before completion; see $log" }
        if ($body.Contains('[OctOpus] AccountTest=Passed')) { return }
        Start-Sleep -Milliseconds 250
    }
    throw "Account harness timed out; see $log"
}
function Start-TestServer([string]$label) {
    $log = Join-Path $output "$label.log"
    $process = if ($LinuxServer) { Start-OwnedLinuxServer $log } else {
        Start-OwnedGame $ServerPath $log @() @{ OCTOPUS_BIND_ADDRESS = $serverAddress; OCTOPUS_DATA_DIR = $dataDirectory }
    }
    $deadline = [DateTime]::UtcNow.AddSeconds(45)
    while ([DateTime]::UtcNow -lt $deadline) {
        if ($process.HasExited) { throw "Account server exited before listening; see $log" }
        $body = Read-TestLog $log
        if ($body.Contains('[OctOpus] Accounts=Enabled') -and $body.Contains('[OctOpus] Server=Started')) { return $process }
        Start-Sleep -Milliseconds 250
    }
    throw "Account server did not start; see $log"
}
function Start-TestClient([string]$label, [string]$case, [string]$login, [string]$password, [string]$pin, [int]$wood) {
    $log = Join-Path $output "$label.log"
    $stopFile = $log + '.stop'
    $environment = @{ OCTOPUS_SERVER_ADDRESS = $serverAddress; OCTOPUS_LOGIN_NAME = $login; OCTOPUS_LOGIN_PASSWORD = $password;
                      OCTOPUS_AUTH_PIN = $pin; OCTOPUS_EXPECTED_WOOD = $wood; OCTOPUS_TEST_STOP_FILE = $stopFile }
    $process = Start-OwnedGame $ClientPath $log @('-octopus-connect', '-octopus-account-test', '-octopus-account-case', $case) $environment
    $ownedClients[$process] = @{ StopFile = $stopFile; Log = $log }
    $environment.Clear()
    Wait-AccountTest $process $log
    return $process
}
function Assert-Connected([Diagnostics.Process]$process, [string]$log) {
    if ($process.HasExited) { throw "Original account client exited; see $log" }
    $body = Read-TestLog $log
    $states = [regex]::Matches($body, '(?m)^\[OctOpus\] Client=(\w+)')
    if ($body.Contains('[OctOpus] AccountTest=Failed') -or $states.Count -eq 0 -or $states[$states.Count - 1].Groups[1].Value -ne 'Started') {
        throw "Original account session was not retained; see $log"
    }
}
function Assert-SavedWood([string]$login, [int]$expected) {
    $snapshot = Get-Content -LiteralPath (Join-Path $dataDirectory 'accounts.json') -Raw | ConvertFrom-Json
    $matches = @($snapshot.Accounts | Where-Object LoginName -EQ $login)
    if ($matches.Count -ne 1 -or $matches[0].WoodCount -ne $expected) { throw 'Persistent wood count did not match the expected test result.' }
}

try {
    if ($LinuxServer) {
        $addresses = & wsl.exe -d $Distribution -- hostname -I
        if ($LASTEXITCODE -ne 0) { throw 'Could not determine the WSL server address.' }
        $serverAddress = (($addresses -join ' ').Trim() -split '\s+' | Where-Object {
            $address = $null
            [Net.IPAddress]::TryParse($_, [ref]$address) -and $address.AddressFamily -eq [Net.Sockets.AddressFamily]::InterNetwork -and -not [Net.IPAddress]::IsLoopback($address)
        } | Select-Object -First 1)
        if ([string]::IsNullOrWhiteSpace($serverAddress)) { throw 'WSL has no usable IPv4 address.' }
    }
    # TLS identity and credentials belong only to this ignored, isolated test directory.
    & "$backendScripts/initialize-account-data.ps1" -DataDirectory $dataDirectory -Distribution $Distribution -ClientDirectory '' | Out-Null
    $pin = [IO.File]::ReadAllText((Join-Path $dataDirectory 'server-pin.txt')).Trim()
    if ($pin -notmatch '^[a-fA-F0-9]{64}$') { throw 'Generated certificate fingerprint is invalid.' }
    $secureFirst = ConvertTo-SecureString $firstPassword -AsPlainText -Force
    $secureSecond = ConvertTo-SecureString $secondPassword -AsPlainText -Force
    try {
        & "$backendScripts/create-account.ps1" -DataDirectory $dataDirectory -ServerPath $ServerPath -LoginName $firstName -Password $secureFirst | Out-Null
        & "$backendScripts/create-account.ps1" -DataDirectory $dataDirectory -ServerPath $ServerPath -LoginName $secondName -Password $secureSecond | Out-Null
    } finally { $secureFirst.Dispose(); $secureSecond.Dispose() }

    $server = Start-TestServer 'server-initial'
    $earned = Start-TestClient 'client-earn' 'earn' $firstName $firstPassword $pin 0
    Assert-SavedWood $firstName 12
    Stop-OwnedGame $earned
    Stop-OwnedGame $server
    Write-Output '신규 계정 목재 0 → 벌목 12 → 동일 클라이언트 재접속 12 검증 통과.'

    $server = Start-TestServer 'server-restarted'
    $restored = Start-TestClient 'client-restored' 'restore' $firstName $firstPassword $pin 12
    $duplicate = Start-TestClient 'client-duplicate' 'reject' $firstName $firstPassword $pin 12
    Stop-OwnedGame $duplicate
    Start-Sleep -Seconds 2
    Assert-Connected $restored (Join-Path $output 'client-restored.log')
    Write-Output '서버 재시작 후 목재 12 복원, 중복 로그인 거절 및 기존 연결 유지 검증 통과.'

    $other = Start-TestClient 'client-other-account' 'restore' $secondName $secondPassword $pin 0
    Assert-SavedWood $secondName 0
    Stop-OwnedGame $other
    # Allow the server to observe the second account disconnect before negative credential cases.
    Start-Sleep -Seconds 2
    $wrongPassword = [guid]::NewGuid().ToString('N') + [guid]::NewGuid().ToString('N')
    $wrong = Start-TestClient 'client-wrong-password' 'reject' $secondName $wrongPassword $pin 0
    Stop-OwnedGame $wrong
    $wrongPin = if ($pin[0] -eq '0') { '1' + $pin.Substring(1) } else { '0' + $pin.Substring(1) }
    $untrusted = Start-TestClient 'client-wrong-pin' 'reject' $secondName $secondPassword $wrongPin 0
    Stop-OwnedGame $untrusted
    # A subsequent successful login rules out a dead endpoint masquerading as credential rejection.
    $recovered = Start-TestClient 'client-after-rejections' 'restore' $secondName $secondPassword $pin 0
    Stop-OwnedGame $recovered
    Assert-Connected $restored (Join-Path $output 'client-restored.log')
    Assert-SavedWood $firstName 12
    Assert-SavedWood $secondName 0
    Write-Output '다른 계정 목재 0 격리, 잘못된 비밀번호·인증서 지문 거절 검증 통과.'
} finally {
    $cleanupFailures = 0
    foreach ($process in $ownedProcesses) {
        try { Stop-OwnedGame $process }
        catch { $cleanupFailures++; Write-Warning 'An owned account test process could not be stopped; inspect the test run.' }
        finally { $process.Dispose() }
    }
    $firstPassword = $null
    $secondPassword = $null
    $wrongPassword = $null
    if ($cleanupFailures -gt 0) { throw "Account test cleanup failed for $cleanupFailures owned processes; logs: $output" }
}
Write-Output "Account session passed. Logs: $output"
