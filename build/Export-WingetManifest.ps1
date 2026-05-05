[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$Repository = "tomcoolpxl/CoolWSL",

    [string]$PackageIdentifier = "tomcoolpxl.CoolWSL",

    [string]$PackageName = "CoolWSL",

    [string]$Publisher = "CoolWSL",

    [string]$Author,

    [string]$PackageLocale = "en-US",

    [string]$License = "MIT",

    [string]$ShortDescription = "Desktop app for managing WSL distros and diagnostics on Windows 11.",

    [string]$Description = "CoolWSL is a Windows 11 desktop app for inspecting WSL distro state, viewing diagnostics and logs, and performing common WSL management tasks without memorizing command flags.",

    [string[]]$PackageDependencies = @('Microsoft.DotNet.DesktopRuntime.10'),

    [string]$RuntimeIdentifier = "win-x64",

    [string]$InstallerPath,

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
    return Get-ReleaseAssetPath -Repository $Repository -Tag $Tag -AssetFileName $checksumsFileName -FailureMessage "Failed to download release checksums for '$Repository' tag '$Tag'. Provide -ChecksumsFile explicitly or verify the release assets exist."
}

function Get-ReleaseAssetPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Repository,

        [Parameter(Mandatory = $true)]
        [string]$Tag,

        [Parameter(Mandatory = $true)]
        [string]$AssetFileName,

        [Parameter(Mandatory = $true)]
        [string]$FailureMessage
    )

    $assetUrl = "https://github.com/$Repository/releases/download/$Tag/$AssetFileName"
    $assetStem = [System.IO.Path]::GetFileNameWithoutExtension($AssetFileName)
    $assetExtension = [System.IO.Path]::GetExtension($AssetFileName)
    $temporaryFileName = "{0}.{1}{2}" -f $assetStem, [Guid]::NewGuid().ToString('N'), $assetExtension
    $temporaryPath = Join-Path ([System.IO.Path]::GetTempPath()) $temporaryFileName

    try {
        $ProgressPreference = 'SilentlyContinue'
        Invoke-WebRequest -Uri $assetUrl -OutFile $temporaryPath
        return $temporaryPath
    }
    catch {
        throw $FailureMessage
    }
}

function Get-MsiPropertyValue {
    param(
        [Parameter(Mandatory = $true)]
        $Database,

        [Parameter(Mandatory = $true)]
        [string]$PropertyName
    )

    $view = $Database.GetType().InvokeMember('OpenView', 'InvokeMethod', $null, $Database, @("SELECT `Value` FROM `Property` WHERE `Property`='$PropertyName'"))

    try {
        $view.GetType().InvokeMember('Execute', 'InvokeMethod', $null, $view, $null) | Out-Null
        $record = $view.GetType().InvokeMember('Fetch', 'InvokeMethod', $null, $view, $null)

        if ($null -eq $record) {
            return $null
        }

        return $record.StringData(1)
    }
    finally {
        if ($null -ne $view) {
            $view.GetType().InvokeMember('Close', 'InvokeMethod', $null, $view, $null) | Out-Null
        }
    }
}

