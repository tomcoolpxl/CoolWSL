# CoolWSL Requirements

This document defines the current v1 product.

Historical roadmap material has been retired from this file. Future product ideas and expansion areas belong in [EXTRA_FEATURES.md](EXTRA_FEATURES.md).

## Product Definition

CoolWSL is a Windows 11 desktop control center for Windows Subsystem for Linux.

Its job is to give users a safe, distro-first view of local WSL state, expose supported lifecycle and configuration actions, and surface diagnostics without depending on undocumented WSL internals.

## Supported Environment

- Windows 11 24H2, build 26100 or later.
- Microsoft Store-distributed WSL 0.67.6 or later.
- x64 only.
- WinUI 3 desktop app on .NET 10.
- Windows App SDK 2.0.x.
- Installer-first release delivery using setup EXE, MSI, ZIP, and checksums assets.
- App-owned data stored under `%LocalAppData%\CoolWSL\`.

## Product Scope

### Shell

The app must provide a single shell with:

- a fixed `Dashboard` destination.
- a dynamic `Distros` group where each discovered distro is its own navigation target.
- fixed footer destinations for `Logs` and `Settings`.
- a persistent bottom status bar visible across the shell.

The shell must remain usable when WSL is unavailable, partially supported, or reports no distros.

### Dashboard

The dashboard must show:

- WSL availability and a plain-language environment summary.
- WSL version when available.
- kernel version when available.
- default WSL version when available.
- distro inventory as clickable tiles or rows.
- distro name, running state, WSL generation label, default marker, and safe management label when applicable.
- degraded or empty-state messaging when inventory cannot be fully resolved.

The dashboard must support:

- refresh.
- opening the default WSL terminal.
- shutting down all running WSL distros.
- navigating directly to a selected distro detail page.

The dashboard must not be the primary home for dense per-distro action stacks.

### Status Bar

The persistent status bar must show:

- WSL availability or version.
- default distro.
- running-distro count.
- last refresh time.

It must degrade safely when data is unavailable.

### Distro Detail

Each distro detail page must show:

- distro name.
- running or stopped state.
- WSL generation.
- default-distro indicator when applicable.
- capability or management messaging when the distro is WSL1 or system-managed.
- a pivot with `Overview`, `Settings`, and `Diagnostics`.

The `Overview` pivot must expose:

- `Open terminal`.
- `Start`.
- `Terminate`.
- `Set default`.

Unavailable actions must be disabled with explanation instead of being silently removed.

### Per-Distro Settings

The app must support reading and editing `/etc/wsl.conf` for the selected distro.

The per-distro settings surface must provide:

- a read/write raw editor.
- structured controls for the supported documented key set.
- validation messages split into errors, warnings, and informational issues.
- backup-before-overwrite behavior.
- restore-defaults behavior by deleting `/etc/wsl.conf` safely.
- explicit restart-impact guidance.
- verification probes that test whether selected settings are effective inside the distro.

The structured editor must cover the currently supported sections:

- `boot`.
- `automount`.
- `network`.
- `interop`.
- `user`.
- `gpu`.
- `time`.

Edits must preserve user intent as closely as possible, including comments, ordering, unknown keys, and raw formatting where the user did not change the entry.

### Global WSL Settings

The app must support reading `%UserProfile%\.wslconfig`.

The v1 product must treat global WSL settings as a handoff flow, not as an in-app full editor.

The global settings experience must provide:

- current file path.
- read-only file contents when the file exists.
- clear missing-file state when the file does not exist.
- validation messaging for malformed content.
- a prominent `WSL Settings` handoff action.
- clear messaging that global changes affect WSL 2 and typically require WSL restart semantics.

The app must not silently edit `.wslconfig` from the user-facing v1 UX.

### Diagnostics

Diagnostics must live on the per-distro `Diagnostics` pivot.

That pivot must own both:

- global checks such as `wsl --status`, `wsl --version`, distro inventory, and default-distro health.
- selected-distro probes such as DNS and internet reachability.

Each diagnostic result must support:

- a title.
- severity labeling.
- a plain-language summary.
- optional detail text.
- optional suggested next step.
- optional raw command text.
- optional raw output.

Diagnostics must summarize and explain problems. They must not attempt automatic repair.

### Logs

The app must provide a dedicated `Logs` page for metadata-only logging.

The logs experience must provide:

- newest-first entries.
- filtering by log level.
- text search over area and message.
- a `Clear` action that clears the currently displayed session view without deleting retained files.
- a `Refresh` action.

Log entries must remain metadata-only. They must not persist command stdout or stderr.

The current page contract is session-scoped display over a retained on-disk log store.

### Settings

The `Settings` page must provide:

- current WSL status, default distro, and inventory summary.
- safe global actions such as opening the default terminal and shutting down WSL.
- read-only global `.wslconfig` visibility and WSL Settings handoff.
- persisted theme selection with `System`, `Light`, and `Dark` choices.
- about information and repository / issue links.

Only theme preference is a persisted user-facing app preference in v1.

If the UI shows future preference placeholders, those controls do not define a supported saved-preference contract yet.

## Supported Technical Boundaries

CoolWSL may rely on:

- `wsl.exe`.
- supported WSL configuration files.
- Windows process launch APIs.
- local app-data storage under `%LocalAppData%\CoolWSL\`.
- supported WinUI 3 and Windows App SDK behavior.

CoolWSL must avoid using undocumented internals as a source of truth.

## Capability Rules

- WSL 2 is the full-featured baseline.
- WSL 1 distros must remain visible, but WSL 2-only behavior must be gated with plain-language explanations.
- Docker Desktop or similarly system-managed distros must remain visible, but destructive and config-editing flows must be restricted when safe identification is available.
- The app remains unelevated in v1. Admin-only workflows are out of scope for the supported product path.
- The app must not silently restart WSL after configuration changes.

## Safety Requirements

- Destructive actions such as terminating a distro, shutting down WSL, reverting settings, or restoring defaults must require explicit user intent.
- The app must prefer supported command and file-based workflows over brittle inference.
- Command execution must pass arguments without shell-concatenation hazards.
- Parser behavior must degrade safely on localized, partial, or unsupported output instead of guessing.
- Refresh flows must reject stale results when a newer refresh supersedes them.

## Data and Privacy Requirements

- Logs must default to metadata only.
- Log retention must default to 30 days.
- App-owned settings, logs, and backups must stay under `%LocalAppData%\CoolWSL\` unless the user explicitly chooses another export destination.
- Backups and exports must be explicit user actions.

## Quality Requirements

- The solution must remain buildable through the checked-in .NET toolchain.
- Automated tests must cover command construction, parsing, diagnostics mapping, configuration handling, dependency injection, and key view-model behavior.
- The app must support non-interactive smoke launch verification through `COOLWSL_SMOKE_TEST=1`.
- Keyboard-triggered refresh and focusable primary controls must remain available on major pages.
- Visual styling must preserve text clarity and theme contrast on supported Windows themes.

## Explicit Non-Goals

The v1 product does not attempt to be:

- a general Hyper-V or VM manager.
- a full in-app editor for global WSL VM settings.
- a disk or VHD management tool.
- a backup, import, or export workbench.
- a service manager for arbitrary Linux daemons.
- an automatic repair tool for broken networking, filesystem, or distro state.
- a UI over undocumented registry structures, private service APIs, or direct `ext4.vhdx` mutation.
- an in-app freeform command-runner product surface.

## Future Work

Potential future work is tracked only in [EXTRA_FEATURES.md](EXTRA_FEATURES.md).

If a feature is not implemented in source and not described in this document as current behavior, it should be treated as future work rather than implied scope.
