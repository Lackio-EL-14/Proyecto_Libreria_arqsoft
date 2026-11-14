-- =========================================================
-- MS Usuarios · SQL Server · LibreriaUsuarios (US-111)
-- Esquema inicial de la sección 5 del planning. Idempotente.
-- Sin token de activación: el primer ingreso usa contraseña
-- temporal (ContrasenaTemporalExpiracion) y Estado = 'Pendiente'.
-- =========================================================

IF DB_ID('LibreriaUsuarios') IS NULL
BEGIN
    CREATE DATABASE LibreriaUsuarios;
END
GO

USE LibreriaUsuarios;
GO

-- ---------------------------------------------------------
-- Rol
-- ---------------------------------------------------------
IF OBJECT_ID('dbo.Rol', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Rol (
        RolId        INT IDENTITY(1,1) PRIMARY KEY,
        Nombre       NVARCHAR(50) NOT NULL,
        Descripcion  NVARCHAR(200) NULL,
        Estado       BIT NOT NULL
            CONSTRAINT DF_Rol_Estado DEFAULT (1),

        CONSTRAINT UQ_Rol_Nombre UNIQUE (Nombre),
        CONSTRAINT CK_Rol_Nombre_NoVacio
            CHECK (LTRIM(RTRIM(Nombre)) <> N'')
    );
END
GO

-- ---------------------------------------------------------
-- Usuario
-- PasswordHash guarda el hash Argon2id con sal aleatoria;
-- nunca la contraseña.
-- La auditoría guarda el PublicId (sub del JWT) sin FK, igual
-- que en las otras tres BD.
-- ---------------------------------------------------------
IF OBJECT_ID('dbo.Usuario', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuario (
        UsuarioId                    INT IDENTITY(1,1) PRIMARY KEY,
        PublicId                     UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_Usuario_PublicId DEFAULT NEWID(),
        NombreUsuario                NVARCHAR(50) NOT NULL,
        Nombre                       NVARCHAR(100) NOT NULL,
        ApellidoPaterno              NVARCHAR(100) NOT NULL,
        ApellidoMaterno              NVARCHAR(100) NULL,
        NombreCompleto               NVARCHAR(150) NOT NULL,
        Correo                       NVARCHAR(254) NOT NULL,
        PasswordHash                 NVARCHAR(500) NULL,
        ContrasenaTemporalExpiracion DATETIME2 NULL,
        RolId                        INT NOT NULL,
        Estado                       NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Usuario_Estado DEFAULT (N'Pendiente'),

        FechaCreacion                DATETIME2 NOT NULL
            CONSTRAINT DF_Usuario_FechaCreacion DEFAULT (SYSDATETIME()),
        FechaModificacion            DATETIME2 NULL,
        UsuarioCreacionId            UNIQUEIDENTIFIER NULL,
        UsuarioModificacionId        UNIQUEIDENTIFIER NULL,

        CONSTRAINT UQ_Usuario_PublicId UNIQUE (PublicId),
        CONSTRAINT UQ_Usuario_NombreUsuario UNIQUE (NombreUsuario),
        CONSTRAINT UQ_Usuario_Correo UNIQUE (Correo),
        CONSTRAINT FK_Usuario_Rol FOREIGN KEY (RolId)
            REFERENCES dbo.Rol (RolId),
        CONSTRAINT CK_Usuario_NombreUsuario_NoVacio
            CHECK (LTRIM(RTRIM(NombreUsuario)) <> N''),
        CONSTRAINT CK_Usuario_ApellidoPaterno_NoVacio
            CHECK (LTRIM(RTRIM(ApellidoPaterno)) <> N''),
        CONSTRAINT CK_Usuario_Estado
            CHECK (Estado IN (N'Pendiente', N'Activo', N'Inactivo'))
    );
END
GO
