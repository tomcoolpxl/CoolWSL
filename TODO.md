# CoolWSL TODO

## Current Phase

### Phase 8 - Raw global WSL configuration editor delivered

- [ ] Add the raw .wslconfig editor with load, create-if-missing, save, revert, and conservative validation behavior.
- [ ] Create backups before every global config overwrite and expose the backup path clearly to the user.
- [ ] Show clear restart-required messaging without automatically shutting down WSL.
- [ ] Add automated coverage for parse, serialize, backup, revert, and malformed-config handling.

## UX Rebuild Track

Parallel UI rework derived from `UX_REVIEW.md`. UX Phases A and B have shipped (see `DONE.md`); the items below cover the remaining phases C through G.

### UX Phase C - Dashboard rebuild

- [ ] Replace naked `Border` cards on `DashboardPage` with `CardBorderStyle` and replace `Opacity`-based secondary text with `SecondaryTextStyle` / `TertiaryTextStyle`.
- [ ] Replace the distro inventory `ListView` with an `ItemsRepeater` so wheel events bubble to the page-level `ScrollViewer` and the page no longer nests scroll regions.
- [ ] Collapse the per-row Open / Start / Terminate / Set Default button stack into a single primary action plus an overflow menu, or move full lifecycle into the distro detail page.
- [ ] Remove `IsTabStop="True"` and `AllowFocusOnInteraction="True"` from the page-level `ScrollViewer` and verify mouse-wheel scrolling and high-DPI rendering on a window short enough to require scrolling.

### UX Phase D - Distro detail rebuild

- [ ] Replace the existing `DistroPage` layout with a single page that contains a header, status pill, and a `Pivot` (Overview, Terminal, Configuration, Diagnostics).
- [ ] Move lifecycle actions and capability messaging into the Overview pivot using `SettingsCard` rows.
- [ ] Redesign the command runner inside the Terminal pivot with a single output area, stdout / stderr colour distinction, copy and clear toolbar, and a virtualised collapsible history.
- [ ] Move per-distro diagnostics into the Diagnostics pivot fed by `IDiagnosticsService.GetSnapshotAsync(selectedDistroName)`.

### UX Phase E - Diagnostics rebuild

- [ ] Replace `DiagnosticsPage` with a severity-grouped `ItemsRepeater` of `SettingsExpander` rows (Errors, Warnings, OK) sourced from `IDiagnosticsService`.
- [ ] Show a single Refresh action at the top of the page and a `LastUpdatedAt` caption.
- [ ] Remove the duplicate per-distro diagnostics rendering from the old `DistroPage` once UX Phase D ships.

### UX Phase F - Settings and Logs pages

- [ ] Replace `PlaceholderPage` for `Settings` with a real settings page using `SettingsExpander` groups (WSL, Appearance, Behaviour, Diagnostics, About).
- [ ] Reintroduce `Logs` in the rail and replace `PlaceholderPage` with a real log viewer once `IAppLogger` is implemented (currently `NullAppLogger`).

### UX Phase G - Accessibility pass

- [ ] Ensure `AutomationProperties.Name` on every per-distro action button includes the distro name in its label.
- [ ] Add a polite live region announcement for command results and diagnostic refresh outcomes.
- [ ] Audit Tab order on every page and remove residual `IsTabStop` / `AllowFocusOnInteraction` properties from page-level `ScrollViewer`s.
- [ ] Run a high-contrast smoke check and confirm card surfaces and status indicator colours remain readable.

## Remaining Plan Items

### Phase 9 - Raw per-distro WSL configuration editor delivered

- [ ] Add the raw /etc/wsl.conf editor for a selected distro with load, save, and conservative validation behavior.
- [ ] Create per-distro backups before overwrite where feasible and expose the save limitations when a backup is not possible.
- [ ] Warn clearly when a save changes boot, systemd, or networking behavior and when a distro restart is required.
- [ ] Add automated coverage for parse, serialize, warning, and save-path behavior plus manual checks for permissions and stopped-distro cases.

### Phase 10 - Distro export workflow delivered

