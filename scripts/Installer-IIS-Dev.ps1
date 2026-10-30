#Requires -RunAsAdministrator
<#
  Crée l'application IIS de développement http://localhost/qr pointant sur src\QrGenerator.Web.
  À lancer une seule fois, dans un PowerShell administrateur. Peut être relancé sans risque.
#>
$ErrorActionPreference = 'Stop'
$appcmd = "$env:windir\System32\inetsrv\appcmd.exe"
$pool   = 'QrGenerator'
$site   = (Resolve-Path "$PSScriptRoot\..\src\QrGenerator.Web").Path

# appcmd « list » est peu fiable pour tester l'existence (erreurs sur stdout, application parente renvoyée) :
# on liste tout et on cherche le nom exact entre guillemets.
function Existe([string] $type, [string] $nom) { [bool]((& $appcmd list $type) -like "*`"$nom`"*") }

# Pool d'applications .NET 4, pipeline intégré.
if (-not (Existe 'apppool' $pool)) {
    & $appcmd add apppool /name:$pool /managedRuntimeVersion:v4.0 /managedPipelineMode:Integrated
}

# Application /qr sous le site par défaut.
if (-not (Existe 'app' 'Default Web Site/qr')) {
    & $appcmd add app /site.name:"Default Web Site" /path:/qr /physicalPath:"$site" /applicationPool:$pool
}
& $appcmd list app /site.name:"Default Web Site"

# Autorise le web.config du site à déclarer son handler.
& $appcmd unlock config -section:system.webServer/handlers

# Lecture du dossier du site pour l'identité du pool.
icacls "$site" /grant "IIS AppPool\${pool}:(OI)(CI)RX" | Out-Null

Write-Host "OK : http://localhost/qr/v1/create-qr-code/?size=150x150&data=Example -> $site"
