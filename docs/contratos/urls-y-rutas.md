# URLs, puertos y rutas del gateway

## URLs y configuración

- El Front tiene **una sola** URL: `Servicios:Gateway:BaseUrl` = `http://localhost:5080`. No conoce la URL de ningún MS.
- Ventas conoce `Servicios:Productos:BaseUrl` y `Servicios:Clientes:BaseUrl` (REST directo entre servicios, propagando el JWT).
- Cadenas de conexión: `ConnectionStrings:<Nombre>Db` en `appsettings.json` **sin contraseña**; la real por variable de entorno (`ConnectionStrings__ClientesDb`, `__VentasDb`, `__ProductosDb`, `__UsuariosDb`). Nunca en el repositorio.

## Proyectos y puertos

| Proyecto | Responsabilidad | Puerto |
|---|---|---|
| `Libreria.Client` | Front (Razor Pages) | 5000 |
| `Libreria.Gateway` | API Gateway (YARP) | 5080 |
| `Libreria.Servicio.Ventas` | MS Ventas | 5101 |
| `Libreria.Servicio.Usuarios` | MS Usuarios | 5102 |
| `Libreria.Servicio.Clientes` | MS Clientes | 5103 |
| `Libreria.Servicio.Productos` | MS Productos | 5104 |

| Contenedor | Motor | Puerto en el host | BD |
|---|---|---|---|
| `clientes-db` | MySQL 8 | 3306 | `libreria_clientes` |
| `ventas-db` | SQL Server 2022 | 1433 | `LibreriaVentas` |
| `productos-db` | MongoDB | 27017 | `libreria_productos` |
| `usuarios-db` | SQL Server 2022 | 1434 | `LibreriaUsuarios` |

## Rutas del gateway

| Archivo (dueño) | Ruta | Destino |
|---|---|---|
| `yarp.g1.json` (G1) | `/api/clientes/{**resto}` | Clientes (5103) |
| | `/api/ventas/{**resto}` | Ventas (5101) |
| | `/api/productos/{**resto}`, `/api/categorias/{**resto}`, `/api/marcas/{**resto}`, `/api/historico/{**resto}` | Productos (5104) |
| `yarp.g2.json` (G2) | `/api/auth/{**resto}` (anónima) | Usuarios (5102) |
| | `/api/usuarios/{**resto}` | Usuarios (5102) |

Si un MS no responde, el gateway devuelve **502/503/504** y el Front muestra `ServicioNoDisponible`.

### Transición: MS que todavía no tienen sus endpoints

Mientras Clientes (US-115) y Productos (US-122) no estén en `develop`, Ventas (5101) sigue atendiendo `api/clientes`, `api/categorias`, `api/marcas`, `api/productos` e `api/historico`. Por eso **`src/Libreria.Gateway/appsettings.Development.json`** apunta los clústeres `clientes` y `productos` a `http://localhost:5101`: en Development el Front completo funciona sin variables.

Precedencia de configuración del gateway (gana el último): `yarp.g1.json` / `yarp.g2.json` → `appsettings.{Entorno}.json` → variables de entorno → línea de comandos.

- **Al mergear US-115 / US-122:** su dueño borra su clúster de `appsettings.Development.json`; el gateway vuelve al destino de `yarp.g1.json` (5103 / 5104). El Front no cambia.
- **Para probar el MS nuevo antes de eso:** `ReverseProxy__Clusters__clientes__Destinations__principal__Address=http://localhost:5103` (o `productos` → 5104).
