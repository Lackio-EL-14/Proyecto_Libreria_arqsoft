# Planning — Sprint de Migración a Microservicios (Gateway + 4 MS + Keycloak)

*Arquitectura de Software · 3.er Elemento de Competencia · Entregable único · Defensa: 24/11/2026 · Historias US-109 a US-140*
*Versión 2 · 17/11/2026 · Incluye las consignas finales del docente*

| Grupo 1 — Clientes, Proveedores y Productos (70 pts) | Grupo 2 — Usuarios, Keycloak y bitácora (70 pts) |
|---|---|
| Edson · Cesar · Josue | Mateo · Gabriel · Mauricio |

Hoy es martes 17/11: quedan **6 días de trabajo** (miércoles 18 a lunes 23) y la defensa es el martes 24. Edson ya terminó US-109 a US-116. Esta versión **reemplaza** las historias que cambiaron con las consignas del 17/11 y **agrega al final** US-139 (Result) y US-140 (Adapter). El tablero de Trello no se modifica: los números de las historias se conservan, aunque algunos títulos cambian.

> Los nombres de clases, archivos y endpoints **existentes** se citan tal como están en el código. Los nombres **nuevos** son propuestas y van marcados con *(propuesto)*; el dueño de la historia puede cambiarlos avisando al grupo. Lo marcado *(confirmar con Cesar)* depende de lo que él ya implementó en Proveedores.

---

## 0. Cambios del 17/11 (leer primero)

| Tema | Antes (versión 1) | Ahora (consigna del docente) |
|---|---|---|
| **Ventas** | MS Ventas con venta entre servicios | **Fuera de este entregable, sí o sí.** Será un 5.º microservicio en la entrega siguiente. Se conserva en una rama (US-120). Sin sagas |
| **2.º MS relacional** | Ventas (SQL Server) | **Proveedores** (SQL Server *(confirmar con Cesar)*), entidad nueva normalizada en 3FN. Ya lo implementó Cesar |
| **CRUD** | Varias tablas por servicio | **Una sola entidad con CRUD por microservicio:** Cliente, Proveedor y Producto. Categoría y Marca viven en Productos, pero no necesitan CRUD |
| **Autenticación y JWT** | Usuarios firmaba sus propios JWT (RS256) | **Keycloak** es el proveedor de identidad: emite los JWT y se encarga del login, las sesiones, las contraseñas y el cambio obligatorio en el primer ingreso |
| **Adapter** | Repositorios y clientes HTTP como adaptadores | **Un adaptador por API consumida desde el Front (4):** una interfaz que la UI usa como puente hacia cada microservicio (US-140) |
| **Result** | `Comun/Result.cs` con tipo de error | **En la capa de dominio**, con la forma que pidió el docente: dos booleanos, un método de error con mensaje que devuelve el mismo objeto y otro que devuelve el arreglo de errores (US-139) |

**Lo que pide el enunciado (EC3), sin cambios:**

- 4 microservicios heterogéneos, **un API Gateway** y un Front en Razor Pages, integrados en un solo producto.
- **Al menos 4 patrones de diseño; Adapter y Result son obligatorios.** Se mantienen Clean Code y SOLID.
- Grupo 1: 2 MS relacionales con proveedores distintos y **Hexagonal** (22 pts cada uno) + 1 MS no relacional con **Clean** (26 pts). CRUD de una entidad por MS y validaciones con **Result**.
- Grupo 2: MS de Usuarios con login, sesiones, **JWT validado por cada microservicio**, bitácora, CRUD de Usuario, cambio de contraseña y **primer inicio de sesión con cambio obligatorio**.
- Cada error ortográfico en la UI resta 1 punto.

**Decisiones vigentes:**

| Tema | Decisión |
|---|---|
| Servicios del Grupo 1 | **Clientes** (MySQL, Hexagonal) · **Proveedores** (SQL Server, Hexagonal) · **Productos** (MongoDB, Clean) |
| Servicio del Grupo 2 | **Usuarios** en C# .NET 10, SQL Server y **Hexagonal**, con **Keycloak** como proveedor de identidad |
| CRUD por servicio | Cliente · Proveedor · Producto · Usuario. Orientados a la entrega siguiente: clientes y productos alimentan las ventas; proveedores, las compras y el abastecimiento |
| Alcance | Fuera: Ventas, Reportes, sagas, orquestadores y bus de eventos. YARP reemplaza a Nginx |
| Gateway | **Uno solo**, con dos archivos de rutas: `yarp.g1.json` (Grupo 1) y `yarp.g2.json` (Grupo 2) |
| Contenedores | **Obligatorio:** una BD por contenedor (4) + Keycloak. **Opcional (US-138):** contenerizar MS, gateway y Front |
| Monolito | Eliminado en US-109 (hecho). Respaldo en el tag `v3-sba` |
| Bugs de venta y comprobante | **Pospuestos** a la entrega de Ventas; quedan anotados en la rama de conservación (US-120) |

---

## 1. Arquitectura objetivo

```
                         ┌──────────── Keycloak (proveedor de identidad, emite los JWT)
                         │ OpenID Connect           ▲ claves públicas (JWKS)
                         ▼                          │
Front (Razor Pages) ─► API Gateway (YARP) ──────────┤ valida el JWT
  4 adaptadores           │                         │
  (uno por API)           ├─► Clientes    (Hexagonal) ─► clientes-db    (MySQL)
                          ├─► Proveedores (Hexagonal) ─► proveedores-db (SQL Server)
                          ├─► Productos   (Clean)     ─► productos-db   (MongoDB)
                          └─► Usuarios    (Hexagonal) ─► usuarios-db    (SQL Server)
                                   └─► API de administración de Keycloak (alta, baja, contraseñas)
```

**Reglas:**

- El Front solo conoce **la URL del gateway** (para las API) y **la de Keycloak** (para el login). Consume cada API mediante **su adaptador** (US-140) y usa DTOs propios.
- Cada MS tiene **su propia BD** y nadie toca la BD de otro. En este entregable **no hay llamadas entre microservicios**.
- No hay bibliotecas compartidas: lo que dos servicios necesiten (por ejemplo, `Result`) se copia.
- **Cada microservicio valida el JWT de Keycloak por sí mismo**, aunque el gateway también lo valide.

**Proyectos y puertos:**

| Proyecto | Responsabilidad | Puerto |
|---|---|---|
| `Libreria.Client` | Front (Razor Pages) | 5000 |
| `Libreria.Gateway` | API Gateway (YARP) | 5080 |
| MS Proveedores *(nombre de proyecto: confirmar con Cesar)* | MS Proveedores | 5101 *(confirmar con Cesar)* |
| `Libreria.Servicio.Usuarios` | MS Usuarios | 5102 |
| `Libreria.Servicio.Clientes` | MS Clientes | 5103 |
| `Libreria.Servicio.Productos` | MS Productos | 5104 |

