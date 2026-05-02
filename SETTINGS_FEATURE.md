# Settings Feature Design

## Decision Summary

CoolWSL should support both structured settings UI and raw file editing.

That is not a bad idea by itself. It only becomes a bad idea if:

- structured editing and raw editing write to different models
- global and per-distro scopes are mixed into one ambiguous surface
- the app silently rewrites or normalizes files in a way that destroys manual edits
- the app pretends changes are live when WSL actually requires restart or shutdown semantics

The right model is the same principle that makes Visual Studio Code work well:

- one source of truth per scope
- multiple views over the same source of truth
- explicit scope selection
- obvious default, modified, and overridden state

## Core Recommendation

Treat settings as three different products that happen to live near each other:

1. App settings
2. Global WSL settings
3. Per-distro WSL settings

Those are not just different sections. They have different files, ownership, safety constraints, and restart behavior.

Because of that, the recommended information architecture is:

- `Settings` global destination owns App settings and global `.wslconfig`
- `Distro > Configuration` owns per-distro `/etc/wsl.conf`
- `Settings` may offer a distro picker that jumps to a distro configuration page, but it should not become the second primary home for per-distro config

This aligns with the current repository plan and avoids building two competing homes for configuration.

## Why This Separation Matters

### App settings

These are CoolWSL preferences such as theme, logging, timeouts, refresh behavior, confirmations, and saved command profiles.

