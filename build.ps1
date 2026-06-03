#Requires -Version 5.1
<#
.SYNOPSIS
    One-command full build for Grex (Windows only).
.DESCRIPTION
    Does everything: restore -> build (Release|x64) -> test -> publish the
    self-contained win-x64 GUI and CLI -> package versioned zips, and (if Inno
    Setup is installed) a setup.exe with a built-in uninstaller, into .\dist\.

    WinUI 3 only builds on Windows. No parameters - just run:  .\build.ps1
#>

$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'   # keeps Compress-Archive fast
Set-Location -LiteralPath $PSScriptRoot       # the script lives at the repo root

# WinUI 3 cannot build off-Windows - fail early with a clear message.
if ($PSVersionTable.PSVersion.Major -ge 6 -and -not $IsWindows) {
    throw 'Grex is a WinUI 3 app and can only be built on Windows.'
}

$Platform = 'x64'
$Config   = 'Release'
$Rid      = 'win-x64'

function Step([string] $Name) { Write-Host "`n==> $Name" -ForegroundColor Cyan }
function Confirm-Exit([string] $What) {
    if ($LASTEXITCODE -ne 0) { throw "$What failed (exit code $LASTEXITCODE)." }
}

# Version comes from the single source of truth, Directory.Build.props.
$match   = Select-String -LiteralPath 'Directory.Build.props' -Pattern '<Version>\s*([\d.]+)\s*</Version>' |
           Select-Object -First 1
$Version = if ($match) { $match.Matches[0].Groups[1].Value } else { '0.0.0' }
Write-Host "Grex build  v$Version  ($Config | $Platform | $Rid)" -ForegroundColor White

Step 'Restore'
dotnet restore grex.sln
Confirm-Exit 'Restore'

Step 'Build'
dotnet build grex.sln -p:Platform=$Platform -c $Config --no-restore
Confirm-Exit 'Build'

Step 'Test'
dotnet test grex.sln -p:Platform=$Platform -c $Config --no-restore
$testsPassed = ($LASTEXITCODE -eq 0)
if (-not $testsPassed) {
    Write-Warning "Tests failed (exit $LASTEXITCODE) - continuing so artifacts are still produced."
}

$dist   = Join-Path $PSScriptRoot 'dist'
$guiDir = Join-Path $dist "grex-$Version-$Rid"
$cliDir = Join-Path $dist "grex-cli-$Version-$Rid"

Step "Publish GUI  ->  $guiDir"
dotnet publish Grex.csproj -p:Platform=$Platform -c $Config -r $Rid --self-contained -o $guiDir
Confirm-Exit 'GUI publish'

Step "Publish CLI  ->  $cliDir"
dotnet publish Grex.Cli/Grex.Cli.csproj -p:Platform=$Platform -c $Config -r $Rid --self-contained -o $cliDir
Confirm-Exit 'CLI publish'

Step 'Package'
$guiZip = Join-Path $dist "grex-$Version-$Rid.zip"
$cliZip = Join-Path $dist "grex-cli-$Version-$Rid.zip"
Remove-Item -LiteralPath $guiZip, $cliZip -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $guiDir '*') -DestinationPath $guiZip
Compress-Archive -Path (Join-Path $cliDir '*') -DestinationPath $cliZip

# Build a setup.exe (with a built-in uninstaller) via Inno Setup, if its compiler is present.
Step 'Installer (setup.exe)'
$iscc = (Get-Command 'iscc.exe' -ErrorAction SilentlyContinue).Source
if (-not $iscc) {
    # Find ISCC.exe under any "Inno Setup <n>" folder (6, 7, ...), newest first.
    foreach ($base in @(${env:ProgramFiles(x86)}, ${env:ProgramFiles})) {
        if (-not $base) { continue }
        $hit = Get-ChildItem -LiteralPath $base -Directory -Filter 'Inno Setup *' -ErrorAction SilentlyContinue |
               ForEach-Object { Join-Path $_.FullName 'ISCC.exe' } |
               Where-Object { Test-Path -LiteralPath $_ } |
               Sort-Object -Descending | Select-Object -First 1
        if ($hit) { $iscc = $hit; break }
    }
}
$setupExe = Join-Path $dist "grex-$Version-setup.exe"
if ($iscc) {
    & $iscc "/DAppVersion=$Version" "/DSourceDir=$guiDir" "/DCliDir=$cliDir" "/DOutputDir=$dist" (Join-Path $PSScriptRoot 'Grex.iss')
    Confirm-Exit 'Inno Setup compile'
}
else {
    $setupExe = $null
    Write-Warning 'Inno Setup compiler (ISCC.exe) not found - skipping setup.exe. Get it from https://jrsoftware.org/isdl.php'
}

Step 'Done'
$artifacts = @($guiZip, $cliZip)
if ($setupExe -and (Test-Path -LiteralPath $setupExe)) { $artifacts += $setupExe }
foreach ($artifact in $artifacts) {
    $file = Get-Item -LiteralPath $artifact
    Write-Host ('    {0}  ({1:N1} MB)' -f $file.FullName, ($file.Length / 1MB)) -ForegroundColor Green
}
if (-not $testsPassed) {
    Write-Warning 'Artifacts were produced, but TESTS FAILED above - review before shipping.'
    exit 1
}
