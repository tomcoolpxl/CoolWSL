# CoolWSL — Comprehensive UX & UI Review

> Audience: anyone touching the WinUI 3 layer of CoolWSL.
> Scope: visual design, information architecture, interaction patterns, accessibility, and a concrete plan to rebuild the UI on top of the existing (solid) backend services.
> Status: the backend (CoolWSL.Wsl, CoolWSL.Diagnostics, CoolWSL.Core) is reusable. Only the **CoolWSL.App** layer needs to be rewritten.
>
> **Decision update — 2026-05-02 (UX Phase E):** the sidebar `Diagnostics` destination proposed in §5.2, §6.6, and Phase E of §9 was dropped. The per-distro Diagnostics pivot (delivered in UX Phase D) already renders the full `IDiagnosticsService.GetSnapshotAsync(distroName)` result set — including the global checks (`wsl --status`, `wsl --version`, inventory, default distro, host note) — with the selected distro as context. The shell now has only Dashboard and Settings as fixed global destinations, plus the dynamic Distros group. Treat the §5.2 layout sketch, the §6.6 "Diagnostics" page spec, and the §9 "Phase E — rebuild Diagnostics" entry as historical; the authoritative IA lives in `REQUIREMENTS.md`, `ARCHITECTURE.md` (ADR 0002), and `DESIGN.md`.

---

## 0. TL;DR

The current UI is functional but visually broken on three axes:

1. **It doesn't *look* like a Windows 11 app.** Cards have no fill, no border, no corner radius. Secondary text uses raw `Opacity="0.72"` instead of theme brushes. There is no Mica/Acrylic backdrop. There are no icons anywhere. The custom title bar adds 48 px of chrome that contributes nothing.
2. **It has hard rendering bugs.** Text is blurry because every "card" is a transparent `Border` and ClearType subpixel rendering can't anti-alias against a non-opaque background. Mousewheel scrolling is dead because `ListView`s nested inside the page-level `ScrollViewer` swallow the scroll wheel without scrolling themselves.
3. **It has the wrong information architecture.** The user wants to manage distros, but the IA forces them through a flat top-level menu (Dashboard / Distros / Diagnostics / Logs / Settings). To restart Ubuntu from the Distros page you must pick it from a `ComboBox` instead of clicking it. Diagnostics is in three places. The command runner is buried at the bottom of the Distros page.

The recommended direction is a **Windows Terminal Settings × Docker Desktop hybrid**:
- Left rail with **Dashboard**, **Diagnostics**, **Settings** as fixed nodes, and **each distro as its own selectable item** (with a status dot).
- Distro detail is a single page with a pivot: **Overview · Terminal · Configuration · Diagnostics**.
- Cards are real cards (`CardBackgroundFillColorDefaultBrush`, `CornerRadius="8"`, 1 px stroke).
- Mica backdrop on the window. Custom title bar minimised to standard height with proper drag region.
- A persistent **status bar** at the bottom (WSL version · default distro · last-refresh time · running-distro count).

The full document below explains every problem, the fix, and the migration order.

---

## 1. Method

I read every authoritative document (`README.md`, `DESIGN.md`, `ARCHITECTURE.md`, `REQUIREMENTS.md`, `IMPLEMENTATION_PLAN.md`, `TODO.md`, `DONE.md`, `CODE_REVIEW.md`) and every XAML file plus its code-behind, the view-models, the DI wiring, and the entire backend service surface. The review draws conclusions only from what is actually in the repository today.

Where I cite a problem, I cite the file and line so you can verify.

---

## 2. What's currently in the app

### 2.1 Navigation

`MainWindow.xaml` hosts a custom 48 px title bar plus a `RootGrid` that swaps to `ShellPage` on first navigation.

`ShellPage.xaml:9-32` defines a `NavigationView` with five flat menu items: **Dashboard, Distros, Diagnostics, Logs, Settings**. The selection handler navigates `ContentFrame` to the matching page; **Logs** and **Settings** both land on `PlaceholderPage`.

### 2.2 Pages

