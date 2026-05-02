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

## Phase 5 - Safe lifecycle actions delivered

- Extended `IWslDistroService`, `WslCommandFactory`, and `WslDistroService` with open-default, open-distro, start, terminate, set-default, shutdown, and in-distro execution operations while preserving shell-safe argument handling.
- Added dashboard and per-distro lifecycle controls with capability gating for running, default, and system-managed distros plus plain-language action status feedback.
- Added shared `OperationRequest` and `OperationConfirmationDialog` flows for terminate and shutdown-all confirmations with explicit target and impact text.
- Added focused automated coverage for lifecycle command construction, service execution, dashboard action refresh behavior, and DI registration.
- Verified `dotnet build .\CoolWSL.App\CoolWSL.App.csproj -c Debug`, `dotnet build .\CoolWSL.sln -c Debug`, and `dotnet test .\CoolWSL.Tests\CoolWSL.Tests.csproj` on 2026-05-01.

## Phase 6 - Per-distro overview and command runner delivered

- Added the Distros navigation surface with a per-distro header that shows name, state, WSL version, default status, management status, and lifecycle capability messaging.
- Added the per-distro command runner with run, cancel, timeout, stdout, stderr, exit code, and per-distro in-memory session history.
- Fixed the command-runner cancellation status race so a completed cancelled result is not overwritten by the transient cancelling message.
- Added focused automated coverage for successful, timed-out, and cancelled in-distro command execution flows.
- Verified `dotnet build .\CoolWSL.sln -c Debug`, `dotnet test .\CoolWSL.Tests\CoolWSL.Tests.csproj`, and a Windows UI Automation spot check that confirmed the Distros page renders and is reachable from shell navigation on 2026-05-01.

## Phase 7 - MVP diagnostics delivered

- Added `IDiagnosticsService`, `DiagnosticsService`, `DiagnosticsSnapshot`, `DiagnosticResult`, `DiagnosticSeverity`, and `DiagnosticSummaryMapper` to collect global WSL diagnostics plus per-distro DNS and internet probes.
- Added the Diagnostics page and the per-distro diagnostics section on the Distros page with plain-language summaries, raw evidence, retry controls, and conservative handling for unsupported probe tools and degraded WSL metadata.
- Fixed the WinUI XAML compiler blocker without dropping compiled bindings by bringing all `x:Load` usages on the new pages into compliance with the documented `x:Name` requirement.
- Fixed a post-delivery runtime regression where redirected host-side `wsl.exe` query output was decoded with the wrong encoding, which caused the Dashboard, Distros, and Diagnostics pages to show degraded or empty metadata despite healthy local WSL state.
- Added focused automated coverage for diagnostics selection, default-distro fallback, warning/error mapping, and service registration.
- Verified `dotnet build .\CoolWSL.App\CoolWSL.App.csproj -c Debug`, `dotnet build .\CoolWSL.sln -c Debug`, `dotnet test .\CoolWSL.Tests\CoolWSL.Tests.csproj`, `dotnet run --project .\CoolWSL.App\CoolWSL.App.csproj -c Debug` with `COOLWSL_SMOKE_TEST=1`, and a Windows UI Automation spot check that confirmed the Diagnostics page renders and is reachable from shell navigation on 2026-05-01.

## UX Phase A - Foundation styles, Mica backdrop, and slim title bar delivered

- Added an app-wide resource dictionary in `App.xaml` with spacing tokens (`SpacingXS` through `SpacingXXL`), a `CardBorderStyle` for opaque rounded card surfaces using `CardBackgroundFillColorDefaultBrush` and `CardStrokeColorDefaultBrush`, and `SecondaryTextStyle` / `TertiaryTextStyle` derived from `TextFillColorSecondaryBrush` / `TextFillColorTertiaryBrush` so de-emphasised typography no longer relies on `Opacity` (which disables ClearType subpixel rendering).
- Kept `XamlControlsResources` in `MergedDictionaries` so the documented `NavigationView` startup-crash protection from `GEMINI.md` is preserved.
- Replaced the 48 px custom-bordered title bar with a slim 32 px drag region containing a Segoe Fluent Icons accent glyph and a `CaptionTextBlockStyle` "CoolWSL" label; dropped the activation-state opacity dimming code that no longer applied.
- Replaced the hard-coded RGBA title-bar button hover and pressed colours with `SubtleFillColorSecondaryBrush` and `SubtleFillColorTertiaryBrush` resolved from the application resources, so the chrome respects light, dark, and high-contrast themes.
- Added `MicaBackdrop` (`MicaKind.Base`) to `MainWindow` behind a `MicaController.IsSupported()` guard.
- Verified `dotnet build .\CoolWSL.App\CoolWSL.App.csproj -c Debug`, `dotnet test .\CoolWSL.Tests\CoolWSL.Tests.csproj`, and `dotnet run --project .\CoolWSL.App\CoolWSL.App.csproj -c Debug` with `COOLWSL_SMOKE_TEST=1` on 2026-05-02 (UX Phase A).

