# Extra Features for the WSL UI App

## Product direction

The app should not only be a graphical wrapper around `wsl.exe`. A stronger direction is:

> A WSL control plane for diagnostics, configuration, networking, storage, developer services, and architecture visibility.

The key opportunity is to expose state that WSL normally hides:

- VM state
- distro instance state
- network state
- filesystem and mount state
- systemd state
- interop state
- disk/VHD state
- compact global resource state, without duplicating official WSL Settings
- configuration drift
- developer service reachability

This makes the app more useful than a basic distro launcher.

## WSL architecture clarification

A useful concept to capture in the UI is that WSL 2 does not normally run each distribution as a separate full Hyper-V VM.

A more precise model is:

> WSL 2 runs active distributions inside a single managed lightweight utility VM. The distributions share the same Linux kernel and several VM-level resources, while each distro has its own container-like isolation boundaries such as PID namespace, mount namespace, user namespace, cgroup namespace, root filesystem, and init process.

This distinction is important for both the UI and the mental model presented to users.

Prefer precise wording:

- Say `WSL 2 utility VM`, not just `VM`, when discussing the shared runtime.
- Say `distro instance`, not `VM`, when discussing Ubuntu, Debian, Kali, etc.
- Say `container-like isolation`, not only `process isolation`.
- Say `global WSL VM setting`, not `distro setting`, for `.wslconfig` values.
- Say `per-distro setting`, not `global setting`, for `/etc/wsl.conf` values.

Avoid misleading wording:

| Avoid | Prefer |
|---|---|
| Each distro is a VM | Each WSL 2 distro is an isolated instance inside the shared WSL utility VM |
| WSL uses process isolation | WSL 2 uses container-like namespace and cgroup isolation per distro |
| Restart Ubuntu to apply `.wslconfig` | Shut down WSL to apply global VM settings |
| Networking is per distro | WSL 2 networking is mostly shared at the utility VM level |

## EXTRA1 - Settings handoff and per-distro configuration

### Goal

Do not duplicate the official WSL Settings app for global WSL 2 VM settings. Treat it as the preferred editor for global `.wslconfig` values. This app should only show a compact read-only global summary when it helps users understand diagnostics or per-distro behavior, then link through to WSL Settings for changes. The app should focus its own editable settings UI on per-distro `/etc/wsl.conf` values.

Microsoft documents two distinct configuration files:

- `%UserProfile%\.wslconfig` controls global WSL settings, mainly WSL 2 utility VM behavior.
- `/etc/wsl.conf` controls settings inside one distro.

The WSL Settings graphical app is analogous to `.wslconfig` and is intended for general settings that apply to all WSL 2 instances, such as hardware resource limits, networking, and custom kernels. Therefore, the app should link users to WSL Settings for global WSL 2 settings instead of rebuilding the same editor.

### Recommended UX

```text
Global WSL 2 settings
- Managed by: WSL Settings app
- Scope: all WSL 2 distributions
- Examples: memory, CPU, swap, networking mode, DNS tunneling, kernel, GUI applications
- Action: Open WSL Settings

Per-distro settings
- Managed by: this app
- Scope: selected distro only
- Examples: default user, systemd, boot command, automount, interop, hostname, resolv.conf generation
- Action: Edit /etc/wsl.conf safely
```

Global settings still matter to diagnostics, but they should not dominate the app. The UI should avoid recreating the official WSL Settings screens.

Recommended behavior for global WSL 2 settings:

- Show a small read-only summary only where relevant.
- Prefer status chips over full duplicate forms, for example `mirrored networking`, `memory limit set`, `custom kernel configured`, `WSLg enabled`.
- Provide a prominent `Open WSL Settings` button for editing.
- Provide an `Open .wslconfig` fallback only for advanced users.
- Warn that global changes affect the shared WSL 2 utility VM and all WSL 2 distros.
- Warn that many global changes require `wsl --shutdown`.
- Do not silently edit `.wslconfig` unless the user explicitly enables advanced mode.
- Do not build full global-setting forms for memory, CPU, swap, kernel, GUI, DNS tunneling, networking mode, or firewall unless the official WSL Settings app is unavailable and the user has enabled advanced mode.

Recommended behavior for per-distro settings:

- Own the `/etc/wsl.conf` editor.
- Validate known sections and keys.
- Preserve unknown keys, comments, whitespace, and ordering.
- Snapshot before editing.
- Show whether `wsl --terminate <distro>` or `wsl --shutdown` is needed.
- Show effective status after restart, for example systemd actually running, not just configured.

### Per-distro settings to expose

| Area | Settings | Scope |
|---|---|---|
| Boot | `systemd`, `command`, `protectBinfmt` | per distro |
| User | default user | per distro |
| Automount | `enabled`, `mountFsTab`, `root`, `options` | per distro |
| Network | `generateHosts`, `generateResolvConf`, `hostname` | per distro, with VM-level interactions |
| Interop | `enabled`, `appendWindowsPath` | per distro |
| GPU | `enabled` | per distro, depends on global WSL support |
| Time | `useWindowsTimezone` | per distro |

### Settings to redirect to WSL Settings

| Area | Examples | Scope |
|---|---|---|
| Resources | memory, processors, swap, swap file | global WSL 2 VM |
| Networking | networking mode, DNS tunneling, DNS proxy, firewall, auto proxy | global WSL 2 VM |
| VM lifecycle | VM idle timeout, safe mode | global WSL 2 VM |
| Kernel | custom kernel, kernel modules, kernel command line | global WSL 2 VM |
| GUI | GUI applications | global WSL 2 VM |
| VHD defaults | default VHD size, sparse VHD defaults | global WSL 2 VM or new distro behavior |

