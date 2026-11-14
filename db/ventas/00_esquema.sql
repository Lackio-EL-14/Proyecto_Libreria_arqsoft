-- =========================================================
-- MS Ventas · SQL Server · LibreriaVentas (US-111)
-- Esquema inicial de la sección 5 del planning. Idempotente.
-- Las referencias a Clientes, Productos y Usuarios son PublicId
-- (GUID) sin FK; los datos que la venta muestra se copian.
-- =========================================================

IF DB_ID('LibreriaVentas') IS NULL
BEGIN
    CREATE DATABASE LibreriaVentas;
END
GO

USE LibreriaVentas;
GO

-- ---------------------------------------------------------
-- Venta
-- ---------------------------------------------------------
IF OBJECT_ID('dbo.Venta', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Venta (
        VentaId                INT IDENTITY(1,1) PRIMARY KEY,
        PublicId               UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_Venta_PublicId DEFAULT NEWID(),

        -- Cliente (MS Clientes): referencia + copia al registrar.
        ClientePublicId        UNIQUEIDENTIFIER NOT NULL,
        ClienteCiNit           NVARCHAR(30) NOT NULL,
        ClienteRazonSocial     NVARCHAR(200) NOT NULL,

        Estado                 NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Venta_Estado DEFAULT (N'Activa'),
        Total                  DECIMAL(18,2) NOT NULL
            CONSTRAINT DF_Venta_Total DEFAULT (0),
        Observacion            NVARCHAR(250) NULL,
        MotivoAnulacion        NVARCHAR(250) NULL,
        VendedorIniciales      NVARCHAR(15) NULL,

        -- Auditoría: `sub` del JWT (PublicId del usuario en MS Usuarios).
        FechaCreacion          DATETIME2 NOT NULL
            CONSTRAINT DF_Venta_FechaCreacion DEFAULT (SYSDATETIME()),
        UsuarioCreacionId      UNIQUEIDENTIFIER NOT NULL,
        FechaModificacion      DATETIME2 NULL,
        UsuarioModificacionId  UNIQUEIDENTIFIER NULL,
        FechaAnulacion         DATETIME2 NULL,
        UsuarioAnulacionId     UNIQUEIDENTIFIER NULL,

        CONSTRAINT UQ_Venta_PublicId UNIQUE (PublicId),
        CONSTRAINT CK_Venta_Estado
            CHECK (Estado IN (N'Activa', N'Anulada')),
        CONSTRAINT CK_Venta_Total
            CHECK (Total >= 0),
        CONSTRAINT CK_Venta_MotivoAnulacion_Longitud
            CHECK (MotivoAnulacion IS NULL
                   OR LEN(LTRIM(RTRIM(MotivoAnulacion))) BETWEEN 10 AND 250)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_Venta_FechaCreacion' AND object_id = OBJECT_ID('dbo.Venta'))
    CREATE INDEX IX_Venta_FechaCreacion ON dbo.Venta (FechaCreacion DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_Venta_ClientePublicId' AND object_id = OBJECT_ID('dbo.Venta'))
    CREATE INDEX IX_Venta_ClientePublicId ON dbo.Venta (ClientePublicId);
GO

-- ---------------------------------------------------------
-- DetalleVenta
-- ---------------------------------------------------------
IF OBJECT_ID('dbo.DetalleVenta', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DetalleVenta (
        DetalleVentaId           INT IDENTITY(1,1) PRIMARY KEY,
        VentaId                  INT NOT NULL,

        -- Producto (MS Productos): referencia + copia del nombre.
        ProductoPublicId         UNIQUEIDENTIFIER NOT NULL,
        ProductoNombre           NVARCHAR(150) NOT NULL,

        Cantidad                 INT NOT NULL,
        PrecioUnitarioVenta      DECIMAL(10,2) NOT NULL,
        CostoAdquisicionUnitario DECIMAL(10,2) NOT NULL,
        Importe                  DECIMAL(10,2) NOT NULL,
        Ganancia                 DECIMAL(10,2) NOT NULL,

        CONSTRAINT FK_DetalleVenta_Venta FOREIGN KEY (VentaId)
            REFERENCES dbo.Venta (VentaId),
        CONSTRAINT CK_DetalleVenta_Cantidad CHECK (Cantidad > 0),
        CONSTRAINT CK_DetalleVenta_Precio   CHECK (PrecioUnitarioVenta >= 0),
        CONSTRAINT CK_DetalleVenta_Costo    CHECK (CostoAdquisicionUnitario >= 0),
        CONSTRAINT CK_DetalleVenta_Importe  CHECK (Importe >= 0)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_DetalleVenta_VentaId' AND object_id = OBJECT_ID('dbo.DetalleVenta'))
    CREATE INDEX IX_DetalleVenta_VentaId ON dbo.DetalleVenta (VentaId);
GO

-- ---------------------------------------------------------
-- HistoricoMovimientoStock
-- Vive en Ventas para entrar en la misma transacción local
-- que Venta y DetalleVenta.
-- ---------------------------------------------------------
IF OBJECT_ID('dbo.HistoricoMovimientoStock', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HistoricoMovimientoStock (
        MovimientoId      INT IDENTITY(1,1) PRIMARY KEY,
        VentaId           INT NOT NULL,
        ProductoPublicId  UNIQUEIDENTIFIER NOT NULL,
        Cantidad          INT NOT NULL,
        TipoMovimiento    NVARCHAR(20) NOT NULL,
        FechaCreacion     DATETIME2 NOT NULL
            CONSTRAINT DF_Movimiento_FechaCreacion DEFAULT (SYSDATETIME()),

        CONSTRAINT FK_Movimiento_Venta FOREIGN KEY (VentaId)
            REFERENCES dbo.Venta (VentaId),
        CONSTRAINT CK_Movimiento_Cantidad CHECK (Cantidad > 0),
        CONSTRAINT CK_Movimiento_Tipo
            CHECK (TipoMovimiento IN (N'Venta', N'Anulacion'))
    );
END
GO