| Contenedor | Motor | Puerto en el host | BD |
|---|---|---|---|
| `clientes-db` | MySQL 8 | 3306 | `libreria_clientes` |
| `proveedores-db` *(confirmar con Cesar)* | SQL Server 2022 | 1433 | `LibreriaProveedores` *(confirmar con Cesar)* |
| `productos-db` | MongoDB | 27017 | `libreria_productos` |
| `usuarios-db` | SQL Server 2022 | 1434 | `LibreriaUsuarios` |
| `keycloak` | Keycloak (modo desarrollo) | 8080 | Realm `libreria` |

**Patrones (mínimo 4; Adapter y Result obligatorios):**

| Patrón | Dónde |
|---|---|
| **Adapter** | **Front:** `IClientesApi`, `IProveedoresApi`, `IProductosApi` e `IUsuariosApi` *(propuestos)*, cada una con su adaptador sobre `HttpClient` (US-140). **Dentro de los MS:** repositorios MySQL, SQL Server y Mongo detrás de sus puertos; adaptador de Keycloak en Usuarios |
| **Result** | Capa de **dominio** de los 4 MS (US-139); lo usan las validaciones de cada CRUD |
| **Factory Method** | `CrudRepositoryFactory<T>` en Clientes, Proveedores y Productos; `HistoricoRepositoryFactory` en Productos; `UsuarioRepositoryFactory` en Usuarios |
| **Singleton** | `ConnectionStringSingleton` en Clientes, Proveedores y Usuarios; `MongoClient` único en Productos |
| **Facade** | `UsuarioFacade` en Usuarios. `VentaFacade` y `StockFacade` se van con Ventas a la rama de conservación |

El Grupo 1 demuestra 4 patrones en su propio código (Adapter, Result, Factory Method y Singleton); el Grupo 2, los 5.

---

## 2. Mapa de migración (actualizado)

| Destino | Qué contiene |
|---|---|
| **MS Clientes** | ✅ Hecho (US-114 a US-116) |
| **MS Proveedores** | Nuevo, implementado por Cesar: entidad `Proveedor` y sus tablas dependientes en 3FN. Mismo estilo hexagonal que Clientes |
| **MS Productos** | Desde `v3-sba`: `Producto`, `Categoria`, `Marca`, `UbicacionesCategoria`, `PaisesMarca`, sus validadores, `CrudRepositoryFactory<T>` y `HistoricoRepositoryFactory`. **CRUD solo de Producto**; Categoría y Marca, solo lectura para los selects |
| **MS Usuarios** | Estructura hexagonal (ya en curso). Las contraseñas, el login y el token **pasan a Keycloak**: salen `Argon2PasswordHasher`, `LoginUseCase`, el token de activación y la emisión propia de JWT. Se conservan `UsuarioFacade`, `UsuarioRepositoryFactory`, `SqlUsuarioRepositoryFactory`, `ConnectionStringSingleton`, `SmtpEmailSender` y `UsernameGenerator` |
| **Rama de conservación de Ventas** | `Libreria.Servicio.Ventas`, sus pruebas, `db/ventas/`, y en el Front `Pages/Ventas`, `Clients/Ventas`, `DTOs/Ventas`, `_MenuVentas` y `venta-registrar.js` (US-120) |
| **Front** | Pierde todo lo de Ventas; los clientes tipados se convierten en los 4 adaptadores (US-140); el login pasa a OpenID Connect con Keycloak (US-125) |

---

## 3. Rúbrica → historias

| Ítem | Pts | Historias |
|---|---|---|
| **G1 · Clientes** (MySQL + Hexagonal): Select 2 · Insert 6 · Update 6 · Delete 2 · Result 6 | 22 | US-114, US-115, US-116 ✅ + US-139 |
| **G1 · Proveedores** (SQL Server + Hexagonal): Select 2 · Insert 6 · Update 6 · Delete 2 · Result 6 | 22 | US-117, US-118, US-119 |
| **G1 · Productos** (MongoDB + Clean): Select 6 · Insert 6 · Update 6 · Delete 2 · Result 6 | 26 | US-121, US-122, US-123 |
| **G2 · Login + manejo de sesiones + implementación de JWT** | 20 | US-124, US-125, US-126 |
| **G2 · Bitácora en los atributos de auditoría** | 20 | US-127 |
| **G2 · CRUD de Usuario:** Select sin contraseña | 4 | US-128 |
| **G2 · CRUD de Usuario:** Insert (nombre por protocolo + contraseña segura por correo) | 12 | US-130 |
| **G2 · Cambio de contraseña** | 8 | US-129 |
| **G2 · Primer inicio de sesión** | 6 | US-131 |
| Patrones obligatorios (Adapter y Result) | — | US-139, US-140 (requisito del enunciado y de la defensa) |
| Clean Code y SOLID (requiere el enlace al repositorio en el documento) | 10 | Transversal + US-134 |
| Documento (Anexo 1) | 10 | US-132, US-133, US-134 |
| Defensa y presentación | 10 | US-137 |
| Ortografía (−1 por error) | — | US-135 |

**Riesgo de puntos:** cualquier rastro de Ventas en `develop` (menú, páginas, rutas del gateway, proyectos) baja la nota. US-120 lo retira el miércoles 18 a primera hora.

---

## 4. Tablero de Trello

Sin cambios de estructura: listas, etiquetas y plantilla de tarjeta como en la versión 1. Las tarjetas US-117 a US-131 conservan su número; el detalle vigente es el de este documento.

**Definition of Done (se copia a todas las tarjetas):**

- [ ] Funciona de punta a punta: **Front → adaptador → gateway → MS → su propia BD**, con un token real de Keycloak (o el usuario de desarrollo antes de US-126).
- [ ] Las validaciones de negocio devuelven el `Result` del dominio (US-139); las excepciones quedan solo para errores inesperados.
- [ ] Las pruebas del código que se toca pasan (`dotnet test`), y las nuevas reglas tienen al menos una prueba.
- [ ] PR a `develop` aprobado por alguien distinto al autor; si toca la carpeta de otro, lo aprueba su dueño.
- [ ] Sin rastros de Ventas, sin `alert()`/`confirm()` nativos y sin faltas de ortografía en lo que toca.
- [ ] Código limpio y defendible: nombres claros, métodos cortos, una responsabilidad por clase, dependencias por interfaz.
- [ ] Insumos para el documento en un comentario de la tarjeta: captura del código y 2–3 líneas del patrón, estilo o principio SOLID aplicado (y el diagrama de la BD si la historia la toca).
- [ ] El autor puede explicar su código en 3 minutos: qué patrón aplica, en qué capa vive y por qué.

---

## 5. Bases de datos

**Convenciones comunes:**