Useful UI behavior:

```text
This is a global WSL 2 setting.
Open WSL Settings to change it.
Impact: all WSL 2 distributions may be affected.
Restart impact: may require wsl --shutdown.
```

### Per-distro config schema and validation

There does not appear to be a public PowerShell command, `wsl.exe` command, JSON schema, registry schema, or documented API that returns a live schema for `.wslconfig` or `/etc/wsl.conf`. The app should therefore hardcode a known per-distro schema, keep it versioned, and treat unknown keys defensively.

Use this as the current built-in schema source policy:

```text
Primary source: MicrosoftDocs WSL wsl-config.md / Microsoft Learn
Secondary source: Ubuntu WSL instance configuration docs for scope and UX guidance
Observed-only sources: WSL logs, GitHub issues, and community references
Rule: only make official Microsoft-documented keys first-class editable UI controls
```

Important distinction:

- Officially documented keys get normal controls.
- Observed but not officially documented keys may be displayed in an advanced raw/unknown section, but should not become normal controls unless verified against the installed WSL build.
- Unknown keys must always be preserved.

### Hardcoded `/etc/wsl.conf` schema

Current official per-distro sections to support as first-class UI groups:

```text
[boot]
[automount]
[network]
[interop]
[user]
[gpu]
[time]
```

Although older Microsoft documentation text still says `wsl.conf` supports four sections, the current table also documents `boot`, `gpu`, and `time`. The app should use the actual current key tables, not the older summary sentence.

#### Schema table

| Key | Type | Default | UI control | Restart impact | Notes |
|---|---|---|---|---|---|
| `boot.systemd` | boolean | distro-dependent; Microsoft docs describe enabling with `true` | switch | terminate distro or shutdown WSL | Enables systemd. Verify by checking PID 1 and `/run/systemd/system`. |
| `boot.command` | string | unset | text field with shell-warning | terminate distro or shutdown WSL | Runs as root when the instance starts. Should be treated as high-risk. |
| `boot.protectBinfmt` | boolean | `true` | advanced switch | terminate distro or shutdown WSL | Prevents WSL from generating systemd units when systemd is enabled. |
| `automount.enabled` | boolean | `true` | switch | terminate distro or shutdown WSL | Controls automatic mounting of fixed Windows drives under the configured root. |
| `automount.mountFsTab` | boolean | `true` | switch | terminate distro or shutdown WSL | Controls whether `/etc/fstab` is processed on WSL start. |
| `automount.root` | absolute Linux path string | `/mnt/` | path field | terminate distro or shutdown WSL | Mount root for fixed Windows drives, for example `/mnt/` or `/`. |
| `automount.options` | DrvFs option list string | null / WSL default options | structured option editor plus raw mode | terminate distro or shutdown WSL | DrvFs options appended to automatic Windows drive mounts. |
| `network.generateHosts` | boolean | `true` | switch | terminate distro or shutdown WSL | Controls generation of `/etc/hosts`. |
| `network.generateResolvConf` | boolean | `true` | switch | terminate distro or shutdown WSL | Controls generation of `/etc/resolv.conf`. Interacts with global WSL DNS settings. |
| `network.hostname` | hostname string | Windows hostname | text field | terminate distro or shutdown WSL | Sets the distro hostname. Validate basic hostname format. |
| `interop.enabled` | boolean | `true` | switch | new shell or terminate distro | Controls launching Windows processes from Linux. |
| `interop.appendWindowsPath` | boolean | `true` | switch | new shell or terminate distro | Controls whether Windows PATH entries are appended to Linux `$PATH`. |
| `user.default` | Linux username string | initial user created on first run | user picker / text field | new WSL session | User must exist inside the distro. Verify with `id <user>`. |
| `gpu.enabled` | boolean | `true` | switch | terminate distro or shutdown WSL | Allows Linux apps to access the Windows GPU via paravirtualization. Depends on host GPU support. |
| `time.useWindowsTimezone` | boolean | `true` | switch | terminate distro or shutdown WSL | Controls whether WSL uses and syncs to the Windows timezone. |

#### DrvFs option schema for `automount.options`

Treat `automount.options` as a structured sub-editor, because a plain text box is easy to get wrong.

| Option | Type | Default | Values / validation | Notes |
|---|---|---|---|---|
| `metadata` | flag | disabled | present or absent | Enables Linux permission metadata on Windows files. |
| `uid` | integer | default distro user id, often `1000` | `0..4294967295` practical range | Owner ID for files on automounted Windows drives. |
| `gid` | integer | default distro group id, often `1000` | `0..4294967295` practical range | Group ID for files on automounted Windows drives. |
| `umask` | octal mask | `022` | octal digits, usually 3 or 4 digits | Permission mask for files and directories. |
| `fmask` | octal mask | `000` | octal digits, usually 3 or 4 digits | Permission mask for files. |
| `dmask` | octal mask | `000` | octal digits, usually 3 or 4 digits | Permission mask for directories. |
| `case` | enum | `off` | `off`, `dir`, `force` | Controls case sensitivity behavior for directories. |

UI behavior for `automount.options`:

```text
Default mode: structured editor for known DrvFs options
Advanced mode: raw comma-separated options string
Parser behavior: preserve unknown option tokens
Warning: options only apply to automatically mounted Windows drives, not the distro's ext4.vhdx filesystem
Warning: for per-drive custom options, use /etc/fstab instead
```

### Hardcoded schema object

This is a practical schema shape to hardcode in the application. Keep it as data, not scattered through UI code.

