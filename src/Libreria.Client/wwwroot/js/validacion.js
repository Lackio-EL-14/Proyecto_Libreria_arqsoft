// Validación del lado del cliente en español (pruebas del 22/11, A4, B2 y C3).
// Se carga después de jQuery Validate y de unobtrusive (ver _ValidationScriptsPartial).
(function ($) {
    "use strict";

    if (!$ || !$.validator) {
        return;
    }

    // Mensajes por defecto: solo se ven si un campo no trae el suyo (data-val-* o data-msg-*).
    // jQuery Validate también lee los atributos HTML min, max, step y maxlength.
    $.extend($.validator.messages, {
        required: "Este campo es obligatorio.",
        remote: "Revise este campo.",
        email: "Ingrese un correo electrónico válido.",
        url: "Ingrese una dirección web válida.",
        date: "Ingrese una fecha válida.",
        dateISO: "Ingrese una fecha válida.",
        number: "Ingrese un número válido.",
        digits: "Ingrese solo dígitos.",
        equalTo: "Los valores no coinciden.",
        maxlength: $.validator.format("No puede superar los {0} caracteres."),
        minlength: $.validator.format("Debe tener al menos {0} caracteres."),
        rangelength: $.validator.format("Debe tener entre {0} y {1} caracteres."),
        range: $.validator.format("Ingrese un valor entre {0} y {1}."),
        max: $.validator.format("Ingrese un valor menor o igual a {0}."),
        min: $.validator.format("Ingrese un valor mayor o igual a {0}."),
        step: $.validator.format("Ingrese un múltiplo de {0}.")
    });

    // El min="0" de los campos numéricos usa el mismo mensaje del [Range] del modelo
    // (por ejemplo, «El stock no puede ser negativo.»).
    document.querySelectorAll("[data-val-range]").forEach(function (campo) {
        if (!campo.hasAttribute("data-msg-min")) {
            campo.setAttribute("data-msg-min", campo.getAttribute("data-val-range"));
        }
        if (!campo.hasAttribute("data-msg-max")) {
            campo.setAttribute("data-msg-max", campo.getAttribute("data-val-range"));
        }
    });

    // [RequeridoAlgunoDe]: este campo o el otro deben tener valor (teléfono o correo del contacto).
    $.validator.addMethod("requeridoalguno", function (valor, elemento, otro) {
        return $.trim(valor) !== "" || (otro && $.trim($(otro).val()) !== "");
    });

    $.validator.unobtrusive.adapters.add("requeridoalguno", ["otro"], function (opciones) {
        var nombre = opciones.element.name;
        var prefijo = nombre.substring(0, nombre.lastIndexOf(".") + 1);
        var nombreOtro = opciones.params.otro.replace("*.", prefijo);
        var otro = $(opciones.form).find(":input").filter(function () {
            return this.name === nombreOtro;
        })[0];

        opciones.rules.requeridoalguno = otro;
        opciones.messages.requeridoalguno = opciones.message;

        // Al escribir en el otro campo, se vuelve a revisar este (si ya se mostró el error).
        if (otro) {
            $(otro).on("input change", function () {
                var $campo = $(opciones.element);
                if ($campo.hasClass("input-validation-error")) {
                    $campo.valid();
                }
            });
        }
    });
})(window.jQuery);