- GUID: `CHAR(36)` en MySQL, `UNIQUEIDENTIFIER` en SQL Server, `string` en Mongo.
- **Auditoría en las 4 BD:** `UsuarioCreacionId` y `UsuarioModificacionId` guardan el **`sub` del token de Keycloak** (el id del usuario en Keycloak, un GUID), junto a `FechaCreacion` y `FechaModificacion`. Sin FK.
- Scripts **idempotentes** en una carpeta por servicio: `db/clientes/`, `db/proveedores/` *(confirmar con Cesar)*, `db/productos/` y `db/usuarios/`. Cada carpeta tiene un solo dueño.
- **Datos semilla con GUID fijos** en `docs/contratos/datos-semilla.md`. Los usuarios `admin` y `vendedor` se crean en Keycloak **con esos mismos GUID** (US-124), para que la auditoría semilla apunte a usuarios reales.

| Servicio · BD | Contenido |
|---|---|
| **Clientes** · MySQL | ✅ `Cliente` con `PublicId`, `Estado` y auditoría (hecho en US-111 y US-114) |
| **Proveedores** · SQL Server | **`Proveedor`** y sus tablas dependientes, en **3FN**, tal como las implementó Cesar. **Revisión de 3FN** (criterio de US-117): ningún atributo que no sea clave depende de otro atributo que no sea clave. **Propuesta mínima, si aún falta normalizar:** `Proveedor` (`PublicId`, `Nit` único, `RazonSocial`, `Telefono`, `Correo`, `Direccion`, `CiudadId`, `Estado`, auditoría) y `Ciudad` (catálogo: `CiudadId`, `Nombre`), porque el nombre de la ciudad depende de la ciudad y no del proveedor |
| **Productos** · MongoDB | **`productos`** con `categoria: { publicId, nombre }`, `marca: { publicId, nombre }` y `historicoCostos: [ … ]` embebidos. **`categorias`** y **`marcas`** como catálogos de solo lectura con datos semilla. Índices únicos por `publicId` |
| **Usuarios** · SQL Server | **`Rol`** igual. **`Usuario`**: datos del perfil (`Nombres`, `ApellidoPaterno`, `ApellidoMaterno`, `Correo`, `NombreUsuario`, `RolId`, `Estado`, auditoría) + **`KeycloakId UNIQUEIDENTIFIER UNIQUE`** (el `sub`). **Se eliminan** las columnas de contraseña y de token de activación: las credenciales viven solo en Keycloak |
| **Keycloak** | Su propio almacenamiento (modo desarrollo, con volumen). Guarda credenciales, roles y sesiones. El realm se versiona como `keycloak/realm-libreria.json` *(propuesto)* **sin secretos** |

**Ventas:** su esquema queda en la rama de conservación (US-120), listo para el 5.º microservicio de la entrega siguiente.

---

## 6. Contratos

Edson los copia a `docs/contratos/` al cerrar US-139. Cambiar un contrato se avisa en el grupo del equipo **antes** de hacer el PR.

### 6.1 URLs y configuración

- Front: `Servicios:Gateway:BaseUrl` = `http://localhost:5080` y `Keycloak:Authority` = `http://localhost:8080/realms/libreria`.
- Gateway y MS: `Keycloak:Authority` (la misma) y `Keycloak:Audience` = `libreria-api`.
- Secretos por variable de entorno, nunca en el repositorio: cadenas de conexión (`ConnectionStrings__<Nombre>Db`), secretos de los clientes de Keycloak (`Keycloak__ClientSecret`) y credenciales SMTP.

### 6.2 Rutas del gateway

| Archivo (dueño) | Ruta | Destino |
|---|---|---|
| `yarp.g1.json` (G1) | `/api/clientes/{**resto}` | Clientes (5103) |
| | `/api/proveedores/{**resto}` | Proveedores (5101 *(confirmar)*) |
| | `/api/productos/{**resto}`, `/api/categorias/{**resto}`, `/api/marcas/{**resto}`, `/api/historico/{**resto}` | Productos (5104) |
| `yarp.g2.json` (G2) | `/api/usuarios/{**resto}` | Usuarios (5102) |

**Se eliminan** `/api/ventas` y `/api/auth`: no hay ventas y el login lo hace Keycloak, no el gateway.

### 6.3 Keycloak y JWT

| Aspecto | Contrato |
|---|---|
| Realm | `libreria`, con idioma por defecto **español** e internacionalización activada |
| Roles del realm | `Administrador` · `Vendedor` |
| Clientes *(propuestos)* | `libreria-front`: confidencial, flujo de código de autorización (OpenID Connect), redirección `http://localhost:5000/signin-oidc`. `libreria-usuarios-admin`: cuenta de servicio con `manage-users` y `view-users` de `realm-management`, para que el MS Usuarios cree, dé de baja y cambie contraseñas. `libreria-pruebas`: solo desarrollo, para obtener tokens en Swagger y `.http` |
| Firma | **RS256**, firmada por Keycloak. El gateway y los MS obtienen las claves públicas solos desde `Keycloak:Authority` (JWKS) |
| Audiencia | `libreria-api` (mapeador de audiencia en Keycloak) |
| Claims que usamos | `sub` (id del usuario en Keycloak) · `preferred_username` · `name` · `email` · **`roles`** (mapeador de roles del realm con ese nombre de claim, para que .NET los lea con `RoleClaimType = "roles"`) |
| Duración | Token de acceso de 60 minutos (configurado en el realm); la cookie del Front dura lo mismo |
| Política de contraseñas del realm | `length(8) and digits(1) and upperCase(1) and lowerCase(1) and specialChars(1)` |
| Primer ingreso | Contraseña **temporal** → Keycloak exige la acción `UPDATE_PASSWORD` y **no emite el token** hasta que el usuario la cambia |
| Transporte | `Authorization: Bearer <token>` en toda llamada Front → gateway → MS |
| Respuestas | Sin token, firma alterada, vencido o audiencia equivocada → **401**. Rol insuficiente → **403** |

### 6.4 Usuario actual y bitácora

- Cada MS tiene **su** puerto de usuario actual con `Guid? UsuarioId` (el `sub`) y `string? NombreUsuario` (`preferred_username`).
- **Adaptador definitivo:** `UsuarioActualDesdeToken` *(propuesto)* en `Infrastructure/Seguridad/` de cada MS (US-127). **Hasta entonces:** `UsuarioActualDesarrollo` (US-110, solo en `Development`).
- La carpeta **`Infrastructure/Seguridad/`** de cada MS es del **Grupo 2**. El Grupo 1 no la edita.
- Los nombres para «creado por» se piden a `GET api/usuarios/nombres?ids=<guid>,<guid>`, que busca por `KeycloakId`.

### 6.5 Result (dominio) y errores

Definido en US-139, con la forma que pidió el docente *(nombres propuestos según la pizarra)*:

