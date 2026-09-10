param(
    [ValidateSet('Setup', 'Test', 'BuildWindows')]
    [string]$Task = 'BuildWindows',
    [string]$EditorPath = 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe'
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $EditorPath -PathType Leaf)) { throw "Unity Editor missing: $EditorPath" }
$repo = (Resolve-Path "$PSScriptRoot/..").Path
$project = Join-Path $repo 'Unity'
$output = Join-Path $repo 'TestResults'
$null = New-Item -ItemType Directory -Force -Path $output
$log = Join-Path $output ("unity-" + $Task + ".log")
$arguments = "-batchmode -nographics -projectPath `"$project`" -logFile `"$log`""
if ($Task -eq 'Test') {
    $report = Join-Path $output 'playmode.xml'
    if (Test-Path -LiteralPath $report) { Remove-Item -LiteralPath $report }
    $arguments += " -runTests -testPlatform PlayMode -testResults `"$report`""
} else {
    $method = if ($Task -eq 'Setup') { 'Create' } else { $Task }
    $target = if ($Task -eq 'BuildLinux') { 'Linux64' } else { 'Win64' }
    $arguments += " -quit -buildTarget $target -standaloneBuildSubtarget Player -executeMethod ProjectSetup.$method"
}
$process = Start-Process -FilePath $EditorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity $Task failed ($($process.ExitCode)). See $log" }
if ($Task -eq 'Test') {
    if (-not (Test-Path -LiteralPath $report)) { throw "Unity did not produce a test report. See $log" }
    [xml]$results = Get-Content -LiteralPath $report
    if ([int]$results.'test-run'.total -lt 1 -or [int]$results.'test-run'.failed -gt 0 -or
        $results.'test-run'.result -ne 'Passed') {
        throw "Unity tests did not pass. See $report"
    }
    Write-Output ("PlayMode passed: " + $results.'test-run'.passed)
}
Write-Output "Unity $Task succeeded. Log: $log"
