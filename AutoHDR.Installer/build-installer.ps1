Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $root

try {
    Write-Host "Budowanie AutoHDR (Release net48)..."
    dotnet build ..\AutoHDR\AutoHDR.csproj -c Release

    Write-Host "Budowanie instalatora MSI..."
    wix build Package.wxs -arch x64 -o AutoHDR-1.0.msi -ext WixToolset.UI.wixext

    Write-Host "Gotowe: $root\AutoHDR-1.0.msi"
}
finally {
    Pop-Location
}
