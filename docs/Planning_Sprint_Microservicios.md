# Planning — Sprint de Migración a Microservicios (Gateway + 4 MS)

*Arquitectura de Software · 3.er Elemento de Competencia · Entregable único · Defensa: 24/11/2026 · Historias desde US-109*

| Grupo 1 — Clientes, Ventas y Productos (70 pts) | Grupo 2 — Usuarios, JWT y bitácora (70 pts) |
|---|---|
| Edson · Cesar · Josue | Mateo · Gabriel · Mauricio |

Hoy es sábado 14/11: quedan **10 días** hasta la defensa y se trabaja todos los días. Este planning se armó con el enunciado `ARQ EC3 - Entregable Unico.pdf`, la foto de la pizarra, el código actual (`Proyecto_Libreria-actual.zip`) y las decisiones tomadas en el chat de planificación. **Edson hace primero las historias base (US-109 a US-113)**: dejan el terreno listo para que nadie se bloquee.

> Los nombres de clases, archivos y endpoints **existentes** se citan tal como están en el código. Los nombres **nuevos** son propuestas y van marcados con *(propuesto)*; el dueño de la historia puede cambiarlos avisando al grupo.

---

## 0. Decisiones y hallazgos (leer primero)

**Lo que pide el enunciado (EC3):**

- 4 microservicios heterogéneos (uno por regla de negocio), **un API Gateway** y un Front en Razor Pages, integrados en un solo producto.
- **Al menos 4 patrones de diseño; Adapter y Result son obligatorios.** Se mantienen Clean Code y SOLID.
- Grupo 1: 2 MS relacionales con proveedores distintos y **Hexagonal** (22 pts cada uno) + 1 MS no relacional con **Clean** (26 pts). CRUD completo de una tabla por MS y validaciones con **Result**.
- Grupo 2: MS de Usuarios (lenguaje, BD y arquitectura libres) con login, sesiones, **JWT validado por cada microservicio**, bitácora, CRUD de Usuario, cambio de contraseña y **primer inicio de sesión con cambio obligatorio**.
- «Deberían tenerse 4 tablas implementadas con CRUDs»: Cliente, Venta, Producto y Usuario.
- Cada error ortográfico en la UI resta 1 punto.

**Decisiones tomadas:**

| Tema | Decisión |
|---|---|
| Servicios del Grupo 1 | **Clientes** (MySQL, Hexagonal) · **Ventas** (SQL Server, Hexagonal) · **Productos** (MongoDB, Clean). Se sigue el PDF: relacionales Hexagonal y no relacional Clean |
| Servicio del Grupo 2 | **Usuarios** en C# .NET 10, SQL Server y **Hexagonal** (se reestructura desde Clean) |
| Alcance | Sale el servicio de Reportes. Nginx lo reemplaza **YARP**. **Sin sagas, sin orquestadores y sin bus de eventos** en este entregable |
| Gateway | **Uno solo**, con dos archivos de rutas: `yarp.g1.json` (Grupo 1) y `yarp.g2.json` (Grupo 2) |
| Update de Venta | **«Corregir datos de la venta»**: cambiar cliente y observación de una venta no anulada. No toca stock, así que no cruza microservicios |
| Venta entre servicios | Versión mínima: Ventas pide a Productos el descuento de stock por REST y, si su propio commit falla, lo restaura (compensación local, no saga). **Si se complica, el comprobante es lo primero que se recorta** |
| Contenedores | **Obligatorio:** una BD por contenedor (4). **Opcional (US-138):** contenerizar MS, gateway y Front; pasa a prioritario si el docente lo exige |
| JWT | RS256, firmado solo por Usuarios, validado en el gateway y en los 4 MS (sección 6.3) |
| Primer ingreso | Contraseña temporal por correo + claim `debe_cambiar_contrasena` + bloqueo en Front **y** en los MS |
| Monolito | **Muere en este sprint**: se eliminan `Libreria.Application`, `Libreria.Domain`, `Libreria.Infrastructure` y `Libreria.Application.Tests` |
| Tech Lead | Edson es «un programador más», pero hace las historias base |

**Hallazgos del código actual que este sprint corrige:**

- El Front **todavía referencia el monolito** y tiene cadena de conexión propia: `Program.cs` conserva el bloque «TEMPORAL» con `AddUsuariosModule()`, `ConnectionStringSingleton` e `IDbConnectionFactory`. Usan tipos del monolito: `Cuenta/CambiarContrasena`, `Usuarios/Index`, `Productos/Details`, `Categorias/Edit`, `Marcas/Edit`, `Shared/Models/ConfirmModalModel` y `Shared/Models/ConfirmationModalModel`.
- `CategoriasController`, `MarcasController`, `ProductosController` y `HistoricoController` llaman a `fabrica.CrearRepositorio()` **desde el controlador**, saltándose la capa de aplicación. En los servicios nuevos la fábrica la usa el caso de uso o servicio de aplicación.
- `VentasApiClient` (unos 47 métodos) atiende a la vez ventas, productos, categorías, marcas e histórico. Si se queda así, tres personas editarían el mismo archivo. Se parte en la base (US-113). ✅ **Hecho en US-113:** un cliente por servicio y los DTOs ya en la carpeta de su módulo (ver `docs/contratos/front.md`).
- No existe CRUD de Cliente en el Front (solo el alta rápida de la venta) y la tabla `Cliente` no tiene `PublicId`, `Estado` ni auditoría.
- MongoDB en un solo contenedor **no admite transacciones de varios documentos**. Por eso el diseño de Productos usa operaciones atómicas sobre un solo documento (sección 5).
- El `.env` real no debe salir del equipo; se actualiza solo `.env.example`.

---

## 1. Arquitectura objetivo

```
                                   ┌─► Clientes   (Hexagonal) ─► clientes-db  (MySQL)
Front (Razor Pages) ─► API Gateway ├─► Ventas     (Hexagonal) ─► ventas-db    (SQL Server)
  cookie + JWT           (YARP)    ├─► Productos  (Clean)     ─► productos-db (MongoDB)
                    valida el JWT  └─► Usuarios   (Hexagonal) ─► usuarios-db  (SQL Server)

Ventas ─► Productos (descontar/restaurar stock) y Ventas ─► Clientes (buscar por CI/NIT),
por REST directo, propagando el mismo JWT del usuario.
```

**Reglas:**

- El Front solo conoce **la URL del gateway** y sus **DTOs propios**. No referencia proyectos de los servicios.
- Cada MS tiene **su propia BD** y nadie toca la BD de otro. Las referencias entre servicios son por `PublicId` (GUID), sin claves foráneas.
- No hay bibliotecas compartidas: lo que dos servicios necesiten (por ejemplo, `Result`) se copia.
- Cada microservicio valida el JWT por sí mismo, aunque el gateway también lo valide.

**Proyectos y puertos** (fijos desde el día 1):

| Proyecto | Responsabilidad | Puerto |
|---|---|---|
| `Libreria.Client` | Front (Razor Pages) | 5000 |
| `Libreria.Gateway` *(propuesto)* | API Gateway (YARP) | 5080 |
| `Libreria.Servicio.Ventas` | MS Ventas | 5101 |
| `Libreria.Servicio.Usuarios` | MS Usuarios | 5102 |
| `Libreria.Servicio.Clientes` *(propuesto)* | MS Clientes | 5103 |
| `Libreria.Servicio.Productos` *(propuesto)* | MS Productos | 5104 |

| Contenedor | Motor | Puerto en el host | BD |
|---|---|---|---|
| `clientes-db` | MySQL 8 | 3306 | `libreria_clientes` |
| `ventas-db` | SQL Server 2022 | 1433 | `LibreriaVentas` |
| `productos-db` | MongoDB | 27017 | `libreria_productos` |
| `usuarios-db` | SQL Server 2022 | 1434 | `LibreriaUsuarios` |

**Patrones por servicio (mínimo 4 en el proyecto; Adapter y Result obligatorios):**

