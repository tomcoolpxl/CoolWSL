# CoolWSL Architecture

This document describes the current v1 architecture of the repository.

It replaces the earlier architecture file that mixed active decisions with historical planning stages. Future expansion ideas belong in [EXTRA_FEATURES.md](EXTRA_FEATURES.md).

## System Overview

CoolWSL is a layered WinUI 3 desktop application that sits on top of supported WSL command and configuration surfaces.

At runtime, the architecture is:

```text
WinUI pages and controls
        |
        v
View models and app services
        |
        v
Core abstractions and shared models
        |
        +----------------------------+
        |            |               |
        v            v               v
     WSL layer   Configuration   Diagnostics
        |            |               |
        +------------+---------------+
                     |
                     v
       wsl.exe, supported config files, local app data
```

The important boundary is that CoolWSL does not talk to private WSL internals. It uses `wsl.exe`, supported config files, and ordinary Windows process and filesystem APIs.

## Solution Structure

| Project | Current responsibility |
| --- | --- |
| `CoolWSL.App` | WinUI shell, pages, controls, view models, theme handling, navigation composition |
| `CoolWSL.Core` | Shared abstractions, models, logging, configuration document model, common operation contracts |
| `CoolWSL.Wsl` | WSL command construction, execution, parsing, error mapping, distro orchestration, distro file access |
| `CoolWSL.Configuration` | Global and per-distro configuration services built on top of core models and WSL file access |
| `CoolWSL.Diagnostics` | Diagnostic snapshot generation and summary mapping |
| `CoolWSL.Tests` | Automated tests for services, parsers, view models, DI wiring, and XAML regressions |
| `build` | Installer packaging scripts, WiX authoring, release-support automation |

## Composition Root

`CoolWSL.App/DependencyInjection/AppServiceCollection.cs` is the composition root.

It wires together:

- app view models.
- app-scoped services such as theme preference.
- the WSL, configuration, diagnostics, and core service registrations.
- WinUI pages and controls.

The current shell is intentionally thin at the page layer. Pages delegate almost all stateful behavior to view models and downstream services.

## UI Architecture

### Main window and shell

- `MainWindow` hosts the shell and the persistent status bar.
- `ShellPage` owns the navigation frame and the dynamic distro entries.
- Dashboard, Logs, Settings, and Distro pages are separate routed surfaces inside the shell frame.

### View-model responsibilities

- `DashboardViewModel` orchestrates dashboard refresh and lifecycle actions.
- `DistroViewModel` coordinates selected-distro identity, overview actions, per-distro settings, and diagnostics.
- `DistroSettingsViewModel` manages the `/etc/wsl.conf` structured editor, raw editor, validation, saves, defaults restore, and verification probes.
- `DistroPageDiagnosticsViewModel` adapts diagnostic snapshots into per-card UI state.
- `SettingsViewModel` handles global WSL summary, read-only `.wslconfig` visibility, theme selection, and shell-level actions.
- `LogsViewModel` exposes the session-scoped metadata log viewer.
- `StatusBarViewModel` projects global environment state into the persistent status bar.

The view-model layer is stateful and UI-facing, but not responsible for low-level WSL command details.

## WSL Execution Layer

The WSL layer is centered on command construction, execution, and conservative parsing.

### Key services

- `WslCommandService` executes host-side `wsl.exe` commands and captures results.
- `WslDistroService` provides higher-level distro operations such as inventory, open, start, terminate, set-default, shutdown, and in-distro command execution.
- `WslDistroFileService` reads, writes, and deletes distro files through supported WSL execution paths.

### Architectural rules

- Command arguments are passed as raw argument lists rather than shell-concatenated strings.
- Host-side metadata commands are parsed defensively and must degrade safely on unsupported or localized output.
- Running-state inference uses both verbose inventory and running-only inventory so localization does not collapse all state into unknown.
- Windows Terminal launch is attempted first for opening a selected distro, with direct `wsl.exe` launch as fallback.

This layer is intentionally conservative: it favors supported behavior and plain-language error mapping over aggressive inference.

## Configuration Architecture

Configuration is split into global WSL settings and per-distro settings because those scopes have different product ownership.

### Global configuration

- `WslGlobalConfigService` reads and validates `%UserProfile%\.wslconfig`.
- Validation is schema-aware enough to catch malformed sections, duplicate keys, and obviously invalid values, but it preserves unknown content.
- The current v1 UX treats this service as read-only visibility plus validation. Editing is handed off to the official WSL Settings app.

### Per-distro configuration

