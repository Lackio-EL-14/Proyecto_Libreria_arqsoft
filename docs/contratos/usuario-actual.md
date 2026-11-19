# Usuario actual y auditoría

Cada microservicio define su propio puerto de usuario actual. Las capas de Aplicación y Dominio no dependen de `HttpContext`.

- En desarrollo, el adaptador de prueba entrega el identificador fijo del administrador.
- El adaptador definitivo lee los claims de la identidad autenticada.
- `UsuarioCreacionId` y `UsuarioModificacionId` guardan el **`sub` del token de Keycloak** (en Usuarios, el `KeycloakId`; en los usuarios semilla coincide con su `PublicId`), junto con las fechas correspondientes.
- El repositorio recibe el puerto por inyección y registra la auditoría en su propia base.

Esto aplica inversión de dependencias: los casos de uso conocen una abstracción; la infraestructura decide cómo obtener la identidad.
