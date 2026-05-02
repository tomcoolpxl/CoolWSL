# CoolWSL Design

## Overview

CoolWSL is a WinUI 3 desktop application for managing WSL environments on Windows 11.

The UI direction is a Windows 11-native shell that combines the entity-first navigation of Docker Desktop with the visual language of Windows Terminal Settings. Users should move through one shell with fixed global destinations, first-class distro navigation, real card surfaces, and a persistent status bar instead of hopping between a flat set of unrelated top-level pages.

The shell is organized around:

- fixed destinations: Dashboard, Logs, Settings
- a dynamic Distros group where each distro is its own navigation item
- a persistent bottom status bar
- a distro detail page with a pivot: Overview, Terminal, Configuration, Diagnostics — the Diagnostics pivot is the only home for full diagnostic results

The design should feel like a Windows 11 control center rather than a generic admin console.

## Delivery Baseline

- Packaged WinUI 3 desktop app delivered as signed MSIX.
- WSL2-first UX with explicit degraded states for WSL1 and older WSL surfaces.
- Docker Desktop distros stay visible, but they are treated as system-managed targets.
- The app remains unelevated in the first release; admin-only actions are disabled and explained.
- Logs are metadata-only by default and retain 30 days unless the user changes retention later.
- The window should use standard Windows 11 chrome, a native backdrop treatment when text clarity remains intact, and app-wide shared styles for cards, typography, spacing, and status indicators.

---

## Design Principles

### Windows-native fidelity

- The app should look and behave like a first-party Windows 11 desktop utility.
- Standard title-bar behavior, Fluent icons, theme brushes, and card surfaces should be the default.
- Typography should follow a small, consistent type ramp instead of page-specific magic numbers.

### Distro-first navigation

- A distro is a primary entity, not a secondary dropdown choice.
- Selecting a distro should be one click from the rail.
- The shell should not require a top-level Distros page before a user can act on a distro.

### Clarity over density

- Show the most relevant information first.
- Avoid action overload in repeated lists.
- Use progressive disclosure for advanced or destructive workflows.

### Safety-first UX

- Destructive actions must be explicit and confirmed.
- Side effects must be visible before execution.
- Global actions must remain visually distinct from per-distro actions.

### Honest diagnostics

- Diagnostics live in the per-distro Diagnostics pivot, which acts as the only full-detail home.
- Summaries may appear elsewhere (for example a compact dashboard health card), but the full diagnostic story is not duplicated as a separate shell destination.
- Raw evidence should stay available behind the summary.

### Resilient UI

- UI must remain usable even when WSL is unavailable.
- Partial failures must not break navigation.
- Empty states should always explain the next useful action.

### Readable and scrollable content

- Text must remain crisp on supported DPI scales.
- Secondary text should use theme brushes instead of opacity-based demotion.
- Page layouts should avoid nested scroll regions that trap the mouse wheel.

---

## Layout Structure

### Main Window

```text
+--------------------------------------------------------------+
| Standard title bar / drag region                             |
+--------------+-----------------------------------------------+
|              |                                               |
| Navigation   | Content area                                  |
| rail         |                                               |
|              |                                               |
+--------------+-----------------------------------------------+
| Persistent status bar                                        |
+--------------------------------------------------------------+
```

The title bar should stay close to the standard Windows 11 height instead of consuming a large custom chrome band.

### Shell Navigation

```text
Dashboard
Logs
Settings

Distros
  Ubuntu
  Debian
  docker-desktop
```

Rules:

- Dashboard, Logs, and Settings are the fixed global destinations.
- Distros is a dynamic group driven by the live distro inventory.
- Each distro item shows name plus a compact state indicator.
- The currently selected distro item opens the distro detail page.
- Diagnostics are reached through the Diagnostics pivot inside a distro detail page rather than through a global rail destination.
- Backups and other secondary workflows should be entered from Settings or contextual actions until they justify first-class placement.

### Status Bar

The bottom status bar is always visible and should surface:

- WSL availability or version
- default distro
- running-distro count
- last refresh time

The status bar provides persistent global context so users do not need to return to the Dashboard to answer basic questions.

---

## Shared UI Primitives

The shell should standardize a small set of reusable primitives instead of styling each page independently.

- Card: opaque background, 1 px stroke, 8 px corner radius, standard inner padding
- Page header: title, subtitle, optional action slot
- Status pill: running, stopped, warning, system-managed, unavailable
- Empty state: glyph, headline, explanatory body, primary action
- Output block: monospace output region with copy and clear affordances
- Status bar: global summary surface bound to refresh state
- SettingsCard and SettingsExpander patterns for settings-like and diagnostics-like content

