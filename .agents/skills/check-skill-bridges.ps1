param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\\..")).Path
)

$ErrorActionPreference = "Stop"

$sourceRoot = Join-Path $RepoRoot ".agent/skills"
$bridgeRoot = Join-Path $RepoRoot ".agents/skills"

function Get-FrontmatterData {
    param([string]$SkillMdPath)

    $raw = Get-Content $SkillMdPath -Raw
    $fmMatch = [regex]::Match($raw, "(?ms)^---\r?\n(.*?)\r?\n---")
    if (-not $fmMatch.Success) {
        throw "No YAML frontmatter in $SkillMdPath"
    }

    $frontmatter = $fmMatch.Groups[1].Value
    $name = [regex]::Match($frontmatter, "(?m)^name:\s*(.+)$").Groups[1].Value.Trim()
    $description = [regex]::Match($frontmatter, "(?m)^description:\s*(.+)$").Groups[1].Value.Trim()
    $sourceSkill = [regex]::Match(
        $frontmatter,
        "(?m)^\s*source_skill:\s*`"?([^`"\r\n]+)`"?\s*$"
    ).Groups[1].Value.Trim()

    return @{
        name = $name
        description = $description
        source_skill = $sourceSkill
    }
}

if (-not (Test-Path $sourceRoot)) {
    throw "Source skills path not found: $sourceRoot"
}
if (-not (Test-Path $bridgeRoot)) {
    throw "Bridge skills path not found: $bridgeRoot"
}

$errors = New-Object System.Collections.Generic.List[string]

$sourceSkills = Get-ChildItem $sourceRoot -Directory |
    Where-Object { Test-Path (Join-Path $_.FullName "SKILL.md") } |
    Sort-Object Name
$bridgeSkills = Get-ChildItem $bridgeRoot -Directory |
    Where-Object { Test-Path (Join-Path $_.FullName "SKILL.md") } |
    Sort-Object Name

$sourceNames = @($sourceSkills.Name)
$bridgeNames = @($bridgeSkills.Name)

foreach ($source in $sourceSkills) {
    $name = $source.Name
    $sourceSkillMd = Join-Path $source.FullName "SKILL.md"
    $bridgeSkillMd = Join-Path $bridgeRoot "$name/SKILL.md"

    if (-not (Test-Path $bridgeSkillMd)) {
        $errors.Add("Missing bridge SKILL.md for '$name'.")
        continue
    }

    $sourceFm = Get-FrontmatterData -SkillMdPath $sourceSkillMd
    $bridgeFm = Get-FrontmatterData -SkillMdPath $bridgeSkillMd
    $expectedSourceSkill = "../../../.agent/skills/$name/SKILL.md"

    if ($bridgeFm.name -ne $name) {
        $errors.Add("Bridge name mismatch for '$name': '$($bridgeFm.name)'")
    }
    if ($bridgeFm.description -ne $sourceFm.description) {
        $errors.Add("Bridge description mismatch for '$name'.")
    }
    if ($bridgeFm.source_skill -ne $expectedSourceSkill) {
        $errors.Add(
            "Bridge source_skill mismatch for '$name': '$($bridgeFm.source_skill)'"
        )
    }
}

foreach ($bridgeName in $bridgeNames) {
    if ($sourceNames -notcontains $bridgeName) {
        $errors.Add("Bridge exists without source skill: '$bridgeName'.")
    }
}

if ($errors.Count -gt 0) {
    Write-Host "Bridge check failed with $($errors.Count) issue(s):" -ForegroundColor Red
    foreach ($errorItem in $errors) {
        Write-Host "- $errorItem"
    }
    exit 1
}

Write-Host "Bridge check passed: $($sourceSkills.Count) skill bridge(s) validated."
exit 0