| Patrón | Clientes | Ventas | Productos | Usuarios |
|---|---|---|---|---|
| **Adapter** | Repositorio MySQL detrás de su puerto | Adaptadores HTTP hacia Productos y Clientes | Repositorios Mongo detrás de sus abstracciones | `Argon2PasswordHasher`, `SmtpEmailSender`, generador de JWT |
| **Result** | Validaciones de Cliente | Validaciones de venta, corrección y anulación | Validaciones de Producto, Categoría y Marca | Login, alta y cambio de contraseña |
| **Factory Method** | `CrudRepositoryFactory<Cliente>` + fábrica MySQL | — | `CrudRepositoryFactory<T>` y `HistoricoRepositoryFactory` con fábricas Mongo | `UsuarioRepositoryFactory` / `SqlUsuarioRepositoryFactory` |
| **Facade** | — | `VentaFacade`, `StockFacade` | — | `UsuarioFacade` |
| **Singleton** | `ConnectionStringSingleton` | `ConnectionStringSingleton` | `MongoClient` único | `ConnectionStringSingleton` |

---

## 2. Mapa de migración: qué archivo va a dónde

El día 1, antes de borrar nada, Edson crea el tag **`v3-sba`** sobre `develop`. Todo lo que se mueve se copia desde ese tag.

| Destino | Origen en el código actual |
|---|---|
| **MS Clientes** (Hexagonal, MySQL) | **Domain:** `Entities/Cliente.cs`. **Application:** `Ports/Primary/IClienteService`, `Services/ClienteService`, `Validators/ClienteValidator`, `Models/Clientes/*`, `Models/Ventas/ClienteVentaItem`, `Ports/Secondary/IClienteRepository`, `Factories/CrudRepositoryFactory` (copia). **Infrastructure:** `Repositories/ClienteRepository` (reescrito para MySQL), `Connection/*` (copia). **Controllers:** `ClientesController` + `ClientesAltaRapidaController` (se unifican) + la parte de clientes de `VentasBusquedasController`. |
| **MS Productos** (Clean, MongoDB) | **Domain:** `Entities/Categoria`, `Entities/Marca`, `Entities/Producto`, `Catalogs/UbicacionesCategoria`, `PaisesMarca`. **Application:** `Services/ProductoService` e `IProductoService` (pasan a casos de uso), `Validators/CategoriaValidator`, `MarcaValidator` y `ProductoValidator`, `Factories/CrudRepositoryFactory` y `HistoricoRepositoryFactory` (las abstractas se copian tal cual), `Models/Categorias`, `Models/Marcas`, `Models/Productos` (incluye `ResultadoOperacion`, que lo reemplaza `Result`), `Models/Historico`, `Models/Ventas/ProductoVentaItem`, puertos `ICrudRepository`, `IProductoRepository`, `IHistoricoCostoRepository` e `ICatalogoProductoRepository`. **Infrastructure:** `Repositories/CategoriaRepository`, `MarcaRepository`, `ProductoRepository`, `HistoricoCostoRepository` y `CatalogoProductoRepository`, y `Factories/*`, todo reescrito para Mongo. **Controllers:** `CategoriasController`, `MarcasController`, `ProductosController`, `HistoricoController` y la parte de productos de `VentasBusquedasController`. |
| **Se queda en Ventas** (Hexagonal, SQL Server) | `Entities/Venta`, `Entities/DetalleVenta`, `Facades/VentaFacade`, `Facades/StockFacade`, `Services/ConsultaVentaService`, `ComprobanteVentaService`, `NumeroALetras`, `Models/Ventas/*` (salvo `ClienteVentaItem` y `ProductoVentaItem`), puertos `IVentaFacade`, `IStockFacade`, `IConsultaVentaService`, `IComprobanteVentaService`, `IVentaRepository`, `IMovimientoStockRepository`, `IUsuarioActual` e `IDbConnectionFactory`; `VentaRepository`, `MovimientoStockRepository`, `Connection/*`, `VentasController` y sus pruebas. `IProductoRepository` e `IClienteRepository` se reemplazan por puertos hacia los otros MS. |
| **MS Usuarios** (Clean → Hexagonal) | Se queda en su proyecto. `Application/Abstractions/*` pasa a `Application/Ports/Secondary`; los casos de uso (`UseCases/*`) implementan puertos primarios en `Application/Ports/Primary`; `Presentation/Controllers` pasa a `Controllers/` (misma convención que Ventas). Se conservan `UsuarioFacade`, `UsuarioRepositoryFactory`, `SqlUsuarioRepositoryFactory`, `ConnectionStringSingleton`, `Argon2PasswordHasher` y `SmtpEmailSender`. Sale todo lo del token de activación (sección 9). |
| **Front** (`Libreria.Client`) | Se queda. Pierde las referencias al monolito y a Reportes, apunta solo al gateway y reparte `VentasApiClient` en un cliente por servicio. |
| **Se elimina** | `src/Libreria.Application`, `src/Libreria.Domain`, `src/Libreria.Infrastructure`, `tests/Libreria.Application.Tests`, `src/Libreria.Servicio.Reportes`, `tests/Libreria.Servicio.Reportes.Tests`; en el Front: `Pages/Reportes`, `_MenuReportes`, `Clients/Reportes`, `DTOs/Reportes`, `AddServicioReportes`, el bloque «TEMPORAL» y la cadena de conexión; `db/init/*` (reemplazado por las carpetas por servicio). Todo queda recuperable desde el tag `v3-sba`. |

---

## 3. Rúbrica → historias

| Ítem | Pts | Historias |
|---|---|---|
| **G1 · Clientes** (MySQL + Hexagonal): Select 2 · Insert 6 · Update 6 · Delete 2 · Result 6 | 22 | US-114, US-115, US-116 |
| **G1 · Ventas** (SQL Server + Hexagonal): Select 2 · Insert 6 · Update 6 · Delete 2 · Result 6 | 22 | US-117, US-118, US-119 |
| **G1 · Productos** (MongoDB + Clean): Select 6 · Insert 6 · Update 6 · Delete 2 · Result 6 | 26 | US-121, US-122, US-123 |
| **G2 · Login + manejo de sesiones + implementación de JWT** | 20 | US-124, US-125, US-126 |
| **G2 · Bitácora en los atributos de auditoría** | 20 | US-127 |
| **G2 · CRUD de Usuario:** Select sin contraseña | 4 | US-128 |
| **G2 · CRUD de Usuario:** Insert (nombre por protocolo + contraseña segura por correo) | 12 | US-130 |
| **G2 · Cambio de contraseña** | 8 | US-129 |
| **G2 · Primer inicio de sesión** | 6 | US-131 |
| Clean Code y SOLID (requiere el enlace al repositorio en el documento) | 10 | Transversal + US-134 |
| Documento (Anexo 1) | 10 | US-132, US-133, US-134 |
| Defensa y presentación | 10 | US-137 |
| Ortografía (−1 por error) | — | US-135 |

Cada grupo se califica sobre 70 puntos por separado; los 30 restantes son comunes. Las historias base (US-109 a US-113), la de bugs (US-120), la regresión (US-136) y la opcional (US-138) no suman puntos por sí mismas, pero son requisito: la rúbrica prueba los MS **en el Front, después de verificar la arquitectura distribuida y el gateway**.

---

## 4. Tablero de Trello

**Listas:** Backlog del sprint · Por hacer · En progreso · En revisión (PR a `develop`) · Hecho (en `develop`) · En `main`.

**Etiquetas:** Grupo 1 · Grupo 2 · Base · Gateway · Front · MS Clientes · MS Ventas · MS Productos · MS Usuarios · BD · Documento · Ortografía · Bug del docente · 🔴 Bloquea a otros · una por persona.

**Plantilla de tarjeta:** título `US-xx – …`, objetivo, responsable, fecha límite, checklist «Criterios de aceptación» y checklist «Definition of Done».

**Definition of Done (se copia a todas las tarjetas):**

- [ ] Funciona de punta a punta: **Front → gateway → MS → su propia BD**, con el JWT de una sesión real (o el token de desarrollo antes de US-125).
- [ ] Las validaciones de negocio devuelven `Result`; las excepciones quedan solo para errores inesperados.
- [ ] Las pruebas del código que se toca pasan (`dotnet test`), y las nuevas reglas tienen al menos una prueba.
- [ ] PR a `develop` aprobado por alguien distinto al autor; si toca la carpeta de otro, lo aprueba su dueño.
- [ ] Sin `alert()`/`confirm()` nativos y sin faltas de ortografía en lo que toca.
- [ ] Código limpio y defendible: nombres claros, métodos cortos, una responsabilidad por clase, dependencias por interfaz.
- [ ] Insumos para el documento en un comentario de la tarjeta: captura del código y 2–3 líneas del patrón, estilo o principio SOLID aplicado (y el diagrama de la BD si la historia la toca).
- [ ] El autor puede explicar su código en 3 minutos: qué patrón aplica, en qué capa vive y por qué.