```json
{
  "schemaId": "wsl-conf-per-distro-2026-04-15",
  "file": "/etc/wsl.conf",
  "format": "ini",
  "scope": "per-distro",
  "sourcePolicy": {
    "firstClassControls": "official Microsoft-documented keys only",
    "unknownKeys": "preserve and show in advanced view",
    "globalSettings": "summarize and hand off to official WSL Settings"
  },
  "sections": {
    "boot": {
      "label": "Boot",
      "keys": {
        "systemd": {
          "type": "boolean",
          "default": null,
          "recommendedControl": "switch",
          "risk": "medium",
          "restart": "terminate-distro-or-shutdown-wsl",
          "verify": "test -d /run/systemd/system && ps -p 1 -o comm=",
          "description": "Enable systemd for this distro."
        },
        "command": {
          "type": "string",
          "default": null,
          "recommendedControl": "multiline-text-with-warning",
          "risk": "high",
          "restart": "terminate-distro-or-shutdown-wsl",
          "runsAs": "root",
          "description": "Command to run when the WSL instance starts."
        },
        "protectBinfmt": {
          "type": "boolean",
          "default": true,
          "recommendedControl": "advanced-switch",
          "risk": "medium",
          "restart": "terminate-distro-or-shutdown-wsl",
          "description": "Prevent WSL from generating systemd units when systemd is enabled."
        }
      }
    },
    "automount": {
      "label": "Windows drive automount",
      "keys": {
        "enabled": {
          "type": "boolean",
          "default": true,
          "recommendedControl": "switch",
          "risk": "medium",
          "restart": "terminate-distro-or-shutdown-wsl",
          "description": "Automatically mount fixed Windows drives."
        },
        "mountFsTab": {
          "type": "boolean",
          "default": true,
          "recommendedControl": "switch",
          "risk": "medium",
          "restart": "terminate-distro-or-shutdown-wsl",
          "description": "Process /etc/fstab on WSL startup."
        },
        "root": {
          "type": "linux-absolute-path",
          "default": "/mnt/",
          "recommendedControl": "path-textbox",
          "risk": "medium",
          "restart": "terminate-distro-or-shutdown-wsl",
          "description": "Directory where fixed Windows drives are mounted."
        },
        "options": {
          "type": "drvfs-options",
          "default": null,
          "recommendedControl": "structured-options-editor",
          "risk": "high",
          "restart": "terminate-distro-or-shutdown-wsl",
          "knownOptions": {
            "metadata": { "type": "flag", "default": false },
            "uid": { "type": "integer", "default": "default distro user id" },
            "gid": { "type": "integer", "default": "default distro group id" },
            "umask": { "type": "octal", "default": "022" },
            "fmask": { "type": "octal", "default": "000" },
            "dmask": { "type": "octal", "default": "000" },
            "case": { "type": "enum", "values": ["off", "dir", "force"], "default": "off" }
          },
          "description": "DrvFs options appended to automounted Windows drives."
        }
      }
    },
    "network": {
      "label": "Per-distro network files",
      "keys": {
        "generateHosts": {
          "type": "boolean",
          "default": true,
          "recommendedControl": "switch",
          "risk": "medium",
          "restart": "terminate-distro-or-shutdown-wsl",
          "description": "Generate /etc/hosts on startup."
        },
        "generateResolvConf": {
          "type": "boolean",
          "default": true,
          "recommendedControl": "switch",
          "risk": "high",
          "restart": "terminate-distro-or-shutdown-wsl",
          "description": "Generate /etc/resolv.conf on startup."
        },
        "hostname": {
          "type": "hostname",
          "default": "Windows hostname",
          "recommendedControl": "text",
          "risk": "low",
          "restart": "terminate-distro-or-shutdown-wsl",
          "description": "Hostname used by this WSL distro."
        }
      }
    },
    "interop": {
      "label": "Windows interoperability",
      "keys": {
        "enabled": {
          "type": "boolean",
          "default": true,
          "recommendedControl": "switch",
          "risk": "medium",
          "restart": "new-shell-or-terminate-distro",
          "verify": "cmd.exe /c ver or powershell.exe -NoProfile -Command $PSVersionTable.PSVersion",
          "description": "Allow launching Windows executables from Linux."
        },
        "appendWindowsPath": {
          "type": "boolean",
          "default": true,
          "recommendedControl": "switch",
          "risk": "medium",
          "restart": "new-shell-or-terminate-distro",
          "verify": "inspect PATH for Windows path entries",
          "description": "Append Windows PATH entries to Linux PATH."
        }
      }
    },
    "user": {
      "label": "Default user",
      "keys": {
        "default": {
          "type": "linux-username",
          "default": "initial username created on first run",
          "recommendedControl": "user-picker-with-text-fallback",
          "risk": "medium",
          "restart": "new-wsl-session",
          "verify": "id <username>",
          "description": "Default user when starting this distro."
        }
      }
    },
    "gpu": {
      "label": "GPU access",
      "keys": {
        "enabled": {
          "type": "boolean",
          "default": true,
          "recommendedControl": "switch-with-host-capability-status",
          "risk": "medium",
          "restart": "terminate-distro-or-shutdown-wsl",
          "verify": "test for WSL GPU libraries and devices where applicable",
          "description": "Allow Linux applications to access the Windows GPU via paravirtualization."
        }
      }
    },
    "time": {
      "label": "Time and timezone",
      "keys": {
        "useWindowsTimezone": {
          "type": "boolean",
          "default": true,
          "recommendedControl": "switch",
          "risk": "low",
          "restart": "terminate-distro-or-shutdown-wsl",
          "verify": "timedatectl or readlink /etc/localtime where available",
          "description": "Use and sync to the timezone configured in Windows."
        }
      }
    }
  }
}
```