Shared primitives matter because the shell must look intentional and consistent even as later phases add services, networking, logs, and configuration editors.

---

## Dashboard

### Purpose

The Dashboard answers:

- Is WSL healthy enough to use?
- Which distros exist and which are running?
- Which distro is the default?
- Are there urgent warnings?
- What are the safest quick actions right now?

### Layout

```text
+--------------------------------------------------------------+
| Page header + Refresh                                        |
+--------------------------------------------------------------+
| Hero status card                                             |
+--------------------------------------------------------------+
| Quick actions                                                |
+--------------------------------------------------------------+
| Distro tiles                                                 |
+--------------------------------------------------------------+
| Health summary                                               |
+--------------------------------------------------------------+
```

### Hero Status Card

Displays:

- WSL installed, unavailable, or error state
- WSL version
- kernel version
- default WSL version
- plain-language summary of the current environment
- warning or next-step guidance when applicable

### Quick Actions

MVP quick actions should emphasize global actions that are safe and common:

- Refresh
- Open default terminal
- Shutdown all WSL

Global actions should not be visually mixed with per-distro actions in the same repeating list.

### Distro Surface

The dashboard should present distros as tiles or simple card rows rather than as a dense table with four inline buttons per row.

Each item should show:

- distro name
- state
- WSL version
- default marker
- system-managed or reduced-capability messaging when applicable

Behavior:

- primary click opens the distro detail page
- secondary lifecycle actions belong in the detail page or an overflow surface
- Docker-managed distros omit destructive shortcuts

### Health Summary

The dashboard may show a compact list of the most important warnings. Each summary entry should deep-link into the relevant distro's Diagnostics pivot for full detail.

---

## Distro Detail

### Layout

```text
+--------------------------------------------------------------+
| Header: name, status, version, default                       |
+--------------------------------------------------------------+
| Pivot: Overview | Terminal | Configuration | Diagnostics     |
+--------------------------------------------------------------+
| Active pivot content                                         |
+--------------------------------------------------------------+
```

### Header

Displays:

- distro name
- running or stopped state
- WSL version
- default indicator
- capability notes when the distro is WSL1 or system-managed

Primary actions:

- Open terminal
- Start distro
- Terminate distro
- Set default

Rules:

- Always show the target distro prominently.
- Disable invalid actions rather than hiding state.
- Explanations must be visible when an action is unavailable because of WSL1, system-managed handling, or admin-only limits.

### Overview Pivot

Shows:

- basic distro identity and state
- capability summary
- lifecycle actions
- future health or resource summaries when available

This pivot should act as the user's primary management surface for safe per-distro actions.

### Terminal Pivot

Purpose:

Run commands inside the selected distro.

Layout:

```text
Command input + Run + Cancel

Primary output area
Status / exit code / timing

History expander
```

Rules:

- Use one primary output area instead of a permanent split pane.
- Stdout and stderr remain distinguishable through styling, labels, or inline markers.
- Output is monospace, scrollable, copyable, and clearable.
- Command history is session-scoped by default and collapsed when not needed.

### Configuration Pivot

The distro configuration pivot owns `/etc/wsl.conf` editing and later structured controls.

It should support:

- raw editor
- validation hints
- save and revert actions
- restart-required messaging
- future structured controls when supported

### Diagnostics Pivot

The per-distro Diagnostics pivot is the single primary home for full diagnostic results. It owns both global checks (rendered with the selected distro as context) and per-distro probes.

It should include:

- WSL availability and version checks (`wsl --status`, `wsl --version`)
- distro inventory and default-distro health
- DNS checks for the selected distro
- internet connectivity checks for the selected distro
- distro-specific notes and host-to-WSL guidance
- raw command output on demand

Layout should be organised for triage rather than execution order:

```text
+--------------------------------------------------------------+
| Refresh + last updated                                       |
+--------------------------------------------------------------+
| Error group                                                  |
+--------------------------------------------------------------+
| Warning group                                                |
+--------------------------------------------------------------+
| OK group                                                     |
+--------------------------------------------------------------+
```

Each result should surface title, severity, short summary, expandable details, raw output, and a suggested next step where safe.

### Future 1.0 Extensions

Version 1.0 may add Services, Filesystem, and Networking as additional pivots or secondary routes, but the primary shell model should stay the same.

---

## Settings

Settings is a fixed global destination built in a Windows 11 Settings style.

Recommended groups:

- WSL
- Appearance
- Behavior
- Diagnostics
- About

MVP content belongs here:

- `.wslconfig` editor
- theme selection
- confirmation behavior
- command timeout defaults
- logging behavior