---

## 5. División de las bases de datos

Regla: lo que cambia junto en una transacción vive en la misma BD. Las referencias a otro servicio se guardan como `PublicId` **sin FK**, y los datos que Ventas necesita mostrar se guardan como **copia** al registrar.

**Convenciones comunes:**

- GUID: `CHAR(36)` en MySQL, `UNIQUEIDENTIFIER` en SQL Server, `string` en Mongo.
- **Auditoría en las 4 BD:** `UsuarioCreacionId`, `UsuarioModificacionId` (y `UsuarioAnulacionId` en `Venta`) guardan el `sub` del JWT (el `PublicId` del usuario). Junto a ellas, `FechaCreacion` y `FechaModificacion`.
- Scripts **idempotentes** en una carpeta por servicio: `db/clientes/` (MySQL), `db/ventas/` (SQL Server), `db/productos/` (Mongo, `.js`) y `db/usuarios/` (SQL Server). Cada carpeta tiene un solo dueño desde que la base se mergea.
- MySQL y Mongo se inicializan solos con `/docker-entrypoint-initdb.d`; los dos SQL Server con el bucle de `sqlcmd` que ya documenta el `README`.
- **Datos semilla con GUID fijos**, definidos una sola vez en `docs/contratos/datos-semilla.md` (US-111): usuarios `admin` y `vendedor`, clientes, categorías, marcas, 5 productos y 3 ventas que apuntan a esos mismos GUID.

| Servicio · BD | Contenido |
|---|---|
| **Clientes** · MySQL | **`Cliente`** con los mismos nombres de columna (`ClienteId`, `CiNit`, `RazonSocial`, `Correo`) para reescribir menos SQL. **Nuevas:** `PublicId CHAR(36) UNIQUE`, `Estado TINYINT(1)` (Delete lógico), `FechaCreacion`, `FechaModificacion`, `UsuarioCreacionId`, `UsuarioModificacionId`. Se conservan `UQ_Cliente_CiNit` y los `CHECK` de no vacío. Juego de caracteres `utf8mb4`. |
| **Ventas** · SQL Server | **`Venta`:** `ClienteId` pasa a `ClientePublicId` + copia de `ClienteCiNit` y `ClienteRazonSocial`; `UsuarioCreacionId` y `UsuarioAnulacionId` pasan a `UNIQUEIDENTIFIER`; **nuevas** `Observacion NVARCHAR(250) NULL`, `FechaModificacion`, `UsuarioModificacionId` (Update) y `VendedorIniciales NVARCHAR(15)` (bug del comprobante). Se conservan `Estado`, `Total`, `MotivoAnulacion` y sus `CHECK`. **`DetalleVenta`:** `ProductoId` pasa a `ProductoPublicId` + copia de `ProductoNombre`; precios, importe y ganancia igual que hoy. **`HistoricoMovimientoStock`:** `ProductoId` pasa a `ProductoPublicId`; conserva su FK a `Venta`. Desaparecen las FK a `Cliente`, `Producto` y `Usuario`. |
| **Productos** · MongoDB | **`productos`**: un documento por producto con sus campos actuales (`nombre`, `descripcionEspecifica`, `fechaVencimiento`, `esPerecedero`, `stock`, `precioVenta`, `costoAdquisicionActual`, `estado`, auditoría), `categoria: { publicId, nombre }`, `marca: { publicId, nombre }` y **`historicoCostos: [ … ]`** embebido. **`categorias`** y **`marcas`** con sus campos actuales. **`contadores`** reemplaza a `CategoriaCodigoSequence` para generar `Codigo`. Índices únicos: `publicId` en las tres; `nombre` en categorías y marcas; `codigo` en categorías. |
| **Usuarios** · SQL Server | **`Rol`** igual. **`Usuario`:** `Apellido` se divide en `ApellidoPaterno` (obligatorio) y `ApellidoMaterno` (opcional); se eliminan `TokenActivacionHash`, `TokenActivacionExpiracion` y `TokenActivacionFechaUso`; **nueva** `ContrasenaTemporalExpiracion DATETIME2 NULL`; `UsuarioCreacionId` y `UsuarioModificacionId` pasan a `UNIQUEIDENTIFIER`; `Estado = 'Pendiente'` significa «tiene contraseña temporal y debe cambiarla». |

**Por qué así (para la defensa):**

- **Stock sin transacciones de Mongo:** descontar es una sola operación atómica sobre el producto que solo descuenta si alcanza el stock. No hace falta transacción.
- **Histórico de costos embebido:** cambiar el costo y registrar el histórico es una sola escritura sobre un documento (atómica).
- **El historial de movimientos vive en Ventas:** entra en la misma transacción local que `Venta` y `DetalleVenta`. Así el commit y el rollback siguen demostrándose sobre 3 tablas.
- **Copias en la venta:** el listado, el detalle y el comprobante funcionan aunque Clientes o Productos estén caídos o cambien después.

**Dialecto (MySQL):** `TOP`→`LIMIT`, `IDENTITY`→`AUTO_INCREMENT`, `SCOPE_IDENTITY()`→`LAST_INSERT_ID()`, `NEWID()`→`UUID()`, `SYSDATETIME()`→`NOW(6)`, `BIT`→`TINYINT(1)`, `NVARCHAR`→`VARCHAR` con `utf8mb4`, duplicado 2627/2601→**1062**. En el contenedor Linux los nombres de tabla distinguen mayúsculas: usar siempre `Cliente`.

---

## 6. Contratos (definidos en este planning; no se espera a nadie)

Edson los copia el día 1 a `docs/contratos/` (US-113). Cambiar un contrato se avisa en el grupo del equipo **antes** de hacer el PR.

### 6.1 URLs y configuración

- El Front tiene **una sola** URL: `Servicios:Gateway:BaseUrl` = `http://localhost:5080`.
- Ventas conoce `Servicios:Productos:BaseUrl` y `Servicios:Clientes:BaseUrl` (REST directo entre servicios, propagando el JWT).
- Cadenas de conexión: `ConnectionStrings:<Nombre>Db` en `appsettings.json` **sin contraseña**; la real por variable de entorno (`ConnectionStrings__ClientesDb`, `__VentasDb`, `__ProductosDb`, `__UsuariosDb`). Nunca en el repositorio.

### 6.2 Rutas del gateway

| Archivo (dueño) | Ruta | Destino |
|---|---|---|
| `yarp.g1.json` (G1) | `/api/clientes/{**resto}` | Clientes (5103) |
| | `/api/ventas/{**resto}` | Ventas (5101) |
| | `/api/productos/{**resto}`, `/api/categorias/{**resto}`, `/api/marcas/{**resto}`, `/api/historico/{**resto}` | Productos (5104) |
| `yarp.g2.json` (G2) | `/api/auth/{**resto}` (anónima) | Usuarios (5102) |
| | `/api/usuarios/{**resto}` | Usuarios (5102) |

### 6.3 JWT

| Aspecto | Contrato |
|---|---|
| Algoritmo | **RS256**. Solo Usuarios tiene la clave privada; el gateway y los 4 MS tienen la pública |
| Claves | Un par de desarrollo generado una vez por Gabriel. Privada: archivo fuera del repo, ruta en `Jwt__ClavePrivadaRuta` (solo Usuarios), compartida por el canal privado del equipo. Pública: en `Jwt:ClavePublicaPem` del `appsettings.json` de cada servicio y del gateway (se puede subir) |
| Emisor / audiencia | `iss` = `Libreria.Usuarios` · `aud` = `Libreria` |
| Claims | `sub` = `PublicId` del usuario · `name` = nombre de usuario · `role` = `Administrador` \| `Vendedor` · `iniciales` (por ejemplo `J.J.P.R.`) · `debe_cambiar_contrasena` = `true` \| `false` |
| Duración | 60 minutos, sin refresh token. La cookie del Front expira junto con el token |
| Transporte | `Authorization: Bearer <token>` en toda llamada Front → gateway → MS y MS → MS |
| Respuestas | Sin token, firma alterada o vencido → **401**. Token con `debe_cambiar_contrasena = true` → **403** en todo, salvo `PUT api/usuarios/contrasena` |
| Políticas *(propuestas)* | Política por defecto: usuario autenticado y contraseña vigente. `PuedeCambiarContrasena`: solo usuario autenticado. Solo `POST api/auth/login` es anónima |

### 6.4 Usuario actual y bitácora