## UX Phase B - Shell rebuild with distros in the rail and a persistent status bar delivered

- Rebuilt `ShellPage` with a Windows-Settings-style `NavigationView`: fixed `Dashboard` and `Diagnostics` items with Segoe Fluent Icons, a `NavigationViewItemHeader` for the dynamic `Distros` group, and a `FooterMenuItems` `Settings` entry.
- Added per-distro `NavigationViewItem`s populated on shell load from `IDashboardStatusService.GetSnapshotAsync()`. Each item is tagged with its `WslDistro` instance so the selection handler can navigate to `DistroPage` with the distro name as the navigation parameter, which `DistroPage.OnNavigatedTo` already feeds into `DistroViewModel.EnsureLoadedAsync(preferredDistroName)`.
- Removed `Logs` from the rail since it still resolves to `PlaceholderPage`; it will be reintroduced once `IAppLogger` is implemented in a later UX phase.
- Stripped `ScrollViewer.VerticalScrollMode="Disabled"` and `ScrollViewer.HorizontalScrollMode="Disabled"` from the content `Frame` so wheel events can flow through the shell once individual pages stop nesting `ListView`s in their page-level `ScrollViewer`s.
- Added a persistent bottom-of-window `StatusBar` UserControl backed by `StatusBarViewModel` (singleton) that derives `WslStatusText`, `DistroSummary`, `DefaultDistroText`, `LastRefreshedText`, and an availability-coloured indicator brush (`SystemFillColorSuccessBrush` / `SystemFillColorCautionBrush` / `SystemFillColorCriticalBrush`) from a `DashboardStatusSnapshot`.
- Updated `MainWindow` to host the status bar in a third `Auto`-height row and updated `AppServiceCollection` to register `StatusBarViewModel` and the `StatusBar` UserControl. Left `ShellViewModel` and the existing `ShellViewModelTests` untouched so the smoke test keeps asserting historical IA without blocking the new XAML.
- Verified `dotnet build .\CoolWSL.App\CoolWSL.App.csproj -c Debug`, `dotnet test .\CoolWSL.Tests\CoolWSL.Tests.csproj`, and `dotnet run --project .\CoolWSL.App\CoolWSL.App.csproj -c Debug` with `COOLWSL_SMOKE_TEST=1` on 2026-05-02 (UX Phase B).

## UX Phase C - Dashboard rebuild delivered

- Replaced the naked `Border` "cards" on `DashboardPage` with `CardBorderStyle` (opaque `CardBackgroundFillColorDefaultBrush`, 1 px stroke, 8 px corner radius) so ClearType subpixel text rendering is restored on the dashboard surfaces.
- Replaced every `Opacity="0.72"` text on the dashboard with `SecondaryTextStyle` and `TertiaryTextStyle` so de-emphasised typography no longer forces compositor rasterisation and respects high-contrast theme brushes.
- Replaced the distro inventory `ListView` with an `ItemsRepeater` (`StackLayout`, 8 px spacing) so mouse-wheel events bubble to the page-level `ScrollViewer` instead of being swallowed by a nested list scroll viewer.
- Collapsed the per-row Open / Start / Terminate / Set Default button stack into a single full-width clickable distro tile styled by a new shared `TileButtonStyle`. Clicking the tile updates the rail `NavigationView.SelectedItem` (so the rail highlights the chosen distro and `ShellPage` performs the navigation) with a `Frame.Navigate(typeof(DistroPage), name)` fallback when the rail cannot be located.
- Re-laid out the dashboard top-to-bottom: page header with refresh button and progress ring, last-refresh caption, hero status card (availability label, summary, optional warning, optional suggested next step, a divider, and the WSL / kernel / default-WSL-version detail grid), action status text, primary `AccentButtonStyle` "Open default terminal" plus regular "Shutdown all WSL" buttons (each with a Segoe Fluent glyph), distro section header and summary, optional empty-state card, and the new tile repeater.
- Each distro tile renders the distro name in `BodyStrongTextBlockStyle`, optional Default and management badges next to it, the existing capability message in `SecondaryTextStyle`, a rounded state pill backed by `ControlFillColorSecondaryBrush` + `CardStrokeColorDefaultBrush`, and the WSL version on the right. The tile button surfaces `OpenAutomationName` for screen readers.
- Removed `IsTabStop="True"` and `AllowFocusOnInteraction="True"` from the page-level `ScrollViewer` so the scroll surface is no longer a Tab stop and no longer steals focus on click. Set `HorizontalScrollMode="Disabled"` and `HorizontalScrollBarVisibility="Disabled"` since the page is vertical-only.
- Verified `dotnet build .\CoolWSL.App\CoolWSL.App.csproj -c Debug`, `dotnet test .\CoolWSL.Tests\CoolWSL.Tests.csproj`, and `dotnet run --project .\CoolWSL.App\CoolWSL.App.csproj -c Debug` with `COOLWSL_SMOKE_TEST=1` on 2026-05-02.
