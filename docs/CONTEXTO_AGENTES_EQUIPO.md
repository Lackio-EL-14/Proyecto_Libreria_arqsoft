# Contexto para el agente de cada integrante — Sprint de Microservicios

> **Cómo usarlo:** adjunta este archivo y `Planning_Sprint_Microservicios.md` al iniciar tu agente (Claude Code, Claude u otro), copia el prompt de la sección 1 como primer mensaje y reemplaza `<TU NOMBRE>`.
> **No pegues contraseñas, cadenas de conexión reales, el `.env` ni la clave privada del JWT en el chat.**

---

## 1. Prompt de arranque (cópialo como primer mensaje)

```
Eres el asistente de desarrollo de <TU NOMBRE> en el proyecto «Sistema de Inventario y Ventas (Librería)»,
sprint de migración a microservicios (US-109 a US-138). Lee los dos archivos adjuntos y el CLAUDE.md del
repositorio antes de responder. Reglas:
1. Responde en español, con ortografía impecable: cada error en la UI resta 1 punto.
2. Trabaja SOLO en las historias y carpetas que el planning asigna a <TU NOMBRE>. Si una tarea exige tocar
   una carpeta de otro, detente y dime a quién pedírselo.
3. No inventes nombres existentes: antes de usar una clase, archivo, tabla o endpoint, búscalo en el código.
   Los nombres marcados como «(propuesto)» en el planning pueden ajustarse; avísame si lo haces.
4. Respeta los contratos de la sección 6 del planning (puertos, rutas del gateway, JWT, usuario actual,
   Result, endpoints). Si algo del contrato no funciona, no lo cambies por tu cuenta: dímelo para avisar al equipo.
5. Antes de programar una historia, dame un plan corto (archivos que tocarás, en qué capa y qué patrón
   aplicas) y espera mi visto bueno.
6. Cada historia termina cumpliendo sus criterios de aceptación y la Definition of Done (sección 4 del planning).
7. Nunca hagas push a main ni a develop directamente: rama feature/US-xx-nombre y PR a develop.
Empieza diciéndome qué historias me tocan, de quién dependo, a quién desbloqueo y qué puedo hacer hoy.
```

---

## 2. El proyecto en 10 líneas

- Librería con inventario y ventas. Venimos de **SBA** (Front + 3 servicios con una BD compartida) y pasamos a **microservicios**: **una BD por servicio**, **un API Gateway (YARP)** y **JWT**.
- **.NET 10**, ADO.NET puro (sin Entity Framework) en los relacionales, `MongoDB.Driver` en Productos.
- **Front** Razor Pages (5000) → **Gateway** YARP (5080) → **Ventas** (5101, SQL Server, Hexagonal), **Usuarios** (5102, SQL Server, Hexagonal), **Clientes** (5103, MySQL, Hexagonal), **Productos** (5104, MongoDB, Clean).
- Ventas llama a Productos (descontar y restaurar stock) y a Clientes (buscar por CI/NIT) por REST directo, propagando el JWT del usuario.
- **Sin sagas, orquestadores ni bus de eventos** en este sprint. La venta usa una compensación local en `StockFacade`.
- **Patrones obligatorios:** Adapter y Result. También se usan Factory Method, Facade y Singleton.
- El monolito (`Libreria.Application`, `Libreria.Domain`, `Libreria.Infrastructure`) y el servicio de Reportes **se eliminan** en US-109. El código anterior queda en el tag **`v3-sba`**.
- Defensa: **martes 24/11/2026**. Congelamiento: **lunes 23/11**. Se trabaja todos los días.
- Cada integrante defiende **individualmente** lo que implementa: el código debe ser entendible y explicable, no solo funcional.
- Equipo: **Grupo 1** Edson, Cesar y Josue · **Grupo 2** Mateo, Gabriel y Mauricio.

---

## 3. Tarjeta de cada integrante

> Busca tu nombre. «Desbloqueado por» = lo que debe estar en `develop` antes de conectar con lo real. «Mientras tanto» = lo que puedes hacer sin esperar.

### Edson — Grupo 1 · Base del sprint + MS Clientes (MySQL, Hexagonal)
- **Historias:** US-109 → US-110 → US-111 → US-112 → US-113 (base), US-114 → US-115 → US-116 (Clientes), US-132 (C4), US-137 (con Gabriel), US-138 (opcional).
- **Carpetas:** `Libreria.Gateway/` (salvo los `yarp.*.json`), `docker-compose.yml`, `LibreriaSystem.slnx`, `tools/`, `Libreria.Servicio.Clientes/`, `db/clientes/`, Front: `Pages/Clientes`, `Clients/Clientes`, `DTOs/Clientes`, `_MenuClientes`, `_ClienteAltaRapidaCampos`. Mientras dure la base, tus PR pueden tocar cualquier carpeta.
- **Desbloqueado por:** nadie. Tú empiezas.
- **Desbloqueas a:** **todos** con US-109 + US-110 (sábado por la mañana) y US-111 + US-112 + US-113 (domingo). A Cesar con `GET api/clientes/buscar` (US-115).
- **Ojo:** el tag `v3-sba` va **antes** de borrar nada. Los GUID de `docs/contratos/datos-semilla.md` son un contrato: una vez publicados, no se cambian.

