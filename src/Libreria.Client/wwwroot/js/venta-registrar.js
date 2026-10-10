(() => {
    'use strict';

    const registro = document.getElementById('registro-venta');
    if (!registro) return;

    // Estado exclusivo de esta página: no se persiste en servidor ni en almacenamiento local.
    const carrito = new Map();
    let clienteSeleccionado = null;
    let productosEncontrados = [];
    const lineas = document.getElementById('lineas-carrito');
    const mensaje = document.getElementById('mensaje-carrito');
    const botonAltaCliente = document.getElementById('registrar-cliente');
    const modalAltaCliente = document.getElementById('cliente-alta-modal');
    const formularioAltaCliente = modalAltaCliente.querySelector('form');
    const campoBusquedaCliente = document.getElementById('cliente-ci-nit');
    const errorAltaCliente = document.getElementById('alta-cliente-error');
    const estadoAltaCliente = document.getElementById('alta-cliente-estado');
    const erroresAltaCliente = Array.from(formularioAltaCliente.querySelectorAll('[data-cliente-error]'));
    let ciNitSinResultados = '';
    let guardandoCliente = false;
    const moneda = new Intl.NumberFormat('es-BO', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
    const precioEnCentavos = precio => Math.round(precio * 100);
    const mostrarImporte = centavos => `Bs. ${moneda.format(centavos / 100)}`;

    function crearElemento(etiqueta, texto, clase) {
        const elemento = document.createElement(etiqueta);
        if (texto !== undefined) elemento.textContent = texto;
        if (clase) elemento.className = clase;
        return elemento;
    }

    function crearBoton(texto, clase, accion) {
        const boton = crearElemento('button', texto, clase);
        boton.type = 'button';
        boton.addEventListener('click', accion);
        return boton;
    }

    function actualizarTotal() {
        let total = 0;
        for (const linea of carrito.values()) {
            total += linea.cantidad * precioEnCentavos(linea.precioVenta);
        }
        document.getElementById('total-venta').textContent = mostrarImporte(total);
        document.getElementById('carrito-vacio').hidden = carrito.size > 0;
        document.getElementById('tabla-carrito').hidden = carrito.size === 0;
    }

    function renderizarCarrito() {
        lineas.replaceChildren();
        for (const [publicId, linea] of carrito) {
            const fila = crearElemento('tr');
            fila.append(
                crearElemento('td', linea.nombre),
                crearElemento('td', mostrarImporte(precioEnCentavos(linea.precioVenta))),
                crearElemento('td', String(linea.stock))
            );

            const celdaCantidad = crearElemento('td');
            const cantidad = crearElemento('input', undefined, 'form-control venta-cantidad');
            cantidad.type = 'number';
            cantidad.min = '1';
            cantidad.max = String(linea.stock);
            cantidad.step = '1';
            cantidad.value = String(linea.cantidad);
            cantidad.setAttribute('aria-label', `Cantidad de ${linea.nombre}`);
            cantidad.setAttribute('aria-describedby', 'mensaje-carrito');
            const subtotal = crearElemento('td',
                mostrarImporte(linea.cantidad * precioEnCentavos(linea.precioVenta)), 'venta-subtotal');

            function actualizarCantidad(restaurarSiInvalida) {
                const valor = Number(cantidad.value);
                if (cantidad.value.trim() === '' || !Number.isInteger(valor) || valor < 1 || valor > linea.stock) {
                    const error = `La cantidad de ${linea.nombre} debe ser un entero entre 1 y ${linea.stock}.`;
                    mensaje.textContent = error;
                    cantidad.setCustomValidity(error);
                    cantidad.setAttribute('aria-invalid', 'true');
                    if (restaurarSiInvalida) {
                        cantidad.value = String(linea.cantidad);
                        cantidad.setCustomValidity('');
                        cantidad.removeAttribute('aria-invalid');
                    }
                    return;
                }

                cantidad.setCustomValidity('');
                cantidad.removeAttribute('aria-invalid');
                mensaje.textContent = '';
                linea.cantidad = valor;
                subtotal.textContent = mostrarImporte(valor * precioEnCentavos(linea.precioVenta));
                actualizarTotal();
                renderizarProductos();
            }

            cantidad.addEventListener('input', () => actualizarCantidad(false));
            cantidad.addEventListener('change', () => actualizarCantidad(true));
            celdaCantidad.append(cantidad);

            const acciones = crearElemento('td');
            const quitar = crearBoton('Quitar', 'btn-table btn-table--danger', () => {
                carrito.delete(publicId);
                mensaje.textContent = '';
                renderizarCarrito();
                renderizarProductos();
            });
            quitar.setAttribute('aria-label', `Quitar ${linea.nombre} del carrito`);
            acciones.append(quitar);
            fila.append(celdaCantidad, subtotal, acciones);
            lineas.append(fila);
        }
        actualizarTotal();
    }

    function agregarProducto(producto) {
        const existente = carrito.get(producto.publicId);
        const cantidad = (existente?.cantidad ?? 0) + 1;
        if (cantidad > producto.stock) {
            mensaje.textContent = `No puede agregar más unidades de ${producto.nombre}: stock disponible ${producto.stock}.`;
            return;
        }

        carrito.set(producto.publicId, { ...producto, cantidad });
        mensaje.textContent = '';
        renderizarCarrito();
        renderizarProductos();
    }

    function renderizarProductos() {
        const resultados = document.getElementById('resultados-productos');
        resultados.replaceChildren();
        for (const producto of productosEncontrados) {
            const item = crearElemento('li', undefined, 'venta-resultado');
            const informacion = crearElemento('div');
            informacion.append(
                crearElemento('strong', producto.nombre),
                crearElemento('small', `${mostrarImporte(precioEnCentavos(producto.precioVenta))} · Stock: ${producto.stock}`)
            );
            const cantidad = carrito.get(producto.publicId)?.cantidad ?? 0;
            const agregar = crearBoton(producto.stock === 0 ? 'Sin stock' : 'Agregar',
                'btn btn--primary', () => agregarProducto(producto));
            agregar.disabled = cantidad >= producto.stock;
            agregar.setAttribute('aria-label', `Agregar ${producto.nombre} al carrito`);
            if (agregar.disabled) agregar.title = 'No hay más unidades disponibles para agregar.';
            item.append(informacion, agregar);
            resultados.append(item);
        }
    }

    function actualizarCliente() {
        document.getElementById('cliente-seleccionado').textContent = clienteSeleccionado
            ? `${clienteSeleccionado.razonSocial} · CI/NIT: ${clienteSeleccionado.ciNit}`
            : 'Ningún cliente seleccionado.';
        document.getElementById('quitar-cliente').hidden = clienteSeleccionado === null;
    }

    function renderizarClientes(clientes) {
        const resultados = document.getElementById('resultados-clientes');
        resultados.replaceChildren();
        for (const cliente of clientes) {
            const item = crearElemento('li', undefined, 'venta-resultado');
            const informacion = crearElemento('div');
            informacion.append(
                crearElemento('strong', cliente.razonSocial),
                crearElemento('small', `CI/NIT: ${cliente.ciNit}`)
            );
            const seleccionar = crearBoton('Seleccionar', 'btn btn--secondary', () => {
                clienteSeleccionado = cliente;
                actualizarCliente();
            });
            seleccionar.setAttribute('aria-label', `Seleccionar a ${cliente.razonSocial}, CI/NIT ${cliente.ciNit}`);
            item.append(informacion, seleccionar);
            resultados.append(item);
        }
    }

    function conectarBuscador(formularioId, campoId, estadoId, resultadosId, url, parametro, renderizar, ayuda, sinResultados, opciones = {}) {
        const formulario = document.getElementById(formularioId);
        const campo = document.getElementById(campoId);
        const estado = document.getElementById(estadoId);
        const resultados = document.getElementById(resultadosId);
        let temporizador;
        let controlador;
        let version = 0;

        function cancelarBusqueda() {
            clearTimeout(temporizador);
            controlador?.abort();
            ++version;
            renderizar([]);
            resultados.setAttribute('aria-busy', 'false');
            opciones.alIniciar?.();
        }

        function programarBusqueda(demora) {
            cancelarBusqueda();
            const versionActual = version;
            const texto = campo.value.trim();
            if (!texto) {
                estado.textContent = ayuda;
                return;
            }

            estado.textContent = 'Buscando…';
            resultados.setAttribute('aria-busy', 'true');
            temporizador = setTimeout(async () => {
                controlador = new AbortController();
                try {
                    const destino = new URL(url, window.location.origin);
                    destino.searchParams.set(parametro, texto);
                    const respuesta = await fetch(destino, {
                        signal: controlador.signal,
                        credentials: 'same-origin',
                        cache: 'no-store',
                        headers: { Accept: 'application/json' }
                    });
                    if (!respuesta.ok || respuesta.redirected) throw new Error('No se pudo realizar la búsqueda.');
                    const datos = await respuesta.json();
                    if (versionActual !== version) return;
                    renderizar(datos);
                    estado.textContent = datos.length > 0
                        ? `${datos.length} resultado(s).`
                        : sinResultados;
                    opciones.alFinalizar?.(datos, texto);
                } catch (error) {
                    if (versionActual !== version || error.name === 'AbortError') return;
                    estado.textContent = 'No se pudo realizar la búsqueda. Revise su conexión y sesión e intente nuevamente.';
                } finally {
                    if (versionActual === version) resultados.setAttribute('aria-busy', 'false');
                }
            }, demora);
        }

        formulario.addEventListener('submit', event => {
            event.preventDefault();
            programarBusqueda(0);
        });
        campo.addEventListener('input', () => programarBusqueda(250));
        return cancelarBusqueda;
    }

    conectarBuscador('buscar-producto', 'producto-nombre', 'estado-productos', 'resultados-productos',
        registro.dataset.productosUrl, 'nombre', productos => {
            productosEncontrados = productos;
            renderizarProductos();
        }, 'Escriba un nombre para buscar productos.', 'No se encontraron productos con ese nombre.');
    const cancelarBusquedaClientes = conectarBuscador('buscar-cliente', 'cliente-ci-nit', 'estado-clientes', 'resultados-clientes',
        registro.dataset.clientesUrl, 'ciNit', renderizarClientes,
        'Escriba el CI/NIT para buscar un cliente.', 'No se encontraron clientes con ese CI/NIT.', {
            alIniciar: () => {
                ciNitSinResultados = '';
                botonAltaCliente.hidden = true;
            },
            alFinalizar: (clientes, ciNit) => {
                ciNitSinResultados = clientes.length === 0 ? ciNit : '';
                botonAltaCliente.hidden = clientes.length !== 0;
            }
        });

    function limpiarErroresAlta() {
        errorAltaCliente.textContent = '';
        errorAltaCliente.hidden = true;
        for (const error of erroresAltaCliente) {
            error.textContent = '';
            formularioAltaCliente.elements.namedItem(error.dataset.clienteError)?.removeAttribute('aria-invalid');
        }
    }

    function mostrarErroresAlta(errores) {
        limpiarErroresAlta();
        const generales = [];
        for (const [campo, texto] of Object.entries(errores)) {
            const destino = erroresAltaCliente.find(error => error.dataset.clienteError === campo);
            if (destino) {
                destino.textContent = texto;
                formularioAltaCliente.elements.namedItem(campo)?.setAttribute('aria-invalid', 'true');
            } else {
                generales.push(texto);
            }
        }
        errorAltaCliente.textContent = generales.length > 0
            ? generales.join(' ')
            : 'Revise los campos señalados.';
        errorAltaCliente.hidden = false;
    }

    function enfocarErrorAlta() {
        const campo = erroresAltaCliente.find(error => error.textContent !== '');
        if (campo) {
            formularioAltaCliente.elements.namedItem(campo.dataset.clienteError).focus();
        } else {
            errorAltaCliente.focus();
        }
    }

    // Las reglas se comprueban con ClienteValidator en el servidor y se muestran en el modal.
    formularioAltaCliente.noValidate = true;
    botonAltaCliente.addEventListener('click', event => {
        if (guardandoCliente || !ciNitSinResultados) {
            event.preventDefault();
            event.stopImmediatePropagation();
            return;
        }
        formularioAltaCliente.reset();
        limpiarErroresAlta();
        estadoAltaCliente.textContent = '';
        formularioAltaCliente.elements.namedItem('Input.CiNit').value = ciNitSinResultados;
        // El componente compartido abre el diálogo con este mismo botón.
        requestAnimationFrame(() => {
            if (modalAltaCliente.open) formularioAltaCliente.elements.namedItem('Input.CiNit').focus();
        });
    });

    // Evitar cerrar el modal mientras el servidor procesa el registro.
    function impedirCierreDuranteGuardado(event) {
        if (guardandoCliente) {
            event.preventDefault();
            event.stopImmediatePropagation();
        }
    }
    modalAltaCliente.addEventListener('click', impedirCierreDuranteGuardado, true);
    // Un clic activado por teclado no tiene coordenadas de puntero: no es un clic en el fondo.
    modalAltaCliente.addEventListener('click', event => {
        if (event.detail === 0 && event.target !== modalAltaCliente) event.stopImmediatePropagation();
    });
    modalAltaCliente.addEventListener('cancel', impedirCierreDuranteGuardado);
    modalAltaCliente.addEventListener('keydown', event => {
        if (event.key === 'Escape') impedirCierreDuranteGuardado(event);
    }, true);

    formularioAltaCliente.addEventListener('submit', async event => {
        event.preventDefault();
        if (guardandoCliente) return;

        // Tomar los datos y el token antifalsificación antes de deshabilitar los controles.
        const datos = new FormData(formularioAltaCliente);
        const controles = Array.from(formularioAltaCliente.querySelectorAll('input:not([type="hidden"]), button'));
        const estadosPrevios = controles.map(control => control.disabled);
        guardandoCliente = true;
        let guardado = false;
        controles.forEach(control => { control.disabled = true; });
        formularioAltaCliente.setAttribute('aria-busy', 'true');
        limpiarErroresAlta();
        estadoAltaCliente.textContent = 'Guardando cliente…';

        try {
            const respuesta = await fetch(formularioAltaCliente.action, {
                method: 'POST',
                body: datos,
                credentials: 'same-origin',
                cache: 'no-store',
                headers: { Accept: 'application/json' }
            });
            if (respuesta.redirected || respuesta.status === 401 || respuesta.status === 403) {
                throw new Error('La sesión no está disponible.');
            }
            const resultado = await respuesta.json();
            if (!respuesta.ok || !resultado.exitoso || !resultado.cliente) {
                mostrarErroresAlta(resultado.errores && Object.keys(resultado.errores).length > 0
                    ? resultado.errores
                    : { '': 'No se pudo registrar el cliente. Intente nuevamente.' });
                return;
            }

            cancelarBusquedaClientes();
            clienteSeleccionado = resultado.cliente;
            campoBusquedaCliente.value = resultado.cliente.ciNit;
            renderizarClientes([resultado.cliente]);
            actualizarCliente();
            document.getElementById('estado-clientes').textContent = 'Cliente registrado y seleccionado.';
            guardado = true;
            modalAltaCliente.close();
        } catch {
            mostrarErroresAlta({ '': 'No se pudo confirmar el registro. Revise su conexión y sesión, y busque el CI/NIT antes de volver a guardar.' });
        } finally {
            guardandoCliente = false;
            controles.forEach((control, indice) => { control.disabled = estadosPrevios[indice]; });
            formularioAltaCliente.setAttribute('aria-busy', 'false');
            estadoAltaCliente.textContent = '';
            if (guardado) {
                document.getElementById('cliente-seleccionado').focus();
            } else {
                enfocarErrorAlta();
            }
        }
    });

    document.getElementById('quitar-cliente').addEventListener('click', () => {
        clienteSeleccionado = null;
        actualizarCliente();
    });
    actualizarCliente();
    renderizarCarrito();
})();
