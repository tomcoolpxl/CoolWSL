# CoolWSL Done

## Phase 1 - Delivery baseline and packaging decision ratified

- Chose packaged WinUI 3 delivery using single-project MSIX and framework-dependent Windows App SDK `2.0.1`, with signed MSIX sideload as the initial install path and optional `.appinstaller` support for direct updates.
- Locked `.NET 10` LTS, planned target `net10.0-windows10.0.26100.0`, Windows 11 24H2 (build 26100), and Microsoft Store WSL `0.67.6+` as the supported baseline.
- Defined baseline behavior for WSL1 distros, Docker Desktop distros, admin-only actions, persistent app-data paths, and 30-day metadata-only log retention.
- Added `ARCHITECTURE.md` and refreshed README.md, REQUIREMENTS.md, DESIGN.md, IMPLEMENTATION_PLAN.md, and TODO.md to reflect the ratified baseline.
- Verified the decisions against current Microsoft Learn guidance for Windows App SDK deployment, Windows app distribution, Windows 11 lifecycle and build numbers, WSL commands, and WSL configuration behavior on 2026-05-01.
