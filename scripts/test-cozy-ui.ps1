param([int]$Width=1280,[int]$Height=720,[switch]$ShowWindow)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path "$PSScriptRoot/..").Path
$output=Join-Path $repo ('TestResults/cozy-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output | Out-Null
$server=$null;$client=$null
try {
    $serverInfo=[Diagnostics.ProcessStartInfo]::new("$repo/../OctOpus-backend/Builds/WindowsServer/OctOpusServer.exe")
    $serverInfo.UseShellExecute=$false;$serverInfo.CreateNoWindow=$true;$serverInfo.WindowStyle='Hidden'
    $serverInfo.Arguments="-batchmode -nographics -octopus-anonymous-regression -logFile `"$output/server.log`""
    $serverInfo.Environment['OCTOPUS_BIND_ADDRESS']='127.0.0.1'
    $server=[Diagnostics.Process]::Start($serverInfo)
    Start-Sleep -Seconds 2
    $info=[Diagnostics.ProcessStartInfo]::new("$repo/Builds/WindowsClient/OctOpus.exe")
    $info.UseShellExecute=$false;$info.WindowStyle=if($ShowWindow){'Normal'}else{'Hidden'}
    $info.Arguments="-force-d3d11 -screen-fullscreen 0 -screen-width $Width -screen-height $Height -octopus-anonymous-regression -logFile `"$output/client.log`""
    $info.Environment['OCTOPUS_UI_CAPTURE']=$output
    $info.Environment['OCTOPUS_SERVER_ADDRESS']='127.0.0.1'
    $client=[Diagnostics.Process]::Start($info)
    $deadline=[DateTime]::UtcNow.AddSeconds(70)
    do {
        Start-Sleep -Milliseconds 500
        if($client.HasExited -or $server.HasExited){throw 'Preview process exited unexpectedly.'}
        $done=(Test-Path "$output/settings.png") -and (Select-String -LiteralPath "$output/client.log" -Pattern 'UiPreview=Passed' -SimpleMatch -Quiet)
    } while(-not $done -and [DateTime]::UtcNow -lt $deadline)
    if(-not $done){throw "UI capture failed: $output"}
    foreach($name in @('login','hud','bag-tree','settings')){if(-not(Test-Path "$output/$name.png")){throw "Missing $name screenshot"}}
    if(Select-String -LiteralPath "$output/client.log" -Pattern 'Exception:|UiPreview=Failed' -Quiet){throw "UI errors: $output"}
    Write-Output "UI capture and pointer blocking passed: $output"
} finally {
    foreach($process in @($client,$server)){
        if($null -ne $process){if(-not $process.HasExited){$process.Kill();$process.WaitForExit()};$process.Dispose()}
    }
}
