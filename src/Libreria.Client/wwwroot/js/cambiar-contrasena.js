(() => {
    'use strict';

    const formulario = document.getElementById('cambiar-contrasena');
    if (!formulario) return;

    const actual = document.getElementById('Input_ContrasenaActual');
    const nueva = document.getElementById('Input_NuevaContrasena');
    const confirmacion = document.getElementById('Input_Confirmacion');
    const estadoConfirmacion = document.getElementById('estado-confirmacion');
    const boton = formulario.querySelector('button[type="submit"]');
    const reglas = [
        ['longitud', valor => valor.length >= 8],
        ['mayuscula', valor => /\p{Lu}/u.test(valor)],
        ['minuscula', valor => /\p{Ll}/u.test(valor)],
        ['numero', valor => /\p{Nd}/u.test(valor)],
        ['especial', valor => /[\p{P}\p{S}]/u.test(valor)]
    ].map(([nombre, cumple]) => {
        const elemento = formulario.querySelector(`[data-regla="${nombre}"]`);
        return { elemento, cumple, texto: elemento.textContent.replace('Pendiente: ', '') };
    });

    function validar() {
        let reglasCumplidas = true;
        for (const regla of reglas) {
            const cumple = regla.cumple(nueva.value);
            regla.elemento.textContent = `${cumple ? 'Cumple' : 'Pendiente'}: ${regla.texto}`;
            regla.elemento.classList.toggle('text-danger', !cumple);
            reglasCumplidas = reglasCumplidas && cumple;
        }

        const coinciden = nueva.value.length > 0 && nueva.value === confirmacion.value;
        estadoConfirmacion.textContent = confirmacion.value.length === 0
            ? 'Introduce la confirmación de la nueva contraseña.'
            : coinciden ? 'Las contraseñas coinciden.' : 'Las contraseñas no coinciden.';
        estadoConfirmacion.classList.toggle('text-danger', !coinciden);

        boton.disabled = !(actual.value.length > 0 && reglasCumplidas && coinciden
            && formulario.checkValidity());
        return !boton.disabled;
    }

    formulario.addEventListener('input', validar);
    formulario.addEventListener('change', validar);
    formulario.addEventListener('submit', evento => {
        if (!validar()) evento.preventDefault();
    });
    validar();
})();
