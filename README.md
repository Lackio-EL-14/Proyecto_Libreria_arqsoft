# Sistema de gestión de librería

Entregable de microservicios con un Front Razor Pages, un API Gateway YARP y una base de datos independiente por servicio.

## Alcance vigente

| Servicio | Proyecto | Puerto | Persistencia | Arquitectura |
|---|---|---:|---|---|
| Proveedores | `src/Libreria.Servicio.Proveedores` | 5101 | SQL Server · `LibreriaProveedores` | Hexagonal |
| Usuarios | `src/Libreria.Servicio.Usuarios` | 5102 | SQL Server · `LibreriaUsuarios` | Hexagonal |
| Clientes | `src/Libreria.Servicio.Clientes` | 5103 | MySQL · `libreria_clientes` | Hexagonal |
| Productos | `src/Libreria.Servicio.Productos` | 5104 | MongoDB · `libreria_productos` | Clean Architecture |
| Gateway | `src/Libreria.Gateway` | 5080 | — | YARP |
| Front | `src/Libreria.Client` | 5000 | — | Razor Pages |
| Keycloak | contenedor `keycloak` | 8080 | Volumen `keycloak_data` · realm `libreria` | Proveedor de identidad (OIDC, JWT RS256) |

El proceso principal no forma parte de este entregable. Su código previo quedó conservado en la rama remota `respaldo/ventas-entrega-final`, para retomarlo únicamente en la entrega final. En esta rama no hay orquestación distribuida ni compensaciones entre microservicios.

## Configuración inicial

1. Copiar `.env.example` como `.env` y reemplazar todos los valores `<contraseña>`.
2. Usar la misma contraseña de `PROVEEDORES_DB_SA_PASSWORD` en la cadena `ConnectionStrings__ProveedoresDb`, y lo mismo para cada servicio.
3. No subir `.env` al repositorio.

```powershell
Copy-Item .env.example .env
notepad .env
docker compose up -d
docker compose ps
```

MySQL (`db/clientes`) y MongoDB (`db/productos`) ejecutan sus scripts cuando el volumen se crea por primera vez. SQL Server no lo hace por sí solo: las tareas de una sola vez `proveedores-db-init` y `usuarios-db-init` corren los scripts idempotentes de `db/proveedores` y `db/usuarios` en cuanto cada base está sana. En `docker compose ps -a` deben quedar «Exited (0)». Para volver a aplicarlos sobre una base existente:

```powershell
docker compose run --rm proveedores-db-init
docker compose run --rm usuarios-db-init
```

## Keycloak (proveedor de identidad)

Keycloak corre en `http://localhost:8080`, realm `libreria`. Contrato completo: [`docs/contratos/keycloak.md`](docs/contratos/keycloak.md).

**Desde cero** (borra los datos de Keycloak y de las cuatro bases):

```powershell
docker compose down -v
docker compose up -d                 # BD + *-db-init + keycloak + keycloak-configurar
docker compose ps -a                 # BD y keycloak «healthy»; *-db-init y keycloak-configurar «Exited (0)»
docker compose logs keycloak-configurar
```

1. `keycloak` arranca en modo desarrollo e importa `keycloak/realm-libreria.json` (roles, los 4 clientes, audiencia `libreria-api`, claim `roles`, política de contraseñas, español, token de 60 minutos). La importación ocurre **solo si el realm no existe**; si cambia el JSON, hay que borrar el volumen (`docker compose down -v`).
2. `keycloak-configurar` corre una vez `keycloak/configurar.sh`: asigna los secretos de los clientes y crea `admin` y `vendedor` con los GUID de `docs/contratos/datos-semilla.md`, con sus contraseñas de `.env`. Es idempotente. Para repetirlo, por ejemplo después de cambiar una contraseña en `.env`:

```powershell
docker compose run --rm keycloak-configurar
```

Variables necesarias en `.env`: `KC_BOOTSTRAP_ADMIN_*`, `KEYCLOAK_*_CLIENT_SECRET`, `KEYCLOAK_*_SEMILLA_PASSWORD` y `KEYCLOAK_HABILITAR_CLIENTE_PRUEBAS` (ver `.env.example`). La consola de administración (`http://localhost:8080/admin`) usa `KC_BOOTSTRAP_ADMIN_*`.

Secretos para los procesos .NET (los mismos valores de `.env`):

```powershell
dotnet user-secrets set "Keycloak:ClientSecret" "<KEYCLOAK_USUARIOS_ADMIN_CLIENT_SECRET>" --project src/Libreria.Servicio.Usuarios
dotnet user-secrets set "Keycloak:VerificacionClientSecret" "<KEYCLOAK_USUARIOS_VERIFICACION_CLIENT_SECRET>" --project src/Libreria.Servicio.Usuarios
```

