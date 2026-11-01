# 04. SERVIDOR — Servicio SOAP EurekaBank (WCF + SQL Server)

.NET Framework 4.7.2, IIS Express en `http://localhost:8087/WSEureka.svc`.

```
EUREKA_SOAP_DOTNET_SERVIDOR/
├── ec/edu/monster/db/ConexionBD.cs          SINGLETON: lee "EurekaBank" de Web.config y abre conexiones
├── ec/edu/monster/modelo/Modelos.cs         Empleado, Cuenta, Movimiento, ErrorServicio (DataContract)
├── ec/edu/monster/servicio/EurekaServicio.cs SINGLETON: login con 3 intentos y bloqueo de 2 min, validaciones, consultas y transacciones (depósito, retiro, transferencia)
├── ec/edu/monster/servicio/GestorSesiones.cs SINGLETON: token → vencimiento (30 min), limpieza automática, ContarActivos()
├── ec/edu/monster/servicio/MonitorServidor.cs SINGLETON: peticiones, éxitos, conteo por cliente y operación, últimos 60 eventos (en memoria)
├── ec/edu/monster/ws/ContextoPeticion.cs     lee el header HTTP X-Cliente y el header SOAP <Token>
├── ec/edu/monster/ws/IWSEureka.cs           contrato SOAP (namespace http://espe.edu.ec/eureka/)
├── ec/edu/monster/ws/WSEureka.svc.cs        implementación WCF: delega y convierte errores en SOAP Fault
├── ec/edu/monster/ws/WSEurekaAsmx.asmx.cs    las mismas operaciones como servicio ASMX (página con formularios de prueba)
├── index.html · panel.js · panel.css · monster.jpg   panel de monitoreo en la raíz del sitio
├── WSEureka.svc
├── WSEureka.asmx
└── Web.config                               cadena de conexión (login monster/monster) + endpoint
```

Requisito: la base `EUREKABANK` creada con `03. BDD\crear_bdd.ps1`.

## Operaciones

| Operación | Token en `<soap:Header>` |
|---|---|
| `login`, `obtenerEstado` | No |
| `cerrarSesion(token)` | Va como parámetro |
| `consultarCuenta`, `traerMovimientos`, `registrarDeposito`, `registrarRetiro`, `registrarTransferencia` | Sí |

Sin token o vencido: Fault **"Token inválido o expirado. Inicie sesión."** Cada petición se anota en el monitor con el cliente del header `X-Cliente`.

## Publicar en IIS (red local)

1. Compilar en Visual Studio (como administrador) → clic derecho en el proyecto → **Publicar** → **Carpeta** `C:\inetpub\eureka` (perfil `FolderProfile.pubxml`).
2. Administrador de IIS: grupo de aplicaciones **EUREKA** (.NET CLR v4.0, Integrada) y sitio **EUREKA** → `C:\inetpub\eureka`, puerto **8087**.
   O todo de una vez, como administrador: `powershell -ExecutionPolicy Bypass -File .\publicar_iis.ps1`.
3. `ipconfig` → tomar la **Dirección IPv4** del adaptador **Wi-Fi** (por ejemplo `192.168.100.2`).
4. Firewall, como administrador: `04. CLIWEB\Habilitar-Firewall-RedLocal.ps1` (TCP 8087 y 5087, solo `LocalSubnet`, perfil Privado). La red Wi-Fi debe estar como **Privada**.
5. **Verificar con el panel:** abrir `http://IP:8087/` desde otro equipo o el celular → debe decir **Servicio activo**; al usar cualquier cliente aparece en *Peticiones en tiempo real* con su etiqueta (WEB, MOVIL, ESCRITORIO, CONSOLA).

Emulador de Android: el servidor de la PC se ve como `http://10.0.2.2:8087/WSEureka.svc`. Celular por cable: `adb reverse tcp:8087 tcp:8087` y `http://localhost:8087/WSEureka.svc`.