| Miembro | Para qué |
|---|---|
| `bool IsSuccess` | La operación salió bien |
| `bool IsError` | La operación falló (siempre lo contrario de `IsSuccess`) |
| `static Result Success()` / `Result<T>.Success(T valor)` | Crea un resultado correcto |
| `static Result Error(string mensaje)` | Crea un error con su mensaje y **devuelve el mismo tipo de objeto** |
| `string[] Errors()` | **Devuelve el arreglo de errores** acumulados |

- Vive en **`Domain/`** de cada MS (copiado, sin biblioteca compartida).
- Las validaciones **devuelven mensajes, no lanzan excepciones**. Las excepciones quedan para lo inesperado (BD caída, red, configuración), y un middleware las traduce a 500 o 503.
- Traducción en el controlador: `IsError` → **400** (`ValidationProblemDetails` con los mensajes); recurso inexistente → **404**. Si un dueño necesita distinguir 409 (duplicado), puede agregar una propiedad opcional de tipo de error **sin quitar** los dos booleanos ni los dos métodos.

### 6.6 Endpoints de cada CRUD

| MS | Endpoints | Historia |
|---|---|---|
| Clientes | ✅ `GET api/clientes` · `GET api/clientes/{publicId}` · `POST` · `PUT api/clientes/{publicId}` · `PATCH api/clientes/{publicId}/estado` | US-115 |
| Proveedores | `GET api/proveedores` (filtros, orden) · `GET api/proveedores/{publicId}` · `POST api/proveedores` · `PUT api/proveedores/{publicId}` · `PATCH api/proveedores/{publicId}/estado` *(confirmar con Cesar)* + catálogos de solo lectura que necesite el formulario | US-118 |
| Productos | `GET api/productos` · `GET api/productos/{publicId}` · `POST` · `PUT` · baja lógica, con las rutas que ya existían. `GET api/categorias` y `GET api/marcas` solo lectura. **Se eliminan** `buscar` y los endpoints de stock (eran para Ventas) | US-122 |
| Usuarios | `GET api/usuarios` · `POST api/usuarios` · `PATCH api/usuarios/{publicId}/estado` · `PUT api/usuarios/contrasena` · `GET api/usuarios/nombres` | US-127 a US-131 |

---

## 7. Ramas y cómo evitar bloqueos

- Todo se integra en **`develop`** por PR. `main` solo recibe código estable el **lunes 23/11** (US-137).
- Ramas `feature/US-xx-nombre` desde `develop` actualizado; `git pull --rebase origin develop` **cada mañana y antes de cada push**; PR pequeños.
- **Miércoles 18, primera hora — dos PR que todos esperan:** US-120 (Ventas fuera de `develop`) y el hito A de US-139 (`Result` en el dominio). Después, todos hacen rebase.
- **Dueños de carpeta:**

| Carpeta | Dueño |
|---|---|
| `Libreria.Gateway/` (salvo los `yarp.*.json`), `docker-compose.yml`, `LibreriaSystem.slnx`, `tools/` | Edson |
| `yarp.g1.json` · `yarp.g2.json` | Grupo 1 · Grupo 2 |
| `Libreria.Servicio.Clientes/`, `db/clientes/` | Edson |
| MS Proveedores, `db/proveedores/` | Cesar |
| `Libreria.Servicio.Productos/`, `db/productos/` | Josue |
| `Libreria.Servicio.Usuarios/`, `db/usuarios/`, **`keycloak/`** | Gabriel (Mauricio y Mateo trabajan en sus casos de uso) |
| `Infrastructure/Seguridad/` de **cada** MS | Grupo 2 (Gabriel: validación del JWT · Mauricio: usuario actual) |
| `Domain/Result.cs` de **cada** MS | Edson crea la clase (US-139); cada dueño la usa en sus validaciones |
| Front: **`Adapters/`** *(propuesto)* y registro de los adaptadores | Edson (US-140) |
| Front: `Pages/Clientes`, `DTOs/Clientes`, `_MenuClientes` | Edson |
| Front: `Pages/Proveedores`, `DTOs/Proveedores`, `_MenuProveedores` *(propuesto)* | Cesar |
| Front: `Pages/Productos`, `Pages/Categorias`, `Pages/Marcas`, `Pages/Historico`, `DTOs/Productos`, `_MenuInventario` | Josue |
| Front: autenticación OpenID Connect, `Logout`, `Seguridad/`, handler del token | Gabriel |
| Front: `Pages/Cuenta/CambiarContrasena`, `Pages/Usuarios/Index`, `Services/Auditoria`, `_AuditoriaInfo` | Mauricio |
| Front: `Pages/Usuarios/Create` | Mateo |

- **Cambios en `docker-compose.yml`** (contenedor `keycloak`, `proveedores-db`): los pide su dueño y los aprueba Edson en el PR.
- **Contract-first:** quien consume algo que aún no está en `develop` trabaja contra la interfaz con datos de `datos-semilla.md`.

---

## 8. Historias base — Edson ✅ (hechas)

### US-109 – Retiro del monolito y de Reportes ✅ (Edson)
- [x] Tag `v3-sba`; eliminados el monolito, `Libreria.Application.Tests`, Reportes y el bloque «TEMPORAL» del Front.

### US-110 – Esqueleto de microservicios y gateway en la solución ✅ (Edson)
- [x] Proyectos de Clientes, Productos y gateway; `Result` inicial en los MS (lo reemplaza US-139); `UsuarioActualDesarrollo`; `run-all.ps1`.

### US-111 – Una base de datos por servicio ✅ (Edson)
- [x] Contenedores por BD, scripts por servicio y `datos-semilla.md`. El contenedor y los scripts de Ventas se reemplazan por los de Proveedores (Cesar, US-117) y se conservan en la rama de US-120.

### US-112 – API Gateway con YARP ✅ (Edson)
- [x] Gateway con `yarp.g1.json` y `yarp.g2.json`. Las rutas de Ventas y `/api/auth` se retiran en US-120 y US-124.

### US-113 – Base del Front y contratos publicados ✅ (Edson)
- [x] Una sola URL al gateway, clientes tipados por servicio y menú por módulo. Los clientes tipados se convierten en adaptadores en US-140.

---

## 9. Grupo 1 — Clientes, Proveedores y Productos · 70 pts

### US-114 – MS Clientes: núcleo hexagonal y adaptador MySQL ✅ (Edson) · Result 6 pts
- [x] Hecho. Su validación pasa al `Result` del dominio en US-139.

### US-115 – MS Clientes: API REST y rutas del gateway ✅ (Edson) · Insert 6 · Update 6 · Delete 2
- [x] Hecho.

### US-116 – UI de Clientes en el Front ✅ (Edson) · Select 2
- [x] Hecho. Pasa a consumir `IClientesApi` en US-140.

