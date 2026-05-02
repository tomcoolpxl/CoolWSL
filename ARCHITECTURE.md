# CoolWSL Architecture

## ADR 0001 - Delivery Baseline

Date: 2026-05-01

### Decision

- CoolWSL ships as a packaged WinUI 3 desktop app using single-project MSIX.
- Windows App SDK deployment is framework-dependent on the stable 2.0 line, starting from `Microsoft.WindowsAppSDK` `2.0.1`.
- The solution scaffold targets `.NET 10` LTS and `net10.0-windows10.0.26100.0`.
- Minimum supported OS is Windows 11 24H2 (build 26100) with current cumulative updates.
- Minimum supported WSL floor is Microsoft Store WSL `0.67.6` or later. Later features remain capability-gated by command and config-key availability.
- WSL1 distros stay visible and only documented shared actions remain enabled. WSL2-only features are disabled with explanation.
- Docker Desktop distros stay visible, are labeled as system-managed when identifiable, and are excluded from destructive, default-distro, and config-editing flows by default.
- The app stays unelevated in the first supported release. Admin-only workflows are disabled with clear manual guidance instead of self-elevating.
- App-owned logs, settings, temp files, and future persistent profiles live under `%LocalAppData%\CoolWSL\`. Exports and backups use explicit user-chosen destinations.
- Logging is metadata-only by default, retains 30 days of history, and does not persist command stdout or stderr unless the user explicitly opts in.

### Rationale

- Microsoft recommends MSIX for most WinUI 3 apps, and WinUI 3 templates are packaged by default.
- Microsoft documents unpackaged WinUI 3 as a niche path with extra runtime and bootstrapper requirements, no package identity, no built-in update flow, and no single-file EXE option.
- Framework-dependent Windows App SDK deployment keeps the app package smaller and inherits Microsoft servicing for the shared runtime.
- `.NET 10` is the current LTS release in May 2026 and provides the longest support window for a new desktop codebase.
- Windows App SDK `2.0.1` is the current stable release in May 2026, while `1.8` is already in maintenance.
- Windows 11 24H2 is the oldest Home and Pro release still in support in May 2026.
- WSL `0.67.6+` is the earliest documented version with systemd support, which sets a safe baseline for the later service-management phase while still allowing capability gating above that floor.

### Delivery Implications

- Phase 2 must scaffold a packaged MSIX solution and avoid introducing an unpackaged bootstrapper path.
- Phase 2 smoke verification must prove that the packaged desktop process can launch `wsl.exe` and read and write `%UserProfile%\.wslconfig` as designed.
- Release packaging should produce a signed MSIX as the primary artifact. `.appinstaller` support is optional but compatible with the baseline.
- Any future Store submission must remain compatible with the packaged baseline instead of forcing a distribution-model rewrite.
- Persistent app-owned state should use `%LocalAppData%\CoolWSL\` rather than relying on package-private storage paths.
- Export workflows remain explicit user operations, not silent background backups.

### Microsoft Documentation Consulted

- Windows App SDK release channels and downloads
- Windows App SDK deployment guides for packaged and unpackaged apps
- Windows app distribution-path guidance
- WSL basic commands and WSL configuration docs
- Windows 11 lifecycle and release-health pages

## ADR 0002 - UX Shell Architecture Baseline

Date: 2026-05-02

### Decision

- CoolWSL uses a single WinUI shell with fixed global destinations for Dashboard, Diagnostics, and Settings plus a dynamic Distros group in the left navigation rail.
- Each distro is a first-class navigation target. The MVP does not rely on a separate top-level Distros page before a user can inspect or act on a distro.
- The window includes a persistent bottom status bar that remains visible across global pages and distro detail pages.
- The dashboard is a summary surface, not the primary home for every distro action. It presents environment status, quick global actions, a distro inventory surface that opens detail pages, and a compact diagnostics summary.
- Each distro opens in a dedicated detail page with the pivots Overview, Terminal, Configuration, and Diagnostics.
- Diagnostics has one primary global home. Other surfaces may summarize or deep-link to diagnostic results, but the full diagnostics experience is not duplicated across multiple pages.
- Settings remains a fixed global destination. Logs, backups, import and export flows, and other secondary workflows are entered through Settings or contextual actions until they justify first-class navigation.
- The UX rebuild is confined to `CoolWSL.App`. The existing service and model contracts in `CoolWSL.Core`, `CoolWSL.Wsl`, `CoolWSL.Diagnostics`, and `CoolWSL.Configuration` remain the data and command boundary for the new shell.
- Shared visual primitives such as card surfaces, typography, spacing, status indicators, and reusable page-level components should be centralized in app-level resources and shared controls instead of being redefined per page.

### Rationale

- The user mental model is distro-centric. Treating each distro as a primary navigation entity shortens the common path to start, inspect, configure, or diagnose a distro.
- The previous flat page model distributed related information across Dashboard, Distros, and Diagnostics, which increased duplication and made it unclear where a given workflow belonged.
- A persistent status bar keeps global WSL context available without forcing navigation back to the dashboard.
- A dedicated distro detail page creates a stable extension point for later services, networking, filesystem, logs, and configuration work without requiring another information-architecture rewrite.
- Keeping the backend boundary unchanged allows the UI layer to be rebuilt without destabilizing the tested command, parsing, and diagnostics services.
- Centralizing shared UI primitives reduces repeated XAML patterns and keeps the application visually and structurally consistent as more surfaces are added.

### Delivery Implications

- `CoolWSL.App` should be organized around a shell composition model: a navigation host, a persistent status bar, global pages, and dedicated distro detail pages.
- The shell navigation state should be driven from distro inventory and environment status rather than from a hard-coded list of flat feature pages.
- Dashboard surfaces should navigate into distro detail pages instead of exposing dense per-row action stacks as the primary interaction model.
- Lifecycle actions remain supported, but they move to distro detail pages or contextual overflow actions instead of dominating the dashboard inventory surface.
- Diagnostics ownership is explicit: the global Diagnostics page owns the complete diagnostics view, and per-distro diagnostics belong in the distro detail page.
- Settings and other secondary workflows should be modeled as contextual or secondary routes so the primary shell remains stable.
- Shared styles and reusable controls should be introduced at the app root so page implementations compose common primitives instead of carrying page-specific typography, spacing, and container rules.
- Existing backend abstractions should be reused as-is where possible. Any future app-layer refactor should preserve the current service-oriented boundary unless a backend limitation is proven.

### Repository Inputs Consulted

- `UX_REVIEW.md`
- `REQUIREMENTS.md`
- `DESIGN.md`
- `IMPLEMENTATION_PLAN.md`
