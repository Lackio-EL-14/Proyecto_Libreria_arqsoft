# Pruebas manuales en el navegador — auditoría del 20/11/2026

Lo que la auditoría no pudo comprobar «con los ojos» desde la consola: aspecto, modales, mensajes en pantalla y textos de Keycloak. Cada integrante ejecuta su bloque y marca la casilla. Si algo falla, anote la observación y abra la tarjeta de bug a su dueño.

## Preparación común (una sola vez, la hace Edson)

1. `git checkout auditoria-microservicios` (o `develop` una vez mergeado el PR).
2. `.env` completo, copiado de `.env.example`. **No compartir sus valores por chat.**
3. Desde cero:

   ```
   docker compose down -v
   docker compose up -d
   ```

   En `docker compose ps -a`, las 4 BD y `keycloak` deben estar «healthy», y `proveedores-db-init`, `usuarios-db-init` y `keycloak-configurar` en «Exited (0)».
4. `./run-all.ps1` → la tabla muestra «OK» en los 6 proyectos.
5. Abrir `http://localhost:5000` en una **ventana privada** del navegador (una por usuario, para no mezclar sesiones).

**Usuarios semilla:** `admin` (Administrador) y `vendedor` (Vendedor). Sus contraseñas son `KEYCLOAK_ADMIN_SEMILLA_PASSWORD` y `KEYCLOAK_VENDEDOR_SEMILLA_PASSWORD` del `.env`.

**Datos semilla:**

| Entidad | Registros |
|---|---|
| Clientes | «Universidad Mayor», «María Fernanda Quispe Choque», «Juan Carlos Pérez Rojas» |
| Proveedores | «Distribuidora Escolar Andina», «Editorial del Sur S.R.L.», «Importadora Técnica Nacional» |
| Productos | 5, entre ellos «Cuaderno Espiral 100 hojas» y «Calculadora Científica» |
| Categorías | 3, entre ellas «Material escolar» |
| Marcas | 3, entre ellas «Faber-Castell» |

**Correos de prueba (Development):** quedan como `.eml` en `src/Libreria.Servicio.Usuarios/bin/Development/mail/`. Ábralos con un editor de texto.

Formato de cada prueba: **N.º · Responsable · Precondición · Pasos · Resultado esperado · OK / Falla + observación**.

---

## A. Edson — Clientes, gateway y arranque

| N.º | Precondición | Pasos | Resultado esperado | OK / Falla + obs. |
|---|---|---|---|---|
| A1 | Sesión `vendedor` | Menú Clientes → listado | Se ven los 3 clientes semilla activos; columnas con encabezados que ordenan (▲/▼) | ☐ OK ☐ Falla: |
| A2 | A1 | Clic en «Razón social» dos veces | Orden ascendente y luego descendente; la flecha cambia | ☐ OK ☐ Falla: |
| A3 | A1 | Buscar «Quispe» y luego «Limpiar» | Filtra a «María Fernanda Quispe Choque»; Limpiar restablece | ☐ OK ☐ Falla: |
| A4 | A1 | «+ Registrar nuevo cliente» con todo vacío → Guardar | Mensajes en rojo por campo: «El CI/NIT es obligatorio.», «La razón social es obligatoria.» | ☐ OK ☐ Falla: |
| A5 | A1 | Registrar CI/NIT `9990001`, razón social «  juan   josé  pérez  », correo válido | Se guarda como «juan josé pérez» (sin espacios sobrantes) y aparece en el listado | ☐ OK ☐ Falla: |
| A6 | A5 | Registrar otro cliente con el mismo CI/NIT | Mensaje «Ya existe un cliente registrado con ese CI/NIT.» | ☐ OK ☐ Falla: |
| A7 | Sesión `admin` | Editar el cliente de A5 → cambiar la razón social → Guardar; abrir Editar de nuevo | El bloque «Trazabilidad» muestra «Registrado por Vendedor Mostrador» y «Última modificación por Administrador Sistema», **con la hora de Bolivia** (no 4 h adelantada) | ☐ OK ☐ Falla: |
| A8 | A7 | «Dar de baja» en el listado | Se abre un **modal** propio (no `confirm()` del navegador); al confirmar, mensaje de éxito y el cliente sale de «Activos» | ☐ OK ☐ Falla: |
| A9 | A8 | Filtro «Inactivos» → «Reactivar» | Modal de reactivación; vuelve a «Activos» | ☐ OK ☐ Falla: |
| A10 | Sesión `vendedor` | Buscar los botones de baja y reactivar | El vendedor **no** ve dar de baja ni reactivar (solo Administrador) | ☐ OK ☐ Falla: |
| A11 | Todo arriba | Botón «Actualizar» del listado | Recarga con los mismos filtros y orden | ☐ OK ☐ Falla: |

