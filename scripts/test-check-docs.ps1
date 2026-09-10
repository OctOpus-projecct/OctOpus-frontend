$ErrorActionPreference = 'Stop'
$checker = Join-Path $PSScriptRoot 'check-docs.ps1'
if (-not (Test-Path -LiteralPath $checker)) { throw 'Document checker is missing.' }
$container = Join-Path ([IO.Path]::GetTempPath()) ('octopus-docs-' + [guid]::NewGuid())
$fixture = Join-Path $container 'OctOpus-frontend'
$sibling = Join-Path $container 'OctOpus-backend'
$required = @('AGENTS.md', 'docs/agent-routing.md', 'docs/tasks/issues.md',
    'docs/tasks/git-workflow.md', 'docs/tasks/review.md',
    '.github/ISSUE_TEMPLATE/task.md', '.github/ISSUE_TEMPLATE/bug.md',
    '.github/pull_request_template.md')
$created = @()
$passed = 0
try {
    foreach ($relative in $required) {
        $file = Join-Path $fixture $relative
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($file)) | Out-Null
        [IO.File]::WriteAllText($file, "# Fixture")
        $created += $file
    }
    $entry = Join-Path $fixture 'AGENTS.md'
    [IO.File]::WriteAllText($entry, '[Route](docs/agent-routing.md)')
    & $checker -RepoRoot $fixture | Out-Null
    $passed++
    [IO.File]::WriteAllText($entry, '[Broken](docs/missing.md)')
    $caught = $false
    try { & $checker -RepoRoot $fixture | Out-Null } catch {
        if ($_.Exception.Message -notmatch 'Broken link') { throw }
        $caught = $true
    }
    if (-not $caught) { throw 'Broken link was accepted.' }
    $passed++
    [IO.File]::WriteAllText($entry, '# Fixture')
    Remove-Item -LiteralPath (Join-Path $fixture 'docs/agent-routing.md')
    $caught = $false
    try { & $checker -RepoRoot $fixture | Out-Null } catch {
        if ($_.Exception.Message -notmatch 'Missing required file') { throw }
        $caught = $true
    }
    if (-not $caught) { throw 'Missing routing file was accepted.' }
    $passed++
    [IO.File]::WriteAllText((Join-Path $fixture 'docs/agent-routing.md'), '# Fixture')
    [IO.File]::WriteAllText($entry, '[Sibling](../OctOpus-backend/AGENTS.md)')
    $result = & $checker -RepoRoot $fixture
    if (($result -join ' ') -notmatch 'sibling links skipped: 1') { throw 'Missing sibling was not explicitly skipped.' }
    $passed++
    [IO.Directory]::CreateDirectory($sibling) | Out-Null
    $caught = $false
    try { & $checker -RepoRoot $fixture | Out-Null } catch {
        if ($_.Exception.Message -notmatch 'Broken link') { throw }
        $caught = $true
    }
    if (-not $caught) { throw 'Broken link in existing sibling was accepted.' }
    $passed++
    $siblingFile = Join-Path $sibling 'AGENTS.md'
    [IO.File]::WriteAllText($siblingFile, '# Sibling')
    $created += $siblingFile
    $result = & $checker -RepoRoot $fixture
    if (($result -join ' ') -notmatch 'file links checked: 1; sibling links skipped: 0') { throw 'Existing sibling link was not checked.' }
    $passed++
    "Document checker tests passed: $passed"
} finally {
    # Delete only the files and empty directories explicitly created by this test.
    foreach ($file in $created) {
        if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file }
    }
    foreach ($relative in @('docs/tasks', 'docs', '.github/ISSUE_TEMPLATE', '.github', '')) {
        $directory = if ($relative) { Join-Path $fixture $relative } else { $fixture }
        if (Test-Path -LiteralPath $directory) { Remove-Item -LiteralPath $directory }
    }
    if (Test-Path -LiteralPath $sibling) { Remove-Item -LiteralPath $sibling }
    if (Test-Path -LiteralPath $container) { Remove-Item -LiteralPath $container }
}