- They belong to the app.
- They should live under `%LocalAppData%\CoolWSL\`.
- They do not change WSL itself.
- They do not require WSL restart.

### Global WSL settings

These are `.wslconfig` settings.

- They live in `%UserProfile%\.wslconfig`.
- They apply globally to WSL 2 distributions.
- They affect the WSL VM and host-level behavior.
- They often require `wsl --shutdown` or a full subsystem restart before changes take effect.
- Some settings are version-gated or experimental.

### Per-distro WSL settings

These are `/etc/wsl.conf` settings.

- They live inside a specific distro.
- They apply only to that distro.
- They can affect boot, systemd, automount, interop, DNS, hostname, and default user.
- They often require terminating or restarting that distro before changes take effect.
- Saving may require elevated or distro-local write behavior.

If CoolWSL blurs these scopes, users will make the wrong edit in the wrong place and blame the app.

## Online Findings

### Visual Studio Code settings model

Sources consulted:

- [Configure settings in VS Code](https://code.visualstudio.com/docs/configure/settings)
- [VS Code settings guide](https://code.visualstudio.com/docs/getstarted/settings)
- [VS Code default settings reference](https://code.visualstudio.com/docs/reference/default-settings)

Useful patterns worth copying:

- explicit scope selection such as User vs Workspace
- UI editor and JSON editor both target the same underlying settings file
- modified indicators
- default values remain visible or recoverable
- search and filter-first discovery
- some settings are UI-editable, some require raw JSON, and the UI says so directly
- advanced and preview settings are labeled instead of hidden behind guesswork

Key lesson:

VS Code succeeds because the user always knows which scope is being edited and can always drop to the file. The UI is an accelerator, not a second persistence system.

### WSL configuration model

Source consulted:

- [Advanced settings configuration in WSL](https://learn.microsoft.com/en-us/windows/wsl/wsl-config)

Important product constraints:

- `.wslconfig` is global and only affects WSL 2 distributions
- `/etc/wsl.conf` is per-distro and applies to WSL 1 and WSL 2 distro behavior
- config changes are not truly live; restart semantics matter
- `.wslconfig` can be missing and may need to be created
- malformed files do not necessarily stop WSL from launching, but settings may be ignored
- several keys are version-gated, platform-gated, deprecated, or experimental

Key lesson:

CoolWSL should behave like a careful config editor, not a live control panel.

## Direct Answer To The Product Question

Yes, users should be able to use both structured UI and raw file editing.

No, they should not be presented as two separate configuration systems.

Yes, users should be able to choose between global and per-distro settings.

No, that choice should not collapse the app into one giant settings page that ignores the distro-first architecture.

The right mental model is two axes:

- scope: app, global WSL, or per-distro WSL
- mode: structured or raw

Scope should be chosen first. Editing mode should be chosen second.

## Recommended Information Architecture

### Global `Settings` destination

This page should own:

- App preferences
- Global WSL configuration summary
- Global `.wslconfig` structured editor
- Global `.wslconfig` raw editor
- WSL restart-required messaging
- A jump affordance into a selected distro's Configuration page

Recommended top-level sections:

1. App
2. Global WSL
3. Advanced or About

`Logs` should stay separate.

`Per-distro config` should not be a peer tab here unless the user explicitly opens a distro-scoped editor from a picker and the UI remains obviously distro-bound.

### `Distro > Configuration`

This page should own:

- selected distro summary
- capability warnings such as WSL 1, system-managed distro, or unsupported keys
- structured `/etc/wsl.conf` editor
- raw `/etc/wsl.conf` editor
- per-distro restart messaging
- backup and write-limitation messaging

This keeps the distro as the active context, which matters because many settings only make sense once a specific distro is in view.

## Recommended UX Shape

### For Global WSL configuration

At the top of the `.wslconfig` experience:

- file path
- file existence state
- last loaded time
- capability summary
- restart-required banner when applicable
- buttons for `Save`, `Revert`, `Create file`, and `Open raw editor`

Below that, provide a mode switch:

- `Structured`
- `Raw`

Within `Structured`, group controls like this:

- Resources: memory, processors, swap, swap file, VM idle timeout
- Networking: localhost forwarding, networking mode, DNS tunneling, firewall, auto proxy
- Virtualization and kernel: nested virtualization, custom kernel path
- Experimental: auto memory reclaim, sparse VHD

Within `Raw`, provide:

- text editor
- syntax and semantic validation panel
- warning panel for unsupported or ignored keys
- explicit note that file changes are applied on next WSL restart, not immediately

### For Per-distro configuration

At the top of the `/etc/wsl.conf` experience:

- distro name
- WSL version and management badge where relevant
- file path
- file existence state
- last loaded time
- restart-required banner when applicable
- buttons for `Save`, `Revert`, `Create file`, and `Open raw editor`

Below that, provide the same mode switch:

- `Structured`
- `Raw`

Within `Structured`, group controls like this:

- User and boot: default user, boot command, systemd
- Filesystem and automount: automount, fstab processing, mount root, Windows path interop
- Network identity: hostname, generated hosts, generated resolv.conf
- Integration and hardware: GPU support, timezone sync

Within `Raw`, provide the same text editor and validation model.

## Recommendation On VS Code-Like Behavior

Copy the principles, not the full shell.

What to borrow from VS Code:

- search-first editing
- explicit scope label near the page title
- modified indicators on changed settings
- `reset to default` or `unset` actions per setting
- clear handoff between structured UI and raw file editing
- filters such as `modified`, `requires restart`, `advanced`, and `experimental`
- visible default values and descriptions

What not to copy literally:

- a giant universal settings surface that mixes app preferences, global WSL VM settings, and per-distro Linux settings without context
- instant auto-save on every change
- pretending file-backed config behaves like a regular preference store

VS Code can auto-apply many settings because it owns the runtime. CoolWSL does not own WSL restart semantics, so explicit save is the safer model.

## Source Of Truth Model

This is the most important implementation rule for the future feature.

For each file scope, structured mode and raw mode must edit the same in-memory document model.

That means:

- load the file once into a document model
- raw editor mutates that document model
- structured controls mutate that same document model
- save serializes that one model back to disk
- unsaved changes are tracked once, not separately per tab

If this rule is violated, the feature will be fragile.

### Required document behavior

The document model should preserve, as much as practical:

- comments
- unknown sections
- unknown keys
- ordering
- whitespace where it is not harmful

Why this matters:

- advanced users will hand-edit these files
- WSL may add keys CoolWSL does not know yet
- round-trip integrity is already a stated repository requirement

If a file is too malformed to safely represent in structured mode, the correct fallback is:

- keep raw editor available
- show structured mode as temporarily unavailable
- explain why
- do not silently rewrite the file into a normalized shape just to make the UI happy

## Validation Model

Validation should happen in four layers.

### 1. Syntax validation

Examples:

- malformed INI section header
- duplicate section ambiguity
- invalid key-value shape

### 2. Type validation

Examples:

- invalid boolean
- invalid numeric range
- invalid size units
- malformed Windows path for `.wslconfig`

### 3. Capability validation

Examples:

- `.wslconfig` setting only valid for WSL 2
- networking mode values unavailable on older WSL or Windows versions
- systemd requiring minimum WSL version
- Docker Desktop or system-managed distros excluded from config editing flows

### 4. Consequence warnings

Examples:

- requires full WSL shutdown
- requires distro restart
- affects boot sequence
- affects systemd
- affects networking resolution
- may break Windows interop

Not every warning should block save. The UI should distinguish:

- blocking error
- warning with save allowed
- informational note

## Effective Value And State Presentation

Each structured setting row should show more than a control.

Recommended metadata per setting:

- setting name
- exact key id such as `wsl2.memory` or `network.generateHosts`
- current value
- default value
- current source or scope
- capability state
- consequence tags

Useful tags:

- `Modified`
- `Default`
- `Requires restart`
- `Experimental`
- `Deprecated`
- `Unsupported on this machine`

This is where the VS Code influence is most valuable.

## Search And Filtering

Search is worth adding early because the setting list will grow.

Recommended search behavior:

- search by label
- search by setting id
- search by description
- search by section

Recommended quick filters:

- `Modified`
- `Requires restart`
- `Advanced`
- `Experimental`
- `Errors`

The goal is not to mimic VS Code syntax exactly. The goal is to make a large settings surface navigable.

## Save Model

Use explicit save, not live apply.

Required actions:

- `Save`
- `Revert`
- `Reset setting`
- `Reset section`
- `Create file if missing`

Recommended save flow:

1. Validate current document.
2. If save is allowed, create backup where supported.
3. Write new content.
4. Show backup path clearly.
5. Show restart guidance clearly.

Do not automatically run `wsl --shutdown` after global changes.

Do not automatically terminate a distro after per-distro changes.

Those can be offered as explicit follow-up actions, but not silently performed.

## Backup And Recovery

Global `.wslconfig` is straightforward:

- always back up before overwrite
- surface backup path in the save result UI

Per-distro `/etc/wsl.conf` is more nuanced:

- back up before overwrite where feasible
- when backup is not feasible, say so before save
- if writing requires elevated or distro-local steps, expose the limitation plainly

Recommended recovery affordances:

- `Restore previous backup`
- `Copy backup path`
- `Open backup folder`

## Handling Missing, Unknown, And Malformed Files

### Missing `.wslconfig`

This is normal.

The UI should say:

- the file does not exist yet
- global WSL settings are currently defaults
- creating the file is safe and expected

### Missing `/etc/wsl.conf`

This is also normal.

The UI should say:

- this distro is using defaults
- create the file to override distro behavior

### Unknown keys

These should be preserved.

The structured UI may omit editing them, but it should not delete them.

### Malformed file

This should not trigger aggressive repair.

Preferred behavior:

- keep raw editor openable
- surface parse problem clearly
- disable structured editing if round-trip safety cannot be guaranteed

## Anti-Patterns To Avoid

### Anti-pattern 1: One giant universal Settings page

Why it fails:

- destroys the distro-first information architecture
- mixes unrelated scopes
- makes restart guidance harder to explain

### Anti-pattern 2: Structured settings saved to app storage, raw settings saved to the real file

Why it fails:

- users will see mismatches immediately
- raw edits will appear to "not stick"
- round-trip integrity is lost

### Anti-pattern 3: Silent normalization

Why it fails:

- comments and unknown keys disappear
- advanced users lose trust

### Anti-pattern 4: Live apply toggles

Why it fails:

- WSL config is file-backed and restart-driven
- the UX will promise more than the platform can deliver

### Anti-pattern 5: Duplicating per-distro config in both `Settings` and `Distro > Configuration`

Why it fails:

- users no longer know the canonical place for distro config
- one surface will drift behind the other

## Recommended Feature Phasing

This design fits the existing roadmap well.

### Phase 8

Deliver raw `.wslconfig` editor first.

Why:

- lowest conceptual risk
- establishes file IO, validation, backup, and restart messaging
- provides the raw mode that later structured mode must round-trip against

### Phase 9

Deliver raw `/etc/wsl.conf` editor next on the distro Configuration page.

Why:

- same editing architecture
- adds distro-scoped file access and write constraints

### Phase 11

Add structured global WSL settings UI over the same `.wslconfig` document model.

### Phase 12

Add structured per-distro settings UI over the same `/etc/wsl.conf` document model.

### Phase 18

Add true app preferences in app-owned storage.

This order matters. The raw editors should define the persistence and recovery model before the structured editors arrive.

## Product Recommendation In One Sentence

Support both structured and raw editing, but keep scope explicit and single-source-of-truth:

- global WSL config in `Settings`
- per-distro config on the distro `Configuration` page
- app preferences separate from both

## Open Questions

These do not block the overall design, but they do affect polish.

1. Should `Settings` include only a jump-to-distro-config affordance, or should it also host a lightweight distro picker that opens the selected distro config in-place?
2. For malformed files, should structured mode become fully read-only, or should it allow editing of unaffected known keys while preserving the raw text buffer?
3. Should experimental `.wslconfig` keys be hidden by default behind an `Advanced` filter, or shown inline with explicit badges?

## Proposed Default Answers

If no further product decision is made, the safest defaults are:

- `Settings` only jumps to distro config instead of hosting it in-place
- malformed files force raw-only mode until parsing is repaired
- experimental keys are visible only when `Advanced` or `Experimental` filters are active
