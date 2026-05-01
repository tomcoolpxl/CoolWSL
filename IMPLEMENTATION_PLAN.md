# CoolWSL Implementation Plan

<!-- markdownlint-disable MD024 MD032 -->

## Overview

CoolWSL is planned as a Windows 11 WinUI 3 desktop application in C#/.NET that provides a safe WSL control center for local machines. The delivery goal is to implement the documented MVP first and then the approved 1.0 extensions without relying on undocumented WSL internals, brittle registry scraping, or unsafe VHD manipulation.

This plan is derived from REQUIREMENTS.md, DESIGN.md, and README.md. The current repository appears documentation-only, so early phases establish the solution, build path, and safe WSL abstraction layer before user-facing features land.

Key constraints that shape the plan:
- Windows 11 only, WSL2 first, with explicit degraded behavior when WSL is unavailable, outdated, or partially supported.
- Supported backends only: wsl.exe, wslapi.dll where justified, %UserProfile%\.wslconfig, /etc/wsl.conf, and documented Windows process and performance APIs.
- Safety-first UX for destructive operations, config writes, exports, imports, and any action that can affect boot, networking, or data.
- Documentation-driven workflow: TODO.md must be refreshed from the active phase before implementation starts, and DONE.md may only contain verified work.
- Packaged MSIX local and direct distribution is the ratified baseline; no cloud service or backend is in scope.

## Assumptions

- Project name is CoolWSL.
- Project type is a Windows 11 WinUI 3 desktop application written in C# on .NET.
- Short delivery goal is to ship a safe local WSL control center that covers the documented MVP and then the 1.0 enhancements.
- Existing authoritative docs are README.md, REQUIREMENTS.md, DESIGN.md, TODO.md, DONE.md, and ARCHITECTURE.md.
- Deployment target is packaged MSIX for local and direct Windows desktop distribution. Microsoft Store submission may be added later, but is not assumed for initial delivery.
- Overall risk tolerance is medium, but tolerance for data loss, config corruption, and misleading diagnostics is low.
- Review cadence is one phase per review cycle. A phase must fit in one review package that can be built, tested, and manually checked in a single cycle.
- The initial repository does not yet contain the WinUI solution or project files described in REQUIREMENTS.md.
- `dotnet build` and `dotnet test` are assumed to be the primary local build and test commands once the solution exists.

## Delivery strategy

This plan uses a hybrid strategy.

The first two phases are layered foundation work because the project cannot be safely sliced vertically until the packaging model, solution structure, and safe WSL command boundary are fixed. After that, the plan shifts to thin vertical slices that each deliver one reviewable user outcome: inventory, lifecycle actions, command execution, diagnostics, config editing, export, and then the 1.0 extensions.

This strategy fits the project type because a Windows desktop control surface for WSL needs strong shared foundations for process execution, parsing, config safety, and error handling before user-facing actions become trustworthy. It also fits the assumed review cadence because each later phase ends in one user-visible capability that can be reviewed with focused automated checks plus a bounded manual smoke pass.

## Phase list

- Phase 1 - Delivery baseline and packaging decision ratified
- Phase 2 - Buildable WinUI solution skeleton established
- Phase 3 - Safe WSL execution and parsing foundation implemented
- Phase 4 - Dashboard inventory slice delivered
- Phase 5 - Safe lifecycle actions delivered
- Phase 6 - Per-distro overview and command runner delivered
- Phase 7 - MVP diagnostics delivered
- Phase 8 - Raw global WSL configuration editor delivered
- Phase 9 - Raw per-distro WSL configuration editor delivered
- Phase 10 - Distro export workflow delivered
- Phase 11 - Structured global settings UI delivered
- Phase 12 - Structured per-distro settings UI delivered
- Phase 13 - Systemd service management delivered
- Phase 14 - Detailed networking diagnostics delivered
- Phase 15 - Filesystem visibility and safe disk operations delivered
- Phase 16 - Import, clone, and destructive distro management delivered
- Phase 17 - Health-aware dashboard enhancements delivered
- Phase 18 - User preferences and command profiles delivered
- Phase 19 - Stabilization, packaging verification, and final review completed

## Detailed phases

### Phase 1 - Delivery baseline and packaging decision ratified

#### Goal

Lock the technical delivery baseline that all later implementation depends on.

#### Scope

- Decide packaged versus unpackaged WinUI 3 delivery and framework-dependent versus self-contained Windows App SDK deployment.
- Lock the target .NET SDK version, Windows App SDK version, minimum Windows 11 build, and minimum WSL feature floor.
- Decide the baseline handling for WSL1 distros, Docker Desktop distros, admin-only operations, and log retention defaults.
- Record the approved constraints and capability gates in project documentation.
- End the phase with approved decisions that unblock solution scaffolding and remove packaging ambiguity.

#### Expected files to change

- README.md
- REQUIREMENTS.md
- DESIGN.md
- IMPLEMENTATION_PLAN.md
- TODO.md
- DONE.md
- ARCHITECTURE.md or a docs/architecture decision record if the team chooses to add one

#### Dependencies

- Existing repository docs must be reviewed first.
- Microsoft documentation for WSL commands, WSL config files, and Windows App SDK deployment must be consulted.
- This phase has no earlier project phase dependency.
- Blockers: unresolved packaged versus unpackaged choice; unresolved minimum platform and WSL version floor.

#### Risks

- Medium. A wrong packaging or version-floor decision will force rework across app startup, file access, elevation prompts, and release packaging.
- Main failure modes are choosing a deployment model that conflicts with required file and process access, or committing to features that are unsupported on the chosen Windows and WSL baseline.

#### Tests and checks to run

- Documentation traceability review against REQUIREMENTS.md and DESIGN.md
- Feasibility review against Microsoft documentation for Windows App SDK deployment
- Feasibility review against Microsoft documentation for `wsl --list --verbose`, `wsl --status`, `wsl --version`, `wsl --shutdown`, `wsl --terminate`, `wsl --export`, and the .wslconfig and /etc/wsl.conf formats
- No code build is expected in this phase

#### Review check before moving work to DONE.md

- Confirm the phase output is documentation and decisions only, with no hidden implementation work.
- Confirm every approved decision maps back to an explicit requirement, design constraint, or documented Microsoft capability.
- Review the risk of choosing the wrong packaging model, version floor, or feature gate before accepting the decision record.
- Confirm README.md, REQUIREMENTS.md, DESIGN.md, and any architecture note were updated if the approved baseline changes them.
- Confirm no extra features were introduced while resolving platform questions.
- Confirm any unanswered follow-up work was written back to TODO.md as explicit later-phase work.
- Confirm the reviewer agrees the documented baseline fully matches this phase goal and scope.

#### Exact TODO.md entries to refresh from this phase

- [ ] Choose packaged or unpackaged WinUI 3 delivery and record the implications for file access, elevation, install and update flow, and release packaging.
- [ ] Lock the target .NET SDK, Windows App SDK version, minimum Windows 11 build, and minimum WSL feature floor.
- [ ] Decide baseline handling for WSL1 distros, Docker Desktop distros, admin-only actions, and default log retention.
- [ ] Refresh README.md, REQUIREMENTS.md, DESIGN.md, and any architecture note with the approved delivery baseline.

#### Exit criteria for moving items to DONE.md

- The delivery model decision is documented, reviewed, and leaves no unresolved blocker for creating the solution.
- The target SDK and platform baseline are written in project docs and approved in review.
- WSL1, Docker Desktop distro handling, admin-only behavior, and log retention defaults are documented as explicit rules rather than implicit assumptions.
- All affected docs are updated and the reviewer confirms the phase outcome matches the goal and scope.

### Phase 2 - Buildable WinUI solution skeleton established

#### Goal

Create a buildable WinUI solution and application shell that later slices can extend without structural rework.

#### Scope

- Create the solution and project structure suggested in REQUIREMENTS.md.
- Add the WinUI application entry point, main window, shell page, navigation frame, dependency injection setup, and placeholder logging abstraction.
- Add the initial shared models and service-registration boundaries without implementing WSL behavior yet.
- Add the test project and the smallest possible smoke tests for solution health.
- Document the local build prerequisites and build and test commands.
- End the phase with an app that launches to an empty but working shell and a solution that builds cleanly.

#### Expected files to change

- CoolWSL.sln
- CoolWSL.App/CoolWSL.App.csproj
- CoolWSL.App/App.xaml
- CoolWSL.App/App.xaml.cs
- CoolWSL.App/MainWindow.xaml
- CoolWSL.App/MainWindow.xaml.cs
- CoolWSL.App/Views/ShellPage.xaml
- CoolWSL.App/ViewModels/ShellViewModel.cs
- CoolWSL.Core/CoolWSL.Core.csproj
- CoolWSL.Wsl/CoolWSL.Wsl.csproj
- CoolWSL.Configuration/CoolWSL.Configuration.csproj
- CoolWSL.Diagnostics/CoolWSL.Diagnostics.csproj
- CoolWSL.Tests/CoolWSL.Tests.csproj
- Directory.Build.props
- README.md

#### Dependencies

- Phase 1 must be complete because project structure depends on the approved packaging and runtime model.
- The Windows SDK, .NET SDK, and Windows App SDK versions must be known before scaffolding.
- Blockers: unresolved packaging model or missing SDK prerequisites from Phase 1.

#### Risks

