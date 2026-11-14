# Cómo consume el Front a los microservicios (US-113)

## Clientes tipados (patrón Adapter)

Un cliente por microservicio. Todos heredan de `Clients/Common/ApiClientBase` y se registran con `UsuarioActualDelegatingHandler` (allí se agregará el `Bearer` en US-125).

| Interfaz | Carpeta | Dueño | Rutas |
|---|---|---|---|
| `IServicioClientesClient` | `Clients/Clientes` | Edson | `api/clientes` |
| `IServicioVentasClient` | `Clients/Ventas` | Cesar | `api/ventas` |
| `IServicioProductosClient` | `Clients/Productos` | Josue | `api/productos`, `api/categorias`, `api/marcas`, `api/historico` |
| `IServicioUsuariosClient` | `Clients/Usuarios` | Grupo 2 | `api/auth`, `api/usuarios` |

`ApiClientBase` ofrece dos estilos (ver [rest-errores-result.md](rest-errores-result.md)):

- **Genérico** (`GetAsync`, `PostAsync`, `PutAsync`, `DeleteAsync`): lanza `ApiClientException` o `ServicioNoDisponibleException`.
- **Tipado con `ResultadoApi`** (`LeerAsync`, `LeerOpcionalAsync`, `EnviarPostAsync`, `EnviarPutAsync`, `EnviarPatchAsync`): las escrituras nunca lanzan; las lecturas lanzan `ServicioNoDisponibleException` si el MS no responde y `HttpRequestException` ante otro error.

## DTOs

Cada módulo tiene su carpeta y su namespace (ya movidos en US-113; **nadie necesita moverlos otra vez**):

| Carpeta / namespace | Contenido |
|---|---|
| `DTOs/Common` (`Libreria.Client.DTOs.Common`) | `ResultadoApi`, `ApiProblemDetailsDto`, `AuditoriaRegistroDto` |
| `DTOs/Clientes` (`Libreria.Client.DTOs.Clientes`) | `ClienteInput`, `ClienteRegistroResult`, `ClienteVentaBusquedaDto` |
| `DTOs/Ventas` (`Libreria.Client.DTOs.Ventas`) | `VentaDtos`, `RegistrarVentaInput`, `DetalleVentaInput`, `ProductoVentaBusquedaDto` |
| `DTOs/Productos` (`Libreria.Client.DTOs.Productos`) | `CatalogoDtos` (categoría, marca), `ProductoDtos`, `HistoricoDtos` |
| `DTOs/Usuarios` (`Libreria.Client.DTOs.Usuarios`) | Login, usuarios, roles, contraseña |

## `Program.cs`

Un método de extensión por módulo, en `Clients/DependencyInjection/ServicioClientsExtensions.cs`. Cada dueño edita solo el suyo:

```csharp
builder.Services
    .AddContextoUsuarioServicios()
    .AddModuloClientes(builder.Configuration)    // Edson
    .AddModuloVentas(builder.Configuration)      // Cesar
    .AddModuloProductos(builder.Configuration)   // Josue
    .AddModuloUsuarios(builder.Configuration);   // Grupo 2
```

Configuración: `Servicios:Gateway:BaseUrl` (obligatoria; sin ella el Front no arranca) y `Servicios:TimeoutSeconds` (30 por defecto, igual que el `ActivityTimeout` del gateway).

## Menús

| Parcial | Contenido | Dueño |
|---|---|---|
| `_MenuVentas` | Registrar venta, Ventas | Cesar |
| `_MenuClientes` | Clientes (aparece cuando exista `Pages/Clientes/Index`, US-116) | Edson |
| `_MenuInventario` | Productos, Categorías, Marcas | Josue |
| `_MenuUsuarios` | Usuarios | Grupo 2 |
