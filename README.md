# Sistema de inventario y ventas — Librería

Proyecto académico: Razor Pages + C# + ADO.NET sobre .NET 10, con SQL Server
en Docker. Aplica Clean Code y principios SOLID.

## Estructura del repo

```
LibreriaSystem/
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

## 2. Clonar el repositorio

```bash
git clone <URL-del-repo>
cd LibreriaSystem
```

## 3. Levantar la base de datos con Docker

```bash
docker compose up -d
```

Espera unos 15-20 segundos a que el contenedor esté "healthy":

```bash
docker ps
```

## 4. Aplicar el esquema (las 4 tablas)

```bash
docker exec libreria-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Libreria2026!" -C -i /scripts/schema.sql
```

> Si da error de que no existe `/opt/mssql-tools18`, corre
> `docker exec libreria-db ls /opt` para ver el nombre exacto de la carpeta
> en tu versión de imagen (puede ser `mssql-tools` sin el 18) y ajusta la ruta.

Solo se necesita correr este paso **una vez** (o cada vez que borres el volumen
`mssql_data`). El script es seguro de re-ejecutar: no duplica tablas ni datos.

## 5. Ejecutar el proyecto

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

## 6. Cómo trabajar en equipo sin bloquearse

- **No trabajes directo sobre `main`.** Cada persona crea su rama:
  ```bash
  git checkout -b feature/categorias-crud
  ```
- Haz commits pequeños y frecuentes, y sube tu rama:
  ```bash
  git push origin feature/categorias-crud
  ```
- Abre un Pull Request a `main` cuando tu historia de usuario esté lista.
  Así evitamos que alguien sobrescriba el trabajo de otro.
- **Antes de empezar a programar cada día**, trae los últimos cambios:
  ```bash
  git checkout main
  git pull
  git checkout tu-rama
  git merge main
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
- `Pages/Categorias/Index.cshtml.cs` es un ejemplo funcional completo de
  consulta con ADO.NET puro (parametrizado, sin concatenar SQL) — úsenlo
  como plantilla para el resto de los módulos.
- La contraseña de SQL Server (`Libreria2026!`) está en texto plano en
  `docker-compose.yml` y `appsettings.json` porque el proyecto corre solo en
  local para fines académicos. No es una práctica recomendada para producción.
