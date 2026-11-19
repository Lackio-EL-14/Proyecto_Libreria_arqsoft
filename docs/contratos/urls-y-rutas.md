# URLs, puertos y rutas

| Proyecto | Puerto | Base propia |
|---|---:|---|
| Front | 5000 | — |
| Gateway YARP | 5080 | — |
| Proveedores | 5101 | SQL Server `LibreriaProveedores`, contenedor `proveedores-db`, host 1433 |
| Usuarios | 5102 | SQL Server `LibreriaUsuarios`, contenedor `usuarios-db`, host 1434 |
| Clientes | 5103 | MySQL `libreria_clientes`, contenedor `clientes-db`, host 3306 |
| Productos | 5104 | MongoDB `libreria_productos`, contenedor `productos-db`, host 27017 |
| Keycloak | 8080 | Realm `libreria`, contenedor `keycloak` (ver `keycloak.md`) |

El Front envía todo a `http://localhost:5080`. YARP enruta:

| Ruta | Clúster |
|---|---|
| `/api/clientes/{**resto}` | `clientes` |
| `/api/proveedores/{**resto}` | `proveedores` |
| `/api/productos/{**resto}` | `productos` |
| `/api/categorias/{**resto}` | `productos` |
| `/api/marcas/{**resto}` | `productos` |
| `/api/historico/{**resto}` | `productos` |
| `/api/auth/{**resto}` | `usuarios` (**transitoria**: se retira en US-125; el login lo hace Keycloak) |
| `/api/usuarios/{**resto}` | `usuarios` |

Cada servicio usa exclusivamente su cadena `ConnectionStrings__<Servicio>Db`. No existen conexiones directas entre sus bases.
