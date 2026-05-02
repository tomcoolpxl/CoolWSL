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
- Verified `dotnet build .\CoolWSL.App\CoolWSL.App.csproj -c Debug`, `dotnet test .\CoolWSL.Tests\CoolWSL.Tests.csproj`, and `dotnet run --project .\CoolWSL.App\CoolWSL.App.csproj -c Debug` with `COOLWSL_SMOKE_TEST=1` on 2026-05-02 (UX Phase C).

## UX Phase D - Distro detail rebuild delivered

- Rebuilt `DistroPage` as a `Grid` with a fixed header row and a star-sized content row that hosts a `Pivot`. The header always shows the page title, and an `x:Load`-gated sub-header reveals the selected distro name in `SubtitleTextBlockStyle`, a rounded state pill, and the WSL version, default, and management labels in `SecondaryTextStyle`.
- Removed the in-page distro `ComboBox`. Distro selection now happens through the rail (Phase B) and the Dashboard tile (Phase C); landing on the page without a parameter still falls back to the default-or-first distro through the existing `DistroViewModel.EnsureLoadedAsync` resolution.
- Moved every lifecycle action into the Overview pivot as a settings-card row using `CardBorderStyle`. Each row pairs a Segoe Fluent glyph, a `BodyStrongTextBlockStyle` heading, a `SecondaryTextStyle` description, and a state-aware action button (`AccentButtonStyle` Open, regular Start, regular Terminate with capability gating, regular Set default). The capability message from `DistroCapabilityHelper` renders below the rows so reduced-feature distros surface their reasoning in plain language.
- Rebuilt the command runner inside the Terminal pivot as three vertically-stacked `CardBorderStyle` panels: an input panel (command `TextBox`, timeout `NumberBox`, accent Run button with `Ctrl+Enter`, regular Cancel button with `Escape`, status text, and exit code), a Standard output panel (Consolas `TextBox` plus copy and clear toolbar buttons backed by a new `CommandRunnerViewModel.ClearOutput()` method and `Windows.ApplicationModel.DataTransfer.Clipboard`), and a Standard error panel coloured with `SystemFillColorCriticalBrush` for both the heading icon and the body text.
- Moved session history into a collapsible `Expander` whose content is an `ItemsRepeater` so the list virtualises through `StackLayout` instead of materialising every entry. Each entry tile shows the command in Consolas, the status label and exit code label in `SecondaryTextStyle`, a Reuse button, and the `StartedAt` timestamp in `TertiaryTextStyle`. Reuse now calls `StartBringIntoView()` on the input box before focusing it so the user is dropped at the top of the Terminal pivot ready to press Run.
- Added a Configuration pivot with a placeholder card pointing at implementation Phase 9 so the slot exists in the IA without bringing in the editor itself.
- Moved per-distro diagnostics into the Diagnostics pivot. The pivot keeps the existing `DistroPageDiagnosticsViewModel` wiring; the rendering is now an `ItemsRepeater` of `CardBorderStyle` cards with a rounded severity pill, a Consolas command-text label, and the optional details, suggested next step, command text, and raw output blocks gated by their existing `Has*` properties.
- Replaced every page-level `ListView` with `ItemsRepeater` so wheel events bubble through the surrounding `ScrollViewer` instead of being captured by an inner list scroll viewer. Removed `IsTabStop`/`AllowFocusOnInteraction` from each pivot's `ScrollViewer` and set them to vertical-only.
- Added `CommandRunnerViewModel.ClearOutput()` so the Terminal pivot's clear toolbar button can reset stdout, stderr, and the exit code without touching private setters.
- Verified `dotnet build .\CoolWSL.App\CoolWSL.App.csproj -c Debug`, `dotnet test .\CoolWSL.Tests\CoolWSL.Tests.csproj`, and `dotnet run --project .\CoolWSL.App\CoolWSL.App.csproj -c Debug` with `COOLWSL_SMOKE_TEST=1` on 2026-05-02 (UX Phase D).

