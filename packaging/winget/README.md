# winget package (prepared, not submitted)

These manifests let people install NoFences with:

```
winget install hofergeorg-tech.NoFences
```

They are submitted as a pull request to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs).
winget accepts unsigned apps (Microsoft scans them when the PR is opened); signing can come later.

## For each release

`.\prepare.ps1 <version>` downloads the release exe, fills in version and SHA256 and validates the
manifests into `out\<version>\`. Then:

1. Copy the three files from `out\<version>\` into `manifests/h/hofergeorg-tech/NoFences/<version>/` in a fork of winget-pkgs.
2. Test the install locally:
   ```powershell
   winget install --manifest <folder>
   ```
3. Open the pull request. Later versions can use `wingetcreate update hofergeorg-tech.NoFences --version <v> --urls <url> --submit`.
