$ErrorActionPreference = 'Continue'

try {
    $raw = [Console]::In.ReadToEnd()
    if (-not $raw) { exit 0 }

    $payload = $raw | ConvertFrom-Json
    $path = $payload.tool_input.file_path
    if (-not $path) { exit 0 }

    if ($path -notmatch '\.cs$') { exit 0 }
    if ($path -notmatch '[/\\]Assets[/\\]') { exit 0 }
    if ($path -match '[/\\]Imported[/\\]') { exit 0 }
    if (-not (Test-Path -LiteralPath $path)) { exit 0 }

    $repoRoot = $env:CLAUDE_PROJECT_DIR
    if (-not $repoRoot) { $repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot) }

    Push-Location $repoRoot
    try {
        & dotnet csharpier format -- $path 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Output "csharpier: formatted $path"
        } else {
            Write-Output "csharpier: skipped $path (exit $LASTEXITCODE)"
        }
    } finally {
        Pop-Location
    }
} catch {
    Write-Output "csharpier hook error: $($_.Exception.Message)"
}

exit 0
