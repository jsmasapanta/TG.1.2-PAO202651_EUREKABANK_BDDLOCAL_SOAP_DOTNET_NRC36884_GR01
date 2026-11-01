// Indicador "Servidor activo" y minutos de sesión (consulta /Acceso/Estado cada 10 s) y botón de modo oscuro.
(function () {
    function pintar(datos) {
        document.querySelectorAll('[data-estado-servidor]').forEach(function (el) {
            el.classList.toggle('caido', !datos.activo);
            var host = (datos.servidor || '').replace(/^https?:\/\//, '').replace(/\/.*$/, '');
            el.querySelector('.texto').textContent = datos.activo ? 'Servidor activo · ' + host : 'Sin respuesta · ' + host;
        });
        document.querySelectorAll('[data-sesion-restante]').forEach(function (el) {
            el.textContent = datos.minutos == null ? '' : '⏱ Sesión: ' + datos.minutos + ' min';
            el.classList.toggle('por-vencer', datos.minutos != null && datos.minutos <= 5);
        });
    }

    function consultar() {
        if (!document.querySelector('[data-estado-servidor]')) return;
        fetch('/Acceso/Estado', { cache: 'no-store' })
            .then(function (r) { return r.json(); })
            .then(pintar)
            .catch(function () { pintar({ activo: false, servidor: '', minutos: null }); });
    }

    window.cambiarTema = function () {
        var oscuro = document.documentElement.dataset.tema !== 'oscuro';
        if (oscuro) document.documentElement.dataset.tema = 'oscuro'; else delete document.documentElement.dataset.tema;
        try { localStorage.setItem('tema', oscuro ? 'oscuro' : 'claro'); } catch (e) { }
        textoTema();
    };

    function textoTema() {
        var oscuro = document.documentElement.dataset.tema === 'oscuro';
        document.querySelectorAll('.boton-tema').forEach(function (b) { b.textContent = oscuro ? '☀ Modo claro' : '🌙 Modo oscuro'; });
    }

    textoTema();
    consultar();
    setInterval(consultar, 10000);
})();