## B. Cesar — Proveedores

| N.º | Precondición | Pasos | Resultado esperado | OK / Falla + obs. |
|---|---|---|---|---|
| B1 | Sesión `admin` | Menú Abastecimiento → Proveedores | 3 proveedores semilla, con encabezados que ordenan, búsqueda (incluye el contacto) y filtro de estado | ☐ OK ☐ Falla: |
| B2 | B1 | Registrar con todo vacío | «El NIT es obligatorio.», **«La razón social es obligatoria.»**, «El nombre del contacto es obligatorio.», «Ingrese al menos un teléfono o correo para el contacto principal.» | ☐ OK ☐ Falla: |
| B3 | B1 | Registrar NIT `7770001`, razón social «Librería Prueba», contacto «  Ana   María  Rojas », correo del contacto válido | Se guarda; el contacto queda «Ana María Rojas» | ☐ OK ☐ Falla: |
| B4 | B3 | Registrar otro con el mismo NIT | «Ya existe un proveedor registrado con ese NIT.» | ☐ OK ☐ Falla: |
| B5 | B3 | Editar el cargo del contacto y guardar; volver a Editar | Se ve el cambio y la Trazabilidad con nombres y hora local | ☐ OK ☐ Falla: |
| B6 | B3 | Dar de baja (modal) → intentar editar desde «Inactivos» | Mensaje «El proveedor está inactivo. Reactívelo antes de editarlo.» | ☐ OK ☐ Falla: |
| B7 | Sesión `vendedor` | Escribir `http://localhost:5000/Proveedores` en la barra | Va a «Acceso denegado» (y el menú no muestra Abastecimiento) | ☐ OK ☐ Falla: |

## C. Josue — Productos, categorías y marcas

| N.º | Precondición | Pasos | Resultado esperado | OK / Falla + obs. |
|---|---|---|---|---|
| C1 | Sesión `admin` | Inventario → Productos | 5 productos; filtros por texto, categoría, marca y estado; botón Actualizar | ☐ OK ☐ Falla: |
| C2 | C1 | Clic en «Precio venta» y luego otra vez | Ordena por precio ascendente y luego descendente (el orden lo hace el MS: en la URL se ven `Orden` y `Direccion`) | ☐ OK ☐ Falla: |
| C3 | C1 | Registrar un producto con todo vacío o en negativo | Mensajes por campo (nombre, stock, precio, costo, categoría, marca) | ☐ OK ☐ Falla: |
| C4 | C1 | Registrar «Cuaderno Espiral 100 hojas» (nombre repetido) | «Ya existe un producto con ese nombre.» | ☐ OK ☐ Falla: |
| C5 | C1 | Precio de venta menor al costo | Modal «Confirmar precio de venta» (propio, no del navegador) | ☐ OK ☐ Falla: |
| C6 | C1 | Editar el costo de un producto → Detalle | El histórico de costos muestra la nueva fila con «Registrado por» = nombre y la hora local | ☐ OK ☐ Falla: |
| C7 | C1 | Dar de baja (modal) → filtro Inactivos → **Reactivar** | Reactivar ahora **abre un modal de confirmación**; al confirmar, mensaje de éxito | ☐ OK ☐ Falla: |
| C8 | C1 | Inventario → Categorías y → Marcas | Solo listado y búsqueda: **sin** «Registrar», «Editar» ni «Dar de baja» | ☐ OK ☐ Falla: |
| C9 | Sesión `vendedor` | `http://localhost:5000/Productos` | Acceso denegado; el Panel principal se ve sin error (contadores en cero) | ☐ OK ☐ Falla: |

## D. Gabriel — Login, roles, JWT y Keycloak

