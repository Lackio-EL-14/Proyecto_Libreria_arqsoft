# Sistema de inventario y ventas — Librería

Solución académica en C# y .NET 10: un Front en Razor Pages, un API Gateway y cuatro microservicios, cada uno con su propia base de datos (en construcción durante el sprint de microservicios; ver `docs/Planning_Sprint_Microservicios.md`).

## Arquitectura actual

| Proceso | Proyecto | Puerto | Estilo |
| --- | --- | --- | --- |
| Front | `src/Libreria.Client` | 5000 | Razor Pages |
| API Gateway | `src/Libreria.Gateway` | 5080 | YARP (ver «API Gateway») |
| MS Ventas | `src/Libreria.Servicio.Ventas` | 5101 | Hexagonal |
| MS Usuarios | `src/Libreria.Servicio.Usuarios` | 5102 | Hexagonal (se migra en US-124) |
| MS Clientes | `src/Libreria.Servicio.Clientes` | 5103 | Hexagonal |
| MS Productos | `src/Libreria.Servicio.Productos` | 5104 | Clean |

| Contenedor | Motor | Puerto en el host | BD | Scripts |
| --- | --- | --- | --- | --- |
| `ventas-db` | SQL Server 2022 | 1433 | `LibreriaVentas` | `db/ventas/` |
| `usuarios-db` | SQL Server 2022 | 1434 | `LibreriaUsuarios` | `db/usuarios/` |
| `clientes-db` | MySQL 8.4 | 3306 | `libreria_clientes` | `db/clientes/` |
| `productos-db` | MongoDB 8 | 27017 | `libreria_productos` | `db/productos/` |

Cada microservicio y el gateway responden `GET /salud` con `{ "servicio": "...", "estado": "ok" }`. Docker Compose levanta una base de datos por microservicio (US-111); ningún servicio lee la BD de otro y las referencias entre servicios son por `PublicId`, con los GUID fijos de `docs/contratos/datos-semilla.md`. El Front y los servicios se ejecutan como proyectos .NET independientes. En este sprint no se usan Nginx, Redis ni réplicas de la aplicación.

El Front no debe agregar referencias de proyecto a los servicios. La comunicación se realiza por HTTP, con DTOs propios del Front y clientes registrados mediante `IHttpClientFactory`.

## Requisitos

- .NET 10 SDK.
- Docker Desktop abierto.
- Git.
- Visual Studio con la carga de trabajo ASP.NET y desarrollo web.

Comprueba la instalación:

```powershell
dotnet --version
docker --version
docker compose version
```

## Preparar el repositorio

```powershell
git switch develop
git pull origin develop
dotnet restore LibreriaSystem.slnx
```

## Levantar las bases

Desde la raíz del repositorio, la primera vez:

```bash
cp .env.example .env      # y completar las contraseñas (no se sube a Git)
docker compose up -d --remove-orphans
docker compose ps
```

`--remove-orphans` retira el contenedor antiguo `libreria-db`. Su volumen `sql_data` queda en el disco; si ya no lo necesitas: `docker volume rm <proyecto>_sql_data` (ver `docker volume ls`).

Espera a que los 4 contenedores aparezcan como `healthy`.

- **MySQL (`clientes-db`) y MongoDB (`productos-db`)** se inicializan solos la primera vez con los scripts de `db/clientes/` y `db/productos/`.
- **Los dos SQL Server** necesitan que ejecutes sus scripts en orden. La contraseña se toma de la variable del propio contenedor; no hace falta escribirla:

```bash
docker exec ventas-db bash -lc 'for f in /scripts/*.sql; do echo "Ejecutando $f..."; /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -i "$f" || exit 1; done'
docker exec usuarios-db bash -lc 'for f in /scripts/*.sql; do echo "Ejecutando $f..."; /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -i "$f" || exit 1; done'
```

Todos los scripts son idempotentes: se pueden volver a ejecutar sin duplicar datos. Para repetir a mano los de MySQL o Mongo:

