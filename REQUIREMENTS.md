# CoolWSL

A WSL Control Center for Windows 11.

## Purpose

CoolWSL is a Windows 11 desktop application for managing WSL distributions, with WSL2 as the full-featured baseline and explicit degraded behavior for WSL1 and partially supported environments.

The application should provide a clear overview of the local WSL environment while also offering a focused per-distro detail experience.

CoolWSL should avoid brittle or undocumented implementation techniques. It should rely on supported WSL commands, documented configuration files, and safe Windows APIs.

## Goals

CoolWSL should:

- Provide a Windows 11-native application shell with fixed global destinations and distro-first navigation.
- Provide a single overview dashboard plus dedicated per-distro detail pages for focused management.
- Provide a persistent status bar for global WSL state and refresh recency.
- Support common WSL lifecycle actions.
- Show distro status, version, default state, and basic health information.
- Edit supported WSL configuration files safely.
- Run commands inside distros and show clear output.
- Offer useful diagnostics for WSL, networking, DNS, and distro health.
- Support safe backup/export operations.
- Avoid undocumented registry scraping, private WSL internals, or direct unsafe VHD manipulation.
- Be suitable for technical users without becoming a fragile low-level VM manager.

## Non-Goals

CoolWSL should not:

- Act as a general Hyper-V VM manager.
- Depend on undocumented WSL registry structures.
- Depend on private WSL service internals.
- Modify distro VHD files directly while WSL is running.
- Hide destructive actions behind casual UI controls.
- Pretend that WSL exposes a rich public management API.
- Automatically repair complex networking or filesystem problems without user review.
- Require users to understand every WSL command manually.

## Target Platform

- Windows 11 version 24H2 (build 26100) or later with current cumulative updates
- Microsoft Store-distributed WSL 0.67.6 or later
- packaged WinUI 3 desktop app using single-project MSIX
- C# on .NET 10 LTS
- Windows App SDK 2.0.1 or later 2.0.x servicing patch

## Delivery Baseline