- Cada MS tiene **su** puerto de usuario actual (`IUsuarioActual` en Ventas y Clientes, `ICurrentUser` en Usuarios, su equivalente en Productos) con: `Guid? UsuarioId`, `string? NombreUsuario`, `string? Iniciales`.
- **Adaptador definitivo:** `UsuarioActualDesdeToken` *(propuesto)*, lee los claims del JWT (US-127).
- **Hasta que exista:** `UsuarioActualDesarrollo` *(propuesto)* devuelve el `PublicId` fijo de `admin` de `datos-semilla.md`, **solo en `Development`** (US-110).
- La carpeta **`Infrastructure/Seguridad/`** de cada MS es del **Grupo 2**: ahí viven la validación del JWT y el adaptador de usuario actual. El Grupo 1 no la edita; el Grupo 2 no edita nada fuera de ella sin PR revisado por el dueño del MS.
- Los nombres para «creado por» se piden a `GET api/usuarios/nombres?ids=<guid>,<guid>`.

### 6.5 REST, errores y `Result`

- JSON; rutas `api/<recurso>`; fechas ISO 8601; dinero `decimal`; solo `PublicId` hacia afuera, nunca ids numéricos.
- `Result` / `Result<T>` *(propuesto)*: `EsExito`, `Valor`, `Errores` (campo → mensaje) y `Tipo` (`Validacion`, `NoEncontrado`, `Conflicto`). Cada MS tiene su copia en `Comun/Result.cs` dentro de la capa más interna que lo use.
- Traducción en el controlador: `Validacion` → **400** (`ValidationProblemDetails`), `NoEncontrado` → **404**, `Conflicto` → **409**, servicio dependiente caído → **503**, inesperado → **500** (`ProblemDetails`).
- **Contrato estable:** los endpoints que hoy existen **conservan ruta y DTO**; solo cambia el servicio que los atiende (el gateway enruta). Así las páginas actuales de Categorías, Marcas, Productos e Histórico cambian poco.

### 6.6 Endpoints nuevos o que cambian

| MS | Endpoint | Historia |
|---|---|---|
| Clientes | `GET api/clientes` (filtros, orden) · `GET api/clientes/{publicId}` · `POST api/clientes` · `PUT api/clientes/{publicId}` · `PATCH api/clientes/{publicId}/estado` | US-115 |
| Clientes | `GET api/clientes/buscar?ciNit=` (para la venta; mismo DTO que `ClienteVentaItem` + `PublicId`) | US-115 |
| Productos | `GET api/productos/buscar?termino=` (para la venta; mismo DTO que `ProductoVentaItem`) | US-122 |
| Productos | `POST api/productos/{publicId}/stock/descontar` y `/stock/restaurar` con `{ cantidad, ventaPublicId }` → 200 / 409 | US-122 |
| Ventas | `PUT api/ventas/{publicId}` (corregir cliente y observación). Se elimina `VentasBusquedasController` | US-117, US-119 |
| Usuarios | `POST api/auth/login` devuelve `{ token, expiraEn, usuario }` | US-125 |
| Usuarios | `GET api/usuarios/nombres` y `sesion-activa` reciben GUID. Salen `activar` y `reenviar-activacion` | US-124, US-125, US-127 |

---

## 7. Ramas y cómo evitar bloqueos

- Todo se integra en **`develop`** por PR. `main` solo recibe código estable el **lunes 23/11** (US-137).
- Ramas `feature/US-xx-nombre` desde `develop` actualizado; `git pull --rebase origin develop` **cada mañana y antes de cada push**; PR pequeños (una historia o un hito por PR).
- **Nadie ramifica antes de que US-109 y US-110 estén en `develop`** (sábado 14). Mientras tanto: sesión de traspaso y lectura de contratos.
- **Sesión de traspaso (sábado 14, 1 hora):** Cesar y Josue explican Usuarios al Grupo 2; Gabriel, Mateo y Mauricio explican el flujo de venta (`VentaFacade`, `StockFacade`, `venta-registrar.js`, comprobante) a Cesar y Josue. Cada uno defiende lo que implementa, así que cada uno debe dominar su nuevo código.
- **Dueños de carpeta:**

| Carpeta | Dueño |
|---|---|
| `Libreria.Gateway/` (salvo los `yarp.*.json`), `docker-compose.yml`, `LibreriaSystem.slnx`, `tools/` | Edson |
| `yarp.g1.json` · `yarp.g2.json` | Grupo 1 · Grupo 2 |
| `Libreria.Servicio.Clientes/`, `db/clientes/` | Edson |
| `Libreria.Servicio.Ventas/`, `db/ventas/` | Cesar |
| `Libreria.Servicio.Productos/`, `db/productos/` | Josue |
| `Libreria.Servicio.Usuarios/`, `db/usuarios/` | Gabriel (Mauricio y Mateo trabajan en sus casos de uso) |
| `Infrastructure/Seguridad/` de **cada** MS | Grupo 2 (Gabriel: JWT · Mauricio: usuario actual) |
| Front: `Pages/Clientes`, `Clients/Clientes`, `DTOs/Clientes`, `_MenuClientes`, `_ClienteAltaRapidaCampos` | Edson |
| Front: `Pages/Ventas`, `Clients/Ventas`, `DTOs/Ventas`, `_MenuVentas`, `venta-registrar.js` | Cesar |
| Front: `Pages/Productos`, `Pages/Categorias`, `Pages/Marcas`, `Pages/Historico`, `Clients/Productos`, `DTOs/Productos`, `_MenuInventario` | Josue |
| Front: `Pages/Cuenta/Login`, `Logout`, `Seguridad/`, autenticación en `Program.cs` | Gabriel |
| Front: `Pages/Cuenta/CambiarContrasena`, `Pages/Usuarios/Index`, `Services/Auditoria`, `_AuditoriaInfo` | Mauricio |
| Front: `Pages/Usuarios/Create`, middleware de primer ingreso | Mateo |

- **`Program.cs` del Front:** cada módulo se registra en su método de extensión (`AddModuloClientes()`, `AddModuloVentas()`, `AddModuloProductos()`, `AddModuloUsuarios()`, en `Clients/DependencyInjection/ServicioClientsExtensions.cs`, US-113); nadie edita el bloque de otro. La autenticación es de Gabriel.
- **DTOs del Front ya repartidos (US-113), con su namespace definitivo:** `DTOs/Common` (`ResultadoApi`), `DTOs/Clientes` (`ClienteInput`, `ClienteRegistroResult`, `ClienteVentaBusquedaDto`), `DTOs/Ventas` (ventas, `RegistrarVentaInput`, `ProductoVentaBusquedaDto`) y `DTOs/Productos` (categorías, marcas, productos, histórico). **Nadie necesita moverlos ni cambiar namespaces**; cada dueño solo agrega o ajusta lo de su carpeta.
- **`.slnx` y `docker-compose.yml`:** solo los toca Edson (US-110, US-111, US-138). Si alguien necesita un cambio, se lo pide.
- **Contract-first:** quien consume un MS que aún no existe trabaja contra su interfaz con datos de ejemplo de `datos-semilla.md`, y cambia a la API real cuando esté en `develop`.

---

## 8. Historias base — Edson (sábado 14 y domingo 15) 🔴

### US-109 – Retiro del monolito y de Reportes 🔴 (Edson, primeras horas del sábado)
- [ ] Tag **`v3-sba`** sobre `develop` antes de borrar nada.
- [ ] Eliminar del disco y de `LibreriaSystem.slnx`: `Libreria.Application`, `Libreria.Domain`, `Libreria.Infrastructure`, `tests/Libreria.Application.Tests`, `Libreria.Servicio.Reportes` y `tests/Libreria.Servicio.Reportes.Tests`.
- [ ] Front: quitar las `ProjectReference` al monolito, el bloque «TEMPORAL» y `AddUsuariosModule()` de `Program.cs`, el registro de `ConnectionStringSingleton`, `IDbConnectionFactory`, `IContrasenaHasher` e `IUsuarioActual` del monolito, y `ConnectionStrings` de `appsettings.json`.
- [ ] Front: quitar Reportes (`Pages/Reportes`, `_MenuReportes`, `Clients/Reportes`, `DTOs/Reportes`, `AddServicioReportes`, `Servicios:Reportes`).
- [ ] Hacer compilar las páginas que usaban tipos del monolito (`Cuenta/CambiarContrasena`, `Usuarios/Index`, `Productos/Details`, `Categorias/Edit`, `Marcas/Edit`, `ConfirmModalModel`, `ConfirmationModalModel`): el tipo que el Front necesite se copia como modelo propio en `DTOs/`. Si una página queda sin función, compila con un `TODO US-xx` y la historia de su dueño la reconecta.
- [ ] `dotnet build` sin errores y `grep -r "Libreria.Application\|Libreria.Domain\|Libreria.Infrastructure" src tests` sin resultados.
- [ ] Mergeado a `develop` el sábado por la mañana: todos ramifican desde aquí.