```bash
docker exec clientes-db bash -lc 'for f in /docker-entrypoint-initdb.d/*.sql; do mysql -uroot -p"$MYSQL_ROOT_PASSWORD" < "$f" || exit 1; done'
docker exec productos-db bash -lc 'for f in /docker-entrypoint-initdb.d/*.js; do mongosh --quiet "$f" || exit 1; done'
```

Comprobación rápida (2 usuarios, 3 clientes, 5 productos y 3 ventas):

```bash
docker exec usuarios-db bash -lc '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d LibreriaUsuarios -Q "SELECT NombreUsuario, Estado FROM dbo.Usuario"'
docker exec clientes-db bash -lc 'mysql -uroot -p"$MYSQL_ROOT_PASSWORD" libreria_clientes -e "SELECT CiNit, RazonSocial, Estado FROM Cliente"'
docker exec productos-db mongosh libreria_productos --quiet --eval "db.productos.countDocuments()"
docker exec ventas-db bash -lc '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d LibreriaVentas -Q "SELECT PublicId, ClienteRazonSocial, Total, Estado FROM dbo.Venta"'
```

Para empezar desde cero (**borra todos los datos**): `docker compose down -v` y repetir los pasos. Para una actualización normal no uses `-v`.

> Transición: hasta US-117 (Ventas) y US-124 (Usuarios), esos dos servicios todavía leen la cadena `LibreriaDb` del esquema anterior. Cada historia los cambia a `ConnectionStrings__VentasDb` y `ConnectionStrings__UsuariosDb`.

## Configuración obligatoria de Usuarios

El Servicio de Usuarios no guarda contraseñas ni cadenas de conexión en Git. Cada integrante debe ejecutar una vez:

```powershell
dotnet user-secrets set "ConnectionStrings:LibreriaDb" "Server=localhost,1433;Database=LibreriaDb;User Id=sa;Password=<contraseña>;TrustServerCertificate=True;" --project src/Libreria.Servicio.Usuarios
```

Comprueba que quedó configurado:

```powershell
dotnet user-secrets list --project src/Libreria.Servicio.Usuarios
```

Visual Studio y `dotnet run` leen automáticamente ese secreto cuando el ambiente es `Development`. Un archivo `.env` de la raíz solo es leído automáticamente por Docker Compose; las aplicaciones .NET no lo cargan por sí solas.

### Correo de activación en desarrollo

En Development no hace falta una cuenta de Gmail. S2 usa `PickupDirectory` y guarda cada correo como archivo `.eml` dentro de:

```text
src/Libreria.Servicio.Usuarios/bin/Development/mail
```

Abre el archivo generado y usa el enlace de activación. Esto permite demostrar el flujo sin internet ni contraseña SMTP.

Si el usuario continúa en estado `Pendiente`, un administrador puede usar **Reenviar activación** desde la pantalla de Usuarios. El sistema genera un token nuevo, invalida el anterior y permite otro reenvío después de un minuto.

El cambio de contraseña no utiliza correo: exige una sesión iniciada, la contraseña actual y la nueva contraseña escrita dos veces.

Para enviar correos reales con Gmail, configura secretos y usa una contraseña de aplicación de Google, no la contraseña normal de la cuenta:

```powershell
dotnet user-secrets set "EMAIL_DELIVERY_MODE" "Smtp" --project src/Libreria.Servicio.Usuarios
dotnet user-secrets set "SMTP_HOST" "smtp.gmail.com" --project src/Libreria.Servicio.Usuarios
dotnet user-secrets set "SMTP_PORT" "587" --project src/Libreria.Servicio.Usuarios
dotnet user-secrets set "SMTP_USER" "correo@gmail.com" --project src/Libreria.Servicio.Usuarios
dotnet user-secrets set "SMTP_PASS" "contraseña-de-aplicación" --project src/Libreria.Servicio.Usuarios
dotnet user-secrets set "SMTP_FROM" "correo@gmail.com" --project src/Libreria.Servicio.Usuarios
dotnet user-secrets set "BASE_URL" "http://localhost:5000" --project src/Libreria.Servicio.Usuarios
```