| Page | XAML | Purpose | Status |
|---|---|---|---|
| Dashboard | `DashboardPage.xaml` | WSL status card, distro inventory grid with 4 inline action buttons per row | implemented |
| Distros | `DistroPage.xaml` | ComboBox selector → header → lifecycle buttons → command runner → diagnostics list | implemented |
| Diagnostics | `DiagnosticsPage.xaml` | Per-distro selector + global+probe results | implemented |
| Logs | `PlaceholderPage.xaml` | empty | stub |
| Settings | `PlaceholderPage.xaml` | empty | stub |

### 2.3 Backend (the part you keep)

All of this is solid, tested, and DI-registered in `CoolWSL.App/DependencyInjection/AppServiceCollection.cs`:

- `IWslDistroService` — inventory, environment status, lifecycle (`OpenDefaultDistroAsync`, `OpenDistroAsync`, `StartDistroAsync`, `TerminateDistroAsync`, `SetDefaultDistroAsync`, `ShutdownAsync`), `RunInDistroAsync`.
- `IWslCommandService` — low-level `ExecuteAsync(WslCommand, ct)`; UTF-16LE encoding for host metadata commands; uses `ProcessStartInfo.ArgumentList` (no shell injection).
- `IDiagnosticsService` — `GetSnapshotAsync(selectedDistroName?)` returning environment status + inventory + DNS/internet probes.
- `IDashboardStatusService` — `GetSnapshotAsync()` returning environment + inventory.
- `IAppLogger` — currently `NullAppLogger`; only the abstraction exists.
- Models: `WslDistro`, `WslEnvironmentStatus`, `WslDistroInventory`, `CommandResult`, `CommandHistoryEntry`, `WslCommandError`, `DiagnosticResult`, `DiagnosticsSnapshot`.

The new UI **does not need to touch any of this**. Every piece of data the redesigned views need is already exposed by these interfaces.

---

## 3. Concrete defects — root cause analysis

### 3.1 "The text is blurry"

**Where:** every "card" on every page.

**Mechanism:**

1. `DashboardPage.xaml:64`, `:95` and the Distros / Diagnostics pages all use `<Border Padding="20">` with **no `Background`, no `BorderBrush`, no `BorderThickness`, no `CornerRadius`**. The `Border` is structurally present but visually nothing.
2. Because the `Border` is transparent, the `TextBlock`s inside it sit directly on whatever the window backdrop happens to be. WinUI 3 disables ClearType subpixel anti-aliasing on text whose ancestor chain is not opaque, falling back to greyscale anti-aliasing. On Mica/transparent backgrounds, on a high-DPI monitor, this reads as "blurry."
3. Many secondary `TextBlock`s set `Opacity="0.72"` (e.g. `DashboardPage.xaml:29, 132, 133`, `DistroPage.xaml:30, 86, 196, 223-225`, `MainWindow.xaml:49`). `Opacity` on a `UIElement` forces the subtree into a compositor surface, which **also** disables subpixel rendering.
4. `UseLayoutRounding="True"` is set on each page (good), but it has to fight against the `Opacity` layers above.

**Fix:**

- Replace every "naked" `Border` with a real card style:
  ```xml
  <Border Style="{StaticResource Card}">…</Border>
  ```
  where `Card` is defined once in `App.xaml`:
  ```xml
  <Style x:Key="Card" TargetType="Border">
      <Setter Property="Background" Value="{ThemeResource CardBackgroundFillColorDefaultBrush}" />
      <Setter Property="BorderBrush" Value="{ThemeResource CardStrokeColorDefaultBrush}" />
      <Setter Property="BorderThickness" Value="1" />
      <Setter Property="CornerRadius" Value="8" />
      <Setter Property="Padding" Value="20" />
  </Style>
  ```
- Stop using `Opacity` for typography. Use `Foreground="{ThemeResource TextFillColorSecondaryBrush}"` for de-emphasised text and `TextFillColorTertiaryBrush` for hints. These are designed for the WinUI 11 type ramp, respect high-contrast, and do not break ClearType.
- Set `TextOptions.TextFormattingMode="Ideal"` and `TextOptions.TextRenderingMode="ClearType"` at the `Page` root once you have an opaque card chain.

### 3.2 "Mousewheel scrolling doesn't work"

**Where:** Dashboard, Distros, Diagnostics. Anywhere the cursor is over a `ListView`.

**Mechanism:**

Each page is laid out as `ScrollViewer ▶ StackPanel ▶ Border ▶ StackPanel ▶ ListView` (e.g. `DashboardPage.xaml:11-147`). A `ListView` *contains its own internal `ScrollViewer`*. When the wheel fires:

1. The event hits the inner `ScrollViewer` first.
2. The inner `ScrollViewer` has unbounded height (no `MaxHeight` on the `ListView`), so its content fits and there's nothing to scroll.
3. WinUI does **not** bubble unhandled wheel events out to an ancestor `ScrollViewer` by default. The wheel is consumed.
4. As a bonus, `ShellPage.xaml:31` sets `ScrollViewer.VerticalScrollMode="Disabled"` on the `Frame`, removing the last fallback scroll surface.

**Fix:** pick one. There are three valid approaches; (B) is the recommended one.

- **(A) Constrain the inner list:** give every nested `ListView` a `MaxHeight` so the inner `ScrollViewer` has somewhere to go. Cheap, works, but breaks the "single long page" feel and adds nested scrollbars — discouraged.
- **(B) Replace `ListView` with `ItemsRepeater` inside the page-level `ScrollViewer`.** `ItemsRepeater` does not introduce its own scroll viewer. Wheel events flow naturally to the outer `ScrollViewer`. This is what Windows 11 Settings does. **Recommended.**
- **(C) If you must keep `ListView`, attach a `PointerWheelChanged` handler that re-routes deltas to the outer `ScrollViewer.ChangeView`.** Fragile, last resort.

Also drop `ScrollViewer.VerticalScrollMode="Disabled"` from the `Frame` — there is no reason to disable it.

### 3.3 Other rendering issues caught while reading the code

- `PlaceholderPage.xaml:8` uses `<ScrollView>` (the new control, partially supported, often misbehaves) instead of `<ScrollViewer>`. Switch to `ScrollViewer`.
- `App.xaml` registers only `XamlControlsResources`. There is no app-wide style dictionary, so every page reinvents type sizes (28/24/20/18/14) and spacing (8/12/16/24/32) inline. You will lose consistency the moment a third developer touches it.
- Page-level `ScrollViewer`s have `IsTabStop="True"` and `AllowFocusOnInteraction="True"` (e.g. `DashboardPage.xaml:17-18`). This makes the entire scroll surface a Tab stop, so keyboard users hit a meaningless "scroll region" focus before they hit the first button. Remove both.
- `MainWindow.xaml.cs` hard-codes the title-bar hover/pressed colours as raw `Color.FromArgb(20, 255, 255, 255)` and similar. They will be invisible in light theme and wrong in high-contrast. Use `SubtleFillColorSecondaryBrush` / `SubtleFillColorTertiaryBrush`.
- The icon "C badge" in `MainWindow.xaml:25-39` is a single coloured circle with the letter C. It is the only visual identity the app has. Replace with a real Segoe Fluent / custom glyph and ship a proper `.ico` — the app currently has no app icon path in `app.manifest`.

---

## 4. UX problems beyond rendering

These are independent of how the pixels look. Even if §3 is fixed, the app is hard to use.

### 4.1 Information architecture is upside-down

The user's mental model is "I have N distros and I want to do something with one of them." The current IA forces:

> Click **Distros** in the rail → wait for the page to load → open the **ComboBox** → pick **Ubuntu** → scroll past header → press **Start** → scroll back up to read status.

A distro should be a **first-class navigation node**, not a dropdown selection. Both Windows Terminal Settings (each profile is a sidebar item) and Docker Desktop (each container/image is a row in the main list) work this way.

### 4.2 The same data lives in three places

Diagnostics results render in **Dashboard** (warning summary), **Distros** (per-distro probes), and **Diagnostics** (everything). The user has no way to predict which page is "the" diagnostic page for their question. Pick one home for diagnostics; reference from the others.

### 4.3 Action overload on Dashboard rows

`DashboardPage.xaml:138-143` renders four buttons per distro row: **Open · Start · Terminate · Set Default**. With three distros this is twelve buttons stacked vertically. The fix is one primary action (Open) plus a `…` overflow menu for the rest, or move full lifecycle to the distro detail page.

### 4.4 The command runner is buried

`DistroPage.xaml:82-183` puts the command runner at the bottom of the Distros page, after the lifecycle section. To run a quick `uname -a` on Ubuntu the user has to:
- click Distros → pick Ubuntu → scroll down → focus the textbox → type → Ctrl+Enter.