### US-110 – Esqueleto de microservicios y gateway en la solución 🔴 (Edson, sábado)
- [ ] Crear `Libreria.Servicio.Clientes` y `Libreria.Servicio.Productos` (Web API, .NET 10, Swagger) y `Libreria.Gateway` (ASP.NET Core vacío), con los puertos de la sección 1, más `tests/Libreria.Servicio.Clientes.Tests` y `tests/Libreria.Servicio.Productos.Tests`.
- [ ] Carpetas vacías con la estructura de cada estilo: Clientes igual que Ventas (Hexagonal: `Domain`, `Application/Ports/Primary`, `Application/Ports/Secondary`, `Application/Services`, `Application/Factories`, `Infrastructure`, `Controllers`); Productos igual que el Usuarios actual (Clean: `Domain`, `Application`, `Infrastructure`, `Presentation`).
- [ ] `Comun/Result.cs` (sección 6.5) copiado en los 4 MS, con un método de extensión que lo traduce a `ProblemDetails`.
- [ ] Puerto de usuario actual con `UsuarioActualDesarrollo` en Clientes y Productos, dentro de `Infrastructure/Seguridad/` (sección 6.4).
- [ ] Todo agregado a `LibreriaSystem.slnx`, perfil de inicio múltiple «Correr todo» y script `run-all.ps1` con `dotnet run` de los 6 proyectos.
- [ ] Un archivo `.http` por servicio y `GET /salud` *(propuesto)* en cada MS para probar el gateway.
- [ ] Mergeado a `develop` el sábado.

### US-111 – Una base de datos por servicio 🔴 (Edson, sábado y domingo)
- [ ] `docker-compose.yml` con los 4 contenedores de la sección 1 (volúmenes y `healthcheck`); se retira `libreria-db`.
- [ ] Carpetas `db/clientes/`, `db/ventas/`, `db/productos/` y `db/usuarios/` con el **esquema inicial de la sección 5**, partido desde los scripts actuales (`00_schema.sql`, `10_usuario_ampliado.sql`, `20_venta_total.sql`, `21_venta_motivo.sql`, `22_historial_movimientos_stock.sql`, `30_argon2_passwords.sql`, `50_datos_prueba.sql`). Idempotentes.
- [ ] `docs/contratos/datos-semilla.md` con los GUID fijos de usuarios, clientes, categorías, marcas, productos y ventas; los scripts de cada BD los usan.
- [ ] `.env.example` con los nombres de todas las variables nuevas (sin valores reales).
- [ ] Sección «Levantar las bases» en el `README`: `docker compose up -d` y cómo ejecutar los scripts de los dos SQL Server.
- [ ] Las 4 BD levantan desde cero en una máquina limpia.

### US-112 – API Gateway con YARP 🔴 (Edson, domingo)
- [ ] `Libreria.Gateway` con `Yarp.ReverseProxy` que carga `yarp.g1.json` y `yarp.g2.json` (sección 6.2); destinos configurables por variable de entorno.
- [ ] Si un MS no responde, el gateway devuelve 502/503 y el Front muestra `ServicioNoDisponible` (ya existe).
- [ ] La validación del JWT en el gateway **no** es de esta historia (es de Gabriel, US-126); aquí solo se enruta.
- [ ] Probado con el `.http`: cada ruta llega a su MS.

### US-113 – Base del Front y contratos publicados 🔴 (Edson, domingo)
- [x] Una sola URL `Servicios:Gateway:BaseUrl`; todos los clientes HTTP tipados apuntan al gateway.
- [x] Partir `VentasApiClient` / `IVentasApiClient` en `IServicioClientesClient`, `IServicioProductosClient` (productos, categorías, marcas, histórico) e `IServicioVentasClient` (el que ya existe), y actualizar las páginas que los usan sin cambiar su comportamiento.
- [x] Mover los DTOs a la carpeta de su módulo (`DTOs/Common`, `DTOs/Clientes`, `DTOs/Ventas`, `DTOs/Productos`), con su namespace definitivo, para que nadie tenga que moverlos después.
- [x] Volver a incluir en el build las páginas de inventario (`Categorias`, `Marcas`, `Productos`, `Historico`) que US-109 había excluido; ya usan `IServicioProductosClient`.
- [x] Partir `_MenuVentas` en `_MenuVentas` (Registrar venta, Ventas) y `_MenuInventario` (Productos, Categorías, Marcas); crear `_MenuClientes`.
- [x] Métodos de extensión por módulo en `Program.cs` (sección 7).
- [x] Copiar la sección 6 completa a `docs/contratos/` (REST por MS, JWT, `Result`, usuario actual, rutas, Front).
- [ ] En `develop` el domingo: desbloquea a los 6.

---

## 9. Grupo 1 — Clientes, Ventas y Productos · 70 pts

### US-114 – MS Clientes: núcleo hexagonal y adaptador MySQL (Edson) · Result 6 pts
- [ ] Copiar desde `v3-sba` lo de la sección 2 y ampliar `Cliente` con `PublicId`, `Estado` y auditoría.
- [ ] `ClienteRepository` reescrito con `MySqlConnector` (**Adapter** del puerto `IClienteRepository`), con los cambios de dialecto de la sección 5.
- [ ] **Factory Method:** `CrudRepositoryFactory<Cliente>` (copia) + `MySqlClienteRepositoryFactory` *(propuesto)*; la usa `ClienteService`, no el controlador.
- [ ] **Singleton:** `ConnectionStringSingleton` copiado.
- [ ] **Result:** `ClienteValidator` y `ClienteService` devuelven `Result` (CI/NIT obligatorio y único, razón social obligatoria, correo con formato válido; duplicado MySQL 1062 → `Conflicto`).
- [ ] Pruebas de `ClienteService` con repositorio falso.

### US-115 – MS Clientes: API REST y rutas del gateway (Edson) · Insert 6 · Update 6 · Delete 2
- [ ] Endpoints de la sección 6.6; un solo `ClientesController`.
- [ ] **Insert:** alta con validaciones de US-114; registra `UsuarioCreacionId`.
- [ ] **Update:** edición de razón social, correo y CI/NIT (sin chocar con otro); registra `UsuarioModificacionId`.
- [ ] **Delete lógico:** `Estado = 0`, sin borrar la fila; se puede reactivar; un cliente inactivo no aparece en `buscar` para la venta.
- [ ] Rutas de Clientes en `yarp.g1.json`. Probado con Swagger y `.http`.
- [ ] Borrar el clúster `clientes` de `src/Libreria.Gateway/appsettings.Development.json` (transición de US-113 que lo apunta a Ventas 5101).

### US-116 – UI de Clientes en el Front (Edson) · Select 2
- [ ] `Pages/Clientes`: **listado con refresco, ordenación por columna y búsqueda**, alta, edición, baja con el modal compartido y reactivación.
- [ ] `_AuditoriaInfo` muestra creado y modificado por.
- [ ] `_MenuClientes`; acceso de Administrador y Vendedor, baja solo Administrador *(propuesta, confirmar en el equipo)*.
- [ ] `_ClienteAltaRapidaCampos` (alta rápida desde la venta) llama a Clientes por el gateway; coordinar con Cesar, que es dueño de `Registrar`. *(US-113 ya dejó `Registrar` usando `IServicioClientesClient` para `POST api/clientes`; falta que el MS Clientes lo atienda.)*
- [ ] Métodos tipados del CRUD en `IServicioClientesClient` (`Clients/Clientes`) y DTOs en `DTOs/Clientes` (la carpeta ya existe).

