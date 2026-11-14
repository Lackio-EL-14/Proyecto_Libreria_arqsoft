# Proyecto Librería — Microservicios (.NET 10)

> Sprint vigente: migración a microservicios, US-109 a US-138, defensa el 24/11/2026.
> Fuente de verdad: `docs/planning_microservicios.md` (historias, dueños, contratos y cronograma).
> Durante US-109 a US-113 parte de esta estructura todavía se está construyendo: si algo no existe aún, revisa qué historia lo crea.

## Estructura objetivo
- Solución: `LibreriaSystem.slnx`.
- Front: `src/Libreria.Client` (Razor Pages, :5000). Solo conoce la URL del gateway.
- Gateway: `src/Libreria.Gateway` (YARP, :5080). Rutas en `yarp.g1.json` (Grupo 1) y `yarp.g2.json` (Grupo 2).
- Microservicios, cada uno con su propia BD:
  - `Libreria.Servicio.Ventas` (:5101, Hexagonal, SQL Server `LibreriaVentas`, contenedor `ventas-db`).
  - `Libreria.Servicio.Usuarios` (:5102, Hexagonal, SQL Server `LibreriaUsuarios`, contenedor `usuarios-db`).
  - `Libreria.Servicio.Clientes` (:5103, Hexagonal, MySQL `libreria_clientes`, contenedor `clientes-db`).
  - `Libreria.Servicio.Productos` (:5104, Clean, MongoDB `libreria_productos`, contenedor `productos-db`).
- Pruebas: `tests/Libreria.Servicio.<Nombre>.Tests` (xUnit).
- Scripts de BD idempotentes: `db/clientes/`, `db/ventas/`, `db/productos/`, `db/usuarios/`. Datos semilla con GUID fijos en `docs/contratos/datos-semilla.md`.
- **Eliminados en US-109:** `Libreria.Domain`, `Libreria.Application`, `Libreria.Infrastructure`, `Libreria.Application.Tests`, `Libreria.Servicio.Reportes` y `db/init/`. Su código está en el tag `v3-sba`; para recuperar algo: `git show v3-sba:<ruta>`. Nunca volver a referenciarlos.

## Comandos
- Compilar: `dotnet build LibreriaSystem.slnx 2>&1 | grep -E "error |Compilación"`
- Probar: `dotnet test LibreriaSystem.slnx`
- Bases: `docker compose up -d` y luego los scripts de los SQL Server (ver `README`). MySQL y Mongo se inicializan solos.
- Consultar:
  - SQL Server: `docker exec ventas-db bash -lc '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d LibreriaVentas -Q "..."'` (igual con `usuarios-db` y `LibreriaUsuarios`).
  - MySQL: `docker exec clientes-db bash -lc 'mysql -uroot -p"$MYSQL_ROOT_PASSWORD" libreria_clientes -e "..."'`
  - Mongo: `docker exec productos-db mongosh libreria_productos --quiet --eval "..."`
- Correr todo: `./run-all.ps1`, o `dotnet run --project src/<proyecto>` en procesos separados.

## Reglas de arquitectura
- Una BD por servicio. Ningún servicio lee la BD de otro; las referencias entre servicios son por `PublicId` (GUID), sin FK.
- Hexagonal (Ventas, Usuarios, Clientes): `Domain` y `Application` no referencian SQL, Mongo ni HTTP. Puertos en `Application/Ports/Primary` y `Application/Ports/Secondary`; adaptadores en `Infrastructure/` y `Controllers/`.
- Clean (Productos): `Domain` ← `Application` ← `Infrastructure` / `Presentation`; dependencias siempre hacia adentro.
- Los controladores llaman a servicios de aplicación o casos de uso, **nunca** a repositorios ni fábricas.
- Validación de negocio con `Result` (`Comun/Result.cs` en cada MS); excepciones solo para lo inesperado. Traducción: Validación → 400, NoEncontrado → 404, Conflicto → 409, dependencia caída → 503 (`ProblemDetails`).
- Patrones: Adapter y Result (obligatorios), Factory Method (`CrudRepositoryFactory<T>`, `HistoricoRepositoryFactory`, `UsuarioRepositoryFactory`), Facade (`VentaFacade`, `StockFacade`, `UsuarioFacade`) y Singleton (`ConnectionStringSingleton`; `MongoClient` en Productos).
- Sin sagas, orquestadores ni bus de eventos. La venta compensa el stock en `StockFacade`.
- MongoDB sin transacciones de varios documentos: usar operaciones atómicas sobre un solo documento.
- JWT RS256: solo Usuarios firma; el gateway y los 4 MS validan con la clave pública. Claims: `sub` (PublicId), `name`, `role`, `iniciales`, `debe_cambiar_contrasena`. Detalle en `docs/contratos/`.
- La auditoría guarda el `sub` del JWT en `UsuarioCreacionId`, `UsuarioModificacionId` y `UsuarioAnulacionId`. Ya **no** se usan los encabezados `X-Usuario-*` (se retiran en US-125/US-127).
- `Infrastructure/Seguridad/` de cada MS pertenece al Grupo 2 (validación del JWT y usuario actual).
- El Front no referencia proyectos de servicios: solo la URL del gateway y DTOs propios. Un cliente HTTP tipado por servicio.
- Sin bibliotecas compartidas entre servicios; lo común se copia.
- ADO.NET parametrizado en los relacionales (`Microsoft.Data.SqlClient`, `MySqlConnector`); `MongoDB.Driver` en Productos. Nada de Entity Framework.
- Hacia afuera solo `PublicId`, nunca ids numéricos.

## Reglas de trabajo
- Una historia a la vez: leer su tarjeta en el planning, proponer un plan corto y esperar aprobación antes de editar.
- Ramas `feature/US-xx-nombre` desde `develop` actualizado; PR a `develop`; `main` solo en US-137. Nunca `push --force` sobre `develop`.
- Commits en español con la historia al inicio: `US-xx: <qué cambia>`.
- Antes de editar, confirmar que el árbol de git está limpio o crear la rama.
- Solo tocar las carpetas del dueño de la historia (tabla de la sección 7 del planning). Si hace falta tocar otra, detenerse y avisar.
- No cambiar contratos (`docs/contratos/`, rutas del gateway, GUID semilla) sin avisar al equipo.
- Nombres existentes: buscarlos en el código antes de usarlos. Los marcados «(propuesto)» en el planning pueden ajustarse avisando.
- Textos de UI en español con tildes, ñ y signos de apertura correctos (cada error de ortografía resta 1 punto). Sin `alert()`/`confirm()` nativos.
- Nunca subir ni imprimir `.env`, contraseñas, cadenas de conexión reales ni la clave privada del JWT. Solo se actualiza `.env.example`.
- `dotnet build` y `dotnet test` en verde antes de cada commit de cierre.
- Al terminar una historia: listar qué falta contra sus criterios de aceptación y la Definition of Done, y redactar el comentario de insumos para el documento (captura sugerida + 2–3 líneas del patrón o principio aplicado).

## Documentos de referencia (en `docs/`)
- `docs/planning_microservicios.md`: historias, responsables, contratos y cronograma vigentes.
- `docs/contexto_agentes.md`: tarjeta de cada integrante y reglas para los agentes.
- `docs/contratos/`: REST por servicio, JWT, `Result`, usuario actual, rutas del gateway y datos semilla.
- `docs/rubricas/`: enunciados y rúbricas del docente.
- `docs/diagramas/`: diagramas C4, de clases y de BD.