The command runner deserves either its own pivot tab inside the distro detail, or a global command palette (`Ctrl+K` style) that runs against the currently selected distro.

### 4.5 No global state cues

There is nowhere on screen that always tells the user:

- Is WSL installed and running?
- Which distro is the default?
- How many distros are running right now?
- When was the data last refreshed?

The Dashboard answers all four, but only when the user is on the Dashboard. A bottom **status bar** is the conventional fix.

### 4.6 No empty states with affordance

When there are no distros, `DashboardPage.xaml:99-100` shows `EmptyStateTitle` and `EmptyStateMessage` as plain text. There is no glyph, no "Install Ubuntu" button, no link to `ms-windows-store://...`. An empty state should always offer the next action.

### 4.7 No keyboard map

Only `F5` (refresh), `Ctrl+Enter` (run), `Esc` (cancel) are wired. There's no `Ctrl+,` (settings), `Ctrl+1..9` (jump to distro), `Ctrl+K` (command palette), `Ctrl+L` (clear output), `Ctrl+Shift+P` (action palette).

### 4.8 Accessibility gaps (carried over from CODE_REVIEW.md but worth re-stating)

- `AutomationProperties.Name` is set on Dashboard row buttons but **does not include the distro name** (`DashboardPage.xaml:139-142` references `OpenAutomationName` which exists, but the per-row Distros page buttons in `DistroPage.xaml:67-72` use static labels like "Start distro" with no context).
- No live region announcing async results ("command finished, exit code 0").
- Opacity-based de-emphasis fails WCAG AA in high-contrast.
- Diagnostics raw output sits inside a non-virtualised `ListView`; with a dozen items the page jitters on scroll.

---

## 5. Recommended design direction

### 5.1 Visual language: "Windows 11 Settings, with content"

Windows Terminal's settings UI is the single best reference for what a Win11 desktop control panel should feel like. Adopt:

- **Mica backdrop** on the window (`SystemBackdrop = MicaBackdrop`).
- **`NavigationView` with `PaneDisplayMode="Left"`**, icons + labels, compact mode auto when narrow.
- **Card-based content**: rounded 8 px corners, 1 px stroke, `CardBackgroundFillColorDefaultBrush`. The `WindowsCommunityToolkit.WinUI.Controls` package provides `SettingsCard` and `SettingsExpander` — use them. They handle hover, click, expand, and accessibility for free.
- **Segoe Fluent Icons** for every nav item, every action button, every status indicator.
- **Type ramp** matched to Win11 Settings: 28 (page title) / 20 (section header) / 18 (subsection) / 14 (body) / 12 (caption). Centralise in `App.xaml`.
- **Spacing tokens**: 4 / 8 / 12 / 16 / 24 / 32. No more inline magic numbers.

### 5.2 Layout pattern: hybrid (Win Terminal Settings × Docker Desktop)

```
┌─────────────────────────────────────────────────────────────┐
│ [Title bar — Mica, 32px standard height]                     │
├──────────────┬──────────────────────────────────────────────┤
│              │                                              │
│ ⌂ Dashboard  │   ── Page header: "Ubuntu" ──                │
│ ⚕ Diagnostics│   ┌ Pivot: Overview · Terminal · Config ·   │
│ ⚙ Settings   │   │         Diagnostics                  ┐ │
│              │   │                                      │ │
│ DISTROS  +   │   │   [card]                             │ │
│ ● Ubuntu     │   │   [card]                             │ │
│ ◌ Debian     │   │                                      │ │
│ ⊘ docker-…   │   └──────────────────────────────────────┘ │
│              │                                              │
├──────────────┴──────────────────────────────────────────────┤
│ WSL 2.5.10 • 3 distros • Default: Ubuntu • Refreshed 12 s   │
└─────────────────────────────────────────────────────────────┘
```

- **Top-level rail nodes** (always visible): Dashboard, Diagnostics, Settings.
- **DISTROS group**: dynamic list bound to `WslDistroInventory.Distros`. Each item shows a status glyph (● running, ◌ stopped, ⊘ system-managed) and the distro name. Selecting a distro navigates to its detail page. The **+** button next to the group label opens an "Install distro" flow (scope-permitting; for now it can deep-link to the Store).
- **Pivot inside each distro page**: Overview, Terminal, Configuration, Diagnostics. This replaces the stacked sections on the current `DistroPage`.
- **Status bar**: bottom of the window, single row, fed by `DashboardStatusService` + a small `RefreshClock` view-model.