- Low to medium. The main risk is choosing a solution structure or app bootstrap pattern that makes later slicing harder or forces broad refactors.
- Failure modes are unstable startup wiring, test project mismatch, or build commands that only work on one machine.

#### Tests and checks to run

- `dotnet restore CoolWSL.sln`
- `dotnet build CoolWSL.sln -c Debug`
- `dotnet build CoolWSL.sln -c Release`
- `dotnet test CoolWSL.Tests/CoolWSL.Tests.csproj`
- Manual smoke launch of the app shell on Windows 11

#### Review check before moving work to DONE.md

- Confirm the shell, project layout, and service-registration boundaries are minimal and do not pre-implement later features.
- Confirm the structure maps back to the suggested project layout in REQUIREMENTS.md.
- Review regression risk around packaging choice, startup wiring, and test discoverability.
- Confirm README.md and local setup instructions were updated with prerequisites and build commands.
- Confirm the phase did not silently introduce WSL feature logic that belongs to later phases.
- Confirm any follow-up bootstrap cleanup or naming decisions were written back to TODO.md.
- Confirm the reviewer agrees the app shell and solution state match this phase goal and scope.

#### Exact TODO.md entries to refresh from this phase

- [ ] Create CoolWSL.sln and the initial CoolWSL.App, CoolWSL.Core, CoolWSL.Wsl, CoolWSL.Configuration, CoolWSL.Diagnostics, and CoolWSL.Tests projects.
- [ ] Wire the WinUI app entry point, main window, shell page, navigation frame, and dependency injection bootstrap.
- [ ] Add baseline shared models and service-registration boundaries without implementing WSL behavior.
- [ ] Add the initial smoke tests and document local build and test commands in README.md.

#### Exit criteria for moving items to DONE.md

- The solution and all planned projects exist in the expected paths and both Debug and Release builds succeed.
- The WinUI app launches to the shell without feature logic and the shell navigation host is visible.
- The initial smoke tests exist and `dotnet test CoolWSL.Tests/CoolWSL.Tests.csproj` passes.
- README.md documents the prerequisites and the commands needed to build and test the solution.

### Phase 3 - Safe WSL execution and parsing foundation implemented

#### Goal

Implement the safe WSL command boundary and parser layer that every later capability depends on.

#### Scope

- Implement command result models, process execution wrappers, timeout handling, cancellation support, stdout and stderr capture, exit code handling, and metadata-only logging defaults.
- Implement safe argument building and quoting to avoid shell injection and to support distro names with spaces.
- Implement parsing for `wsl --list --verbose`, `wsl --status`, and `wsl --version` with explicit degraded behavior when parsing is unreliable or unsupported.
- Map low-level failures into plain-language error results without guessing unsupported behavior.
- Add focused unit tests for command building, parsing, timeout, cancellation, and error mapping.
- End the phase with a test-covered service layer that is usable by the UI without requiring later refactors.

#### Expected files to change

- CoolWSL.Core/Models/CommandResult.cs
- CoolWSL.Core/Models/WslDistro.cs
- CoolWSL.Core/Abstractions/IWslCommandService.cs
- CoolWSL.Core/Abstractions/IWslDistroService.cs
- CoolWSL.Wsl/Services/WslCommandService.cs
- CoolWSL.Wsl/Services/WslDistroService.cs
- CoolWSL.Wsl/Parsing/WslListParser.cs
- CoolWSL.Wsl/Parsing/WslStatusParser.cs
- CoolWSL.Wsl/Errors/WslErrorMapper.cs
- CoolWSL.Core/Logging/
- CoolWSL.Tests/Wsl/

#### Dependencies

- Phase 2 must be complete because the service layer needs the solution and DI shell.
- Phase 1 decisions on supported platform and WSL feature floor must already exist.
- Blockers: unresolved rules for unsupported or localized WSL output; missing decision on whether wslapi.dll is allowed in the first implementation pass.

#### Risks

- Medium to high. This phase carries the main correctness risk for quoting, parser brittleness, timeout handling, and error reporting.
- Failure modes are injection vulnerabilities, parser breakage on older WSL versions, incorrect default-distro detection, and misleading success states when warnings appear on stderr.

#### Tests and checks to run

- `dotnet build CoolWSL.sln -c Debug`
- `dotnet test CoolWSL.Tests/CoolWSL.Tests.csproj`
- Targeted unit tests for command argument building
- Targeted unit tests for parser fixtures covering running, stopped, default, no distro, missing version, old WSL, and unexpected whitespace cases

#### Review check before moving work to DONE.md

- Confirm command construction avoids shell injection and handles distro names with spaces correctly.
- Confirm parser behavior maps directly to documented WSL outputs and explicit degraded modes, not guesses.
- Review regression risk for all later WSL-facing features because this layer is shared infrastructure.
- Confirm developer notes or README updates were made if setup, fixtures, or test prerequisites changed.
- Confirm no user-facing UI behavior was bundled into this phase beyond what is needed to exercise the core layer.
- Confirm any unsupported-output edge cases that remain were written back to TODO.md.
- Confirm the reviewer agrees the core execution and parsing boundary matches the phase goal and scope.

#### Exact TODO.md entries to refresh from this phase

- [ ] Implement the process execution wrapper with timeout, cancellation, stdout and stderr capture, exit code handling, and metadata-only logging.
- [ ] Implement safe WSL argument building and distro-list, status, and version parsers with explicit degraded modes.
- [ ] Map common WSL execution failures to plain-language error results without relying on undocumented behavior.
- [ ] Add unit tests for command building, parser fixtures, timeout handling, cancellation handling, and error mapping.

#### Exit criteria for moving items to DONE.md

- The execution wrapper exists, is wired into DI, and passes timeout, cancellation, and exit code tests.
- Parser fixtures cover the required output variants and the related tests pass.
- Error mapping produces plain-language results for the documented failure classes and reviewer approval confirms the mapping is safe.
- `dotnet test CoolWSL.Tests/CoolWSL.Tests.csproj` passes with the new core tests included.

### Phase 4 - Dashboard inventory slice delivered

#### Goal

Deliver a read-only dashboard that accurately shows WSL availability and distro inventory.

#### Scope

- Build the dashboard page, view model, and refresh flow.
- Show WSL installed and unavailable status, WSL version, kernel version where available, default WSL version where available, and the distro table with name, state, version, and default marker.
- Handle no distros installed, WSL not installed, old WSL with reduced features, and partial failures without breaking navigation.
- Keep this phase read-only aside from refresh so the inventory view can be validated before actions are added.
- End the phase with a dashboard that answers the basic inventory questions from the MVP.

#### Expected files to change

- CoolWSL.App/Views/DashboardPage.xaml
- CoolWSL.App/ViewModels/DashboardViewModel.cs
- CoolWSL.App/Models/DashboardState.cs
- CoolWSL.App/Services/RefreshCoordinator.cs
- CoolWSL.Core/Models/WslEnvironmentStatus.cs
- CoolWSL.Diagnostics/Status/
- CoolWSL.Tests/App/Dashboard/
- CoolWSL.App/Resources/

#### Dependencies

- Phase 3 must be complete because the dashboard depends on the command and parser layer.
- Phase 2 shell navigation must already exist.
- Blockers: none beyond Phase 3 output.

#### Risks

- Medium. Main risks are stale state, incomplete degraded-mode handling, and UI logic that collapses when WSL is unavailable.
- Failure modes include default markers drifting from parsed state, refresh race conditions, and hiding useful error information.

#### Tests and checks to run

- `dotnet build CoolWSL.sln -c Debug`
- `dotnet test CoolWSL.Tests/CoolWSL.Tests.csproj`
- Manual dashboard smoke check with a normal WSL environment
- Manual dashboard smoke check with mocked or simulated WSL unavailable and no-distro states
- Manual keyboard navigation and screen-reader label spot check for the dashboard controls

#### Review check before moving work to DONE.md

- Confirm the dashboard is read-only except for refresh and does not bundle later action work.
- Confirm every displayed field maps to a documented requirement or design element.
- Review regression risk for partial-failure states, async refresh, and empty-state handling.
- Confirm any UI copy, setup notes, or screenshots that should live in docs were updated.
- Confirm the phase did not expand into diagnostics or lifecycle actions.
- Confirm any missing inventory fields or edge cases were written back to TODO.md.
- Confirm the reviewer agrees the dashboard inventory slice matches the phase goal and scope.

#### Exact TODO.md entries to refresh from this phase

- [ ] Add the dashboard page and view model that load WSL environment status and distro inventory.
- [ ] Show distro name, running state, WSL version, and default marker with explicit empty and unavailable states.
- [ ] Implement refresh behavior that keeps the UI usable during load and partial failure conditions.
- [ ] Add automated coverage for dashboard state mapping and manual accessibility checks for the dashboard surface.

#### Exit criteria for moving items to DONE.md

- The dashboard loads and shows the required inventory fields without crashing when WSL is unavailable or when no distros exist.
- Refresh updates the dashboard state and reviewer approval confirms the async behavior is understandable.
- Automated tests cover the dashboard state mapping and pass in the shared test run.
- Manual accessibility and degraded-mode checks were completed and any follow-up items were either fixed or written back to TODO.md.

### Phase 5 - Safe lifecycle actions delivered

#### Goal

Add the MVP lifecycle actions with explicit safety gates and clear target identification.