## API Gateway (YARP) — US-112

`Libreria.Gateway` (puerto 5080) es la única puerta de entrada a los microservicios. No tiene lógica de negocio: recibe la solicitud, elige el microservicio por la ruta y la reenvía tal cual (método, ruta, query, cuerpo y `Authorization: Bearer`). Las rutas no están en el código sino en dos archivos, uno por grupo, que el gateway fusiona al arrancar:

| Archivo (dueño) | Ruta | Cluster → destino por defecto |
| --- | --- | --- |
| `yarp.g1.json` (Grupo 1) | `/api/clientes/{**resto}` | `clientes` → `http://localhost:5103` |
| | `/api/ventas/{**resto}` | `ventas` → `http://localhost:5101` |
| | `/api/productos/…`, `/api/categorias/…`, `/api/marcas/…`, `/api/historico/…` | `productos` → `http://localhost:5104` |
| `yarp.g2.json` (Grupo 2) | `/api/auth/{**resto}` | `usuarios` → `http://localhost:5102` |
| | `/api/usuarios/{**resto}` | `usuarios` → `http://localhost:5102` |

- Cualquier otra ruta responde **404**. `GET /salud` es del propio gateway.
- Los IDs de ruta llevan el prefijo del grupo (`g1-…`, `g2-…`) para que los dos archivos no choquen. Agregar una ruta es editar el JSON del grupo; no hay que tocar `Program.cs`.
- **Microservicio caído:** si el destino no responde (conexión rechazada o más de 30 s sin actividad), el gateway devuelve **503** `application/problem+json` con `title` «Servicio no disponible» y `servicio` = nombre del cluster. El Front convierte 502/503/504 en `ServicioNoDisponibleException` y muestra la página **Servicio no disponible**.
- La validación del JWT en el gateway llega en US-126; por ahora solo enruta.

### Cambiar un destino sin tocar el repositorio

Las variables de entorno (y los argumentos de línea de comandos) ganan sobre los JSON:

```bash
ReverseProxy__Clusters__<cluster>__Destinations__principal__Address=http://host:puerto
# ejemplo con dotnet run:
ReverseProxy__Clusters__productos__Destinations__principal__Address=http://localhost:5101 dotnet run --project src/Libreria.Gateway
```

**Transición (hasta US-115 y US-122):** mientras Clientes y Productos son esqueletos, Ventas (5101) sigue atendiendo `api/clientes`, `api/productos`, `api/categorias`, `api/marcas` y `api/historico`. Para usar esas pantallas a través del gateway, apunta los dos clusters a Ventas:

```bash
export ReverseProxy__Clusters__clientes__Destinations__principal__Address=http://localhost:5101
export ReverseProxy__Clusters__productos__Destinations__principal__Address=http://localhost:5101
dotnet run --project src/Libreria.Gateway
```

En PowerShell: `$env:ReverseProxy__Clusters__productos__Destinations__principal__Address = "http://localhost:5101"`. Cuando cada MS esté en `develop`, basta con no definir la variable.

Las peticiones de prueba de cada ruta están en `src/Libreria.Gateway/Libreria.Gateway.http`, y las pruebas automáticas en `tests/Libreria.Gateway.Tests`.

## Correr todo

Los 6 proyectos (Front, gateway y 4 microservicios) se levantan a la vez de cualquiera de estas dos formas.

### Desde Visual Studio

1. Abre `LibreriaSystem.slnx`.
2. En el selector de inicio elige el perfil **Correr todo** (definido en `LibreriaSystem.slnLaunch`).
3. Inicia con o sin depuración y abre `http://localhost:5000`.

### Con `run-all.ps1`