function Get-MsiMetadata {
    param(
        [Parameter(Mandatory = $true)]
        [string]$InstallerFilePath
    )

    $installer = New-Object -ComObject WindowsInstaller.Installer
    $database = $installer.GetType().InvokeMember('OpenDatabase', 'InvokeMethod', $null, $installer, @($InstallerFilePath, 0))

    return [pscustomobject]@{
        ProductName = Get-MsiPropertyValue -Database $database -PropertyName 'ProductName'
        Manufacturer = Get-MsiPropertyValue -Database $database -PropertyName 'Manufacturer'
        ProductVersion = Get-MsiPropertyValue -Database $database -PropertyName 'ProductVersion'
        ProductCode = Get-MsiPropertyValue -Database $database -PropertyName 'ProductCode'
        UpgradeCode = Get-MsiPropertyValue -Database $database -PropertyName 'UpgradeCode'
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

$temporaryInstallerPath = $null
if ([string]::IsNullOrWhiteSpace($InstallerPath)) {
    $candidateInstallerPath = Join-Path (Split-Path -Parent $resolvedChecksumsPath) $msiFileName
    if (Test-Path -LiteralPath $candidateInstallerPath -PathType Leaf) {
        $resolvedInstallerPath = $candidateInstallerPath
    }
    else {
        $temporaryInstallerPath = Get-ReleaseAssetPath -Repository $Repository -Tag $tag -AssetFileName $msiFileName -FailureMessage "Failed to download installer '$msiFileName' from '$Repository' tag '$tag'. Provide -InstallerPath explicitly or verify the release asset exists."
        $resolvedInstallerPath = $temporaryInstallerPath
    }
}
else {
    $resolvedInstallerPath = Resolve-RepoPath -Path $InstallerPath
    if (-not (Test-Path -LiteralPath $resolvedInstallerPath -PathType Leaf)) {
        throw "Installer file '$resolvedInstallerPath' was not found."
    }
}

try {
    $installerSha256 = Get-ArtifactHashFromChecksums -ChecksumsPath $resolvedChecksumsPath -ArtifactFileName $msiFileName
    $installerMetadata = Get-MsiMetadata -InstallerFilePath $resolvedInstallerPath
}
finally {
    if (-not [string]::IsNullOrWhiteSpace($temporaryChecksumsPath) -and (Test-Path -LiteralPath $temporaryChecksumsPath -PathType Leaf)) {
        Remove-Item -LiteralPath $temporaryChecksumsPath -Force -ErrorAction SilentlyContinue
    }

    if (-not [string]::IsNullOrWhiteSpace($temporaryInstallerPath) -and (Test-Path -LiteralPath $temporaryInstallerPath -PathType Leaf)) {
        Remove-Item -LiteralPath $temporaryInstallerPath -Force -ErrorAction SilentlyContinue
    }
}

if (-not [string]::IsNullOrWhiteSpace($installerMetadata.ProductVersion) -and $installerMetadata.ProductVersion -ne $Version) {
    throw "Installer ProductVersion '$($installerMetadata.ProductVersion)' did not match requested version '$Version'."
}

$owner = ($Repository -split '/')[0]
$manifestAuthor = if ([string]::IsNullOrWhiteSpace($Author)) { $owner } else { $Author }

if (-not $PSBoundParameters.ContainsKey('PackageName') -and -not [string]::IsNullOrWhiteSpace($installerMetadata.ProductName)) {
    $PackageName = $installerMetadata.ProductName
}

if (-not $PSBoundParameters.ContainsKey('Publisher') -and -not [string]::IsNullOrWhiteSpace($installerMetadata.Manufacturer)) {
    $Publisher = $installerMetadata.Manufacturer
}

$releaseTagUrl = "https://github.com/$Repository/releases/tag/$tag"
$manifestRelativePath = Get-ManifestRelativeDirectory -Identifier $PackageIdentifier -PackageVersion $Version
$resolvedOutputRoot = Resolve-RepoPath -Path $OutputDirectory
$manifestDirectory = Join-Path $resolvedOutputRoot $manifestRelativePath

$versionManifestPath = Join-Path $manifestDirectory "$PackageIdentifier.yaml"
$installerManifestPath = Join-Path $manifestDirectory "$PackageIdentifier.installer.yaml"
$defaultLocaleManifestPath = Join-Path $manifestDirectory "$PackageIdentifier.locale.$PackageLocale.yaml"

$versionManifest = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.version.1.10.0.schema.json

PackageIdentifier: $PackageIdentifier
PackageVersion: $Version
DefaultLocale: $PackageLocale
ManifestType: version
ManifestVersion: 1.10.0
"@

$installerMetadataBlockLines = @()
if (-not [string]::IsNullOrWhiteSpace($installerMetadata.ProductCode)) {
    $installerMetadataBlockLines += "ProductCode: '$($installerMetadata.ProductCode)'"
}

$appsAndFeaturesEntryLines = @()
if (-not [string]::IsNullOrWhiteSpace($PackageName)) {
    $appsAndFeaturesEntryLines += "- DisplayName: $PackageName"
}
if (-not [string]::IsNullOrWhiteSpace($Publisher)) {
    $appsAndFeaturesEntryLines += "  Publisher: $Publisher"
}
if (-not [string]::IsNullOrWhiteSpace($installerMetadata.ProductCode)) {
    $appsAndFeaturesEntryLines += "  ProductCode: '$($installerMetadata.ProductCode)'"
}
if (-not [string]::IsNullOrWhiteSpace($installerMetadata.UpgradeCode)) {
    $appsAndFeaturesEntryLines += "  UpgradeCode: '$($installerMetadata.UpgradeCode)'"
}
if ($appsAndFeaturesEntryLines.Count -gt 0) {
    $installerMetadataBlockLines += 'AppsAndFeaturesEntries:'
    $installerMetadataBlockLines += $appsAndFeaturesEntryLines
}

$packageDependencyIdentifiers = @(
    $PackageDependencies |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
)

if ($packageDependencyIdentifiers.Count -gt 0) {
    $installerMetadataBlockLines += 'Dependencies:'
    $installerMetadataBlockLines += '  PackageDependencies:'

    foreach ($packageDependencyIdentifier in $packageDependencyIdentifiers) {
        $installerMetadataBlockLines += "    - PackageIdentifier: $packageDependencyIdentifier"
    }
}

$installerMetadataBlock = ''
if ($installerMetadataBlockLines.Count -gt 0) {
    $installerMetadataBlock = ([string]::Join("`r`n", $installerMetadataBlockLines)) + "`r`n"
}

$installerManifest = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.installer.1.10.0.schema.json

PackageIdentifier: $PackageIdentifier
PackageVersion: $Version
InstallerType: wix
Scope: machine
ElevationRequirement: elevationRequired
$installerMetadataBlock
Installers:
- Architecture: x64
  InstallerUrl: $installerUrl
  InstallerSha256: $installerSha256
ManifestType: installer
ManifestVersion: 1.10.0
"@

$defaultLocaleManifest = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.defaultLocale.1.10.0.schema.json

PackageIdentifier: $PackageIdentifier
PackageVersion: $Version
PackageLocale: $PackageLocale
Publisher: $Publisher
PublisherUrl: https://github.com/$owner
PublisherSupportUrl: https://github.com/$Repository/issues
Author: $manifestAuthor
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
