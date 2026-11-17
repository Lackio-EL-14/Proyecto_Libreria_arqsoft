# Datos semilla con identificadores fijos

Los scripts `db/<servicio>/50_datos_semilla.*` son idempotentes y usan estos prefijos:

| Prefijo | Entidad | Base |
|---|---|---|
| `a0000000-…` | Usuario | `LibreriaUsuarios` |
| `c0000000-…` | Cliente | `libreria_clientes` |
| `f0000000-…` | Proveedor | `LibreriaProveedores` |
| `ca000000-…` | Categoría | `libreria_productos` |
| `b0000000-…` | Marca | `libreria_productos` |
| `d0000000-…` | Producto | `libreria_productos` |

## Proveedores

| PublicId | NIT | Razón social | Contacto principal |
|---|---|---|---|
| `f0000000-0000-0000-0000-000000000001` | `1020304050` | Distribuidora Escolar Andina | Ana Pérez |
| `f0000000-0000-0000-0000-000000000002` | `2040608011` | Editorial del Sur S.R.L. | Carlos Suárez |
| `f0000000-0000-0000-0000-000000000003` | `3050709012` | Importadora Técnica Nacional | Lucía Rojas |

Cada contacto se guarda en `ContactoProveedor`, ligado por `ProveedorId`; no se duplica dentro de la tabla principal.
