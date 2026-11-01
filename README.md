# TG.1.2 · EurekaBank — SOAP .NET con base de datos (SQL Server)

**TG.1.2 PAO202651_EUREKABANK_BDDLOCAL_SOAP_DOTNET_NRC36884_GR01** · ESPE · Arquitectura de Software · NRC 36884 · Grupo 1 · Tutor: Ing. Mauricio Campaña
Repositorio: https://github.com/jsmasapanta/TG.1.2-PAO202651_EUREKABANK_BDDLOCAL_SOAP_DOTNET_NRC36884_GR01

Proyecto de **Arquitectura de Software (ESPE)**: un **servicio web SOAP** en .NET (WCF) que maneja las cuentas de ahorro de **EurekaBank** sobre **SQL Server**, y **cuatro clientes** que lo consumen: **consola, escritorio, web y móvil**.

Operaciones: **consultar cuenta y movimientos, depósito, retiro y transferencia**. Cada operación queda grabada en la tabla `Movimiento` y actualiza el saldo de `Cuenta` dentro de una transacción.

| | |
|---|---|
| **Usuario de la app** | `monster` / `monster69` (tabla `Empleado`, código `0012`) |
| **Login de SQL Server** | `monster` / `monster` (lo usa el servidor para conectarse) |
| **Base de datos** | `EUREKABANK` en SQL Server local (`localhost`) |
| **Panel del servidor** | `http://IP-DE-LA-PC:8087/` en IIS (estado, sesiones, peticiones por cliente y prueba rápida) |
| **Servidor SOAP** | `http://IP-DE-LA-PC:8087/WSEureka.svc` en IIS (WSDL: `?wsdl`) · por defecto `192.168.100.2` |
| **Página ASMX** | `http://IP-DE-LA-PC:8087/WSEureka.asmx` (las mismas 8 operaciones) |
| **Cliente web** | `http://IP-DE-LA-PC:5087` |
| **IDE** | Visual Studio 2022 Community |
| **Paquetes** | `ec.edu.monster.db` · `modelo` · `servicio` · `ws` (servidor) y `modelo` · `vista` · `controlador` · `servicio` (clientes) |
| **Sesión** | Token de 30 minutos en el header SOAP `<Token>`; `cerrarSesion` lo invalida |
| **Patrones** | Singleton (`ConexionBD`, `EurekaServicio`, `GestorSesiones`, `MonitorServidor`, `ClienteEureka`) · MVC en los clientes |

---

## 1. Arquitectura

```
                 X-Cliente          ┌──────────────────────────────────────────┐
  CLICON (consola)   CONSOLA ────►  │ IIS / IIS Express :8087                  │
  CLIESC (escritorio) ESCRITORIO ►  │  /            panel (index.html) PANEL   │
  CLIWEB  :5087      WEB ────────►  │  WSEureka.svc (WCF · SOAP 1.1) · .asmx   │      ┌────────────────────┐
  CLIMOV (Android)   MOVIL ──────►  │  ec.edu.monster.ws       contrato        │      │ SQL Server         │
                                    │  ec.edu.monster.servicio EurekaServicio ──────► │ EUREKABANK         │
       SOAP / HTTP + <Token>        │    GestorSesiones · MonitorServidor      │ ADO │ login monster      │
                                    │  ec.edu.monster.db       ConexionBD      │ .NET└────────────────────┘
                                    └──────────────────────────────────────────┘
```

- **Protocolo:** SOAP 1.1 sobre HTTP (`basicHttpBinding`). Namespace XML `http://espe.edu.ec/eureka/`.
- **Errores:** el servidor responde un **SOAP Fault** con `ErrorServicio { Codigo, Mensaje }`:
  `VALOR_INVALIDO` (formato), `REGLA_NEGOCIO` (saldo insuficiente, cuenta cancelada, monedas distintas…), `CREDENCIALES_INVALIDAS`, `USUARIO_BLOQUEADO`, `SESION_INVALIDA` (token) y `ERROR_BD` (SQL Server apagado). Cada cliente muestra el `Mensaje` en rojo; si no llega al servidor dice *"No se pudo conectar a {url}. Verifica IP, puerto y red."*

