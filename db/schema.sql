-- =========================================================
-- Esquema inicial: Sistema de inventario - Librería
-- Tablas: Categoria, Marca, Cliente, Rol, Usuario, Producto, HistoricoCostoProducto, Venta, DetalleVenta
-- =========================================================

IF DB_ID('LibreriaDb') IS NULL
BEGIN
    CREATE DATABASE LibreriaDb;
END
GO

USE LibreriaDb;
GO

-- ---------------------------------------------------------
-- Categoria
-- ---------------------------------------------------------
IF OBJECT_ID('dbo.Categoria', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categoria (
        CategoriaId       INT IDENTITY(1,1) PRIMARY KEY,
        Codigo            NVARCHAR(20) NOT NULL,
        Nombre            NVARCHAR(100) NOT NULL,
        Descripcion       NVARCHAR(255) NULL,
        Ubicacion         NVARCHAR(100) NOT NULL
            CONSTRAINT DF_Categoria_Ubicacion DEFAULT (N'Sin asignar'),
        Estado            BIT NOT NULL DEFAULT (1),
        FechaCreacion     DATETIME2 NOT NULL DEFAULT (SYSDATETIME()),
        FechaModificacion DATETIME2 NULL,
        CONSTRAINT UQ_Categoria_Codigo UNIQUE (Codigo),
        CONSTRAINT UQ_Categoria_Nombre UNIQUE (Nombre),
        CONSTRAINT CK_Categoria_Ubicacion CHECK (
            Ubicacion IN (N'Estante principal', N'Depósito', N'Vitrina', N'Bodega', N'Sin asignar')
        )
    );
END
GO

IF COL_LENGTH('dbo.Categoria', 'Codigo') IS NULL
BEGIN
    ALTER TABLE dbo.Categoria ADD Codigo NVARCHAR(20) NULL;
END
GO

UPDATE dbo.Categoria
SET Codigo = CONCAT(N'CAT-', RIGHT(N'000000' + CONVERT(NVARCHAR(6), CategoriaId), 6))
WHERE Codigo IS NULL OR LTRIM(RTRIM(Codigo)) = N'';
GO

ALTER TABLE dbo.Categoria ALTER COLUMN Codigo NVARCHAR(20) NOT NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_Categoria_Codigo'
      AND object_id = OBJECT_ID('dbo.Categoria')
)
BEGIN
    CREATE UNIQUE INDEX UQ_Categoria_Codigo ON dbo.Categoria (Codigo);
END
GO

IF COL_LENGTH('dbo.Categoria', 'Ubicacion') IS NULL
BEGIN
    ALTER TABLE dbo.Categoria ADD Ubicacion NVARCHAR(100) NOT NULL
        CONSTRAINT DF_Categoria_Ubicacion DEFAULT (N'Sin asignar');
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.Categoria')
      AND c.name = 'Ubicacion'
)
BEGIN
    ALTER TABLE dbo.Categoria ADD CONSTRAINT DF_Categoria_Ubicacion
        DEFAULT (N'Sin asignar') FOR Ubicacion;
END
GO

UPDATE dbo.Categoria
SET Ubicacion = CASE UPPER(LTRIM(RTRIM(Ubicacion)))
    WHEN N'ESTANTE PRINCIPAL' THEN N'Estante principal'
    WHEN N'DEPÓSITO' THEN N'Depósito'
    WHEN N'VITRINA' THEN N'Vitrina'
    WHEN N'BODEGA' THEN N'Bodega'
    WHEN N'SIN ASIGNAR' THEN N'Sin asignar'
    ELSE N'Sin asignar'
END;
GO

IF OBJECT_ID('dbo.CK_Categoria_Ubicacion', 'C') IS NULL
BEGIN
    ALTER TABLE dbo.Categoria WITH CHECK ADD CONSTRAINT CK_Categoria_Ubicacion
        CHECK (Ubicacion IN (
            N'Estante principal', N'Depósito', N'Vitrina', N'Bodega', N'Sin asignar'
        ));
END
GO

IF COL_LENGTH('dbo.Categoria', 'Orden') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Categoria DROP COLUMN Orden;
END
GO

