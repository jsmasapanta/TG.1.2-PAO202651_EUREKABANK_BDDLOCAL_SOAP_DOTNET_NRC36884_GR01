# 04. CLIWEB — Cliente web (ASP.NET Core MVC .NET 8)

`http://IP-DE-LA-PC:5087` (en la misma PC también `http://localhost:5087`). Todo dentro de `ec/edu/monster`:
- `controlador/` AccesoController (login/logout; los intentos los cuenta el servidor), BancoController (movimientos y operaciones), RequiereSesionAttribute.
- `vista/` Acceso/Login, Banco/Movimientos, Banco/Operacion, Shared/_Layout (Program.cs indica a Razor que busque aquí).
- `servicio/ClienteEureka.cs` registrado con `AddSingleton` (una sola ChannelFactory).
- `servicio/IWSEureka.cs` contrato (incluye `cerrarSesion`).
- `modelo/` Empleado, Cuenta, Movimiento, ErrorServicio (solo get/set), TipoOperacion y ViewModels.cs. Sin reglas del banco: las valida el servidor.
- `wwwroot/img/monster.jpg` en el login y el menú.

## Servidor SOAP

`appsettings.json`:
```json
"ServidorSoap": { "Host": "192.168.100.2", "Puerto": 8087, "Ruta": "WSEureka.svc" }
```
`Host` = IP de la PC con el servidor en IIS (`ipconfig` en esa PC). **Desde la pantalla de login**: **⚙ Servidor · cambiar** → IP y puerto → **Guardar** (valida y lo escribe en `appsettings.json`). Si `Host` o `Puerto` no son válidos, la pantalla de login lo dice. Cada petición envía `X-Cliente: WEB` y el token de la sesión del usuario en el header SOAP; "Cerrar sesión" llama a `cerrarSesion`.

## Red local

| Script | Qué hace |
|---|---|
| `Iniciar-RedLocal.ps1` | Inicia la web en `0.0.0.0:5087` y muestra las URL con las IP de la PC |
| `Detener-RedLocal.ps1` | Detiene la web iniciada por el script anterior |
| `Habilitar-Firewall-RedLocal.ps1` | **Como administrador**: abre TCP 8087 (SOAP) y 5087 (web) solo para `LocalSubnet` y perfil Privado |

```powershell
powershell -ExecutionPolicy Bypass -File .\Habilitar-Firewall-RedLocal.ps1   # una vez, como administrador
powershell -ExecutionPolicy Bypass -File .\Iniciar-RedLocal.ps1
```
Desde el celular o otra laptop: `http://IP-DE-LA-PC:5087`. Para revisar que el servidor responde: `http://IP-DEL-SERVIDOR:8087/` (panel, **Servicio activo**). Publicación del servidor en IIS: ver `04. SERVIDOR/LEEME.md`.
