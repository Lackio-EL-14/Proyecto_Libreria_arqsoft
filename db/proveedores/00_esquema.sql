-- MS Proveedores · SQL Server · esquema relacional en tercera forma normal.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID('LibreriaProveedores') IS NULL CREATE DATABASE LibreriaProveedores;
GO
USE LibreriaProveedores;
GO

IF OBJECT_ID('dbo.Proveedor','U') IS NULL
BEGIN
    CREATE TABLE dbo.Proveedor(
        ProveedorId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Proveedor PRIMARY KEY,
        PublicId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Proveedor_PublicId DEFAULT NEWID(),
        Nit NVARCHAR(30) NOT NULL,
        RazonSocial NVARCHAR(200) NOT NULL,
        Telefono NVARCHAR(30) NULL,
        Correo NVARCHAR(254) NULL,
        Estado BIT NOT NULL CONSTRAINT DF_Proveedor_Estado DEFAULT 1,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Proveedor_FechaCreacion DEFAULT SYSUTCDATETIME(),
        UsuarioCreacionId UNIQUEIDENTIFIER NULL,
        FechaModificacion DATETIME2 NULL,
        UsuarioModificacionId UNIQUEIDENTIFIER NULL,
        CONSTRAINT UQ_Proveedor_PublicId UNIQUE(PublicId),
        CONSTRAINT UQ_Proveedor_Nit UNIQUE(Nit)
    );
END
GO

-- Los datos de personas de contacto no se repiten en Proveedor: dependen de su propia clave.
IF OBJECT_ID('dbo.ContactoProveedor','U') IS NULL
BEGIN
    CREATE TABLE dbo.ContactoProveedor(
        ContactoProveedorId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ContactoProveedor PRIMARY KEY,
        ProveedorId INT NOT NULL,
        Nombre NVARCHAR(150) NOT NULL,
        Cargo NVARCHAR(100) NULL,
        Telefono NVARCHAR(30) NULL,
        Correo NVARCHAR(254) NULL,
        EsPrincipal BIT NOT NULL CONSTRAINT DF_ContactoProveedor_Principal DEFAULT 0,
        CONSTRAINT FK_ContactoProveedor_Proveedor FOREIGN KEY(ProveedorId)
            REFERENCES dbo.Proveedor(ProveedorId),
        CONSTRAINT CK_ContactoProveedor_Medio CHECK(Telefono IS NOT NULL OR Correo IS NOT NULL)
    );
END
GO

IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UQ_ContactoProveedor_Principal' AND object_id=OBJECT_ID('dbo.ContactoProveedor'))
    CREATE UNIQUE INDEX UQ_ContactoProveedor_Principal ON dbo.ContactoProveedor(ProveedorId) WHERE EsPrincipal=1;
GO