### 5.3 Why this beats the alternatives

- **Sidebar-with-distros** (your first idea) is the right primary structure. Putting *commands* in the sidebar (your second idea) is wrong — commands are per-distro and ad-hoc; promoting them to navigation would clutter the rail forever.
- **Pure Docker Desktop** (your third idea) puts everything in one big resizable list with a detail pane. Works for hundreds of containers; overkill for ≤10 distros and unfamiliar to Windows users.
- **Pure Windows Terminal Settings** treats every page as a long settings list with `SettingsCard`s. Perfect for configuration; wrong for dynamic status (Dashboard) and live output (Terminal).

The hybrid above takes the IA from Docker Desktop (entities in the rail), the visual style from Windows Terminal Settings (cards, icons, spacing), and the layout primitives from WinUI 11.

---

## 6. Page-by-page redesign

For each page I list **what the user needs to do**, **what data is already available from the backend**, and **the proposed layout**.

### 6.1 Window chrome

- Remove the 48 px custom title bar in `MainWindow.xaml:11-53`. Replace with the WinUI 3 standard title bar (32 px) and use `Window.SetTitleBar(AppTitleBar)` only on a thin drag region above the content.
- Apply Mica: in `App.xaml.cs`, set `m_window.SystemBackdrop = new MicaBackdrop()`.
- Single app icon (`Assets/AppIcon.ico`) referenced from the title bar and the manifest.

### 6.2 Sidebar

- `NavigationView`, `PaneDisplayMode="Left"`, `IsPaneToggleButtonVisible="True"`.
- Icons via `FontIcon Glyph="&#xE80F;"` (Home), `&#xE9D9;` (Diagnostics), `&#xE713;` (Settings).
- Below the fixed items, a `NavigationViewItemHeader Content="Distros"` followed by an `ItemsRepeater` bound to `inventory.Distros`. Each item is a small custom control: status dot (running/stopped/system-managed), distro name, ellipsis-menu for context actions.
- The whole sidebar collapses to icon-only at narrow widths automatically.

### 6.3 Dashboard

**User goal:** "Is my WSL healthy? What's my default? Anything wrong?"

**Data:** `IDashboardStatusService.GetSnapshotAsync()` → `DashboardStatusSnapshot { EnvironmentStatus, DistroInventory }`.

**Layout (top to bottom):**
1. **Hero status card.** Single sentence: "WSL 2.5.10 is running. 3 distros installed, 1 running." With a status glyph (✓ green, ⚠ amber, ✗ red).
2. **Distros grid.** `ItemsRepeater` with a tile per distro (name, state pill, version chip, default star). Click a tile → distro detail. **Replaces the four-button row.** Move lifecycle into the detail page or into the per-tile context menu.
3. **Quick actions row.** Three buttons: "Run command…" (opens command palette pre-targeted at default distro), "Open default terminal", "Shutdown all" (with confirmation).
4. **Health summary card.** Top three diagnostic findings, each linking to the full Diagnostics page.

### 6.4 Distro detail (replaces `DistroPage`)

**User goal:** "Manage Ubuntu specifically."

**Layout:** page header with the distro name, state pill, default star; below it a `Pivot`:

- **Overview** (`SettingsCard`s for state, version, default, capability messages from `DistroCapabilityHelper`, and lifecycle actions Start/Terminate/Set Default/Open Terminal as `SettingsCard.ActionIcon` rows).
- **Terminal** (the existing command runner, redesigned: input row at top, single output pane with a tab strip for stdout / stderr / combined, history list collapsed by default in an expander).
- **Configuration** (Phase 8 target — `/etc/wsl.conf` editor; cards for memory/CPU/networking once the structured editor lands in Phase 12).
- **Diagnostics** (per-distro probes via `IDiagnosticsService`).

This is also where the Phase 9–16 work lands, so the IA scales without re-thinking the rail.

### 6.5 Command runner (within Terminal pivot, plus global palette)