- CoolWSL ships as a packaged WinUI 3 desktop app using single-project MSIX.
- Windows App SDK deployment is framework-dependent on the stable 2.0 line.
- The initial scaffold targets `net10.0-windows10.0.26100.0`.
- App-owned logs, settings, temp files, and future persistent profiles live under `%LocalAppData%\CoolWSL\`.
- Exports and backups must always use explicit user-selected locations.
- The first supported release stays unelevated and disables admin-only actions with clear guidance.

## Supported Backends

CoolWSL should use the following supported mechanisms:

- `wsl.exe`
- `wslapi.dll` where appropriate
- `%UserProfile%\.wslconfig`
- `/etc/wsl.conf`
- commands executed inside distros using `wsl -d <distro> -- <command>`
- Windows process and performance APIs for host-side metrics

## Unsupported or Avoided Backends

CoolWSL should avoid:

- undocumented `Lxss` registry parsing
- private WSL service APIs
- Hyper-V VM enumeration as a source of truth for WSL distros
- direct modification of `ext4.vhdx`
- assumptions based on localized `wsl.exe` output where avoidable

## User Experience Model

CoolWSL should use a single shell with:

- fixed left-rail destinations for Dashboard, Logs, and Settings
- a dynamic Distros group where each distro is its own first-class navigation item
- a persistent bottom status bar showing WSL availability, default distro, running-distro count, and last refresh time
- a Windows 11-native visual language built from real cards, Fluent icons, theme brushes, and standard window chrome

## Dashboard

The dashboard is the main landing page.

It should answer:

- Is WSL installed and working?
- Which distros exist?
- Which distros are running?
- Which distro is the default?
- Are there obvious problems?
- Are global WSL settings pending restart?
- What quick actions are available?

## Distro Detail

Selecting a distro from the rail should open its dedicated detail page.

It should answer:

- What is the state of this distro?
- What configuration applies to it?
- Can I run commands inside it?
- Are services healthy?
- Is networking working?
- Is disk usage concerning?
- What safe actions can I perform?

The MVP detail page should be organized as:

```text
Overview
Terminal
Configuration
Diagnostics
```

## Global Destinations

Diagnostics live inside the per-distro detail page as the Diagnostics pivot. Global checks (`wsl --status`, `wsl --version`, inventory, default distro) are surfaced inside that pivot alongside per-distro probes; the dashboard may summarize top findings but the shell does not expose a separate Diagnostics destination.

Logs should remain a fixed global destination for metadata-only app and command history.

Settings should remain a fixed global destination for application settings and global WSL configuration.

Backups and other secondary workflows may be reached from Settings or contextual actions rather than occupying first-class shell positions in MVP.

## MVP Requirements

## MVP Dashboard

The dashboard must show:

- WSL installed or unavailable status.
- WSL version where available.
- WSL kernel version where available.
- Default WSL version where available.
- Plain-language environment summary.
- List or tile surface of registered distros.
- Distro name.
- Distro running state.
- Distro WSL version.
- Default distro marker.
- Quick global actions.
- Compact health or diagnostic summary with links to the full Diagnostics page.

Required actions:

- Refresh status.
- Open default distro.
- Navigate to selected distro detail.
- Shutdown all WSL instances.

Lifecycle actions such as start, terminate, and set default must remain available, but they should live on the distro detail page or in a per-distro overflow surface rather than forcing four inline actions onto every dashboard row.

The shutdown action must clearly warn that it affects all running WSL distros.

## MVP Shell Navigation

The shell navigation must support:

- Fixed top-level destinations for Dashboard, Logs, and Settings.
- A Distros group bound to all registered distros.
- Selecting a distro as a first-class navigation action.
- Distinguishing running and stopped distros with clear state indicators.
- Showing the default distro clearly.
- Handling distro names with spaces.
- Handling no distros installed.
- Handling WSL not installed.
- Handling old WSL versions with reduced feature availability.
- Showing WSL1 distros with explicit reduced-capability messaging.
- Labeling Docker Desktop distros distinctly when they can be identified safely.

Selecting a distro should open its dedicated detail page. The MVP should not require a separate top-level Distros page before a user can act on a distro.

WSL1 distros remain first-class inventory items, but any WSL2-only feature must be disabled with a plain-language explanation.

Docker Desktop distros must never be the default target for destructive or config-editing flows in the initial release.

## MVP Status Bar

The persistent status bar must show:

- WSL version or availability state.
- Default distro.
- Running distro count.
- Last refresh time.

It must remain visible across Dashboard, Logs, Settings, and distro detail pages and degrade safely when data is unavailable.

## MVP Distro Detail

Each distro detail page must show:

- Distro name.
- Running state.
- WSL version.
- Whether it is the default distro.
- Capability messaging when the distro is WSL1 or system-managed.
- A pivot with Overview, Terminal, Configuration, and Diagnostics.

Required actions:

- Open terminal.
- Start distro.
- Terminate distro.
- Set as default.
- Run command.

The command runner must be reachable directly from the Terminal pivot and should not be buried below unrelated lifecycle content.

## MVP Command Runner

The command runner must:

- Run a command inside a selected distro.
- Capture stdout.
- Capture stderr.
- Capture exit code.
- Support cancellation.
- Support timeout.
- Preserve command history for the session.
- Clearly show whether the command succeeded or failed.
- Present one primary output surface, with stderr visually distinguished without requiring a permanent side-by-side split.

Nice to have in MVP:

- Run as root.
- Copy or clear output.
- Save output to file.

## MVP Global Configuration

CoolWSL must support reading and editing:

```text
%UserProfile%\.wslconfig
```

The MVP should provide:

- Raw text editor.
- Basic validation.
- Backup before save.
- Save changes.
- Revert changes.
- Clear notice when restart is required.

The app must not silently restart WSL after config changes.

The UI must clearly state that `.wslconfig` applies only to WSL2 distributions.

## MVP Per-Distro Configuration

CoolWSL must support reading and editing:

```text
/etc/wsl.conf
```

The MVP should provide:

- Raw text editor.
- Basic validation.
- Backup before save where feasible.
- Save changes using commands executed inside the distro.
- Clear notice when distro restart is required.

## MVP Diagnostics

The MVP diagnostics experience must include:

- A Diagnostics pivot within each distro detail page that owns the full diagnostics view, covering both per-distro probes and global checks for the chosen distro context.
- `wsl --status`
- `wsl --version` where available
- Distro list diagnostics
- Default distro
- Internet connectivity test from selected distro
- DNS resolution test from selected distro
- Basic host-to-WSL notes

Diagnostics should be presented in plain language, with raw command output available.

The dashboard may surface only a compact summary of top findings. Full diagnostic detail lives in the per-distro Diagnostics pivot rather than being duplicated across multiple shell destinations.

## MVP Export

CoolWSL must support exporting a distro.

Requirements:

- Select distro.
- Select destination.
- Run export.
- Show progress state where feasible.
- Show final result.
- Show error output on failure.
- Prevent export from being treated as destructive.

## MVP Logging

CoolWSL must keep an application log containing:

- WSL commands executed by the app.
- Start time.
- End time.
- Exit code.
- Errors.
- Configuration changes.
- Export operations.

Logs must avoid storing sensitive command output by default unless the user enables it.

Logs must be written under `%LocalAppData%\CoolWSL\Logs`.

Metadata-only logs are retained for 30 days by default.

## Version 1.0 Requirements

## 1.0 Dashboard Enhancements

The dashboard should add:

- Running distro count.
- Approximate WSL memory usage.
- Approximate WSL CPU usage.
- Disk usage summary.
- Health warnings.
- Pending restart warnings.
- Recent actions.
- Failed diagnostics summary.

## 1.0 Health Detection

CoolWSL should detect:

- Failed systemd services.
- DNS failure.
- No internet connectivity.
- Disk almost full.
- WSL version too old for selected features.
- Missing default distro.
- Unsupported configuration settings.
- Config changes requiring restart.

Health warnings must be explainable and dismissible.

## 1.0 Service Management

For distros with systemd enabled, CoolWSL should support:

- List services.
- Show running services.
- Show failed services.
- Start service.
- Stop service.
- Restart service.
- View service status.
- View recent journal output.

Service actions must show the exact distro affected.

## 1.0 Disk Management

CoolWSL should support:

- Show Linux filesystem usage using `df`.
- Show distro disk usage where safely available.
- Resize distro VHD using supported WSL commands where available.
- Warn before disk operations.
- Refuse unsupported disk operations instead of using brittle workarounds.

Shrink and compact operations should remain out of scope unless implemented through a safe, documented workflow.

## 1.0 Backup and Import

CoolWSL should support:

- Export distro to `.tar`.
- Export distro to `.vhd` where supported.
- Import distro from backup.
- Clone distro through export/import.
- Unregister distro with strong confirmation.

Unregister must require a destructive-action confirmation, such as typing the distro name.

## 1.0 Networking Diagnostics

CoolWSL should show:

- Distro IP address.
- Default route.
- DNS servers.
- DNS resolution test.
- Internet connectivity test.
- Windows host reachability test.
- Localhost forwarding status where inferable.
- Mirrored networking configuration where configured.

The app should diagnose before offering fixes.

## 1.0 Global Settings UI

Instead of raw `.wslconfig` editing only, CoolWSL should provide structured controls for:

- memory
- processors
- swap
- swap file
- localhost forwarding
- networking mode
- DNS tunneling
- firewall
- auto proxy
- nested virtualization
- auto memory reclaim
- sparse VHD
- VM idle timeout
- custom kernel path

The raw editor should remain available.

## 1.0 Per-Distro Settings UI

CoolWSL should provide structured controls for supported `/etc/wsl.conf` settings:

- default user
- automount
- Windows path interop
- hostname
- generated hosts
- generated resolv.conf
- boot command
- systemd
- GPU support
- timezone sync

The raw editor should remain available.

## 1.0 Command Profiles

CoolWSL should support saved command profiles.

Each profile should include:

- name
- distro
- command
- run as default user or root
- timeout
- description
- whether output should be logged

Example profiles:

- Update packages.
- Show failed services.
- Restart SSH.
- Show Docker containers.
- Show Kubernetes contexts.
- Show disk usage.

## 1.0 Application Settings

CoolWSL should provide settings for:

- default terminal integration
- command timeout
- logging behavior
- whether to store command output
- theme
- refresh interval
- confirmation behavior
- export default location

## Architecture Requirements

## Suggested Project Structure

```text
CoolWSL.App
CoolWSL.Core
CoolWSL.Wsl
CoolWSL.Configuration
CoolWSL.Diagnostics
CoolWSL.Tests
```

## Core Interfaces

```csharp
public interface IWslCommandService
{
    Task<CommandResult> RunAsync(
        string arguments,
        CancellationToken cancellationToken = default);
}