-- ---------------------------------------------------------
-- Marca
-- ---------------------------------------------------------
IF OBJECT_ID('dbo.Marca', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Marca (
        MarcaId           INT IDENTITY(1,1) PRIMARY KEY,
        Nombre            NVARCHAR(100) NOT NULL,
        Descripcion       NVARCHAR(255) NULL,
        PaisOrigen        NVARCHAR(100) NOT NULL,
        SitioWeb          NVARCHAR(200) NULL,
        Estado            BIT NOT NULL DEFAULT (1),
        FechaCreacion     DATETIME2 NOT NULL DEFAULT (SYSDATETIME()),
        FechaModificacion DATETIME2 NULL,
        CONSTRAINT UQ_Marca_Nombre UNIQUE (Nombre)
    );
END
GO

IF COL_LENGTH('dbo.Marca', 'SitioWeb') IS NULL
BEGIN
    ALTER TABLE dbo.Marca ADD SitioWeb NVARCHAR(200) NULL;
END
GO

DECLARE @PaisesValidos TABLE (
    Nombre NVARCHAR(100) PRIMARY KEY
);

INSERT INTO @PaisesValidos (Nombre)
VALUES
    (N'No especificado'),
    (N'Alemania'),
    (N'Argentina'),
    (N'Bolivia'),
    (N'Brasil'),
    (N'Canadá'),
    (N'Chile'),
    (N'China'),
    (N'Colombia'),
    (N'Corea del Sur'),
    (N'Ecuador'),
    (N'España'),
    (N'Estados Unidos'),
    (N'Francia'),
    (N'India'),
    (N'Italia'),
    (N'Japón'),
    (N'México'),
    (N'Paraguay'),
    (N'Perú'),
    (N'Reino Unido'),
    (N'Uruguay'),
    (N'Venezuela');

UPDATE m
SET PaisOrigen = COALESCE(p.Nombre, N'No especificado')
FROM dbo.Marca m
OUTER APPLY (
    SELECT TOP 1 pv.Nombre
    FROM @PaisesValidos pv
    WHERE pv.Nombre = LTRIM(RTRIM(m.PaisOrigen))
) p;
GO

ALTER TABLE dbo.Marca ALTER COLUMN PaisOrigen NVARCHAR(100) NOT NULL;
GO

-- ---------------------------------------------------------
-- Cliente
-- ---------------------------------------------------------
IF OBJECT_ID('dbo.Cliente', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cliente (
        ClienteId    INT IDENTITY(1,1) PRIMARY KEY,
        CiNit        NVARCHAR(30) NOT NULL,
        RazonSocial  NVARCHAR(200) NOT NULL,
        Correo       NVARCHAR(254) NULL,

        CONSTRAINT UQ_Cliente_CiNit UNIQUE (CiNit),
        CONSTRAINT CK_Cliente_CiNit_NoVacio
            CHECK (LTRIM(RTRIM(CiNit)) <> N''),
        CONSTRAINT CK_Cliente_RazonSocial_NoVacia
            CHECK (LTRIM(RTRIM(RazonSocial)) <> N'')
    );
END
GO

-- ---------------------------------------------------------
-- Rol (US-37)
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
-- Usuario (US-37)
-- PasswordHash guarda el hash PBKDF2 generado por
-- PasswordHasher de ASP.NET Core; nunca la contraseña.
-- ---------------------------------------------------------
IF OBJECT_ID('dbo.Usuario', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuario (
        UsuarioId          INT IDENTITY(1,1) PRIMARY KEY,
        PublicId           UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_Usuario_PublicId DEFAULT NEWID(),
        NombreUsuario      NVARCHAR(50) NOT NULL,
        NombreCompleto     NVARCHAR(150) NOT NULL,
        PasswordHash       NVARCHAR(500) NOT NULL,
        RolId              INT NOT NULL,
        Estado             BIT NOT NULL
            CONSTRAINT DF_Usuario_Estado DEFAULT (1),
        FechaCreacion      DATETIME2 NOT NULL
            CONSTRAINT DF_Usuario_FechaCreacion DEFAULT (SYSDATETIME()),
        FechaModificacion  DATETIME2 NULL,

        CONSTRAINT UQ_Usuario_PublicId UNIQUE (PublicId),
        CONSTRAINT UQ_Usuario_NombreUsuario UNIQUE (NombreUsuario),
        CONSTRAINT FK_Usuario_Rol FOREIGN KEY (RolId)
            REFERENCES dbo.Rol (RolId),
        CONSTRAINT CK_Usuario_NombreUsuario_NoVacio
            CHECK (LTRIM(RTRIM(NombreUsuario)) <> N'')
    );
END
GO

-- Roles base del sistema (se insertan solo si no existen)
IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE Nombre = N'Administrador')
    INSERT INTO dbo.Rol (Nombre, Descripcion)
    VALUES (N'Administrador', N'Acceso total: catálogos, ventas, anulaciones y reportes de ganancia');

IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE Nombre = N'Vendedor')
    INSERT INTO dbo.Rol (Nombre, Descripcion)
    VALUES (N'Vendedor', N'Registro de ventas y consulta de catálogo');
GO

-- Usuarios semilla (contraseñas: Admin2026! y Vendedor2026!)
IF NOT EXISTS (SELECT 1 FROM dbo.Usuario WHERE NombreUsuario = N'admin')
    INSERT INTO dbo.Usuario (NombreUsuario, NombreCompleto, PasswordHash, RolId)
    SELECT N'admin', N'Administrador del sistema',
           N'AQAAAAIAAYagAAAAEOvA0yb/Qb8i4baG1c5Jyu6QwIsOL+E/+RD5uENxg4ry41ZMRzrjyHVg2gP/OxcS4Q==',
           RolId
    FROM dbo.Rol WHERE Nombre = N'Administrador';

IF NOT EXISTS (SELECT 1 FROM dbo.Usuario WHERE NombreUsuario = N'vendedor')
    INSERT INTO dbo.Usuario (NombreUsuario, NombreCompleto, PasswordHash, RolId)
    SELECT N'vendedor', N'Vendedor de mostrador',
           N'AQAAAAIAAYagAAAAEDh3RAQ9Yc7ibdHGSmgl501Txm81qOOimdTVAg2u4ST4xUm/WSwxNhyd8tHeTamYsQ==',
           RolId
    FROM dbo.Rol WHERE Nombre = N'Vendedor';
GO

-- ---------------------------------------------------------
-- Producto
-- CostoAdquisicionActual es una copia "cacheada" del último
-- costo vigente, para no tener que hacer JOIN con el histórico
-- solo para mostrar el producto. La fuente de verdad histórica
-- vive en HistoricoCostoProducto.
-- ---------------------------------------------------------
IF OBJECT_ID('dbo.Producto', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Producto (
        ProductoId            INT IDENTITY(1,1) PRIMARY KEY,
        Nombre                NVARCHAR(150) NOT NULL,
        DescripcionEspecifica NVARCHAR(500) NULL,
        FechaVencimiento      DATE NULL,
        EsPerecedero          BIT NOT NULL DEFAULT (0),
        Stock                 INT NOT NULL DEFAULT (0),
        PrecioVenta           DECIMAL(10,2) NOT NULL,
        CostoAdquisicionActual DECIMAL(10,2) NOT NULL DEFAULT (0),
        CategoriaId           INT NOT NULL,
        MarcaId               INT NOT NULL,
        Estado                BIT NOT NULL DEFAULT (1),
        FechaCreacion         DATETIME2 NOT NULL DEFAULT (SYSDATETIME()),
        FechaModificacion     DATETIME2 NULL,
        CONSTRAINT FK_Producto_Categoria FOREIGN KEY (CategoriaId)
            REFERENCES dbo.Categoria (CategoriaId),
        CONSTRAINT FK_Producto_Marca FOREIGN KEY (MarcaId)
            REFERENCES dbo.Marca (MarcaId),
        CONSTRAINT CK_Producto_Stock CHECK (Stock >= 0),
        CONSTRAINT CK_Producto_PrecioVenta CHECK (PrecioVenta >= 0),
        CONSTRAINT CK_Producto_CostoAdquisicionActual CHECK (CostoAdquisicionActual >= 0)
    );
END
GO


IF COL_LENGTH('dbo.Producto', 'EsPerecedero') IS NULL
BEGIN
    ALTER TABLE dbo.Producto
    ADD EsPerecedero BIT NOT NULL
        CONSTRAINT DF_Producto_EsPerecedero DEFAULT (0);
END
GO

-- Los productos existentes que ya tenían fecha de vencimiento
-- pasan automáticamente a ser perecederos.
UPDATE dbo.Producto
SET EsPerecedero = 1
WHERE FechaVencimiento IS NOT NULL;
GO

-- ---------------------------------------------------------
-- HistoricoCostoProducto
-- Cada fila es un registro inmutable: nunca se actualiza ni
-- se borra, solo se inserta uno nuevo cuando cambia el costo.
-- ---------------------------------------------------------
IF OBJECT_ID('dbo.HistoricoCostoProducto', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HistoricoCostoProducto (
        HistoricoCostoId  INT IDENTITY(1,1) PRIMARY KEY,
        ProductoId        INT NOT NULL,
        CostoAdquisicion  DECIMAL(10,2) NOT NULL,
        FechaVencimiento  DATE NULL,
        TipoCambioUsd     DECIMAL(10,4) NULL,
        Motivo            NVARCHAR(200) NULL, -- ej: 'Registro inicial', 'Edición manual', 'Compra a proveedor'
        FechaVigencia     DATETIME2 NOT NULL DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_Historico_Producto FOREIGN KEY (ProductoId)
            REFERENCES dbo.Producto (ProductoId),
        CONSTRAINT CK_Historico_Costo CHECK (CostoAdquisicion >= 0)
    );
END
GO

-- =========================================================
-- MIGRACIÓN US-27: Fecha de vencimiento en histórico
-- =========================================================

IF COL_LENGTH('dbo.HistoricoCostoProducto', 'FechaVencimiento') IS NULL
BEGIN
    ALTER TABLE dbo.HistoricoCostoProducto
    ADD FechaVencimiento DATE NULL;
END
GO

-- ---------------------------------------------------------
-- Venta
-- ---------------------------------------------------------
IF OBJECT_ID('dbo.Venta', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Venta (
        VentaId             INT IDENTITY(1,1) PRIMARY KEY,
        PublicId            UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_Venta_PublicId DEFAULT NEWID(),
        ClienteId           INT NOT NULL,
        Estado              NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Venta_Estado DEFAULT (N'Activa'),
        UsuarioCreacionId   INT NOT NULL,
        UsuarioAnulacionId  INT NULL,

        CONSTRAINT UQ_Venta_PublicId UNIQUE (PublicId),
        CONSTRAINT FK_Venta_Cliente FOREIGN KEY (ClienteId)
            REFERENCES dbo.Cliente (ClienteId),
        CONSTRAINT CK_Venta_Estado
            CHECK (Estado IN (N'Activa', N'Anulada'))
    );
END
GO

-- ---------------------------------------------------------
-- DetalleVenta
-- ---------------------------------------------------------
IF OBJECT_ID('dbo.DetalleVenta', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DetalleVenta (
        DetalleVentaId           INT IDENTITY(1,1) PRIMARY KEY,
        VentaId                  INT NOT NULL,
        ProductoId               INT NOT NULL,
        Cantidad                 INT NOT NULL,
        PrecioUnitarioVenta      DECIMAL(10,2) NOT NULL,
        CostoAdquisicionUnitario DECIMAL(10,2) NOT NULL,
        Importe                  DECIMAL(10,2) NOT NULL,
        Ganancia                 DECIMAL(10,2) NOT NULL,

        CONSTRAINT FK_DetalleVenta_Venta FOREIGN KEY (VentaId)
            REFERENCES dbo.Venta (VentaId),
        CONSTRAINT FK_DetalleVenta_Producto FOREIGN KEY (ProductoId)
            REFERENCES dbo.Producto (ProductoId),
        CONSTRAINT CK_DetalleVenta_Cantidad
            CHECK (Cantidad > 0),
        CONSTRAINT CK_DetalleVenta_Precio
            CHECK (PrecioUnitarioVenta >= 0),
        CONSTRAINT CK_DetalleVenta_Costo
            CHECK (CostoAdquisicionUnitario >= 0),
        CONSTRAINT CK_DetalleVenta_Importe
            CHECK (Importe >= 0)
    );
END
GO

-- ---------------------------------------------------------
-- Datos semilla opcionales (se pueden borrar sin problema)
-- ---------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Categoria)
BEGIN
    INSERT INTO dbo.Categoria (Codigo, Nombre, Descripcion, Ubicacion) VALUES
        (N'MAT-ESC', N'Material escolar', N'Cuadernos, lápices, útiles en general', N'Estante principal'),
        (N'LIBROS', N'Libros', N'Libros de texto y literatura', N'Bodega'),
        (N'ARTE', N'Arte y manualidades', N'Pinturas, pinceles, acrílicos', N'Vitrina');
END
GO

UPDATE dbo.Categoria
SET Codigo = CONCAT(
    N'CAT-',
    CASE
        WHEN CategoriaId < 1000
            THEN RIGHT(N'000' + CONVERT(NVARCHAR(10), CategoriaId), 3)
        ELSE CONVERT(NVARCHAR(10), CategoriaId)
    END
);
GO

IF OBJECT_ID('dbo.CategoriaCodigoSequence', 'SO') IS NULL
BEGIN
    DECLARE @SiguienteCodigo INT = ISNULL((SELECT MAX(CategoriaId) FROM dbo.Categoria), 0) + 1;
    DECLARE @CrearSecuencia NVARCHAR(MAX) = N'CREATE SEQUENCE dbo.CategoriaCodigoSequence AS INT START WITH '
        + CONVERT(NVARCHAR(20), @SiguienteCodigo)
        + N' INCREMENT BY 1;';
    EXEC sys.sp_executesql @CrearSecuencia;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Marca)
BEGIN
    INSERT INTO dbo.Marca (Nombre, Descripcion, PaisOrigen, SitioWeb) VALUES
        (N'Acrilex', N'Pinturas y productos de arte', N'Brasil', N'https://acrilex.com.br'),
        (N'Norma', N'Cuadernos y útiles escolares', N'Colombia', N'https://www.norma.com'),
        (N'Genérico', N'Sin marca específica', N'No especificado', NULL);
END
GO

-- =========================================================
-- MIGRACIÓN US-18: Identificadores Públicos (GUID)
-- =========================================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Categoria') AND name = 'PublicId')
BEGIN
    ALTER TABLE dbo.Categoria ADD PublicId UNIQUEIDENTIFIER NULL;
    EXEC('UPDATE dbo.Categoria SET PublicId = NEWID() WHERE PublicId IS NULL');
    ALTER TABLE dbo.Categoria ALTER COLUMN PublicId UNIQUEIDENTIFIER NOT NULL;
    ALTER TABLE dbo.Categoria ADD CONSTRAINT DF_Categoria_PublicId DEFAULT NEWID() FOR PublicId;
    ALTER TABLE dbo.Categoria ADD CONSTRAINT UQ_Categoria_PublicId UNIQUE (PublicId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Marca') AND name = 'PublicId')
BEGIN
    ALTER TABLE dbo.Marca ADD PublicId UNIQUEIDENTIFIER NULL;
    EXEC('UPDATE dbo.Marca SET PublicId = NEWID() WHERE PublicId IS NULL');
    ALTER TABLE dbo.Marca ALTER COLUMN PublicId UNIQUEIDENTIFIER NOT NULL;
    ALTER TABLE dbo.Marca ADD CONSTRAINT DF_Marca_PublicId DEFAULT NEWID() FOR PublicId;
    ALTER TABLE dbo.Marca ADD CONSTRAINT UQ_Marca_PublicId UNIQUE (PublicId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Producto') AND name = 'PublicId')
BEGIN
    ALTER TABLE dbo.Producto ADD PublicId UNIQUEIDENTIFIER NULL;
    EXEC('UPDATE dbo.Producto SET PublicId = NEWID() WHERE PublicId IS NULL');
    ALTER TABLE dbo.Producto ALTER COLUMN PublicId UNIQUEIDENTIFIER NOT NULL;
    ALTER TABLE dbo.Producto ADD CONSTRAINT DF_Producto_PublicId DEFAULT NEWID() FOR PublicId;
    ALTER TABLE dbo.Producto ADD CONSTRAINT UQ_Producto_PublicId UNIQUE (PublicId);
END
GO

-- =========================================================
-- MIGRACIÓN US-40: Auditoría de usuario
-- UsuarioCreacionId / UsuarioModificacionId (FK a Usuario,
-- nullable: los registros anteriores quedan en NULL).
-- =========================================================
IF COL_LENGTH('dbo.Categoria', 'UsuarioCreacionId') IS NULL
    ALTER TABLE dbo.Categoria ADD UsuarioCreacionId INT NULL;
IF COL_LENGTH('dbo.Categoria', 'UsuarioModificacionId') IS NULL
    ALTER TABLE dbo.Categoria ADD UsuarioModificacionId INT NULL;

IF COL_LENGTH('dbo.Marca', 'UsuarioCreacionId') IS NULL
    ALTER TABLE dbo.Marca ADD UsuarioCreacionId INT NULL;
IF COL_LENGTH('dbo.Marca', 'UsuarioModificacionId') IS NULL
    ALTER TABLE dbo.Marca ADD UsuarioModificacionId INT NULL;

IF COL_LENGTH('dbo.Producto', 'UsuarioCreacionId') IS NULL
    ALTER TABLE dbo.Producto ADD UsuarioCreacionId INT NULL;
IF COL_LENGTH('dbo.Producto', 'UsuarioModificacionId') IS NULL
    ALTER TABLE dbo.Producto ADD UsuarioModificacionId INT NULL;

IF COL_LENGTH('dbo.HistoricoCostoProducto', 'UsuarioCreacionId') IS NULL
    ALTER TABLE dbo.HistoricoCostoProducto ADD UsuarioCreacionId INT NULL;
IF COL_LENGTH('dbo.HistoricoCostoProducto', 'UsuarioModificacionId') IS NULL
    ALTER TABLE dbo.HistoricoCostoProducto ADD UsuarioModificacionId INT NULL;
GO

IF OBJECT_ID('dbo.FK_Categoria_UsuarioCreacion', 'F') IS NULL
    ALTER TABLE dbo.Categoria ADD CONSTRAINT FK_Categoria_UsuarioCreacion
        FOREIGN KEY (UsuarioCreacionId) REFERENCES dbo.Usuario (UsuarioId);
IF OBJECT_ID('dbo.FK_Categoria_UsuarioModificacion', 'F') IS NULL
    ALTER TABLE dbo.Categoria ADD CONSTRAINT FK_Categoria_UsuarioModificacion
        FOREIGN KEY (UsuarioModificacionId) REFERENCES dbo.Usuario (UsuarioId);

IF OBJECT_ID('dbo.FK_Marca_UsuarioCreacion', 'F') IS NULL
    ALTER TABLE dbo.Marca ADD CONSTRAINT FK_Marca_UsuarioCreacion
        FOREIGN KEY (UsuarioCreacionId) REFERENCES dbo.Usuario (UsuarioId);
IF OBJECT_ID('dbo.FK_Marca_UsuarioModificacion', 'F') IS NULL
    ALTER TABLE dbo.Marca ADD CONSTRAINT FK_Marca_UsuarioModificacion
        FOREIGN KEY (UsuarioModificacionId) REFERENCES dbo.Usuario (UsuarioId);

IF OBJECT_ID('dbo.FK_Producto_UsuarioCreacion', 'F') IS NULL
    ALTER TABLE dbo.Producto ADD CONSTRAINT FK_Producto_UsuarioCreacion
        FOREIGN KEY (UsuarioCreacionId) REFERENCES dbo.Usuario (UsuarioId);
IF OBJECT_ID('dbo.FK_Producto_UsuarioModificacion', 'F') IS NULL
    ALTER TABLE dbo.Producto ADD CONSTRAINT FK_Producto_UsuarioModificacion
        FOREIGN KEY (UsuarioModificacionId) REFERENCES dbo.Usuario (UsuarioId);

IF OBJECT_ID('dbo.FK_Historico_UsuarioCreacion', 'F') IS NULL
    ALTER TABLE dbo.HistoricoCostoProducto ADD CONSTRAINT FK_Historico_UsuarioCreacion
        FOREIGN KEY (UsuarioCreacionId) REFERENCES dbo.Usuario (UsuarioId);
IF OBJECT_ID('dbo.FK_Historico_UsuarioModificacion', 'F') IS NULL
    ALTER TABLE dbo.HistoricoCostoProducto ADD CONSTRAINT FK_Historico_UsuarioModificacion
        FOREIGN KEY (UsuarioModificacionId) REFERENCES dbo.Usuario (UsuarioId);
GO
