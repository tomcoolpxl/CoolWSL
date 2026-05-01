# CoolWSL Design

## Overview

CoolWSL is a WinUI 3 desktop application for managing WSL environments on Windows 11.  
The design emphasizes clarity, safety, and fast access to common operations, while avoiding complexity and fragile behavior.

The UI is structured around two primary contexts:

- Global dashboard (system-level overview)
- Per-distro detail view (focused management)

The design should feel closer to a "control center" than a traditional settings application.

---

## Design Principles

### Clarity over density

- Show the most relevant information first.
- Avoid overwhelming users with low-level details.
- Provide progressive disclosure for advanced features.

### Safety-first UX

- Destructive actions must be explicit and confirmed.
- Side effects must be visible before execution.
- No hidden or implicit global actions.

### Fast access to common actions

- Frequently used actions must be one click away.
- Avoid deep nesting for core operations.

### Honest system representation

- Reflect actual WSL behavior.
- Do not abstract away important limitations.
- Do not hide failures or partial states.

### Resilient UI

- UI must remain usable even when WSL is unavailable.
- Partial failures must not break navigation.

---

## Layout Structure

### Main Window

```text
+------------------------------------------------------+
| Sidebar |                Content Area                |
+------------------------------------------------------+
````

### Sidebar Navigation

```text
Dashboard
Distros
Global Settings
Backups
Diagnostics
Logs
Settings
```

MVP may include:

```text
Dashboard
Distros
Diagnostics
Logs
Settings
```

---

## Dashboard Design

### Purpose

Provide a quick, accurate overview of:

- WSL health
- running distros
- actionable issues
- quick operations

### Layout

```text
+------------------------------------------------------+
| WSL Status Card                                      |
+------------------------------------------------------+
| Resource Summary | Alerts                            |
+------------------------------------------------------+
| Distro Table                                         |
+------------------------------------------------------+
| Recent Activity                                      |
+------------------------------------------------------+
```

---

### WSL Status Card

Displays:

- WSL installed or not
- WSL version
- kernel version
- default WSL version

States:

- Healthy
- Missing
- Error

---

### Resource Summary (1.0)

Displays:

- number of running distros
- approximate memory usage
- approximate CPU usage

MVP may omit resource visualization.

---

### Alerts Panel

Displays:

- failed diagnostics
- config requiring restart
- failed services (1.0)
- disk issues (1.0)

Alerts must:

- be dismissible
- include explanation
- include action if possible

---

### Distro Table

Columns:

```text
Name | State | Version | Default | Actions
```

Example:

```text
Ubuntu     Running   WSL2   Yes   [Open] [Terminate] [Details]
Debian     Stopped   WSL2         [Start] [Details]
```

Rules:

- Actions must be inline.
- Default distro must be visually distinct.
- Running state must be clearly indicated.
- Avoid icon-only actions without labels.

---

### Recent Activity (1.0)

Shows:

- recent commands
- exports
- config changes

---

## Per-Distro View

### Layout

```text
+------------------------------------------------------+
| Header (Name + Status + Actions)                     |
+------------------------------------------------------+
| Tabs                                                 |
+------------------------------------------------------+
| Tab Content                                          |
+------------------------------------------------------+
```

---

### Header

Displays:

- distro name
- running or stopped
- WSL version
- default indicator

Actions:

```text
[Open Terminal] [Run Command] [Terminate] [Set Default]
```

Rules:

- Always show distro name prominently.
- Disable invalid actions (e.g. terminate when stopped).

---

### Tabs

MVP:

```text
Overview
Commands
Config
Diagnostics
```

1.0:

```text
Overview
Services
Filesystem
Config
Networking
Commands
Logs
```

---

## Tab Designs

### Overview Tab

Displays:

- basic distro info
- default user if available
- system state summary

Optional (1.0):

- service health
- disk usage summary
- network summary

---

### Commands Tab

Purpose:

Run commands inside the distro.

Layout:

```text
Command Input Field
[Run] [Run as root]