### US-117 – MS Ventas: BD propia y retiro de lo que ya no le pertenece (Cesar) 🔴
- [ ] Eliminar de Ventas todo lo que va a Clientes y Productos (sección 2): entidades, controladores (`CategoriasController`, `MarcasController`, `ProductosController`, `HistoricoController`, `ClientesController`, `ClientesAltaRapidaController`, `VentasBusquedasController`), servicios, validadores, factorías, repositorios y modelos. Edson y Josue copian desde `v3-sba`, así que no hay que esperarlos.
- [ ] `VentaRepository` y `MovimientoStockRepository` sobre `LibreriaVentas` (sección 5): `ClientePublicId` + copia, `ProductoPublicId` + copia, auditoría GUID, `Observacion`, `VendedorIniciales`.
- [ ] `IUsuarioActual` con el contrato de la sección 6.4 (`Guid? UsuarioId`, `NombreUsuario`, `Iniciales`); la entidad `Venta` y sus repositorios usan GUID para la auditoría.
- [ ] Listado, detalle y comprobante se arman solo con la BD de Ventas (sin JOIN a otras tablas).
- [ ] `NumeroALetrasTests` y las pruebas `VentasController*` pasan.
- [ ] En `develop` el lunes 16 a más tardar.

### US-118 – Registro y anulación de venta entre microservicios (Cesar) · Insert 6 · Delete 2
- [ ] Puertos secundarios hacia los otros MS *(propuestos: `IProductosPort`, `IClientesPort`)* con adaptadores HTTP (**Adapter**) que propagan el JWT.
- [ ] **`StockFacade`** cambia por dentro: descuenta línea por línea en Productos y, si una falla, **restaura las ya descontadas** (compensación); devuelve `Result`.
- [ ] **`VentaFacade.RegistrarVentaAsync`:** valida, obtiene el cliente (Clientes) y los precios vigentes (Productos), descuenta stock con `StockFacade` y guarda `Venta`, `DetalleVenta` e `HistoricoMovimientoStock` en **una transacción local**. Si el commit falla, `StockFacade` restaura todo. Se conserva el guardado idempotente con `SolicitudId`.
- [ ] Guarda `VendedorIniciales` desde el claim `iniciales`.
- [ ] **Anulación lógica** (`POST api/ventas/{publicId}/anular`): restaura el stock en Productos y luego cambia a «Anulada» con motivo, usuario y fecha; si Productos falla, la venta no se anula; si el commit local falla, se vuelve a descontar. No se anula dos veces.
- [ ] Prueba forzando un error a mitad: el stock en Productos queda igual que antes.
- [ ] Errores claros: 409 por stock insuficiente, 503 si Productos o Clientes no responden.

### US-119 – CRUD de Venta: listado, corrección de datos y Result (Cesar) · Select 2 · Update 6 · Result 6
- [ ] **Select:** una fila por venta, con refresco, ordenación (fecha, total) y los filtros que ya existen (`FiltroVentasInput`); la ganancia solo la ve el Administrador.
- [ ] **Update «Corregir datos de la venta»:** `PUT api/ventas/{publicId}` cambia el cliente (búsqueda por CI/NIT en Clientes; se actualiza la copia) y la observación (máximo 250 caracteres). Solo si la venta está activa. Registra `UsuarioModificacionId` y `FechaModificacion`. Modal en `Ventas/Index` con confirmación; el detalle muestra la modificación.
- [ ] **Result:** venta sin líneas, cantidad no válida, cliente inexistente o inactivo, venta anulada no editable, motivo fuera de 10–250 caracteres y doble anulación devuelven `Result` (400/404/409). `VentaRegistroException` y las `ArgumentException` de negocio desaparecen.
- [ ] El comprobante sigue funcionando (`GET api/ventas/{publicId}/comprobante`). **Si el tiempo no alcanza, se recorta** y se anota la decisión en la tarjeta.

### US-120 – Bugs de venta y comprobante (Cesar) · bugs del docente
- [ ] **Stock máximo:** en `venta-registrar.js`, `actualizarCantidad(true)` (evento `change`) devuelve en silencio el último valor válido, y por eso «recorta un dígito». Se corrige: el valor ingresado se mantiene, se muestra el error, la fila queda inválida y **«Guardar» se deshabilita** hasta corregirlo. El servidor también lo rechaza (409), por si alguien salta la UI.
- [ ] **Vendedor en el comprobante:** se muestran **solo las iniciales** (`VendedorIniciales`, por ejemplo «Atendido por: J.J.P.R.»), nunca el nombre completo. `ComprobanteVenta.GeneradoPor` cambia en consecuencia.
- [ ] Ambos casos probados y con captura para el documento.

### US-121 – MS Productos: estructura Clean y persistencia MongoDB (Josue) 🔴
- [ ] Capas Clean igual que el Usuarios actual, con las dependencias hacia adentro: `Domain` (`Producto`, `Categoria`, `Marca`, histórico de costos, `UbicacionesCategoria`, `PaisesMarca`), `Application` (casos de uso, abstracciones, DTOs, factorías), `Infrastructure` (Mongo), `Presentation/Controllers`.
- [ ] Copiar desde `v3-sba` lo de la sección 2; `ProductoService` pasa a casos de uso.
- [ ] **Singleton:** `MongoClient` registrado una sola vez, más un contexto que entrega las colecciones.
- [ ] **Factory Method:** `CrudRepositoryFactory<T>` y `HistoricoRepositoryFactory` **sin cambios**, con fábricas concretas nuevas para Mongo (la mejor demostración de Abierto/Cerrado); las usan los casos de uso, no los controladores.
- [ ] **Adapter:** repositorios Mongo que implementan las abstracciones de `Application`.
- [ ] Modelo de documentos de la sección 5 y contador para `Codigo` de categoría.
- [ ] Pruebas de los casos de uso con repositorios falsos.
- [ ] El endpoint de stock y `buscar` en `develop` el martes 17 (desbloquean a Cesar).

### US-122 – MS Productos: API de productos, catálogos e inventario (Josue) · Insert 6 · Update 6 · Delete 2 · Result 6
- [ ] Mismas rutas y DTOs que hoy (`api/productos`, `api/categorias`, `api/marcas`, `api/historico`, incluidos `formulario`, `edicion`, `ubicaciones`, `paises`, `estado` y `baja`), más `buscar` y los de stock (sección 6.6).
- [ ] **Insert:** producto con categoría y marca activas; el costo inicial entra a `historicoCostos` con motivo «Registro inicial».
- [ ] **Update:** si cambia el costo, `$set` + `$push` al histórico en una sola operación.
- [ ] **Delete lógico:** baja sin borrar el documento; se conserva la regla actual de reactivación.
- [ ] **Stock atómico:** `descontar` solo actúa si `stock >= cantidad` (si no, 409); `restaurar` suma.
- [ ] Renombrar una categoría o marca actualiza la copia embebida en los productos.
- [ ] **Result:** nombre obligatorio, precio y costo no negativos, perecedero con fecha de vencimiento, categoría y marca activas, ubicación y país válidos, nombres únicos.
- [ ] Rutas de Productos en `yarp.g1.json`.
- [ ] Borrar el clúster `productos` de `src/Libreria.Gateway/appsettings.Development.json` (transición de US-113 que lo apunta a Ventas 5101).

### US-123 – UI de inventario conectada a Productos (Josue) · Select 6
- [ ] **Listado de productos con refresco, ordenación por columna y filtros** (categoría, marca, estado).
- [x] `Productos`, `Categorias`, `Marcas` e `Historico` usan `IServicioProductosClient` y compilan de nuevo (hecho en US-113; DTOs ya en `DTOs/Productos`, sin cambios de namespace pendientes).
- [ ] Comprobar que funcionan igual que antes contra el MS Productos real (después de borrar el clúster `productos` de `appsettings.Development.json` del gateway, US-122).
- [ ] `Productos/Details` muestra el histórico embebido.
- [ ] Menú en `_MenuInventario`.

---

## 10. Grupo 2 — Usuarios, JWT y bitácora · 70 pts

### US-124 – MS Usuarios: de Clean a Hexagonal y BD propia (Gabriel) 🔴
- [ ] Misma convención que Ventas: `Application/Abstractions/*` → `Application/Ports/Secondary`; puertos primarios en `Application/Ports/Primary` implementados por los casos de uso; `Presentation/Controllers` → `Controllers/`; `Infrastructure` = adaptadores de salida.
- [ ] Se conservan **Facade** (`UsuarioFacade`), **Factory Method** (`UsuarioRepositoryFactory`, `SqlUsuarioRepositoryFactory`), **Singleton** (`ConnectionStringSingleton`) y **Adapter** (`Argon2PasswordHasher`, `SmtpEmailSender`).
- [ ] `UserRepository` sobre `LibreriaUsuarios` con el esquema de la sección 5 (`ApellidoPaterno`, `ApellidoMaterno`, `ContrasenaTemporalExpiracion`, auditoría GUID).
- [ ] Sale el token de activación: `ActivationTokenService`, `ActivateUserUseCase`, `ResendActivationUseCase`, sus endpoints, sus pruebas y la página `Usuarios/Activar`. Mientras Mateo termina US-130, `CreateUserUseCase` crea el usuario como Pendiente sin enviar correo.
- [ ] Las pruebas de `tests/Libreria.Servicio.Usuarios.Tests` se adaptan y pasan.
- [ ] **Primer hito (domingo 15):** login funcionando contra la BD nueva, para que Mauricio y Mateo no esperen.