### Observed but not first-class keys

Some keys appear in WSL logs, community references, or GitHub issues, but are not currently first-class in the Microsoft Learn `wsl.conf` table. Do not drop them. Do not expose them as normal controls by default.

| Key | Status in this app | Reason |
|---|---|---|
| `automount.cgroups` | preserve; optional advanced experimental display | Seen in community references and WSL-related issues, but not in the official Microsoft `wsl.conf` table used as primary source. |
| `automount.ldconfig` | preserve; optional advanced experimental display | Appears in WSL safe-mode log output references, but not first-class in the official table. |
| `gpu.appendLibPath` | preserve; optional advanced experimental display | Appears in WSL safe-mode log output and community references, but not first-class in the official table. |
| `fileServer.enabled` | preserve; optional advanced experimental display | Appears in safe-mode log references, but not documented as a normal `/etc/wsl.conf` UI setting. |
| `windowsterminal.enabled` | preserve if found, but probably belongs to distro packaging/config metadata rather than `/etc/wsl.conf` | Mentioned around validation issues, not a normal Microsoft-documented per-distro setting. |

Optional advanced behavior:

```text
If an observed key is present:
- show it under "Advanced / not in official schema"
- preserve it losslessly
- allow raw edit only
- show "not documented as a first-class wsl.conf setting by Microsoft"

If an observed key is absent:
- do not suggest adding it by default
- do not include it in generated templates
```

### Validation rules

Static validation:

| Type | Validation |
|---|---|
| boolean | accept only `true` or `false`; preserve original casing only in raw mode |
| Linux absolute path | must start with `/`; warn if path does not exist inside distro |
| hostname | non-empty, no whitespace, avoid invalid hostname characters |
| Linux username | must match Linux username pattern; verify with `getent passwd <user>` or `id <user>` |
| command | non-empty string; warn that it runs as root; warn about destructive commands |
| DrvFs options | parse comma-separated tokens; validate known tokens; preserve unknown tokens |
| octal mask | only octal digits; usually 3 or 4 digits |
| enum | must be one of known values |

Runtime validation commands:

```powershell
# Read current per-distro config
wsl.exe -d <Distro> -- cat /etc/wsl.conf

# Verify default user exists
wsl.exe -d <Distro> -u root -- sh -lc 'id <user>'

# Verify systemd after restart
wsl.exe -d <Distro> -- sh -lc 'test -d /run/systemd/system && ps -p 1 -o comm='

# Verify generated DNS file behavior
wsl.exe -d <Distro> -- sh -lc 'ls -l /etc/resolv.conf && cat /etc/resolv.conf'

# Verify Windows drive automounts
wsl.exe -d <Distro> -- sh -lc 'findmnt -t drvfs || mount | grep drvfs || true'

# Verify interop
wsl.exe -d <Distro> -- sh -lc 'command -v powershell.exe >/dev/null 2>&1; echo $?'
```

Restart handling:

```text
Most /etc/wsl.conf changes:
- preferred: wsl --terminate <Distro>
- stronger fallback: wsl --shutdown

Use wsl --shutdown only when:
- WSL global state must be refreshed
- the distro terminate path does not apply the change
- the user explicitly accepts that all WSL 2 distros will stop
```

### UI grouping for the per-distro settings page

Recommended tabs:

```text
Basics
- Default user
- Hostname
- Systemd

Windows integration
- Windows executable interop
- Append Windows PATH
- Windows drive automount
- Mount root
- DrvFs options

Network files
- Generate /etc/hosts
- Generate /etc/resolv.conf
- Link to global WSL Settings for DNS/networking mode

Boot and services
- Boot command
- protectBinfmt
- Link to systemd dashboard

GPU and time
- GPU enabled
- Use Windows timezone

Advanced raw config
- Full lossless /etc/wsl.conf editor
- Unknown keys
- Observed but not first-class keys
```

### Generated minimal template

The app should avoid generating a huge config file with every default value. Prefer minimal explicit settings:

```ini
[boot]
systemd=true

[user]
default=master
```

For advanced templates, include comments but keep defaults optional:

```ini
[automount]
enabled=true
root=/mnt/
mountFsTab=true
options=metadata,umask=022,fmask=011

[network]
generateHosts=true
generateResolvConf=true

[interop]
enabled=true
appendWindowsPath=true

[boot]
systemd=true
protectBinfmt=true

[gpu]
enabled=true

[time]
useWindowsTimezone=true
```

### Implementation notes

PowerShell and WSL probes can detect capabilities, but not a full schema:

```powershell
$wslVersionText = & wsl.exe --version 2>$null
$wslStatusText  = & wsl.exe --status 2>$null
$distros        = & wsl.exe --list --verbose 2>$null
$os             = Get-CimInstance Win32_OperatingSystem
$buildNumber    = [int]$os.BuildNumber
```

Per-distro probes:

```powershell
wsl.exe -d Ubuntu -- cat /etc/wsl.conf
wsl.exe -d Ubuntu -- sh -lc 'test -d /run/systemd/system && echo systemd-present || echo no-systemd'
wsl.exe -d Ubuntu -- sh -lc 'systemctl is-system-running 2>/dev/null || true'
wsl.exe -d Ubuntu -- sh -lc 'uname -r'
```

Schema metadata to store per key:

