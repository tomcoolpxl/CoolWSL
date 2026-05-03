[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$PackageVersion,

    [string]$ProjectPath = "CoolWSL.App/CoolWSL.App.csproj",

    [string]$ManifestPath = "CoolWSL.App/Package.appxmanifest",

    [string]$OutputDirectory = "artifacts/release",

    [string]$CertificateFile,

    [string]$CertificatePassword,

    [switch]$Unsigned
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

function Set-GitHubOutputValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($env:GITHUB_OUTPUT)) {
        return
    }

    $delimiter = [Guid]::NewGuid().ToString('N')
    Add-Content -Path $env:GITHUB_OUTPUT -Value "$Name<<$delimiter"
    Add-Content -Path $env:GITHUB_OUTPUT -Value $Value
    Add-Content -Path $env:GITHUB_OUTPUT -Value $delimiter
}

function Get-SemanticVersionMatch {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $semanticVersionMatch = [regex]::Match(
        $Value,
        '^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?<suffix>[-+].+)?$')

    if (-not $semanticVersionMatch.Success) {
        throw "Version '$Value' must be a SemVer value like 1.2.3 or 1.2.3-rc.1."
    }

    return $semanticVersionMatch
}

function Test-DotQuadVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $parts = $Value.Split('.')
    if ($parts.Length -ne 4) {
        return $false
    }

    foreach ($part in $parts) {
        $parsedPart = 0
        if (-not [int]::TryParse($part, [ref]$parsedPart)) {
            return $false
        }

        if ($parsedPart -lt 0 -or $parsedPart -gt 65535) {
            return $false
        }
    }

    return $true
}

function Get-ResolvedPackageVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ApplicationVersion,

        [string]$ExplicitPackageVersion
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPackageVersion)) {
        if (-not (Test-DotQuadVersion -Value $ExplicitPackageVersion)) {
            throw "PackageVersion '$ExplicitPackageVersion' must be a numeric dot-quad version such as 1.2.3.0."
        }

        return $ExplicitPackageVersion
    }

    $semanticVersionMatch = Get-SemanticVersionMatch -Value $ApplicationVersion
    if ($semanticVersionMatch.Groups['suffix'].Success) {
        throw "PackageVersion is required when Version '$ApplicationVersion' includes prerelease or build metadata. MSIX package versions must be numeric dot-quad values."
    }

    return '{0}.{1}.{2}.0' -f
        $semanticVersionMatch.Groups['major'].Value,
        $semanticVersionMatch.Groups['minor'].Value,
        $semanticVersionMatch.Groups['patch'].Value
}

$resolvedProjectPath = Resolve-RepoPath -Path $ProjectPath
$resolvedManifestPath = Resolve-RepoPath -Path $ManifestPath
$resolvedOutputDirectory = Resolve-RepoPath -Path $OutputDirectory
$resolvedPackageVersion = Get-ResolvedPackageVersion -ApplicationVersion $Version -ExplicitPackageVersion $PackageVersion

if (-not (Test-Path -LiteralPath $resolvedProjectPath -PathType Leaf)) {
    throw "Project file '$resolvedProjectPath' was not found."
}

if (-not (Test-Path -LiteralPath $resolvedManifestPath -PathType Leaf)) {
    throw "Manifest file '$resolvedManifestPath' was not found."
}

if (-not $Unsigned.IsPresent) {
    if ([string]::IsNullOrWhiteSpace($CertificateFile)) {
        throw 'CertificateFile is required unless -Unsigned is specified.'
    }

    if ([string]::IsNullOrWhiteSpace($CertificatePassword)) {
        throw 'CertificatePassword is required unless -Unsigned is specified.'
    }
}

$resolvedCertificateFile = $null
if (-not [string]::IsNullOrWhiteSpace($CertificateFile)) {
    $resolvedCertificateFile = Resolve-RepoPath -Path $CertificateFile
    if (-not (Test-Path -LiteralPath $resolvedCertificateFile -PathType Leaf)) {
        throw "Certificate file '$resolvedCertificateFile' was not found."
    }
}

Remove-Item -LiteralPath $resolvedOutputDirectory -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $resolvedOutputDirectory -Force | Out-Null

$temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("coolwsl-release-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporaryDirectory -Force | Out-Null

$stampedManifestPath = Join-Path $temporaryDirectory 'Package.appxmanifest'

try {
    $manifestContent = Get-Content -LiteralPath $resolvedManifestPath -Raw
    $stampedManifestContent = [regex]::Replace(
        $manifestContent,
        'Version="\d+\.\d+\.\d+\.\d+"',
        "Version=`"$resolvedPackageVersion`"",
        1)

    if ($stampedManifestContent -eq $manifestContent) {
        throw "Failed to update the package identity version in '$resolvedManifestPath'."
    }

    Set-Content -LiteralPath $stampedManifestPath -Value $stampedManifestContent -Encoding utf8NoBOM

    $dotnetArguments = @(
        'build',
        $resolvedProjectPath,
        '-c',
        'Release',
        '-p:GenerateAppxPackageOnBuild=true',
        '-p:AppxBundle=Never',
        '-p:UapAppxPackageBuildMode=SideloadOnly',
        "-p:PackageManifestPath=$stampedManifestPath",
        "-p:AppxPackageDir=$resolvedOutputDirectory\"
    )

    if ($Unsigned.IsPresent) {
        $dotnetArguments += '-p:AppxPackageSigningEnabled=false'
    }
    else {
        $dotnetArguments += '-p:AppxPackageSigningEnabled=true'
        $dotnetArguments += '-p:PackageCertificateThumbprint='
        $dotnetArguments += "-p:PackageCertificateKeyFile=$resolvedCertificateFile"
        $dotnetArguments += "-p:PackageCertificatePassword=$CertificatePassword"
    }

    $previousCoolWslVersion = $env:COOLWSL_VERSION
    $coolWslVersionWasSet = $null -ne $previousCoolWslVersion
    $env:COOLWSL_VERSION = $Version

    try {
        & dotnet @dotnetArguments
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet build failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        if ($coolWslVersionWasSet) {
            $env:COOLWSL_VERSION = $previousCoolWslVersion
        }
        else {
            Remove-Item Env:COOLWSL_VERSION -ErrorAction SilentlyContinue
        }
    }

    $msixFile = Get-ChildItem -LiteralPath $resolvedOutputDirectory -Recurse -File -Filter '*.msix' |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $msixFile) {
        throw "No .msix file was produced under '$resolvedOutputDirectory'."
    }

    $fileHash = Get-FileHash -LiteralPath $msixFile.FullName -Algorithm SHA256
    $checksumPath = [System.IO.Path]::ChangeExtension($msixFile.FullName, '.sha256.txt')
    Set-Content -LiteralPath $checksumPath -Value ($fileHash.Hash.ToLowerInvariant() + ' *' + $msixFile.Name) -Encoding ascii

    Set-GitHubOutputValue -Name 'app_version' -Value $Version
    Set-GitHubOutputValue -Name 'package_version' -Value $resolvedPackageVersion
    Set-GitHubOutputValue -Name 'msix_path' -Value $msixFile.FullName
    Set-GitHubOutputValue -Name 'sha256_path' -Value $checksumPath
    Set-GitHubOutputValue -Name 'output_directory' -Value $resolvedOutputDirectory

    Write-Host "App version: $Version"
    Write-Host "Package version: $resolvedPackageVersion"
    Write-Host "MSIX: $($msixFile.FullName)"
    Write-Host "SHA256: $checksumPath"
}
finally {
    Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force -ErrorAction SilentlyContinue
}