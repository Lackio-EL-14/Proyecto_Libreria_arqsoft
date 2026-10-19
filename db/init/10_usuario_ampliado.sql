USE LibreriaDb;
GO

-- =========================================================
-- US-65: Ampliación de Usuario
-- =========================================================

-- Nombre, Apellido y Correo
IF COL_LENGTH('dbo.Usuario', 'Nombre') IS NULL
    ALTER TABLE dbo.Usuario ADD Nombre NVARCHAR(100) NULL;

IF COL_LENGTH('dbo.Usuario', 'Apellido') IS NULL
    ALTER TABLE dbo.Usuario ADD Apellido NVARCHAR(100) NULL;

IF COL_LENGTH('dbo.Usuario', 'Correo') IS NULL
    ALTER TABLE dbo.Usuario ADD Correo NVARCHAR(254) NULL;
GO

-- Completar datos de usuarios existentes.
UPDATE dbo.Usuario
SET
    Nombre = CASE
        WHEN Nombre IS NULL OR LTRIM(RTRIM(Nombre)) = N''
            THEN LEFT(NombreCompleto, CHARINDEX(N' ', NombreCompleto + N' ') - 1)
        ELSE Nombre
    END,
    Apellido = CASE
        WHEN Apellido IS NULL OR LTRIM(RTRIM(Apellido)) = N''
            THEN LTRIM(SUBSTRING(
                NombreCompleto,
                CHARINDEX(N' ', NombreCompleto + N' ') + 1,
                LEN(NombreCompleto)
            ))
        ELSE Apellido
    END,
    Correo = CASE
        WHEN Correo IS NULL OR LTRIM(RTRIM(Correo)) = N''
            THEN LOWER(NombreUsuario) + N'@local.invalid'
        ELSE Correo
    END;
GO

ALTER TABLE dbo.Usuario ALTER COLUMN Nombre NVARCHAR(100) NOT NULL;
ALTER TABLE dbo.Usuario ALTER COLUMN Apellido NVARCHAR(100) NOT NULL;
ALTER TABLE dbo.Usuario ALTER COLUMN Correo NVARCHAR(254) NOT NULL;
GO

-- Correo único
IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE name = 'UQ_Usuario_Correo'
      AND parent_object_id = OBJECT_ID('dbo.Usuario')
)
BEGIN
    ALTER TABLE dbo.Usuario
    ADD CONSTRAINT UQ_Usuario_Correo UNIQUE (Correo);
END
GO

-- Los usuarios Pendientes todavía no tienen contraseña.
ALTER TABLE dbo.Usuario
ALTER COLUMN PasswordHash NVARCHAR(500) NULL;
GO

-- Token de activación.
IF COL_LENGTH('dbo.Usuario', 'TokenActivacionHash') IS NULL
    ALTER TABLE dbo.Usuario ADD TokenActivacionHash NVARCHAR(200) NULL;

IF COL_LENGTH('dbo.Usuario', 'TokenActivacionExpiracion') IS NULL
    ALTER TABLE dbo.Usuario ADD TokenActivacionExpiracion DATETIME2 NULL;

IF COL_LENGTH('dbo.Usuario', 'TokenActivacionFechaUso') IS NULL
    ALTER TABLE dbo.Usuario ADD TokenActivacionFechaUso DATETIME2 NULL;
GO

-- Auditoría.
IF COL_LENGTH('dbo.Usuario', 'UsuarioCreacionId') IS NULL
    ALTER TABLE dbo.Usuario ADD UsuarioCreacionId INT NULL;

IF COL_LENGTH('dbo.Usuario', 'UsuarioModificacionId') IS NULL
    ALTER TABLE dbo.Usuario ADD UsuarioModificacionId INT NULL;
GO

-- =========================================================
-- Estado:
-- antes era BIT
-- ahora: Pendiente / Activo / Inactivo
-- =========================================================

DECLARE @TipoEstado NVARCHAR(128);

SELECT @TipoEstado = DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'Usuario'
  AND COLUMN_NAME = 'Estado';

IF @TipoEstado = N'bit'
BEGIN
    DECLARE @DefaultEstado NVARCHAR(128);

    SELECT @DefaultEstado = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON dc.parent_object_id = c.object_id
       AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.Usuario')
      AND c.name = 'Estado';

    IF @DefaultEstado IS NOT NULL
        EXEC(N'ALTER TABLE dbo.Usuario DROP CONSTRAINT [' + @DefaultEstado + N']');

    ALTER TABLE dbo.Usuario
    ALTER COLUMN Estado NVARCHAR(20) NOT NULL;

    UPDATE dbo.Usuario
    SET Estado =
        CASE
            WHEN Estado = N'1' THEN N'Activo'
            ELSE N'Inactivo'
        END;
END
GO

-- Normalizar por si quedó una migración parcial.
UPDATE dbo.Usuario
SET Estado = N'Activo'
WHERE Estado = N'1';

UPDATE dbo.Usuario
SET Estado = N'Inactivo'
WHERE Estado = N'0';
GO

-- Default de nuevos usuarios: Pendiente.
DECLARE @DefaultEstadoActual NVARCHAR(128);

SELECT @DefaultEstadoActual = dc.name
FROM sys.default_constraints dc
INNER JOIN sys.columns c
    ON dc.parent_object_id = c.object_id
   AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.Usuario')
  AND c.name = 'Estado';

IF @DefaultEstadoActual IS NOT NULL
    EXEC(N'ALTER TABLE dbo.Usuario DROP CONSTRAINT [' + @DefaultEstadoActual + N']');

ALTER TABLE dbo.Usuario
ADD CONSTRAINT DF_Usuario_Estado
DEFAULT (N'Pendiente') FOR Estado;
GO

IF OBJECT_ID('dbo.CK_Usuario_Estado', 'C') IS NULL
BEGIN
    ALTER TABLE dbo.Usuario
    ADD CONSTRAINT CK_Usuario_Estado
        CHECK (Estado IN (N'Pendiente', N'Activo', N'Inactivo'));
END
GO

-- Auditoría de Usuario.
IF OBJECT_ID('dbo.FK_Usuario_UsuarioCreacion', 'F') IS NULL
BEGIN
    ALTER TABLE dbo.Usuario
    ADD CONSTRAINT FK_Usuario_UsuarioCreacion
        FOREIGN KEY (UsuarioCreacionId)
        REFERENCES dbo.Usuario (UsuarioId);
END

IF OBJECT_ID('dbo.FK_Usuario_UsuarioModificacion', 'F') IS NULL
BEGIN
    ALTER TABLE dbo.Usuario
    ADD CONSTRAINT FK_Usuario_UsuarioModificacion
        FOREIGN KEY (UsuarioModificacionId)
        REFERENCES dbo.Usuario (UsuarioId);
END
GO