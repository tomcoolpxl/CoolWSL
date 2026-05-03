[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$ProjectPath = "CoolWSL.App/CoolWSL.App.csproj",

    [string]$InstallerProjectPath = "build/CoolWSL.Installer.wixproj",

    [string]$OutputDirectory = "artifacts/release",

    [string]$RuntimeIdentifier = "win-x64"
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

    $major = [int]$match.Groups['major'].Value
    $minor = [int]$match.Groups['minor'].Value
    $patch = [int]$match.Groups['patch'].Value

    if ($major -gt 255 -or $minor -gt 255 -or $patch -gt 65535) {
        throw "Version '$Value' is outside Windows Installer ProductVersion limits (major/minor <= 255, patch <= 65535)."
    }

    return [pscustomobject]@{
        ApplicationVersion = $Value
        InstallerVersion   = '{0}.{1}.{2}' -f $major, $minor, $patch
    }
}

function Join-RelativePath {
    param(
        [string]$Left,
        [Parameter(Mandatory = $true)]
        [string]$Right
    )

    if ([string]::IsNullOrWhiteSpace($Left)) {
        return $Right
    }

    return $Left + '\\' + $Right
}

function Get-IdentifierHash {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
        $hashBytes = $sha256.ComputeHash($bytes)
        return ([System.BitConverter]::ToString($hashBytes)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
    }
}

function New-WixIdentifier {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Prefix,

        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $normalizedPath = $RelativePath.Replace('\\', '/').ToLowerInvariant()
    return $Prefix + (Get-IdentifierHash -Value $normalizedPath).Substring(0, 24)
}

function New-DeterministicGuid {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $hash = Get-IdentifierHash -Value $Value
    return '{0}-{1}-{2}-{3}-{4}' -f
        $hash.Substring(0, 8),
        $hash.Substring(8, 4),
        $hash.Substring(12, 4),
        $hash.Substring(16, 4),
        $hash.Substring(20, 12)
}

