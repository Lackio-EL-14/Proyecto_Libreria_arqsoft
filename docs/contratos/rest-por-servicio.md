# REST por microservicio

Todas las rutas se llaman a través del gateway (`http://localhost:5080`). «Hoy» indica qué proceso las atiende en `develop` al publicar este contrato (US-113).

## Clientes — MS Clientes (5103) · dueño: Edson

Atendido por el MS Clientes desde US-115 (`ClientesController`). Errores según [rest-errores-result.md](rest-errores-result.md); BD caída → 503.

| Endpoint | Respuesta | Historia |
|---|---|---|
| `GET api/clientes?busqueda=&estado=&orden=&descendente=` | 200 `ClienteDetalle[]` (`publicId, ciNit, razonSocial, correo, estado, fechaCreacion, usuarioCreacionId, fechaModificacion, usuarioModificacionId`). `estado`: sin enviar = activos · `false` = inactivos · vacío (`estado=`) = todos. `orden`: `RazonSocial` (defecto), `CiNit`, `Correo`, `FechaCreacion`; otro valor → 400 | US-115 |
| `GET api/clientes/{publicId}` | 200 `ClienteDetalle` · 404 | US-115 |
| `GET api/clientes/buscar?ciNit=` (para la venta; solo **activos**, por prefijo) | 200 `[{ publicId, ciNit, razonSocial }]` (`ClienteVentaItem` + `PublicId`) · 400 sin `ciNit`. Reemplaza `api/ventas/busquedas/clientes` | US-115 |
| `POST api/clientes` `{ ciNit, razonSocial, correo }` (alta y alta rápida de la venta) | **201** + `Location`, cuerpo `ClienteRegistroResult` `{ exitoso, cliente: { publicId, ciNit, razonSocial }, errores }` (misma forma que antes, con `publicId` de más) · 400 por campo · 409 CI/NIT duplicado (`errors.CiNit`) | US-115 |
| `PUT api/clientes/{publicId}` `{ ciNit, razonSocial, correo }` | 200 `ClienteDetalle` · 400 · 404 · 409 (CI/NIT de otro cliente o cliente inactivo) | US-115 |
| `PATCH api/clientes/{publicId}/estado` `{ "activo": false \| true }` | 204 · 400 sin `activo` · 404. `false` = delete lógico (la fila queda); `true` = reactivar | US-115 |

## Ventas — MS Ventas (5101) · dueño: Cesar

| Endpoint | Estado | Historia |
|---|---|---|
| `GET api/ventas?estado=&fechaDesde=&fechaHasta=` | Existe | — |
| `GET api/ventas/{publicId}?esAdministrador=` | Existe | — |
| `GET api/ventas/{publicId}/comprobante` | Existe | — |
| `POST api/ventas` | Existe | US-118 (cruza a Productos y Clientes) |
| `POST api/ventas/{publicId}/anular` (`{ motivo }`) | Existe | US-118 |
| `PUT api/ventas/{publicId}` (corregir cliente y observación) | Nuevo | US-117, US-119 |
| `GET api/ventas/busquedas/productos?nombre=` · `GET api/ventas/busquedas/clientes?ciNit=` | **Sale** (`VentasBusquedasController`): pasan a `api/productos/buscar` y `api/clientes/buscar` (este último ya existe, US-115) | US-117 |

## Productos — MS Productos (5104) · dueño: Josue

| Endpoint | Estado | Historia |
|---|---|---|
| `GET api/categorias?busqueda=` · `GET api/categorias/{publicId}?estadoEsperado=` · `GET api/categorias/ubicaciones` | Existe (hoy en Ventas) | US-122 |
| `POST api/categorias` · `PUT api/categorias/{publicId}` · `PATCH api/categorias/{publicId}/estado?nuevoEstado=` | Existe (hoy en Ventas) | US-122 |
| `GET api/marcas?nombre=` · `GET api/marcas/{publicId}?estadoEsperado=` · `GET api/marcas/paises` | Existe (hoy en Ventas) | US-122 |
| `POST api/marcas` · `PUT api/marcas/{publicId}` · `PATCH api/marcas/{publicId}/estado?nuevoEstado=` | Existe (hoy en Ventas) | US-122 |
| `GET api/productos?busqueda=&categoriaId=&marcaId=` · `GET api/productos/formulario` | Existe (hoy en Ventas) | US-122 |
| `GET api/productos/{publicId}` · `GET api/productos/{publicId}/edicion` | Existe (hoy en Ventas) | US-122 |
| `POST api/productos` · `PUT api/productos/{publicId}` · `PATCH api/productos/{publicId}/baja` | Existe (hoy en Ventas) | US-122 |
| `GET api/historico/{publicId}` | Existe (hoy en Ventas) | US-122 |
| `GET api/productos/buscar?termino=` (para la venta; mismo DTO que `ProductoVentaItem`) | Nuevo | US-122 |
| `POST api/productos/{publicId}/stock/descontar` y `/stock/restaurar` con `{ cantidad, ventaPublicId }` → 200 / 409 | Nuevo | US-122 |

## Usuarios — MS Usuarios (5102) · dueño: Gabriel

| Endpoint | Estado | Historia |
|---|---|---|
| `POST api/auth/login` → `{ token, expiraEn, usuario }` (anónima) | Cambia (hoy devuelve el usuario sin token) | US-125 |
| `GET api/usuarios?estado=` · `GET api/usuarios/roles` · `POST api/usuarios` · `DELETE api/usuarios/{publicId}` | Existe | US-128, US-130 |
| `PUT api/usuarios/contrasena` | Existe | US-129, US-131 |
| `GET api/usuarios/nombres?ids=` y `GET api/usuarios/{id}/sesion-activa` | Cambian: reciben GUID | US-124, US-125, US-127 |
| `POST api/usuarios/activar` · `POST api/usuarios/{publicId}/reenviar-activacion` | **Salen** | US-124 |

## Todos

| Endpoint | Contrato |
|---|---|
| `GET /salud` | `{ "servicio": "<Nombre>", "estado": "ok" }`, en cada MS y en el gateway |
