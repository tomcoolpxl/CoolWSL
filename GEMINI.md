# CoolWSL

A WSL Control Center for Windows 11.

## Project Rules

This repository is dedicated to building **CoolWSL** throughout the course.
Do not start or switch to a different project unless explicitly instructed by the course.

## Authoritative Files

The following files define the source of truth for the project:

* `REQUIREMENTS.md` – scope and acceptance criteria
* `IMPLEMENTATION_PLAN.md` – phases and work ordering
* `TODO.md` – current actionable tasks
* `DONE.md` – completed and verified work

## Workflow Rules

### Task Management

* Each item in `TODO.md` must be small enough to complete within one review cycle.
* Always derive `TODO.md` items from the current phase in `IMPLEMENTATION_PLAN.md`.
* Update `TODO.md` **before** starting any new implementation work.
* After completing work, update both `TODO.md` and `DONE.md`.

### Completion Criteria

Move a task to `DONE.md` only when all of the following are true:

* Implementation is complete
* Required checks/tests pass
* Changes have been reviewed
* Documentation is updated if needed

`DONE.md` must contain **only verified and complete work**.

### File Maintenance

* Update `REQUIREMENTS.md` when scope or acceptance criteria change.
* Update `IMPLEMENTATION_PLAN.md` when task order, phases, or grouping changes.

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

* `TODO.md` can keep the current phase explicit and still include remaining plan-derived phase checklists as backlog when the user asks for the full set of TODO items from `IMPLEMENTATION_PLAN.md`.
* Prefer preserving exact `Exact TODO.md entries` wording from `IMPLEMENTATION_PLAN.md` when rebuilding `TODO.md`.
* WinUI 3 startup can crash in `Microsoft.UI.Xaml.dll` if `App.xaml` omits `XamlControlsResources` while using controls like `NavigationView`; keep the merged dictionary in `App.xaml`.
* WinUI compiled XAML fails if an element uses `x:Load` without an `x:Name`; on the new Distros and Diagnostics pages, keep deferred elements explicitly named instead of dropping `x:Bind` to work around compiler failures.
* Redirected host-side `wsl.exe` metadata commands (`--status`, `--version`, `--list --verbose`, and similar non-`--exec` mutations) emit UTF-16LE on this machine; keep explicit Unicode stream encoding on those commands, but do not force that encoding onto in-distro `--exec` commands.
* CoolWSL supports non-interactive startup verification with `COOLWSL_SMOKE_TEST=1` and an optional `COOLWSL_SMOKE_TEST_FILE` marker path. This is the preferred local smoke-launch check.
* Phase 3 uses `ProcessStartInfo.ArgumentList` for WSL command execution so distro names with spaces and shell metacharacters stay as raw arguments instead of shell-interpreted text.
* `WslListParser` and `WslStatusParser` must degrade safely when WSL output is unsupported, localized, or missing expected fields instead of guessing inventory or environment details.
* Agent shell commands on this Windows machine should run PowerShell with `login=false` / no profile loading. The user's PowerShell profile writes outside the workspace and probes console/CIM state, which causes sandbox access-denied noise before the intended command runs.
* EXTRA1 introduces a lossless INI document model under CoolWSL.Core/Models/Configuration. Both the per-distro and (later) global config services should round-trip user input byte-for-byte; never serialize via System.Configuration or other lossy libraries.
* For release automation monitoring, prefer non-interactive polling via `gh api repos/<owner>/<repo>/actions/runs/<run_id>` (`status`, `conclusion`, `updated_at`) and `.../jobs` step state over `gh run watch`; in this environment, watch output can enter alternate-buffer mode and obscure completion state.
* If a release run is stalled (no `updated_at` movement and same in-progress step), recover by canceling the run, re-pushing the same release tag to retrigger CI, and then polling the new run ID until `status=completed`.

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
