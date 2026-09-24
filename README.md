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
        │   ├── Categorias/    # Gestión de Categorías
        │   ├── Marcas/        # Gestión de Marcas
        │   ├── Productos/     # Catálogo e Inventario
        │   ├── Historico/     # Auditoría de Costos
        │   └── Shared/        # Componentes reutilizables (_ConfirmModal, _Layout)
        └── wwwroot/
            ├── css/           # Estilos y variables de diseño
            └── js/            # Scripts (confirm-modal, sidebar)
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

## 5. Componente Compartido: Modal de Confirmación (US-28)

Para realizar bajas lógicas o acciones críticas sin redirigir a páginas completas, se utiliza el componente compartido `_ConfirmModal.cshtml`.

### Cómo invocar el Modal desde cualquier vista Razor

1. **Importar el namespace de modelos:**
   ```cshtml
   @using Libreria.Web.Pages.Shared.Models
   ```

2. **Crear el botón disparador en la tabla o interfaz:**
   ```cshtml
   <button
       type="button"
       class="btn-table btn-table--danger"
       data-confirm-trigger
       data-modal-target="mi-modal-id"
       data-entity-id="@item.PublicId"
       data-entity-name="@item.Nombre"
       data-has-warning="false">
       Dar de baja
   </button>
   ```

3. **Renderizar la vista parcial al final del archivo `.cshtml`:**
   ```cshtml
   @await Html.PartialAsync(
       "_ConfirmModal",
       new ConfirmModalModel
       {
           Id = "mi-modal-id",
           Titulo = "Confirmar baja de registro",
           MensajePrincipal = "¿Está seguro de dar de baja el registro",
           Handler = "DarDeBaja",
           CampoIdentificador = "publicId",
           TextoBotonConfirmar = "Confirmar baja",
           TextoBotonCancelar = "Cancelar",
           MensajeAdvertencia = "Esta acción desactivará el elemento del catálogo activo."
       })
   ```

4. **Recibir la petición en el `PageModel` (`.cshtml.cs`):**
   ```csharp
   public async Task<IActionResult> OnPostDarDeBajaAsync(Guid publicId)
   {
       await _repository.CambiarEstadoAsync(publicId, false);
       TempData["MensajeExito"] = "Registro dado de baja correctamente.";
       return RedirectToPage("./Index");
   }
   ```

### Características del Modal
- **Accesibilidad y Focus Trap:** Navegación por teclado dentro del modal con <kbd>Tab</kbd> y cierre con <kbd>Escape</kbd>.
- **Cierre por Overlay:** Clic en el fondo oscuro para cancelar la operación.
- **Advertencias Condicionales:** Muestra advertencias visuales en ámbar/rojo si la entidad posee registros vinculados activos.

## 6. Ejecutar el proyecto con Visual Studio / Terminal

```bash
cd src/Libreria.Web
dotnet restore
dotnet run
```

Abre la URL que muestra la consola (`http://localhost:5000`).

#### Para ingresar al contenedor de la base de datos

```bash
docker exec -it libreria-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Libreria2026!' -C
```

## 7. Cómo trabajar en equipo sin bloquearse

- **No trabajes directo sobre `develop`.** Cada persona actualiza `develop` y crea su rama:
  ```bash
  git switch develop
  git pull origin develop
  git checkout -b feature/mi-funcionalidad
  ```
- Haz commits pequeños y frecuentes, y sube tu rama:
  ```bash
  git push origin feature/mi-funcionalidad
  ```
- Abre un Pull Request a `develop` cuando tu historia de usuario esté lista.

## 8. Notas de diseño

- El acceso a datos usa `IDbConnectionFactory` (interfaz) en vez de crear `SqlConnection` directamente en cada página — Inversión de Dependencias (SOLID).
- Las páginas Razor dependen de contratos específicos y delegan el acceso ADO.NET a repositorios. Las validaciones de negocio viven en clases separadas de las entidades.
- **Navegación Escalable (US-29):** Mini-Sidebar colapsable con memoria de estado y Dashboard con métricas de KPI en tiempo real.