public interface IWslDistroService
{
    Task<IReadOnlyList<WslDistro>> ListAsync(
        CancellationToken cancellationToken = default);

    Task TerminateAsync(
        string distroName,
        CancellationToken cancellationToken = default);

    Task SetDefaultAsync(
        string distroName,
        CancellationToken cancellationToken = default);

    Task<CommandResult> RunInDistroAsync(
        string distroName,
        string command,
        CancellationToken cancellationToken = default);
}

public interface IWslGlobalConfigService
{
    Task<WslGlobalConfig> ReadAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        WslGlobalConfig config,
        CancellationToken cancellationToken = default);
}

public interface IWslDistroConfigService
{
    Task<WslDistroConfig> ReadAsync(
        string distroName,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        string distroName,
        WslDistroConfig config,
        CancellationToken cancellationToken = default);
}
```

## Command Execution Requirements

All command execution must:

- Avoid shell injection.
- Quote distro names safely.
- Support cancellation.
- Support timeout.
- Capture stdout.
- Capture stderr.
- Capture exit code.
- Log command metadata.
- Avoid logging sensitive output by default.
- Handle missing `wsl.exe`.
- Handle non-zero exit codes.
- Handle commands that write warnings to stderr but still succeed.

## Parser Requirements

Parsers must be tested against:

- normal `wsl --list --verbose` output
- no distros installed
- stopped distros
- running distros
- default distro marker
- distro names containing spaces
- old WSL versions
- missing `wsl --version`
- unexpected whitespace
- localized output risks

Where parsing is unreliable, the app should expose reduced functionality rather than guessing.

## Safety Requirements

CoolWSL must:

- Confirm destructive operations.
- Never silently unregister a distro.
- Never silently shutdown WSL after config save.
- Never edit VHD files directly.
- Create backups before overwriting config files.
- Show affected distro before running an operation.
- Show raw command output for failed operations.
- Prefer refusing unsupported actions over using undocumented workarounds.
- Disable admin-only actions until an explicit elevation model is approved.
- Treat identified Docker Desktop distros as system-managed and protect them from destructive flows by default.

## Destructive Operations

The following operations require confirmation:

- Shutdown all WSL.
- Terminate distro.
- Unregister distro.
- Import over existing distro.
- Resize disk.
- Save config that changes boot behavior.
- Save config that changes systemd behavior.
- Save config that changes networking behavior.

The following operations require strong confirmation:

- Unregister distro.
- Delete backup.
- Replace existing distro.
- Any operation that may destroy distro data.

## Error Handling Requirements

CoolWSL must handle:

- WSL not installed.
- WSL installed but unavailable.
- `wsl.exe` command failure.
- Unsupported WSL feature.
- Access denied.
- Distro not found.
- Distro currently running.
- Distro currently stopped.
- Network failure.
- DNS failure.
- Config file missing.
- Config parse failure.
- Export failure.
- Timeout.
- Cancellation.

Errors should include:

- plain-language summary
- command attempted
- exit code where available
- stderr where available
- suggested next step where safe

## Testing Requirements

Unit tests must cover:

- command argument building
- distro list parsing
- status parsing
- config parsing
- config serialization
- command result handling
- timeout handling
- cancellation handling
- error mapping

Integration tests should cover:

- WSL unavailable
- no distros installed
- one stopped distro
- one running distro
- command execution inside distro
- reading `/etc/wsl.conf`
- reading `.wslconfig`

Tests should avoid destructive operations unless explicitly marked.

## Security Requirements

CoolWSL must:

- Avoid privilege escalation unless explicitly required.
- Show when an operation requires administrator rights.
- Avoid storing sensitive command output by default.
- Avoid leaking environment variables in logs.
- Avoid executing user-provided commands through an intermediate shell unless necessary.
- Escape and quote arguments safely.
- Keep backups in a predictable and user-visible location.

## Accessibility Requirements

The UI should support:

- keyboard navigation
- screen readers
- high contrast mode
- scalable text
- clear focus states
- accessible labels for action buttons that include the affected distro when applicable
- live announcements for long-running operation results and command completion
- page scrolling that works predictably with mouse, keyboard, and assistive technology
- confirmation dialogs that are readable and specific

## UX Requirements

## Shell UX

The main shell should use this structure:

```text
Dashboard
Logs
Settings
Distros
    Ubuntu
    Debian
    docker-desktop
