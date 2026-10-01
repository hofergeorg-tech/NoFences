# Builds a self-contained single-file NoFences.exe into .\dist (no .NET install needed to run it).
$ErrorActionPreference = 'Stop'
$dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
if (-not $dotnet) { $dotnet = "C:\Program Files\dotnet\dotnet.exe" }

& $dotnet publish "$PSScriptRoot\NoFences\NoFences.csproj" -c Release -o "$PSScriptRoot\dist"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$exe = Get-Item "$PSScriptRoot\dist\NoFences.exe"
"{0} ({1:N1} MB)" -f $exe.FullName, ($exe.Length / 1MB)
