# Usuario actual y bitácora

- Cada MS tiene **su** puerto de usuario actual (`IUsuarioActual` en Ventas y Clientes, `ICurrentUser` en Usuarios, su equivalente en Productos) con: `Guid? UsuarioId`, `string? NombreUsuario`, `string? Iniciales`.
- **Adaptador definitivo:** `UsuarioActualDesdeToken` *(propuesto)*, lee los claims del JWT (US-127).
- **Hasta que exista:** `UsuarioActualDesarrollo` devuelve el `PublicId` fijo de `admin` de [datos-semilla.md](datos-semilla.md), **solo en `Development`** (US-110).
- La carpeta **`Infrastructure/Seguridad/`** de cada MS es del **Grupo 2**: ahí viven la validación del JWT y el adaptador de usuario actual. El Grupo 1 no la edita; el Grupo 2 no edita nada fuera de ella sin PR revisado por el dueño del MS.
- Auditoría en las 4 BD: `UsuarioCreacionId`, `UsuarioModificacionId` (y `UsuarioAnulacionId` en `Venta`) guardan el `sub` del JWT, junto a `FechaCreacion` y `FechaModificacion`.
- Los nombres para «creado por» se piden a `GET api/usuarios/nombres?ids=<guid>,<guid>`.
