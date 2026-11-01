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
- **Desde la pantalla:** login → **⚙ Servidor SOAP (IIS)** → IP y puerto → **Guardar** (queda guardado en el teléfono; **Restablecer** vuelve al de `appsettings.json`). Así no hace falta recompilar cuando cambia la red.
- Pantallas: bienvenida, login con caritas de intentos y estado del servidor, menú con saludo y accesos de colores, cuenta como tarjeta con gráficos, comprobante (Compartir) y modo oscuro (🌙 en el menú).
- Emulador: `http://10.0.2.2:8087/WSEureka.svc`.

Envía `X-Cliente: MOVIL`, arma el sobre con `SecurityElement.Escape` y el token en `<s:Header>`; "Salir" llama a `cerrarSesion`. Verificación: en el panel `http://IP:8087/` las peticiones aparecen con la etiqueta MOVIL.
`Directory.Build.props` envía `obj` a `%LOCALAPPDATA%\EUREKA_CLIMOV\obj` por el límite de 260 caracteres.