- `WslDistroConfigService` owns `/etc/wsl.conf` read, validate, save, restore-defaults, and probe workflows.
- Save and restore-defaults flows create local backups under `%LocalAppData%\CoolWSL\Backups\WslDistroConfig`.
- Verification probes execute targeted commands inside the selected distro to test whether configured values are effective.

### Lossless INI model

The core configuration model is intentionally loss-aware.

- `IniDocument`, `IniSection`, `IniEntry`, comments, blank lines, and malformed lines are represented explicitly.
- Unknown keys, comments, ordering, and unchanged raw lines are preserved.
- Entries expose both raw stored value and effective unquoted value so validation and UI can reason about the setting without discarding the user's original text.
- `WslDistroConfigSchema` defines the current supported first-class `/etc/wsl.conf` keys.

This is one of the most important architectural choices in the repo: the app edits config as a document, not as a lossy object graph.

## Diagnostics Architecture

Diagnostics are assembled as snapshots rather than streamed as a live event system.

### Current flow

- `DiagnosticsService` runs global WSL checks in parallel: status, version, verbose inventory, and running inventory.
- It resolves the selected distro context from the requested distro, then falls back to the default distro or first known distro.
- When a distro context exists, it also runs DNS and internet probes inside that distro.
- The resulting data is mapped into `DiagnosticResult` cards with severity, summary, details, next-step text, command text, and optional raw output.

This architecture keeps diagnostics deterministic and page-refresh-driven. It is designed for safe troubleshooting, not for background remediation.

## Logging and Local State

### Metadata logging

- `FileAppLogger` is the current `IAppLogger` and `IAppLogReader` implementation.
- Log files are stored under `%LocalAppData%\CoolWSL\Logs` as JSON lines.
- Retention defaults to 30 days.
- The log reader exposes only entries from the current app session after the session watermark.
- `Clear` on the Logs page advances the session watermark instead of deleting retained files.

The logging contract is deliberately narrow: metadata only, no command stdout or stderr persistence.

### Theme preference

- `ThemePreferenceService` is the current persisted application-preference service.
- It maps the chosen system, light, or dark theme into the UI root and keeps that preference across launches.

## Delivery Architecture

The repository still contains app-package-capable project settings, but the active release path is installer-first.

### Current release path

- `Directory.Build.props` provides shared version metadata and supports release stamping through `COOLWSL_VERSION`.
- `CoolWSL.App.csproj` supports both packaged and install-folder output modes through `CoolWslDistributionKind`.
- The active release flow builds a self-contained install-folder layout, packages it as an MSI through WiX, and wraps that MSI in a Burn setup EXE for Winget-friendly elevation.
- `build/Invoke-ReleaseInstaller.ps1` produces the public artifacts:
        - Setup EXE bootstrapper
  - MSI installer
  - ZIP portable package
  - SHA-256 checksums file

This aligns the codebase with the current published delivery model and winget-oriented release flow.

## Active Architectural Decisions

The current repository is built around these active decisions:

1. Use a distro-first shell with one detail page per distro rather than a flat feature-page IA.
2. Keep diagnostics contextual to the selected distro instead of exposing a separate full diagnostics route.
3. Treat global WSL configuration as a read-only summary plus handoff to official WSL Settings.
4. Own per-distro `/etc/wsl.conf` editing directly, with validation, backups, and restart guidance.
5. Keep WSL integration on supported surfaces only and avoid undocumented registry, service, or VHD internals.
6. Ship through installer-first artifacts rather than MSIX-first release packaging.
7. Retain metadata-only logging by default.

## Verification Model

The repository's verification strategy combines:

- targeted unit tests for parsers, mappers, services, and view models.
- DI-registration tests.
- focused XAML regression tests for known layout pitfalls.
- non-interactive smoke launch verification through `COOLWSL_SMOKE_TEST=1`.

The architecture depends on these checks because the application crosses UI, process-launch, parsing, and filesystem boundaries.

## Explicit Boundaries

The current architecture does not include:

- direct `ext4.vhdx` manipulation.
- undocumented `Lxss` registry parsing as a source of truth.
- private WSL service APIs.
- automatic repair engines.
- a user-facing freeform command-runner subsystem.
- a full global WSL settings editor.

Those boundaries are intentional and should not be eroded casually.

## Future Work

Potential future architecture changes are tracked only in [EXTRA_FEATURES.md](EXTRA_FEATURES.md).

If a subsystem is not implemented in source and not described here as current architecture, it should be treated as future work rather than inferred from old planning documents.