Desde la raíz del repositorio, en PowerShell 7 (`pwsh`) en Windows, Linux o macOS:

```powershell
./run-all.ps1            # compila una vez y arranca los 6
./run-all.ps1 -SinCompilar
```

El script compila la solución, arranca cada proyecto con `dotnet run --no-build`, comprueba `GET /salud` de cada servicio y `GET /` del Front, y muestra una tabla con el estado de cada uno. En Windows cada proyecto abre su propia ventana; en Linux y macOS los logs quedan en la carpeta temporal `libreria-run-all`. **Ctrl+C** detiene los 6 procesos.

### Un proyecto suelto

```powershell
dotnet run --project src/<proyecto>
```

Los puertos están fijados en el `Properties/launchSettings.json` de cada proyecto (tabla de arriba). Los `.http` de cada proyecto traen la petición de `/salud` para probarlo.

## Base compartida del Front — US-80

La estructura preparada para cada módulo es:

```text
src/Libreria.Client/
├── Clients/
│   ├── Ventas/
│   ├── Usuarios/
│   ├── Common/
│   └── DependencyInjection/
├── DTOs/
│   ├── Ventas/
│   └── Usuarios/
└── Pages/
    ├── Ventas/
    └── Usuarios/
```

Los endpoints base viven en `src/Libreria.Client/appsettings.json`. `Program.cs` registra cada módulo mediante:

```csharp
AddServicioVentas(configuration)
AddServicioUsuarios(configuration)
```

Cada solicitud autenticada realizada por esos clientes envía:

- `X-Usuario-Id`
- `X-Usuario-Nombre`

El valor procede exclusivamente de los claims reales de la cookie. No existe usuario fijo de desarrollo. El login del Front llama a `POST api/auth/login` de S2 y la vigencia de la cookie se revisa mediante `GET api/usuarios/{id}/sesion-activa`, con una caché local de 20 segundos.

`AuditoriaService` vive en el Front y convierte los identificadores de auditoría en nombres mediante `GET api/usuarios/nombres`. Ventas no debe consultar directamente la tabla `Usuario`.

Si un servicio no responde (error de red, o 502/503/504 devuelto por el gateway), el Front redirige a una pantalla uniforme de servicio no disponible. Las demás respuestas HTTP con error se convierten en `ApiClientException`, conservando el código de estado y el mensaje de `ProblemDetails`.

## Reglas para continuar el sprint

- Cada módulo coloca sus DTOs y clientes en su carpeta; no modifica las carpetas de otro grupo.
- No se comparten entidades de dominio entre Front y servicios.
- Los servicios exponen JSON mediante rutas `api/<recurso>`.
- Se usan `Guid PublicId` en los contratos HTTP; no se exponen identificadores internos.
- Los códigos esperados son 200, 201, 400, 404 y 409.
- Los errores de API usan `ProblemDetails`.
- Las fechas se envían en ISO 8601 y los decimales como números JSON.
- Las URLs y credenciales se obtienen de configuración; no se escriben secretos en el repositorio.

Las páginas antiguas de Ventas, Productos, Categorías, Marcas e Histórico permanecen versionadas como referencia, pero están temporalmente excluidas de la compilación. US-83 debe migrarlas a los clientes HTTP y retirar esas exclusiones del archivo `Libreria.Client.csproj`. El login ya consume S2; las demás páginas de Usuarios se migran en US-95 a US-98.

## Validación

```powershell
dotnet build LibreriaSystem.slnx
dotnet test tests/Libreria.Servicio.Usuarios.Tests/Libreria.Servicio.Usuarios.Tests.csproj
dotnet test tests/Libreria.Servicio.Ventas.Tests/Libreria.Servicio.Ventas.Tests.csproj
```

Usuarios de datos semilla:

| Usuario | Contraseña | Rol |
| --- | --- | --- |
| `admin` | `Admin2026!` | Administrador |
| `vendedor` | `Vendedor2026!` | Vendedor |
