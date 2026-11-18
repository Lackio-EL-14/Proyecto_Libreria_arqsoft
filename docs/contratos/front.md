# Front y clientes HTTP

El Front conoce una única URL: `Servicios:Gateway:BaseUrl`. Cada módulo implementa un Adapter HTTP tipado sobre `ApiClientBase`.

| Módulo | Interfaz | Rutas |
|---|---|---|
| Clientes | `IServicioClientesClient` | `api/clientes` |
| Proveedores | `IServicioProveedoresClient` | `api/proveedores` |
| Productos | `IServicioProductosClient` | `api/productos`, `api/categorias`, `api/marcas`, `api/historico` |
| Usuarios | `IServicioUsuariosClient` | `api/auth`, `api/usuarios` |

Registro en `Program.cs`:

```csharp
builder.Services
    .AddContextoUsuarioServicios()
    .AddModuloClientes(builder.Configuration)
    .AddModuloProveedores(builder.Configuration)
    .AddModuloProductos(builder.Configuration)
    .AddModuloUsuarios(builder.Configuration);
```

`UsuarioActualDelegatingHandler` agrega el contexto de identidad a las peticiones. Los menús son parciales independientes: `_MenuClientes`, `_MenuProveedores`, `_MenuInventario` y `_MenuUsuarios`.