```

The shell should also include a persistent bottom status bar.

Backups and other secondary workflows should be entered from Settings or contextual actions until they justify first-class navigation.

## Dashboard UX

The dashboard should prioritize:

- current WSL status
- running distros
- warnings
- quick actions

The distro surface should prefer tiles or simple card rows over an action-dense table.

Each dashboard distro item should:

- show name, state, version, and default status clearly
- use a primary click or tap action to open the distro detail page
- keep secondary lifecycle actions in the detail page or an overflow surface

## Distro Detail UX

The distro detail page should use this structure in MVP:

```text
Overview
Terminal
Configuration
Diagnostics
```

Version 1.0 may extend this with Services, Filesystem, and Networking as additional pivots or secondary routes without changing the primary shell model.

## Diagnostics UX

Diagnostics should:

- live in the per-distro Diagnostics pivot as the only full-results home
- present results in a severity-first or otherwise easy-to-triage structure
- keep raw output available on demand
- allow the dashboard to summarize top findings without duplicating the full pivot

## Visual UX

The app should:

- use real card surfaces instead of transparent content containers
- use theme brushes instead of opacity-based typography for secondary text
- use Fluent-style icons and standard Windows 11 title-bar behavior
- support a Windows 11 backdrop treatment such as Mica when it does not compromise text clarity
- keep page scrolling natural and avoid dead mouse-wheel zones caused by nested scroll surfaces

## Confirmation UX

Confirmation dialogs must include:

- operation name
- affected distro
- whether the operation is destructive
- whether data loss is possible
- exact consequence
- cancel as the default option for dangerous operations

## Status UX

The shell must maintain a persistent status bar that surfaces WSL availability, default distro, running-distro count, and refresh recency independently of the active page.

Long-running operations must show:

- operation name
- target distro
- elapsed time
- current status
- cancel button where safe
- final result

## Implementation Principles

CoolWSL should be:

- safe first
- explicit about side effects
- conservative with unsupported features
- clear about when restart is required
- tolerant of older WSL installations
- useful without requiring administrator rights
- testable at the command-wrapper layer
- honest about feature availability

## MVP Acceptance Criteria

The MVP is acceptable when:

- The app starts on Windows 11.
- It detects whether WSL is available.
- It uses a fixed shell with Dashboard, Logs, Settings, and per-distro navigation items.
- It lists registered distros.
- It shows running or stopped state.
- It shows the default distro.
- It shows a persistent status bar with global WSL state and last refresh information.
- The dashboard presents a summary card, distro inventory surface, quick actions, and a diagnostics summary.
- Each distro opens in a detail page with Overview, Terminal, Configuration, and Diagnostics.
- It can open a distro.
- It can terminate a distro.
- It can set the default distro.
- It can shutdown WSL with confirmation.
- It can run a command inside a distro.
- It can show stdout, stderr, and exit code.
- It provides mouse-wheel and keyboard scrolling that works on the main content pages.
- It can read and edit `.wslconfig`.
- It can read and edit `/etc/wsl.conf`.
- It can run basic diagnostics inside the per-distro Diagnostics pivot, which owns both global and per-distro checks.
- It can export a distro.
- It logs operations safely.
- It avoids undocumented WSL internals.

## Version 1.0 Acceptance Criteria

Version 1.0 is acceptable when:

- The dashboard includes health warnings.
- The app provides structured global WSL settings.
- The app provides structured per-distro settings.
- The app supports service inspection for systemd distros.
- The app supports safe import and clone workflows.
- The app supports disk usage inspection.
- The app supports supported disk resizing where available.
- The app supports networking diagnostics.
- The app supports saved command profiles.
- The app has robust error handling.
- The app has unit tests for parsers and command services.
- The app has clear warnings for destructive operations.
- The app remains free of undocumented WSL internals.

## Open Questions

Phase 1 resolved the delivery baseline as follows:

- CoolWSL is WSL2-first, but WSL1 distros remain visible and only documented shared actions stay enabled.
- Docker Desktop distros remain visible, are labeled as system-managed when identifiable, and stay out of destructive and config-editing flows by default.
- Command output is not stored by default; metadata-only logs are retained for 30 days unless the user changes retention later.
- Admin-only actions are disabled with guidance instead of prompting for elevation in the initial release.

Remaining open questions:

- Should exports be managed as first-class backups?
- Should CoolWSL support scheduled backups?
- Should there be a portable mode?
- Should the app expose raw command history?
- Should per-distro settings be editable while the distro is stopped only?
- Should the app support remote WSL instances in the future?

## Recommended MVP Scope

Build first:

- Dashboard
- Shell navigation with distro entries
- Open distro
- Terminate distro
- Shutdown all WSL
- Set default distro
- Per-distro detail shell
- Command runner
- Raw `.wslconfig` editor
- Raw `/etc/wsl.conf` editor
- Basic diagnostics
- Distro export
- Safe operation logging

Defer:

- Service manager
- Disk resize
- Import and clone
- Structured settings UI
- Networking assistant
- Resource graphs
- Backup scheduler
- Docker integration
- Kubernetes integration
- Health scoring
