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
