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
        Nombre            NVARCHAR(100) NOT NULL,
        Descripcion       NVARCHAR(255) NULL,
        Orden             INT NULL,
        Estado            BIT NOT NULL DEFAULT (1),
        FechaCreacion     DATETIME2 NOT NULL DEFAULT (SYSDATETIME()),
        FechaModificacion DATETIME2 NULL,
        CONSTRAINT UQ_Categoria_Nombre UNIQUE (Nombre)
    );
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
        PaisOrigen        NVARCHAR(100) NULL,
        Estado            BIT NOT NULL DEFAULT (1),
        FechaCreacion     DATETIME2 NOT NULL DEFAULT (SYSDATETIME()),
        FechaModificacion DATETIME2 NULL,
        CONSTRAINT UQ_Marca_Nombre UNIQUE (Nombre)
    );
END
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
        TipoCambioUsd     DECIMAL(10,4) NULL,
        Motivo            NVARCHAR(200) NULL, -- ej: 'Registro inicial', 'Edición manual', 'Compra a proveedor'
        FechaVigencia     DATETIME2 NOT NULL DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_Historico_Producto FOREIGN KEY (ProductoId)
            REFERENCES dbo.Producto (ProductoId),
        CONSTRAINT CK_Historico_Costo CHECK (CostoAdquisicion >= 0)
    );
END
GO

-- ---------------------------------------------------------
-- Datos semilla opcionales (se pueden borrar sin problema)
-- ---------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Categoria)
BEGIN
    INSERT INTO dbo.Categoria (Nombre, Descripcion, Orden) VALUES
        (N'Material escolar', N'Cuadernos, lápices, útiles en general', 1),
        (N'Libros', N'Libros de texto y literatura', 2),
        (N'Arte y manualidades', N'Pinturas, pinceles, acrílicos', 3);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Marca)
BEGIN
    INSERT INTO dbo.Marca (Nombre, Descripcion, PaisOrigen) VALUES
        (N'Acrilex', N'Pinturas y productos de arte', N'Brasil'),
        (N'Norma', N'Cuadernos y útiles escolares', N'Colombia'),
        (N'Genérico', N'Sin marca específica', NULL);
END
GO
