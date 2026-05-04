# CoolWSL Design

This document describes the current v1 user experience and visual design.

Historical roadmap sections have been removed. Future UX expansion ideas belong in [EXTRA_FEATURES.md](EXTRA_FEATURES.md).

## Design Intent

CoolWSL should feel like a Windows 11 control center for WSL rather than a generic admin console.

The design direction is:

- distro-first navigation.
- honest diagnostics.
- safe lifecycle and configuration actions.
- readable, width-bound layouts.
- consistent card-based surfaces.
- minimal duplication between global and per-distro workflows.

## Core Principles

### Windows-native fidelity

- Use WinUI 3 controls, theme brushes, standard window controls, and Fluent iconography.
- Prefer calm Windows utility styling over custom chrome theatrics.
- Keep the title bar slim and integrated with the rest of the shell.

### Distro-first navigation

- A distro is a first-class entity in the navigation rail.
- Users should not have to enter a separate Distros hub before acting on a distro.
- Diagnostics are contextual to a selected distro rather than split into a disconnected global page.

### Clarity over density

- Lead with state and next actions, not raw command output.
- Use summaries first and details on demand.
- Avoid turning repeated inventory rows into action toolbars.

### Safety over cleverness

- Destructive actions stay explicit.
- Unsupported or reduced-capability states are explained in plain language.
- Global settings are handed off to the official WSL Settings app instead of being recreated in full.

### Readability first

- Text must stay crisp on opaque surfaces.
- Secondary and tertiary emphasis should come from theme brushes rather than control opacity.
- Pages should avoid nested scrolling regions that trap the mouse wheel.

## Shell Composition

The current shell is a four-part composition:

```text
+--------------------------------------------------------------+
| Slim title bar                                               |
+--------------+-----------------------------------------------+
| Navigation   | Content frame                                 |
| rail         |                                               |
+--------------+-----------------------------------------------+
| Persistent status bar                                        |
+--------------------------------------------------------------+
```

### Navigation model

The navigation rail is organized as:

```text
Dashboard

Distros
  Ubuntu
  Debian
  docker-desktop

Logs
Settings
```

Rules:

- `Dashboard` is the landing page.
- `Distros` is generated from live inventory.
- `Logs` and `Settings` live in the footer.
- There is no separate top-level Diagnostics destination.
- Selecting a distro opens one dedicated distro detail page.

### Status bar

The bottom status bar remains visible across the shell and carries:

- WSL availability or version.
- default distro.
- running-distro count.
- last refresh time.

It provides persistent global context so the user does not need to keep returning to the dashboard.

## Shared Visual System

The app uses a small set of shared primitives rather than page-specific one-off styles.

### Cards and layout

- Opaque card surfaces with rounded corners and a light stroke.
- Centered, width-bound content hosts on major pages.
- Consistent vertical stacking with generous spacing.
- `ItemsRepeater` for repeated content where nested `ListView` scrolling would be a liability.

### Typography

- Page titles use the system title styles.
- Secondary and tertiary text use shared theme-aware text styles.
- Monospace text is reserved for paths, config content, and raw diagnostic output.

### Semantic states

- Small status pills communicate running state and diagnostic severity.
- Accent color highlights primary entry points such as `Open terminal` or `WSL Settings`.
- Caution and critical colors are reserved for warning and error states.

### Chrome and backdrop

- The window uses a slim custom title region with native caption buttons.
- Mica is intentionally disabled in the current design because the text-clarity pass prioritized opaque surfaces and ClearType rendering.

## Dashboard

### Purpose

The dashboard answers four questions quickly:

- Is WSL available?
- What distros exist?
- Which distro is the default?
- What safe global actions are available right now?

### Dashboard composition

The page is built in this order:

```text
Header
Last refreshed text
Hero status card
Action status text
Quick actions
Distro section heading
Optional empty-state card
Distro tiles
```

### Hero status card

The hero card combines:

- availability label.
- plain-language summary.
- optional warning.
- optional suggested next step.
- detail grid for WSL version, kernel version, and default WSL version.

### Quick actions

The dashboard keeps only safe, common global actions visible:

- `Refresh`.
- `Open default WSL terminal`.
- `Shutdown all WSL`.

Per-distro lifecycle actions are intentionally moved off the dashboard and into the distro detail page.

### Distro tiles

Each tile shows:

- distro name.
- default badge when applicable.
- system-managed label when applicable.
- capability message.
- running or stopped pill.
- WSL generation label.