## 2. Estructura del repositorio

```
01. UML/03. DIAGRAMAS   casos de uso, actividad, secuencia, clases, arquitectura (.puml + .png)
02. MER/                modelo conceptual, lógico y físico (.puml + .png)
03. BDD/                01_Crea_BD.sql, 02_Carga_Datos.sql, 03_Crea_Login_monster.sql,
                        04_Consultas_Prueba.sql y crear_bdd.ps1 (ejecuta todo)
04. SERVIDOR/           EUREKA_SOAP_DOTNET_SERVIDOR (WCF .NET Framework 4.7.2, panel index.html) + publicar_iis.ps1
04. CLICON/             EUREKA_SOAP_DOTNET_CLICON  (consola .NET Framework 4.7.2)
04. CLIESC/             EUREKA_SOAP_DOTNET_CLIESC  (Windows Forms .NET Framework 4.7.2)
04. CLIWEB/             EUREKA_SOAP_DOTNET_CLIWEB  (ASP.NET Core MVC .NET 8) + scripts de red local
                        (Iniciar-RedLocal.ps1, Detener-RedLocal.ps1, Habilitar-Firewall-RedLocal.ps1)
04. CLIMOV/             EUREKA_SOAP_DOTNET_CLIMOV  (.NET MAUI Android .NET 9, sucesor de Xamarin)
05. DOCUMENTACIÓN/      manual, presentación y capturas
```

## 3. Operaciones del servicio

| Operación SOAP | Devuelve | Qué hace en la BDD |
|---|---|---|
| `login(usuario, clave)` | `Empleado` + `Token` | Busca en `Empleado` (usuario sin distinguir mayúsculas, clave exacta); 3 intentos y bloqueo de 2 min |
| `cerrarSesion(token)` | `true`/`false` | Invalida el token (no toca la BDD) |
| `obtenerEstado()` | `EstadoServidor` | Métricas del monitor para el panel; no pide login ni cuenta como petición |
| `consultarCuenta(cuenta)` | `Cuenta` | `Cuenta` + `Cliente` + `Moneda` + `Sucursal` |
| `traerMovimientos(cuenta)` | lista de `Movimiento` | `Movimiento` + `TipoMovimiento`, el más reciente primero |
| `registrarDeposito(cuenta, importe, codEmpleado)` | nuevo saldo | suma saldo, movimiento **003** |
| `registrarRetiro(cuenta, importe, codEmpleado)` | nuevo saldo | valida saldo, resta, movimiento **004** |
| `registrarTransferencia(origen, destino, importe, codEmpleado)` | saldo del origen | **009** en el origen y **008** en el destino, cada uno con la otra cuenta como referencia |

**Reglas:** cuenta de 8 dígitos; importe mayor que cero, con máximo 2 decimales y hasta 1 000 000; la cuenta debe estar `ACTIVO`; el retiro y la transferencia exigen saldo suficiente; la transferencia es entre cuentas distintas y de la misma moneda. Las cuentas se leen con `UPDLOCK` dentro de la transacción, así dos operaciones simultáneas sobre la misma cuenta no se pisan. El número de movimiento es `int_cuencontmov + 1`.

### Sesión con token (30 minutos)

`login` devuelve un **token** (32 caracteres hexadecimales). `consultarCuenta`, `traerMovimientos` y `registrar*` lo exigen en el **header SOAP**:

```xml
<s:Header><Token xmlns="http://espe.edu.ec/eureka/">9f3c…</Token></s:Header>
```

Sin token, con uno inventado o vencido responden un Fault `SESION_INVALIDA`: **"Token inválido o expirado. Inicie sesión."** `GestorSesiones` (Singleton) guarda `token → vencimiento`, renueva los 30 minutos en cada uso, borra los vencidos cada minuto y cuenta las sesiones activas. `cerrarSesion` lo invalida. Ningún cliente muestra el token.

### Monitor y panel (`http://IP:8087/`)

