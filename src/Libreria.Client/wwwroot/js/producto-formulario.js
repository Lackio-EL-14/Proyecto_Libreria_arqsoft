// Alta y edición de productos: confirmación de precio menor al costo y fecha de vencimiento
// solo para perecederos. Si el formulario tiene errores (jQuery unobtrusive), no se abre el modal.
(function () {
    "use strict";

    const form = document.getElementById("productoForm");
    if (!form) {
        return;
    }

    const modalPrecio = document.getElementById("modal-confirmar-precio");
    const btnConfirmarPrecio = document.getElementById("btnConfirmarPrecioModal");
    let precioConfirmado = false;

    function formularioValido() {
        return !(window.jQuery && window.jQuery.fn.valid) || window.jQuery(form).valid();
    }

    form.addEventListener("submit", function (event) {
        if (precioConfirmado || !formularioValido()) {
            return;
        }

        const precioVenta = parseFloat(document.getElementById("Input_PrecioVenta").value);
        const costoAdquisicion = parseFloat(document.getElementById("Input_CostoAdquisicion").value);

        if (!isNaN(precioVenta) && !isNaN(costoAdquisicion) && precioVenta < costoAdquisicion) {
            event.preventDefault();
            if (typeof modalPrecio.showModal === "function") {
                modalPrecio.showModal();
            } else {
                modalPrecio.setAttribute("open", "");
                modalPrecio.style.display = "block";
            }
        }
    });

    btnConfirmarPrecio.addEventListener("click", function () {
        precioConfirmado = true;
        if (typeof modalPrecio.close === "function") {
            modalPrecio.close();
        } else {
            modalPrecio.removeAttribute("open");
            modalPrecio.style.display = "none";
        }
        form.requestSubmit();
    });

    const esPerecedero = document.getElementById("esPerecedero");
    const fechaGroup = document.getElementById("fechaVencimientoGroup");
    const fechaInput = document.getElementById("fechaVencimiento");

    function actualizarFechaVencimiento() {
        if (esPerecedero.checked) {
            fechaGroup.style.display = "block";
            fechaInput.disabled = false;
        } else {
            fechaInput.value = "";
            fechaInput.disabled = true;
            fechaGroup.style.display = "none";
        }
    }

    esPerecedero.addEventListener("change", actualizarFechaVencimiento);
    actualizarFechaVencimiento();
})();
