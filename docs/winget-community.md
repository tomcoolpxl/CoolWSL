# Community Winget Packaging (winget-pkgs)

This repository publishes installer-first GitHub Releases with these assets:

- `CoolWSL-<version>-win-x64.msi`
- `CoolWSL-<version>-win-x64.zip`
- `CoolWSL-<version>-win-x64.checksums.txt`

Use the MSI asset for the community Winget package.

## One-time setup

1. Fork `microsoft/winget-pkgs`.
2. Clone your fork locally.
3. Ensure `winget` is available on your machine.

Optional: if you enable GitHub Pages for this repo, you can later point `PublisherUrl` and `PackageUrl` to the site. It is not required for winget-pkgs submission.

## Generate manifests from a CoolWSL release

For tagged releases (`vX.Y.Z`), CI now also generates and uploads a `winget-manifests-<tag>` artifact automatically from the release checksums file.

You can still generate manifests locally when preparing or iterating before submission:

From the CoolWSL repo root:

```powershell
pwsh -NoProfile -File .\build\Export-WingetManifest.ps1 `
  -Version 1.0.4 `
  -ChecksumsFile .\artifacts\release-1.0.4-buildoutput\CoolWSL-1.0.4-win-x64.checksums.txt
```

This generates files under:

```text
artifacts/winget/manifests/t/tomcoolpxl/CoolWSL/1.0.4/
```

Expected files:

- `tomcoolpxl.CoolWSL.yaml`
- `tomcoolpxl.CoolWSL.installer.yaml`
- `tomcoolpxl.CoolWSL.locale.en-US.yaml`

## Submit to winget-pkgs

1. Copy the generated version folder into your fork of `winget-pkgs` under:
   `manifests/t/tomcoolpxl/CoolWSL/<version>/`
2. Validate:

  ```powershell
  winget validate --manifest .\manifests\t\tomcoolpxl\CoolWSL\1.0.4
  ```

1. Commit and push your branch.
2. Open a pull request to `microsoft/winget-pkgs`.

## Recommended release checklist before opening the PR

1. Confirm the GitHub Release has all three files (`.msi`, `.zip`, `.checksums.txt`).
2. Confirm the MSI filename matches `CoolWSL-<version>-win-x64.msi`.
3. Confirm the checksums file contains the MSI hash entry.
4. Test install from URL locally after merge (or from your branch with a local manifest):

  ```powershell
  winget install --manifest .\manifests\t\tomcoolpxl\CoolWSL\1.0.4
  ```

## Notes

- Community package publication happens in `microsoft/winget-pkgs`, not in this repository.
- The script uses the release checksums file so the Winget `InstallerSha256` matches release artifacts exactly.