`MonitorServidor` (Singleton, seguro entre hilos) guarda **en memoria** la hora de inicio, el total de peticiones y de operaciones exitosas, el conteo por cliente (`WEB`, `MOVIL`, `ESCRITORIO`, `CONSOLA`, `PANEL`, `OTRO`), el conteo por operación (`Deposito`, `Retiro`, `Transferencia`, `Consulta`, `Movimientos`) y los **últimos 60 eventos** (hora, cliente, operación, éxito). **No guarda claves, tokens ni montos** y se reinicia al volver a desplegar. El cliente se toma del header HTTP **`X-Cliente`** que envía cada aplicación.

La raíz del sitio abre el **panel** (`index.html` + `panel.js` + `panel.css` + `monster.jpg`): estado *Servicio activo / Sin respuesta* (refresca cada 3 s con `obtenerEstado`), tarjetas de tiempo activo, sesiones, operaciones y peticiones, lista de eventos, barras por operación y por cliente, prueba rápida (login, consultar cuenta, depósito, cerrar sesión) y la tabla de operaciones con el enlace al WSDL. El panel envía `X-Cliente: PANEL`; sus consultas a `obtenerEstado` no cuentan como peticiones.

### WCF (`.svc`) y ASMX (`.asmx`)

El servidor publica las mismas 8 operaciones de dos formas; las dos son SOAP, usan `EurekaServicio` y la misma base:

| | `WSEureka.svc` | `WSEureka.asmx` |
|---|---|---|
| Tecnología | WCF (la que usan los 4 clientes) | ASP.NET Web Services (la anterior a WCF) |
| Nombres de operación | `login`, `consultarCuenta`, … | `Login`, `ConsultarCuenta`, … |
| En el navegador | "Servicio de WSEureka" y el WSDL en `?wsdl` | Lista de operaciones con formulario para probarlas |
| Errores | SOAP Fault con `ErrorServicio { Codigo, Mensaje }` | SOAP Fault con el mismo detalle `ErrorServicio` |

## 4. Patrones: Singleton y MVC

### Singleton (constructor privado + `Lazy<T>`, seguro entre hilos)

| Dónde | Clase | Uso |
|---|---|---|
| Servidor | `ec.edu.monster.db.ConexionBD` | Lee la cadena `EurekaBank` de `Web.config` una vez; `ConexionBD.Instancia.AbrirConexion()` |
| Servidor | `ec.edu.monster.servicio.EurekaServicio` | `EurekaServicio.Instancia.RegistrarRetiro(...)` |
| Servidor | `ec.edu.monster.servicio.GestorSesiones` | Tokens y vencimientos |
| Servidor | `ec.edu.monster.servicio.MonitorServidor` | Métricas del panel |
| CLICON / CLIESC | `ec.edu.monster.servicio.ClienteEureka` | `ClienteEureka.Instancia` (una sola `ChannelFactory`) |
| CLIWEB | `ec.edu.monster.servicio.ClienteEureka` | `builder.Services.AddSingleton<ClienteEureka>()` |
| CLIMOV | `ClienteSoap` y `ControladorEureka` | `ClienteSoap.Instancia`, `ControladorEureka.Instancia` |

```csharp
public sealed class ConexionBD
{
    private static readonly Lazy<ConexionBD> _instancia = new Lazy<ConexionBD>(() => new ConexionBD());
    public static ConexionBD Instancia => _instancia.Value;
    private ConexionBD() { /* lee Web.config una sola vez */ }
    public SqlConnection AbrirConexion() { ... }
}
```

### MVC en los clientes (`ec/edu/monster/...`)

