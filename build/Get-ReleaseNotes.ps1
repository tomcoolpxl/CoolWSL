[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^v\d+\.\d+\.\d+$')]
    [string]$Tag,

    [Parameter(Mandatory)]
    [string]$OutputPath,

    [string]$Repository = $env:GITHUB_REPOSITORY
)

$ErrorActionPreference = 'Stop'

function Invoke-Git {
    param(
        [Parameter(ValueFromRemainingArguments)]
        [string[]]$Arguments
    )

    $output = & git @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed."
    }

    return $output
}

function Get-CategoryName {
    param(
        [AllowEmptyString()]
        [string]$CommitType
    )

    if ([string]::IsNullOrWhiteSpace($CommitType)) {
        return 'Other Changes'
    }

    switch -Regex ($CommitType.ToLowerInvariant()) {
        '^(feat|feature)$' { return 'Features' }
        '^(fix|bugfix|hotfix)$' { return 'Fixes' }
        '^(winget|release|build|ci|packaging)$' { return 'Packaging and Delivery' }
        '^(docs|doc)$' { return 'Documentation' }
        default { return 'Other Changes' }
    }
}

$stableTags = @(Invoke-Git tag --list 'v*.*.*' --sort=version:refname | Where-Object { $_ -match '^v\d+\.\d+\.\d+$' })
if (-not $stableTags) {
    throw 'No stable vX.Y.Z tags were found in the repository checkout.'
}

if ($stableTags -notcontains $Tag) {
    throw "Tag '$Tag' was not found in the repository checkout."
}

$currentIndex = [Array]::IndexOf([string[]]$stableTags, $Tag)
$previousTag = if ($currentIndex -gt 0) { $stableTags[$currentIndex - 1] } else { $null }
$rangeSpec = if ($previousTag) { "$previousTag..$Tag" } else { $Tag }

$categories = [ordered]@{
    'Features' = [System.Collections.Generic.List[string]]::new()
    'Fixes' = [System.Collections.Generic.List[string]]::new()
    'Packaging and Delivery' = [System.Collections.Generic.List[string]]::new()
    'Documentation' = [System.Collections.Generic.List[string]]::new()
    'Other Changes' = [System.Collections.Generic.List[string]]::new()
}

$commitLines = @(Invoke-Git log --reverse --format='%H%x09%s' $rangeSpec)
foreach ($line in $commitLines) {
    if ([string]::IsNullOrWhiteSpace($line)) {
        continue
    }

    $parts = $line -split "`t", 2
    if ($parts.Count -lt 2) {
        continue
    }

    $commitSha = $parts[0].Trim()
    $subject = $parts[1].Trim()

    $commitType = ''
    $summary = $subject
    if ($subject -match '^(?<type>[^:]+):\s*(?<summary>.+)$') {
        $commitType = $Matches.type.Trim()
        $summary = $Matches.summary.Trim()
    }

    $categoryName = Get-CategoryName -CommitType $commitType
    $entry = if ([string]::IsNullOrWhiteSpace($Repository)) {
        "- $summary"
    }
    else {
        $shortSha = $commitSha.Substring(0, 7)
        "- $summary ([$shortSha](https://github.com/$Repository/commit/$commitSha))"
    }

    $categories[$categoryName].Add($entry)
}

$releaseNotes = [System.Collections.Generic.List[string]]::new()
$releaseNotes.Add('## What Changed')
$releaseNotes.Add('')

foreach ($category in $categories.GetEnumerator()) {
    if ($category.Value.Count -eq 0) {
        continue
    }

    $releaseNotes.Add("### $($category.Key)")
    foreach ($entry in $category.Value) {
        $releaseNotes.Add($entry)
    }
    $releaseNotes.Add('')
}

if ($previousTag) {
    if (-not [string]::IsNullOrWhiteSpace($Repository)) {
        $releaseNotes.Add("**Full Changelog**: https://github.com/$Repository/compare/$previousTag...$Tag")
    }
    else {
        $releaseNotes.Add("**Full Changelog**: $previousTag...$Tag")
    }
}
else {
    $releaseNotes.Add('Initial stable release.')
}

$outputDirectory = Split-Path -Path $OutputPath -Parent
if ($outputDirectory) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

Set-Content -Path $OutputPath -Value $releaseNotes -Encoding utf8