#### Scope

- Add dashboard and per-distro-header actions for open default distro, open selected distro, start distro where needed, terminate selected distro, set selected distro as default, and shutdown all WSL instances.
- Add confirmation dialogs for destructive or high-impact operations, with cancel as the safe default.
- Disable invalid actions when a distro is already stopped or when a required target is unavailable.
- Show the affected distro or the global impact before an action runs and log metadata for each operation.
- End the phase with the required MVP lifecycle actions available and reviewable.

#### Expected files to change

- CoolWSL.App/Views/DashboardPage.xaml
- CoolWSL.App/Views/DistroPage.xaml
- CoolWSL.App/ViewModels/DashboardViewModel.cs
- CoolWSL.App/ViewModels/DistroViewModel.cs
- CoolWSL.App/Dialogs/ConfirmationDialogs/
- CoolWSL.Wsl/Services/WslDistroService.cs
- CoolWSL.Core/Models/OperationRequest.cs
- CoolWSL.Tests/App/LifecycleActions/
- CoolWSL.Tests/Wsl/ActionArgumentBuilders/

#### Dependencies

- Phase 4 must be complete because actions are anchored in the inventory UI.
- Phase 3 must already provide safe argument building and command execution.
- Blockers: unresolved rules for launching terminals or elevation prompts from Phase 1.

#### Risks

- Medium to high. Main risks are accidental destructive behavior, targeting the wrong distro, or leaving the UI in an inconsistent state after failures.
- Failure modes include missing confirmation on shutdown, wrong default-distro updates, and action buttons remaining enabled after errors.

#### Tests and checks to run

- `dotnet build CoolWSL.sln -c Debug`
- `dotnet test CoolWSL.Tests/CoolWSL.Tests.csproj`
- Targeted unit tests for terminate, shutdown, set default, and open command argument building
- Manual smoke check for open, terminate, set default, and shutdown confirmation flows on a disposable or non-critical distro
- Manual review that destructive confirmations include operation name, target, impact, and cancel as the safe default

#### Review check before moving work to DONE.md

- Confirm every destructive or broad-impact action is gated by the right confirmation strength and clear target labeling.
- Confirm each action maps to an explicit MVP requirement and no 1.0 destructive workflows were added early.
- Review regression risk around command targeting, disabled states, and error recovery after failed actions.
- Confirm any user-visible behavior changes were documented where needed.
- Confirm the phase did not absorb command-runner or import and unregister work from later phases.
- Confirm unfinished lifecycle follow-ups were written back to TODO.md.
- Confirm the reviewer agrees the delivered actions match the phase goal and scope.

#### Exact TODO.md entries to refresh from this phase

- [ ] Add open, start, terminate, set default, and shutdown actions to the dashboard and per-distro header.
- [ ] Add confirmation dialogs for terminate and shutdown all with clear target and consequence text.
- [ ] Disable invalid lifecycle actions and surface plain-language error results when an action fails.
- [ ] Add automated coverage for lifecycle command construction and manual checks for the confirmation flows.

#### Exit criteria for moving items to DONE.md

- The required lifecycle actions are visible, target the correct distro or global scope, and behave correctly in manual smoke checks.
- Terminate and shutdown all are confirmed before execution and the confirmation content matches the documented UX requirements.
- Invalid actions are disabled and failure states are shown without leaving the UI stuck.
- Automated tests for lifecycle command construction pass and reviewer approval confirms the safety gates are correct.

### Phase 6 - Per-distro overview and command runner delivered

#### Goal

Deliver the per-distro page and command runner so users can inspect and operate inside one selected distro.

#### Scope

- Build the per-distro page with header information for name, running state, WSL version, and default marker.
- Add the command runner UI with command input, run action, timeout handling, cancellation, stdout, stderr, exit code display, and session history.
- Keep output readable, copyable, and clearly separated by channel.
- Preserve command history for the current session only in this phase.
- End the phase with a user-reviewable workflow for running commands inside a selected distro.

#### Expected files to change

- CoolWSL.App/Views/DistroPage.xaml
- CoolWSL.App/Views/CommandsPage.xaml
- CoolWSL.App/ViewModels/DistroViewModel.cs
- CoolWSL.App/ViewModels/CommandRunnerViewModel.cs
- CoolWSL.Core/Models/CommandHistoryEntry.cs
- CoolWSL.Wsl/Services/WslInteractiveCommandService.cs
- CoolWSL.Tests/App/CommandRunner/
- CoolWSL.Tests/Wsl/RunInDistro/

#### Dependencies

- Phase 5 must be complete because the per-distro selection and navigation surfaces already exist there.
- Phase 3 is required for safe command execution, timeout, and cancellation support.
- Blockers: unresolved rule for optional run-as-root support, if that is included beyond the MVP minimum.

#### Risks

- Medium. Main risks are command quoting issues, UI freezes during long-running commands, and confusing output presentation.
- Failure modes include stale cancellation tokens, mixed stdout and stderr, and history handling that leaks across sessions unexpectedly.

#### Tests and checks to run

- `dotnet build CoolWSL.sln -c Debug`
- `dotnet test CoolWSL.Tests/CoolWSL.Tests.csproj`
- Targeted unit tests for run-in-distro command construction, timeout, cancellation, and command history behavior
- Manual smoke check running a successful command, a failing command, a timed-out command, and a canceled command
- Manual accessibility check for command input, output regions, and action labels

#### Review check before moving work to DONE.md

- Confirm the phase delivers one clear user outcome: safe per-distro command execution.
- Confirm the output model, timeout behavior, and session history map directly to MVP requirements.
- Review regression risk for UI responsiveness, cancellation, and output channel separation.
- Confirm any changes to local usage documentation or screenshots were updated if needed.
- Confirm the phase did not silently add persistence, command profiles, or unrelated diagnostics work.
- Confirm any unresolved root-run or output-copy follow-ups were written back to TODO.md.
- Confirm the reviewer agrees the command-runner behavior matches the phase goal and scope.

#### Exact TODO.md entries to refresh from this phase

- [ ] Build the per-distro page header that shows name, state, WSL version, and default indicator.
- [ ] Add the command runner with run, cancel, timeout, stdout, stderr, exit code, and session history.
- [ ] Keep command output readable and clearly separated by channel in the UI.
- [ ] Add automated coverage for run-in-distro behavior and manual checks for success, failure, timeout, and cancellation flows.

#### Exit criteria for moving items to DONE.md

- The per-distro page shows the required header data for the selected distro.
- The command runner can execute commands, cancel safely, time out, and display stdout, stderr, and exit code distinctly.
- Session history works within the running app session and does not leak beyond the intended scope.
- Automated tests and manual command-runner smoke checks pass and reviewer approval confirms the feature matches the goal.

### Phase 7 - MVP diagnostics delivered

#### Goal

Provide the MVP diagnostics surface that explains WSL and per-distro health without making changes automatically.

#### Scope

- Build the diagnostics page and the per-distro diagnostics view.
- Run and present `wsl --status`, `wsl --version` where available, distro list diagnostics, default distro diagnostics, internet connectivity tests, DNS resolution tests, and basic host-to-WSL notes.
- Translate results into plain-language summaries while preserving raw output access.
- Keep diagnostics retryable and safe; do not add automatic repair logic in this phase.
- End the phase with a diagnostics surface that can be reviewed against healthy and failing cases.

#### Expected files to change

- CoolWSL.App/Views/DiagnosticsPage.xaml
- CoolWSL.App/ViewModels/DiagnosticsViewModel.cs
- CoolWSL.Diagnostics/Services/DiagnosticsService.cs
- CoolWSL.Diagnostics/Models/DiagnosticResult.cs
- CoolWSL.Diagnostics/Mappers/DiagnosticSummaryMapper.cs
- CoolWSL.Wsl/Services/WslStatusService.cs
- CoolWSL.Tests/Diagnostics/
- CoolWSL.Tests/App/Diagnostics/

#### Dependencies

- Phase 6 must be complete because per-distro context and command execution are already in place.
- Phase 3 parser and execution services are required.
- Blockers: none beyond the availability of the core command layer.

#### Risks

- Medium. Main risks are false positives, overconfident plain-language summaries, and environment-specific network results that are hard to interpret.
- Failure modes include surfacing misleading advice or hiding raw evidence when diagnostics fail.

#### Tests and checks to run

- `dotnet build CoolWSL.sln -c Debug`
- `dotnet test CoolWSL.Tests/CoolWSL.Tests.csproj`
- Targeted unit tests for diagnostic result mapping and plain-language summaries
- Manual smoke check for diagnostics in a healthy distro
- Manual smoke check for DNS failure or no-internet scenarios using mocks or controlled test conditions

#### Review check before moving work to DONE.md

- Confirm diagnostics explain state without adding any repair or mutation logic.
- Confirm each diagnostic maps back to an explicit MVP requirement and preserves raw output.
- Review regression risk for false positives, retry behavior, and partial diagnostic failure handling.
- Confirm docs were updated if new setup or troubleshooting guidance is now needed.
- Confirm the phase did not expand into 1.0 networking detail or health scoring.
- Confirm any ambiguous diagnostic heuristics were written back to TODO.md.
- Confirm the reviewer agrees the diagnostics slice matches the phase goal and scope.

#### Exact TODO.md entries to refresh from this phase