| Cliente | modelo | vista | controlador |
|---|---|---|---|
| CLICON | `Empleado`, `Cuenta`, `Movimiento`, `ErrorServicio`, `TipoOperacion` | `VistaConsola` | `ControladorConsola` |
| CLIESC | `Empleado`, `Cuenta`, `Movimiento`, `ErrorServicio`, `TipoOperacion` | `FormLogin`, `FormPrincipal`, `PanelMovimientos`, `PanelOperacion` | `ControladorEureka` |
| CLIWEB | `Empleado`, `Cuenta`, `Movimiento`, `ErrorServicio`, `TipoOperacion`, `ViewModels.cs` | `vista/Acceso/Login`, `vista/Banco/Movimientos`, `vista/Banco/Operacion` | `AccesoController`, `BancoController` |
| CLIMOV | `Empleado`, `Cuenta`, `Movimiento`, `TipoOperacion` | `LoginPage`, `MenuPage`, `MovimientosPage`, `OperacionPage` | `ControladorEureka` |

**Los clientes solo presentan; todo viene del servicio:**
Igual que el proyecto de referencia del GR05, cada cliente tiene cuatro carpetas:
- **`modelo/`**: `Empleado`, `Cuenta`, `Movimiento` (y `ErrorServicio`) con **solo get/set**, para recibir lo que devuelve el servicio.
- **`servicio/`**: llama al servicio. Consola, escritorio y web usan `ChannelFactory<IWSEureka>` con la interfaz `servicio/IWSEureka.cs` (mismas operaciones y namespace `http://espe.edu.ec/eureka/`); el móvil arma el sobre SOAP con `HttpClient` y llena los modelos.
- **`controlador/`**: solo pasa las llamadas de la vista al servicio y guarda el empleado de la sesión.
- **`vista/`**: las pantallas y el formato de los datos (`Formato`: dinero, títulos, colores).

**Todo lo demás está en el servidor:** el login y los **3 intentos** (al tercero el usuario queda bloqueado 2 minutos), el formato de la cuenta, el importe (mayor que cero, 2 decimales), las cuentas distintas, el saldo, la moneda y el estado. El cliente solo convierte el importe a número (el contrato recibe un `decimal`) y muestra el mensaje del SOAP Fault.

En la web las vistas también están en `ec/edu/monster/vista` (se configuró `RazorViewEngineOptions` en `Program.cs`).

## 5. Requisitos

| Herramienta | Para qué |
|---|---|
| **SQL Server** (2019/2022, Developer o Express) en **modo de autenticación mixto** | Base `EUREKABANK` |
| `sqlcmd` (viene con SQL Server) o SQL Server Management Studio | Ejecutar los scripts |
| **Visual Studio 2022 Community** con *ASP.NET y web* (+ WCF), *Escritorio .NET* y *.NET MAUI* | Todo el código |
| Celular Android 7.0+ con depuración USB | Cliente móvil |

> **Regla de oro:** abre **cada solución en su propia ventana de Visual Studio**. Si abres otra solución en la ventana del servidor, se apaga IIS Express.

---

## Paso 0 — Base de datos (`03. BDD`)

**Opción A (script):** en PowerShell, dentro de `03. BDD`:
```powershell
powershell -ExecutionPolicy Bypass -File .\crear_bdd.ps1
```
Para otra instancia: `.\crear_bdd.ps1 -Servidor ".\SQLEXPRESS"`.

**Opción B (SSMS):** abre y ejecuta en orden `01_Crea_BD.sql`, `02_Carga_Datos.sql`, `03_Crea_Login_monster.sql`.

Resultado: 6 cuentas, 38 movimientos, 12 empleados (incluido `monster`) y el login de SQL `monster`. Si SQL Server está solo con autenticación de Windows, actívale el modo mixto (SSMS → Propiedades del servidor → Seguridad) y reinicia el servicio.

**Cuentas de prueba:**

| Cuenta | Cliente | Moneda | Saldo inicial | Estado |
|---|---|---|---|---|
| 00100001 | Alan Aranda | Soles | 6 900.00 | ACTIVO |
| 00100002 | Alan Aranda | Dólares | 4 500.00 | ACTIVO |
| 00200001 | Rosa Flores | Soles | 7 000.00 | ACTIVO |
| 00200002 | Eric Coronel | Soles | 6 800.00 | ACTIVO |
| 00200003 | Edgar Chávez | Dólares | 6 000.00 | ACTIVO |
| 00300001 | Gabriel Gonzales | Soles | 0.00 | CANCELADO |

