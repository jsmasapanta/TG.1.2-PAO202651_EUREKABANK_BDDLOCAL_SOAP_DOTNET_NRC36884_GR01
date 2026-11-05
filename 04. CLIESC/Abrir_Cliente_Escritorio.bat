@echo off
rem ===================================================================
rem  Abre el cliente de escritorio EurekaBank apuntando al servidor SOAP en IIS.
rem  Uso: doble clic (pregunta la IP) o desde cmd:  Abrir_Cliente_Escritorio.bat 10.40.65.203 8087
rem  Si el .exe no existe todavia, lo compila con MSBuild de Visual Studio.
rem ===================================================================
setlocal
cd /d "%~dp0"
set "EXE=%~dp0EUREKA_SOAP_DOTNET_CLIESC\EUREKA_SOAP_DOTNET_CLIESC\bin\Debug\EUREKA_SOAP_DOTNET_CLIESC.exe"
set "PROYECTO=%~dp0EUREKA_SOAP_DOTNET_CLIESC\EUREKA_SOAP_DOTNET_CLIESC\EUREKA_SOAP_DOTNET_CLIESC.csproj"

set "IP=%~1"
set "PUERTO=%~2"
if "%IP%"=="" set /p "IP=IP de la PC del servidor (ejemplo 10.40.65.203): "
if "%PUERTO%"=="" set "PUERTO=8087"
if "%IP%"=="" (
    echo No escribio la IP del servidor.
    pause
    exit /b 1
)

if not exist "%EXE%" (
    echo Compilando el cliente de escritorio...
    set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
    call :compilar
    if not exist "%EXE%" (
        echo No se pudo compilar. Abra la solucion en Visual Studio y compile con Ctrl+Shift+B.
        pause
        exit /b 1
    )
)

echo Probando http://%IP%:%PUERTO%/WSEureka.svc ...
powershell -NoProfile -Command "try { $r = Invoke-WebRequest -UseBasicParsing -TimeoutSec 5 'http://%IP%:%PUERTO%/WSEureka.svc?wsdl'; Write-Host 'Servidor activo' -ForegroundColor Green } catch { Write-Host 'Sin respuesta del servidor: revise IP, puerto, firewall y que esten en la misma red Wi-Fi.' -ForegroundColor Red }"

rem El cliente lee SOAP_HOST y SOAP_PORT antes que el App.config.
set "SOAP_HOST=%IP%"
set "SOAP_PORT=%PUERTO%"
start "" "%EXE%"
exit /b 0

:compilar
for /f "usebackq delims=" %%i in (`"%VSWHERE%" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do set "MSBUILD=%%i"
if not defined MSBUILD (
    echo No se encontro MSBuild de Visual Studio.
    exit /b 1
)
"%MSBUILD%" "%PROYECTO%" -p:Configuration=Debug -v:m -nologo
exit /b 0