| N.º | Precondición | Pasos | Resultado esperado | OK / Falla + obs. |
|---|---|---|---|---|
| D1 | Sin sesión | Abrir `http://localhost:5000` | Pantalla de Keycloak en español: **«Iniciar sesión en Librería»** e **«Ingrese a su cuenta»** (sin «tu»). Revisar la ortografía de cada texto | ☐ OK ☐ Falla: |
| D2 | D1 | Contraseña incorrecta | Mensaje en español «Usuario o contraseña incorrectos.» | ☐ OK ☐ Falla: |
| D3 | D1 | Entrar como `admin` | Menú: General, Clientes, Abastecimiento, Inventario, Administración | ☐ OK ☐ Falla: |
| D4 | D1 | Entrar como `vendedor` (otra ventana privada) | Menú: General y Clientes solamente | ☐ OK ☐ Falla: |
| D5 | Sesión `admin` | «Cerrar sesión» y luego botón «Atrás» del navegador | Vuelve al login con «Cerró su sesión correctamente.»; no se puede ver ninguna página sin volver a ingresar | ☐ OK ☐ Falla: |
| D6 | Swagger | Poner `KEYCLOAK_HABILITAR_CLIENTE_PRUEBAS=true`, ejecutar `docker compose run --rm keycloak-configurar` y obtener un token (`keycloak.md` §5). En `http://localhost:5103/swagger`, «Authorize» | Con token → 200; sin token → 401. **Volver a poner `false`** y repetir `keycloak-configurar` | ☐ OK ☐ Falla: |
| D7 | Navegador F12 → Red | En `/Clientes`, revisar las peticiones del navegador | Las llamadas al gateway no salen del navegador (las hace el servidor Razor); en el navegador solo se ve la cookie de sesión | ☐ OK ☐ Falla: |

## E. Mauricio — Listado de usuarios, cambio de contraseña y bitácora

| N.º | Precondición | Pasos | Resultado esperado | OK / Falla + obs. |
|---|---|---|---|---|
| E1 | Sesión `admin` | Administración → Usuarios | Columnas: nombre completo, usuario, correo, rol y estado; **sin contraseña**. Encabezados que ordenan y botón Actualizar | ☐ OK ☐ Falla: |
| E2 | E1 | Ver el código fuente (Ctrl+U) | No aparece ninguna contraseña, hash ni id numérico | ☐ OK ☐ Falla: |
| E3 | Sesión `vendedor` | Botón «Cambiar contraseña» de la barra superior. Escribir una nueva débil (`abc`) | Las 5 reglas se marcan «Pendiente» y el botón **sigue deshabilitado** | ☐ OK ☐ Falla: |
| E4 | E3 | Nueva válida y confirmación distinta | «Las contraseñas no coinciden.»; el botón sigue deshabilitado | ☐ OK ☐ Falla: |
| E5 | E3 | Contraseña actual incorrecta con nueva válida (dos veces) → Cambiar | «La contraseña actual es incorrecta.» y la sesión sigue abierta | ☐ OK ☐ Falla: |
| E6 | E3 | Actual correcta y nueva válida dos veces → Cambiar | **Se cierra la sesión** y el login muestra «Su contraseña se cambió correctamente. Inicie sesión con la nueva contraseña.» | ☐ OK ☐ Falla: |
| E7 | E6 | Entrar con la contraseña anterior y después con la nueva | La anterior falla; la nueva entra | ☐ OK ☐ Falla: |
| E8 | E6 | **Restaurar** la contraseña del `.env`: `docker compose run --rm keycloak-configurar` | El vendedor vuelve a entrar con la contraseña del `.env` | ☐ OK ☐ Falla: |
| E9 | Clientes y Productos editados | Abrir la Trazabilidad de un cliente, un proveedor y el detalle de un producto | Nombres (no GUID) y hora local correcta | ☐ OK ☐ Falla: |

## F. Mateo — Alta de usuario, correo y primer ingreso (y pase de ortografía)

