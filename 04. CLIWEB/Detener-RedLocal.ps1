# ===================================================================
#  Detiene el cliente web que inició Iniciar-RedLocal.ps1.
#    powershell -ExecutionPolicy Bypass -File ".\Detener-RedLocal.ps1"
# ===================================================================

$Archivo = Join-Path $PSScriptRoot '.redlocal.pid'

if (-not (Test-Path $Archivo)) {
    Write-Host 'No hay un cliente web iniciado con Iniciar-RedLocal.ps1.' -ForegroundColor Yellow
    return
}

$id = Get-Content $Archivo
$proceso = Get-Process -Id $id -ErrorAction SilentlyContinue
if ($proceso) {
    # dotnet run deja un proceso hijo (EUREKA_SOAP_DOTNET_CLIWEB.exe): se cierra también.
    Get-CimInstance Win32_Process -Filter "ParentProcessId = $id" | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
    Stop-Process -Id $id -Force
    Write-Host "Cliente web detenido (proceso $id)." -ForegroundColor Green
} else {
    Write-Host "El proceso $id ya no estaba corriendo." -ForegroundColor Yellow
}
Remove-Item $Archivo -Force