Primary click opens the distro detail page and syncs with the shell selection when possible.

## Distro Detail

### Page structure

The distro page uses a fixed header plus a pivot body:

```text
Header + refresh
Last refreshed / warning / action status
Pivot: Overview | Settings | Diagnostics
```

The page keeps an explicit empty state when no distro is selected or no matching distro can be resolved.

### Header

The header shows:

- the `Distros` page title.
- selected distro name.
- running-state pill.
- metadata text summarizing WSL version and default / management state.
- refresh affordance.
- warning or action status text when needed.

### Overview pivot

The overview pivot is a stack of action cards, one action per card:

- `Open terminal`.
- `Start`.
- `Terminate`.
- `Set as default`.

Each card pairs a glyph, a bold title, a short description, and an action button. This keeps lifecycle operations readable and avoids the feel of a cramped tool matrix.

Terminal launch is intentionally external. The app opens the distro in Windows Terminal when available rather than embedding a terminal surface in the page.

### Settings pivot

The settings pivot owns per-distro configuration and is built around a dual representation of `/etc/wsl.conf`.

Current composition:

- global WSL summary card with `WSL Settings` handoff.
- action row with `Defaults`, `Revert`, and `Save`.
- settings header card showing the selected distro and file path.
- structured settings card rendered from the supported schema.
- second action row for convenience after the structured editor.
- raw editor card.
- validation card shown when edits are pending.
- restart-required card when the edit implies restart impact.
- backup-path card after save or restore-defaults flows.

The structured editor and raw editor are two views over the same document. Editing either surface updates the shared model.

### Diagnostics pivot

The diagnostics pivot is the only full-detail diagnostics home in the product.

It renders a vertically stacked list of result cards. Each card can show:

- result title.
- severity pill.
- summary.
- optional detail text.
- optional next-step text.
- optional raw command text.
- optional raw output box.

This pivot deliberately mixes global WSL checks with the selected distro's DNS and internet probes so the user gets one coherent troubleshooting surface instead of two competing pages.

## Settings Page

The global Settings page is an expander-based surface with four current groups:

- `WSL`.
- `Appearance`.
- `Behaviour`.
- `About`.

### WSL group

This group shows:

- WSL status.
- default distro.
- inventory summary.
- safe global actions.
- read-only `.wslconfig` state and content.
- `WSL Settings` handoff.

It is intentionally a summary-and-handoff surface, not a full global WSL editor.

### Appearance group

This group contains the only currently persisted app preference: theme selection.

The design keeps the disabled `Use Mica backdrop` toggle visible as explanatory UI, but the current product behavior is fixed: Mica stays off to protect text clarity.

### Behaviour group

The behaviour group exists to reserve the long-term settings shape, but its current controls are non-editable placeholders. The page should visually communicate that these controls are not active product behavior yet.

### About group

The About group provides:

- product identity.
- current version.
- repository link.
- issue-report link.

## Logs Page

The Logs page is a dedicated metadata viewer for the current app session.

### Logs composition

- page header.
- `Clear` and `Refresh` actions.
- last refreshed text.
- filter card with level filter, search box, and summary text.
- empty state when no entries match.
- repeated list of log rows.

### Log row design

Each row shows:

- timestamp.
- level.
- area.
- message.

The page is intentionally plain. It is an inspection surface, not a visual centerpiece.

## Interaction Rules

### Refresh model

- Major pages provide `F5` refresh.
- Refresh state is visible through progress rings and last-loaded text.
- Newer refreshes win over stale completions.

### Confirmation model

- Destructive flows must require explicit confirmation.
- Confirmation copy must name the target and the impact.

### Disabled states

- Disabled controls should remain visible when they communicate unavailable capability.
- Reduced capability on WSL1 or system-managed distros should be explained inline.

### Scrolling model

- Pages use one primary vertical scroll surface.
- Repeated content prefers `ItemsRepeater` to avoid nested list scroll viewers.
- Scroll surfaces should not steal focus unnecessarily.

## Accessibility and Readability

- Major actions carry explicit automation names.
- Semantic states should not rely on color alone.
- Text must stay legible on light, dark, and high-contrast themes.
- Opaque surfaces and theme brushes take precedence over visual effects that soften text.

## Future Work

Future UX and information-architecture work lives in [EXTRA_FEATURES.md](EXTRA_FEATURES.md).

If a flow is not described here as current behavior, it should not be implied by old roadmap language.