- **Single output area, not side-by-side.** Stdout and stderr are colour-coded (stderr in `SystemFillColorCriticalBrush`). Add a "Show stderr inline" toggle for users who want them merged. The current side-by-side layout halves usable width and never matches what they see in a real terminal.
- **Copy** and **Clear** buttons in the output toolbar.
- **History** is an expander, default collapsed, virtualised when expanded.
- **Global command palette (`Ctrl+K`)** opens a centred `ContentDialog`-like overlay with: distro picker (defaults to selected sidebar distro), command field, run button. Same backend (`RunInDistroAsync`).

### 6.6 Diagnostics

**User goal:** "Something is wrong, tell me what."

- One page, grouped by **severity** (Errors → Warnings → OK), not by source.
- Each result is a `SettingsExpander`: collapsed shows title + severity glyph + summary; expanded reveals details, suggested next step, the command that was run, and the raw output in a monospace code block.
- A **single** "Refresh" button at the top with a `LastUpdatedAt` caption next to it.
- **Remove diagnostics from the Distros page.** Per-distro diagnostics live in the distro detail's Diagnostics pivot.

### 6.7 Settings

Convert from `PlaceholderPage` to a real Settings page using `SettingsExpander`s grouped under headings:

- **WSL** — link to `.wslconfig` editor (Phase 8), default distro pick, "Shutdown all".
- **Appearance** — theme override (System / Light / Dark), Mica on/off.
- **Behaviour** — confirm-before-destructive toggle, command timeout default, log retention.
- **Diagnostics** — diagnostic command profiles, where logs are stored, "Open log folder".
- **About** — version, "Open repository", "Report an issue".

This matches Windows 11 Settings exactly and is what most Windows users now expect.

### 6.8 Logs

Replace `PlaceholderPage` with a real log viewer once `IAppLogger` is implemented (currently `NullAppLogger`):

- Filter row (severity, source, date).
- Virtualised `ItemsRepeater` of log entries.
- "Open log folder" button.

---

## 7. Component library to introduce

Centralise these in `CoolWSL.App/Styles/` and merge them in `App.xaml`. A non-exhaustive list:

| Component | Purpose | Notes |
|---|---|---|
| `Card` (Style) | Replace every naked `Border`. | §3.1 fix. |
| `PageHeader` (UserControl) | Title + subtitle + optional action slot. | Removes 28/0.72 boilerplate from every page. |
| `StatusPill` (UserControl) | "Running"/"Stopped"/"System-managed" with semantic colour. | Bind to `WslDistroState`. |
| `SectionHeader` (Style) | 20 px SemiBold + optional caption. | One look everywhere. |
| `EmptyState` (UserControl) | Glyph + headline + body + primary action. | For no-distro / WSL-not-installed / no-results. |
| `LiveRegionTextBlock` | `AutomationProperties.LiveSetting=Polite` wrapper. | Announce async results to screen readers. |
| `OutputBlock` (UserControl) | Monospace, virtualised, with copy/clear toolbar. | Shared by command runner and diagnostics raw output. |
| `StatusBar` (UserControl) | Bottom bar with WSL version, default distro, running count, last refresh. | New. |

The `WindowsCommunityToolkit.WinUI.Controls` `SettingsCard` / `SettingsExpander` cover the rest — no need to roll your own.

---

## 8. Specific bug fixes (paste-ready snippets)

### 8.1 Real card style — fixes blurry text everywhere

Add to `App.xaml`:

```xml
<Style x:Key="Card" TargetType="Border">
    <Setter Property="Background"      Value="{ThemeResource CardBackgroundFillColorDefaultBrush}" />
    <Setter Property="BorderBrush"     Value="{ThemeResource CardStrokeColorDefaultBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="CornerRadius"    Value="8" />
    <Setter Property="Padding"         Value="20" />
</Style>

<Style x:Key="SecondaryText" TargetType="TextBlock">
    <Setter Property="Foreground" Value="{ThemeResource TextFillColorSecondaryBrush}" />
</Style>
```

Then replace every `<Border Padding="20">` in `DashboardPage.xaml`, `DistroPage.xaml`, `DiagnosticsPage.xaml` with `<Border Style="{StaticResource Card}">`, and every `<TextBlock Opacity="0.72" …>` with `<TextBlock Style="{StaticResource SecondaryText}" …>`.

