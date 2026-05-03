# CoolWSL

WSL Control Center for Windows 11.

## Status

Installer-first release packaging is implemented and verified locally. The repository now publishes a self-contained install folder plus MSI, ZIP, and checksums artifacts suitable for GitHub Releases and winget manifests.

## Approved Delivery Baseline

- Unpackaged WinUI 3 desktop app published as a self-contained install folder.
- Framework-dependent Windows App SDK `2.0.1`, with future updates staying on the supported `2.0.x` servicing line until a later upgrade phase says otherwise.
- `.NET 10` LTS SDK targeting `net10.0-windows10.0.26100.0`.
- Minimum OS: Windows 11 24H2 (build 26100) with current cumulative updates.
- Minimum WSL floor: Microsoft Store WSL `0.67.6` or later.
- WSL1 distros stay visible, but WSL2-only features are gated with explicit explanations.
- Docker Desktop distros stay visible, but are treated as system-managed and excluded from destructive and config-editing flows by default.
- Metadata-only logs are retained for 30 days by default, and command output stays opt-in.

## Delivery Notes

- Initial install flow is installer-first: MSI for standard install and ZIP for portable-style extraction.
- Release automation publishes three assets per tag: `.msi`, `.zip`, and `.checksums.txt`.
- App-owned logs, settings, temp files, and future persistent profiles live under `%LocalAppData%\CoolWSL\`.
- Exports and user backups remain explicit, user-chosen locations rather than package-owned storage.
- The initial release stays unelevated. Admin-only operations are disabled and explained instead of triggering self-elevation.
- The repo defaults the app version to `0.1.0`. Set `COOLWSL_VERSION` before build or packaging if you want to stamp a newer assembly/About version without editing project files.

## Local Prerequisites

- Windows 11 24H2 (build 26100) or later.
- .NET 10 SDK.
- Windows App Runtime `2.0.1` x64.
- WiX Toolset support via the repository's `WixToolset.Sdk` restore path.

## Local Validation

```powershell
dotnet restore .\CoolWSL.sln
dotnet build .\CoolWSL.sln -c Debug
dotnet build .\CoolWSL.sln -c Release
dotnet test .\CoolWSL.Tests\CoolWSL.Tests.csproj -c Release

$marker = Join-Path $env:TEMP 'coolwsl-smoke-marker.txt'
Remove-Item $marker -ErrorAction SilentlyContinue
$env:COOLWSL_SMOKE_TEST = '1'
$env:COOLWSL_SMOKE_TEST_FILE = $marker

Start-Process -FilePath .\CoolWSL.App\bin\Debug\net10.0-windows10.0.26100.0\win-x64\CoolWSL.App.exe -PassThru | Wait-Process
Get-Content $marker

Remove-Item Env:COOLWSL_SMOKE_TEST
Remove-Item Env:COOLWSL_SMOKE_TEST_FILE

pwsh -NoProfile -File .\build\Invoke-ReleaseInstaller.ps1 -Version 0.1.3 -OutputDirectory artifacts\release-local-installer
```

Debug runs as an unpackaged native WinUI 3 desktop app so startup can be verified non-interactively. Release packaging uses the install-folder publish mode and wraps that output as MSI and ZIP.

See `ARCHITECTURE.md` for the full decision record and rationale.

## CI And Releases

- GitHub Actions CI runs on pushes to `main`, pull requests targeting `main`, merge-queue checks, and manual dispatches.
- GitHub Actions release packaging runs when you push a stable SemVer tag in the form `vX.Y.Z`.
- The release workflow stamps the managed app version from the tag, builds a self-contained install folder, generates a WiX MSI, creates a ZIP from the install folder, writes SHA-256 checksums, uploads artifacts, and publishes them to the matching GitHub Release with generated notes.

## Release Secrets

- Installer-first release assets do not require a code-signing certificate secret to build.
- If you later add MSI signing, introduce signing secrets at that point and document the signing step in the release workflow.

## Release Command

```powershell
git tag v0.2.0
git push origin v0.2.0
```

That tag triggers the release workflow, which runs restore, tests, signed packaging, artifact upload, and `gh release create --generate-notes`.

## Winget Notes

- For installer manifests, use the GitHub Release `.msi` asset URL as installer URL and the corresponding hash from `.checksums.txt`.
- The `.zip` asset can be used for a portable distribution flow where appropriate.
- Use stable `vX.Y.Z` tags so app versioning, release asset names, and winget manifest versions stay aligned.
