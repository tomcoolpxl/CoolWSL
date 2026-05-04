[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$Repository = "tomcoolpxl/CoolWSL",

    [string]$PackageIdentifier = "tomcoolpxl.CoolWSL",

    [string]$PackageName = "CoolWSL",

    [string]$Publisher = "tomcoolpxl",

    [string]$PackageLocale = "en-US",

    [string]$License = "MIT",

    [string]$ShortDescription = "WSL Control Center for Windows 11.",

    [string]$Description = "CoolWSL is a desktop control center for Windows Subsystem for Linux on Windows 11.",

    [string]$RuntimeIdentifier = "win-x64",

    [string]$ChecksumsFile,

    [string]$OutputDirectory = "artifacts/winget"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))

function Resolve-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Get-StableSemanticVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $match = [regex]::Match(
        $Value,
        '^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)$')

    if (-not $match.Success) {
        throw "Version '$Value' must be a stable SemVer value like 1.2.3."
    }
}

function Get-ManifestRelativeDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Identifier,

        [Parameter(Mandatory = $true)]
        [string]$PackageVersion
    )

    $segments = $Identifier -split '\.'
    if ($segments.Count -lt 2) {
        throw "Package identifier '$Identifier' must use at least two segments (for example: Contoso.Tool)."
    }

    $firstSegment = $segments[0]
    if ([string]::IsNullOrWhiteSpace($firstSegment)) {
        throw "Package identifier '$Identifier' is invalid."
    }

    $firstLetter = $firstSegment.Substring(0, 1).ToLowerInvariant()
    return ('manifests/{0}/{1}/{2}' -f $firstLetter, ($segments -join '/'), $PackageVersion)
}

function Get-ArtifactHashFromChecksums {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ChecksumsPath,

        [Parameter(Mandatory = $true)]
        [string]$ArtifactFileName
    )

    $line = Get-Content -LiteralPath $ChecksumsPath |
        Where-Object { $_ -match ('\s\*' + [regex]::Escape($ArtifactFileName) + '$') } |
        Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace($line)) {
        throw "Could not find '$ArtifactFileName' in checksums file '$ChecksumsPath'."
    }

    $hash = ($line -split '\s+\*')[0].Trim()
    if ($hash -notmatch '^[a-fA-F0-9]{64}$') {
        throw "Found hash for '$ArtifactFileName' in '$ChecksumsPath', but it was not a valid SHA256 value."
    }

    return $hash.ToUpperInvariant()
}

function Get-ReleaseChecksumsPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Repository,

        [Parameter(Mandatory = $true)]
        [string]$Tag,

        [Parameter(Mandatory = $true)]
        [string]$AssetBaseName
    )

    $checksumsFileName = "$AssetBaseName.checksums.txt"
    $checksumsUrl = "https://github.com/$Repository/releases/download/$Tag/$checksumsFileName"
    $temporaryPath = Join-Path ([System.IO.Path]::GetTempPath()) ("$checksumsFileName." + [Guid]::NewGuid().ToString('N'))

    try {
        $ProgressPreference = 'SilentlyContinue'
        Invoke-WebRequest -Uri $checksumsUrl -OutFile $temporaryPath
        return $temporaryPath
    }
    catch {
        throw "Failed to download release checksums from '$checksumsUrl'. Provide -ChecksumsFile explicitly or verify the release assets exist."
    }
}

function Write-Utf8NoBomFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Content
    )

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $encoding = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

Get-StableSemanticVersion -Value $Version

$tag = "v$Version"
$assetBaseName = "CoolWSL-$Version-$RuntimeIdentifier"
$msiFileName = "$assetBaseName.msi"
$installerUrl = "https://github.com/$Repository/releases/download/$tag/$msiFileName"

if ($Repository -notmatch '^[^/]+/[^/]+$') {
    throw "Repository '$Repository' must be in the format owner/name."
}

$temporaryChecksumsPath = $null
if ([string]::IsNullOrWhiteSpace($ChecksumsFile)) {
    $temporaryChecksumsPath = Get-ReleaseChecksumsPath -Repository $Repository -Tag $tag -AssetBaseName $assetBaseName
    $resolvedChecksumsPath = $temporaryChecksumsPath
}
else {
    $resolvedChecksumsPath = Resolve-RepoPath -Path $ChecksumsFile
    if (-not (Test-Path -LiteralPath $resolvedChecksumsPath -PathType Leaf)) {
        throw "Checksums file '$resolvedChecksumsPath' was not found."
    }
}

try {
    $installerSha256 = Get-ArtifactHashFromChecksums -ChecksumsPath $resolvedChecksumsPath -ArtifactFileName $msiFileName
}
finally {
    if (-not [string]::IsNullOrWhiteSpace($temporaryChecksumsPath) -and (Test-Path -LiteralPath $temporaryChecksumsPath -PathType Leaf)) {
        Remove-Item -LiteralPath $temporaryChecksumsPath -Force -ErrorAction SilentlyContinue
    }
}

$owner = ($Repository -split '/')[0]
$releaseTagUrl = "https://github.com/$Repository/releases/tag/$tag"
$manifestRelativePath = Get-ManifestRelativeDirectory -Identifier $PackageIdentifier -PackageVersion $Version
$resolvedOutputRoot = Resolve-RepoPath -Path $OutputDirectory
$manifestDirectory = Join-Path $resolvedOutputRoot $manifestRelativePath

$versionManifestPath = Join-Path $manifestDirectory "$PackageIdentifier.yaml"
$installerManifestPath = Join-Path $manifestDirectory "$PackageIdentifier.installer.yaml"
$defaultLocaleManifestPath = Join-Path $manifestDirectory "$PackageIdentifier.locale.$PackageLocale.yaml"

$versionManifest = @"
PackageIdentifier: $PackageIdentifier
PackageVersion: $Version
DefaultLocale: $PackageLocale
ManifestType: version
ManifestVersion: 1.10.0
"@

$installerManifest = @"
PackageIdentifier: $PackageIdentifier
PackageVersion: $Version
InstallerType: wix
Scope: machine
ElevationRequirement: elevationRequired
Installers:
- Architecture: x64
  InstallerUrl: $installerUrl
  InstallerSha256: $installerSha256
ManifestType: installer
ManifestVersion: 1.10.0
"@

$defaultLocaleManifest = @"
PackageIdentifier: $PackageIdentifier
PackageVersion: $Version
PackageLocale: $PackageLocale
Publisher: $Publisher
PublisherUrl: https://github.com/$owner
PublisherSupportUrl: https://github.com/$Repository/issues
Author: $Publisher
PackageName: $PackageName
PackageUrl: https://github.com/$Repository
License: $License
LicenseUrl: https://github.com/$Repository/blob/main/LICENSE
ShortDescription: $ShortDescription
Description: $Description
ReleaseNotesUrl: $releaseTagUrl
Tags:
- wsl
- windows-subsystem-for-linux
- windows
- diagnostics
ManifestType: defaultLocale
ManifestVersion: 1.10.0
"@

Write-Utf8NoBomFile -Path $versionManifestPath -Content $versionManifest
Write-Utf8NoBomFile -Path $installerManifestPath -Content $installerManifest
Write-Utf8NoBomFile -Path $defaultLocaleManifestPath -Content $defaultLocaleManifest

Write-Host "Generated Winget manifests:"
Write-Host "- $versionManifestPath"
Write-Host "- $installerManifestPath"
Write-Host "- $defaultLocaleManifestPath"