### Cesar — Grupo 1 · MS Ventas (SQL Server, Hexagonal)
- **Historias:** US-117 → US-118 → US-119 → US-120, regresión del G1 (US-136), revisión del documento (US-134).
- **Carpetas:** `Libreria.Servicio.Ventas/`, `db/ventas/`, Front: `Pages/Ventas`, `Clients/Ventas`, `DTOs/Ventas`, `_MenuVentas`, `wwwroot/js/venta-registrar.js`.
- **Desbloqueado por:** US-109 + US-110 para empezar US-117; US-111 para la BD real. Productos `buscar` + stock (Josue, US-122, martes 17) y Clientes `buscar` (Edson, US-115) para conectar la venta real.
- **Desbloqueas a:** nadie de forma directa, pero US-117 deja limpio el servicio que Josue y Edson usan de referencia.
- **Mientras tanto:** en US-118, programa los puertos hacia Productos y Clientes con adaptadores **falsos** que respondan con los datos de `datos-semilla.md`, y cambia a los HTTP reales cuando estén en `develop`. US-120 (bugs del JS y del comprobante) no depende de nadie: hazla cuando te trabes.
- **Ojo:** el comprobante es lo primero que se recorta si el tiempo no alcanza. Nunca el registro ni la anulación.

### Josue — Grupo 1 · MS Productos (MongoDB, Clean)
- **Historias:** US-121 → US-122 → US-123.
- **Carpetas:** `Libreria.Servicio.Productos/`, `db/productos/`, Front: `Pages/Productos`, `Pages/Categorias`, `Pages/Marcas`, `Pages/Historico`, `Clients/Productos`, `DTOs/Productos`, `_MenuInventario`.
- **Desbloqueado por:** US-109 + US-110 (sábado); US-111 para el contenedor de Mongo; US-113 para el cliente HTTP del Front.
- **Desbloqueas a:** **Cesar** con `GET api/productos/buscar` y los endpoints de stock (`descontar` y `restaurar`). **Hazlos primero en US-122** y mergéalos el martes 17 aunque el resto del CRUD no esté listo.
- **Mientras tanto:** sin el contenedor, avanza el dominio, los casos de uso y sus pruebas con repositorios falsos.
- **Ojo:** Mongo en un solo contenedor **no tiene transacciones de varios documentos**. Usa operaciones atómicas sobre un documento (`$inc` con filtro `stock >= cantidad`; `$set` + `$push` para el histórico). Las fábricas abstractas `CrudRepositoryFactory<T>` y `HistoricoRepositoryFactory` se copian **sin cambios**.

### Gabriel — Grupo 2 · MS Usuarios (hexagonal) + login + JWT
- **Historias:** US-124 → US-125 → US-126, US-137 (con Edson).
- **Carpetas:** `Libreria.Servicio.Usuarios/` (Mauricio y Mateo trabajan en sus casos de uso), `db/usuarios/`, `Infrastructure/Seguridad/` de **cada** MS (parte JWT), Front: `Pages/Cuenta/Login`, `Logout`, `Seguridad/`, autenticación en `Program.cs`.
- **Desbloqueado por:** US-109 + US-110 (sábado) y US-111 para la BD de Usuarios.
- **Desbloqueas a:** **Mauricio y Mateo** con el hito de login de US-124 (domingo 15). A **todos** con US-125 (JWT real y token de desarrollo) y US-126 (validación en los 4 MS y el gateway). A **Cesar** con el claim `iniciales` del comprobante.
- **Ojo:** el par de claves RS256 se genera **una sola vez**. La pública se sube en los `appsettings`; la privada se comparte solo por el canal privado del equipo. Tus cambios en el `Program.cs` de un MS del G1 van en un PR revisado por su dueño.

### Mauricio — Grupo 2 · Bitácora + listado de usuarios + cambio de contraseña
- **Historias:** US-128 → US-127 → US-129, US-133 (patrones en el documento), regresión del G2 (US-136).
- **Carpetas:** casos de uso de listado y cambio de contraseña en Usuarios, `Infrastructure/Seguridad/` de **cada** MS (parte usuario actual), Front: `Pages/Cuenta/CambiarContrasena`, `Pages/Usuarios/Index`, `Services/Auditoria`, `_AuditoriaInfo`.
- **Desbloqueado por:** hito de login de US-124 (domingo) para US-128; US-125 para tener claims reales en US-127; los CRUD de los 3 MS del G1 para la prueba cruzada de bitácora (jueves 19).
- **Desbloqueas a:** **Mateo** con `PUT api/usuarios/contrasena` aceptando el token de primer ingreso (US-129). A **todos** con la bitácora real (US-127).
- **Mientras tanto:** escribe `UsuarioActualDesdeToken` y sus pruebas con un `ClaimsPrincipal` armado a mano; no necesitas el login real para eso.

