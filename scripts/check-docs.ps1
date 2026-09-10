param([string]$RepoRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $RepoRoot).Path
$required = @('AGENTS.md', 'docs/agent-routing.md', 'docs/tasks/issues.md',
    'docs/tasks/git-workflow.md', 'docs/tasks/review.md',
    '.github/ISSUE_TEMPLATE/task.md', '.github/ISSUE_TEMPLATE/bug.md',
    '.github/pull_request_template.md')
foreach ($relative in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $root $relative) -PathType Leaf)) {
        throw "Missing required file: $relative"
    }
}
$files = @(Get-ChildItem -LiteralPath $root -File -Filter '*.md')
foreach ($directory in @('docs', '.github')) {
    $path = Join-Path $root $directory
    if (Test-Path -LiteralPath $path) {
        $files += Get-ChildItem -LiteralPath $path -Recurse -File -Filter '*.md'
    }
}
$checked = 0
$skipped = 0
$errors = @()
foreach ($file in $files) {
    $body = [IO.File]::ReadAllText($file.FullName)
    # Inline file links only. Code examples, remote URLs and heading anchors are not checked.
    $body = [regex]::Replace($body, '(?ms)^\x60{3}[^\r\n]*\r?\n.*?^\x60{3}[^\r\n]*$', '')
    $body = [regex]::Replace($body, '\x60[^\x60\r\n]*\x60', '')
    foreach ($match in [regex]::Matches($body, '\]\(([^)\r\n]+)\)')) {
        $target = $match.Groups[1].Value.Trim()
        if ($target -match '^(https?://|mailto:|#)') { continue }
        $target = ($target -split '#', 2)[0].Trim('<', '>')
        $target = [Uri]::UnescapeDataString($target)
        $resolved = [IO.Path]::GetFullPath((Join-Path $file.DirectoryName $target))
        $inside = $resolved.StartsWith($root + [IO.Path]::DirectorySeparatorChar,
            [StringComparison]::OrdinalIgnoreCase)
        if (-not $inside) {
            # Cross-repository references are optional in a single-repository CI checkout.
            $parent = Split-Path -Parent $root
            $sibling = $null
            foreach ($repo in @('OctOpus-frontend', 'OctOpus-backend')) {
                $candidate = Join-Path $parent $repo
                if ($resolved.StartsWith($candidate + [IO.Path]::DirectorySeparatorChar,
                    [StringComparison]::OrdinalIgnoreCase)) { $sibling = $candidate }
            }
            if (-not $sibling) { $errors += "Link outside known repositories: $($file.FullName): $target"; continue }
            if (-not (Test-Path -LiteralPath $sibling)) {
                $skipped++
                Write-Output "SKIP sibling checkout unavailable: $target"
                continue
            }
        }
        $checked++
        if (-not (Test-Path -LiteralPath $resolved)) { $errors += "Broken link: $($file.FullName): $target" }
    }
}
if ($errors.Count) { throw ($errors -join [Environment]::NewLine) }
"Documents: $($files.Count); file links checked: $checked; sibling links skipped: $skipped"