```json
{
  "boot.systemd": {
    "type": "boolean",
    "scope": "per-distro",
    "file": "/etc/wsl.conf",
    "section": "boot",
    "restart": "wsl --terminate <distro>",
    "verify": "test -d /run/systemd/system && systemctl is-system-running"
  },
  "interop.appendWindowsPath": {
    "type": "boolean",
    "scope": "per-distro",
    "file": "/etc/wsl.conf",
    "section": "interop",
    "restart": "new shell or distro restart",
    "verify": "inspect PATH"
  }
}
```

Important parser rule:

Never use a lossy serializer for `/etc/wsl.conf`. The editor must preserve unknown sections, unknown keys, comments, whitespace, and order. WSL can gain new settings before this app knows about them. Unknown values should be shown as advanced entries, not deleted.

Example warning:

```text
Unknown setting detected:
[boot]
someFutureSetting=true

This app does not know this setting yet. It will be preserved.
```

Runtime validation should be layered:

- static validation: section, key, type, enum, path, size, boolean
- capability validation: Windows build, WSL version, distro WSL version, systemd availability
- runtime validation: restart distro, re-read actual state, report whether the setting took effect

For global `.wslconfig` settings, do not use the same editor path by default. Show read-only diagnostics and link to WSL Settings.

### External settings handoff notes

The app should have a dedicated action for the official WSL Settings app. Implementation should be defensive because install source and registration can vary by WSL version and Windows installation.

Suggested launch behavior:

```text
Primary action: Open WSL Settings
Fallback 1: Search Start menu for WSL Settings
Fallback 2: Open .wslconfig in advanced mode
Fallback 3: Show Microsoft documentation link
```

Do not assume a stable undocumented executable path or URI unless verified on the target machine. Detect whether the app is registered locally and report a clear message if it is missing or broken.

The global settings page should be intentionally small. It should be a bridge and explainer, not a second global settings editor:

- what is global
- why it affects all WSL 2 distros
- whether a shutdown is required
- where to open the official settings app
- a compact summary of current values that are useful for diagnostics
- what this app intentionally does not edit by default

Recommended page shape:

```text
Global WSL 2 settings
- Managed by official WSL Settings
- Compact status: memory limit, networking mode, DNS mode, GUI support, custom kernel indicator
- Action: Open WSL Settings
- Advanced fallback: open .wslconfig read-only or edit with explicit confirmation
```

Avoid:

```text
A full clone of the WSL Settings app with every global setting rendered as editable controls.
```

### Difficulty

Low to medium.

## EXTRA2 - Architecture and runtime visibility

### Shared VM versus per-distro isolation inspector

Add an inspector that explains which resources are global to the WSL 2 utility VM and which are isolated per distribution.

Example view:

```text
Windows host
└── WSL managed utility VM
    ├── shared Linux kernel
    ├── shared CPU pool
    ├── shared memory pool
    ├── shared swap
    ├── shared network namespace
    ├── Ubuntu instance
    │   ├── own PID namespace
    │   ├── own mount namespace
    │   ├── own user namespace
    │   ├── own cgroup namespace
    │   ├── own root filesystem / VHD
    │   └── own init process
    └── Debian instance
        ├── own PID namespace
        ├── own mount namespace
        ├── own user namespace
        ├── own cgroup namespace
        ├── own root filesystem / VHD
        └── own init process
```

Useful UI distinction:

| Scope | Examples |
|---|---|
| Global WSL VM | kernel, memory, CPU limit, swap, networking mode, GUI support, VM idle timeout |
| Per distro | root filesystem, default user, systemd setting, boot command, automount config, interop config |

Why this matters:

- `.wslconfig` affects the shared WSL 2 utility VM.
- `/etc/wsl.conf` affects a single distro.
- `wsl --shutdown` affects all running WSL 2 distros because it shuts down the shared utility VM.
- `wsl --terminate <distro>` affects only one distro instance.
- networking issues can affect multiple distros because networking is mostly shared at VM level.

Possible feature name:

```text
WSL Architecture Inspector
```

### Live WSL runtime graph

Add a live topology view that shows how Windows-side and Linux-side WSL components relate to each other.

Show layers such as:

| Layer | Example data |
|---|---|
| Windows side | `wsl.exe`, `wslservice.exe`, `wslhost.exe`, WSL version |
| WSL utility VM | kernel version, CPU allocation, memory use, swap use, GPU availability |
| Distro side | init PID, default user, running commands, systemd state |
| Network side | NAT or mirrored mode, IP addresses, DNS status, localhost forwarding |
| Filesystem side | VHD path, VHD size, sparse status, mounted Windows drives |

Possible UI:

```text
Windows
└── wslservice.exe
    └── WSL utility VM
        ├── kernel: 6.x
        ├── memory: 3.8 GB / 8 GB
        ├── network: mirrored
        ├── Ubuntu: running
        └── Debian: stopped
```

Value:

- Helps users understand what is actually running.
- Helps distinguish VM problems from distro problems.
- Makes WSL failures easier to localize.

### Boot timeline and startup profiler

Add a boot profiler per distro.

Measure:

- VM startup time
- distro instance startup time
- init startup time
- systemd readiness time
- boot command duration
- shell readiness time
- DNS setup time
- Plan9 or mount setup time
- slow services

Example output:

```text
VM startup        1.2s
mini_init         0.3s
network/gns       0.8s
init/systemd      4.6s
shell ready       0.4s
```

Useful details:

- highlight if systemd is degraded
- show failed systemd units
- show last boot duration
- compare boot duration across distros
- identify whether WSL startup or distro startup is slow

### Difficulty

Medium to hard.

## EXTRA3 - Networking, ports, and reachability

### Networking doctor

Networking is one of the highest-value diagnostic areas.

Checks:

- NAT versus mirrored networking mode
- WSL IP address
- Windows host IP from WSL
- DNS resolution
- `localhost` from Windows to WSL
- `localhost` from WSL to Windows
- VPN interference
- proxy settings
- Hyper-V firewall state
- portproxy rules
- listening services inside WSL
- whether services bind to `127.0.0.1`, `0.0.0.0`, or a specific interface

Example output:

```text
Networking mode: NAT
Windows -> WSL localhost: OK
WSL -> Windows host: OK
DNS resolution: FAIL
VPN detected: yes
Hyper-V firewall: enabled
Port 3000 exposed: Windows localhost only
Suggested fix: enable DNS tunneling or mirrored mode
```

Possible wizard:

```text
Publish service
- Distro: Ubuntu
- Service: 8080
- Expose to: Windows only / LAN / VPN
- Method: NAT portproxy / mirrored mode / SSH tunnel
```

### Port and service map

Show all services running inside WSL and how reachable they are.

Example:

| Port | Process | Distro | Bind address | Windows localhost | LAN |
|---|---|---|---|---|---|
| 3000 | node | Ubuntu | 127.0.0.1 | yes | no |
| 5432 | postgres | Debian | 0.0.0.0 | yes | maybe |
| 22 | sshd | Ubuntu | 0.0.0.0 | no | blocked |

Features:

- map listening ports to processes
- detect web dev servers
- detect databases
- show Windows reachability
- show LAN reachability
- warn about unintended exposure
- create safe portproxy rules
- remove stale portproxy rules

### Difficulty

Medium to hard.

## EXTRA4 - Distro storage, VHD management, backup, and rollback

### Disk and VHD control center

This extra is about the storage backing each WSL 2 distro, especially the distro's `ext4.vhdx` virtual disk and safe lifecycle operations around it. It is not primarily about Windows drive mounts such as `/mnt/c`; those belong in `EXTRA6`.

Add a storage dashboard for each distro.

Show:

- VHD location
- VHD file size on Windows
- Linux filesystem used/free space
- maximum virtual size
- sparse VHD status, shown as read-only if managed globally by WSL Settings
- compaction opportunity
- backup size estimate
- ext4 health
- read-only fallback detection
- Docker overlay usage
- largest directories

Actions:

| Action | Purpose |
|---|---|
| Resize VHD | Avoid disk-full failures |
| Enable sparse VHD | Link to WSL Settings or use advanced mode if this is a global/default behavior |
| Compact VHD | Save disk after deleting files |
| Export backup | Make risky operations safer |
| Move distro | Help users with full C: drives |
| Repair guide | Handle read-only or mount failures |

Example output:

```text
Ubuntu
- VHD path: C:\Users\user\AppData\Local\Packages\...
- VHD size on host: 82 GB
- Used inside Linux: 51 GB
- Reclaimable estimate: 24 GB
- Sparse VHD: disabled
```

### Backup, clone, snapshot, and rollback workflow

Add safe lifecycle operations for distros.

Features:

- export distro
- import distro
- clone distro
- move distro to another drive
- named snapshots
- pre-change snapshots before config edits
- backup verification
- restore wizard
- compare two distros
- backup size estimator
- scheduled backup reminders

Important design rule:

Always tell the user what kind of backup is being created:

| Method | Meaning |
|---|---|
| tar export | file-level distro export |
| VHD export | virtual disk export |
| clone | imported duplicate distro |
| file backup | selected files only |

### Boundary with EXTRA6

Keep `EXTRA4` focused on distro-owned storage:

| Belongs in EXTRA4 | Belongs in EXTRA6 |
|---|---|
| `ext4.vhdx` location | `/mnt/c` and `/mnt/d` availability |
| host VHD file size | DrvFs mount options |
| Linux filesystem used/free space | `/etc/fstab` entries |
| max virtual disk size | Windows drive automount behavior |
| VHD resize/compact/repair | Plan9/DrvFs/virtiofs diagnostics |
| export/import/clone/restore | elevated versus non-elevated mount namespace differences |

Global or default VHD behavior that is already exposed in official WSL Settings should be summarized, not recreated as a full editable form. Per-distro VHD operations can remain first-class features because they are tied to a selected distro and are not simply global `.wslconfig` editing.

### Difficulty

Medium.

## EXTRA5 - Doctor report, error decoder, and event recorder

### WSL doctor report

Add a one-click health report.

Checks:

- WSL version
- WSL kernel version
- default distro
- running distros
- WSL 1 versus WSL 2
- systemd status
- DNS status
- interop status
- Windows PATH injection
- memory reclaim setting
- sparse VHD status
- VHD size
- DrvFs mount status
- WSLg status
- GPU availability
- failed systemd units
- suspicious config settings

Example:

```text
WSL version: 2.x
Kernel: OK
Default distro: Ubuntu
Systemd: enabled, degraded
DNS: failing
Interop: enabled
Windows PATH injection: enabled
Memory reclaim: disabled
Sparse VHD: disabled
VHD size: 82 GB, reclaimable: 24 GB
DrvFs metadata: disabled
GUI apps: enabled
GPU passthrough: available
```

Support bundle contents:

- `wsl --version`
- `wsl --status`
- `wsl -l -v`
- `.wslconfig`
- `/etc/wsl.conf`
- `systemctl --failed`
- `ip addr`
- `/etc/resolv.conf`
- mount table
- disk usage
- recent relevant logs, where available

### Error explanation panel

Add an error decoder for common WSL errors.

Input examples:

```text
Wsl/Service/CreateInstance/MountVhd/HCS/ERROR_SHARING_VIOLATION
```

Output format:

