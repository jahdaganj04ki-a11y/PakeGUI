<#
.SYNOPSIS
    Baut die Pake GUI als einzelne Windows-EXE.

.DESCRIPTION
    Benoetigt das .NET 10 SDK (oder neuer). Ergebnis:
    dist\win-x64\PakeGUIWin.exe  (self-contained, keine Installation noetig)

.BEISPIELE
    .\build.ps1              # Release-Build nach dist\win-x64
    .\build.ps1 -RunTests    # vorher die Tests ausfuehren
#>
[CmdletBinding()]
param(
    [switch]$RunTests,
    [string]$Output = "dist/win-x64"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

Push-Location $root
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw "dotnet wurde nicht gefunden. Bitte das .NET 10 SDK installieren: https://dotnet.microsoft.com/download"
    }

    if ($RunTests) {
        Write-Host ">> Tests" -ForegroundColor Cyan
        dotnet test tests/PakeGui.Core.Tests/PakeGui.Core.Tests.csproj -c Release
        if ($LASTEXITCODE -ne 0) { throw "Tests fehlgeschlagen." }
    }

    Write-Host ">> Publish nach $Output" -ForegroundColor Cyan
    dotnet publish src/PakeGuiWin/PakeGuiWin.csproj -c Release -r win-x64 -o $Output
    if ($LASTEXITCODE -ne 0) { throw "Publish fehlgeschlagen." }

    $exe = Join-Path $Output "PakeGUIWin.exe"
    $size = [math]::Round((Get-Item $exe).Length / 1MB, 1)
    Write-Host ""
    Write-Host "Fertig: $exe ($size MB)" -ForegroundColor Green
}
finally {
    Pop-Location
}
