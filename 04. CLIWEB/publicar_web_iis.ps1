# ===================================================================
#  Publica el CLIENTE WEB (ASP.NET Core .NET 8) en IIS, puerto 5087,
#  para que los demás equipos lo abran en http://IP-DE-LA-PC:5087
#  sin tener que dejar Visual Studio o "dotnet run" abiertos.
#
#  EJECUTAR COMO ADMINISTRADOR:
#    powershell -ExecutionPolicy Bypass -File ".\publicar_web_iis.ps1"
#
#  Requisitos: IIS (ya activado por 04. SERVIDOR\publicar_iis.ps1) y el
#  "ASP.NET Core Hosting Bundle" de .NET 8 (módulo AspNetCoreModuleV2).
#  El cliente web busca al servidor SOAP en ServidorSoap de appsettings.json
#  (por defecto 192.168.100.2:8087); también se cambia desde su login.
# ===================================================================

$ErrorActionPreference = 'Stop'
$Puerto   = 5087
$Sitio    = 'EUREKA_WEB'
$Destino  = 'C:\inetpub\eureka_web'
$Proyecto = Join-Path $PSScriptRoot 'EUREKA_SOAP_DOTNET_CLIWEB\EUREKA_SOAP_DOTNET_CLIWEB\EUREKA_SOAP_DOTNET_CLIWEB.csproj'
$Temporal = Join-Path $env:TEMP 'eureka_web_publicado'

function Paso($texto) { Write-Host "`n>> $texto" -ForegroundColor Cyan }

$admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $admin) { throw 'Abra la terminal COMO ADMINISTRADOR y vuelva a ejecutar el script.' }
if (-not (Test-Path 'C:\Program Files\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll')) {
    throw 'Falta el "ASP.NET Core Hosting Bundle" de .NET 8 (https://dotnet.microsoft.com/download/dotnet/8.0). Instálelo y vuelva a ejecutar.'
}

# 1. Compilar y publicar en una carpeta temporal
Paso "1/5  Publicando el cliente web (Release)..."
if (Test-Path $Temporal) { Remove-Item $Temporal -Recurse -Force }
dotnet publish $Proyecto -c Release -o $Temporal --nologo
if ($LASTEXITCODE -ne 0) { throw 'No se pudo publicar el cliente web.' }

# 2. Detener el sitio (si existe) y copiar. Si ya había un appsettings.json en IIS se conserva,
#    porque ahí queda guardado el servidor que se eligió desde el login.
Paso "2/5  Copiando a $Destino..."
Import-Module WebAdministration
if (Test-Path "IIS:\Sites\$Sitio") { Stop-Website -Name $Sitio -ErrorAction SilentlyContinue }
if (Test-Path "IIS:\AppPools\$Sitio") { Stop-WebAppPool -Name $Sitio -ErrorAction SilentlyContinue; Start-Sleep -Seconds 2 }
New-Item -ItemType Directory -Force -Path $Destino | Out-Null
$excluir = @()
if (Test-Path (Join-Path $Destino 'appsettings.json')) { $excluir = @('/XF', 'appsettings.json') }
robocopy $Temporal $Destino /E /NFL /NDL /NJH /NJS /NP @excluir | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy falló (código $LASTEXITCODE)." }

# 3. Grupo de aplicaciones SIN código administrado (ASP.NET Core corre su propio proceso) y sitio en el 5087
Paso "3/5  Creando el sitio '$Sitio' en el puerto $Puerto..."
if (-not (Test-Path "IIS:\AppPools\$Sitio")) { New-WebAppPool -Name $Sitio | Out-Null }
Set-ItemProperty "IIS:\AppPools\$Sitio" -Name managedRuntimeVersion -Value ''
if (Test-Path "IIS:\Sites\$Sitio") { Remove-Website -Name $Sitio }
New-Website -Name $Sitio -Port $Puerto -PhysicalPath $Destino -ApplicationPool $Sitio | Out-Null
icacls $Destino /grant "IIS AppPool\$($Sitio):(OI)(CI)RX" /T /Q | Out-Null
# Permiso de escritura solo sobre appsettings.json: "Cambiar servidor" del login lo guarda ahí.
icacls (Join-Path $Destino 'appsettings.json') /grant "IIS AppPool\$($Sitio):M" /Q | Out-Null
Start-WebAppPool -Name $Sitio
Start-Website -Name $Sitio

# 4. Firewall: puerto 5087 en cualquier perfil (la Wi-Fi puede estar como red Pública), solo red local
Paso "4/5  Abriendo el puerto $Puerto en el firewall..."
Get-NetFirewallRule -DisplayName "EUREKA WEB $Puerto" -ErrorAction SilentlyContinue | Remove-NetFirewallRule
New-NetFirewallRule -DisplayName "EUREKA WEB $Puerto" -Direction Inbound -Protocol TCP `
    -LocalPort $Puerto -Action Allow -Profile Any -RemoteAddress LocalSubnet | Out-Null

# 5. Comprobación
Paso '5/5  Comprobando...'
try {
    $r = Invoke-WebRequest "http://localhost:$Puerto/Acceso/Login" -UseBasicParsing -TimeoutSec 60
    Write-Host "   Cliente web OK (HTTP $($r.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   No respondió: $($_.Exception.Message)" -ForegroundColor Red
}

$ips = Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -notmatch '^(127|169)\.' -and $_.InterfaceAlias -match 'Wi-?Fi|Ethernet' }
Write-Host "`nListo. Desde otros equipos de la red:" -ForegroundColor Green
foreach ($ip in $ips) { Write-Host ("   {0,-12} http://{1}:{2}" -f $ip.InterfaceAlias, $ip.IPAddress, $Puerto) }
