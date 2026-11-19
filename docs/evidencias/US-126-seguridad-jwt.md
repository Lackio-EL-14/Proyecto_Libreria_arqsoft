# US-126: evidencia de la validación del JWT

**Corrida en vivo:** 19/11/2026, con Keycloak 26.8.1 (`docker compose up -d`), los 4 MS, el gateway y el Front en ejecución. Los tokens son **reales**, emitidos por el realm `libreria`, y no se muestran.

| Columna | Token |
|---|---|
| Sin token | sin `Authorization` |
| Firma alterada | token real de `admin` con un carácter de la firma cambiado |
| Sin `aud` libreria-api | token real de `admin` emitido para `libreria-usuarios-verificacion`, sin audiencia ni roles |
| Vendedor / Administrador | tokens reales de `vendedor` / `admin` (cliente `libreria-pruebas`) |

| Endpoint | Vía | Sin token | Firma alterada | Sin `aud` | Vendedor | Administrador |
|---|---|---:|---:|---:|---:|---:|
| `GET /api/clientes` | gateway / directo | 401 / 401 | 401 / 401 | 401 / 401 | 200 / 200 | 200 / 200 |
| `PATCH /api/clientes/{id}/estado` | gateway / directo | 401 / 401 | 401 / 401 | 401 / 401 | **403 / 403** | 404 / 404 (id inexistente) |
| `GET /api/proveedores` | gateway / directo | 401 / 401 | 401 / 401 | 401 / 401 | **403 / 403** | 200 / 200 |
| `GET /api/productos` | gateway / directo | 401 / 401 | 401 / 401 | 401 / 401 | **403 / 403** | 200 / 200 |
| `GET /api/categorias` | gateway / directo | 401 / 401 | 401 / 401 | 401 / 401 | **403 / 403** | 200 / 200 |
| `GET /api/marcas` | gateway / directo | 401 / 401 | 401 / 401 | 401 / 401 | **403 / 403** | 200 / 200 |
| `GET /api/historico/{id}` | gateway / directo | 401 / 401 | 401 / 401 | 401 / 401 | **403 / 403** | 200 / 200 |
| `GET /api/usuarios` | gateway / directo | 401 / 401 | 401 / 401 | 401 / 401 | **403 / 403** | 200 / 200 |
| `GET /api/usuarios/roles` | gateway / directo | 401 / 401 | 401 / 401 | 401 / 401 | **403 / 403** | 200 / 200 |
| `GET /api/usuarios/{sub}/sesion-activa` | gateway / directo | 401 / 401 | 401 / 401 | 401 / 401 | 200 / 200 | 200 / 200 |
| `GET /salud` (5080, 5101-5104) | — | 200 | — | — | — | — |

«Directo» significa llamar al puerto del MS (5101-5104) sin pasar por el gateway: el MS valida el token por su cuenta (defensa en profundidad).

**Token vencido:** no se puede esperar 60 minutos en la defensa. Se cubre con las pruebas automáticas (`TokenInvalido_Responde401` con el caso «vencido», en los 5 proyectos), que firman un token RS256 ya vencido.

**Swagger:** `http://localhost:510x/swagger` muestra el botón «Authorize» (esquema `Bearer`, JWT) en los 4 MS.

## Pruebas automáticas (sin Keycloak)

`dotnet test LibreriaSystem.slnx`. Los tokens se firman con una clave local (`tests/*/Seguridad/TokensDePrueba.cs`), y el servicio mantiene su validación real: emisor, audiencia, vencimiento, RS256 y roles.

| Proyecto | Clase | Casos |
|---|---|---|
| Gateway | `SeguridadJwtGatewayTests` | 7 tokens inválidos → 401 con ProblemDetails; vencido con su mensaje; válido → reenvía el mismo Bearer a los 4 MS; `/salud` anónima; ruta inexistente sin token → 401 |
| Clientes | `SeguridadJwtClientesTests` | 6 inválidos → 401; Administrador y Vendedor → 200; Vendedor `PATCH estado` → 403; Administrador → no 403; `/salud`; Swagger con Bearer |
| Proveedores | `SeguridadJwtProveedoresTests` | 6 inválidos → 401; Administrador → 200; Vendedor → 403; `/salud`; Swagger |
| Productos | `SeguridadJwtProductosTests` | 6 inválidos → 401; Administrador → 200; Vendedor → 403 en los 4 controladores; `/salud`; Swagger |
| Usuarios | `SeguridadJwtUsuariosTests` | 6 inválidos → 401; Administrador → 200; Vendedor → 403 en listar, crear, roles y baja; sesión activa → 200; **una cabecera `X-Usuario-Id` falsa no suplanta al admin**; `/salud`; Swagger |

Para reproducir la tabla a mano: `US-126-seguridad-jwt.http`.
