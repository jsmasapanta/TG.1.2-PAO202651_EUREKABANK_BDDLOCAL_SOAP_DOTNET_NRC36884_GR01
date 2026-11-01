# ===================================================================
#  Publica el servidor SOAP WSEureka en IIS (puerto 8087) para que
#  los demás integrantes (web, escritorio, móvil) se conecten por WiFi.
#
#  EJECUTAR COMO ADMINISTRADOR:
#    clic derecho en el menú Inicio > "Terminal (Administrador)" y luego:
#    powershell -ExecutionPolicy Bypass -File ".\publicar_iis.ps1"
#
#  Antes: compile el servidor en Visual Studio (Compilar > Compilar solución)
#  y CIERRE la ventana de Visual Studio del servidor (IIS Express usa el 8087).
#  La base EUREKABANK debe existir (03. BDD\crear_bdd.ps1): el servicio entra
#  a SQL Server con el login monster/monster de Web.config, no con el usuario de IIS.
# ===================================================================

$ErrorActionPreference = 'Stop'
$Puerto   = 8087
$Sitio    = 'EUREKA'
$Destino  = 'C:\inetpub\eureka'
$Proyecto = Join-Path $PSScriptRoot 'EUREKA_SOAP_DOTNET_SERVIDOR\EUREKA_SOAP_DOTNET_SERVIDOR'

function Paso($texto) { Write-Host "`n>> $texto" -ForegroundColor Cyan }

$admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $admin) { throw 'Abra la terminal COMO ADMINISTRADOR y vuelva a ejecutar el script.' }

if (-not (Test-Path (Join-Path $Proyecto 'bin\EUREKA_SOAP_DOTNET_SERVIDOR.dll'))) {
    throw "No se encontró bin\EUREKA_SOAP_DOTNET_SERVIDOR.dll. Compile primero el servidor en Visual Studio."
}

# 1. Características de Windows: IIS + ASP.NET 4.8 + activación HTTP de WCF (.svc)
Paso '1/5  Activando IIS, ASP.NET 4.8 y WCF (Activación HTTP)...'
$caracteristicas = 'IIS-WebServerRole','IIS-WebServer','IIS-ManagementConsole','IIS-ASPNET45',
                   'IIS-NetFxExtensibility45','IIS-ISAPIExtensions','IIS-ISAPIFilter',
                   'NetFx4Extended-ASPNET45','WCF-Services45','WCF-HTTP-Activation45'
foreach ($c in $caracteristicas) {
    $estado = (Get-WindowsOptionalFeature -Online -FeatureName $c).State
    if ($estado -ne 'Enabled') {
        Write-Host "   activando $c"
        Enable-WindowsOptionalFeature -Online -FeatureName $c -All -NoRestart | Out-Null
    }
}

# 2. Copiar solo lo necesario para ejecutar: .svc (WCF), .asmx, Web.config, el panel y bin\
Paso "2/5  Copiando el servidor a $Destino..."
New-Item -ItemType Directory -Force -Path $Destino, "$Destino\bin" | Out-Null
Copy-Item (Join-Path $Proyecto 'WSEureka.svc') $Destino -Force
Copy-Item (Join-Path $Proyecto 'WSEureka.asmx') $Destino -Force
Copy-Item (Join-Path $Proyecto 'Web.config') $Destino -Force
# Panel de monitoreo en la raíz del sitio (http://IP:8087/)
foreach ($f in 'index.html','panel.js','panel.css','monster.jpg') { Copy-Item (Join-Path $Proyecto $f) $Destino -Force }
Copy-Item (Join-Path $Proyecto 'bin\*') "$Destino\bin" -Recurse -Force

# 3. Grupo de aplicaciones (.NET CLR v4.0) y sitio en el puerto 8087
Paso "3/5  Creando el sitio '$Sitio' en el puerto $Puerto..."
Import-Module WebAdministration
if (-not (Test-Path "IIS:\AppPools\$Sitio")) {
    New-WebAppPool -Name $Sitio | Out-Null
}
Set-ItemProperty "IIS:\AppPools\$Sitio" -Name managedRuntimeVersion -Value 'v4.0'
if (Test-Path "IIS:\Sites\$Sitio") { Remove-Website -Name $Sitio }
New-Website -Name $Sitio -Port $Puerto -PhysicalPath $Destino -ApplicationPool $Sitio | Out-Null
icacls $Destino /grant "IIS AppPool\$($Sitio):(OI)(CI)RX" /T /Q | Out-Null
Start-Website -Name $Sitio

# 4. Firewall: permitir conexiones entrantes al puerto 8087 (redes públicas y privadas)
Paso "4/5  Abriendo el puerto $Puerto en el firewall..."
if (-not (Get-NetFirewallRule -DisplayName "EUREKA SOAP $Puerto" -ErrorAction SilentlyContinue)) {
    New-NetFirewallRule -DisplayName "EUREKA SOAP $Puerto" -Direction Inbound -Protocol TCP `
        -LocalPort $Puerto -Action Allow -Profile Any | Out-Null
}

# 5. Comprobación
Paso '5/5  Comprobando...'
$ips = Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -notmatch '^(127|169)\.' -and $_.InterfaceAlias -match 'Wi-?Fi|Ethernet' }
try {
    $r = Invoke-WebRequest "http://localhost:$Puerto/WSEureka.svc?wsdl" -UseBasicParsing -TimeoutSec 30
    Write-Host "   WSDL OK (HTTP $($r.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   No respondió: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nListo. Los demás integrantes usan una de estas URL (la del adaptador Wi-Fi):" -ForegroundColor Green
foreach ($ip in $ips) { Write-Host ("   {0,-12} http://{1}:{2}/WSEureka.svc   (panel: http://{1}:{2}/ · ASMX: /WSEureka.asmx)" -f $ip.InterfaceAlias, $ip.IPAddress, $Puerto) }
