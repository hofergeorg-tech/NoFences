# winget package (prepared, not submitted)

These manifests let people install NoFences with:

```
winget install hofergeorg-tech.NoFences
```

They are submitted as a pull request to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs).
Do that **after** the exe is code-signed (SignPath): unsigned exes regularly fail Microsoft's validation
or trigger SmartScreen for every user.

## For each release

1. Copy the three files into `manifests/h/hofergeorg-tech/NoFences/<version>/` in a fork of winget-pkgs.
2. Replace `{VERSION}` and `{SHA256}`:
   ```powershell
   (Get-FileHash NoFences.exe -Algorithm SHA256).Hash
   ```
3. Validate and test locally:
   ```powershell
   winget validate --manifest <folder>
   winget install --manifest <folder>
   ```
4. Open the pull request. Later versions can use `wingetcreate update hofergeorg-tech.NoFences --version <v> --urls <url> --submit`.
