# Fills in the winget manifests for a published release and validates them.
#   .\prepare.ps1 2.4.0
# Output: packaging\winget\out\<version>\ (not committed). Submitting is a separate, manual step (see README.md).
param([Parameter(Mandatory)][string]$Version)
$ErrorActionPreference = 'Stop'

$url = "https://github.com/hofergeorg-tech/NoFences/releases/download/v$Version/NoFences.exe"
$out = Join-Path $PSScriptRoot "out\$Version"
New-Item -ItemType Directory -Force $out | Out-Null

$exe = Join-Path $out "NoFences.exe"
Write-Host "Downloading $url"
Invoke-WebRequest $url -OutFile $exe -UseBasicParsing
$sha = (Get-FileHash $exe -Algorithm SHA256).Hash
Remove-Item $exe

foreach ($template in Get-ChildItem $PSScriptRoot -Filter "hofergeorg-tech.NoFences*.yaml") {
    $text = (Get-Content $template.FullName -Raw).Replace("{VERSION}", $Version).Replace("{SHA256}", $sha)
    Set-Content (Join-Path $out $template.Name) $text -Encoding utf8 -NoNewline
}
Write-Host "SHA256: $sha"
Write-Host "Manifests: $out"

if (Get-Command winget -ErrorAction SilentlyContinue) {
    winget validate --manifest $out
} else {
    Write-Host "winget not found - skipping validation"
}