### US-117 – MS Proveedores: núcleo hexagonal y BD SQL Server en 3FN (Cesar) 🔴 · Result 6 pts
*Implementado por Cesar; esta tarjeta sirve para verificarlo contra los criterios.*
- [ ] Estructura hexagonal igual que Clientes: `Domain`, `Application/Ports/Primary`, `Application/Ports/Secondary`, `Application/Services`, `Application/Factories`, `Infrastructure`, `Controllers`.
- [ ] **Entidad `Proveedor` y sus tablas dependientes en 3FN** (sección 5), con `PublicId`, `Estado` y auditoría. Scripts idempotentes en su carpeta de `db/`.
- [ ] Contenedor SQL Server propio en `docker-compose.yml` (PR aprobado por Edson); se retira `ventas-db`.
- [ ] **Adapter:** repositorio ADO.NET (`Microsoft.Data.SqlClient`) detrás de su puerto. **Factory Method:** `CrudRepositoryFactory<Proveedor>` + fábrica concreta de SQL Server. **Singleton:** `ConnectionStringSingleton`.
- [ ] **Result (del dominio, US-139):** NIT obligatorio y único, razón social obligatoria, correo y teléfono con formato válido, ciudad existente.
- [ ] Los controladores llaman al servicio de aplicación, nunca al repositorio ni a la fábrica.
- [ ] Pruebas del servicio de aplicación con repositorio falso.

### US-118 – MS Proveedores: API REST y rutas del gateway (Cesar) · Insert 6 · Update 6 · Delete 2
- [ ] Endpoints de la sección 6.6 en un solo controlador.
- [ ] **Insert:** alta con las validaciones de US-117; registra `UsuarioCreacionId`.
- [ ] **Update:** edición sin chocar el NIT con otro proveedor; registra `UsuarioModificacionId`.
- [ ] **Delete lógico:** `Estado` inactivo, sin borrar la fila; se puede reactivar.
- [ ] Ruta `/api/proveedores` en `yarp.g1.json`. Probado con Swagger y `.http`.

### US-119 – UI de Proveedores en el Front (Cesar) · Select 2
- [ ] `Pages/Proveedores`: **listado con refresco, ordenación por columna y búsqueda**, alta, edición, baja con el modal compartido y reactivación.
- [ ] Consume `IProveedoresApi` (interfaz publicada en el hito A de US-140); nunca `HttpClient` directo.
- [ ] `_AuditoriaInfo` muestra creado y modificado por.
- [ ] Menú `_MenuProveedores`; acceso de Administrador *(propuesta, confirmar en el equipo)*.

### US-120 – Ventas fuera del entregable: rama de conservación y retiro de `develop` (Cesar) 🔴 · miércoles 18, primera hora
- [ ] Desde `develop` actualizado, crear y subir la rama **`conservar/ventas`** *(propuesto)* con el estado actual de Ventas (servicio, pruebas, scripts, páginas, JS, rutas) **y un README breve** que anote los bugs pendientes del docente: no dejar guardar si se supera el stock (hoy recorta un dígito) e iniciales del vendedor en el comprobante.
- [ ] En `develop`, eliminar: el proyecto de Ventas y sus pruebas (también de `LibreriaSystem.slnx`, con aviso a Edson), `db/ventas/`, las rutas `/api/ventas` de `yarp.g1.json`, y en el Front `Pages/Ventas`, `Clients/Ventas`, `DTOs/Ventas`, `_MenuVentas`, `venta-registrar.js`, su registro en `Program.cs` y cualquier enlace o tarjeta del inicio que apunte a ventas.
- [ ] `grep -ri "venta" src` solo devuelve coincidencias justificadas (por ejemplo, `PrecioVenta` en Productos).
- [ ] Build y tests en verde; el Front arranca sin opciones de venta.

### US-121 – MS Productos: estructura Clean y persistencia MongoDB (Josue) 🔴
- [ ] Capas Clean con dependencias hacia adentro: `Domain` (`Producto`, `Categoria`, `Marca`, histórico de costos, `UbicacionesCategoria`, `PaisesMarca`, `Result`), `Application` (casos de uso, abstracciones, DTOs, factorías), `Infrastructure` (Mongo), `Presentation/Controllers`.
- [ ] **Singleton:** `MongoClient` registrado una sola vez. **Factory Method:** `CrudRepositoryFactory<T>` y `HistoricoRepositoryFactory` sin cambios, con fábricas concretas para Mongo. **Adapter:** repositorios Mongo detrás de las abstracciones de `Application`.
- [ ] Categorías y marcas cargadas desde los datos semilla, **solo lectura**.
- [ ] Pruebas de los casos de uso con repositorios falsos.

### US-122 – MS Productos: CRUD de Producto (Josue) · Insert 6 · Update 6 · Delete 2 · Result 6
- [ ] **Solo Producto tiene CRUD.** Categoría y Marca exponen únicamente `GET` para los selects del formulario.
- [ ] **Insert:** producto con categoría y marca activas; el costo inicial entra a `historicoCostos` con motivo «Registro inicial».
- [ ] **Update:** si cambia el costo, `$set` + `$push` al histórico en una sola operación atómica.
- [ ] **Delete lógico:** baja sin borrar el documento; se puede reactivar.
- [ ] **Result (del dominio):** nombre obligatorio, precio y costo no negativos, perecedero con fecha de vencimiento, categoría y marca existentes y activas.
- [ ] Se eliminan los endpoints `buscar`, `stock/descontar` y `stock/restaurar` si ya existían.
- [ ] Rutas de Productos en `yarp.g1.json`.

### US-123 – UI de Productos conectada al MS (Josue) · Select 6
- [ ] **Listado de productos con refresco, ordenación por columna y filtros** (categoría, marca, estado).
- [ ] Alta, edición, baja y reactivación de productos; `Productos/Details` muestra el histórico de costos.
- [ ] Consume `IProductosApi` (hito A de US-140).
- [ ] Las páginas de `Categorias` y `Marcas` quedan como **listados de solo lectura** (o se ocultan del menú); su CRUD no suma en este entregable.

---

## 10. Grupo 2 — Usuarios, Keycloak y bitácora · 70 pts

> Lo que Gabriel ya avanzó de la estructura hexagonal y de la BD de Usuarios se conserva. Cambia la autenticación: **Keycloak emite los JWT y maneja login, sesiones y contraseñas.**

