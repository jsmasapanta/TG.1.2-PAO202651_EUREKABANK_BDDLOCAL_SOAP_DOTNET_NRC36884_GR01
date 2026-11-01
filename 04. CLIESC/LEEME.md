# 04. CLIESC — Cliente escritorio (Windows Forms)

.NET Framework 4.7.2. MVC en `ec/edu/monster`:
- `vista/` FormLogin (con la imagen de Monsters Inc.), FormPrincipal (menú lateral), PanelMovimientos, PanelOperacion, Formato, Tema.
- `controlador/ControladorEureka.cs` solo pasa las llamadas (asíncronas) y guarda el empleado de la sesión.
- `servicio/IWSEureka.cs` (contrato) y `servicio/ClienteEureka.cs` SINGLETON con `ChannelFactory<IWSEureka>`.
- `modelo/` Empleado, Cuenta, Movimiento, ErrorServicio (solo get/set) y TipoOperacion. Sin reglas del banco: las valida el servidor.
- `Recursos/monster.jpg` (recurso incrustado).

Servidor en `App.config` (`SoapHost`, `SoapPuerto` = 8087, `SoapRuta`) o variables `SOAP_HOST` / `SOAP_PORT`, igual que la consola. Envía `X-Cliente: ESCRITORIO` y el token en el header SOAP; "Cerrar sesión" llama a `cerrarSesion`.