## Paso 1 — Servidor (`04. SERVIDOR`)

1. Abre `EUREKA_SOAP_DOTNET_SERVIDOR.sln` → clic derecho en `index.html` → **Establecer como página principal**.
2. **Depurar → Iniciar sin depurar**. Debe abrir el panel `http://localhost:8087/` con **Servicio activo** (el servicio está en `/WSEureka.svc`).
3. La conexión está en `Web.config`:
   ```xml
   <add name="EurekaBank" connectionString="Data Source=localhost;Initial Catalog=EUREKABANK;User ID=monster;Password=monster;TrustServerCertificate=True;Connect Timeout=10" />
   ```
   Con SQL Express cambia `Data Source=.\SQLEXPRESS`.

## Paso 2 — Consola (`04. CLICON`)

Abre `EUREKA_SOAP_DOTNET_CLICON.sln` en otra ventana → **Iniciar sin depurar** → `monster` / `monster69`. Menú: **[1] Consultar cuenta y movimientos, [2] Depósito, [3] Retiro, [4] Transferencia, [0] Cerrar sesión** (invalida el token en el servidor). El servidor se configura en `App.config` (`SoapHost`, `SoapPuerto` = 8087, `SoapRuta`) o sin recompilar con las variables `SOAP_HOST` / `SOAP_PORT`:
```powershell
$env:SOAP_HOST = "192.168.100.2"; .\EUREKA_SOAP_DOTNET_CLICON.exe
```

## Paso 3 — Escritorio (`04. CLIESC`)

Abre `EUREKA_SOAP_DOTNET_CLIESC.sln` → **Iniciar sin depurar** → login. Menú lateral: **Movimientos, Depósito, Retiro, Transferencia**. Pide confirmación antes de registrar y muestra el nuevo saldo; a la derecha lleva las operaciones de la sesión. Servidor en `App.config` (`SoapHost`, `SoapPuerto`, `SoapRuta`) o variables `SOAP_HOST` / `SOAP_PORT`, igual que la consola.

## Paso 4 — Web (`04. CLIWEB`)

Abre `EUREKA_SOAP_DOTNET_CLIWEB.sln` → perfil **EUREKA_SOAP_DOTNET_CLIWEB** → **Iniciar sin depurar** → `http://localhost:5087`. Si pide certificado SSL responde **No**. Servidor en `appsettings.json`, sección `ServidorSoap` (`Host`, `Puerto` = 8087, `Ruta`). Para abrirla desde otros equipos: `Iniciar-RedLocal.ps1` (ver Paso 6). Incluye filtro `[RequiereSesion]` y `ValidateAntiForgeryToken`. Es *responsive*.

## Paso 5 — Móvil (`04. CLIMOV`)

Igual que en el TI.1.2: celular con **Depuración USB**, abrir `EUREKA_SOAP_DOTNET_CLIMOV.sln`, elegir el celular y **Iniciar sin depurar**. En la pantalla de login, cuadro **Servidor SOAP**:

- Por cable (solo si no hay Wi-Fi común): `adb reverse tcp:8087 tcp:8087`
- Por WiFi: `http://IP-DE-LA-PC:8087/WSEureka.svc` (servidor publicado en IIS, Paso 6)
- Emulador: `http://10.0.2.2:8087/WSEureka.svc`

`Directory.Build.props` manda la carpeta `obj` a `%LOCALAPPDATA%\EUREKA_CLIMOV\obj` (Android falla con rutas de más de 260 caracteres).

## Configuración del servidor en cada cliente

El servidor corre en **IIS** y los clientes lo buscan por la **IP de la PC** (no `localhost`): por defecto `192.168.100.2:8087`.
Como la IP cambia según la red (casa, aula, guardia), **cada cliente la puede cambiar desde su propia pantalla**, sin tocar archivos ni recompilar. Ningún cliente tiene la dirección fija en el código; si el host o el puerto no son válidos muestran un mensaje claro.

