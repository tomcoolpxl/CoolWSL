# CoolWSL Done

## Phase 1 - Delivery baseline and packaging decision ratified

- Chose packaged WinUI 3 delivery using single-project MSIX and framework-dependent Windows App SDK `2.0.1`, with signed MSIX sideload as the initial install path and optional `.appinstaller` support for direct updates.
- Locked `.NET 10` LTS, planned target `net10.0-windows10.0.26100.0`, Windows 11 24H2 (build 26100), and Microsoft Store WSL `0.67.6+` as the supported baseline.
- Defined baseline behavior for WSL1 distros, Docker Desktop distros, admin-only actions, persistent app-data paths, and 30-day metadata-only log retention.
- Added `ARCHITECTURE.md` and refreshed README.md, REQUIREMENTS.md, DESIGN.md, IMPLEMENTATION_PLAN.md, and TODO.md to reflect the ratified baseline.
- Verified the decisions against current Microsoft Learn guidance for Windows App SDK deployment, Windows app distribution, Windows 11 lifecycle and build numbers, WSL commands, and WSL configuration behavior on 2026-05-01.

## Phase 2 - Buildable WinUI solution skeleton established

- Created `CoolWSL.sln` plus the initial `CoolWSL.App`, `CoolWSL.Core`, `CoolWSL.Wsl`, `CoolWSL.Configuration`, `CoolWSL.Diagnostics`, and `CoolWSL.Tests` projects.
- Wired the WinUI 3 app entry point, main window, shell page, navigation frame, and dependency injection bootstrap for the initial Windows 11 shell.
- Added baseline shared models, service-registration boundaries, and smoke tests without implementing WSL behavior yet.
- Verified `dotnet build .\CoolWSL.sln -c Debug`, `dotnet build .\CoolWSL.sln -c Release`, and `dotnet test .\CoolWSL.Tests\CoolWSL.Tests.csproj -c Release` on 2026-05-01.
- Fixed the startup crash caused by missing `XamlControlsResources`, and added an automated Debug smoke-launch mode that writes a marker file and exits cleanly after first window activation.
- Refreshed README.md with local prerequisites, build and test commands, and the non-interactive smoke-launch command.

## Phase 3 - Safe WSL execution and parsing foundation implemented

- Added the core WSL command, result, distro, and environment models plus `IWslCommandService` and `IWslDistroService` abstractions for later UI slices.
- Implemented `WslCommandService` with shell-safe argument passing via `ProcessStartInfo.ArgumentList`, stdout and stderr capture, exit code handling, timeout and cancellation handling, and metadata-only command logging.
- Implemented WSL command builders and parsers for `wsl --list --verbose`, `wsl --status`, and `wsl --version` with explicit degraded behavior for unsupported or unrecognized output.
- Added `WslErrorMapper` plain-language failure mapping and `WslDistroService` inventory and environment queries backed by the new execution layer.
- Added focused automated coverage for command building, parser fixtures, timeout handling, cancellation handling, error mapping, service registration, and WSL environment and inventory mapping.
- Verified `dotnet build .\CoolWSL.sln -c Debug` and `dotnet test .\CoolWSL.Tests\CoolWSL.Tests.csproj` on 2026-05-01.

## Phase 4 - Dashboard inventory slice delivered

- Added a read-only dashboard page, state model, refresh coordinator, and view model that load WSL environment status and distro inventory through a shared dashboard status service.
- Added dashboard UI for WSL availability, WSL version, kernel version, default WSL version, and distro inventory rows with explicit empty, unavailable, and degraded-state messaging.
- Kept refresh behavior safe by preserving existing dashboard state during reloads and ignoring superseded refresh results.
- Added focused automated coverage for healthy, unavailable, no-distro, degraded, and refresh-race dashboard states plus DI coverage for the new dashboard status service.
- Verified `dotnet build .\CoolWSL.sln -c Debug`, `dotnet test .\CoolWSL.Tests\CoolWSL.Tests.csproj`, and `dotnet run --project CoolWSL.App/CoolWSL.App.csproj -c Debug` with `COOLWSL_SMOKE_TEST=1` on 2026-05-01.
- Completed a Windows UI Automation spot check that confirmed the running dashboard exposes readable status text and a keyboard-focusable `Refresh` button.