- [ ] Add the export workflow with distro selection, destination selection, and explicit start action.
- [ ] Support tar export and gate VHD export behind capability detection and clear messaging.
- [ ] Show in-progress state, final result, and raw error output for failed exports.
- [ ] Add automated coverage for export argument building and manual checks for success and failure paths.

### Phase 11 - Structured global settings UI delivered

- [ ] Add structured controls for the approved global WSL settings while keeping the raw editor available.
- [ ] Gate unsupported controls by Windows and WSL version instead of allowing invalid edits.
- [ ] Preserve round-trip integrity between structured and raw .wslconfig representations.
- [ ] Add automated coverage for mapping, validation, and feature gating plus manual checks for structured editing flows.

### Phase 12 - Structured per-distro settings UI delivered

- [ ] Add structured controls for the supported /etc/wsl.conf settings while keeping the raw editor available.
- [ ] Reuse and expose explicit warnings for boot, systemd, and networking changes.
- [ ] Gate unsupported per-distro settings and preserve round-trip integrity for raw content.
- [ ] Add automated coverage for mapping and warning logic plus manual checks for structured distro-editing flows.

### Phase 13 - Systemd service management delivered

- [ ] Detect systemd support safely for the selected distro.
- [ ] Add the services view with service list, status, and start, stop, restart, status, and journal actions.
- [ ] Disable or hide service actions for unsupported distros and explain why they are unavailable.
- [ ] Add automated coverage for capability detection and service parsing plus manual checks on supported and unsupported distros.

### Phase 14 - Detailed networking diagnostics delivered

- [ ] Add the networking page with IP address, route, DNS server, DNS test, internet test, host reachability, and localhost-forwarding visibility.
- [ ] Read and surface networking mode and related config context where it can be inferred safely.
- [ ] Keep raw evidence visible and avoid any automatic networking repair behavior.
- [ ] Add automated coverage for parsing and inference rules plus manual checks across supported network scenarios.

### Phase 15 - Filesystem visibility and safe disk operations delivered

- [ ] Add the filesystem page with Linux filesystem usage and mount visibility.
- [ ] Detect and show supported disk-usage or resize capabilities conservatively.
- [ ] Gate any resize action behind explicit capability detection and confirmation, and refuse unsupported shrink or compact workflows.
- [ ] Add automated coverage for capability detection plus manual checks for read-only visibility and confirmation flows.

### Phase 16 - Import, clone, and destructive distro management delivered

- [ ] Add import from supported backup formats with explicit target and destination selection.
- [ ] Add clone workflow built from explicit export and import steps with clear source and target naming.
- [ ] Add unregister workflow with strong confirmation that requires the user to acknowledge permanent data loss.
- [ ] Add automated coverage for overwrite guards and command construction plus manual checks for destructive confirmation flows.

### Phase 17 - Health-aware dashboard enhancements delivered

- [ ] Add dashboard resource and recent-action summaries backed by shared diagnostics and log data.
- [ ] Evaluate and show health alerts for services, networking, disk, default-distro, unsupported-config, version-floor, and restart-required conditions.
- [ ] Make health alerts dismissible and explainable without hiding raw evidence.
- [ ] Add automated coverage for health rules and manual checks for healthy and degraded dashboard states.

### Phase 18 - User preferences and command profiles delivered

- [ ] Add application settings for terminal integration, timeout, logging, output retention, theme, refresh interval, confirmation behavior, and export default location.
- [ ] Add saved command profiles with validated target distro, command, timeout, privilege flag, description, and logging preference.
- [ ] Persist settings and profiles in a documented local location with clear reset behavior.
- [ ] Add automated coverage for settings and profile serialization plus manual checks that saved profiles still honor safety rules.

### Phase 19 - Stabilization, packaging verification, and final review completed

- [ ] Run the full Debug and Release build, test, accessibility, and end-to-end smoke checklist for the approved scope.
- [ ] Fix only scope-approved defects and documentation mismatches discovered during final validation.
- [ ] Verify the chosen packaged or unpackaged local delivery path on a clean review machine or clean environment.
- [ ] Reconcile TODO.md and DONE.md so only verified completed work moves to DONE.md.