### 8.2 Mousewheel — fixes scrolling everywhere

Replace each top-level `<ListView … ItemsSource="…">` that lives **inside** a page-level `ScrollViewer` with:

```xml
<ItemsRepeater ItemsSource="{x:Bind …}">
    <ItemsRepeater.Layout>
        <StackLayout Spacing="12" />
    </ItemsRepeater.Layout>
    <ItemsRepeater.ItemTemplate>
        <DataTemplate x:DataType="…"> … </DataTemplate>
    </ItemsRepeater.ItemTemplate>
</ItemsRepeater>
```

`ItemsRepeater` does not introduce a nested scroll viewer, so wheel events bubble to the outer `ScrollViewer`. Selection / virtualisation still work; if you need selection states, wrap items in `ToggleButton` or add a `ListView` only inside an explicit `MaxHeight` region.

Then in `ShellPage.xaml:31` remove `ScrollViewer.VerticalScrollMode="Disabled"` and `ScrollViewer.HorizontalScrollMode="Disabled"` — the `Frame` should not be policing scroll.

### 8.3 Page-level `ScrollViewer` cleanup

Remove `IsTabStop="True"` and `AllowFocusOnInteraction="True"` from `DashboardPage.xaml:17-18`, `DistroPage.xaml:18-19`, `DiagnosticsPage.xaml` (same lines). Also drop `HorizontalScrollBarVisibility="Auto"` — vertical-only is the right default for settings-style pages.

### 8.4 Title bar

In `MainWindow.xaml.cs`, drop the hard-coded RGBA hover/pressed colours and use:

```csharp
appTitleBar.ButtonHoverBackgroundColor   = ((SolidColorBrush)Application.Current.Resources["SubtleFillColorSecondaryBrush"]).Color;
appTitleBar.ButtonPressedBackgroundColor = ((SolidColorBrush)Application.Current.Resources["SubtleFillColorTertiaryBrush"]).Color;
```

Better yet, drop the custom title bar to a slim 32 px drag region and rely on `Window.SetTitleBar(...)`.

### 8.5 `PlaceholderPage`

Change `<ScrollView>` to `<ScrollViewer VerticalScrollBarVisibility="Auto">` in `PlaceholderPage.xaml:8`. Or delete the file entirely once `Settings` and `Logs` get real pages.

### 8.6 Mica backdrop

In `App.xaml.cs`, after creating `m_window`:

```csharp
if (MicaController.IsSupported())
{
    m_window.SystemBackdrop = new MicaBackdrop { Kind = MicaKind.Base };
}
```

Required NuGet: `Microsoft.Graphics.Win2D` for backdrop helpers (the package reference may already be transitive via Windows App SDK 2.0.1).

---

## 9. Migration plan

A rebuild this large is best done as a parallel "v2 shell" branch behind a build-time flag, then cut over once parity is reached. The view-models and services do not change; only views move.

**Phase A — foundations (1 sitting)**
- Add `App.xaml` resource dictionary: spacing tokens, type ramp, `Card` style, `SecondaryText` style, `StatusPill` template.
- Add Mica backdrop in `App.xaml.cs`.
- Drop the custom title bar; use the standard one.
- Verify rendering on 100/125/150% DPI.

**Phase B — rebuild ShellPage (1 sitting)**
- New `ShellPage` with `NavigationView` containing fixed nodes + a `NavigationViewItemHeader Content="Distros"` + `ItemsRepeater` bound to `WslDistroInventory.Distros`.
- Status bar `UserControl` at the bottom of `MainWindow`.
- Frame still hosts pages; `ScrollViewer.VerticalScrollMode` stripped.

**Phase C — rebuild Dashboard (1 sitting)**
- Hero status card → `IDashboardStatusService`.
- Distros grid using `ItemsRepeater` (no `ListView`, no inline buttons; clicking a tile navigates).
- Quick-actions row.
- Verify mousewheel on a window short enough to require scrolling.

**Phase D — rebuild distro detail (2 sittings)**
- New `DistroDetailPage` with `Pivot`: Overview, Terminal, Configuration (placeholder until Phase 8/9 land), Diagnostics.
- Lifecycle moves to Overview as `SettingsCard` rows.
- Command runner redesigned: single output area, stderr highlighted, history collapsible.
- Per-distro diagnostics moves here from the old Distros page.

