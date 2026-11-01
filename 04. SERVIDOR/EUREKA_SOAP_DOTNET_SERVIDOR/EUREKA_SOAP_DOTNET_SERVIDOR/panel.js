// Panel del servidor (index.html). Habla con WSEureka.svc por SOAP, como cualquier cliente,
// con el header X-Cliente: PANEL. obtenerEstado no pide login y no cuenta como petición.
const URL_SOAP = new URL("WSEureka.svc", location.href).href;
const NS = "http://espe.edu.ec/eureka/";
const OPERACIONES = {
  Deposito: ["Depósitos", "var(--verde)"], Retiro: ["Retiros", "var(--rojo)"], Transferencia: ["Transferencias", "var(--azul)"],
  Consulta: ["Consultas", "var(--cian)"], Movimientos: ["Movimientos", "var(--morado)"]
};
const COLOR_CLIENTE = { WEB: "var(--cian)", MOVIL: "var(--verde)", ESCRITORIO: "var(--morado)", CONSOLA: "var(--amarillo)", PANEL: "var(--azul)", OTRO: "#94a3b8" };
let vistos = new Set(), sesion = null;

const $ = id => document.getElementById(id);
const esc = t => String(t ?? "").replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&apos;" }[c]));
const dinero = n => Number(n).toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const hijos = (el, nombre) => el ? Array.from(el.children).filter(e => e.localName === nombre) : [];
const texto = (el, nombre) => (hijos(el, nombre)[0] || {}).textContent ?? "";
$("base").textContent = URL_SOAP;

/** Envía la operación SOAP y devuelve el elemento <operacionResult>; si hay Fault lanza su <Mensaje>. */
async function soap(operacion, parametros = {}, token = null) {
  const cuerpo = Object.entries(parametros).map(([k, v]) => `<${k}>${esc(v)}</${k}>`).join("");
  const cabecera = token ? `<s:Header><Token xmlns="${NS}">${esc(token)}</Token></s:Header>` : "";
  const sobre = `<?xml version="1.0" encoding="utf-8"?><s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">${cabecera}`
    + `<s:Body><${operacion} xmlns="${NS}">${cuerpo}</${operacion}></s:Body></s:Envelope>`;
  const r = await fetch(URL_SOAP, {
    method: "POST", cache: "no-store", body: sobre,
    headers: { "Content-Type": "text/xml; charset=utf-8", "SOAPAction": `"${NS}WSEureka/${operacion}"`, "X-Cliente": "PANEL" }
  });
  const doc = new DOMParser().parseFromString(await r.text(), "text/xml");
  const resultado = doc.getElementsByTagNameNS("*", operacion + "Result")[0];
  if (!resultado) {
    const mensaje = doc.getElementsByTagNameNS("*", "Mensaje")[0] || doc.getElementsByTagName("faultstring")[0];
    throw new Error(mensaje ? mensaje.textContent : `Respuesta inesperada (HTTP ${r.status})`);
  }
  return resultado;
}

function contar(id, fin) {
  const el = $(id), ini = parseInt(el.textContent) || 0;
  if (ini === fin) return;
  const t0 = performance.now();
  (function paso(t) { const k = Math.min((t - t0) / 600, 1); el.textContent = Math.round(ini + (fin - ini) * k); if (k < 1) requestAnimationFrame(paso); })(t0);
}
function duracion(s) { const h = Math.floor(s / 3600), m = Math.floor(s % 3600 / 60); return h ? `${h} h ${m} min` : m ? `${m} min ${s % 60} s` : `${s} s`; }
const hora = iso => iso.substring(11, 19);
const conteos = el => hijos(el, "Conteo").map(c => [texto(c, "Nombre"), +texto(c, "Cantidad")]);

function barras(id, datos, color) {
  const max = Math.max(1, ...datos.map(d => d[1]));
  $(id).innerHTML = datos.map(([k, v]) =>
    `<div class="barra"><div class="fila"><span>${esc(color(k)[0])}</span><b>${v}</b></div><div class="pista"><div class="relleno" data-w="${v / max * 100}" style="background:${color(k)[1]}"></div></div></div>`).join("");
  requestAnimationFrame(() => document.querySelectorAll(`#${id} .relleno`).forEach(b => b.style.width = b.dataset.w + "%"));
}

