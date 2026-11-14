# Datos semilla con GUID fijos

> Contrato definido en US-111. Los scripts de cada BD (`db/<servicio>/50_datos_semilla.*`) usan **estos mismos** `PublicId`.
> Así, una venta semilla apunta a un cliente, unos productos y unos usuarios que existen de verdad en sus BD, sin FK entre servicios.
> Cambiar un GUID es cambiar un contrato: se avisa al equipo antes del PR.

Cada tipo tiene un prefijo propio para reconocerlo a simple vista:

| Prefijo | Entidad | BD |
|---|---|---|
| `a0000000-…` | Usuario | `LibreriaUsuarios` (SQL Server, `usuarios-db`) |
| `c0000000-…` | Cliente | `libreria_clientes` (MySQL, `clientes-db`) |
| `ca000000-…` | Categoría | `libreria_productos` (MongoDB, `productos-db`) |
| `b0000000-…` | Marca | `libreria_productos` |
| `d0000000-…` | Producto | `libreria_productos` |
| `e0000000-…` | Venta | `LibreriaVentas` (SQL Server, `ventas-db`) |

## Usuarios

| PublicId | Usuario | Contraseña | Rol | Nombre | Apellido paterno | Iniciales | Estado |
|---|---|---|---|---|---|---|---|
| `a0000000-0000-0000-0000-000000000001` | `admin` | `Admin2026!` | Administrador | Administrador | Sistema | `A.S.` | Activo |
| `a0000000-0000-0000-0000-000000000002` | `vendedor` | `Vendedor2026!` | Vendedor | Vendedor | Mostrador | `V.M.` | Activo |

`UsuarioActualDesarrollo` (sección 6.4) devuelve el PublicId de `admin`, el mismo que ya está en `appsettings.Development.json` de Clientes y Productos.

## Clientes

| PublicId | CI/NIT | Razón social | Correo | Estado |
|---|---|---|---|---|
| `c0000000-0000-0000-0000-000000000001` | `9999999` | Universidad Mayor | `compras@um.edu.bo` | Activo |
| `c0000000-0000-0000-0000-000000000002` | `4567890` | María Fernanda Quispe Choque | `maria.quispe@correo.bo` | Activo |
| `c0000000-0000-0000-0000-000000000003` | `1234567` | Juan Carlos Pérez Rojas | — | **Inactivo** (sirve para probar que no aparece en `buscar`) |

## Categorías

| PublicId | Código | Nombre | Ubicación |
|---|---|---|---|
| `ca000000-0000-0000-0000-000000000001` | `CAT-001` | Material escolar | Estante principal |
| `ca000000-0000-0000-0000-000000000002` | `CAT-002` | Libros de texto | Bodega |
| `ca000000-0000-0000-0000-000000000003` | `CAT-003` | Arte y manualidades | Vitrina |

El contador `contadores._id = "categoria"` empieza en `3`: la siguiente categoría recibe `CAT-004`.

## Marcas

| PublicId | Nombre | País de origen |
|---|---|---|
| `b0000000-0000-0000-0000-000000000001` | Faber-Castell | Alemania |
| `b0000000-0000-0000-0000-000000000002` | Editorial Santillana | España |
| `b0000000-0000-0000-0000-000000000003` | Acrilex | Brasil |

## Productos

| PublicId | Nombre | Categoría | Marca | Stock | Precio | Costo |
|---|---|---|---|---|---|---|
| `d0000000-0000-0000-0000-000000000001` | Cuaderno Espiral 100 hojas | Material escolar | Faber-Castell | 150 | 15.00 | 10.00 |
| `d0000000-0000-0000-0000-000000000002` | Caja Bolígrafos Azules x12 | Material escolar | Faber-Castell | 80 | 25.00 | 18.00 |
| `d0000000-0000-0000-0000-000000000003` | Libro Matemáticas 5to Sec | Libros de texto | Editorial Santillana | 40 | 120.00 | 90.00 |
| `d0000000-0000-0000-0000-000000000004` | Historia Universal 2026 | Libros de texto | Editorial Santillana | 25 | 180.00 | 130.00 |
| `d0000000-0000-0000-0000-000000000005` | Calculadora Científica | Material escolar | Faber-Castell | 30 | 85.00 | 60.00 |

Cada producto trae una entrada en `historicoCostos` con motivo «Registro inicial». El stock semilla es el stock vigente; no se recalcula con las ventas de abajo.

## Ventas

| PublicId | Fecha | Cliente | Líneas | Total | Estado | Registró | Anuló |
|---|---|---|---|---|---|---|---|
| `e0000000-0000-0000-0000-000000000001` | 02/11/2026 10:15 | `c…0001` Universidad Mayor | 1 × `d…0001` · 1 × `d…0003` | 135.00 | Activa | `admin` | — |
| `e0000000-0000-0000-0000-000000000002` | 03/11/2026 16:40 | `c…0002` María Fernanda Quispe Choque | 2 × `d…0001` | 30.00 | Activa | `vendedor` | — |
| `e0000000-0000-0000-0000-000000000003` | 04/11/2026 09:05 | `c…0001` Universidad Mayor | 1 × `d…0005` | 85.00 | Anulada | `vendedor` | `admin` |

Cada venta guarda copia de `ClienteCiNit`, `ClienteRazonSocial`, `ProductoNombre` y `VendedorIniciales`. `HistoricoMovimientoStock` tiene un movimiento «Venta» por línea y uno «Anulacion» para la venta anulada.