- [ ] Add the diagnostics page and service that run the required global and per-distro diagnostic checks.
- [ ] Map diagnostic results to plain-language summaries while keeping raw output visible.
- [ ] Add retry behavior and failure handling for partial or unsupported diagnostic results.
- [ ] Add automated coverage for diagnostic result mapping and manual checks for healthy and failing scenarios.

#### Exit criteria for moving items to DONE.md

- The diagnostics page runs the required MVP checks and displays plain-language summaries plus raw output.
- Retry behavior works and unsupported checks fail clearly instead of silently disappearing.
- Automated diagnostic mapping tests pass and reviewer approval confirms the summaries are not overstated.
- Manual healthy and failing scenario checks were completed and any remaining follow-ups were written back to TODO.md.

### Phase 8 - Raw global WSL configuration editor delivered

#### Goal

Provide safe raw editing of the global .wslconfig file.

#### Scope

- Add the global settings page for raw .wslconfig editing.
- Support load, create-if-missing behavior where appropriate, basic validation, backup before save, revert, and clear restart-required messaging.
- Keep the workflow explicit: the app must not silently run `wsl --shutdown` after save.
- Validate only what is safely known; on malformed content, preserve the user's text and surface the issue clearly.
- End the phase with a reviewable raw-editor workflow for global WSL settings.

#### Expected files to change

- CoolWSL.App/Views/GlobalSettingsPage.xaml
- CoolWSL.App/ViewModels/GlobalSettingsViewModel.cs
- CoolWSL.Configuration/Services/WslGlobalConfigService.cs
- CoolWSL.Configuration/Models/WslGlobalConfigDocument.cs
- CoolWSL.Configuration/Validation/WslGlobalConfigValidator.cs
- CoolWSL.Configuration/Backup/BackupService.cs
- CoolWSL.Tests/Configuration/WslGlobalConfig/
- CoolWSL.Tests/App/GlobalSettings/

#### Dependencies

- Phase 3 must be complete because safe file and command abstractions are required.
- Phase 2 shell navigation must exist.
- Phase 1 platform decisions must define whether the app is packaged or unpackaged and how user-profile file access will work.
- Blockers: unresolved backup location or permission rules.

#### Risks

- Medium to high. Main risks are config corruption, incorrect validation, lost user changes, and misleading restart messaging.
- Failure modes include overwriting the file without backup, normalizing away unsupported settings, or implying that changes apply immediately.

#### Tests and checks to run

- `dotnet build CoolWSL.sln -c Debug`
- `dotnet test CoolWSL.Tests/CoolWSL.Tests.csproj`
- Targeted unit tests for global config parse, serialize, backup, revert, and validation behavior
- Integration tests using a temp user-profile path
- Manual smoke check for load, save, revert, and malformed-config handling

#### Review check before moving work to DONE.md

- Confirm save flow always creates a backup before overwriting and never shuts WSL down automatically.
- Confirm the validation behavior is conservative and traceable to documented .wslconfig rules.
- Review regression risk for malformed files, unsupported keys, and restart guidance.
- Confirm any user-facing save, backup, or restart guidance was documented where needed.
- Confirm the phase did not absorb the later structured settings UI.
- Confirm unfinished global-config edge cases were written back to TODO.md.
- Confirm the reviewer agrees the raw global-editor workflow matches the phase goal and scope.

#### Exact TODO.md entries to refresh from this phase

- [ ] Add the raw .wslconfig editor with load, create-if-missing, save, revert, and conservative validation behavior.
- [ ] Create backups before every global config overwrite and expose the backup path clearly to the user.
- [ ] Show clear restart-required messaging without automatically shutting down WSL.
- [ ] Add automated coverage for parse, serialize, backup, revert, and malformed-config handling.

#### Exit criteria for moving items to DONE.md

- The raw global editor can load and save .wslconfig safely and produces a backup before overwrite.
- Revert restores the expected editable content and malformed files are surfaced without data loss.
- Restart-required messaging is visible and accurately explains that the app will not restart WSL automatically.
- Automated config tests pass and reviewer approval confirms the workflow is safe and within scope.

### Phase 9 - Raw per-distro WSL configuration editor delivered

#### Goal

Provide safe raw editing of /etc/wsl.conf for a selected distro.

#### Scope

- Add the per-distro config editor under the distro view.
- Support read, basic validation, backup-before-save where feasible, safe save through distro commands, and clear restart-required messaging.
- Handle missing files, stopped distros, permissions issues, and unsupported save scenarios explicitly.
- Show elevated or root-required behavior only when the app can do so transparently and safely; otherwise fail clearly.
- End the phase with a reviewable raw-editor workflow for /etc/wsl.conf.

#### Expected files to change

- CoolWSL.App/Views/DistroConfigPage.xaml
- CoolWSL.App/ViewModels/DistroConfigViewModel.cs
- CoolWSL.Configuration/Services/WslDistroConfigService.cs
- CoolWSL.Configuration/Models/WslDistroConfigDocument.cs
- CoolWSL.Configuration/Validation/WslDistroConfigValidator.cs
- CoolWSL.Wsl/Services/WslFileCommandService.cs
- CoolWSL.Tests/Configuration/WslDistroConfig/
- CoolWSL.Tests/App/DistroConfig/

#### Dependencies

- Phase 6 must be complete because the distro context and command runner infrastructure are already in place.
- Phase 8 should be complete because the backup and config abstraction patterns can be reused.
- Blockers: unresolved rule for saving when the distro is stopped or when root permissions are required.

#### Risks

- High. The main risks are boot-impacting config writes, permissions failures, unexpected distro restarts, and unsafe save workflows inside the distro.
- Failure modes include partial writes, backup failure, or silently changing systemd, boot, or network behavior without clear warnings.

#### Tests and checks to run

- `dotnet build CoolWSL.sln -c Debug`
- `dotnet test CoolWSL.Tests/CoolWSL.Tests.csproj`
- Targeted unit tests for distro config parse, serialize, validation, and warning triggers
- Integration tests for read and write workflows using a temp-file or stubbed distro file adapter
- Manual smoke check for load, save, backup, and warnings around boot, systemd, and networking changes

#### Review check before moving work to DONE.md

- Confirm the save workflow is explicit about the affected distro, permissions, backup path, and restart impact.
- Confirm validation and warning logic align with documented /etc/wsl.conf behavior and do not guess unsupported settings.
- Review regression risk for boot, systemd, and network side effects because this is a high-risk surface.
- Confirm docs were updated if user guidance or operational caveats changed.
- Confirm the phase did not absorb the later structured per-distro settings UI.
- Confirm unfinished edge cases around stopped distros, permissions, or unsupported saves were written back to TODO.md.
- Confirm the reviewer agrees the raw per-distro editor matches the phase goal and scope.

#### Exact TODO.md entries to refresh from this phase

- [ ] Add the raw /etc/wsl.conf editor for a selected distro with load, save, and conservative validation behavior.
- [ ] Create per-distro backups before overwrite where feasible and expose the save limitations when a backup is not possible.
- [ ] Warn clearly when a save changes boot, systemd, or networking behavior and when a distro restart is required.
- [ ] Add automated coverage for parse, serialize, warning, and save-path behavior plus manual checks for permissions and stopped-distro cases.

#### Exit criteria for moving items to DONE.md

- The per-distro editor can load and save /etc/wsl.conf safely for supported scenarios and explains unsupported ones clearly.
- Backup behavior is implemented or explicitly blocked with a reviewed explanation where backup is infeasible.
- Boot, systemd, and networking changes trigger the expected warnings and restart guidance.
- Automated tests and manual save-flow checks pass and reviewer approval confirms the feature is safe enough to mark done.

### Phase 10 - Distro export workflow delivered

#### Goal

Provide the MVP export workflow as a safe, reviewable backup action.

#### Scope

- Add the export surface for selecting a distro, choosing a destination, and starting export.
- Support tar export and add VHD export only when the chosen WSL baseline and selected distro support it.
- Show progress or in-progress state where feasible, final result, and raw error output on failure.
- Treat export as non-destructive while still validating destination paths and logging the operation metadata.
- End the phase with a user-reviewable export workflow that does not overlap with import or unregister behavior.

#### Expected files to change

- CoolWSL.App/Views/BackupsPage.xaml or CoolWSL.App/Views/ExportDialog.xaml
- CoolWSL.App/ViewModels/ExportViewModel.cs
- CoolWSL.Wsl/Services/WslExportService.cs
- CoolWSL.Core/Models/ExportRequest.cs
- CoolWSL.Core/Models/ExportResult.cs
- CoolWSL.Tests/Wsl/WslExportService/
- CoolWSL.Tests/App/Export/
- README.md if export limitations need end-user notes

#### Dependencies

- Phase 5 must be complete because distro targeting and action safety patterns are already established.
- Phase 3 execution and logging foundations are required.
- Blockers: unresolved support floor for VHD export or file-picking behavior under the chosen packaging model.

#### Risks

- Medium. Main risks are long-running operation handling, incomplete progress reporting, and invalid export destinations.
- Failure modes include UI lockups, incorrect VHD support claims, or ambiguous failure messages when the export command fails late.

#### Tests and checks to run

- `dotnet build CoolWSL.sln -c Debug`
- `dotnet test CoolWSL.Tests/CoolWSL.Tests.csproj`
- Targeted unit tests for export argument building, capability detection, and destination validation
- Manual smoke check for a tar export to a disposable destination
- Manual failure-path check for invalid destination or insufficient space handling

