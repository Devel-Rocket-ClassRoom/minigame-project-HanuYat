# PreToolUse guard. Replicates .claude/settings.json deny rules for Codex (no native glob deny-list).
# Denies: direct edits to .unity/.meta/.csproj/.sln/.slnx, destructive recursive deletes, shell writes into .unity/.meta.
# Fail-open: any hook error exits 0 (never brick the agent).

$ErrorActionPreference = 'Continue'

function Deny([string]$reason) {
    $obj = @{
        hookSpecificOutput = @{
            hookEventName            = 'PreToolUse'
            permissionDecision       = 'deny'
            permissionDecisionReason = $reason
        }
    }
    Write-Output ($obj | ConvertTo-Json -Compress -Depth 5)
    exit 0
}

try {
    $raw = [Console]::In.ReadToEnd()
    if (-not $raw) { exit 0 }

    $p = $raw | ConvertFrom-Json
    $tool = "$($p.tool_name)"

    # --- Bash: block destructive deletes + shell writes into Unity binary/meta ---
    if ($tool -match '(?i)bash') {
        $cmd = "$($p.tool_input.command)"
        if (-not $cmd) { exit 0 }

        # rm with recursive + force (any flag order). Neutralize `git rm` first (git op, not a filesystem nuke).
        $c = $cmd -replace '(?i)\bgit\s+rm\b', 'git_rm'
        $rmHit = ($c -match '(?i)(^|[\s;&|(])rm\s+-[a-z]*r[a-z]*f') -or
                 ($c -match '(?i)(^|[\s;&|(])rm\s+-[a-z]*f[a-z]*r') -or
                 (($c -match '(?i)(^|[\s;&|(])rm\s+.*--recursive') -and ($c -match '(?i)--force'))
        if ($rmHit) { Deny "Recursive force delete (rm -rf) blocked by guard-edits hook." }

        # Remove-Item -Recurse -Force
        if (($cmd -match '(?i)Remove-Item') -and ($cmd -match '(?i)-Recurse') -and ($cmd -match '(?i)-Force')) {
            Deny "Remove-Item -Recurse -Force blocked by guard-edits hook."
        }

        # redirect/write into a .unity or .meta file via shell
        if ($cmd -match '(?i)>\s*["'']?[^"''\s]+\.(unity|meta)\b') {
            Deny "Writing to .unity/.meta via shell blocked. Edit inside Unity Editor only."
        }
        exit 0
    }

    # --- Edit/Write (apply_patch): block protected target files ---
    $path = "$($p.tool_input.file_path)"
    if (-not $path) { $path = "$($p.tool_input.path)" }
    if ($path -match '(?i)\.(unity|meta|csproj|sln|slnx)$') {
        Deny "Direct edit of '$path' blocked. .unity/.meta = Unity Editor only; .csproj/.sln/.slnx = Unity-generated."
    }

    # fallback: scan whole tool_input for a .unity/.meta target (covers apply_patch field-name variance)
    $blob = ($p.tool_input | ConvertTo-Json -Depth 8 -Compress)
    if ($blob -match '(?i)"[^"]*\.(unity|meta)"') {
        Deny "Edit touches a .unity/.meta path. Unity Editor only."
    }

    exit 0
} catch {
    exit 0
}
