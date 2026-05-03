# CoolWSL

CoolWSL is a desktop control center for Windows Subsystem for Linux on Windows 11. It gives you a cleaner, safer way to inspect distro state, run diagnostics, and manage common WSL tasks without memorizing command flags.

## Features

* Distro dashboard with status, defaults, and quick actions.
* Per-distro configuration support so you can manage settings for each distro independently.
* Diagnostics and logs views for troubleshooting WSL environments.
* Settings and health-focused UX designed for safe, explicit operations.

## Download

Get the latest builds from GitHub Releases:

[https://github.com/tomcoolpxl/CoolWSL/releases](https://github.com/tomcoolpxl/CoolWSL/releases)

## Local Build (Brief)

Prerequisites:

* Windows 11
* .NET 10 SDK

Build and test:

```powershell
dotnet restore .\CoolWSL.sln
dotnet build .\CoolWSL.sln -c Debug
dotnet test .\CoolWSL.Tests\CoolWSL.Tests.csproj -c Debug
```

Run the app locally:

```powershell
dotnet run --project .\CoolWSL.App\CoolWSL.App.csproj -c Debug
```

Create local installer artifacts (MSI and ZIP):

```powershell
pwsh -NoProfile -File .\build\Invoke-ReleaseInstaller.ps1 -Version 1.0.0 -OutputDirectory artifacts\release-local
```