Output Panel
-----------------------------------
STDOUT
STDERR
Exit Code
```

Requirements:

- monospace output
- scrollable
- copyable
- clear separation of stdout and stderr

Nice to have:

- command history dropdown
- save command

---

### Config Tab

Sections:

#### Global Config (.wslconfig)

- text editor
- validation hints
- save button
- revert button

#### Distro Config (/etc/wsl.conf)

- text editor
- validation hints
- save button

Warnings:

- "Changes require restart"
- "Incorrect config may break distro startup"

---

### Diagnostics Tab

Displays:

- WSL status output
- distro-specific checks
- DNS test
- internet test

Layout:

```text
Test Name | Result | Details | Action
```

Example:

```text
DNS Resolution     Failed    Timeout     [Retry]
Internet Access    OK        -           -
```

---

### Services Tab (1.0)

Displays:

- list of services
- status
- actions

```text
Service Name | Status | Actions
```

Actions:

- start
- stop
- restart

---

### Filesystem Tab (1.0)

Displays:

- disk usage (`df`)
- mount points

---

### Networking Tab (1.0)

Displays:

- IP address
- DNS servers
- routing
- connectivity checks

---

### Logs Tab (1.0)

Displays:

- app logs
- optional command logs

---

## Global Settings Page

### Layout

Sections:

- Memory
- CPU
- Swap
- Networking
- Advanced

Each section:

```text
Label
Input Control
Description
```

Example:

```text
Memory Limit
[ 4 GB ]
Limits WSL VM memory usage.
```

---

### Behavior

- Changes are staged.
- Banner shown:

```text
Changes require WSL restart.
[Restart Now] [Later]
```

---

## Backups Page (1.0)

Displays:

- list of exports
- export actions

Actions:

- export distro
- import distro
- delete backup

---

## Logs Page

Displays:

- command execution logs
- errors
- timestamps

Must support:

- filtering
- copying
- clearing logs

---

## Interaction Design

### Confirmation Dialogs

Must include:

- operation name
- affected distro
- consequences
- cancel button as default

Example:

```text
Terminate Ubuntu?

This will stop all processes in the distro.

[Cancel] [Terminate]
```

Destructive example:

```text
Unregister Ubuntu?

This will permanently delete all data.

Type "Ubuntu" to confirm:
[________]

[Cancel] [Delete]
```

---

### Disabled States

Buttons must be disabled when:

- action is not valid
- WSL unavailable
- distro not running when required

---

### Loading States

Show:

- spinner or progress bar
- operation label

Example:

```text
Exporting Ubuntu...
[Cancel]
```

---

### Error States

Display:

- plain description
- command executed
- exit code
- stderr snippet

Avoid raw dumps without explanation.

---

## Visual Design

### Style

- Clean, minimal
- Native Windows look (WinUI)
- No excessive color usage

### Color usage

- Green: success
- Yellow: warning
- Red: error
- Neutral: normal state

### Typography

- Clear hierarchy
- Monospace for command output

---

## Accessibility

Must support:

- keyboard navigation
- screen readers
- high contrast
- focus indicators
- accessible labels

---

## Responsiveness

The app should:

- adapt to smaller window sizes
- allow resizing panels
- keep command output usable in narrow layouts

---

## State Management

UI must handle:

- WSL not installed
- WSL stopped
- partial data availability
- command failures
- long-running operations

UI must never freeze during command execution.

---

## Error UX Principles

- Always explain what failed.
- Never hide raw output.
- Provide retry where possible.
- Do not assume recovery.

---

## Future Design Considerations

- multi-window support
- tabbed distro views
- plugin system
- Docker/Kubernetes integration panels
- remote WSL management (if ever supported)

---

## Summary

The CoolWSL design should:

- center around a dashboard + per-distro model
- prioritize clarity and safety
- expose WSL functionality without hiding its nature
- avoid fragile or undocumented behavior
- remain fast and responsive for common workflows
