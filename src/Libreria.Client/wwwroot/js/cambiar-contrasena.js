(() => {
    'use strict';

    const formulario = document.getElementById('cambiar-contrasena');
    if (!formulario) return;

    const actual = document.getElementById('Input_ContrasenaActual');
    const nueva = document.getElementById('Input_NuevaContrasena');
    const confirmacion = document.getElementById('Input_Confirmacion');
    const estadoConfirmacion = document.getElementById('estado-confirmacion');
    const reglas = [
        ['longitud', valor => valor.length >= 8],
        ['mayuscula', valor => /\p{Lu}/u.test(valor)],
        ['minuscula', valor => /\p{Ll}/u.test(valor)],
        ['numero', valor => /\p{Nd}/u.test(valor)],
        ['especial', valor => /[\p{P}\p{S}]/u.test(valor)]
    ].map(([nombre, cumple]) => {
        const elemento = formulario.querySelector(`[data-regla="${nombre}"]`);
        return { elemento, cumple, texto: elemento.textContent.trim() };
    });

    function validar() {
        let reglasCumplidas = true;
        for (const regla of reglas) {
            const cumple = regla.cumple(nueva.value);
            // El círculo o la marca verde se ven por CSS; el lector de pantalla oye «Cumple» o «Pendiente».
            const estado = document.createElement('span');
            estado.className = 'sr-only';
            estado.textContent = cumple ? 'Cumple: ' : 'Pendiente: ';
            regla.elemento.replaceChildren(estado, regla.texto);
            regla.elemento.classList.toggle('is-cumplida', cumple);
            reglasCumplidas = reglasCumplidas && cumple;
        }

        const coinciden = nueva.value.length > 0 && nueva.value === confirmacion.value;
        estadoConfirmacion.textContent = confirmacion.value.length === 0
            ? 'Escriba de nuevo la nueva contraseña para confirmarla.'
            : coinciden ? 'Las contraseñas coinciden.' : 'Las contraseñas no coinciden.';
        estadoConfirmacion.classList.toggle('text-danger', confirmacion.value.length > 0 && !coinciden);
        estadoConfirmacion.classList.toggle('is-cumplida', coinciden);

        return actual.value.length > 0 && reglasCumplidas && coinciden;
    }

    // El botón queda siempre activo (pruebas del 22/11): al enviar, jQuery unobtrusive muestra
    // el mensaje de cada campo y el servidor vuelve a validar la política.
    formulario.addEventListener('input', validar);
    formulario.addEventListener('change', validar);
    formulario.addEventListener('submit', validar);
    validar();
})();