function Save-WixDocument {
    param(
        [Parameter(Mandatory = $true)]
        [System.Xml.Linq.XDocument]$Document,

        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $settings = [System.Xml.XmlWriterSettings]::new()
    $settings.Encoding = [System.Text.UTF8Encoding]::new($false)
    $settings.Indent = $true
    $settings.NewLineChars = [Environment]::NewLine
    $settings.NewLineHandling = [System.Xml.NewLineHandling]::Replace

    $writer = [System.Xml.XmlWriter]::Create($Path, $settings)
    try {
        $Document.Save($writer)
    }
    finally {
        $writer.Dispose()
    }
}

function New-WixFilesFragment {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PublishDirectory,

        [Parameter(Mandatory = $true)]
        [string]$OutputPath
    )

    $wixNamespace = [System.Xml.Linq.XNamespace]::Get('http://wixtoolset.org/schemas/v4/wxs')
    $wixElement = [System.Xml.Linq.XElement]::new($wixNamespace + 'Wix')
    $fragmentElement = [System.Xml.Linq.XElement]::new($wixNamespace + 'Fragment')
    $directoryRefElement = [System.Xml.Linq.XElement]::new($wixNamespace + 'DirectoryRef')
    $directoryRefElement.SetAttributeValue('Id', 'INSTALLFOLDER')
    $componentGroupElement = [System.Xml.Linq.XElement]::new($wixNamespace + 'ComponentGroup')
    $componentGroupElement.SetAttributeValue('Id', 'InstallFiles')

    $fragmentElement.Add($directoryRefElement)
    $fragmentElement.Add($componentGroupElement)
    $wixElement.Add($fragmentElement)

    $companionFileNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $null = $companionFileNames.Add('Microsoft.UI.Xaml.dll')
    $null = $companionFileNames.Add('Microsoft.UI.Xaml.Phone.dll')

    function Add-DirectoryItems {
        param(
            [Parameter(Mandatory = $true)]
            [System.Xml.Linq.XElement]$ParentElement,

            [Parameter(Mandatory = $true)]
            [string]$DirectoryPath,

            [string]$RelativePath
        )

        $files = @(Get-ChildItem -LiteralPath $DirectoryPath -File | Sort-Object Name)
        $companionParentComponentElement = $null
        $companionParentFileId = $null

        foreach ($file in $files) {
            $fileRelativePath = Join-RelativePath -Left $RelativePath -Right $file.Name
            $fileId = New-WixIdentifier -Prefix 'fil' -RelativePath $fileRelativePath

            if ($companionFileNames.Contains($file.Name)) {
                if ($null -eq $companionParentComponentElement -or [string]::IsNullOrWhiteSpace($companionParentFileId)) {
                    throw "Cannot author companion file '$fileRelativePath' before its parent file in '$DirectoryPath'."
                }

                $fileElement = [System.Xml.Linq.XElement]::new($wixNamespace + 'File')
                $fileElement.SetAttributeValue('Id', $fileId)
                $fileElement.SetAttributeValue('Source', $file.FullName)
                $fileElement.SetAttributeValue('CompanionFile', $companionParentFileId)

                $companionParentComponentElement.Add($fileElement)
                continue
            }

            $componentId = New-WixIdentifier -Prefix 'cmp' -RelativePath $fileRelativePath

            $componentElement = [System.Xml.Linq.XElement]::new($wixNamespace + 'Component')
            $componentElement.SetAttributeValue('Id', $componentId)
            $componentElement.SetAttributeValue('Guid', '*')

            $fileElement = [System.Xml.Linq.XElement]::new($wixNamespace + 'File')
            $fileElement.SetAttributeValue('Id', $fileId)
            $fileElement.SetAttributeValue('Source', $file.FullName)
            $fileElement.SetAttributeValue('KeyPath', 'yes')

            if ($file.Name -eq 'CoolWSL.App.exe') {
                $componentElement.SetAttributeValue('Guid', (New-DeterministicGuid -Value $fileRelativePath))

                $shortcutElement = [System.Xml.Linq.XElement]::new($wixNamespace + 'Shortcut')
                $shortcutElement.SetAttributeValue('Id', 'StartMenuShortcut')
                $shortcutElement.SetAttributeValue('Advertise', 'yes')
                $shortcutElement.SetAttributeValue('Directory', 'CoolWSLProgramMenuFolder')
                $shortcutElement.SetAttributeValue('Name', 'CoolWSL')
                $shortcutElement.SetAttributeValue('Description', 'WSL Control Center for Windows 11')
                $shortcutElement.SetAttributeValue('WorkingDirectory', 'INSTALLFOLDER')

                $fileElement.Add($shortcutElement)

                $removeFolderElement = [System.Xml.Linq.XElement]::new($wixNamespace + 'RemoveFolder')
                $removeFolderElement.SetAttributeValue('Id', 'RemoveCoolWSLProgramMenuFolder')
                $removeFolderElement.SetAttributeValue('Directory', 'CoolWSLProgramMenuFolder')
                $removeFolderElement.SetAttributeValue('On', 'uninstall')

                $componentElement.Add($removeFolderElement)
            }

            $componentElement.Add($fileElement)
            $ParentElement.Add($componentElement)

            if ($file.Name -eq 'CoolWSL.App.exe') {
                $companionParentComponentElement = $componentElement
                $companionParentFileId = $fileId
            }

            $componentRefElement = [System.Xml.Linq.XElement]::new($wixNamespace + 'ComponentRef')
            $componentRefElement.SetAttributeValue('Id', $componentId)
            $componentGroupElement.Add($componentRefElement)
        }

        $subdirectories = @(Get-ChildItem -LiteralPath $DirectoryPath -Directory | Sort-Object Name)
        foreach ($subdirectory in $subdirectories) {
            $subdirectoryRelativePath = Join-RelativePath -Left $RelativePath -Right $subdirectory.Name
            $directoryId = New-WixIdentifier -Prefix 'dir' -RelativePath $subdirectoryRelativePath

            $directoryElement = [System.Xml.Linq.XElement]::new($wixNamespace + 'Directory')
            $directoryElement.SetAttributeValue('Id', $directoryId)
            $directoryElement.SetAttributeValue('Name', $subdirectory.Name)

            $ParentElement.Add($directoryElement)
            Add-DirectoryItems -ParentElement $directoryElement -DirectoryPath $subdirectory.FullName -RelativePath $subdirectoryRelativePath
        }
    }

    Add-DirectoryItems -ParentElement $directoryRefElement -DirectoryPath $PublishDirectory -RelativePath ''

    $document = [System.Xml.Linq.XDocument]::new(
        [System.Xml.Linq.XDeclaration]::new('1.0', 'utf-8', $null),
        $wixElement)

    Save-WixDocument -Document $document -Path $OutputPath
}

function Write-ChecksumsFile {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$FilePaths,

        [Parameter(Mandatory = $true)]
        [string]$OutputPath
    )

    $lines = foreach ($filePath in $FilePaths) {
        $hash = Get-FileHash -LiteralPath $filePath -Algorithm SHA256
        $hash.Hash.ToLowerInvariant() + ' *' + [System.IO.Path]::GetFileName($filePath)
    }

    Set-Content -LiteralPath $OutputPath -Value $lines -Encoding ascii
}

$resolvedProjectPath = Resolve-RepoPath -Path $ProjectPath
$resolvedInstallerProjectPath = Resolve-RepoPath -Path $InstallerProjectPath
$resolvedOutputDirectory = Resolve-RepoPath -Path $OutputDirectory
$resolvedIconPath = Resolve-RepoPath -Path 'CoolWSL.App/Assets/AppIcon.ico'
$projectDirectory = Split-Path -Parent $resolvedProjectPath
$resolvedVersion = Get-StableSemanticVersion -Value $Version

if (-not (Test-Path -LiteralPath $resolvedProjectPath -PathType Leaf)) {
    throw "Project file '$resolvedProjectPath' was not found."
}