#### Review check before moving work to DONE.md

- Confirm export is clearly presented as non-destructive and stays separate from import and unregister work.
- Confirm capability detection for VHD export is tied to documented support and not assumed.
- Review regression risk for long-running UI state, destination validation, and result reporting.
- Confirm any end-user notes about export limitations or prerequisites were added to docs if needed.
- Confirm the phase did not absorb restore, clone, or delete workflows.
- Confirm unfinished export follow-up work was written back to TODO.md.
- Confirm the reviewer agrees the export workflow matches the phase goal and scope.

#### Exact TODO.md entries to refresh from this phase

- [ ] Add the export workflow with distro selection, destination selection, and explicit start action.
- [ ] Support tar export and gate VHD export behind capability detection and clear messaging.
- [ ] Show in-progress state, final result, and raw error output for failed exports.
- [ ] Add automated coverage for export argument building and manual checks for success and failure paths.

#### Exit criteria for moving items to DONE.md

- The export workflow can complete a tar export successfully and report the result clearly.
- VHD export is either supported with correct capability detection or clearly unavailable with reviewed messaging.
- Invalid destination and failure states are surfaced without freezing the UI.
- Automated export tests pass and reviewer approval confirms the workflow is safe and within scope.

### Phase 11 - Structured global settings UI delivered

#### Goal

Provide structured editing for the supported global WSL settings in .wslconfig.

#### Scope

- Build typed models and UI controls for memory, processors, swap, swap file, localhost forwarding, networking mode, DNS tunneling, firewall, auto proxy, nested virtualization, auto memory reclaim, sparse VHD, VM idle timeout, and custom kernel path.
- Gate settings by Windows and WSL version support instead of showing unsupported controls as active.
- Keep the raw editor available and ensure round-trip integrity between structured and raw representations.
- Reuse the backup, validation, and restart-required behavior already established in the raw editor phase.
- End the phase with a structured global settings experience that still preserves raw-edit escape hatches.

#### Expected files to change

- CoolWSL.App/Views/GlobalSettingsPage.xaml
- CoolWSL.App/ViewModels/StructuredGlobalSettingsViewModel.cs
- CoolWSL.Configuration/Models/StructuredWslGlobalConfig.cs
- CoolWSL.Configuration/Mapping/WslGlobalConfigMapper.cs
- CoolWSL.Configuration/Validation/StructuredGlobalSettingsValidator.cs
- CoolWSL.Tests/Configuration/StructuredGlobalConfig/
- CoolWSL.Tests/App/StructuredGlobalSettings/

#### Dependencies

- Phase 8 must be complete because the raw editor, backup flow, and global config storage already exist.
- Phase 1 platform and WSL feature-floor decisions must define which controls can be active.
- Blockers: unresolved feature gating for Windows 11 build-specific or WSL-version-specific settings.

#### Risks

- Medium. Main risks are losing unknown keys during round-trip, exposing unsupported controls, and normalizing values incorrectly.
- Failure modes include overwriting hand-authored settings, incorrect unit parsing for memory and swap, and misleading availability states for networking features.

#### Tests and checks to run

- `dotnet build CoolWSL.sln -c Debug`
- `dotnet test CoolWSL.Tests/CoolWSL.Tests.csproj`
- Targeted unit tests for structured-to-raw and raw-to-structured round-trip behavior
- Targeted unit tests for feature gating and value validation
- Manual smoke check editing several supported settings and confirming restart guidance remains clear

#### Review check before moving work to DONE.md

- Confirm the structured UI preserves the raw editor and does not remove unknown or unsupported settings silently.
- Confirm control availability and validation rules trace back to documented .wslconfig support.
- Review regression risk for round-trip integrity, unit parsing, and hidden-key loss.
- Confirm any new user guidance on structured versus raw editing was documented where needed.
- Confirm the phase did not expand into per-distro structured settings.
- Confirm unresolved round-trip or gating gaps were written back to TODO.md.
- Confirm the reviewer agrees the structured global settings UI matches the phase goal and scope.

#### Exact TODO.md entries to refresh from this phase

- [ ] Add structured controls for the approved global WSL settings while keeping the raw editor available.
- [ ] Gate unsupported controls by Windows and WSL version instead of allowing invalid edits.
- [ ] Preserve round-trip integrity between structured and raw .wslconfig representations.
- [ ] Add automated coverage for mapping, validation, and feature gating plus manual checks for structured editing flows.

#### Exit criteria for moving items to DONE.md

- The structured global settings UI can edit the supported keys and save through the existing safe global config workflow.
- Unsupported controls are disabled or hidden according to reviewed capability rules.
- Round-trip tests prove that supported values and unknown text survive the structured workflow appropriately.
- Automated tests and manual structured-editing checks pass and reviewer approval confirms the phase outcome matches scope.

### Phase 12 - Structured per-distro settings UI delivered

#### Goal

Provide structured editing for the supported /etc/wsl.conf settings.

#### Scope

- Build typed models and UI controls for default user, automount, Windows path interop, hostname, generated hosts, generated resolv.conf, boot command, systemd, GPU support, and timezone sync.
- Preserve the raw editor as the fallback path and maintain round-trip safety.
- Reuse the high-risk warnings for boot, systemd, and networking changes.
- Gate settings that are unsupported on the approved baseline or on the selected distro.
- End the phase with a structured per-distro settings experience that stays explicit about side effects.

#### Expected files to change

- CoolWSL.App/Views/DistroConfigPage.xaml
- CoolWSL.App/ViewModels/StructuredDistroSettingsViewModel.cs
- CoolWSL.Configuration/Models/StructuredWslDistroConfig.cs
- CoolWSL.Configuration/Mapping/WslDistroConfigMapper.cs
- CoolWSL.Configuration/Validation/StructuredDistroSettingsValidator.cs
- CoolWSL.Tests/Configuration/StructuredDistroConfig/
- CoolWSL.Tests/App/StructuredDistroSettings/

#### Dependencies

- Phase 9 must be complete because the raw per-distro editor and save path already exist.
- Phase 1 feature-floor decisions must already define supported settings by Windows and WSL version.
- Blockers: unresolved rule for default user handling in imported distros or distros with missing expected launcher behavior.

#### Risks

- Medium to high. Main risks are changing boot, networking, or systemd behavior with misleading UI, and losing raw settings during round-trip.
- Failure modes include silent removal of user-authored keys, wrong warnings for restart requirements, and unsupported controls appearing valid.

#### Tests and checks to run

- `dotnet build CoolWSL.sln -c Debug`
- `dotnet test CoolWSL.Tests/CoolWSL.Tests.csproj`
- Targeted unit tests for structured-to-raw and raw-to-structured distro config mapping
- Targeted unit tests for warning triggers around boot, systemd, and networking changes
- Manual smoke check for structured edits on a disposable distro or mocked config file

#### Review check before moving work to DONE.md

- Confirm the UI stays explicit about risky settings and preserves the raw editor path.
- Confirm supported keys, warnings, and gating logic map to documented /etc/wsl.conf behavior.
- Review regression risk for round-trip integrity, warning coverage, and distro-specific unsupported cases.
- Confirm docs were updated if structured editing introduces new user guidance or constraints.
- Confirm the phase did not absorb service-management behavior that belongs later.
- Confirm unresolved distro-specific edge cases were written back to TODO.md.
- Confirm the reviewer agrees the structured per-distro settings UI matches the phase goal and scope.

#### Exact TODO.md entries to refresh from this phase

- [ ] Add structured controls for the supported /etc/wsl.conf settings while keeping the raw editor available.
- [ ] Reuse and expose explicit warnings for boot, systemd, and networking changes.
- [ ] Gate unsupported per-distro settings and preserve round-trip integrity for raw content.
- [ ] Add automated coverage for mapping and warning logic plus manual checks for structured distro-editing flows.

#### Exit criteria for moving items to DONE.md

- The structured per-distro settings UI can edit the supported keys and save through the safe distro config workflow.
- Risky setting changes trigger the expected warnings and restart guidance.
- Round-trip tests pass and reviewer approval confirms raw content and unsupported settings are not lost silently.
- Automated tests and manual structured per-distro checks pass and match the phase scope.

### Phase 13 - Systemd service management delivered

#### Goal

Provide service inspection and service actions for systemd-enabled distros.

#### Scope

- Detect when the selected distro supports systemd service management.
- Add the services view with service list, status, failed-state visibility, and actions for start, stop, restart, status, and recent journal output.
- Keep unsupported or non-systemd distros explicit rather than attempting workarounds.
- Show the exact target distro on every service action and result.
- End the phase with a reviewable service-management workflow for eligible distros only.

#### Expected files to change

- CoolWSL.App/Views/ServicesPage.xaml
- CoolWSL.App/ViewModels/ServicesViewModel.cs
- CoolWSL.Diagnostics/Services/SystemdCapabilityService.cs
- CoolWSL.Wsl/Services/WslServiceManagementService.cs
- CoolWSL.Core/Models/ServiceStatusSummary.cs
- CoolWSL.Tests/Wsl/ServiceManagement/
- CoolWSL.Tests/App/ServicesPage/

#### Dependencies