Prueba de integración del adaptador de Keycloak (crea y borra un usuario temporal):

```powershell
$env:KEYCLOAK_PRUEBAS_INTEGRACION = "1"   # y $env:Keycloak__ClientSecret / $env:Keycloak__VerificacionClientSecret
dotnet test tests/Libreria.Servicio.Usuarios.Tests --filter "FullyQualifiedName~KeycloakIntegracion"
```

El Front inicia sesión con OpenID Connect contra Keycloak (US-125). Necesita el secreto de `libreria-front`:

```powershell
dotnet user-secrets set "Keycloak:ClientSecret" "<KEYCLOAK_FRONT_CLIENT_SECRET>" --project src/Libreria.Client
```

Usuarios de desarrollo: `admin` y `vendedor`, con las contraseñas de `KEYCLOAK_*_SEMILLA_PASSWORD` de su `.env`.

**Todas las API exigen token (US-126):** el gateway y los 4 MS validan el JWT de Keycloak (firma RS256, emisor, audiencia `libreria-api`, vencimiento) y aplican los roles. Sin token responden 401, y con un rol insuficiente, 403. Para probar en Swagger (`http://localhost:510x/swagger`), use el botón «Authorize» con un token de `libreria-pruebas` (`keycloak.md` §5). Evidencias: `docs/evidencias/US-126-seguridad-jwt.md`.

## Variables para ejecutar .NET localmente

`./run-all.ps1` lee el mismo `.env` y entrega a cada proceso solo lo suyo, sin imprimirlo:

| Proceso | Variables |
|---|---|
| Proveedores · Usuarios · Clientes · Productos | `ConnectionStrings__<Servicio>Db`: se toma del `.env` si existe; si no, se arma con `*_DB_SA_PASSWORD` / `CLIENTES_DB_*` y los puertos de `docker-compose.yml` |
| Usuarios | `Keycloak__ClientSecret` = `KEYCLOAK_USUARIOS_ADMIN_CLIENT_SECRET`, `Keycloak__VerificacionClientSecret` = `KEYCLOAK_USUARIOS_VERIFICACION_CLIENT_SECRET`, y `BASE_URL`/`SMTP_*` si están definidos |
| Front | `Keycloak__ClientSecret` = `KEYCLOAK_FRONT_CLIENT_SECRET` |

Si se usa `dotnet run` a mano, esas mismas claves van en el entorno o en User Secrets. Ejemplo para Proveedores:

```powershell
dotnet user-secrets set "ConnectionStrings:ProveedoresDb" "Server=localhost,1433;Database=LibreriaProveedores;User Id=sa;Password=TU_CLAVE;TrustServerCertificate=True;" --project src/Libreria.Servicio.Proveedores
```

## Levantar la solución

Con las cuatro bases sanas y el `.env` completo:

```powershell
./run-all.ps1
```

El script compila y levanta Proveedores, Usuarios, Clientes, Productos, Gateway y Front. Para iniciar cada proceso manualmente, use una consola por comando:

```powershell
dotnet run --project src/Libreria.Servicio.Proveedores
dotnet run --project src/Libreria.Servicio.Usuarios
dotnet run --project src/Libreria.Servicio.Clientes
dotnet run --project src/Libreria.Servicio.Productos
dotnet run --project src/Libreria.Gateway
dotnet run --project src/Libreria.Client
```

## Pruebas

```powershell
dotnet build LibreriaSystem.slnx
dotnet test LibreriaSystem.slnx --no-build
```

## Diseño de Proveedores

- `Proveedor` es la raíz del agregado y ofrece el CRUD requerido.
- `ContactoProveedor` es una tabla dependiente en tercera forma normal; evita repetir datos de contacto en `Proveedor`.
- `IProveedorService` es el puerto primario y `IProveedorRepository` el puerto secundario.
- `ProveedorService` coordina las reglas y actúa como fachada de aplicación.
- `CrudRepositoryFactory<T>` y `ProveedorRepositoryFactory` son creadores abstractos; `SqlProveedorRepositoryFactory` es el creador concreto del Factory Method.
- `ProveedorRepository` ejecuta las escrituras de proveedor y contacto dentro de una transacción local.
- Las validaciones esperadas usan `Result`; las excepciones se reservan para fallas inesperadas.

Contratos detallados: [`docs/contratos/README.md`](docs/contratos/README.md).