## Text rendering fix - Mica removal and Opacity sweep delivered

- Removed `MicaBackdrop` (added in UX Phase A) from `MainWindow`. Mica forces the window root onto a transparent backdrop, which pushes WinUI text rendering into the alpha-composited path and disables ClearType subpixel anti-aliasing - the proximate cause of the page-specific softness reported during UX Phase D.
- Set the `MainWindow` root `Grid` and the slim title bar row to `ApplicationPageBackgroundThemeBrush` so every text surface in the app now sits on an opaque solid theme colour rather than a translucent backdrop. ClearType is restored on light, dark, and high-contrast themes.
- Removed the now-unused `Microsoft.UI.Composition.SystemBackdrops` import and `ConfigureSystemBackdrop()` method from `MainWindow.xaml.cs`.
- Swept the remaining `Opacity="0.72"` attributes (one in `OperationConfirmationDialog`, three on `DiagnosticsPage`) and replaced them with `TextFillColorSecondaryBrush` / `SecondaryTextStyle`. `Opacity` on a `UIElement` forces the subtree into a compositor surface and was contributing to the same alpha-path text rendering. The repository now has zero `Opacity=` attributes in `CoolWSL.App`.
- Verified `dotnet build .\CoolWSL.App\CoolWSL.App.csproj -c Debug`, `dotnet test .\CoolWSL.Tests\CoolWSL.Tests.csproj`, and `dotnet run --project .\CoolWSL.App\CoolWSL.App.csproj -c Debug` with `COOLWSL_SMOKE_TEST=1` on 2026-05-02.

## UX Phase E - Diagnostics consolidation delivered

- Reframed the original "rebuild Diagnostics page" plan as a removal: the per-distro Diagnostics pivot already calls `IDiagnosticsService.GetSnapshotAsync(distroName)`, which returns the full result set including the global checks (`wsl --status`, `wsl --version`, inventory, default distro, host note). Keeping a separate sidebar destination would have duplicated the same data on a second surface, which the user explicitly did not want.
- Removed the `Diagnostics` `NavigationViewItem` from `ShellPage.xaml` and the `"Diagnostics"` switch arm from `ShellPage.OnSelectionChanged`. Dashboard and Settings remain the only fixed global destinations beside the dynamic Distros group.
- Deleted `Views/DiagnosticsPage.xaml`, `Views/DiagnosticsPage.xaml.cs`, and `ViewModels/DiagnosticsViewModel.cs`. Removed the `DiagnosticsViewModel` and `DiagnosticsPage` registrations from `AppServiceCollection`.
- Deleted `Tests/App/Diagnostics/DiagnosticsViewModelTests.cs` (and its now-empty folder) and removed the `DiagnosticsViewModel` assertion from `ServiceRegistrationTests`. Left `AppSection.Diagnostics`, `ShellViewModel`, and `ShellViewModelTests` untouched per the UX Phase B note that those preserve historical IA without blocking the rebuild.
- Left `IDiagnosticsService`, `DiagnosticsService`, all diagnostic models, and `DistroPageDiagnosticsViewModel` untouched. The per-distro Diagnostics pivot continues to render every result the old page used to show, including the global ones, with the selected distro as context.
- Updated `REQUIREMENTS.md` (User Experience Model, Global Destinations, MVP Shell Navigation, MVP Diagnostics, Diagnostics UX, MVP Acceptance Criteria), `ARCHITECTURE.md` (ADR 0002 Decision and Delivery Implications), and `DESIGN.md` (Overview, Honest diagnostics principle, Shell Navigation, Health Summary, Diagnostics Pivot subsection) to describe Diagnostics as living only inside the per-distro pivot. Removed the standalone `## Diagnostics` section from `DESIGN.md` and folded its severity-grouped triage layout into the per-distro Diagnostics Pivot description.
- Verified `dotnet build .\CoolWSL.sln -c Debug`, `dotnet test .\CoolWSL.Tests\CoolWSL.Tests.csproj` (49 passed), and `dotnet run --project .\CoolWSL.App\CoolWSL.App.csproj -c Debug` with `COOLWSL_SMOKE_TEST=1` (exit 0) on 2026-05-02 (UX Phase E).