- Phase 6 must be complete because per-distro selection and command execution are required.
- Phase 12 should be complete because per-distro configuration and systemd-related warnings already exist.
- Blockers: unresolved minimum systemd support floor or inability to distinguish supported and unsupported distros safely.

#### Risks

- High. Main risks are running service commands on unsupported distros, causing unintended service restarts, or misreporting systemd status.
- Failure modes include service actions targeting the wrong distro, journal parsing issues, and unsupported distros appearing actionable.

#### Tests and checks to run

- `dotnet build CoolWSL.sln -c Debug`
- `dotnet test CoolWSL.Tests/CoolWSL.Tests.csproj`
- Targeted unit tests for systemd capability detection and service status parsing
- Targeted unit tests for service action command construction
- Manual smoke check on a systemd-enabled distro and a non-systemd distro

#### Review check before moving work to DONE.md

- Confirm service actions are only available on supported distros and always show the exact target.
- Confirm capability detection and status parsing are conservative and do not guess support.
- Review regression risk for service side effects, journal visibility, and unsupported distro messaging.
- Confirm docs were updated if service-management prerequisites or caveats changed.
- Confirm the phase did not expand into broader health scoring or networking fixes.
- Confirm unfinished capability-detection or journaling gaps were written back to TODO.md.
- Confirm the reviewer agrees the service-management slice matches the phase goal and scope.

#### Exact TODO.md entries to refresh from this phase

- [ ] Detect systemd support safely for the selected distro.
- [ ] Add the services view with service list, status, and start, stop, restart, status, and journal actions.
- [ ] Disable or hide service actions for unsupported distros and explain why they are unavailable.
- [ ] Add automated coverage for capability detection and service parsing plus manual checks on supported and unsupported distros.

#### Exit criteria for moving items to DONE.md

- Systemd-capable distros show the services view and unsupported distros show a reviewed unavailable state.
- Service actions target the selected distro and manual smoke checks verify start, stop, restart, and status behavior on a disposable service.
- Automated tests for capability detection and command construction pass.
- Reviewer approval confirms the service-management behavior is safe, scoped, and correctly gated.

### Phase 14 - Detailed networking diagnostics delivered

#### Goal

Provide the 1.0 networking visibility and diagnostics surface without adding automatic repair behavior.

#### Scope

- Add networking diagnostics for distro IP address, default route, DNS servers, DNS resolution test, internet connectivity test, Windows host reachability, localhost forwarding status where inferable, and mirrored networking configuration where configured.
- Present raw evidence and plain-language summaries side by side.
- Diagnose before suggesting next steps and do not apply any automatic network changes.
- Show unsupported or ambiguous states clearly instead of inferring beyond the documented data.
- End the phase with a reviewable networking page that explains common connectivity issues.

#### Expected files to change

- CoolWSL.App/Views/NetworkingPage.xaml
- CoolWSL.App/ViewModels/NetworkingViewModel.cs
- CoolWSL.Diagnostics/Services/NetworkingDiagnosticsService.cs
- CoolWSL.Diagnostics/Models/NetworkDiagnosticSnapshot.cs
- CoolWSL.Configuration/Services/NetworkingModeReader.cs
- CoolWSL.Tests/Diagnostics/Networking/
- CoolWSL.Tests/App/NetworkingPage/

#### Dependencies

- Phase 7 must be complete because the MVP diagnostics framework and plain-language mapping already exist.
- Phase 11 and Phase 12 should be complete because networking mode and related settings are easier to interpret once config readers exist.
- Blockers: unresolved support rules for mirrored networking inference on the chosen WSL and Windows baseline.

#### Risks

- Medium. Main risks are environment-specific false positives, ambiguous localhost-forwarding detection, and overconfident summaries.
- Failure modes include reporting stale IP information, misreading DNS configuration, or suggesting support for a networking mode that is not actually active.

#### Tests and checks to run

- `dotnet build CoolWSL.sln -c Debug`
- `dotnet test CoolWSL.Tests/CoolWSL.Tests.csproj`
- Targeted unit tests for route, DNS, and networking-mode parsing
- Manual smoke check on a normal NAT-based setup
- Manual smoke check on any available mirrored-networking or custom DNS setup, or mocked equivalents if real environments are not available

#### Review check before moving work to DONE.md

- Confirm the networking page stays diagnostic-only and does not mutate configuration or run silent fixes.
- Confirm each data point and summary is traceable to command output or documented config state.
- Review regression risk for ambiguous network environments, stale data, and unsupported-feature messaging.
- Confirm docs were updated if the networking feature introduces setup or interpretation notes.
- Confirm the phase did not expand into health scoring or configuration editing work.
- Confirm unresolved inference limits were written back to TODO.md.
- Confirm the reviewer agrees the networking diagnostics slice matches the phase goal and scope.

#### Exact TODO.md entries to refresh from this phase

- [ ] Add the networking page with IP address, route, DNS server, DNS test, internet test, host reachability, and localhost-forwarding visibility.
- [ ] Read and surface networking mode and related config context where it can be inferred safely.
- [ ] Keep raw evidence visible and avoid any automatic networking repair behavior.
- [ ] Add automated coverage for parsing and inference rules plus manual checks across supported network scenarios.

#### Exit criteria for moving items to DONE.md

- The networking page shows the required 1.0 fields and explains unsupported or ambiguous states clearly.
- Summaries are backed by raw evidence and reviewer approval confirms the UI does not overstate confidence.
- Automated networking tests pass and manual smoke checks cover at least one healthy and one degraded scenario.
- Any missing environment-specific coverage is documented as follow-up rather than left implicit.

### Phase 15 - Filesystem visibility and safe disk operations delivered

#### Goal

Provide disk usage visibility and only the disk operations that can be performed through documented, safe workflows.

#### Scope

- Add the filesystem view with `df`-based Linux filesystem usage and mount visibility.
- Show distro disk usage where it can be derived safely from documented commands or supported APIs.
- Add documented, supported resize workflows only when the platform baseline and capability detection prove they are available.
- Refuse unsupported shrink, compact, or direct-VHD operations rather than attempting brittle workarounds.
- End the phase with a disk surface that is useful, explicit, and conservative.

#### Expected files to change

- CoolWSL.App/Views/FilesystemPage.xaml
- CoolWSL.App/ViewModels/FilesystemViewModel.cs
- CoolWSL.Diagnostics/Services/FilesystemDiagnosticsService.cs
- CoolWSL.Wsl/Services/WslDiskService.cs
- CoolWSL.Core/Models/DiskUsageSnapshot.cs
- CoolWSL.Tests/Wsl/WslDiskService/
- CoolWSL.Tests/App/FilesystemPage/

#### Dependencies

- Phase 6 must be complete because per-distro command execution is required.
- Phase 1 must have locked the supported platform and WSL feature floor for disk operations.
- Blockers: unresolved documented support for resize on the chosen baseline or unclear safety model for any requested disk operation.

#### Risks

- High. This phase touches storage and can easily drift into unsafe territory if unsupported operations are not rejected hard.
- Failure modes include exposing unsupported disk actions, misleading users about resize support, or presenting incomplete disk data as authoritative.

#### Tests and checks to run

- `dotnet build CoolWSL.sln -c Debug`
- `dotnet test CoolWSL.Tests/CoolWSL.Tests.csproj`
- Targeted unit tests for disk capability detection and command construction
- Manual smoke check for read-only filesystem and disk-usage visibility
- Manual confirmation-flow review for any resize action, including clear refusal paths for unsupported operations

#### Review check before moving work to DONE.md

- Confirm the phase refuses unsupported disk actions instead of attempting undocumented or direct-VHD workflows.
- Confirm disk capability detection and messaging map to documented WSL support.
- Review regression risk for destructive or misleading storage behavior because this is a high-risk surface.
- Confirm any disk-operation caveats or prerequisites were added to docs where needed.
- Confirm the phase did not absorb import, clone, or unregister work.
- Confirm unresolved storage edge cases were written back to TODO.md.
- Confirm the reviewer agrees the filesystem and disk slice matches the phase goal and scope.

#### Exact TODO.md entries to refresh from this phase

- [ ] Add the filesystem page with Linux filesystem usage and mount visibility.
- [ ] Detect and show supported disk-usage or resize capabilities conservatively.
- [ ] Gate any resize action behind explicit capability detection and confirmation, and refuse unsupported shrink or compact workflows.
- [ ] Add automated coverage for capability detection plus manual checks for read-only visibility and confirmation flows.

#### Exit criteria for moving items to DONE.md

- The filesystem page shows the intended disk usage information without relying on undocumented data sources.
- Unsupported disk actions are clearly unavailable and reviewed messaging explains why.
- Any supported resize workflow is gated behind capability detection and explicit confirmation.
- Automated tests and manual storage checks pass and reviewer approval confirms the high-risk behavior is safely constrained.

### Phase 16 - Import, clone, and destructive distro management delivered

#### Goal

Provide restore and destructive lifecycle workflows with strong confirmation and no ambiguous target behavior.

#### Scope

- Add import from tar and VHD where supported, clone through export and import, and unregister with strong confirmation.
- Prevent overwrite or replace behavior unless the flow explicitly asks for it and names the target clearly.
- Require typed or equally strong confirmation for data-loss operations.
- Keep import, clone, and unregister fully separated in UI copy and logging so the user always knows whether the operation is restorative or destructive.
- End the phase with audited, strongly gated restore and destructive workflows.