| Cliente | Cambiar desde la pantalla | Valor por defecto | Envía `X-Cliente` |
|---|---|---|---|
| Consola | En el login escribir **S** → IP y puerto (se guardan en el `.exe.config`) | `App.config` → `SoapHost`, `SoapPuerto` (o variables `SOAP_HOST`, `SOAP_PORT`) | `CONSOLA` |
| Escritorio | Login → fila **Servidor** `[IP] : [puerto]` → **Cambiar** (se guarda en el `.exe.config`) | `App.config` → `SoapHost`, `SoapPuerto` | `ESCRITORIO` |
| Web | Login → **⚙ Servidor · cambiar** → **Guardar** (se guarda en `appsettings.json`) | `appsettings.json` → `ServidorSoap:Host`, `Puerto`, `Ruta` | `WEB` |
| Móvil | Login → **⚙ Servidor SOAP (IIS)** → IP y puerto → **Guardar** (queda en el teléfono; **Restablecer** vuelve al valor por defecto) | `appsettings.json` incrustado (`EmbeddedResource`) | `MOVIL` (`ESCRITORIO` si corre en Windows) |

## Presentación de los clientes (estilo Monsters Inc.)

Solo cambian las pantallas: los datos y las reglas siguen viniendo del servidor y la estructura `modelo / servicio / controlador / vista` es la misma.

| Mejora | Consola | Escritorio | Web | Móvil |
|---|:-:|:-:|:-:|:-:|
| Título **MONSTER** y **EUREKA BANK** | letras de bloque | marca en el menú | marca en el menú | login y menú |
| Pantalla de bienvenida con Sulley y Boo | — | ✔ | (login con foto) | ✔ |
| Intentos de login con 3 caritas 👾 (según "Intentos restantes" del servidor) | ■ ■ □ | ✔ | ✔ | ✔ |
| Indicador **Servidor activo / Sin respuesta** y minutos de sesión (token de 30 min) | barra en el encabezado + opción 5 | barra de estado | arriba a la derecha | menú y login |
| Cuenta como **tarjeta bancaria** (0010 0001, titular, saldo) | — | ✔ | ✔ | ✔ |
| **Gráficos**: total por tipo y evolución del saldo | — | ✔ | ✔ | ✔ |
| **Comprobante** después de cada operación (N.º de movimiento, sello APROBADO) | marco doble | ventana + Imprimir/PDF | ventana + Imprimir/PDF | ventana + Compartir |
| "Enviando…" mientras responde el servidor | spinner ⠹ | cursor de espera | — | indicador |
| Modo oscuro | — | — | ✔ (se recuerda) | ✔ (se recuerda) |

El número de movimiento del comprobante y el titular se piden al servidor con `consultarCuenta` justo después de la operación (el movimiento recién grabado es el último de la cuenta).

## Paso 6 — Trabajo en red

| Integrante | Rol |
|---|---|
| 1 | Base de datos + servidor + consola |
| 2 | Web + escritorio (apuntando a la IP del integrante 1) |
| 3 | Móvil en celular real (misma WiFi) |

IIS Express solo acepta conexiones locales. El integrante 1 compila el servidor, cierra su Visual Studio y ejecuta **como administrador**:
```powershell
powershell -ExecutionPolicy Bypass -File "RUTA\04. SERVIDOR\publicar_iis.ps1"
```
El script crea el sitio **EUREKA** en el puerto **8087**, abre el firewall y muestra la URL Wi-Fi. Como el servicio entra a SQL Server con el login `monster`, no hace falta dar permisos al usuario de IIS.

**Publicación manual (como la pide el ingeniero):**
1. Visual Studio **como administrador** → abrir el servidor → configuración **Release** → clic derecho en el proyecto → **Publicar** → **Carpeta** → `C:\inetpub\eureka` → **Publicar** (el perfil queda en `Properties/PublishProfiles/FolderProfile.pubxml`).
2. Administrador de IIS → **Grupos de aplicaciones** → **Agregar**: nombre `EUREKA`, **.NET CLR v4.0**, canalización **Integrada**.
3. **Sitios** → **Agregar sitio web**: nombre `EUREKA`, grupo `EUREKA`, ruta física `C:\inetpub\eureka`, `http`, *Todas las no asignadas*, puerto `8087`.
4. Firewall: regla de entrada para el puerto TCP **8087** (el script la crea como "EUREKA SOAP 8087").
5. Probar en el navegador `http://IP:8087/WSEureka.svc` y `http://IP:8087/WSEureka.asmx`.

