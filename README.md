# Sistema de inventario y ventas — Librería

Proyecto académico: Razor Pages + C# + ADO.NET sobre .NET 10, con SQL Server
en Docker. Aplica Clean Code y principios SOLID.

## Estructura del repo

```
Proyecto_Libreria_arqsoft/
├── docker-compose.yml       # Contenedor de SQL Server
├── db/
│   └── schema.sql            # Las 4 tablas + datos semilla opcionales
└── src/
    └── Libreria.Web/         # Proyecto Razor Pages
        ├── Data/              # Capa ADO.NET (IDbConnectionFactory)
        ├── Pages/
        │   ├── Categorias/    # US-01 a US-04 — YA tiene un ejemplo funcional
        │   ├── Marcas/        # US-05 a US-08
        │   ├── Productos/     # US-09 a US-13
        │   └── Historico/     # US-14
        └── wwwroot/css/       # Estilos (paleta sobria, sin plantilla default)
```

## 1. Requisitos previos (instalar una sola vez)

- [.NET SDK 10](https://dotnet.microsoft.com/download) — verificar con `dotnet --version`
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — verificar con `docker --version`
- Git
- Editor: Visual Studio, VS Code (con la extensión C# Dev Kit) o Rider — cualquiera sirve

## 2. Clonar el repositorio y entrar a `develop`

```bash
git clone https://github.com/Lackio-EL-14/Proyecto_Libreria_arqsoft.git
cd Proyecto_Libreria_arqsoft
git switch develop
git pull origin develop
```

Todos los comandos de Docker deben ejecutarse desde esta carpeta raíz, donde
se encuentran `README.md`, `docker-compose.yml`, `db/` y `src/`.

## 3. Levantar la base de datos con Docker

Abre **Docker Desktop manualmente** y espera hasta que indique que el motor
está en ejecución. La primera vez, descarga la imagen de SQL Server:

```bash
docker compose pull db
```

```bash
docker compose up -d
```

Espera unos 15-20 segundos y confirma que `libreria-db` aparezca como
`healthy`:

```bash
docker compose ps
```

Si el contenedor existe pero no refleja los cambios, recréalo conservando el
volumen de datos:

```bash
docker compose up -d --force-recreate
```

## 4. Aplicar el esquema (las 4 tablas)

```bash
docker exec libreria-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Libreria2026!" -C -i /scripts/schema.sql
```
> Si no les crea las columnas intenten con comillas simples 'Libreria2026!' en esta parte

> Si da error de que no existe `/opt/mssql-tools18`, corre
> `docker exec libreria-db ls /opt` para ver el nombre exacto de la carpeta
> en tu versión de imagen (puede ser `mssql-tools` sin el 18) y ajusta la ruta.

Ejecuta este paso al preparar el proyecto y después de traer cambios que
modifiquen `db/schema.sql`. El script es seguro de re-ejecutar: actualiza el
esquema sin duplicar tablas ni datos semilla.

## 5. Ejecutar el proyecto con Visual Studio

1. En Visual Studio selecciona **Abrir un proyecto o una solución**.
2. Abre `src/Libreria.Web/Libreria.Web.csproj`.
3. Espera a que Visual Studio restaure las dependencias NuGet.
4. Selecciona `Libreria.Web` como proyecto de inicio y ejecútalo con el botón
   verde o con `F5`.

También puede ejecutarse desde terminal:

```bash
cd src/Libreria.Web
dotnet restore
dotnet run
```

Abre la URL que muestra la consola (algo como `https://localhost:5001` o
`http://localhost:5000`).

Si todo salió bien, deberías ver el Home con el menú y, al entrar a
**Categorías**, la lista con las 3 categorías semilla del script SQL — esa
página ya está conectada de verdad a la base de datos vía ADO.NET.

#### Para Ingresar al contenerdor de la DB

```bash
docker exec -it libreria-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Libreria2026!' -C
```
Una vez dentro hagan las consultas que necesiten

## 6. Cómo trabajar en equipo sin bloquearse

- **No trabajes directo sobre `develop`.** Cada persona actualiza `develop` y
  crea su rama:
  ```bash
  git switch develop
  git pull origin develop
  git checkout -b feature/categorias-crud
  ```
- Haz commits pequeños y frecuentes, y sube tu rama:
  ```bash
  git push origin feature/categorias-crud
  ```
- Abre un Pull Request a `develop` cuando tu historia de usuario esté lista.
  Así evitamos que alguien sobrescriba el trabajo de otro.
- **Antes de empezar a programar cada día**, trae los últimos cambios:
  ```bash
  git checkout develop
  git pull
  git checkout tu-rama
  git merge develop
  ```

### Quién trabaja en qué carpeta (evita conflictos de archivos)

| Persona | Carpeta | Historias |
|---|---|---|
| 1 | `Pages/Categorias/` | US-01 a US-04 |
| 2 | `Pages/Marcas/` | US-05 a US-08 |
| 3 | `Pages/Productos/` (Alta + Consulta) | US-09, US-10, US-13 |
| 4 | `Pages/Productos/` (Edición + Baja) | US-11, US-12 |
| 5 | `Pages/Historico/` | US-14 |
| 6 | `Pages/Shared/_Layout.cshtml`, `wwwroot/css` | US-15 |

> Persona 3 y 4 comparten la carpeta `Productos/` — conviene que se
> pongan de acuerdo primero en el nombre y los parámetros del método que
> registra el histórico (en `Data/`), antes de escribir código, para no
> pisarse cuando junten sus ramas.

## 7. Agregar al docente como colaborador

En GitHub/GitLab: **Settings → Collaborators (o Members) → Add people**, y
agregar el usuario que indique el docente. Recuerda que esto es obligatorio
según la consigna del trabajo.

## 8. Notas de diseño

- El acceso a datos usa `IDbConnectionFactory` (interfaz) en vez de crear
  `SqlConnection` directamente en cada página — es un ejemplo concreto de
  **Inversión de Dependencias (SOLID)** que se puede mencionar en el informe.
- Las páginas Razor dependen de contratos específicos y delegan el acceso
  ADO.NET a repositorios. Las validaciones de negocio viven en clases
  separadas de las entidades.
- La contraseña de SQL Server (`Libreria2026!`) está en texto plano en
  `docker-compose.yml` y `appsettings.json` porque el proyecto corre solo en
  local para fines académicos. No es una práctica recomendada para producción.
