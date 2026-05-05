# CoolWSL

A WSL Control Center for Windows 11.

## Project Rules

This repository is dedicated to building **CoolWSL** throughout the course.
Do not start or switch to a different project unless explicitly instructed by the course.

## Authoritative Files

The following files define the source of truth for the project:

* `REQUIREMENTS.md` – current v1 product scope and acceptance boundaries
* `DESIGN.md` – current shipped UX and interaction model
* `ARCHITECTURE.md` – current shipped architecture and active technical decisions
* `EXTRA_FEATURES.md` – approved future product direction and expansion ideas
* `TODO.md` – current actionable tasks when the user wants an explicit work queue
* `DONE.md` – completed and verified work

## Workflow Rules

### Task Management

* Each item in `TODO.md` must be small enough to complete within one review cycle.
* Derive `TODO.md` items from the current approved work, not from retired roadmap planning.
* Use `EXTRA_FEATURES.md` as the source for future ideas only when the user is explicitly planning or scheduling future work.
* Update `TODO.md` **before** starting any new multi-step implementation work when the user wants task tracking.
* After completing work, update both `TODO.md` and `DONE.md`.

### Completion Criteria

Move a task to `DONE.md` only when all of the following are true:

* Implementation is complete
* Required checks/tests pass
* Changes have been reviewed
* Documentation is updated if needed

`DONE.md` must contain **only verified and complete work**.

### File Maintenance

* Update `REQUIREMENTS.md`, `DESIGN.md`, and `ARCHITECTURE.md` when shipped product scope, UX, or technical structure changes.
* Update `EXTRA_FEATURES.md` when the approved future direction changes.
* Do not reintroduce retired roadmap or speculative planning content into `REQUIREMENTS.md`, `DESIGN.md`, or `ARCHITECTURE.md`.

### Change Control

* Ask before:

  * Large refactors
  * Changing directory structure
  * Removing or altering tests

### Context Handling

* For narrow or focused tasks, include the relevant authoritative files in the prompt instead of rewriting context.

### Pre-Completion Checklist

Before marking work as done:

* Review the diff
* Run all required checks/tests
* Update documentation if the change impacts scope or structure

## Repository Notes

* Scope discipline: `TODO.md` may be intentionally minimal or empty when there is no active user-approved work queue; future ideas stay in `EXTRA_FEATURES.md`; `REQUIREMENTS.md`, `DESIGN.md`, and `ARCHITECTURE.md` should stay focused on shipped behavior.
* WinUI/XAML guardrails: keep `XamlControlsResources` in `App.xaml`; unnamed `x:Load` fails compilation; `DistroPage` async selection state should use live `Visibility`; `ColumnDefinition.Width` needs a `GridLength` resource rather than `SettingsValueControlWidth`; centered scrolling layouts should use a named host `Grid` bound to `ActualWidth`.
* Smoke verification: `COOLWSL_SMOKE_TEST=1` with optional `COOLWSL_SMOKE_TEST_FILE` is the preferred local smoke check; for unpackaged verification, prefer `dotnet run --project CoolWSL.App/CoolWSL.App.csproj -c Debug`; keep the smoke exit path in `App.OnLaunched`, not `Window.Activated`.
* WSL execution and config handling: host-side metadata `wsl.exe` commands can emit UTF-16LE and need explicit Unicode handling, but in-distro `--exec` should not; use `ProcessStartInfo.ArgumentList`; `WslListParser` and `WslStatusParser` must fail soft on localized or unsupported output; keep the lossless INI model for config editing; global `%UserProfile%\.wslconfig` remains read-only in-app and hands off to WSL Settings via `explorer.exe shell:AppsFolder\{6D809377-6AF0-444B-8957-A3773F02200E}\WSL\wslsettings\wslsettings.exe` because `wslsettings.exe` alone is not reliable here.
* Local shell behavior: agent PowerShell commands on this machine should run with no profile loading because the user's profile writes outside the workspace and probes console/CIM state before the intended command runs.
* Theme and versioning: theme persists through `CoolWSL.App/Services/ThemePreferenceService.cs` and is applied by `MainWindow`; keep `IncludeSourceRevisionInInformationalVersion=false` in `Directory.Build.props`, and use `COOLWSL_VERSION` to stamp newer versions without editing project files.
* Release flow: releases are tag-driven through CI/CD only; push `main`, create tag `vX.Y.Z`, and push the tag; after triggering a release, use one blocking `gh run watch <run_id> --repo tomcoolpxl/CoolWSL --exit-status`, then verify once with `gh release view vX.Y.Z --repo tomcoolpxl/CoolWSL` and confirm MSI, ZIP, and checksums assets are present.
* Packaging and winget: packaging is installer-first through `build/Invoke-ReleaseInstaller.ps1` and CI publishes `.msi`, `.zip`, and `.checksums.txt` assets from stable `vX.Y.Z` tags; source winget hash and MSI-derived ARP metadata from the same published release assets; framework-dependent installers must emit the `Microsoft.DotNet.DesktopRuntime.10` package dependency; generate WiX/MSI winget manifests with `ElevationRequirement: elevatesSelf` so the installer prompts for UAC itself instead of tripping the current non-elevated winget MSI launch bug; package the Release build layout directly instead of self-contained `dotnet publish`; local silent upgrade failures like `1603` or `Error 1730` are usually admin-related validation limits, not proof of a bad rebuild; keep `ICE03` suppressed specifically in `build/CoolWSL.Installer.wixproj`; keep `RemoveFolder Directory="CoolWSLProgramMenuFolder" On="uninstall"` in the shortcut-owning component to satisfy `ICE64`.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:

* State your assumptions explicitly. If uncertain, ask.
* If multiple interpretations exist, present them - don't pick silently.
* If a simpler approach exists, say so. Push back when warranted.
* If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

* No features beyond what was asked.
* No abstractions for single-use code.
* No "flexibility" or "configurability" that wasn't requested.
* No error handling for impossible scenarios.
* If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:

* Don't "improve" adjacent code, comments, or formatting.
* Don't refactor things that aren't broken.
* Match existing style, even if you'd do it differently.
* If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:

* Remove imports/variables/functions that YOUR changes made unused.
* Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:

* "Add validation" → "Write tests for invalid inputs, then make them pass"
* "Fix the bug" → "Write a test that reproduces it, then make it pass"
* "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:

1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.