```text
Likely cause:
Docker Desktop or another process is holding the VHD.

Safe actions:
1. Close Docker Desktop
2. Run wsl --shutdown
3. Retry
```

Categories:

- HCS errors
- VHD mount errors
- DNS errors
- interop errors
- firewall errors
- systemd degraded state
- GUI display errors
- read-only filesystem fallback
- bad config keys
- distro registration errors

### WSL event recorder

Add a persistent timeline of WSL state changes.

Track:

- distro started
- distro stopped
- VM shut down
- IP changed
- DNS broke
- VHD grew
- memory spiked
- service failed
- port opened
- WSL version changed
- config changed
- backup created

Example timeline:

```text
09:12 Ubuntu started
09:13 systemd degraded: docker.service failed
09:14 port 8080 opened
09:28 VHD grew by 3.2 GB
09:31 DNS resolution failed after VPN connected
```

Value:

- helps debug intermittent issues
- helps correlate failures with VPN, Docker, updates, or config changes
- useful for support reports

### Difficulty

Low to hard, depending on whether this is a static report or persistent recorder.

## EXTRA6 - Mounts, interop, and Windows/Linux boundary

### Interop inspector

WSL interop is powerful but opaque. Add a panel that explains and tests Windows/Linux process interop.

Show:

- interop enabled/disabled
- Windows PATH appended or not
- `$WSL_INTEROP` value
- active interop socket
- ability to launch `notepad.exe`
- ability to launch `powershell.exe`
- ability to launch `explorer.exe`
- ability to launch `code.exe`
- stale interop socket detection
- security warning for Windows PATH injection

Actions:

- open current Linux directory in Explorer
- open current Linux directory in VS Code
- launch Windows Terminal profile
- toggle interop config per distro
- toggle Windows PATH injection per distro

### Mount namespace and DrvFs explorer

This extra is about how a distro sees Windows resources and other mounted filesystems. It complements `EXTRA4`, which is about the distro's own VHD-backed Linux filesystem.

Add a mount explorer for Windows drive mounts and Linux mount namespaces.

Show:

- mounted Windows drives
- mount options
- `/mnt/c` availability
- `/etc/fstab` entries
- DrvFs metadata setting
- elevated versus non-elevated mount differences
- Plan9/DrvFs/virtiofs style, where detectable

Useful diagnostics:

- `/mnt/c` missing
- Windows drive not mounted
- metadata disabled when user expects Linux permissions
- slow project under `/mnt/c`
- mismatched elevated/non-elevated mounts
- invalid fstab entry

Actions:

- mount/unmount Windows drives
- edit per-distro automount settings in `/etc/wsl.conf`
- edit fstab safely
- explain permission behavior
- recommend moving heavy Linux workloads into the ext4 filesystem

### Security posture panel

Add a WSL security checklist.

Checks:

- interop enabled
- Windows PATH injection enabled
- Windows drives mounted
- default user is root
- SSH listening
- LAN exposure
- Hyper-V firewall state
- `.wslconfig` custom kernel path
- sensitive Windows folders mounted
- systemd services listening on all interfaces
- world-writable files in sensitive paths

Example output:

```text
Security posture: medium risk
- Interop is enabled
- Windows PATH is injected into Linux PATH
- SSH listens on 0.0.0.0
- PostgreSQL is reachable from Windows localhost
- Windows C: drive is mounted at /mnt/c
```

Important: phrase this carefully. These are not automatically vulnerabilities. They are exposure and boundary-crossing indicators.

### Difficulty

Medium.

## EXTRA7 - Systemd and developer services

### Systemd service dashboard

Add a systemd dashboard per distro.

Show:

- systemd enabled/disabled
- PID 1 status
- system state: running, degraded, initializing
- failed units
- enabled services
- running services
- user services
- service logs
- boot duration

Actions:

- start service
- stop service
- restart service
- enable service at boot
- disable service at boot
- inspect logs through `journalctl`
- explain degraded systemd state

Useful for:

- Docker
- SSH
- PostgreSQL
- MySQL
- Redis
- Kubernetes tools
- background dev services

### Kubernetes and container mode

Add a developer cluster view for Docker, Kubernetes, `kind`, `k3d`, and similar tools.

Checks:

- Docker Desktop WSL integration
- Docker socket availability
- Docker service status inside WSL
- Kubernetes contexts
- kind clusters
- k3d clusters
- minikube profiles
- exposed container ports
- Docker overlay disk usage
- memory pressure
- VHD growth from container images
- cgroup configuration
- warning when memory reclaim settings may conflict with container workloads

Useful views:

```text
Containers
- Docker socket: OK
- Current kube context: kind-dev
- Clusters: kind-dev, k3d-test
- Docker disk usage: 38 GB
- Exposed ports: 8080, 5432
```

### Difficulty

Medium to hard.

## EXTRA8 - GUI apps, USB, and hardware-adjacent workflows

### WSLg and Linux GUI app launcher

Add a Linux GUI application launcher.

Features:

- detect installed `.desktop` files
- group apps by distro
- show WSLg availability
- show Wayland environment
- show X11 environment
- check GPU/vGPU support
- create Windows shortcuts
- pin shortcuts where possible
- diagnose missing display socket
- diagnose GUI app launch failure

Example view:

```text
Ubuntu GUI apps
- Firefox
- Gedit
- Meld
- xeyes

WSLg: available
Wayland socket: OK
X11 socket: OK
GPU acceleration: available
```

### USB device manager

Add USB passthrough management through `usbipd`, where available.

Features:

- list USB devices
- bind device
- unbind device
- attach device to distro
- detach device
- remember preferred distro per device
- auto-attach known devices
- show required admin actions
- show missing kernel module warnings
- show missing `usbipd` warning

Useful for:

- embedded development
- serial devices
- microcontrollers
- USB storage tests
- hardware labs

### Difficulty

Medium.

## EXTRA9 - Resource profiles and global state explanation

### Resource profile recommendations

Do not implement a full global resource editor if the official WSL Settings app already covers it. Instead, show recommendations, explain the impact, and hand off to WSL Settings for changes unless advanced mode is explicitly enabled.

Settings to summarize or reason about:

- memory limit
- CPU count
- swap size
- swap file path
- automatic memory reclaim
- sparse VHD
- VM idle timeout
- nested virtualization
- GPU support

Profiles:

| Profile | Example purpose |
|---|---|
| Low memory laptop | Reduce memory pressure |
| Docker/Kubernetes | More memory, more CPUs, suitable swap |
| GPU/ML | GPU checks, higher memory |
| Fast startup | Avoid heavyweight boot services |
| Battery saver | Lower CPU/memory, shorter idle timeout |
| Safe recovery | Conservative settings |

Example:

```text
Profile recommendation: Laptop battery
Suggested global settings to review in WSL Settings:
- memory limit
- CPU count
- swap size
- auto memory reclaim
- VM idle timeout
```

Recommended behavior:

- Show profile recommendations, not a full duplicate editor.
- Explain which settings are global.
- Link to WSL Settings to apply global settings.
- Optionally generate a `.wslconfig` snippet in advanced mode, without silently applying it.
- Keep per-distro performance hints inside this app, for example slow projects under `/mnt/c`, heavy services, Docker disk usage, and failed systemd units.

### Difficulty

Low to medium.

## EXTRA10 - Teaching mode and architecture explorer

Add an educational layer that explains WSL internals.

Topics:

- What happens when a distro starts?
- What is the WSL utility VM?
- Why does `wsl --shutdown` affect all WSL 2 distros?
- Why does each distro have its own init process?
- What is `wslservice.exe`?
- What is `wslhost.exe`?
- What is Plan9 used for?
- What does interop mean?
- What is the difference between `.wslconfig` and `/etc/wsl.conf`?
- Why does a service work from Windows localhost but not from another LAN machine?

This is especially useful if the app is also aimed at students, developers, or system administrators.

### Difficulty

Low.

## Suggested implementation priority

### First priority

| Priority | Feature | Reason |
|---|---|---|
| 1 | EXTRA1 - Settings handoff plus per-distro config editor | Avoids duplicating official global settings, keeps ownership of `/etc/wsl.conf` |
| 2 | EXTRA3 - Networking doctor | Frequent pain point |
| 3 | EXTRA4 - Distro storage/VHD control center | Concrete value, prevents painful failures |
| 4 | EXTRA5 - WSL doctor report | Good support/debugging feature |
| 5 | EXTRA7 - Systemd dashboard | Very useful for developer workloads |
| 6 | EXTRA2 - Shared VM versus per-distro inspector | Distinctive and educational |

### Second priority

| Priority | Feature | Reason |
|---|---|---|
| 7 | EXTRA3 - Port and service map | Excellent for web/dev users |
| 8 | EXTRA4 - Backup/clone/rollback | Valuable, but must be safe |
| 9 | EXTRA9 - Resource profiles | Helps laptop, Docker, and Kubernetes users |
| 10 | EXTRA8 - WSLg app launcher | Differentiates from command-line wrappers |
| 11 | EXTRA8 - USB manager | Useful, but depends on `usbipd` integration |

### Advanced priority

| Feature | Why later |
|---|---|
| EXTRA2 - Boot profiler | Harder to measure accurately |
| EXTRA5 - Event recorder | Requires persistent monitoring |
| EXTRA6 - Security posture panel | Needs careful wording and accurate checks |
| EXTRA7 - Kubernetes/container mode | Valuable but broader scope |
| EXTRA10 - Architecture explorer | Useful polish, not core functionality |

## Concrete MVP

A strong next version could include the following modules.

```text
Dashboard
- WSL version, kernel, default distro
- Running distros
- VM memory usage
- Disk usage per distro
- Networking mode
- WSLg/GPU/systemd status
- Shared VM versus distro isolation summary

Doctor
- DNS test
- localhost test
- interop test
- systemd test
- VHD health
- config validation
- suspicious settings

Distro page
- Start/terminate/default
- Open shell
- Open home in Explorer
- systemd services
- listening ports
- VHD size
- backup/export

Settings page
- Open WSL Settings for global WSL 2 settings
- Show read-only global setting summary where detectable
- Edit /etc/wsl.conf per distro
- Validate before saving
- Preserve unknown keys and comments
- Show restart impact
- Explain global versus per-distro settings

Distro storage page
- Locate VHD
- Resize
- Show sparse status with WSL Settings handoff when global/default
- Compact
- Backup
- Repair guide

Network page
- NAT/mirrored status
- IP addresses
- DNS
- port proxy
- firewall hints
- service reachability
```

## Source notes

Current source assumptions to keep verified:

- Microsoft WSL configuration documentation: https://learn.microsoft.com/en-us/windows/wsl/wsl-config
- Microsoft WSL basic commands documentation: https://learn.microsoft.com/en-us/windows/wsl/basic-commands
- Ubuntu WSL instance configuration reference, including WSL Settings description: https://documentation.ubuntu.com/wsl/latest/reference/instance_configuration/
- WSL technical documentation: https://wsl.dev/technical-documentation/

These links should be periodically rechecked when maintaining the schema and UI text.