### US-125 – Login con emisión de JWT y sesión en el Front (Gabriel) · Login + sesiones + JWT
- [ ] Generar el par de claves de desarrollo (sección 6.3) y compartir la privada por el canal privado del equipo.
- [ ] Generador de tokens RS256 detrás de un puerto (**Adapter**) con los claims de la sección 6.3.
- [ ] `POST api/auth/login`: Activo y Pendiente ingresan (Pendiente con `debe_cambiar_contrasena = true`); Inactivo, rol inactivo o credenciales malas → mensaje genérico. `LoginUseCase` devuelve `Result`.
- [ ] Iniciales: primera letra de cada nombre y de cada apellido (Juan José Pérez Rojas → `J.J.P.R.`).
- [ ] Front: `Cuenta/Login` llama al gateway; la cookie guarda los claims y el token, con la misma duración que el token; `Logout` cierra la sesión.
- [ ] `UsuarioActualDelegatingHandler` (encabezados `X-Usuario-*`) se reemplaza por un handler que agrega `Authorization: Bearer`. Un 401 de cualquier servicio cierra la sesión y lleva al login con un mensaje.
- [ ] `ValidarEstadoUsuarioCookieEvents` consulta `sesion-activa` por `PublicId`: una baja cierra la sesión activa.
- [ ] Cada rol ve un menú distinto (Administrador y Vendedor), validado también por políticas en el servidor.
- [ ] Herramienta `tools/TokenDesarrollo` *(propuesta)* para generar tokens de prueba (admin, vendedor, pendiente) para Swagger y `.http`.

### US-126 – Validación del JWT en los 4 MS y en el gateway (Gabriel) · 20 pts
- [ ] En `Infrastructure/Seguridad/` de cada MS: un método de extensión *(propuesto `AddSeguridadJwt()`)* que configura `JwtBearer` con la clave pública, emisor, audiencia, vencimiento y un desfase de reloj de 1 minuto, más las políticas de la sección 6.3. Todo exige token, salvo `POST api/auth/login`.
- [ ] El gateway valida igual; la ruta de login es anónima.
- [ ] Swagger de cada MS con el botón «Authorize» para pegar el token.
- [ ] Pruebas por MS: sin token → 401; firma alterada → 401; vencido → 401; válido → 200; `debe_cambiar_contrasena` → 403. Evidencias en `.http` y capturas para la defensa.
- [ ] Las líneas de `Program.cs` de cada MS las agrega Gabriel en un PR revisado por el dueño del MS.

### US-127 – Bitácora basada en la sesión en los 4 MS (Mauricio) · 20 pts
- [ ] `UsuarioActualDesdeToken` *(propuesto)* en `Infrastructure/Seguridad/` de cada MS; reemplaza a `UsuarioActualDesarrollo`, `UsuarioActualDesdeEncabezados` (Ventas) y `CurrentUserMiddleware` + `CurrentUser` (Usuarios).
- [ ] Verificar que **todo** CRUD llene la auditoría: Cliente (alta, edición, baja), Venta (registro, corrección, anulación), Producto, Categoría, Marca e Histórico (alta, edición, baja) y Usuario (alta, baja, cambio de contraseña).
- [ ] `GetUserNamesUseCase` y `GET api/usuarios/nombres` trabajan con GUID; `AuditoriaService` y `AuditoriaRegistroDto` del Front también; `_AuditoriaInfo` muestra los nombres.
- [ ] Prueba cruzada con `admin` y `vendedor` sobre los 4 MS, con la consulta de cada BD como evidencia (SQL en MySQL y SQL Server, `find` en Mongo).
- [ ] Pruebas del adaptador.

### US-128 – Listado de usuarios sin contraseña (Mauricio) · 4 pts
- [ ] `GET api/usuarios` devuelve un DTO sin contraseña, sin hash y sin ids numéricos; solo Administrador.
- [ ] `Usuarios/Index` se reconecta a S2 por el gateway (hoy depende del monolito). Columnas: nombre completo, usuario, correo, rol y estado.
- [ ] La baja de usuario (sin puntos en esta rúbrica) sigue funcionando con el modal: no a uno mismo ni al último Administrador.
- [ ] Revisar el HTML y el JSON para confirmar que no viaja nada sensible.

### US-129 – Cambio de contraseña (Mauricio) · 8 pts + bug del docente
- [ ] `Cuenta/CambiarContrasena`: contraseña actual y nueva dos veces; accesible para cualquier usuario con sesión.
- [ ] **Bug «no espera a validar la nueva contraseña»:** validación en vivo con una lista de las 5 reglas (8 caracteres, número, mayúscula, minúscula y carácter especial) y el botón deshabilitado hasta cumplirlas. El servidor valida de nuevo con `PasswordPolicyValidator` y responde con un mensaje por regla incumplida (`Result`).
- [ ] Argon2 se mantiene. **El cambio rige desde el próximo inicio de sesión:** al guardar se cierra la sesión y se pide ingresar de nuevo.
- [ ] `PUT api/usuarios/contrasena` acepta el token con `debe_cambiar_contrasena` (lo usa Mateo en US-131) y registra `UsuarioModificacionId`.
- [ ] Recuperar `PasswordPolicyValidatorTests` desde `v3-sba` y adaptarlas.

### US-130 – Alta de usuario: nombre por protocolo y contraseña temporal por correo (Mateo) · 12 pts + bugs del docente
- [ ] **Bug de nombres:** el formulario acepta nombres con espacios («Juan José») y pide `ApellidoPaterno` (obligatorio) y `ApellidoMaterno` (opcional). Se normalizan: sin espacios sobrantes y con mayúscula inicial en cada palabra, conservando tildes.
- [ ] **Protocolo de nombre de usuario** *(propuesta, confirmar)*: inicial del primer nombre + apellido paterno + inicial del materno, en minúsculas, sin tildes ni espacios, con sufijo numérico si ya existe (`UsernameGenerator`). Juan José Pérez Rojas → `jperezr`.
- [ ] **Contraseña temporal segura:** aleatoria criptográfica (`RandomNumberGenerator`), 12 caracteres que cumplen la política; en la BD solo su hash Argon2; vence en 48 horas (`ContrasenaTemporalExpiracion`); el usuario queda Pendiente.
- [ ] Correo por `SmtpEmailSender` con el usuario, la contraseña temporal y el enlace al login **del Front** (`BASE_URL`). La contraseña nunca aparece en la UI, en los logs ni en la respuesta de la API.
- [ ] **Facade:** `UsuarioFacade` orquesta nombre de usuario, contraseña temporal y correo.
- [ ] **Result:** correo con formato válido y único, campos obligatorios. Contraseña temporal vencida → login rechazado con mensaje genérico.
- [ ] Pruebas: `UsernameGeneratorTests` (espacios, tildes, duplicados, sin materno) y `CreateUserUseCaseTests`.

### US-131 – Primer inicio de sesión con cambio obligatorio (Mateo) · 6 pts + bug crítico
- [ ] **Bug crítico:** un usuario Pendiente que inicia sesión **no puede entrar a ningún otro apartado**. Un middleware del Front *(propuesto `RequiereCambioContrasenaMiddleware`)* redirige toda página, salvo `CambiarContrasena` y `Logout`, a la página de cambio en «modo primer ingreso» (se reutiliza la de Mauricio).
- [ ] Defensa en el servidor: con `debe_cambiar_contrasena` los MS responden 403 (US-126), así que escribir la URL o llamar a la API tampoco funciona.
- [ ] Al cambiarla: Estado → Activo, `ContrasenaTemporalExpiracion` → nulo, se cierra la sesión y el siguiente login ya trae un token normal.
- [ ] Prueba: un Pendiente intenta `/Ventas/Registrar`, `/Clientes` y una llamada directa a la API con su token, y todo queda bloqueado.

