param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\\..")).Path
)

$ErrorActionPreference = "Stop"

$sourceRoot = Join-Path $RepoRoot ".agent/rules"
$codexBridgeRoot = Join-Path $RepoRoot ".agents/rules"
$claudeBridgeRoot = Join-Path $RepoRoot ".claude/rules"
$cursorBridgeRoot = Join-Path $RepoRoot ".cursor/rules"
$githubBridgeRoot = Join-Path $RepoRoot ".github/rules"

function Get-RuleData {
    param([string]$RulePath)

    $raw = Get-Content $RulePath -Raw
    $fmMatch = [regex]::Match($raw, "(?ms)^---\r?\n(.*?)\r?\n---")
    if (-not $fmMatch.Success) {
        throw "No YAML frontmatter in $RulePath"
    }

    $frontmatter = $fmMatch.Groups[1].Value
    $body = $raw.Substring($fmMatch.Index + $fmMatch.Length).Trim()
    $trigger = [regex]::Match($frontmatter, "(?m)^trigger:\s*(.+)$").Groups[1].Value.Trim()
    $sourceRule = [regex]::Match(
        $frontmatter,
        "(?m)^\s*source_rule:\s*`"?([^`"\r\n]+)`"?\s*$"
    ).Groups[1].Value.Trim()

    return @{
        raw = $raw
        body = $body
        trigger = $trigger
        source_rule = $sourceRule
    }
}

if (-not (Test-Path $sourceRoot)) {
    throw "Source rules path not found: $sourceRoot"
}
if (-not (Test-Path $codexBridgeRoot)) {
    throw "Codex bridge rules path not found: $codexBridgeRoot"
}
if (-not (Test-Path $claudeBridgeRoot)) {
    throw "Claude bridge rules path not found: $claudeBridgeRoot"
}
if (-not (Test-Path $cursorBridgeRoot)) {
    throw "Cursor bridge rules path not found: $cursorBridgeRoot"
}
if (-not (Test-Path $githubBridgeRoot)) {
    throw "GitHub Copilot bridge rules path not found: $githubBridgeRoot"
}

$errors = New-Object System.Collections.Generic.List[string]

$sourceRules = Get-ChildItem $sourceRoot -File -Filter "*.md" |
    Where-Object { $_.Name -ne "README.md" } |
    Sort-Object Name
$codexBridgeRules = Get-ChildItem $codexBridgeRoot -File -Filter "*.md" |
    Where-Object { $_.Name -ne "AUTHORING_POLICY.md" } |
    Sort-Object Name
$claudeBridgeRules = Get-ChildItem $claudeBridgeRoot -File -Filter "*.md" |
    Sort-Object Name
$cursorBridgeRules = Get-ChildItem $cursorBridgeRoot -File -Filter "*.md" |
    Sort-Object Name
$githubBridgeRules = Get-ChildItem $githubBridgeRoot -File -Filter "*.md" |
    Sort-Object Name

$sourceNames = @($sourceRules.Name)
$codexBridgeNames = @($codexBridgeRules.Name)
$claudeBridgeNames = @($claudeBridgeRules.Name)
$cursorBridgeNames = @($cursorBridgeRules.Name)
$githubBridgeNames = @($githubBridgeRules.Name)

foreach ($source in $sourceRules) {
    $name = $source.Name
    $sourceRuleMd = $source.FullName
    $codexBridgeRuleMd = Join-Path $codexBridgeRoot $name
    $claudeBridgeRuleMd = Join-Path $claudeBridgeRoot $name
    $cursorBridgeRuleMd = Join-Path $cursorBridgeRoot $name
    $githubBridgeRuleMd = Join-Path $githubBridgeRoot $name

    if (-not (Test-Path $codexBridgeRuleMd)) {
        $errors.Add("Missing Codex bridge rule for '$name'.")
    }
    if (-not (Test-Path $claudeBridgeRuleMd)) {
        $errors.Add("Missing Claude bridge rule for '$name'.")
    }
    if (-not (Test-Path $cursorBridgeRuleMd)) {
        $errors.Add("Missing Cursor bridge rule for '$name'.")
    }
    if (-not (Test-Path $githubBridgeRuleMd)) {
        $errors.Add("Missing GitHub Copilot bridge rule for '$name'.")
    }
    if ((-not (Test-Path $codexBridgeRuleMd)) -or (-not (Test-Path $claudeBridgeRuleMd)) -or (-not (Test-Path $cursorBridgeRuleMd)) -or (-not (Test-Path $githubBridgeRuleMd))) {
        continue
    }

    $sourceData = Get-RuleData -RulePath $sourceRuleMd
    $codexData = Get-RuleData -RulePath $codexBridgeRuleMd
    $claudeData = Get-RuleData -RulePath $claudeBridgeRuleMd
    $cursorData = Get-RuleData -RulePath $cursorBridgeRuleMd
    $githubData = Get-RuleData -RulePath $githubBridgeRuleMd

    $expectedSourceRule = "../../../.agent/rules/$name"
    $expectedCodexBridgeRef = "../../../.agents/rules/$name"
    $expectedClaudeBridgeRef = "../../../.claude/rules/$name"

    if ($codexData.trigger -ne $sourceData.trigger) {
        $errors.Add("Codex bridge trigger mismatch for '$name'.")
    }
    if ($codexData.source_rule -ne $expectedSourceRule) {
        $errors.Add(
            "Codex bridge source_rule mismatch for '$name': '$($codexData.source_rule)'"
        )
    }
    if ($codexData.body -notmatch [regex]::Escape($expectedSourceRule)) {
        $errors.Add("Codex bridge reference mismatch for '$name'.")
    }

    if ($claudeData.trigger -ne $sourceData.trigger) {
        $errors.Add("Claude bridge trigger mismatch for '$name'.")
    }
    if ($claudeData.body -notmatch [regex]::Escape($expectedCodexBridgeRef)) {
        $errors.Add("Claude bridge reference mismatch for '$name'.")
    }

    if ($cursorData.trigger -ne $sourceData.trigger) {
        $errors.Add("Cursor bridge trigger mismatch for '$name'.")
    }
    if ($cursorData.body -notmatch [regex]::Escape($expectedClaudeBridgeRef)) {
        $errors.Add("Cursor bridge reference mismatch for '$name'.")
    }

    if ($githubData.trigger -ne $sourceData.trigger) {
        $errors.Add("GitHub Copilot bridge trigger mismatch for '$name'.")
    }
    if ($githubData.source_rule -ne $expectedSourceRule) {
        $errors.Add(
            "GitHub Copilot bridge source_rule mismatch for '$name': '$($githubData.source_rule)'"
        )
    }
    if ($githubData.body -notmatch [regex]::Escape($expectedSourceRule)) {
        $errors.Add("GitHub Copilot bridge reference mismatch for '$name'.")
    }
}

foreach ($codexBridgeName in $codexBridgeNames) {
    if ($sourceNames -notcontains $codexBridgeName) {
        $errors.Add("Codex bridge rule exists without source rule: '$codexBridgeName'.")
    }
}

foreach ($claudeBridgeName in $claudeBridgeNames) {
    if ($sourceNames -notcontains $claudeBridgeName) {
        $errors.Add("Claude bridge rule exists without source rule: '$claudeBridgeName'.")
    }
}

foreach ($cursorBridgeName in $cursorBridgeNames) {
    if ($sourceNames -notcontains $cursorBridgeName) {
        $errors.Add("Cursor bridge rule exists without source rule: '$cursorBridgeName'.")
    }
}

foreach ($githubBridgeName in $githubBridgeNames) {
    if ($sourceNames -notcontains $githubBridgeName) {
        $errors.Add("GitHub Copilot bridge rule exists without source rule: '$githubBridgeName'.")
    }
}

foreach ($sourceName in $sourceNames) {
    if ($codexBridgeNames -notcontains $sourceName) {
        $errors.Add("Source rule missing in Codex bridge tree: '$sourceName'.")
    }
    if ($claudeBridgeNames -notcontains $sourceName) {
        $errors.Add("Source rule missing in Claude bridge tree: '$sourceName'.")
    }
    if ($cursorBridgeNames -notcontains $sourceName) {
        $errors.Add("Source rule missing in Cursor bridge tree: '$sourceName'.")
    }
    if ($githubBridgeNames -notcontains $sourceName) {
        $errors.Add("Source rule missing in GitHub Copilot bridge tree: '$sourceName'.")
    }
}

if ($errors.Count -gt 0) {
    Write-Host "Rule bridge check failed with $($errors.Count) issue(s):" -ForegroundColor Red
    foreach ($errorItem in $errors) {
        Write-Host "- $errorItem"
    }
    exit 1
}

Write-Host "Rule bridge check passed:"
Write-Host "- source rules: $($sourceRules.Count)"
Write-Host "- codex bridges: $($codexBridgeRules.Count)"
Write-Host "- claude bridges: $($claudeBridgeRules.Count)"
Write-Host "- cursor bridges: $($cursorBridgeRules.Count)"
Write-Host "- github copilot bridges: $($githubBridgeRules.Count)"
exit 0
