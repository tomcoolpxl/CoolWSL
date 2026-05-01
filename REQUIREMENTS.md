# CoolWSL

A WSL Control Center for Windows 11.

## Purpose

CoolWSL is a Windows 11 desktop application for managing WSL distributions, with WSL2 as the full-featured baseline and explicit degraded behavior for WSL1 and partially supported environments.

The application should provide a clear overview of the local WSL environment while also offering a focused per-distro management mode.

CoolWSL should avoid brittle or undocumented implementation techniques. It should rely on supported WSL commands, documented configuration files, and safe Windows APIs.

## Goals

CoolWSL should:

- Provide a single overview dashboard for the local WSL environment.
- Provide a separate per-distro view for detailed management.
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

CoolWSL should have two primary modes:

## Dashboard Mode

The dashboard is the main landing page.

It should answer:

- Is WSL installed and working?
- Which distros exist?
- Which distros are running?
- Which distro is the default?
- Are there obvious problems?
- Are global WSL settings pending restart?
- What quick actions are available?

## Per-Distro Mode

The per-distro view should answer:

- What is the state of this distro?
- What configuration applies to it?
- Can I run commands inside it?
- Are services healthy?
- Is networking working?
- Is disk usage concerning?
- What safe actions can I perform?

## MVP Requirements

## MVP Dashboard

The dashboard must show:

- WSL installed or unavailable status.
- WSL version where available.
- WSL kernel version where available.
- Default WSL version where available.
- List of registered distros.
- Distro name.
- Distro running state.
- Distro WSL version.
- Default distro marker.
- Quick action buttons.

Required actions:

- Refresh status.
- Open default distro.
- Open selected distro.
- Terminate selected distro.
- Set selected distro as default.
- Shutdown all WSL instances.

The shutdown action must clearly warn that it affects all running WSL distros.

## MVP Distro List

The distro list must support:

- Listing all registered distros.
- Distinguishing running and stopped distros.
- Showing WSL version 1 or 2 where available.
- Showing the default distro.
- Handling distro names with spaces.
- Handling no distros installed.
- Handling WSL not installed.
- Handling old WSL versions with reduced feature availability.
- Showing WSL1 distros with explicit reduced-capability messaging.
- Labeling Docker Desktop distros distinctly when they can be identified safely.

WSL1 distros remain first-class inventory items, but any WSL2-only feature must be disabled with a plain-language explanation.

Docker Desktop distros must never be the default target for destructive or config-editing flows in the initial release.

## MVP Per-Distro Overview

Each distro page must show:

- Distro name.
- Running state.
- WSL version.
- Whether it is the default distro.
- Basic command actions.

Required actions:

- Open terminal.
- Start distro.
- Terminate distro.
- Set as default.
- Run command.

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

Nice to have in MVP:

- Run as root.
- Copy output.
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

The MVP diagnostics page must include:

- `wsl --status`
- `wsl --version` where available
- Distro list diagnostics
- Default distro
- Internet connectivity test from selected distro
- DNS resolution test from selected distro
- Basic host-to-WSL notes

Diagnostics should be presented in plain language, with raw command output available.

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
- accessible labels for action buttons
- confirmation dialogs that are readable and specific

## UX Requirements

## Dashboard UX

The dashboard should prioritize:

- current WSL status
- running distros
- warnings
- quick actions

The distro table should include:

```text
Name
State
Version
Default
Actions
```

Common actions should be visible inline.

## Per-Distro UX

The per-distro page should use this structure:

```text
Overview
Services
Filesystem
Config
Networking
Commands
Logs
```

MVP may only implement:

```text
Overview
Config
Commands
Diagnostics
```

## Confirmation UX

Confirmation dialogs must include:

- operation name
- affected distro
- whether the operation is destructive
- whether data loss is possible
- exact consequence
- cancel as the default option for dangerous operations

## Status UX

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
- It lists registered distros.
- It shows running or stopped state.
- It shows the default distro.
- It can open a distro.
- It can terminate a distro.
- It can set the default distro.
- It can shutdown WSL with confirmation.
- It can run a command inside a distro.
- It can show stdout, stderr, and exit code.
- It can read and edit `.wslconfig`.
- It can read and edit `/etc/wsl.conf`.
- It can run basic diagnostics.
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
- Distro list
- Open distro
- Terminate distro
- Shutdown all WSL
- Set default distro
- Per-distro overview
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