---

## 11. Documento, ortografía e integración

### US-132 – Documento: arquitectura y repositorio (Edson)
- [ ] C4 niveles 1, 2 y 3 (**uno por microservicio**) y el combinado 1-2-3, **con la tecnología de cada contenedor** (.NET 10, YARP, MySQL, SQL Server, MongoDB, JWT RS256).
- [ ] Sección de repositorio: URL, ramas (`main`, `develop`, `feature/US-xx`), tags y forma de trabajo con PR.

### US-133 – Documento: patrones de diseño y estilos arquitectónicos (Mauricio)
- [ ] Diagramas de clases y capturas de Adapter, Result, Factory Method, Facade y Singleton, armados con los insumos de las tarjetas.
- [ ] Capturas de los estilos: Microservicios (gateway y BD por servicio), Hexagonal (Clientes, Ventas y Usuarios) y Clean (Productos).

### US-134 – Documento: contenido restante y entrega (Mateo; revisa Cesar)
- [ ] Carátula, introducción, **marco teórico** (microservicios, estilos, patrones y lenguajes aplicados), descripción del proyecto, requisitos funcionales, **diagramas de las 4 BD**, conclusiones (microservicios, patrones, dificultades y recomendaciones) y bibliografía.
- [ ] **Enlace al repositorio en el documento** (condición de los 10 pts de Clean Code y SOLID).
- [ ] `README` actualizado a microservicios (hoy describe SBA y una sola BD).
- [ ] Revisión final y subida al LMS por **los 6 integrantes**.

### US-135 – Revisión ortográfica de toda la UI (todos; coordina Mateo)
- [ ] Cada persona revisa sus pantallas; Mateo hace el pase final.
- [ ] Alcance: etiquetas, botones, validaciones, mensajes de `Result`, modales, comprobante, correo de la contraseña temporal y páginas de error.
- [ ] Cada error vale −1 punto: se pasa un corrector sobre los `.cshtml`, los `.js` y los mensajes en C#.

### US-136 – Regresión integral (Cesar para el G1, Mauricio para el G2)
- [ ] Checklist con los 6 proyectos y las 4 BD corriendo: CRUD de Clientes y Productos, venta completa (buscadores, alta rápida, compensación de stock), corrección y anulación, login por rol, alta con correo, primer ingreso, cambio de contraseña, bitácora y rechazo de tokens inválidos.
- [ ] Cada falla se abre como tarjeta de bug asignada a su dueño.
- [ ] Se ejecuta dos veces: jueves 19 por la noche y domingo 22.

### US-137 – Congelamiento, `main` y ensayo de defensa (Edson y Gabriel, lunes 23)
- [ ] Todo arranca limpio desde `develop` en una máquina sin nada previo (compose + scripts + `run-all.ps1`).
- [ ] Merge de `develop` a `main` con el tag `v4-microservicios`.
- [ ] Ensayo: cada integrante explica en 3 minutos su MS o su parte (patrones, estilo, SOLID, por qué su BD) y responde preguntas cruzadas.

### US-138 – (Opcional) Contenerizar microservicios, gateway y Front (Edson)
- [ ] `Dockerfile` por proyecto y servicios en `docker-compose.yml` con URLs internas.
- [ ] **Pasa a prioritaria** si el docente lo exige en clase; si no, solo se hace si sobra tiempo después de la primera regresión.

---

## 12. Bugs del docente → historias

| Bug | Grupo | Historia |
|---|---|---|
| Venta: no dejar guardar si se supera el stock (hoy recorta un dígito a la derecha) | G1 | US-120 (Cesar) |
| Comprobante: iniciales o acrónimo del vendedor, sin el nombre explícito | G1 | US-120 (Cesar), con el claim `iniciales` de US-125 |
| Alta de usuario: normalizar apellidos paterno y materno; el nombre debe aceptar espacios («Juan José») | G2 | US-130 (Mateo) |
| **Crítico:** antes de activar la cuenta con su contraseña se puede entrar a otros apartados | G2 | US-131 (Mateo), con la política de US-126 |
| Cambio de contraseña: no espera a validar la nueva | G2 | US-129 (Mauricio) |

---

## 13. Dependencias y cronograma

```
Sáb 14 (mañana) Edson: US-109 retiro del monolito → US-110 esqueleto → develop
                Todos: sesión de traspaso (1 h) + lectura de la sección 6
                    ↓
Sáb 14 – Dom 15 Edson: US-111 BD por servicio → US-112 gateway → US-113 base del Front
                En paralelo, desde develop:
                  Cesar:   US-117 (Ventas sin catálogos, BD propia)
                  Josue:   US-121 (Productos: Clean + Mongo)
                  Gabriel: US-124 (Usuarios hexagonal; hito: login el domingo)
                  Mauricio y Mateo: lectura de su código y pruebas contra datos de ejemplo
                    ↓
Lun 16 – Mar 17 Edson:    US-114 → US-115
                Cesar:    US-118 (contra datos de ejemplo hasta que Josue publique stock)
                Josue:    US-122 (stock y buscar primero: desbloquea a Cesar)
                Gabriel:  US-125 (JWT + Front)
                Mauricio: US-128 → US-127
                Mateo:    US-130
                    ↓
Mié 18          Edson: US-116 · Cesar: US-119 · Josue: US-123
                Gabriel: US-126 · Mauricio: US-129 · Mateo: US-131
                    ↓
Jue 19          Integración: venta completa por el gateway con JWT real; US-120;
                prueba cruzada de bitácora (US-127); primera regresión (US-136)
                    ↓
Vie 20 – Dom 22 Correcciones; US-132, US-133, US-134; US-135; segunda regresión;
                US-138 solo si el docente la exige o sobra tiempo
                    ↓
Lun 23          US-137: congelamiento, develop → main, ensayo
Mar 24          Defensa
```

**Desbloqueos clave:**

- **US-109 + US-110 en `develop`** → todos ramifican.
- **US-111** → cada dueño trabaja sobre su BD real.
- **Hito de login de US-124** → Mauricio y Mateo prueban contra Usuarios real.
- **Stock y `buscar` de US-122** → Cesar conecta la venta real.
- **US-125** → todos pasan del usuario de desarrollo al JWT real.
- **US-126 + US-127** → la bitácora se prueba de punta a punta.

| Día | Meta |
|---|---|
| Sáb 14 | US-109 y US-110 en `develop`; traspaso; arrancan US-117, US-121 y US-124 |
| Dom 15 | US-111, US-112 y US-113 en `develop`; hito de login de US-124 |
| Lun 16 | US-117 en `develop`; US-114; avance de US-118, US-122, US-125, US-128 y US-130 |
| Mar 17 | US-115, stock de US-122, US-125, US-128 y US-130 en `develop` |
| Mié 18 | US-116, US-118, US-119, US-123, US-126, US-127, US-129 y US-131 |
| Jue 19 | US-120, integración completa y primera regresión |
| Vie 20 | Correcciones; US-132 y US-133 |
| Sáb 21 | US-134 y US-135; US-138 si corresponde |
| Dom 22 | Segunda regresión y correcciones finales |
| Lun 23 | Congelamiento: US-137, `develop` → `main` y ensayo |
| Mar 24 | Defensa |

---

## 14. Riesgos y cómo se cubren

| Riesgo | Mitigación |
|---|---|
| La venta entre microservicios se complica | Compensación local en `StockFacade`; si no alcanza el tiempo, se recorta el comprobante, nunca el registro ni la anulación |
| Mongo sin transacciones de varios documentos | Diseño de la sección 5: operaciones atómicas sobre un solo documento |
| Cada grupo hereda código que no escribió | Sesión de traspaso del sábado + criterio de la DoD «puedo explicarlo en 3 minutos» + ensayo del lunes |
| Conflictos en archivos compartidos | Dueños de carpeta, dos archivos de rutas en el gateway, métodos de extensión por módulo y `VentasApiClient` partido en la base |
| Claves JWT distintas entre máquinas | Un solo par de desarrollo generado por Gabriel; la pública se sube y la privada se comparte por el canal privado |
| Nombres de tabla en MySQL (Linux distingue mayúsculas) | Usar siempre `Cliente`, igual en scripts y repositorio |
| El docente exige contenedores para los MS | US-138 lista para pasar a prioritaria |
| Datos de prueba inconsistentes entre BD | GUID fijos en `datos-semilla.md`, definidos una sola vez en US-111 |
