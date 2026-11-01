# 04. CLICON — Cliente consola

.NET Framework 4.7.2. MVC en `ec/edu/monster`: `modelo/` (Empleado, Cuenta, Movimiento, ErrorServicio: solo get/set; TipoOperacion), `vista/VistaConsola.cs` (pantallas y formato), `controlador/ControladorConsola.cs` (solo pasa las llamadas), `servicio/IWSEureka.cs` (contrato) y `servicio/ClienteEureka.cs` (SINGLETON, `ChannelFactory<IWSEureka>`). Sin reglas del banco: las valida el servidor.

Login `monster`/`monster69` (los 3 intentos los controla el servidor) → menú: consultar cuenta y movimientos, depósito, retiro, transferencia.
## Servidor SOAP

`App.config` → `appSettings`:
```xml
<add key="SoapHost" value="192.168.100.2" />
<add key="SoapPuerto" value="8087" />
<add key="SoapRuta" value="WSEureka.svc" />
```
Sin recompilar se puede usar las variables de entorno (tienen prioridad):
```powershell
$env:SOAP_HOST = "192.168.100.2"; $env:SOAP_PORT = "8087"; .\EUREKA_SOAP_DOTNET_CLICON.exe
```
**Desde la pantalla:** en el login escriba **S** en "Usuario" → IP y puerto (se guardan en el `.exe.config`).

`SOAP_HOST` es la IP de la PC del servidor (`ipconfig` → IPv4 del Wi-Fi). Si el host o el puerto no son válidos se muestra el motivo. Si no llega: *"No se pudo conectar a {url}. Verifica IP, puerto y red."* — revisar que el panel `http://IP:8087/` diga **Servicio activo** y que el firewall tenga abierto el 8087 (`04. CLIWEB\Habilitar-Firewall-RedLocal.ps1`). Publicación en IIS: `04. SERVIDOR/LEEME.md`.

Envía `X-Cliente: CONSOLA` y el token en el header SOAP; **[0] Cerrar sesión** llama a `cerrarSesion`. La consola nunca imprime el token.
