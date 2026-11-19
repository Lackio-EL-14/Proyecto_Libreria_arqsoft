-- =========================================================
-- MS Usuarios · datos semilla (US-111). Idempotente.
-- GUID fijos de docs/contratos/datos-semilla.md.
-- Las contraseñas no se documentan aquí. En Keycloak las fija
-- keycloak/configurar.sh desde .env (KEYCLOAK_*_SEMILLA_PASSWORD).
-- Los hashes Argon2 son TRANSITORIOS (login propio hasta US-125).
-- =========================================================

-- UX_Usuario_KeycloakId es un índice filtrado: crearlo y escribir en
-- dbo.Usuario exige QUOTED_IDENTIFIER ON. ADO.NET lo trae activo;
-- sqlcmd no, así que se fija aquí para toda la sesión.
SET QUOTED_IDENTIFIER ON;
GO

USE LibreriaUsuarios;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE Nombre = N'Administrador')
    INSERT INTO dbo.Rol (Nombre, Descripcion)
    VALUES (N'Administrador', N'Acceso total a catálogos, clientes, proveedores y usuarios');

IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE Nombre = N'Vendedor')
    INSERT INTO dbo.Rol (Nombre, Descripcion)
    VALUES (N'Vendedor', N'Consulta de catálogo y gestión de clientes');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Usuario
               WHERE PublicId = 'a0000000-0000-0000-0000-000000000001' OR NombreUsuario = N'admin')
    INSERT INTO dbo.Usuario (PublicId, NombreUsuario, Nombre, ApellidoPaterno, ApellidoMaterno,
                             NombreCompleto, Correo, PasswordHash, RolId, Estado, FechaCreacion)
    SELECT 'a0000000-0000-0000-0000-000000000001', N'admin', N'Administrador', N'Sistema', NULL,
           N'Administrador Sistema', N'admin@libreria.local',
           N'$argon2id$v=19$m=65536,t=3,p=2$hMoB9SdI1DZ2jluCBzKNrw$56NVveYp/elIPZQVGMYG5DZ7BDUa94CUoZuJWeKpmxg',
           RolId, N'Activo', '2026-11-01T08:00:00'
    FROM dbo.Rol WHERE Nombre = N'Administrador';

IF NOT EXISTS (SELECT 1 FROM dbo.Usuario
               WHERE PublicId = 'a0000000-0000-0000-0000-000000000002' OR NombreUsuario = N'vendedor')
    INSERT INTO dbo.Usuario (PublicId, NombreUsuario, Nombre, ApellidoPaterno, ApellidoMaterno,
                             NombreCompleto, Correo, PasswordHash, RolId, Estado,
                             FechaCreacion, UsuarioCreacionId)
    SELECT 'a0000000-0000-0000-0000-000000000002', N'vendedor', N'Vendedor', N'Mostrador', NULL,
           N'Vendedor Mostrador', N'vendedor@libreria.local',
           N'$argon2id$v=19$m=65536,t=3,p=2$Z4mACIjDrYYGsZ6qUBnowA$8m3mPzVPXCqXntIigdrzBOeeTrtjXdifwvzE6dF84Ig',
           RolId, N'Activo', '2026-11-01T08:05:00', 'a0000000-0000-0000-0000-000000000001'
    FROM dbo.Rol WHERE Nombre = N'Vendedor';
GO

-- US-124: admin y vendedor existen en Keycloak con el mismo GUID
-- (keycloak/usuarios-semilla.json), así que KeycloakId = PublicId.
UPDATE dbo.Usuario
SET KeycloakId = PublicId
WHERE PublicId IN ('a0000000-0000-0000-0000-000000000001',
                   'a0000000-0000-0000-0000-000000000002')
  AND KeycloakId IS NULL;
GO