### US-124 – Keycloak como proveedor de identidad y MS Usuarios hexagonal (Gabriel) 🔴 · miércoles 18
- [ ] Servicio `keycloak` en `docker-compose.yml` (modo desarrollo, puerto 8080, volumen de datos, importación del realm al arrancar). Credenciales de administrador por variables de entorno. PR aprobado por Edson.
- [ ] `keycloak/realm-libreria.json` *(propuesto)* con lo de la sección 6.3: roles, los 3 clientes, mapeador de audiencia `libreria-api`, mapeador de roles con claim `roles`, política de contraseñas, idioma español y duración del token. **Sin secretos en el archivo.**
- [ ] Script `keycloak/configurar` *(propuesto)* que, con variables de entorno, asigna los secretos de los clientes y crea `admin` y `vendedor` **con los GUID de `datos-semilla.md`** y sus contraseñas de desarrollo.
- [ ] MS Usuarios hexagonal: puertos en `Application/Ports/Primary` y `Ports/Secondary`, controladores en `Controllers/`. Puerto **`IProveedorIdentidad`** *(propuesto)* con su **adaptador de Keycloak** (API de administración, cliente `libreria-usuarios-admin`): crear usuario, asignar rol, fijar contraseña temporal o definitiva, habilitar y deshabilitar.
- [ ] BD de Usuarios con el esquema de la sección 5 (`KeycloakId`, sin columnas de contraseña). Salen `Argon2PasswordHasher`, `LoginUseCase`, la emisión propia de JWT, el token de activación y sus pruebas.
- [ ] Se retira `/api/auth` de `yarp.g2.json`.
- [ ] **Hito (miércoles 18 por la noche):** Keycloak levanta desde cero con el realm y los usuarios semilla. Desbloquea a todos.

### US-125 – Login y sesiones con OpenID Connect en el Front (Gabriel) · Login + sesiones + JWT
- [ ] El Front autentica con **OpenID Connect contra Keycloak** (cliente `libreria-front`, flujo de código, cookie como sesión local, tokens guardados en la sesión).
- [ ] La página de login de Keycloak muestra el nombre del realm («Librería») y está en español; se revisa su ortografía (US-135).
- [ ] Un `DelegatingHandler` agrega `Authorization: Bearer <token>` a **todas** las llamadas de los adaptadores; reemplaza a `UsuarioActualDelegatingHandler` y a los encabezados `X-Usuario-*`.
- [ ] Cierre de sesión en el Front **y** en Keycloak.
- [ ] Un 401 de cualquier servicio cierra la sesión y lleva al login con un mensaje.
- [ ] Usuario dado de baja (deshabilitado en Keycloak): no puede volver a iniciar sesión y pierde la sesión al vencer el token.
- [ ] **Cada rol ve un menú distinto** (Administrador y Vendedor), validado también con políticas en el servidor.
- [ ] Comando documentado en `docs/contratos/` para obtener un token de prueba con `libreria-pruebas` (para Swagger y `.http`).

### US-126 – Validación del JWT de Keycloak en los 4 MS y en el gateway (Gabriel) · 20 pts
- [ ] En `Infrastructure/Seguridad/` de cada MS: un método de extensión *(propuesto `AddSeguridadJwt()`)* que configura `JwtBearer` con `Authority`, `Audience` = `libreria-api`, vencimiento y `RoleClaimType = "roles"`. Todo exige token.
- [ ] El gateway valida igual.
- [ ] Swagger de cada MS con el botón «Authorize».
- [ ] Pruebas por MS: sin token → 401; firma alterada → 401; vencido → 401; audiencia equivocada → 401; válido → 200; Vendedor en un endpoint de Administrador → 403. Evidencias en `.http` y capturas para la defensa.
- [ ] Las líneas de `Program.cs` de cada MS del Grupo 1 se agregan en un PR revisado por su dueño.

### US-127 – Bitácora basada en la sesión en los 4 MS (Mauricio) · 20 pts
- [ ] `UsuarioActualDesdeToken` *(propuesto)* en `Infrastructure/Seguridad/` de cada MS: lee `sub` y `preferred_username` del token de Keycloak; reemplaza a `UsuarioActualDesarrollo`.
- [ ] **Todo** CRUD llena la auditoría: Cliente, Proveedor, Producto y Usuario (alta, edición, baja y cambio de contraseña).
- [ ] `GET api/usuarios/nombres` busca por `KeycloakId`; `AuditoriaService` y `_AuditoriaInfo` del Front muestran los nombres.
- [ ] Prueba cruzada con `admin` y `vendedor` sobre los 4 MS, con la consulta de cada BD como evidencia.
- [ ] Pruebas del adaptador con un `ClaimsPrincipal` armado a mano.

### US-128 – Listado de usuarios sin contraseña (Mauricio) · 4 pts
- [ ] `GET api/usuarios` devuelve un DTO sin contraseñas ni ids numéricos; solo Administrador.
- [ ] `Usuarios/Index` consume `IUsuariosApi`. Columnas: nombre completo, usuario, correo, rol y estado.
- [ ] Estado **Pendiente** mientras el usuario no haya cambiado su contraseña temporal (se consulta a Keycloak por medio de `IProveedorIdentidad` *(propuesto)*), Activo o Inactivo.
- [ ] La baja (sin puntos en esta rúbrica) deshabilita al usuario en Keycloak y marca Inactivo en la BD: no a uno mismo ni al último Administrador.
- [ ] Revisar el HTML y el JSON para confirmar que no viaja nada sensible.

### US-129 – Cambio de contraseña (Mauricio) · 8 pts + bug del docente
- [ ] `Cuenta/CambiarContrasena`: contraseña actual y nueva dos veces; accesible para cualquier usuario con sesión.
- [ ] **Bug «no espera a validar la nueva contraseña»:** validación en vivo con la lista de las 5 reglas y el botón deshabilitado hasta cumplirlas.
- [ ] `PUT api/usuarios/contrasena`: verifica la contraseña actual contra Keycloak y fija la nueva por el adaptador de Keycloak. Si Keycloak la rechaza por su política, el mensaje vuelve como `Result` con un texto en español por regla incumplida.
- [ ] **El cambio rige desde el próximo inicio de sesión:** al guardar se cierra la sesión y se pide ingresar de nuevo.
- [ ] Registra `UsuarioModificacionId`.

### US-130 – Alta de usuario: nombre por protocolo y contraseña temporal por correo (Mateo) · 12 pts + bugs del docente
- [ ] **Bug de nombres:** el formulario acepta nombres con espacios («Juan José») y pide `ApellidoPaterno` (obligatorio) y `ApellidoMaterno` (opcional). Se normalizan: sin espacios sobrantes y con mayúscula inicial en cada palabra, conservando tildes.
- [ ] **Protocolo de nombre de usuario** *(propuesta, confirmar)*: inicial del primer nombre + apellido paterno + inicial del materno, en minúsculas, sin tildes ni espacios, con sufijo numérico si ya existe (`UsernameGenerator`). Juan José Pérez Rojas → `jperezr`.
- [ ] **Contraseña temporal segura:** aleatoria criptográfica (`RandomNumberGenerator`), 12 caracteres que cumplen la política del realm.
- [ ] **Facade:** `UsuarioFacade` orquesta nombre de usuario, alta en la BD, alta en Keycloak con la contraseña **temporal** y el rol, y el correo.
- [ ] Correo por `SmtpEmailSender` con el usuario, la contraseña temporal y el enlace al Front (`BASE_URL`). La contraseña nunca aparece en la UI, en los logs ni en la respuesta de la API.
- [ ] Si Keycloak falla, el usuario no queda a medias: se revierte el alta en la BD y se devuelve un `Result` con un mensaje claro.
- [ ] **Result (del dominio):** correo con formato válido y único, campos obligatorios.
- [ ] Pruebas: `UsernameGeneratorTests` (espacios, tildes, duplicados, sin materno) y alta con el proveedor de identidad falso.

