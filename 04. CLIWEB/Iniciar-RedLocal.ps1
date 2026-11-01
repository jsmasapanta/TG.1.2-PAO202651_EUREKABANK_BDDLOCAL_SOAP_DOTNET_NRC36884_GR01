# ===================================================================
#  Inicia el cliente web en 0.0.0.0:5087 para abrirlo desde otros
#  equipos de la misma red (celular, otra laptop) y muestra las URL.
#
#    powershell -ExecutionPolicy Bypass -File ".\Iniciar-RedLocal.ps1"
#
#  El servidor SOAP se configura en appsettings.json (ServidorSoap:Host,
#  Puerto 8087, Ruta). Para que lo vean otros equipos abra antes los
#  puertos con Habilitar-Firewall-RedLocal.ps1 (como administrador).
#  Para detenerlo: Detener-RedLocal.ps1
# ===================================================================

$ErrorActionPreference = 'Stop'
$Puerto   = 5087
$Proyecto = Join-Path $PSScriptRoot 'EUREKA_SOAP_DOTNET_CLIWEB\EUREKA_SOAP_DOTNET_CLIWEB'
$Archivo  = Join-Path $PSScriptRoot '.redlocal.pid'

if (Test-Path $Archivo) {
    $anterior = Get-Process -Id (Get-Content $Archivo) -ErrorAction SilentlyContinue
    if ($anterior) { throw "El cliente web ya está corriendo (proceso $($anterior.Id)). Use Detener-RedLocal.ps1." }
}

$ocupado = Get-NetTCPConnection -LocalPort $Puerto -State Listen -ErrorAction SilentlyContinue
if ($ocupado) { throw "El puerto $Puerto ya está en uso (proceso $($ocupado[0].OwningProcess)). Cierre Visual Studio o la otra instancia." }

Write-Host "Compilando e iniciando el cliente web en 0.0.0.0:$Puerto ..." -ForegroundColor Cyan
$proceso = Start-Process -FilePath 'dotnet' -PassThru -WindowStyle Minimized -WorkingDirectory $Proyecto `
    -ArgumentList 'run', '--no-launch-profile', '--urls', "http://0.0.0.0:$Puerto"
Set-Content -Path $Archivo -Value $proceso.Id

# Espera a que Kestrel responda (la primera vez compila, puede tardar).
$listo = $false
foreach ($i in 1..60) {
    Start-Sleep -Seconds 2
    try { Invoke-WebRequest "http://localhost:$Puerto/" -UseBasicParsing -TimeoutSec 3 | Out-Null; $listo = $true; break } catch { }
    if ($proceso.HasExited) { break }
}
if (-not $listo) { throw "El cliente web no respondió. Revise la ventana minimizada de dotnet." }

$ips = Get-NetIPAddress -AddressFamily IPv4 |
       Where-Object { $_.IPAddress -notmatch '^(127|169)\.' -and $_.InterfaceAlias -match 'Wi-?Fi|Ethernet' }
Write-Host "`nCliente web activo (proceso $($proceso.Id)). Desde otros equipos de la red:" -ForegroundColor Green
foreach ($ip in $ips) { Write-Host ("   {0,-14} http://{1}:{2}" -f $ip.InterfaceAlias, $ip.IPAddress, $Puerto) }
Write-Host "   En esta PC     http://localhost:$Puerto"
Write-Host "`nPanel del servidor SOAP: http://<IP-del-servidor>:8087/  (debe decir 'Servicio activo')" -ForegroundColor DarkGray