if (-not (Test-Path -LiteralPath $resolvedInstallerProjectPath -PathType Leaf)) {
    throw "Installer project '$resolvedInstallerProjectPath' was not found."
}

if (-not (Test-Path -LiteralPath $resolvedIconPath -PathType Leaf)) {
    throw "Installer icon '$resolvedIconPath' was not found."
}

$assetBaseName = "CoolWSL-$Version-$RuntimeIdentifier"
$publishDirectory = Join-Path $resolvedOutputDirectory $assetBaseName
$zipPath = Join-Path $resolvedOutputDirectory ($assetBaseName + '.zip')
$msiPath = Join-Path $resolvedOutputDirectory ($assetBaseName + '.msi')
$checksumsPath = Join-Path $resolvedOutputDirectory ($assetBaseName + '.checksums.txt')

Remove-Item -LiteralPath $resolvedOutputDirectory -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $resolvedOutputDirectory -Force | Out-Null

$temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ('coolwsl-installer-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporaryDirectory -Force | Out-Null

$generatedFragmentPath = Join-Path $temporaryDirectory 'CoolWSL.Installer.Files.wxs'
$wixOutputDirectory = Join-Path $temporaryDirectory 'wix-bin'
$wixIntermediateDirectory = Join-Path $temporaryDirectory 'wix-obj'

try {
    $buildArguments = @(
        'build',
        $resolvedProjectPath,
        '-c',
        'Release',
        '-r',
        $RuntimeIdentifier,
        '--no-self-contained',
        '-p:CoolWslDistributionKind=InstallFolder'
    )

    $previousCoolWslVersion = $env:COOLWSL_VERSION
    $coolWslVersionWasSet = $null -ne $previousCoolWslVersion
    $env:COOLWSL_VERSION = $Version

    try {
        & dotnet @buildArguments
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

    $releaseOutputDirectory = Join-Path $projectDirectory (Join-Path 'bin\Release\net10.0-windows10.0.26100.0' $RuntimeIdentifier)
    if (-not (Test-Path -LiteralPath $releaseOutputDirectory -PathType Container)) {
        throw "Expected Release output directory '$releaseOutputDirectory' was not found."
    }

    Copy-Item -LiteralPath $releaseOutputDirectory -Destination $publishDirectory -Recurse -Force

    Get-ChildItem -LiteralPath $publishDirectory -Recurse -File -Filter '*.pdb' |
        Remove-Item -Force

    $mainExecutablePath = Join-Path $publishDirectory 'CoolWSL.App.exe'
    if (-not (Test-Path -LiteralPath $mainExecutablePath -PathType Leaf)) {
        throw "Expected published executable '$mainExecutablePath' was not found."
    }

    New-WixFilesFragment -PublishDirectory $publishDirectory -OutputPath $generatedFragmentPath

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        $publishDirectory,
        $zipPath,
        [System.IO.Compression.CompressionLevel]::Optimal,
        $false)

    $installerBuildArguments = @(
        'build',
        $resolvedInstallerProjectPath,
        '-c',
        'Release',
        '-p:AcceptEula=wix7',
        "-p:GeneratedWixFragmentPath=$generatedFragmentPath",
        "-p:CoolWslInstallerVersion=$($resolvedVersion.InstallerVersion)",
        "-p:CoolWslInstallerIconPath=$resolvedIconPath",
        "-p:OutputPath=$wixOutputDirectory\\",
        "-p:IntermediateOutputPath=$wixIntermediateDirectory\\"
    )

    & dotnet @installerBuildArguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE while producing the MSI installer."
    }

    $builtInstaller = Get-ChildItem -LiteralPath $wixOutputDirectory -Recurse -File -Filter '*.msi' |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $builtInstaller) {
        throw "No .msi file was produced under '$wixOutputDirectory'."
    }

    Copy-Item -LiteralPath $builtInstaller.FullName -Destination $msiPath -Force

    Write-ChecksumsFile -FilePaths @($msiPath, $zipPath) -OutputPath $checksumsPath

    Set-GitHubOutputValue -Name 'app_version' -Value $Version
    Set-GitHubOutputValue -Name 'installer_version' -Value $resolvedVersion.InstallerVersion
    Set-GitHubOutputValue -Name 'publish_directory' -Value $publishDirectory
    Set-GitHubOutputValue -Name 'msi_path' -Value $msiPath
    Set-GitHubOutputValue -Name 'zip_path' -Value $zipPath
    Set-GitHubOutputValue -Name 'checksums_path' -Value $checksumsPath
    Set-GitHubOutputValue -Name 'output_directory' -Value $resolvedOutputDirectory

    Write-Host "App version: $Version"
    Write-Host "Installer version: $($resolvedVersion.InstallerVersion)"
    Write-Host "Install folder: $publishDirectory"
    Write-Host "MSI: $msiPath"
    Write-Host "ZIP: $zipPath"
    Write-Host "Checksums: $checksumsPath"
}
finally {
    Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force -ErrorAction SilentlyContinue
}