**Phase E — rebuild Diagnostics (1 sitting)**
- One page, severity-grouped, `SettingsExpander` per result.
- Last-updated caption.

**Phase F — Settings + Logs (1 sitting once `IAppLogger` is real)**
- Replace `PlaceholderPage` usages.

**Phase G — accessibility + a11y test pass (1 sitting)**
- `AutomationProperties.Name` includes distro name on every per-distro action.
- Live region announces async results.
- Tab order verified manually.
- High-contrast smoke check.

Each phase ends with a manual smoke launch (`COOLWSL_SMOKE_TEST=1`) and the existing test suite must stay green.

---

## 10. Backend → UI mapping (the part that doesn't change)

| New surface | Backend method already available |
|---|---|
| Hero status card on Dashboard | `IDashboardStatusService.GetSnapshotAsync()` |
| Sidebar distro list | `IWslDistroService.GetDistroInventoryAsync()` |
| Status bar (WSL version, kernel, default) | `IWslDistroService.GetEnvironmentStatusAsync()` |
| Distro Overview pivot | `WslDistro` + `DistroCapabilityHelper` (no new code) |
| Lifecycle actions | `IWslDistroService.OpenDistroAsync / StartDistroAsync / TerminateDistroAsync / SetDefaultDistroAsync / ShutdownAsync` |
| Open default terminal | `IWslDistroService.OpenDefaultDistroAsync` |
| Command runner & palette | `IWslDistroService.RunInDistroAsync(name, command, timeout?)` |
| Diagnostics pivot & full Diagnostics page | `IDiagnosticsService.GetSnapshotAsync(selectedDistroName?)` |
| Logs page | requires implementing `IAppLogger` (currently `NullAppLogger`) |
| Settings → `.wslconfig` editor | requires Phase 8 work in `CoolWSL.Configuration` (currently empty stub) |

The new UI introduces zero new abstractions in the backend.

---

## 11. Open questions (decide before Phase A)

1. **Mica vs. plain.** Mica looks great but requires a clean opaque card pass. Are you OK with the cards being opaque cards and the gaps showing the desktop tint? (Recommended: yes.)
2. **Distros in the rail vs. a top-level "Distros" page that lists them.** I argue for in-the-rail because it shortens every flow by two clicks. The trade-off is rail length on machines with many distros — but for ≤10 the rail is fine.
3. **Single output area vs. stdout/stderr split in the command runner.** Single is more terminal-like and gives more width to the content; split is what the current page does and what some users may expect from a "diagnostic tool" framing.
4. **Settings as a single page vs. sub-routes.** Win11 Settings uses sub-routes (e.g. Settings → System → Display). For CoolWSL's scope, a single page with `SettingsExpander`s is enough.
5. **Command palette (`Ctrl+K`) — in scope for v2 or defer?** It's easy to add but needs design (where does it pull "distro" from when invoked from the Dashboard?). Reasonable to defer.

---

## 12. What this review deliberately does **not** propose

- No changes to `CoolWSL.Wsl`, `CoolWSL.Diagnostics`, `CoolWSL.Core`. The backend is sound.
- No third-party theme. Stick with native Win11 system theme + community toolkit cards.
- No web-tech (Webview2-based UI). Native WinUI 3 is the right tool.
- No "Docker Desktop port" — only the IA primitive (entities in rail) is borrowed, not the visual chrome.
- No removal of safety features (confirmations, Docker-Desktop distro protection, degraded modes). Those stay verbatim.

---

## 13. Closing rationale

The current app is **80 % functionally correct and 20 % visually finished**. The view-models, services, and command pipeline are tested and trustworthy. The XAML, on the other hand, was clearly written to ship Phase-by-Phase functionality without ever stopping to define a visual system or a navigation primitive. That is the gap this review closes.

Following §5–§9 yields:
- a Windows-11-native looking app (cards, Mica, Fluent icons, real type ramp);
- working text rendering and working mouse-wheel scrolling;
- a navigation model where every primary user task (manage a distro, run a command, debug) is one click from the rail;
- a foundation that the remaining IMPLEMENTATION_PLAN phases (8–19) can extend without further IA changes.

The work is mechanical once §5 and §8 are agreed. It is not a research project — it is a few sittings of disciplined XAML.
