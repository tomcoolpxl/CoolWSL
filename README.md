# CoolWSL

WSL Control Center for Windows 11.

## Status

Phase 1 delivery baseline is ratified. The repository is still documentation-first, and Phase 2 will scaffold the initial WinUI solution.

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

See `ARCHITECTURE.md` for the full decision record and rationale.
