# EXTRA1 Implementation Plan - Per-Distro Settings (`/etc/wsl.conf`)

This plan delivers EXTRA1 from `EXTRA_FEATURES.md`: a first-class per-distro `/etc/wsl.conf` editor inside the distro detail page, with structured controls, a lossless raw editor, validation, restart guidance, and explicit handoff to the official WSL Settings app for global `.wslconfig` changes.

It also performs two adjacent IA edits the user requested:

1. Rename the per-distro **Configuration** pivot to **Settings**.
2. Remove the per-distro **Terminal** pivot.

> Per `GEMINI.md`: this plan does not modify `CLAUDE.md`, only this new file. `REQUIREMENTS.md` and `IMPLEMENTATION_PLAN.md` updates required by these IA changes are listed in [Section 14](#14-document-updates-required).

---

## 0. Authoritative sources verified for this plan

All schema decisions in this plan trace to documents fetched while drafting it. Re-verify before any future schema change.

| Source | Used for | Last verified |
|---|---|---|
| Microsoft Learn - [Advanced settings configuration in WSL](https://learn.microsoft.com/en-us/windows/wsl/wsl-config) (`ms.date: 2025-07-31`, `updated_at: 2025-12-09`) | First-class `/etc/wsl.conf` schema, defaults, restart language, `.wslconfig` handoff guidance | 2026-05-02 |
| Microsoft WSL [Release 2.7.1](https://github.com/microsoft/WSL/releases/tag/2.7.1) | Confirms `WindowsTerminal.enabled` belongs to `wsl-distribution.conf`, NOT `/etc/wsl.conf` | 2026-05-02 |
| Microsoft Learn - [Build a Custom Linux Distro for WSL](https://learn.microsoft.com/en-us/windows/wsl/build-custom-distro) | Confirms `wsl-distribution.conf` is a separate file owned by the distro packager, out of scope for EXTRA1 | 2026-05-02 |
| Ubuntu - [WSL instance configuration](https://documentation.ubuntu.com/wsl/latest/reference/instance_configuration/) | Cross-reference scope and editing guidance only; does not document additional first-class keys | 2026-05-02 |
| In-repo `SETTINGS_FEATURE.md` | Information architecture decision: per-distro config lives on the distro detail page, not in the global Settings destination | n/a (in-repo) |
| In-repo `CoolWSL.Configuration/Services/WslGlobalConfigService.cs` | Reference implementation pattern (load, validate, backup, save) to mirror for the per-distro service | n/a (in-repo) |

### 0.1 Corrections to `EXTRA_FEATURES.md` discovered during verification

These corrections must be reflected in the schema and UI copy delivered by this plan. They do not require an `EXTRA_FEATURES.md` rewrite, since `EXTRA_FEATURES.md` is the brainstorm document; the canonical schema lives in code.

1. **`[boot]` section is gated**. Microsoft Learn states *"The Boot setting is only available on Windows 11 and Server 2022."* The structured editor must hide or disable `[boot]` controls when running on Windows 10 or older Server SKUs. `EXTRA_FEATURES.md` did not flag this.
2. **`automount.metadata` default is `disabled`**, not `false`. It is a flag whose absence means disabled; presence with no value enables it. Treat as a tri-state in storage (absent / present-as-flag) but as a checkbox in the UI.
3. **8-second rule**. Microsoft Learn explicitly says *"This typically takes about 8 seconds after closing ALL instances of the distribution shell."* Restart guidance copy must mention this so users do not assume changes are immediate.
4. **`boot.systemd`** is documented in prose, not in the `[boot]` table. It exists. Floor: WSL `0.67.6+`. Verify with `wsl --version` reporting at refresh time.
5. **`wsl-distribution.conf` is a separate file** (created by the distro packager, not the user). EXTRA_FEATURES mentions `windowsterminal.enabled` in the "observed but not first-class" list under wsl.conf - that is wrong. It belongs to `wsl-distribution.conf` and is out of scope here.
6. **Schema id timestamp**. `EXTRA_FEATURES.md` shows `wsl-conf-per-distro-2026-04-15`. Use today's verification date instead: `wsl-conf-per-distro-2026-05-02`.
7. **`autoMemoryReclaim` is a string enum**, not a boolean. It is in `.wslconfig` not `wsl.conf`, so it does not affect EXTRA1 directly; it does mean the global service's existing handling (already correct, no type-validation rule, key is in `KnownKeys` only) is the pattern to copy.

---

## 1. Scope and explicit non-goals

### 1.1 In scope

- Per-distro `/etc/wsl.conf` reading via `wsl.exe -d <distro> --exec`.
- Per-distro `/etc/wsl.conf` writing via `wsl.exe -d <distro> -u root --exec` against a temp file in distro home.
- Lossless INI document model that preserves comments, ordering, whitespace, and unknown sections / keys.
- Schema-driven structured editor for the seven officially documented `/etc/wsl.conf` sections.
- Raw editor backed by the same in-memory document.
- Static, capability, and runtime validation as four explicit layers (see [Section 7](#7-validation-model)).
- Backup before save, kept in `%LocalAppData%\CoolWSL\Backups\WslDistroConfig\<Distro>\`.
- Restart-required messaging that distinguishes `wsl --terminate <distro>` from `wsl --shutdown` and surfaces the 8-second rule.
- Removal of the **Terminal** pivot from `DistroPage`.
- Rename of the **Configuration** pivot to **Settings**.
- Global `.wslconfig` handoff card inside the per-distro Settings pivot: read-only summary plus "Open WSL Settings" button.
- Test coverage for parser, serializer, validator, capability gating, restart inference, save flow, and backup behavior.

### 1.2 Out of scope (deferred or never)

| Out of scope | Reason | Where it goes instead |
|---|---|---|
| `.wslconfig` structured editor | Already covered by the official WSL Settings app and a future Phase 11 in this repo. EXTRA1 only adds a read-only summary and handoff button on the per-distro page. | Phase 11 of `IMPLEMENTATION_PLAN.md` (unchanged) |
| `wsl-distribution.conf` editing | Owned by the distro packager, not the user. Editing it is unsupported. | Excluded permanently |
| Auto-running `wsl --terminate` after save | Side effects must be explicit (`SETTINGS_FEATURE.md` anti-pattern 4). | The save-result UI offers an explicit follow-up button only. |
| Auto-running `wsl --shutdown` after save | Same reason; also affects every other distro. | Same. |
| Editing `/etc/wsl.conf` on Docker Desktop or other system-managed distros | Per `REQUIREMENTS.md` line 596: *"Treat identified Docker Desktop distros as system-managed and protect them from destructive flows by default."* | Show a capability-disabled state with explanation. |
| Editing `/etc/wsl.conf` on WSL 1 distros | Allowed by spec but not WSL 2-only keys. WSL 1 distros are visible (REQUIREMENTS line 184); only documented shared keys (`automount`, `network`, `interop`, `user`) remain editable. `[boot]`, `[gpu]` hidden with explanation. | Capability gating in the structured editor. |
| Inline command runner UI | The user has decided the Terminal pivot is useless. Verification commands (`id <user>`, `test -d /run/systemd/system`) run silently inside the new validation runner. | Removed. See [Section 4](#4-removing-the-terminal-pivot). |
| Networking mode / DNS tunneling controls (`.wslconfig` global) | Belongs to WSL Settings app handoff. EXTRA1 only summarizes. | Phase 11 |
| Boot profiler, port map, doctor report, etc. (other EXTRA features) | Separate plans. | EXTRA2-EXTRA10 |

### 1.3 Naming collision with the global `Settings` destination

The shell already has a top-level **Settings** destination in the `NavigationView` footer (`CoolWSL.App/Views/ShellPage.xaml:38-42`). Renaming the per-distro pivot to **Settings** introduces two destinations with the same word in the chrome at the same time.

Decision: rename anyway. Disambiguation comes from context, not from a different word:

- The global **Settings** destination is in the navigation footer, captioned "Settings", and shows the page title "Settings" with the subtitle *"Application preferences, WSL defaults, diagnostics, logs, and product information."*
- The per-distro pivot lives under the distro's own header (which already shows the distro name, state pill, WSL version, default and management labels) and the pivot tab reads "Settings".
- The per-distro Settings pivot's first card is a header reading **"`<DistroName>` settings - `/etc/wsl.conf`"** with the file path in monospace. This makes the scope visible at a glance.

This matches `SETTINGS_FEATURE.md` Section *"Recommended Information Architecture"*: the global Settings destination owns app prefs and `.wslconfig`; the per-distro page owns `/etc/wsl.conf`.

---

## 2. Information architecture before / after

### 2.1 Current

```
NavigationView
├── Dashboard
├── (Distros header)
│   ├── Ubuntu
│   ├── Debian
│   └── ...
├── (Footer)
│   ├── Logs
│   └── Settings              # global, owns .wslconfig
└── DistroPage Pivot
    ├── Overview
    ├── Terminal              # to be removed
    ├── Configuration         # to be renamed -> Settings + filled in
    └── Diagnostics
```

### 2.2 After EXTRA1 ships

```
NavigationView
├── Dashboard
├── (Distros header)
│   └── ...
├── (Footer)
│   ├── Logs
│   └── Settings              # unchanged: global .wslconfig + app prefs
└── DistroPage Pivot
    ├── Overview              # gains the "Open terminal" lifecycle card
    ├── Settings              # renamed; full /etc/wsl.conf experience
    └── Diagnostics
```

The "Open terminal" card already exists in the Overview pivot (`DistroPage.xaml:138-169`) and remains the entry point to a real Windows Terminal session. No replacement for the in-app command runner is needed; verification probes run silently from the new validation layer.

---

## 3. Phasing - delivery order

The work is broken into eight phases. Each phase is independently shippable and reviewable. The first three are pure refactor / IA work and unblock the rest.

| Phase | Title | Depends on | Approx. cost |
|---|---|---|---|
| **E1.0** | Pivot rename + Terminal removal + scaffolding | none | S |
| **E1.1** | Distro filesystem service + lossless INI document | E1.0 | M |
| **E1.2** | Per-distro config service (read, validate, save, backup) | E1.1 | M |
| **E1.3** | Static + capability validation | E1.2 | S |
| **E1.4** | Settings pivot UI - raw editor + restart messaging | E1.2 | M |
| **E1.5** | Settings pivot UI - structured editor over the same model | E1.4 | L |
| **E1.6** | Runtime validation probes + global `.wslconfig` summary card | E1.3, E1.5 | M |
| **E1.7** | Polish, accessibility audit, smoke verification | all above | S |

The phases map onto the existing `Phase 9` (raw per-distro editor) and `Phase 12` (structured per-distro editor) in `IMPLEMENTATION_PLAN.md`. EXTRA1 effectively brings Phase 12 forward and merges it with Phase 9 because the lossless document model is the right foundation for both, and writing it twice is wasteful.

---

## 4. Removing the Terminal pivot

### 4.1 What gets removed

- `DistroPage.xaml:262-449` - the entire `<PivotItem Header="Terminal">` block including the input card, stdout card, stderr card, and the session-history `Expander`.
- `DistroPage.xaml.cs:76-109` - the click handlers `OnRunCommandClick`, `OnCancelCommandClick`, `OnCopyStdoutClick`, `OnCopyStderrClick`, `OnClearOutputClick`, `OnReuseHistoryEntryClick` and the `CommandInputBox` reference.

### 4.2 What gets kept (for now)

- `CoolWSL.App/ViewModels/CommandRunnerViewModel.cs` - kept as a service-layer dependency. The new validation runner ([Section 7.4](#74-runtime-validation-probes)) uses `IWslDistroService.RunInDistroAsync` directly, not `CommandRunnerViewModel`. The view model is no longer wired to any UI.
- The `CommandRunner` property on `DistroViewModel` becomes unused. Per `CLAUDE.md` rule "Avoid backwards-compatibility hacks ... If you are certain that something is unused, you can delete it completely", remove the property and the constructor parameter once tests confirm nothing else references it. The DI singleton in `AppServiceCollection.cs:24` goes too.
- The `ReuseHistoryEntry` and `History` features - delete with the view model.

### 4.3 What this breaks

- **`REQUIREMENTS.md` MVP contract** lines 208-218 currently mandate *"A pivot with Overview, Terminal, Configuration, and Diagnostics"* and *"The command runner must be reachable directly from the Terminal pivot..."* These must be updated. See [Section 14](#14-document-updates-required).
- **`DONE.md`** lines 98-99, 103 record the Terminal pivot work as completed. Per `GEMINI.md`: *"`DONE.md` must contain only verified and complete work."* The historical entries stay (they were true at the time) but new entries in the EXTRA1 phase block must explicitly note the removal so the audit trail is honest.
- **`ARCHITECTURE.md` ADR 0002** line 57 mentions *"Overview, Terminal, Configuration, and Diagnostics."* That ADR was a baseline decision; its language must be amended (or a new ADR 0003 issued superseding the pivot list). I recommend a small in-place edit to ADR 0002 with a dated note rather than a new ADR, because the underlying shell architecture decision is unchanged - only the pivot composition is.

### 4.4 What this gains

- One fewer surface to maintain.
- The Overview pivot's "Open terminal" card becomes the only sanctioned terminal entry point. That card already opens a real Windows Terminal session via `IWslDistroService.OpenDistroAsync`, which is more useful than an in-app textbox-based runner.
- Verification commands run from the validation layer without leaking command-runner state into the UI.

---

## 5. Renaming the Configuration pivot

### 5.1 Mechanics

- `DistroPage.xaml:451` - change `Header="Configuration"` to `Header="Settings"`.
- `DistroPage.xaml:454-462` - replace the placeholder card body with the new per-distro Settings layout from [Section 9](#9-ui-layout).
- Search the codebase for the literal string `"Configuration"` in user-facing copy. `Grep` confirms two file matches besides `DistroPage.xaml`: `DONE.md:100` and `REQUIREMENTS.md` (multiple). Source files referring to the *.NET* `Configuration` namespace and project (`CoolWSL.Configuration.*`) are NOT renamed; that namespace owns the *file* model and remains correctly named relative to its purpose.

### 5.2 Why not a different word

- "Config" - too jargon-y for a Windows 11 native shell.
- "Setup" - implies first-run.
- "Tuning" - implies performance only.
- "Settings" matches the system vocabulary users already understand from Windows Settings, WSL Settings, and the existing global Settings destination.

### 5.3 Required disambiguation

Inside the renamed pivot, the first row reads:

```
Settings - <DistroName>
/etc/wsl.conf
This page edits per-distro settings. To change global WSL settings (memory, CPU, networking mode, kernel), use the WSL Settings app.
                                          [ Open WSL Settings ]
```

The "Open WSL Settings" button is implemented in [Section 9.4](#94-global-handoff-card).

---

## 6. Document model and parser

### 6.1 Why a custom INI model

- WSL files are INI-shaped but neither `System.Configuration.ConfigurationManager` nor any Microsoft.Extensions.Configuration provider preserves comments, blank lines, ordering, or unknown keys.
- `SETTINGS_FEATURE.md` Section *"Source Of Truth Model"* requires structured and raw modes to share one in-memory document.
- The existing `WslGlobalConfigService` validates raw text but does NOT model it. EXTRA1 introduces the document model and the per-distro service uses it. The global service is left alone for now (Phase 11 will refactor it onto the same model).

### 6.2 Model shape

New file: `CoolWSL.Core/Models/Configuration/IniDocument.cs` (and helpers in the same folder).

```csharp
namespace CoolWSL.Core.Models.Configuration;

public sealed class IniDocument
{
    public IReadOnlyList<IniNode> Nodes { get; }       // ordered top-to-bottom
    public IReadOnlyList<IniSection> Sections { get; } // sections only, in order

    public IniSection? Section(string name);
    public IniDocument WithSection(IniSection section);
    public string Serialize();                          // round-trips byte-for-byte where possible
}

public abstract class IniNode { public int LineNumber { get; } }

public sealed class IniBlankLine : IniNode { }

public sealed class IniComment : IniNode { public string Raw { get; } }    // includes leading # or ;

public sealed class IniSection : IniNode
{
    public string Name { get; }                         // canonical, lower-cased for lookup
    public string RawHeader { get; }                    // e.g. "[ boot ]" preserved as written
    public IReadOnlyList<IniNode> Body { get; }         // entries, comments, blanks in order
    public IniEntry? Entry(string key);
    public IniSection WithEntry(IniEntry entry);
    public IniSection WithoutEntry(string key);
}

public sealed class IniEntry : IniNode
{
    public string Key { get; }                          // canonical
    public string RawKey { get; }                       // exact spelling
    public string Value { get; }                        // unquoted, trimmed
    public string RawLine { get; }                      // exact text (used when caller does not edit)
    public bool IsKnown { get; init; }
}

public sealed class IniMalformedLine : IniNode { public string Raw { get; } public string Reason { get; } }
```

### 6.3 Parser rules

Implemented in `CoolWSL.Core/Models/Configuration/IniParser.cs`:

1. Lines are read with the file's existing line endings preserved on the document; serialization emits the same line ending. If the file has mixed line endings, normalize to `\n` (Linux convention; the file lives inside the distro) and emit a single warning.
2. Leading and trailing whitespace on a line is preserved as-written for output. For semantic comparison, whitespace is trimmed.
3. A line whose first non-whitespace character is `#` or `;` is a comment. `IniComment.Raw` keeps the entire original text.
4. A line of the form `[<name>]` (with optional inner whitespace) is a section header. `Name` is normalized (`Trim()` then `ToLowerInvariant()` for lookup); `RawHeader` keeps the original.
5. A line of the form `key = value` is an entry. Splitting is on the *first* `=`. `Key` is normalized to lower-case for lookup; `RawKey` and `Value` keep the original text. The value loses one pair of surrounding double quotes if both ends match.
6. Anything else inside a section becomes an `IniMalformedLine` with a `Reason` such as "expected `key=value`". Outside a section, the same applies. Malformed lines are preserved verbatim on serialization.
7. Duplicate section headers are kept as-written. Lookup returns the *last* section with that name (matches WSL behavior per the docs: *"WSL may use the later value"*).
8. Duplicate keys inside a section are kept as-written. `IniSection.Entry(key)` returns the last one. Validation emits a warning on duplicates.

### 6.4 Serializer rules

`IniDocument.Serialize()`:

1. Walk `Nodes` in order. For each node:
   - `IniBlankLine` -> emit one empty line.
   - `IniComment` -> emit `Raw`.
   - `IniMalformedLine` -> emit `Raw`.
   - `IniSection` -> emit `RawHeader`, then walk `Body`.
   - `IniEntry` -> if untouched, emit `RawLine`. If `WithEntry` was called or the entry was constructed fresh, emit `$"{RawKey}={Value}"` with quoting only when the value contains a comma followed by whitespace ambiguity, since DrvFs values can contain commas.
2. New sections appended via `WithSection` are appended at the end with a leading blank line, header `[name]`, and one blank trailing line if any nodes follow.
3. New entries appended via `WithEntry` go at the end of the section's body, separated from the previous entry by zero blank lines if the section already ends with content, or one blank line if the section ends with a comment.
4. Round-trip test: parsing then serializing the unchanged document must produce the byte-identical input (modulo final trailing newline normalization to a single `\n`). This is enforced by a property test (see [Section 12](#12-tests)).

### 6.5 What the parser is NOT

- It is not a full INI library. Inline comments after `key=value` on the same line are not supported (Microsoft's WSL parser does not document them and the docs' example files do not use them). They are treated as part of the value, which matches what WSL does.
- It does not coerce types. Type coercion happens in the validator and in the structured editor binding layer.

---

## 7. Validation model

Validation runs in four explicit, ordered layers. This is `SETTINGS_FEATURE.md` Section *"Validation Model"* applied to per-distro config.

### 7.1 Syntax validation (parser)

Already covered by the parser (Section 6). Errors here are blocking.

### 7.2 Type validation (schema)

Implemented in `CoolWSL.Core/Models/Configuration/WslDistroConfigSchema.cs`. Each known key has a typed `WslConfigKey` record:

```csharp
public sealed record WslConfigKey(
    string Section,
    string Key,
    WslConfigValueType ValueType,
    object? Default,
    WslConfigRestartImpact RestartImpact,
    WslConfigCapabilityRequirement Capability,
    string Description,
    string? VerifyCommand,
    bool IsAdvanced);

public enum WslConfigValueType { Boolean, Integer, OctalMask, LinuxPath, Hostname, LinuxUsername, FreeText, DrvFsOptions, Enum }
```

Type validation rules:

- `Boolean` accepts `true`, `false` only (case-insensitive). Other values are blocking errors. Microsoft's WSL parser is liberal with truthy values, but EXTRA1 is strict so users do not silently get the wrong setting.
- `Integer` accepts a non-negative decimal integer.
- `OctalMask` accepts 3 or 4 octal digits.
- `LinuxPath` requires a leading `/`.
- `Hostname` accepts the RFC 952 / 1123 character set, max 63 characters.
- `LinuxUsername` accepts the POSIX `[a-z_][a-z0-9_-]*[$]?` pattern, max 32 characters.
- `DrvFsOptions` parses comma-separated tokens; known tokens validate their typed sub-value (see Section 8.4); unknown tokens are preserved with a warning.
- `Enum` accepts one of `WslConfigKey.AllowedValues` (a separate optional list on the record). Used for `automount.case` (`off`, `dir`, `force`).
- `FreeText` accepts anything.

### 7.3 Capability validation

The structured editor must hide or disable controls that the host cannot honor. `WslConfigCapabilityRequirement` is a flags enum:

```csharp
[Flags]
public enum WslConfigCapabilityRequirement
{
    None             = 0,
    Wsl2Required     = 1 << 0,
    Windows11Plus    = 1 << 1,   // covers [boot], several .wslconfig keys
    Systemd067_6Plus = 1 << 2,   // boot.systemd
    NotSystemManaged = 1 << 3,   // disables editing on Docker Desktop distros
}
```

Capability inputs:

- WSL version comes from `WslEnvironmentStatusBuilder` (already populated, see `CoolWSL.Wsl/Builders/WslEnvironmentStatusBuilder.cs`).
- Windows build comes from `Environment.OSVersion.Build` and is also already accessible via `Get-CimInstance Win32_OperatingSystem` if needed (the existing pattern in EXTRA_FEATURES section "Implementation notes").
- WSL 1 vs WSL 2 comes from `WslDistro.WslVersion`.
- System-managed flag comes from `WslDistro.IsSystemManaged` (already populated for Docker Desktop).

When a capability requirement is unmet, the editor:

- Hides the section entirely if every key in the section requires the missing capability (e.g. WSL 1 distro hides `[boot]`, `[gpu]` per Microsoft's docs that say `[boot]` is Windows 11 only).
- Otherwise disables the individual key control with a one-line tooltip explaining why.

The raw editor remains available regardless. Saving is allowed even if the file declares capability-missing keys, with a warning, because the user may be authoring a config to be moved to another machine.

### 7.4 Runtime validation probes

After a save, or on demand from a "Verify" button at the top of the Settings pivot, the validation runner executes a small set of read-only probes inside the distro and surfaces the results next to each key. Probes use `IWslDistroService.RunInDistroAsync` with a 10-second per-probe timeout.

Probes (all keyed by setting):

| Setting | Probe command | Pass criteria |
|---|---|---|
| `boot.systemd` | `test -d /run/systemd/system && readlink /proc/1/exe` | Directory exists AND PID 1 is `systemd` |
| `user.default` | `id <username>` (after argument-quoting; `<username>` taken from the parsed entry) | Exit code 0 |
| `network.generateResolvConf` | `ls -l /etc/resolv.conf` | File exists; symlink-vs-file presence reported |
| `network.generateHosts` | `test -f /etc/hosts && head -n 1 /etc/hosts` | File exists |
| `automount.enabled` | `findmnt -t drvfs -no SOURCE` | Output non-empty when enabled |
| `automount.root` | `mount \| grep " on $(escape)"` | Mount point matches configured root |
| `interop.enabled` | `command -v powershell.exe >/dev/null 2>&1; echo $?` | Exit `0` means interop works |
| `interop.appendWindowsPath` | `printf '%s\n' "$PATH" \| tr ':' '\n' \| grep -i '/mnt/c/' \|\| true` | Match present means appended |
| `gpu.enabled` | `test -e /dev/dxg && ls /usr/lib/wsl/lib 2>/dev/null` | Both succeed |
| `time.useWindowsTimezone` | `readlink -f /etc/localtime` | Reports the current zoneinfo file |

Probes use `wsl.exe -d <distro> --exec /bin/sh -lc '<probe>'`, which mirrors `WslCommandFactory.CreateRunInDistroCommand`. They never run as root unless the probe explicitly needs it (only `id` probes for the configured `user.default` need to read `/etc/passwd`, which is world-readable, so root is not required).

Probes never modify state. They are clearly labeled "Verifies what is currently effective. Restart the distro to apply pending changes."

### 7.5 Consequence warnings

These are surfaced before save (in a banner above the Save button) and after save (in the result panel):

| Edit | Warning |
|---|---|
| Any `[boot]` change | "Requires distro restart - run `wsl --terminate <distro>`. About 8 seconds." |
| `boot.command` set or changed | "Runs as **root** at startup. Verify the command before saving." |
| `boot.systemd` toggled | "Restarting the distro is required. systemd may take additional time to reach a steady state." |
| `network.*` change | "Affects DNS or hostname. Other distros are not affected." |
| `automount.*` change | "Affects how Windows drives appear inside this distro." |
| `interop.*` change | "Restart the distro shell or run `wsl --terminate <distro>`." |
| `user.default` change | "Affects the next WSL session. Existing sessions are not changed." |
| Any change introducing a key with `WslConfigCapabilityRequirement` unmet | "This setting requires `<missing capability>`. The file will save, but the setting will be ignored." |

Severity: `Boot` and `boot.command` are `Warning`. `Capability missing` is `Info`. None are blocking by themselves.

---

## 8. Schema definition

The schema is data, not code distributed across the UI. It lives in one file and is the single source of truth for both the structured editor and validation.

### 8.1 File location

`CoolWSL.Core/Models/Configuration/WslDistroConfigSchema.cs`. Static `WslDistroConfigSchema.Current` returns the immutable schema instance.

### 8.2 Sections (order = display order)

```text
[user]        -> "User" (Basics group)
[boot]        -> "Boot and services"   (gated: Windows 11+)
[automount]   -> "Windows drive automount"
[network]     -> "Per-distro network files"
[interop]     -> "Windows interoperability"
[gpu]         -> "GPU access"          (gated: WSL 2)
[time]        -> "Time and timezone"
```

### 8.3 First-class keys

This list is the canonical schema. It is identical in shape to the table in `EXTRA_FEATURES.md` Section *"Schema table"* with the corrections from [Section 0.1](#01-corrections-to-extra_featuresmd-discovered-during-verification) folded in.

| Section | Key | Type | Default | Capability | Restart | Verify probe |
|---|---|---|---|---|---|---|
| `boot` | `systemd` | Boolean | (not set) | `Windows11Plus \| Systemd067_6Plus \| Wsl2Required` | terminate distro | `test -d /run/systemd/system && readlink /proc/1/exe` |
| `boot` | `command` | FreeText | (not set) | `Windows11Plus` | terminate distro | none |
| `boot` | `protectBinfmt` | Boolean | `true` | `Windows11Plus` | terminate distro | none |
| `automount` | `enabled` | Boolean | `true` | `None` | terminate distro | `findmnt -t drvfs -no SOURCE` |
| `automount` | `mountFsTab` | Boolean | `true` | `None` | terminate distro | none |
| `automount` | `root` | LinuxPath | `/mnt/` | `None` | terminate distro | path-mount probe |
| `automount` | `options` | DrvFsOptions | (not set; default DrvFs options apply) | `None` | terminate distro | none |
| `network` | `generateHosts` | Boolean | `true` | `None` | terminate distro | `test -f /etc/hosts` |
| `network` | `generateResolvConf` | Boolean | `true` | `None` | terminate distro | `ls -l /etc/resolv.conf` |
| `network` | `hostname` | Hostname | Windows hostname | `None` | terminate distro | `hostname` |
| `interop` | `enabled` | Boolean | `true` | `None` | new shell | `command -v powershell.exe` |
| `interop` | `appendWindowsPath` | Boolean | `true` | `None` | new shell | inspect `$PATH` |
| `user` | `default` | LinuxUsername | initial install user | `None` | new WSL session | `id <username>` |
| `gpu` | `enabled` | Boolean | `true` | `Wsl2Required` | terminate distro | `test -e /dev/dxg` |
| `time` | `useWindowsTimezone` | Boolean | `true` | `None` | terminate distro | `readlink -f /etc/localtime` |

### 8.4 DrvFs options sub-schema (`automount.options`)

Stored as `WslConfigDrvFsOption` records inside the schema entry for `automount.options`:

| Token | Type | Default | Notes |
|---|---|---|---|
| `metadata` | Flag | absent | Presence enables. UI = checkbox; absent state means "use WSL default (disabled)" |
| `uid` | Integer | distro user UID (typically `1000`) | Range `0..4294967295` |
| `gid` | Integer | distro user GID (typically `1000`) | Same range |
| `umask` | OctalMask | `022` | 3-4 octal digits |
| `fmask` | OctalMask | `000` | Same |
| `dmask` | OctalMask | `000` | Same |
| `case` | Enum | `off` | `off`, `dir`, `force` |

Editor offers structured and raw modes for this single value. Unknown tokens are preserved on save and listed under "Advanced - unknown DrvFs option" with their original value.

### 8.5 Observed but not first-class

These keys appear in safe-mode log output and community references. They are **preserved** if found, **not** offered as structured controls, and not added by template generation.

| Key | Why preserved | Why not first-class |
|---|---|---|
| `automount.cgroups` | Sometimes seen in safe-mode logs | Not in Microsoft Learn `[automount]` table; cgroupsv2 is now standard since WSL 2.5.1, making this mostly historical |
| `automount.ldconfig` | Sometimes seen in safe-mode logs | Not documented; mechanism may change |
| `gpu.appendLibPath` | Seen in WSLg-related references | Not in Microsoft Learn `[gpu]` table |
| `fileServer.enabled` | Seen in safe-mode logs | Not a per-distro user-facing setting |

Anything else unknown is preserved verbatim and shown under the **Advanced - unknown keys** expander with the message: *"Not documented in the Microsoft `wsl.conf` reference. Preserved as-is."*

### 8.6 What is intentionally NOT in the schema

- `windowsterminal.enabled` - this is a `wsl-distribution.conf` key, a different file owned by the distro packager. If a user pastes it into `/etc/wsl.conf`, EXTRA1 surfaces it in the "Unknown keys" expander with a special note: *"This key belongs in `/etc/wsl-distribution.conf`, not `/etc/wsl.conf`. Preserved as-is."*
- Any `.wslconfig` (`[wsl2]`, `[experimental]`) keys - they belong to the global config service and the WSL Settings handoff card.

---

## 9. UI layout

### 9.1 Files touched

| File | Change |
|---|---|
| `CoolWSL.App/Views/DistroPage.xaml` | Remove Terminal pivot, rename Configuration -> Settings, add Settings layout |
| `CoolWSL.App/Views/DistroPage.xaml.cs` | Remove command-runner handlers, add Settings handlers |
| `CoolWSL.App/ViewModels/DistroViewModel.cs` | Remove `CommandRunner` property, add `Settings` property of type `DistroSettingsViewModel` |
| `CoolWSL.App/ViewModels/DistroSettingsViewModel.cs` | NEW |
| `CoolWSL.App/ViewModels/DistroSettingsRowViewModel.cs` | NEW |
| `CoolWSL.App/Views/Controls/WslConfigKeyCard.xaml(.cs)` | NEW reusable per-key card |
| `CoolWSL.App/DependencyInjection/AppServiceCollection.cs` | Register `DistroSettingsViewModel`, deregister `CommandRunnerViewModel` |

### 9.2 Settings pivot layout

```
Settings pivot
├── Header card
│   ├── Title:    "Settings - <DistroName>"
│   ├── Subtitle: "/etc/wsl.conf"
│   ├── State:    "Loaded 2026-05-02 14:32:11" or "File not found - distro is using defaults"
│   └── Actions:  [Reload]  [Verify]  [Open WSL Settings] (handoff)
├── Mode switch:  ( Structured | Raw )
├── If Structured:
│   ├── Group: "Basics"
│   │   ├── user.default        (text + user-picker dropdown when distro is running)
│   │   ├── network.hostname    (text)
│   │   └── boot.systemd        (toggle, gated)
│   ├── Group: "Windows integration"
│   │   ├── interop.enabled
│   │   ├── interop.appendWindowsPath
│   │   ├── automount.enabled
│   │   ├── automount.root
│   │   └── automount.options   (sub-card with structured DrvFs editor + raw fallback)
│   ├── Group: "Network files"
│   │   ├── network.generateHosts
│   │   └── network.generateResolvConf
│   ├── Group: "Boot and services"     (hidden when not Windows 11)
│   │   ├── boot.command
│   │   └── boot.protectBinfmt
│   ├── Group: "GPU and time"
│   │   ├── gpu.enabled        (hidden when WSL 1)
│   │   └── time.useWindowsTimezone
│   └── Group: "Advanced - unknown keys" (collapsed by default; only shown when present)
│       └── List of preserved unknown sections / keys
├── If Raw:
│   └── Single TextBox, monospace, with line-numbered gutter (later phase)
├── Validation panel
│   └── Three columns: Errors, Warnings, Info
├── Restart panel  (visible when document differs from on-disk and at least one changed key has a non-trivial restart impact)
│   ├── Plain-language summary (e.g. "Restarting Ubuntu is needed. About 8 seconds.")
│   └── Buttons: [Save and terminate distro]  [Save only]
└── Save bar
    ├── Status text       (e.g. "Saved 2026-05-02 14:35:02. Backup at C:\...\Ubuntu\wsl.conf.20260502T143502Z.bak")
    └── Buttons: [Revert]  [Save]
```

### 9.3 Per-key card

Each key gets a `WslConfigKeyCard` with:

- Label and key id (`automount.options`) in monospace.
- Control (toggle / textbox / combo / numberbox) bound to the document model via `DistroSettingsRowViewModel`.
- Value source pill: `Default`, `Modified`, `Unset`.
- Capability tag: `Requires Windows 11`, `Requires WSL 2`, `Requires WSL 0.67.6+`.
- Restart tag: `Requires distro restart` or `Requires new shell`.
- Description text (one line from the schema).
- "Reset to default" link button. Clicking it removes the entry from the document and the file (after save) reverts to default.
- Verify result chip when probe data is available: `✓ Effective`, `✗ Not effective`, `… Unknown` with timestamp.

### 9.4 Global handoff card

A single small card at the very top of the Settings pivot, above the header:

```
+-------------------------------------------------------------+
| Global WSL settings                                         |
| Memory: 50% of host  |  Networking: NAT  |  GUI apps: on    |
| Custom kernel: not configured                               |
| Settings applied at the WSL VM level affect every distro.   |
|                                              [ Open WSL Settings ] |
+-------------------------------------------------------------+
```

Implementation:

- Read `%UserProfile%\.wslconfig` via the existing `IWslGlobalConfigService.ReadAsync` (already implemented).
- Parse the result with the new `IniParser` (Section 6) to extract the chip values.
- "Open WSL Settings" button:
  1. Try `Start "wslsettings:"` URI activation.
  2. Fallback: `Process.Start(new ProcessStartInfo { FileName = "wslsettings.exe", UseShellExecute = true })`.
  3. Fallback: open the Start menu search via `Process.Start("start", "ms-settings:") ` (no perfect URI for the WSL Settings store app exists across all builds, so the third fallback is to open the WSL Settings docs URL).
  4. If all fail, surface a Teaching Tip linking to the Microsoft Store WSL Settings page.

### 9.5 Capability messages reused

Where a key is hidden or disabled, reuse `DistroCapabilityHelper` (already in `CoolWSL.Core/Helpers/DistroCapabilityHelper.cs`) for the explanation. Do not invent new messages.

---

## 10. Per-distro filesystem service

### 10.1 New abstraction

`CoolWSL.Core/Abstractions/IWslDistroFileService.cs`:

```csharp
public interface IWslDistroFileService
{
    Task<DistroFileReadResult> ReadTextAsync(
        string distroName,
        string linuxPath,
        bool readAsRoot = false,
        CancellationToken cancellationToken = default);

    Task<DistroFileWriteResult> WriteTextAsync(
        string distroName,
        string linuxPath,
        string contents,
        bool writeAsRoot = true,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string distroName,
        string linuxPath,
        CancellationToken cancellationToken = default);
}
```

### 10.2 Read implementation

Use `wsl.exe -d <distro> --exec cat -- <path>`. Capture stdout. Stderr non-empty + non-zero exit code -> map to `WslCommandError`. Non-zero exit code with empty stderr usually means "file not found": treat as `FileNotFound` and return an empty document (consistent with the global config service's behavior).

Important: do **not** force UTF-16 encoding here. `GEMINI.md` Repository Notes line 70 says: *"Redirected host-side `wsl.exe` metadata commands ... emit UTF-16LE on this machine; keep explicit Unicode stream encoding on those commands, but do not force that encoding onto in-distro `--exec` commands."* Use UTF-8.

### 10.3 Write implementation

WSL config files are owned by `root`. Writing requires elevation inside the distro, not on Windows.

Steps (executed via one composed `--exec` command):

1. Generate a random temp filename, e.g. `/tmp/coolwsl-wsl-conf.<guid>.tmp`.
2. Pipe the new contents through stdin to `tee` running as root:
   ```
   wsl.exe -d <distro> -u root --exec /bin/sh -lc 'umask 022 && cat > /tmp/coolwsl-<id>.tmp && install -m 0644 -o root -g root /tmp/coolwsl-<id>.tmp /etc/wsl.conf && rm -f /tmp/coolwsl-<id>.tmp'
   ```
3. The Windows-side host writes `contents` to the process's redirected `StandardInput`, then closes it. This avoids quoting issues and never puts the contents on the command line.
4. Capture exit code. Non-zero = the save failed; surface stderr verbatim.

The existing `WslCommandService.ExecuteAsync` does not currently support stdin redirection. EXTRA1 adds a thin overload `ExecuteWithStdinAsync(WslCommand, string stdin, CancellationToken)` to `IWslCommandService` and implements it in `WslCommandService` with `RedirectStandardInput = true`. Existing callers are unaffected.

### 10.4 Why not use `\\wsl.localhost\<distro>\etc\wsl.conf` on the Windows side

It works for reading but writing through Plan 9 to a root-owned file is unreliable and depends on whether the user launched the distro elevated. The `--exec` path is uniform, scriptable, and matches `IMPLEMENTATION_PLAN.md` Phase 9 acceptance criteria.

### 10.5 Backups before save

Implemented in the new `WslDistroConfigService`, not in `WslDistroFileService`. The file service is intentionally generic.

Backup directory: `%LocalAppData%\CoolWSL\Backups\WslDistroConfig\<DistroName>\` (mirrors the existing `WslGlobalConfigService` pattern in `CoolWSL.Configuration/Services/WslGlobalConfigService.cs:347-351`).

Backup filename: `wsl.conf.<UTC ISO 8601 with hyphens>.bak`, e.g. `wsl.conf.20260502T143502Z.bak`.

The original file content is read once before write and saved to the backup directory. If the original file did not exist, no backup is written and the save-result panel says so explicitly: *"This is a new file. No backup was needed."*

---

## 11. Per-distro config service

### 11.1 New abstraction

`CoolWSL.Core/Abstractions/IWslDistroConfigService.cs`:

```csharp
public interface IWslDistroConfigService
{
    Task<WslDistroConfigDocument> ReadAsync(
        string distroName,
        CancellationToken cancellationToken = default);

    WslConfigValidationResult Validate(
        IniDocument document,
        WslDistroCapabilityContext capabilities);

    Task<WslDistroConfigSaveResult> SaveAsync(
        string distroName,
        IniDocument document,
        WslDistroCapabilityContext capabilities,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WslConfigProbeResult>> ProbeAsync(
        string distroName,
        IniDocument document,
        CancellationToken cancellationToken = default);
}
```

### 11.2 Models

New under `CoolWSL.Core/Models/`:

- `WslDistroConfigDocument` - wraps `IniDocument`, source `distroName`, on-disk content (verbatim), `Existed` bool, `LoadedAt` timestamp, schema validation snapshot.
- `WslDistroConfigSaveResult` - distro name, on-disk path inside distro (`/etc/wsl.conf`), backup path (Windows-side), saved-at timestamp, validation snapshot, restart suggestion enum.
- `WslDistroCapabilityContext` - captures Windows build, WSL version, distro WSL version, distro `IsSystemManaged`, distro existing user list (lazily populated from `getent passwd`).
- `WslConfigProbeResult` - probe id, key id, status (`Effective`, `NotEffective`, `Unknown`, `Skipped`), evidence (raw stdout snippet, ≤ 500 chars), command attempted, run-at timestamp.

`WslConfigValidationResult`, `WslConfigValidationIssue`, and `WslConfigValidationSeverity` already exist (`CoolWSL.Core/Models/WslConfigValidationIssue.cs` and friends). Reuse them as-is.

### 11.3 DI registration

`CoolWSL.Configuration/DependencyInjection/ServiceCollectionExtensions.cs` gains:

```csharp
services.AddSingleton<IWslDistroConfigService, WslDistroConfigService>();
services.AddSingleton<IWslDistroFileService, WslDistroFileService>();
```

`WslDistroFileService` lives in `CoolWSL.Wsl/Services/WslDistroFileService.cs` (it is a wsl-shell wrapper, not a config concern). `WslDistroConfigService` lives in `CoolWSL.Configuration/Services/WslDistroConfigService.cs` next to the global service. The DI extension methods of the two assemblies are unchanged in shape.

### 11.4 Save flow

```
1. Validate (syntax already done by parser; type + capability layers).
2. If validation has blocking errors: throw InvalidOperationException("...").
   The view model converts this into a user-visible "Cannot save" banner that lists every error.
3. Read current /etc/wsl.conf via IWslDistroFileService for backup snapshot.
   If it does not exist: backup snapshot is null.
4. If snapshot exists: write backup file under
   %LocalAppData%\CoolWSL\Backups\WslDistroConfig\<DistroName>\wsl.conf.<utc>.bak
5. Serialize the IniDocument to text.
6. Write via IWslDistroFileService.WriteTextAsync (writeAsRoot: true).
7. Construct WslDistroConfigSaveResult and return.
```

The view model is the only thing that decides what to do with the result (show restart panel, offer Verify, etc.). The service is pure.

### 11.5 Cancellation

All async paths flow `CancellationToken` end-to-end. Mid-write cancellation is best-effort (the `wsl.exe` process is killed); the service does not attempt to roll back a partial write because `install -m 0644 ... <dst>` is atomic on success and never overwrites on failure.

---

## 12. Tests

All tests live in `CoolWSL.Tests/Configuration/` and `CoolWSL.Tests/Core/`. The existing `WslGlobalConfigServiceTests.cs` is the style template (MSTest, `TestClass`, `TestMethod`, single-arrange-act-assert per method).

### 12.1 Parser tests (`IniParserTests.cs`)

- Empty file -> empty document, round-trip.
- Single section, single key -> 1 section, 1 entry, round-trip byte-identical.
- Comment with `#` and with `;` -> preserved verbatim.
- Mixed line endings -> normalized to `\n` with one warning.
- Duplicate sections -> both preserved; lookup returns last; warning issued.
- Malformed line in middle -> preserved as `IniMalformedLine`; round-trip preserves position.
- Section with body that contains comment, blank, entry, blank, entry -> body order preserved on serialize.
- Quoted DrvFs options value `"metadata,uid=1003,gid=1003,umask=077,fmask=11,case=off"` -> outer quotes stripped on parse, restored on emit only when the value contains a comma.

### 12.2 Schema tests (`WslDistroConfigSchemaTests.cs`)

- Every documented section is present.
- Every key has a non-null type, a description, a restart impact, a capability requirement.
- `automount.options` carries the DrvFs sub-schema.
- No key has `Capability == None & VerifyCommand != null` for a probe that requires root (defense in depth).

### 12.3 Validator tests (`WslDistroConfigValidatorTests.cs`)

- Valid minimal `[boot]\nsystemd=true` on Windows 11 + WSL 0.67.6+ -> 0 issues.
- Same on Windows 10 -> 1 capability info.
- `[boot]\nsystemd=maybe` -> 1 type error, blocking.
- `[automount]\noptions=metadata,uid=abc` -> 1 type error on `uid`.
- `[automount]\noptions=metadata,unknownThing` -> 1 warning (unknown DrvFs token), no error.
- `[fileServer]\nenabled=true` -> 1 warning (unknown section), preserved.
- Duplicate section -> warning.
- Duplicate key -> warning.

### 12.4 File service tests (`WslDistroFileServiceTests.cs`)

These cannot run inside a distro on a build machine reliably. Use a scriptable `IWslCommandService` test double that returns deterministic stdout / stderr / exit code based on the command shape. Verify:

- ReadAsync sends `cat <path>` and returns stdout.
- ReadAsync with a "file not found" stderr maps to `FileNotFound`.
- WriteAsync composes the `tee + install` pipeline, sends content via stdin.
- WriteAsync with non-zero exit returns failure with stderr.

### 12.5 Config service tests (`WslDistroConfigServiceTests.cs`)

- ReadAsync on a missing file returns an empty document with `Existed = false`.
- SaveAsync with blocking errors throws.
- SaveAsync with valid input writes a backup, then writes the file, then returns the backup path.
- SaveAsync on a brand-new file does not write a backup; result reports "no backup needed".
- ProbeAsync runs all expected probes for the keys present in the document and only those.

### 12.6 ViewModel tests (`DistroSettingsViewModelTests.cs`)

- Loading a missing file shows the "using defaults" empty state.
- Editing a structured control marks the document Modified.
- Switching to Raw mode and editing the text and switching back to Structured does not lose edits (single source of truth).
- Switching to Raw mode after a parse failure leaves Structured disabled with the failure reason visible; saving the raw text is still allowed.
- Save failure propagates the stderr into the save-result panel.

### 12.7 Smoke test additions

`CoolWSL.Tests/Smoke/` already exists. Add a manual checklist (not automated) for:

1. Open the app, navigate to a distro, click Settings pivot. Verify the file loads.
2. Toggle `boot.systemd`. Verify the restart panel appears.
3. Click Save. Verify the backup path is shown.
4. Open the backup file in Notepad. Verify it matches the pre-save content.
5. Click Verify. Verify probes run and update the chips.
6. Click "Open WSL Settings". Verify the WSL Settings app opens (or a fallback teaches the user where to find it).

---

## 13. Risks, mitigations, open questions

### 13.1 Risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Stdin-redirected `wsl.exe --exec` corrupts the file under odd encodings | Low | High | Tests cover round-trip with non-ASCII content; use UTF-8 explicitly; refuse to save if the contents contain ` ` |
| Write succeeds but the distro reads a stale config because it was running | Medium | Medium | Restart panel explains the 8-second rule; "Save and terminate distro" button offered explicitly |
| User edits the file inside the distro between Read and Save (lost-update) | Low | High | Before write, re-read the file and compare to the snapshot taken at Read time. If different, abort and surface a "File changed externally - reload?" prompt |
| `wsl --terminate` from inside the app fails for system-managed distros | Low | Low | Capability gating already disables editing for system-managed distros; the offered button checks again before running |
| Parser misclassifies an exotic INI dialect | Low | Medium | Parser is strict; unknown shapes become `IniMalformedLine` and are preserved verbatim; warnings show |
| Renaming the pivot to Settings confuses users with the global Settings destination | Medium | Low | Disambiguation per Section 5.3; user research after first release will confirm |
| Future WSL release adds a new `[boot]` key not in the schema | Certain over time | Low | "Advanced - unknown keys" expander preserves and surfaces it; schema can be updated in a small follow-up PR; no data is lost in the meantime |

### 13.2 Open questions

1. **Should EXTRA1 also include a per-distro user-picker driven by `getent passwd`?** This requires running the distro at least once. Recommendation: yes for E1.5 (running distros), no for stopped distros. Stopped-distro users get a free-text input with the "id `<user>`" probe deferred until next start.
2. **Should EXTRA1 expose a "Restore from backup" button?** Recommendation: yes, in E1.7. List the backup files under the distro's backup folder, sorted newest-first, with a confirmation that the current file will be overwritten (and itself backed up).
3. **Should the Raw editor have line numbers and INI syntax coloring?** Recommendation: line numbers yes (cheap, native `TextBox` does not have them so we use a `Grid` with a left gutter `ItemsRepeater`). Coloring no in EXTRA1; it costs more than it returns.
4. **Should we generate `[boot]\nsystemd=true` automatically when Save is clicked on a distro that does not have `[boot]` and where the user toggled the systemd switch?** Recommendation: yes, append a fresh section. The serializer already supports this (Section 6.4).

---

## 14. Document updates required

Done as part of Phase E1.0 so the rest of the work happens against an honest contract.

### 14.1 `REQUIREMENTS.md`

| Line(s) | Change |
|---|---|
| 117-124 (`MVP Distro Detail` block diagram) | Drop `Terminal`. Rename `Configuration` to `Settings`. |
| 199-218 (`MVP Distro Detail` text + actions) | Drop the Terminal pivot mandate. Drop the "command runner reachable from Terminal pivot" requirement. Update the pivot list to `Overview`, `Settings`, `Diagnostics`. |
| 220-238 (`MVP Command Runner`) | Mark this section as superseded by EXTRA1's "Open terminal" overview card and the validation runner. Remove the user-facing requirement. The interface `IWslDistroService.RunInDistroAsync` stays in the codebase as a service-level primitive. |
| 261-275 (`MVP Per-Distro Configuration`) | Replace "Raw text editor" with "Structured editor and raw editor over a shared lossless document model". Keep "Backup before save where feasible" and "Clear notice when distro restart is required". |
| 432-446 (`1.0 Per-Distro Settings UI`) | Reword: "Delivered by EXTRA1 ahead of Phase 12. Structured controls cover the seven officially documented sections; raw editor is preserved." |
| 738-744 (`Distro Detail UX` block) | Same diagram change as line 117-124. |
| 808-829 (`MVP Acceptance Criteria`) | Update the pivot list bullet to drop Terminal and rename Configuration to Settings. |

### 14.2 `IMPLEMENTATION_PLAN.md`

Add a new dated "EXTRA1" block at the top describing this plan and pointing to this file. Mark Phase 9 and Phase 12 entries as "Superseded by EXTRA1; see EXTRA1_IMPLEMENTATION_PLAN.md". Do not delete the original entries (history matters).

### 14.3 `TODO.md`

Replace the Phase 9 block with a new "Current Phase: EXTRA1" block whose tasks mirror Section 3 of this plan. Move the original Phase 9 + Phase 12 bullets to a "Superseded by EXTRA1" section under "Remaining Plan Items" so the trail remains visible.

### 14.4 `ARCHITECTURE.md` ADR 0002

Add a dated note at the bottom of ADR 0002:

```
2026-05-02: Per EXTRA1, the per-distro pivot composition is reduced to
Overview, Settings, Diagnostics. The Terminal pivot was removed. The
"Open terminal" lifecycle card on the Overview pivot remains the
sanctioned terminal entry point. The Configuration pivot was renamed to
Settings to match the Windows 11 system vocabulary; disambiguation from
the global Settings destination is provided by the distro header and a
"<DistroName> settings - /etc/wsl.conf" sub-header inside the pivot.
```

### 14.5 `DONE.md`

Each delivered phase E1.x block appends an entry. The entry names what was added AND, where applicable, what was removed (e.g. *"Removed the Terminal pivot and the inline command runner UI per EXTRA1. `CommandRunnerViewModel` deleted; `IWslDistroService.RunInDistroAsync` retained as a service primitive."*).

### 14.6 `GEMINI.md` Repository Notes

Add one new note:

```
- EXTRA1 introduces a lossless INI document model under CoolWSL.Core/Models/Configuration. Both the per-distro and (later) global config services should round-trip user input byte-for-byte; never serialize via System.Configuration or other lossy libraries.
```

---

## 15. Done definition

EXTRA1 is delivered when all of the following are true:

1. `DistroPage` shows three pivots: `Overview`, `Settings`, `Diagnostics`. No Terminal pivot anywhere.
2. The Settings pivot loads `/etc/wsl.conf` from the selected distro and round-trips byte-identical content through Save.
3. Editing a structured control updates the same in-memory document the raw editor edits, and switching modes loses no data.
4. Save creates a timestamped backup under `%LocalAppData%\CoolWSL\Backups\WslDistroConfig\<DistroName>\` for every existing-file save. New-file saves report "no backup needed".
5. The restart panel correctly distinguishes `wsl --terminate <distro>` (per-distro keys) from `wsl --shutdown` (only when explicitly chosen by the user), and mentions the 8-second rule.
6. The "Open WSL Settings" handoff opens the system app or a graceful fallback.
7. Validation issues display in three buckets (Errors, Warnings, Info) and Errors block save.
8. Capability gating hides `[boot]` on Windows 10, hides `[gpu]` on WSL 1 distros, disables editing entirely on system-managed distros, and explains why in plain language.
9. Verify probes run for every present key with a probe and update the chips.
10. Documentation under [Section 14](#14-document-updates-required) is updated.
11. `dotnet build` Debug + Release succeeds.
12. `dotnet test CoolWSL.Tests/CoolWSL.Tests.csproj` succeeds, with at least the new tests under [Section 12](#12-tests).
13. The packaged smoke launch with `COOLWSL_SMOKE_TEST=1` succeeds.
14. Manual smoke checklist [Section 12.7](#127-smoke-test-additions) passes on a clean machine.