## UX Phase F - Settings and Logs pages delivered

- Replaced the `Settings` `PlaceholderPage` route with a real `SettingsPage` registered in DI and reached from the shell footer. The page uses Windows-Settings-style `Expander` groups for WSL, Appearance, Behaviour, Diagnostics, Logs, and About, with card surfaces, Fluent icons, refresh support, repository/issue links, and safe global WSL actions.
- Added `SettingsViewModel` so the Settings page loads live WSL status, default distro, installed/running distro counts, last-refresh text, and action status through the existing `IDashboardStatusService` and `IWslDistroService` boundaries.
- Reintroduced `Logs` in the shell footer and added a real `LogsPage` with refresh, level filtering, text search, empty state, and an `ItemsRepeater` log list. The page is backed by `LogsViewModel` and no longer uses `PlaceholderPage`.
- Replaced the default `NullAppLogger` registration with `FileAppLogger`, a metadata-only `IAppLogger` / `IAppLogReader` implementation that writes JSON-line daily log files under `%LocalAppData%\CoolWSL\Logs`, keeps command output out of the log, prunes files older than the 30-day retention window, and returns newest entries first for the Logs page.
- Added `AppLogEntry`, `IAppLogReader`, and `LogEntryRow`, updated service-registration coverage for `IAppLogReader`, `LogsViewModel`, and `SettingsViewModel`, and added focused `FileAppLoggerTests` for metadata storage and newest-first ordering.
- Replaced the remaining `PlaceholderPage` `<ScrollView>` with the app-standard vertical `<ScrollViewer>` so any future placeholder route keeps the same mouse-wheel and focus behavior as the rebuilt pages.
- Updated `REQUIREMENTS.md`, `ARCHITECTURE.md`, and `DESIGN.md` so the documented shell and requirements now describe Dashboard, Logs, Settings, and dynamic distro entries as the current fixed navigation model.
- Verified `dotnet build .\CoolWSL.App\CoolWSL.App.csproj -c Debug --no-restore -m:1 /p:UseSharedCompilation=false`, `dotnet test .\CoolWSL.Tests\CoolWSL.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false` (51 passed), and `dotnet run --project .\CoolWSL.App\CoolWSL.App.csproj -c Debug --no-restore --no-build` with `COOLWSL_SMOKE_TEST=1` on 2026-05-02 (UX Phase F).

## Phase 8 - Raw global WSL configuration editor delivered

- Added `IWslGlobalConfigService` and `WslGlobalConfigService` for `%UserProfile%\.wslconfig` read, conservative validation, backup-before-overwrite save, and missing-file handling.
- Added raw `.wslconfig` editing to the Settings WSL section with file path, existence state, create-file draft, save, revert, validation output, backup path output, and restart-required messaging that does not automatically shut down WSL.
- Kept raw text as the source of truth so comments, unknown sections, unknown keys, ordering, and whitespace are preserved on save unless the user changes them.
- Added metadata logging for successful global config saves without logging file contents.
- Added automated coverage for valid config parsing, malformed syntax, unknown-key preservation warnings, missing-file reads, raw serialization without normalization, backup creation, malformed save blocking, Settings revert behavior, and save disabling for malformed editor content.
- Verified current WSL configuration behavior against Microsoft Learn's WSL advanced settings documentation on 2026-05-02: `.wslconfig` lives under the user profile, applies globally to WSL 2, and changes require WSL restart/shutdown semantics.
- Verified `dotnet build .\CoolWSL.App\CoolWSL.App.csproj -c Debug --no-restore -m:1 /p:UseSharedCompilation=false`, `dotnet test .\CoolWSL.Tests\CoolWSL.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false -v:q` (60 passed), and `dotnet run --project .\CoolWSL.App\CoolWSL.App.csproj -c Debug --no-restore --no-build` with `COOLWSL_SMOKE_TEST=1` on 2026-05-02.