Settings content should use settings cards, descriptive copy, and clear restart-required messaging rather than free-form stacked controls.

---

## Secondary Flows

### Logs

Logs are a fixed global surface backed by the metadata-only app logger.

The logs surface should support:

- filtering
- copying
- metadata-first entries by default
- retention messaging for the 30-day default

### Backups and Import/Export

Import and export are important workflows, but they are secondary to the main navigation model.

These flows should be launched from:

- contextual actions on a distro
- Settings
- dedicated dialogs or secondary pages when the workflow is complex enough

They should not displace Dashboard, Logs, Settings, or the distro rail in the main shell.

---

## Interaction Design

### Confirmation Dialogs

Dialogs must include:

- operation name
- affected distro
- consequences
- cancel as the default action for dangerous operations

Example:

```text
Terminate Ubuntu?

This will stop all processes in the distro.

[Cancel] [Terminate]
```

Strong destructive example:

```text
Unregister Ubuntu?

This will permanently delete all data.

Type "Ubuntu" to confirm:
[________]

[Cancel] [Delete]
```

### Disabled States

Buttons must be disabled when:

- the action is invalid
- WSL is unavailable
- the distro is not in the required state
- the action would require elevation in the current release

Disabled controls must explain why when the target is WSL1, system-managed, or blocked by an admin-only requirement.

### Loading States

Long-running workflows should show:

- spinner or progress bar
- operation label
- target distro or global scope
- cancel affordance where safe

### Error States

Errors should display:

- plain description
- command executed
- exit code
- stderr snippet
- suggested next step when safe

Avoid raw dumps without explanation, but never hide raw output.

### Keyboard Model

MVP keyboard behavior should preserve existing core shortcuts and reserve room for shell-wide shortcuts later.

- `F5` refreshes the current data surface
- `Ctrl+Enter` runs the current command
- `Esc` cancels a running command where safe

Later candidates include `Ctrl+,` for Settings and `Ctrl+K` for a command palette.

### Scrolling Model

Page content should scroll as one coherent surface.

- Avoid nested scroll viewers for primary content lists.
- Prefer repeaters inside the page scroll surface over nested list controls when selection is not needed.
- Scroll regions should not become meaningless Tab stops.

---

## Visual Design

### Backdrop and Chrome

- Prefer a standard Windows 11 title bar and drag region over tall custom chrome.
- Use a native backdrop treatment such as Mica when it does not compromise text rendering.
- App iconography should feel deliberate and Windows-native.

### Cards and Surfaces

- Primary content lives on real cards with opaque fills, 1 px strokes, and rounded corners.
- Avoid transparent container surfaces that reduce perceived text sharpness.
- Secondary text should use theme brushes, not opacity on the text element.

### Typography and Spacing

Use a small shared ramp:

- 28 px page title
- 20 px section header
- 18 px subsection header
- 14 px body
- 12 px caption

Use a shared spacing scale such as 4, 8, 12, 16, 24, and 32.

### Icons and Semantic Color

- Use Fluent-style icons for navigation, actions, and state.
- Semantic colors are reserved for status and severity.
- Success, warning, error, and neutral states must remain readable in high contrast.

---

## Accessibility

The design must support:

- keyboard navigation
- screen readers
- high contrast
- scalable text
- strong focus indicators
- accessible labels that include the affected distro where relevant
- live announcements for async completion and status changes

The shell should avoid focus traps, dead scroll regions, and visual-only state cues.

---

## Responsiveness

The app should:

- adapt to smaller window sizes
- allow the rail to collapse to icon-only mode
- keep card layouts readable on narrow widths
- preserve usable terminal output on smaller windows
- keep the status bar compact rather than forcing page reflow

---

## State Management

UI must handle:

- WSL not installed
- WSL unavailable
- partial data availability
- command failures
- long-running operations
- no distros installed
- reduced capability for WSL1 and system-managed distros

UI must never freeze during command execution.

---

## Error UX Principles

- Always explain what failed.
- Never hide raw output.
- Provide retry where possible.
- Do not assume recovery.
- Keep the failing target explicit.

---

## Future Design Considerations

- multi-window support
- tabbed distro views
- command palette
- richer logs surface
- remote WSL management if the backend model ever expands

---

## Summary

The CoolWSL design should:

- center around one Windows 11-native shell
- treat distros as first-class navigation entities
- keep diagnostics in one primary home
- prioritize clarity, safety, and crisp readable content
- avoid fragile or visually inconsistent UI patterns
- remain extensible for later services, networking, logs, and configuration work without rethinking the shell