async function refrescar() {
  try {
    const e = await soap("obtenerEstado");
    $("estado").classList.remove("caido");
    $("txtEstado").textContent = "Servicio activo";
    $("uptime").textContent = duracion(+texto(e, "SegundosActivo"));
    $("inicio").textContent = "Inicio: " + texto(e, "Inicio").substring(0, 19).replace("T", " ");
    const porOperacion = conteos(hijos(e, "PorOperacion")[0]);
    contar("sesiones", +texto(e, "SesionesActivas"));
    contar("operaciones", porOperacion.reduce((s, d) => s + d[1], 0));
    contar("peticiones", +texto(e, "TotalPeticiones"));
    $("exitosas").textContent = `${texto(e, "TotalExitosas")} exitosas`;
    barras("barras", porOperacion, k => OPERACIONES[k] || [k, "var(--azul)"]);
    barras("clientes", conteos(hijos(e, "PorCliente")[0]), k => [k, COLOR_CLIENTE[k] || "#94a3b8"]);

    const eventos = hijos(hijos(e, "Eventos")[0], "EventoMonitor");
    if (eventos.length) {
      $("eventos").innerHTML = eventos.map(ev => {
        const h = texto(ev, "Hora"), cli = texto(ev, "Cliente"), op = texto(ev, "Operacion"), ok = texto(ev, "Exito") === "true";
        const id = h + cli + op, nuevo = !vistos.has(id); vistos.add(id);
        return `<li class="evento ${ok ? "" : "error"}" style="${nuevo ? "" : "animation:none"}"><span class="hora">${esc(hora(h))}</span>`
          + `<span class="badge b-${esc(cli)}">${esc(cli)}</span><span><b>${esc(op)}</b> · ${ok ? "éxito" : "error"}</span></li>`;
      }).join("");
    }
  } catch (err) {
    $("estado").classList.add("caido");
    $("txtEstado").textContent = "Sin respuesta";
  }
}

function mostrar(html, ok) { const r = $("res"); r.className = "resultado"; void r.offsetWidth; r.className = "resultado " + (ok ? "ok" : "mal"); r.innerHTML = html; }

$("fLogin").onsubmit = async ev => {
  ev.preventDefault();
  try {
    if (sesion) {
      await soap("cerrarSesion", { token: sesion.token });
      sesion = null;
      $("fCuenta").style.display = "none"; $("btnLogin").textContent = "Iniciar sesión";
      mostrar("Sesión cerrada: el token ya no sirve.", true);
    } else {
      const r = await soap("login", { usuario: $("usr").value, clave: $("clv").value });
      sesion = { token: texto(r, "Token"), codigo: texto(r, "Codigo") };
      $("clv").value = "";
      $("fCuenta").style.display = "grid"; $("btnLogin").textContent = "Cerrar sesión";
      mostrar(`✅ Bienvenido, <b>${esc(texto(r, "Nombre"))}</b> (empleado ${esc(sesion.codigo)}).`, true);
    }
  } catch (err) { mostrar("❌ " + esc(err.message), false); }
  refrescar();
};

$("fCuenta").onsubmit = async ev => {
  ev.preventDefault();
  const cuenta = $("cta").value.trim();
  try {
    const c = await soap("consultarCuenta", { cuenta }, sesion.token);
    const m = hijos(await soap("traerMovimientos", { cuenta }, sesion.token), "Movimiento").slice(0, 5);
    const filas = m.map(x => { const ing = texto(x, "Accion") === "INGRESO";
      return `<tr><td>${esc(texto(x, "Numero"))}</td><td>${esc(texto(x, "Fecha").substring(0, 10))}</td><td>${esc(texto(x, "Tipo"))}</td><td class="${ing ? "ing" : "sal"}">${ing ? "+" : "−"}${dinero(texto(x, "Importe"))}</td></tr>`; }).join("");
    mostrar(`✅ Cuenta <b>${esc(cuenta)}</b> · ${esc(texto(c, "Cliente"))} (${esc(texto(c, "Estado"))})<br>Saldo: <b style="font-size:20px">${dinero(texto(c, "Saldo"))}</b> ${esc(texto(c, "Moneda"))}`
      + (filas ? `<table class="movs"><tr><th>#</th><th>Fecha</th><th>Tipo</th><th>Importe</th></tr>${filas}</table>` : ""), true);
  } catch (err) { mostrar("❌ " + esc(err.message), false); }
  refrescar();
};

$("btnDepositar").onclick = async () => {
  const cuenta = $("cta").value.trim(), importe = $("imp").value.trim().replace(",", ".");
  try {
    const saldo = (await soap("registrarDeposito", { cuenta, importe, codEmpleado: sesion.codigo }, sesion.token)).textContent;
    mostrar(`✅ Depósito de <b>${dinero(importe)}</b> en ${esc(cuenta)}. Nuevo saldo: <b style="font-size:20px">${dinero(saldo)}</b>`, true);
  } catch (err) { mostrar("❌ " + esc(err.message), false); }
  refrescar();
};

refrescar(); setInterval(refrescar, 3000);