### US-131 – Primer inicio de sesión con cambio obligatorio (Mateo) · 6 pts + bug crítico
- [ ] **Bug crítico resuelto por diseño:** con contraseña temporal, Keycloak muestra la pantalla de cambio obligatorio (`UPDATE_PASSWORD`) y **no emite el token** hasta que el usuario la cambia, así que no puede entrar a ningún apartado.
- [ ] Verificar y dejar evidencia: un usuario recién creado inicia sesión, ve la pantalla de cambio en español, no puede saltarla y, al cambiarla, entra con su rol.
- [ ] Prueba negativa: sin cambiar la contraseña, intentar `/Clientes`, `/Productos` y una llamada directa a la API → redirección al login o 401.
- [ ] Revisar la ortografía de los textos de esa pantalla; si alguno de Keycloak tiene errores, se sobrescribe en los textos del realm.
- [ ] Tras el cambio, el estado del usuario pasa a Activo en el listado (US-128).

---

## 11. Documento, ortografía e integración

### US-132 – Documento: arquitectura y repositorio (Edson)
- [ ] C4 niveles 1, 2 y 3 (**uno por microservicio**) y el combinado 1-2-3, con la tecnología de cada contenedor (.NET 10, YARP, Keycloak, MySQL, SQL Server, MongoDB, JWT RS256). Ventas aparece solo como trabajo futuro.
- [ ] Sección de repositorio: URL, ramas (`main`, `develop`, `feature/US-xx`, `conservar/ventas`), tags y forma de trabajo con PR.

### US-133 – Documento: patrones de diseño y estilos arquitectónicos (Mauricio)
- [ ] Diagramas de clases y capturas de **Adapter** (los 4 adaptadores del Front y el de Keycloak), **Result** (dominio), Factory Method, Facade y Singleton, armados con los insumos de las tarjetas.
- [ ] Capturas de los estilos: Microservicios (gateway, BD por servicio, proveedor de identidad), Hexagonal (Clientes, Proveedores y Usuarios) y Clean (Productos).

### US-134 – Documento: contenido restante y entrega (Mateo; revisa Cesar)
- [ ] Carátula, introducción, **marco teórico** (microservicios, Keycloak y OpenID Connect, estilos, patrones y lenguajes), descripción del proyecto, requisitos funcionales, **diagramas de las 4 BD** (incluida la 3FN de Proveedores), conclusiones (microservicios, patrones, dificultades y recomendaciones) y bibliografía.
- [ ] **Enlace al repositorio en el documento** (condición de los 10 pts de Clean Code y SOLID).
- [ ] `README` actualizado: microservicios, Keycloak y cómo levantar todo.
- [ ] Revisión final y subida al LMS por **los 6 integrantes**.

### US-135 – Revisión ortográfica de toda la UI (todos; coordina Mateo)
- [ ] Cada persona revisa sus pantallas; Mateo hace el pase final.
- [ ] Alcance: etiquetas, botones, validaciones, mensajes de `Result`, modales, correo de la contraseña temporal, páginas de error y **pantallas de Keycloak** (login y cambio de contraseña).
- [ ] Cada error vale −1 punto.

### US-136 – Regresión integral (Cesar para el G1, Mauricio para el G2)
- [ ] Checklist con todo corriendo: CRUD de Clientes, Proveedores y Productos; login por rol con Keycloak; alta con correo; primer ingreso; cambio de contraseña; bitácora en las 4 BD; rechazo de tokens inválidos; **ningún rastro de Ventas**.
- [ ] Cada falla se abre como tarjeta de bug asignada a su dueño.
- [ ] Se ejecuta dos veces: viernes 20 por la noche y domingo 22.

### US-137 – Congelamiento, `main` y ensayo de defensa (Edson y Gabriel, lunes 23)
- [ ] Todo arranca limpio desde `develop` en una máquina sin nada previo (compose + Keycloak + scripts + `run-all.ps1`).
- [ ] Merge de `develop` a `main` con el tag `v4-microservicios`.
- [ ] Ensayo: cada integrante explica en 3 minutos su parte (patrones, estilo, SOLID, por qué su BD o por qué Keycloak) y responde preguntas cruzadas.

### US-138 – (Opcional) Contenerizar microservicios, gateway y Front (Edson)
- [ ] `Dockerfile` por proyecto y servicios en `docker-compose.yml` con URLs internas.
- [ ] Ojo con Keycloak: el emisor (`iss`) del token debe coincidir dentro y fuera de Docker; se fija la URL pública de Keycloak (`KC_HOSTNAME`).
- [ ] **Pasa a prioritaria** solo si el docente lo exige.

---

## 12. Historias nuevas — patrones obligatorios (Edson)

### US-139 – Patrón Result en la capa de dominio de los 4 MS (Edson) 🔴 · miércoles 18
- [ ] **Hito A (miércoles 18, mañana):** clase `Result` y `Result<T>` en **`Domain/`** de Clientes, Proveedores, Productos y Usuarios, con la forma de la sección 6.5: `IsSuccess`, `IsError`, `Success(...)`, `Error(string mensaje)` (devuelve el mismo tipo de objeto) y `Errors()` (devuelve el arreglo de errores). Se retira el `Comun/Result.cs` de US-110 sin romper la compilación (las referencias se actualizan en el mismo PR).
- [ ] Método de extensión en cada MS que traduce `Result` a `ValidationProblemDetails` (400) para los controladores.
- [ ] Middleware de excepciones inesperadas → 500 o 503 con `ProblemDetails` y sin detalles internos.
- [ ] **Clientes** (Edson) reemplaza todas sus excepciones de negocio por `Result`. Los demás dueños hacen lo mismo en sus validaciones (US-117, US-122, US-130); Edson revisa esos PR.
- [ ] Pruebas de la clase `Result` (éxito, un error, varios errores, `IsError` siempre opuesto a `IsSuccess`).
- [ ] Guía de una página en `docs/contratos/result.md` *(propuesto)*: cuándo devolver `Result` y cuándo lanzar una excepción, con un ejemplo antes y después.
- [ ] Insumo para el documento: diagrama de clases de `Result` y captura de una validación de cada MS.