## Phase E1.6 - Runtime validation probes and global summary card

- Implemented `ProbeAsync` in `WslDistroConfigService` running explicit verification commands via `wsl --exec`.
- Updated `DistroSettingsViewModel` to orchestrate validation probe execution and apply results to `DistroSettingsRowViewModel`.
- Added the Global WSL settings summary card at the top of the Settings pivot with read-only indicators and a fallback handoff to the official WSL Settings app.

## Phase E1.7 - Polish and smoke verification

- Verified all `dotnet build` and `dotnet test` output passes clean on 2026-05-02.
- Verified `dotnet run --project .\CoolWSL.App\CoolWSL.App.csproj -c Debug` with `COOLWSL_SMOKE_TEST=1` finishes successfully on 2026-05-02.

## UX fix pass - layout stability, clearer distro labels, and settings simplification delivered

- Stabilized the main page content hosts by centering the Dashboard, Settings, Logs, and Distro-page pivot content inside named width-bound containers, which removes the prior drift between unconstrained and max-width layouts.
- Replaced raw per-distro WSL version integers with explicit `WSL 1` / `WSL 2` labels so the dashboard tiles and distro header no longer show stray standalone numbers.
- Hardened dashboard distro-tile navigation by tagging each tile with the distro name and using that explicit target for both rail selection and direct `DistroPage` fallback navigation.
- Reworked per-distro configuration verification so probe results render in plain language, show evidence when available, and clear automatically on reload or edit instead of leaving stale `NotEffective` text behind.
- Simplified the global WSL settings surface so the Settings page now shows `.wslconfig` read-only when present, states clearly when the file is missing, and routes editing through the official WSL Settings app instead of exposing create, revert, and save controls in CoolWSL.
- Removed the redundant Diagnostics and Logs cards from Settings so those workflows remain in their dedicated destinations rather than being duplicated on the global settings surface.
- Verified the updated global-settings guidance against Microsoft Learn on 2026-05-03, which recommends using the WSL Settings app for `.wslconfig` changes and confirms that `.wslconfig` applies to WSL 2 only and takes effect after WSL restarts.
- Verified `dotnet test .\CoolWSL.Tests\CoolWSL.Tests.csproj -c Debug --filter "FullyQualifiedName~DashboardStateTests|FullyQualifiedName~DistroSelectionItemTests"`, `dotnet test .\CoolWSL.Tests\CoolWSL.Tests.csproj -c Debug --filter "FullyQualifiedName~SettingsViewModelTests|FullyQualifiedName~DistroSettingsViewModelTests|FullyQualifiedName~DistroSettingsRowViewModelTests"`, and `dotnet run --project .\CoolWSL.App\CoolWSL.App.csproj -c Debug --no-build` with `COOLWSL_SMOKE_TEST=1` on 2026-05-03.

## Code review remediation - CODE_REVIEW findings resolved

- Reordered `WslCommandService` redirected output reads ahead of stdin writes so large stdin payloads cannot deadlock behind unread stdout/stderr, and added focused stdin regression coverage.
- Updated `DistroSettingsViewModel` to cancel superseded loads via the existing refresh-coordination pattern, reject stale completions, reuse one shared global-summary and capability-loading path across load and save, and replace the nested WSL Settings launcher fallback with an ordered loop.
- Preserved raw quoted INI values in the document model, added an explicit unquoted semantic value for validation and structured-editor consumers, and tightened serialization to reuse `RawLine` only when an entry is unchanged.
- Reduced localized distro-state degradation by combining `wsl.exe --list --verbose` with `wsl.exe --list --running --quiet` so running and stopped states can still be inferred when verbose state labels are localized.
- Verified `dotnet test .\CoolWSL.Tests\CoolWSL.Tests.csproj -c Debug` (77 passed) and `dotnet run --project .\CoolWSL.App\CoolWSL.App.csproj -c Debug --no-build` with `COOLWSL_SMOKE_TEST=1` on 2026-05-02.
