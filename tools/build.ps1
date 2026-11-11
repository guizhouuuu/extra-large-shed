param(
    [Parameter(Mandatory = $true)][string]$GamePath,
    [string]$DotnetPath = 'dotnet',
    [switch]$RunSmokeTests
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
& $DotnetPath build (Join-Path $projectRoot 'ExtraLargeShed.csproj') -c Release "-p:GamePath=$GamePath" '-p:EnableModDeploy=false'
if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
if ($RunSmokeTests) {
    & $DotnetPath build (Join-Path $projectRoot 'tests\SmokeTests\SmokeTests.csproj') -c Release "-p:GamePath=$GamePath" '-p:EnableModDeploy=false' '-p:EnableModZip=false'
    if ($LASTEXITCODE -ne 0) { throw 'Smoke-test build failed.' }
    & $DotnetPath (Join-Path $projectRoot 'tests\SmokeTests\bin\Release\net6.0\SmokeTests.dll') $GamePath (Join-Path $projectRoot 'bin\Release\net6.0')
    if ($LASTEXITCODE -ne 0) { throw 'Smoke tests failed. A .NET 6 runtime is required to execute them.' }
}
Write-Output 'Build complete. Game files and saves were not changed.'