| N.º | Precondición | Pasos | Resultado esperado | OK / Falla + obs. |
|---|---|---|---|---|
| F1 | Sesión `admin` | Usuarios → «+ Registrar usuario» con todo vacío | Mensajes en español, incluido «El apellido paterno es obligatorio.» | ☐ OK ☐ Falla: |
| F2 | F1 | Nombre «  juan   josé », paterno «pérez», materno «rojas», un correo nuevo, rol Vendedor | Mensaje de éxito con el usuario **`jperezr`**; la contraseña **no** aparece en pantalla | ☐ OK ☐ Falla: |
| F3 | F2 | Registrar «Juan» «Pérez» (sin materno) dos veces con correos distintos | Usuarios `jperez` y `jperez2` | ☐ OK ☐ Falla: |
| F4 | F2 | Repetir el correo de F2 | «Ya existe un usuario registrado con ese correo.» | ☐ OK ☐ Falla: |
| F5 | F2 | Abrir el `.eml` más reciente | Asunto «Acceso temporal - Sistema Librería»; texto en «usted»; usuario y contraseña temporal de 12 caracteres con mayúscula, minúscula, número y especial; enlace a `http://localhost:5000`. Revisar ortografía | ☐ OK ☐ Falla: |
| F6 | F5 | Ventana privada nueva: entrar con `jperezr` y la temporal | Keycloak muestra **«Tiene que cambiar su contraseña para activar su cuenta.»** con «Nueva contraseña» y «Confirme la contraseña». No hay forma de saltarla | ☐ OK ☐ Falla: |
| F7 | F6 | En otra pestaña de esa ventana, ir a `http://localhost:5000/Clientes` sin completar el cambio | Vuelve a Keycloak; no entra a ningún apartado | ☐ OK ☐ Falla: |
| F8 | F6 | Poner `abc` como nueva | «La contraseña debe tener al menos 8 caracteres.» (sin «Contraseña incorrecta») | ☐ OK ☐ Falla: |
| F9 | F6 | Nueva válida dos veces | Entra al Panel con el menú de Vendedor | ☐ OK ☐ Falla: |
| F10 | F9, sesión `admin` | Usuarios → Actualizar | `jperezr` aparece **Activo**; los que no ingresaron siguen **Pendiente** | ☐ OK ☐ Falla: |
| F11 | Sesión `admin` | Crear un usuario **Administrador**, hacer su primer ingreso y entrar a Usuarios con él | Ve el listado y puede crear usuarios (antes recibía «Acceso denegado») | ☐ OK ☐ Falla: |
| F12 | Sesión `admin` | Dar de baja a `jperez2` (modal) e intentar entrar con él | Estado Inactivo; Keycloak no lo deja entrar | ☐ OK ☐ Falla: |
| F13 | — | **Pase de ortografía**: recorrer TODAS las páginas (Panel, Clientes, Proveedores, Productos, Categorías, Marcas, Histórico, Usuarios, Registrar usuario, Cambiar contraseña, Acceso denegado, Error, Servicio no disponible, Login y las pantallas de Keycloak) | Sin faltas: tildes, ñ, ¿¡, mayúsculas, y «usted» en todo | ☐ OK ☐ Falla: |

## G. Todos (lo coordina Cesar, regresión US-136) — Servicio caído

| N.º | Precondición | Pasos | Resultado esperado | OK / Falla + obs. |
|---|---|---|---|---|
| G1 | Sesión `admin` | Cerrar la consola o el proceso de **Proveedores** (puerto 5101) → abrir Proveedores en el Front | Página «Servicio no disponible» con «El servicio de Proveedores no respondió a tiempo.» y «Volver al inicio»; **sin** pantalla de error técnico | ☐ OK ☐ Falla: |
| G2 | G1 | Ir a Clientes y Productos | Funcionan con normalidad | ☐ OK ☐ Falla: |
| G3 | G1 | Con Productos detenido, intentar dar de baja un producto (dejar abierta la página antes de detenerlo) | Mensaje «El servicio de Productos no está disponible en este momento. Intente nuevamente en unos minutos.» | ☐ OK ☐ Falla: |
| G4 | — | Volver a iniciar los procesos (`./run-all.ps1`) | Todo responde | ☐ OK ☐ Falla: |
| G5 | Usuarios detenido | Recargar cualquier página con sesión | Login con «No se pudo validar su sesión porque el servicio de Usuarios no responde…» | ☐ OK ☐ Falla: |

---

**Cierre:** cuando todos los bloques estén en OK, Edson y Gabriel ejecutan US-137 (merge a `main` y tag `v4-microservicios`).
