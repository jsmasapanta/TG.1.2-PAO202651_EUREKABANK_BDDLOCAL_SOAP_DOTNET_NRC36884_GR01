# ===================================================================
#  Abre en el firewall de Windows los puertos del proyecto, SOLO para
#  la red local (LocalSubnet) y el perfil Privado:
#    TCP 8087  servidor SOAP (IIS)
#    TCP 5087  cliente web
#
#  EJECUTAR COMO ADMINISTRADOR:
#    powershell -ExecutionPolicy Bypass -File ".\Habilitar-Firewall-RedLocal.ps1"
#
#  La red Wi-Fi debe estar como "Privada" (Configuración > Red e Internet >
#  Wi-Fi > propiedades). En una red "Pública" estas reglas no se aplican.
# ===================================================================

$ErrorActionPreference = 'Stop'

$admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $admin) { throw 'Abra la terminal COMO ADMINISTRADOR y vuelva a ejecutar el script.' }

$reglas = @(
    @{ Nombre = 'EUREKA RedLocal SOAP 8087'; Puerto = 8087 },
    @{ Nombre = 'EUREKA RedLocal WEB 5087';  Puerto = 5087 }
)

foreach ($r in $reglas) {
    Get-NetFirewallRule -DisplayName $r.Nombre -ErrorAction SilentlyContinue | Remove-NetFirewallRule
    New-NetFirewallRule -DisplayName $r.Nombre -Direction Inbound -Protocol TCP -LocalPort $r.Puerto `
        -Action Allow -Profile Private -RemoteAddress LocalSubnet | Out-Null
    Write-Host ("Regla creada: {0} (TCP {1}, LocalSubnet, Privado)" -f $r.Nombre, $r.Puerto) -ForegroundColor Green
}

$perfiles = Get-NetConnectionProfile | Select-Object InterfaceAlias, NetworkCategory
Write-Host "`nPerfil de las redes conectadas:" -ForegroundColor Cyan
$perfiles | ForEach-Object { Write-Host ("   {0,-14} {1}" -f $_.InterfaceAlias, $_.NetworkCategory) }
if ($perfiles | Where-Object { $_.NetworkCategory -eq 'Public' }) {
    Write-Host "`nHay una red 'Public': cámbiela a Privada para que otros equipos se conecten." -ForegroundColor Yellow
}
