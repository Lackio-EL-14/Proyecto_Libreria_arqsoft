-- =========================================================
-- MS Usuarios · SQL Server · LibreriaUsuarios (US-111)
-- Esquema inicial de la sección 5 del planning. Idempotente.
-- US-124: las credenciales viven en Keycloak. KeycloakId guarda el
-- sub del token. PasswordHash y ContrasenaTemporalExpiracion son
-- TRANSITORIAS: solo las usa el login propio hasta US-125.
-- =========================================================

-- UX_Usuario_KeycloakId es un índice filtrado: crearlo y escribir en
-- dbo.Usuario exige QUOTED_IDENTIFIER ON. ADO.NET lo trae activo;
-- sqlcmd no, así que se fija aquí para toda la sesión.
SET QUOTED_IDENTIFIER ON;
GO

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
-- KeycloakId: id del usuario en Keycloak (el sub del token), sin FK.
-- PasswordHash (TRANSITORIA, se elimina en US-125): hash Argon2id
-- del login propio; nunca la contraseña.
-- La auditoría guarda el PublicId (sub del JWT) sin FK, igual
-- que en las otras tres BD.
-- ---------------------------------------------------------
IF OBJECT_ID('dbo.Usuario', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuario (
        UsuarioId                    INT IDENTITY(1,1) PRIMARY KEY,
        PublicId                     UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_Usuario_PublicId DEFAULT NEWID(),
        KeycloakId                   UNIQUEIDENTIFIER NULL,
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

-- ---------------------------------------------------------
-- US-124: KeycloakId en bases creadas antes de esta historia.
-- Único solo cuando tiene valor (los usuarios aún sin alta en
-- Keycloak quedan en NULL).
-- ---------------------------------------------------------
IF COL_LENGTH('dbo.Usuario', 'KeycloakId') IS NULL
BEGIN
    ALTER TABLE dbo.Usuario ADD KeycloakId UNIQUEIDENTIFIER NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UX_Usuario_KeycloakId'
                 AND object_id = OBJECT_ID('dbo.Usuario'))
BEGIN
    CREATE UNIQUE INDEX UX_Usuario_KeycloakId
        ON dbo.Usuario (KeycloakId)
        WHERE KeycloakId IS NOT NULL;
END
GO