### US-140 – Patrón Adapter: un adaptador por API consumida desde el Front (Edson) · miércoles 18 y jueves 19
- [ ] **Hito A (miércoles 18):** 4 interfaces destino en el Front, en `Adapters/` *(propuesto)*: `IClientesApi`, `IProveedoresApi`, `IProductosApi` e `IUsuariosApi` *(propuestos)*, con las operaciones que la UI necesita. Se publican primero para que Cesar, Josue, Mauricio y Mateo construyan sus páginas contra ellas.
- [ ] **Hito B (jueves 19):** 4 adaptadores (`ClientesApiAdapter`, `ProveedoresApiAdapter`, `ProductosApiAdapter`, `UsuariosApiAdapter` *(propuestos)*) que envuelven el `HttpClient` hacia el gateway: arman la URL, envían el token (handler de US-125) y traducen JSON, `ProblemDetails` y códigos HTTP a DTOs del Front y a mensajes para la UI.
- [ ] Reemplazan a los clientes tipados de US-113; las páginas y `PageModel` dependen **solo de la interfaz** (inversión de dependencias).
- [ ] Registro por `IHttpClientFactory` en un método de extensión propio.
- [ ] Servicio caído (502, 503 o tiempo agotado) → mensaje amigable con `ServicioNoDisponible`, nunca una excepción en la página.
- [ ] Pruebas de cada adaptador con un `HttpMessageHandler` falso: 200 → DTO; 400 → mensajes de error; 404; 503.
- [ ] Un PR por adaptador; el dueño de las páginas que lo consumen lo aprueba.
- [ ] Insumo para el documento: diagrama del patrón (cliente → interfaz destino → adaptador → `HttpClient` → API) y la explicación de «un puente por API».

---

## 13. Bugs del docente → historias

| Bug | Grupo | Historia |
|---|---|---|
| Venta: no dejar guardar si se supera el stock (hoy recorta un dígito) | — | **Pospuesto:** anotado en la rama `conservar/ventas` (US-120) |
| Comprobante: iniciales o acrónimo del vendedor, sin el nombre explícito | — | **Pospuesto:** anotado en la rama `conservar/ventas` (US-120) |
| Alta de usuario: normalizar apellidos paterno y materno; el nombre debe aceptar espacios («Juan José») | G2 | US-130 (Mateo) |
| **Crítico:** antes de activar la cuenta con su contraseña se puede entrar a otros apartados | G2 | US-131 (Mateo), resuelto con la acción obligatoria de Keycloak |
| Cambio de contraseña: no espera a validar la nueva | G2 | US-129 (Mauricio) |

---

## 14. Dependencias y cronograma

```
✅ EDSON  US-109 → US-116  TERMINADO

Mié 18 (mañana)  Cesar:  US-120 Ventas fuera de develop      ─┐ todos hacen rebase
                 Edson:  US-139 hito A (Result en el dominio) ─┘
Mié 18           Gabriel: US-124 Keycloak + realm + usuarios semilla
                 Edson:   US-140 hito A (4 interfaces) → US-139 en Clientes
                 Cesar:   US-117 (verificar) → US-118
                 Josue:   US-121 → US-122
                 Mauricio: US-128 · Mateo: US-130 (lógica y pruebas)
                    ↓
Jue 19           Gabriel:  US-125 (login OIDC) → US-126
                 Edson:    US-140 hito B (4 adaptadores)
                 Cesar:    US-119 · Josue: US-123
                 Mauricio: US-127 · Mateo: US-130 con Keycloak real
                    ↓
Vie 20           Mauricio: US-129 · Mateo: US-131
                 Integración completa y primera regresión (US-136)
                    ↓
Sáb 21           Documento: US-132, US-133, US-134 · Ortografía: US-135
Dom 22           Segunda regresión y correcciones finales
Lun 23           US-137: congelamiento, develop → main y ensayo
Mar 24           Defensa
```

**Quién desbloquea a quién:**

```
Cesar   US-120 (Ventas fuera)    →  todos (develop limpio)
Edson   US-139 hito A (Result)   →  Cesar, Josue, Gabriel, Mateo (validaciones con Result)
Edson   US-140 hito A (interfaces) → Cesar US-119, Josue US-123, Mauricio US-128, Mateo US-130 (UI)
Gabriel US-124 (Keycloak)        →  Gabriel US-125, Mateo US-130 y US-131, Mauricio US-129
Gabriel US-125 (login + token)   →  Mauricio US-127, Edson US-140 hito B (handler del token)
Gabriel US-126 + Mauricio US-127 →  prueba cruzada de bitácora (viernes 20)
```

| Quién | Puede empezar ya | Espera a |
|---|---|---|
| **Edson** | US-139 → US-140 hito A → US-140 hito B | Gabriel US-125 para probar los adaptadores con token real |
| **Cesar** | US-120 → US-117 → US-118 | Edson US-140 hito A para US-119 |
| **Josue** | US-121 → US-122 | Edson US-140 hito A para US-123 |
| **Gabriel** | US-124 → US-125 → US-126 | Nadie (Edson solo aprueba el cambio del compose) |
| **Mauricio** | `UsuarioActualDesdeToken` con claims de prueba, US-128 contra la interfaz | Gabriel US-125 para US-127; Gabriel US-124 para US-129 |
| **Mateo** | Lógica de US-130 (nombres, usuario, contraseña temporal) | Gabriel US-124 para el alta real y para US-131 |

**Las 4 entregas que no se pueden atrasar (todas el miércoles 18):**

1. **Cesar, US-120:** Ventas fuera de `develop`.
2. **Edson, US-139 hito A:** `Result` en el dominio.
3. **Edson, US-140 hito A:** las 4 interfaces del Front.
4. **Gabriel, US-124:** Keycloak levantado con el realm y los usuarios semilla.

---

## 15. Riesgos y cómo se cubren

| Riesgo | Mitigación |
|---|---|
| Queda algo de Ventas en `develop` y baja la nota | US-120 a primera hora del miércoles + `grep` en la regresión |
| Keycloak es nuevo para el equipo y Gabriel concentra el riesgo | Hito del miércoles por la noche; el realm se versiona y se importa solo; Mauricio y Mateo trabajan contra `IProveedorIdentidad` con un adaptador falso mientras tanto |
| Los usuarios de Keycloak se pierden al recrear el contenedor | Volumen de datos + script `keycloak/configurar` que recrea los usuarios semilla con sus GUID fijos |
| Secretos de Keycloak en el repositorio | El realm se exporta sin secretos; los secretos y las contraseñas semilla van por variables de entorno |
| Textos de Keycloak con errores ortográficos | Revisión de sus pantallas en US-135; sobrescritura de textos en el realm |
| El cambio de `Result` rompe ramas en curso | Hito A pequeño y temprano, actualizado en el mismo PR; todos hacen rebase el miércoles |
| Los adaptadores del Front chocan con las páginas de otros | Interfaces publicadas primero (hito A); un PR por adaptador aprobado por el dueño de las páginas |
| El emisor del token no coincide si se contenerizan los servicios | Solo afecta a US-138: se fija la URL pública de Keycloak |
| Cada grupo hereda código que no escribió | Criterio de la DoD «puedo explicarlo en 3 minutos» + ensayo del lunes |