### Mateo — Grupo 2 · Alta de usuario + primer inicio de sesión
- **Historias:** US-130 → US-131, US-134 (documento) y coordinación de US-135 (ortografía).
- **Carpetas:** casos de uso de alta en Usuarios (`CreateUserUseCase`, `UsernameGenerator`, `UsuarioFacade`), Front: `Pages/Usuarios/Create` y el middleware de primer ingreso.
- **Desbloqueado por:** hito de login de US-124 (domingo) para US-130; US-126 (403 en los MS) y US-129 (cambio de contraseña) para cerrar US-131.
- **Desbloqueas a:** nadie de forma directa; US-131 cierra el bug crítico de seguridad.
- **Mientras tanto:** la normalización de nombres, el protocolo de usuario y la contraseña temporal son lógica pura. Prográmalas con sus pruebas desde el sábado.
- **Ojo:** la contraseña temporal **nunca** aparece en la UI, en los logs ni en la respuesta de la API: solo en el correo.

---

## 4. Reglas que todo agente debe cumplir

**Git**
- Ramas `feature/US-xx-nombre-corto` creadas desde `develop` actualizado. `git pull --rebase origin develop` cada mañana y antes de cada push.
- Commits en español con la historia al inicio: `US-118: compensa el stock si falla el commit de la venta`.
- PR a `develop`, uno por historia o por hito, con: qué cambia, cómo probarlo (`.http` o pasos en el Front), capturas y la casilla de la DoD marcada. Lo aprueba alguien distinto al autor; si toca la carpeta de otro, lo aprueba su dueño.
- `main` solo en US-137. Nunca `git push --force` sobre `develop`.

**Seguridad**
- Nada de secretos en el repositorio: cadenas de conexión, credenciales SMTP y la clave privada del JWT van por variables de entorno. Solo se actualiza `.env.example`, con nombres y sin valores.
- Solo `PublicId` (GUID) hacia afuera; nunca ids numéricos.

**Arquitectura**
- Hexagonal (Clientes, Ventas, Usuarios): el núcleo (`Domain`, `Application`) no conoce SQL, HTTP ni Mongo. Puertos en `Application/Ports/Primary` y `Ports/Secondary`; adaptadores en `Infrastructure` y `Controllers`.
- Clean (Productos): dependencias siempre hacia adentro, de `Presentation` a `Infrastructure`, `Application` y `Domain`.
- Los controladores **no** llaman a repositorios ni a fábricas: llaman a un servicio de aplicación o a un caso de uso.
- Validación de negocio → `Result`. Excepción → solo lo inesperado. El controlador traduce `Result` a 400/404/409 (`ProblemDetails`).
- Ningún servicio lee la BD de otro. Lo que se necesita de otro servicio se pide por REST o se guarda como copia al registrar.
- No hay bibliotecas compartidas entre servicios: si dos necesitan lo mismo, se copia.
- `Program.cs`: cada módulo se registra en su propio método de extensión.

**Calidad**
- Nombres claros, métodos cortos, una responsabilidad por clase y dependencias por interfaz (SOLID). Si una clase supera unas 150 líneas, revísala.
- Toda regla nueva tiene al menos una prueba xUnit. `dotnet build` y `dotnet test` en verde antes del PR.
- UI sin `alert()` ni `confirm()` nativos (se usa el modal compartido `_ConfirmModal`) y sin faltas de ortografía: tildes, signos de apertura (¿ ¡) y mayúsculas.
- Al cerrar una historia, deja en la tarjeta de Trello los insumos del documento: captura del código y 2–3 líneas del patrón o principio aplicado.

---

## 5. Cómo debe trabajar el agente en cada historia

1. Leer la historia completa en el planning y los contratos de la sección 6 que use.
2. Buscar en el código lo existente que se reutiliza (y en el tag `v3-sba` si se movió).
3. Proponer un plan corto: archivos, capa, patrón y pruebas. **Esperar el visto bueno.**
4. Programar en pasos pequeños, compilando y probando en cada paso.
5. Probar de punta a punta: Front → gateway → MS → BD (o con datos de ejemplo si la dependencia aún no está en `develop`).
6. Revisar la DoD y la ortografía de todo texto visible.
7. Preparar el PR y el comentario de insumos para el documento.
8. Explicarle al integrante, en 5 líneas, qué se hizo y cómo defenderlo: patrón, capa y principio SOLID.

---

## 6. Cuándo detenerse y avisar al equipo

- Una tarea exige editar una carpeta, contrato, `.slnx`, `docker-compose.yml` o archivo de rutas que no es tuyo.
- Un contrato de la sección 6 no alcanza o tiene un error.
- Una dependencia no llegó a `develop` en la fecha del cronograma y ya no hay trabajo posible con datos de ejemplo.
- Aparece un bug en el código de otro integrante: se abre una tarjeta de bug asignada a su dueño, no se corrige por cuenta propia.
- Algo pondría un secreto en el repositorio.
