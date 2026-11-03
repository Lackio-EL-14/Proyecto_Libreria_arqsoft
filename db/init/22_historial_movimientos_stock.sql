
IF OBJECT_ID('dbo.HistoricoMovimientoStock', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HistoricoMovimientoStock (
        MovimientoId INT IDENTITY(1,1) PRIMARY KEY,
        VentaId INT NOT NULL,
        ProductoId INT NOT NULL,
        Cantidad INT NOT NULL,
        TipoMovimiento NVARCHAR(20) NOT NULL,
        FechaCreacion DATETIME2 NOT NULL
            DEFAULT SYSDATETIME(),

        CONSTRAINT FK_Movimiento_Venta
            FOREIGN KEY (VentaId) REFERENCES dbo.Venta(VentaId),

        CONSTRAINT FK_Movimiento_Producto
            FOREIGN KEY (ProductoId) REFERENCES dbo.Producto(ProductoId),

        CONSTRAINT CK_Movimiento_Cantidad
            CHECK (Cantidad > 0),

        CONSTRAINT CK_Movimiento_Tipo
            CHECK (TipoMovimiento IN (N'Venta', N'Anulacion'))
    );
END
GO
