-- =========================================================
-- Esquema inicial: Sistema de inventario - Librería
-- Tablas: Categoria, Marca, Producto, HistoricoCostoProducto
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
