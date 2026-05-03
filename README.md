# CoolWSL

WSL Control Center for Windows 11.

## Status

Phase 2 solution skeleton is implemented and verified. The repository now contains the WinUI 3 shell, project boundaries, baseline smoke tests, and an automated Debug smoke-launch path for non-interactive startup verification.

## Approved Delivery Baseline

- Packaged WinUI 3 desktop app using single-project MSIX.
- Framework-dependent Windows App SDK `2.0.1`, with future updates staying on the supported `2.0.x` servicing line until a later upgrade phase says otherwise.
- `.NET 10` LTS SDK targeting `net10.0-windows10.0.26100.0`.
- Minimum OS: Windows 11 24H2 (build 26100) with current cumulative updates.
- Minimum WSL floor: Microsoft Store WSL `0.67.6` or later.
- WSL1 distros stay visible, but WSL2-only features are gated with explicit explanations.
- Docker Desktop distros stay visible, but are treated as system-managed and excluded from destructive and config-editing flows by default.
- Metadata-only logs are retained for 30 days by default, and command output stays opt-in.

## Delivery Notes

- Initial install and update flow is signed MSIX sideload, with optional `.appinstaller` support for direct-update scenarios.
- App-owned logs, settings, temp files, and future persistent profiles live under `%LocalAppData%\CoolWSL\`.
- Exports and user backups remain explicit, user-chosen locations rather than package-owned storage.
- The initial release stays unelevated. Admin-only operations are disabled and explained instead of triggering self-elevation.
- The repo defaults the app version to `0.1.0`. Set `COOLWSL_VERSION` before build or packaging if you want to stamp a newer assembly/About version without editing project files.

## Local Prerequisites

- Windows 11 24H2 (build 26100) or later.
- .NET 10 SDK.
- Windows App Runtime `2.0.1` x64.
- Developer Mode or another sideload-enabled policy if you want to register the Release MSIX package locally.

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
```

Debug runs as an unpackaged native WinUI 3 desktop app so startup can be verified non-interactively. Release keeps single-project MSIX packaging enabled for the signed sideload delivery path.

See `ARCHITECTURE.md` for the full decision record and rationale.