## Casos de prueba

| # | Caso | Entrada | Resultado esperado |
|---|---|---|---|
| 1 | Login correcto | monster / monster69 | Entra al menú |
| 2 | Login incorrecto | monster / otra | "Credenciales inválidas. Intentos restantes: 2." (lo cuenta el servidor) |
| 2b | Tercer intento fallido | monster / otra (3 veces) | "Cuenta bloqueada: se superaron los 3 intentos. Intentos restantes: 0. Espere 2 minutos." |
| 2c | Sin token o token cerrado | `consultarCuenta` sin header `<Token>` | "Token inválido o expirado. Inicie sesión." |
| 3 | Consultar cuenta | 00100001 | Saldo 6,900.00 y 7 movimientos |
| 4 | Cuenta inexistente | 99999999 | "La cuenta 99999999 no existe." |
| 5 | Formato de cuenta | 123 | "La cuenta debe tener 8 dígitos" (lo responde el servidor) |
| 6 | Depósito | 00100001, 100.50 | Saldo 7,000.50, movimiento 003 |
| 7 | Retiro | 00100001, 0.50 | Saldo 7,000.00, movimiento 004 |
| 8 | Saldo insuficiente | 00100001, 50000 | "Saldo insuficiente…" |
| 9 | Cuenta cancelada | retiro en 00300001 | "La cuenta 00300001 no está activa (CANCELADO)." |
| 10 | Transferencia | 00100001 → 00200001, 100 | Movimientos 009 y 008 con referencia cruzada |
| 11 | Monedas distintas | 00100001 → 00100002 | "Solo se puede transferir entre cuentas de la misma moneda." |
| 12 | Más de 2 decimales | 1.555 | "El importe admite como máximo 2 decimales." |
| 13 | SQL Server apagado | cualquier operación | "No se pudo acceder a la base de datos EUREKABANK…" (comportamiento esperado; no se probó porque requiere detener el servicio de SQL Server) |
| 14 | Servidor SOAP apagado | — | "No se pudo conectar a http://…:8087/WSEureka.svc. Verifica IP, puerto y red." |
| 14b | Host inválido en la configuración | `SoapHost` = "mal host" | "Host del servidor inválido…" |
| 15 | Página protegida (web) | `/Banco/Movimientos` sin sesión | Redirección al login |

Para volver a los datos iniciales, ejecuta otra vez `03. BDD\crear_bdd.ps1`.

## Documentación

| Archivo | Contenido |
|---|---|
| `01. UML/01. ERS/ERS_EUREKABANK_SOAP_DOTNET_BDD_GR01.docx` | Especificación de requisitos (IEEE 830): 14 RF y 8 RNF |
| `01. UML/02. ECUD/ECUD_EUREKABANK_SOAP_DOTNET_BDD_GR01.docx` | Casos de uso CU01–CU07 con sus diagramas de actividad y secuencia |
| `05. DOCUMENTACIÓN/WS_EUREKABANK_SOAP_DOTNET_BDD_GR01.docx` | Manual técnico paso a paso, con el código y las capturas |
| `05. DOCUMENTACIÓN/MANUAL_EUREKABANK_SOAP_DOTNET_BDD_GR01.pptx` | Presentación (16 diapositivas) |
| `05. DOCUMENTACIÓN/capturas/` | Capturas reales de servidor, BDD, consola, escritorio y web |

Al abrir el manual en Word, si el índice aparece vacío: clic derecho sobre él → **Actualizar campo**.

Los documentos se generan con los scripts de `_herramientas/generador_documentos_eureka` (fuera de este repositorio). Las capturas del móvil se tomaron en una tablet Samsung SM-P610 con `adb exec-out screencap -p`.
