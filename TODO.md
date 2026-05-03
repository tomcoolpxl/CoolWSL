# CoolWSL TODO

## Current Phase

### Current Phase: EXTRA1

- [x] Add an unpackaged self-contained Release publish mode for installer builds.
- [x] Add a WiX-based MSI plus ZIP release packaging flow for GitHub Releases.
- [x] Document the installer-first winget release flow and retire the MSIX-first release notes.
- [x] Add GitHub Actions CI for restore, build, test, and smoke verification.
- [x] Add tag-driven release packaging that stamps assembly versions and installer artifact versions automatically.
- [x] Document winget-compatible installer release flow for MSI/ZIP/checksums artifacts.

- [ ] Phase E1.0: Pivot rename + Terminal removal + scaffolding
- [ ] Phase E1.1: Distro filesystem service + lossless INI document
- [ ] Phase E1.2: Per-distro config service (read, validate, save, backup)
- [ ] Phase E1.3: Static + capability validation
- [ ] Phase E1.4: Settings pivot UI - raw editor + restart messaging
- [ ] Phase E1.5: Settings pivot UI - structured editor over the same model
- [ ] Phase E1.6: Runtime validation probes + global `.wslconfig` summary card
- [ ] Phase E1.7: Polish, accessibility audit, smoke verification

## UX Rebuild Track

Parallel UI rework derived from `UX_REVIEW.md`. UX Phases A through F have shipped (see `DONE.md`); the item below covers the remaining phase G.

Recent distro-detail rendering, dashboard-card navigation, rail-selection crash, and the 2026-05-03 card-width / settings-button-reorder / defaults-flow / terminal-cursor / Logs-Clear hotfix are complete; the remaining UX backlog is the accessibility pass below.

### Active UX Fix

- [x] Restore Dashboard card host centering while keeping settings-width cards.
- [x] Normalize About version metadata and hide SDK git hashes.
- [ ] Normalize Settings expander widths in collapsed and expanded states.
- [ ] Align Distro pivot tabs and Overview cards to the Settings card envelope.
- [x] Generate packaged app icons and wire the unpackaged app window icon from the repository logo asset.
- [x] Add the CoolWSL logo to the Dashboard header and Settings About section.
- [x] Restore the original shell rail and restyle the Dashboard logo to use the transparent asset directly.

### UX Phase G - Accessibility pass

- [ ] Ensure `AutomationProperties.Name` on every per-distro action button includes the distro name in its label.
- [ ] Add a polite live region announcement for command results and diagnostic refresh outcomes.
- [ ] Audit Tab order on every page and remove residual `IsTabStop` / `AllowFocusOnInteraction` properties from page-level `ScrollViewer`s.
- [ ] Run a high-contrast smoke check and confirm card surfaces and status indicator colours remain readable.

## Remaining Plan Items

### Superseded by EXTRA1

#### Phase 9 - Raw per-distro WSL configuration editor delivered
- [ ] Add the raw /etc/wsl.conf editor for a selected distro with load, save, and conservative validation behavior.
- [ ] Create per-distro backups before overwrite where feasible and expose the save limitations when a backup is not possible.
- [ ] Warn clearly when a save changes boot, systemd, or networking behavior and when a distro restart is required.
- [ ] Add automated coverage for parse, serialize, warning, and save-path behavior plus manual checks for permissions and stopped-distro cases.

#### Phase 12 - Structured per-distro settings UI delivered
- [ ] Add structured controls for the supported /etc/wsl.conf settings while keeping the raw editor available.
- [ ] Reuse and expose explicit warnings for boot, systemd, and networking changes.
- [ ] Gate unsupported per-distro settings and preserve round-trip integrity for raw content.
- [ ] Add automated coverage for mapping and warning logic plus manual checks for structured distro-editing flows.

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