#### Expected files to change

- CoolWSL.App/Views/BackupsPage.xaml
- CoolWSL.App/Views/ImportDialog.xaml
- CoolWSL.App/Dialogs/StrongConfirmationDialog.xaml
- CoolWSL.App/ViewModels/ImportViewModel.cs
- CoolWSL.Wsl/Services/WslImportService.cs
- CoolWSL.Wsl/Services/WslCloneService.cs
- CoolWSL.Wsl/Services/WslUnregisterService.cs
- CoolWSL.Core/Models/ImportRequest.cs
- CoolWSL.Tests/Wsl/ImportAndClone/
- CoolWSL.Tests/App/StrongConfirmation/

#### Dependencies

- Phase 10 must be complete because export is the basis for clone flows and backup semantics.
- Phase 5 safety patterns for destructive confirmations must already exist.
- Phase 15 should be complete if any import path depends on disk-capability or storage constraints being known.
- Blockers: unresolved overwrite rules, replace-existing-distro policy, or packaged-app file-picking limitations.

#### Risks

- High. This phase directly touches permanent data-loss workflows.
- Failure modes include importing over the wrong location, unregistering the wrong distro, or using weak confirmation for destructive actions.

#### Tests and checks to run

- `dotnet build CoolWSL.sln -c Debug`
- `dotnet test CoolWSL.Tests/CoolWSL.Tests.csproj`
- Targeted unit tests for import, clone, and unregister command construction and overwrite guards
- Manual confirmation-flow review for unregister and replace scenarios
- Manual smoke check using a disposable distro or mocked workflow for import and clone

#### Review check before moving work to DONE.md

- Confirm destructive actions require strong confirmation and always show the exact target and consequence.
- Confirm import, clone, and unregister behavior map to documented WSL commands and explicit project safety rules.
- Review regression risk for data loss, overwrite prevention, and result reporting because this is a high-risk phase.
- Confirm any user guidance about destructive operations, backups, or replace behavior was documented.
- Confirm the phase did not absorb unrelated dashboard or settings work.
- Confirm unresolved destructive-flow questions were written back to TODO.md.
- Confirm the reviewer agrees the restore and destructive workflows match the phase goal and scope.

#### Exact TODO.md entries to refresh from this phase

- [ ] Add import from supported backup formats with explicit target and destination selection.
- [ ] Add clone workflow built from explicit export and import steps with clear source and target naming.
- [ ] Add unregister workflow with strong confirmation that requires the user to acknowledge permanent data loss.
- [ ] Add automated coverage for overwrite guards and command construction plus manual checks for destructive confirmation flows.

#### Exit criteria for moving items to DONE.md

- Import and clone flows are explicit, target the correct location, and complete successfully in reviewed manual smoke checks or approved mocks.
- Unregister requires the approved strong-confirmation pattern and cannot execute against an ambiguous target.
- Overwrite or replace behavior is blocked unless an explicit reviewed flow exists.
- Automated destructive-flow tests pass and reviewer approval confirms the safety gates are strong enough.

### Phase 17 - Health-aware dashboard enhancements delivered

#### Goal

Upgrade the dashboard from inventory-only to actionable health visibility.

#### Scope

- Add running-distro count, approximate WSL memory usage, approximate CPU usage, disk usage summary, pending restart warnings, failed diagnostics summary, and recent actions.
- Add health warnings for failed services, DNS failure, no internet connectivity, disk almost full, missing default distro, unsupported configuration settings, WSL version too old for selected features, and config changes requiring restart where the underlying evidence exists.
- Keep alerts explainable and dismissible rather than opaque or noisy.
- Reuse outputs from diagnostics, services, filesystem, config, and operation logging instead of duplicating detection logic.
- End the phase with a dashboard that surfaces actionable warnings and recent system state changes.

#### Expected files to change

- CoolWSL.App/Views/DashboardPage.xaml
- CoolWSL.App/ViewModels/DashboardViewModel.cs
- CoolWSL.Diagnostics/Services/HealthEvaluationService.cs
- CoolWSL.Diagnostics/Models/HealthAlert.cs
- CoolWSL.Core/Models/RecentActionEntry.cs
- CoolWSL.Tests/Diagnostics/HealthEvaluation/
- CoolWSL.Tests/App/DashboardHealth/

#### Dependencies

- Phase 7 must be complete because diagnostics outputs are required.
- Phase 10 must be complete because recent actions depend on operation logging.
- Phase 13, Phase 14, and Phase 15 should be complete because service, network, and disk signals feed health alerts.
- Blockers: none if those earlier phases are done; this phase should not start early.

#### Risks

- Medium. Main risks are alert fatigue, duplicate or conflicting warnings, and stale health state that undermines trust.
- Failure modes include surfacing noisy alerts without clear evidence or hiding the source of a health conclusion.

#### Tests and checks to run

- `dotnet build CoolWSL.sln -c Debug`
- `dotnet test CoolWSL.Tests/CoolWSL.Tests.csproj`
- Targeted unit tests for health-evaluation rules and alert deduplication
- Manual dashboard smoke check with a healthy environment
- Manual dashboard smoke check with mocked failed-service, DNS, and low-disk scenarios

#### Review check before moving work to DONE.md

- Confirm every health alert is backed by explicit evidence and clear explanation text.
- Confirm the enhanced dashboard maps to documented 1.0 dashboard and health-detection requirements.
- Review regression risk for alert noise, stale summaries, and conflicting signals from reused services.
- Confirm any new user guidance for interpreting alerts or dismissing them was documented if needed.
- Confirm the phase did not absorb automatic fix behavior or additional settings work.
- Confirm unresolved health-rule tuning work was written back to TODO.md.
- Confirm the reviewer agrees the dashboard enhancements match the phase goal and scope.

#### Exact TODO.md entries to refresh from this phase

- [ ] Add dashboard resource and recent-action summaries backed by shared diagnostics and log data.
- [ ] Evaluate and show health alerts for services, networking, disk, default-distro, unsupported-config, version-floor, and restart-required conditions.
- [ ] Make health alerts dismissible and explainable without hiding raw evidence.
- [ ] Add automated coverage for health rules and manual checks for healthy and degraded dashboard states.

#### Exit criteria for moving items to DONE.md

- The dashboard shows the new summary fields and recent actions with reviewed data sources.
- Health alerts appear only when backed by evidence and their explanations are clear in manual review.
- Automated health-rule tests pass and reviewer approval confirms alert quality and scope discipline.
- Any remaining tuning or non-blocking alert ideas are written back to TODO.md instead of being bundled into done work.

### Phase 18 - User preferences and command profiles delivered

#### Goal

Persist operator preferences and reusable command workflows without weakening the safety model.

#### Scope

- Add application settings for default terminal integration, command timeout, logging behavior, whether to store command output, theme, refresh interval, confirmation behavior, and export default location.
- Add saved command profiles with name, distro, command, run-as-default-user or root flag, timeout, description, and output-logging flag.
- Persist settings and profiles in a user-visible, documented location with clear upgrade and reset behavior.
- Reuse existing command validation and logging safeguards so stored profiles cannot bypass safety rules.
- End the phase with a reviewable persistence model for user preferences and repeatable commands.

#### Expected files to change

- CoolWSL.App/Views/SettingsPage.xaml
- CoolWSL.App/Views/CommandProfilesPage.xaml
- CoolWSL.App/ViewModels/SettingsViewModel.cs
- CoolWSL.App/ViewModels/CommandProfilesViewModel.cs
- CoolWSL.Core/Models/AppSettings.cs
- CoolWSL.Core/Models/CommandProfile.cs
- CoolWSL.Configuration/Services/AppSettingsService.cs
- CoolWSL.Configuration/Services/CommandProfileService.cs
- CoolWSL.Tests/Configuration/AppSettings/
- CoolWSL.Tests/App/SettingsAndProfiles/

#### Dependencies

- Phase 6 must be complete because command runner semantics and timeout behavior already exist.
- Phase 10 must be complete because logging defaults and export settings interact with persisted preferences.
- Phase 1 decisions on log retention and local-only delivery are required.
- Blockers: unresolved persistence location or reset behavior under the chosen packaging model.

#### Risks

- Medium. Main risks are unsafe persistence defaults, profile execution bypassing validation, and compatibility problems when settings evolve.
- Failure modes include storing sensitive output unintentionally, corrupt settings files, or executing a saved profile with stale distro references.

#### Tests and checks to run

- `dotnet build CoolWSL.sln -c Debug`
- `dotnet test CoolWSL.Tests/CoolWSL.Tests.csproj`
- Targeted unit tests for settings and command-profile serialization, validation, and migration or reset behavior
- Manual smoke check for changing preferences and saving, editing, running, and deleting command profiles
- Manual review that stored profiles still respect timeout, target-distro, and logging safety rules

#### Review check before moving work to DONE.md

- Confirm persisted settings and profiles do not weaken existing safety, confirmation, or logging controls.
- Confirm each saved field maps back to documented requirements or explicit 1.0 scope.
- Review regression risk for profile drift, settings corruption, and sensitive-output storage defaults.
- Confirm docs were updated with settings storage location, reset behavior, and any privacy notes.
- Confirm the phase did not expand into cloud sync or multi-machine profile sharing.
- Confirm unresolved persistence or migration follow-up work was written back to TODO.md.
- Confirm the reviewer agrees the preferences and profiles slice matches the phase goal and scope.

