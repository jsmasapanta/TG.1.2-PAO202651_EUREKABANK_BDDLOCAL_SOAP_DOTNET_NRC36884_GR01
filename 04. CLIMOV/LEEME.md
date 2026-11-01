# 04. CLIMOV — Cliente móvil (.NET MAUI Android, sucesor de Xamarin.Forms)

```
ec/edu/monster/modelo/                        Empleado, Cuenta, Movimiento (solo get/set) y TipoOperacion
ec/edu/monster/servicio/ClienteSoap.cs        sobre SOAP con HttpClient; llena los modelos · SINGLETON
ec/edu/monster/controlador/ControladorEureka.cs  solo pasa las llamadas y guarda la sesión · SINGLETON
ec/edu/monster/vista/Formato.cs               dinero, títulos y colores
ec/edu/monster/vista/LoginPage                login (mensajes e intentos del servidor) + URL + imagen
ec/edu/monster/vista/MenuPage                 4 opciones + operaciones de la sesión
ec/edu/monster/vista/MovimientosPage          saldo y movimientos de una cuenta
ec/edu/monster/vista/OperacionPage            depósito, retiro o transferencia
```

Servidor: `appsettings.json` del proyecto, incrustado como `EmbeddedResource` (al cambiar la IP hay que **recompilar e instalar**):
```json
"ServidorSoap": { "Host": "192.168.100.2", "Puerto": 8087, "Ruta": "WSEureka.svc" }
```
- Celular por WiFi: `Host` = IP de la PC del servidor (`ipconfig`), misma red y firewall abierto (`04. CLIWEB\Habilitar-Firewall-RedLocal.ps1`).
- Celular por cable: `adb reverse tcp:8087 tcp:8087` y en la pantalla de login cambiar la URL a `http://localhost:8087/WSEureka.svc` (solo para esa ejecución).
- Emulador: `http://10.0.2.2:8087/WSEureka.svc`.

Envía `X-Cliente: MOVIL`, arma el sobre con `SecurityElement.Escape` y el token en `<s:Header>`; "Salir" llama a `cerrarSesion`. Verificación: en el panel `http://IP:8087/` las peticiones aparecen con la etiqueta MOVIL.
`Directory.Build.props` envía `obj` a `%LOCALAPPDATA%\EUREKA_CLIMOV\obj` por el límite de 260 caracteres.