#### Exact TODO.md entries to refresh from this phase

- [ ] Add application settings for terminal integration, timeout, logging, output retention, theme, refresh interval, confirmation behavior, and export default location.
- [ ] Add saved command profiles with validated target distro, command, timeout, privilege flag, description, and logging preference.
- [ ] Persist settings and profiles in a documented local location with clear reset behavior.
- [ ] Add automated coverage for settings and profile serialization plus manual checks that saved profiles still honor safety rules.

#### Exit criteria for moving items to DONE.md

- Application settings can be changed, persisted, and reloaded reliably.
- Saved command profiles can be created, edited, executed, and deleted without bypassing command safety or logging rules.
- Persistence format, storage location, and reset behavior are documented and reviewed.
- Automated tests and manual settings and profile checks pass and reviewer approval confirms the feature stays within scope.

### Phase 19 - Stabilization, packaging verification, and final review completed

#### Goal

Produce a release-candidate build that is validated, documented, and ready for the chosen local delivery path.

#### Scope

- Run the full regression pass across MVP and 1.0 features, including destructive-flow safety reviews on disposable or mocked environments only.
- Fix only defects, gaps, and documentation mismatches discovered from the planned scope; do not add new features in this phase.
- Complete accessibility, keyboard navigation, focus state, readable confirmation dialog, and high-contrast checks.
- Verify build, release configuration, and packaging or install flow for the chosen packaged or unpackaged delivery model.
- Reconcile TODO.md and DONE.md so remaining items are either verified complete or explicitly deferred.
- End the phase with a release candidate that matches the approved scope and has documented validation evidence.

#### Expected files to change

- CoolWSL.sln
- CoolWSL.App/
- CoolWSL.Core/
- CoolWSL.Wsl/
- CoolWSL.Configuration/
- CoolWSL.Diagnostics/
- CoolWSL.Tests/
- README.md
- REQUIREMENTS.md
- DESIGN.md
- TODO.md
- DONE.md
- Packaging or deployment files chosen in Phase 1

#### Dependencies

- All earlier phases must be complete.
- The packaged versus unpackaged path from Phase 1 must be implemented and buildable.
- Blockers: any unresolved blocking open question, failing regression, or undocumented deployment prerequisite.

#### Risks

- Medium. The main risk is discovering cross-phase defects late or attempting to hide unfinished work inside a generic cleanup phase.
- Failure modes include broad churn, new feature creep, and release verification that only works on the developer machine.

#### Tests and checks to run

- `dotnet build CoolWSL.sln -c Debug`
- `dotnet build CoolWSL.sln -c Release`
- `dotnet test CoolWSL.Tests/CoolWSL.Tests.csproj`
- `[format check command]`
- Manual end-to-end smoke test for dashboard, lifecycle actions, command runner, diagnostics, config editing, export, and each 1.0 feature in scope
- Manual accessibility checks for keyboard navigation, screen-reader labels, high contrast, scalable text, and focus visibility
- Packaging or install verification for the approved local distribution path

#### Review check before moving work to DONE.md

- Confirm the phase contains only stabilization, packaging verification, documentation, and defect fixes tied to approved scope.
- Confirm every acceptance criterion from REQUIREMENTS.md is traceable to passing tests or reviewed manual evidence.
- Review regression risk across the whole app, with special attention to destructive actions, config editing, logging, and unsupported-feature gating.
- Confirm README.md, REQUIREMENTS.md, DESIGN.md, TODO.md, and DONE.md reflect the shipped state accurately.
- Confirm no unfinished work is hidden; any remaining items must be written back to TODO.md or explicitly deferred outside the current scope.
- Confirm the reviewer agrees the release candidate matches the phase goal and scope.

#### Exact TODO.md entries to refresh from this phase

- [ ] Run the full Debug and Release build, test, accessibility, and end-to-end smoke checklist for the approved scope.
- [ ] Fix only scope-approved defects and documentation mismatches discovered during final validation.
- [ ] Verify the chosen packaged or unpackaged local delivery path on a clean review machine or clean environment.
- [ ] Reconcile TODO.md and DONE.md so only verified completed work moves to DONE.md.

#### Exit criteria for moving items to DONE.md

- Debug and Release builds succeed, the full automated test suite passes, and the agreed format check passes if configured.
- Manual end-to-end smoke checks and accessibility checks are complete and reviewer approval confirms the results.
- Packaging or install verification succeeds for the approved local delivery path.
- TODO.md contains only unfinished or deferred work, DONE.md contains only verified completed work, and project docs match the shipped behavior.

## Dependency notes

- Phases 1 through 3 are hard prerequisites. No user-facing implementation should start before the delivery baseline, solution skeleton, and safe WSL command layer are in place.
- Phases 4 through 10 are the MVP path. Each phase adds one user-visible capability and should remain serial so the shared WSL and safety abstractions stabilize before more features depend on them.
- Phases 11 and 12 depend on the raw editors because structured settings are safer to build on top of tested read, validate, backup, and save flows.
- Phases 13 through 16 isolate the higher-risk 1.0 capabilities: service management, networking diagnostics, disk operations, and destructive restore or removal workflows. They should not be combined.
- Phase 17 depends on earlier diagnostics, service, disk, and logging outputs. Do not start it before the underlying signals are stable.
- Phase 18 depends on finalized command, logging, and settings behavior so persisted profiles and preferences do not encode unstable contracts.
- Phase 19 is final-only work. If it discovers a missing core capability, that work must return to a prior phase or a newly split phase rather than being hidden in stabilization.
- This plan assumes serial execution. If the team later wants parallel work, it must first re-validate shared contracts after Phase 10 and refresh IMPLEMENTATION_PLAN.md before splitting workstreams.

## Review policy

The expected review size is one phase per review cycle. A phase is acceptable only if it can be built, tested, and manually checked in one cycle and the resulting review package remains focused enough for a reviewer to understand the goal, scope, risk, and evidence without cross-referencing unfinished work.

A phase must be split before implementation starts if any of the following are true:
- The phase contains more than one primary user outcome.
- The phase introduces more than one high-risk or destructive surface.
- The phase cannot be validated with focused automated checks plus a bounded manual smoke pass in the same review cycle.
- The phase requires changes across multiple unrelated subsystems that would make review mostly about navigation rather than behavior.
- The phase mixes foundational refactoring with one or more user-facing features in a way that hides risk.

Oversized phases are not allowed to proceed unchanged. If a phase grows beyond the agreed review size, IMPLEMENTATION_PLAN.md and TODO.md must be refreshed first, and the work must be split into smaller dependency-ordered phases before coding continues.

## Definition of done for the plan

The overall project is done only when all approved phases are complete and verified, and all MVP and 1.0 acceptance criteria in REQUIREMENTS.md are satisfied by code, tests, and reviewed manual evidence.

The following must be true before the project is considered complete:
- The WinUI application builds in Debug and Release using the approved local build commands.
- The app can safely perform the documented inventory, lifecycle, command-runner, diagnostics, config editing, export, and approved 1.0 capabilities without relying on undocumented WSL internals.
- Unit tests and any planned integration tests pass for command execution, parsing, configuration handling, and high-risk workflows.
- Manual smoke checks cover the core user journeys and the destructive or high-risk flows use disposable or mocked environments only.
- Accessibility expectations are verified for keyboard navigation, readable focus states, screen-reader labeling, high contrast, and scalable text.
- Documentation is current: README.md describes setup and usage, REQUIREMENTS.md and DESIGN.md reflect the delivered scope, IMPLEMENTATION_PLAN.md matches the executed ordering, TODO.md contains only unfinished or deferred work, and DONE.md contains only reviewed and verified completed work.
- Packaging or install verification succeeds for the chosen local delivery path from Phase 1.
- Any unresolved item that remains after final review is explicitly deferred and kept out of DONE.md.

## Open questions

### Resolved in Phase 1

- CoolWSL ships as a packaged WinUI 3 desktop app using single-project MSIX.
- Windows App SDK deployment is framework-dependent on the stable 2.0 line, starting with 2.0.1 and keeping later servicing updates in the same major line.
- The scaffold targets `.NET 10` LTS and `net10.0-windows10.0.26100.0`.
- Minimum supported OS is Windows 11 24H2 (build 26100) with current updates.
- Minimum WSL floor is Microsoft Store WSL 0.67.6+, with later features gated by capability detection.
- WSL1 distros remain visible and only documented shared actions stay enabled.
- Docker Desktop distros remain visible, are labeled as system-managed when identifiable, and stay out of destructive, default-distro, and config-editing flows by default.
- Admin-only actions are disabled with guidance in the initial release; no self-elevation is planned in early phases.
- App-owned logs, settings, temp files, and future persistent profiles live under `%LocalAppData%\CoolWSL\`, while exports and backups remain user-directed.

### Remaining non-blocking unknowns

- Should exports become first-class managed backups with retention policies or remain explicit one-off operations?
- Should the app expose raw command history beyond the current session?
- Should per-distro settings be editable while the distro is stopped, or only when the app can verify a safe save path?
- Should there be a portable mode later, or is a standard local install the only supported delivery model?
- Should recent activity and logs be surfaced only in the dashboard, or also on a dedicated logs page in a later revision?
