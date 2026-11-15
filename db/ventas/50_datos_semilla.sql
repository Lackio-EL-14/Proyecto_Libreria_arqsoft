-- =========================================================
-- MS Ventas · datos semilla (US-111). Idempotente.
-- Los GUID salen de docs/contratos/datos-semilla.md: clientes,
-- productos y usuarios existen con esos mismos PublicId en sus BD.
-- =========================================================

USE LibreriaVentas;
GO

-- Venta 1: activa, Universidad Mayor, registrada por admin.
IF NOT EXISTS (SELECT 1 FROM dbo.Venta WHERE PublicId = 'e0000000-0000-0000-0000-000000000001')
BEGIN
    BEGIN TRANSACTION;

    INSERT INTO dbo.Venta (PublicId, ClientePublicId, ClienteCiNit, ClienteRazonSocial,
                           Estado, Total, VendedorIniciales, FechaCreacion, UsuarioCreacionId)
    VALUES ('e0000000-0000-0000-0000-000000000001',
            'c0000000-0000-0000-0000-000000000001', N'9999999', N'Universidad Mayor',
            N'Activa', 135.00, N'A.S.', '2026-11-02T10:15:00',
            'a0000000-0000-0000-0000-000000000001');
    DECLARE @Venta1 INT = SCOPE_IDENTITY();

    INSERT INTO dbo.DetalleVenta (VentaId, ProductoPublicId, ProductoNombre, Cantidad,
                                  PrecioUnitarioVenta, CostoAdquisicionUnitario, Importe, Ganancia)
    VALUES (@Venta1, 'd0000000-0000-0000-0000-000000000001', N'Cuaderno Espiral 100 hojas', 1, 15.00, 10.00, 15.00, 5.00),
           (@Venta1, 'd0000000-0000-0000-0000-000000000003', N'Libro Matemáticas 5to Sec', 1, 120.00, 90.00, 120.00, 30.00);

    INSERT INTO dbo.HistoricoMovimientoStock (VentaId, ProductoPublicId, Cantidad, TipoMovimiento, FechaCreacion)
    VALUES (@Venta1, 'd0000000-0000-0000-0000-000000000001', 1, N'Venta', '2026-11-02T10:15:00'),
           (@Venta1, 'd0000000-0000-0000-0000-000000000003', 1, N'Venta', '2026-11-02T10:15:00');

    COMMIT TRANSACTION;
END
GO

-- Venta 2: activa, María Fernanda Quispe Choque, registrada por vendedor.
IF NOT EXISTS (SELECT 1 FROM dbo.Venta WHERE PublicId = 'e0000000-0000-0000-0000-000000000002')
BEGIN
    BEGIN TRANSACTION;

    INSERT INTO dbo.Venta (PublicId, ClientePublicId, ClienteCiNit, ClienteRazonSocial,
                           Estado, Total, Observacion, VendedorIniciales, FechaCreacion, UsuarioCreacionId)
    VALUES ('e0000000-0000-0000-0000-000000000002',
            'c0000000-0000-0000-0000-000000000002', N'4567890', N'María Fernanda Quispe Choque',
            N'Activa', 30.00, N'Pago en efectivo.', N'V.M.', '2026-11-03T16:40:00',
            'a0000000-0000-0000-0000-000000000002');
    DECLARE @Venta2 INT = SCOPE_IDENTITY();

    INSERT INTO dbo.DetalleVenta (VentaId, ProductoPublicId, ProductoNombre, Cantidad,
                                  PrecioUnitarioVenta, CostoAdquisicionUnitario, Importe, Ganancia)
    VALUES (@Venta2, 'd0000000-0000-0000-0000-000000000001', N'Cuaderno Espiral 100 hojas', 2, 15.00, 10.00, 30.00, 10.00);

    INSERT INTO dbo.HistoricoMovimientoStock (VentaId, ProductoPublicId, Cantidad, TipoMovimiento, FechaCreacion)
    VALUES (@Venta2, 'd0000000-0000-0000-0000-000000000001', 2, N'Venta', '2026-11-03T16:40:00');

    COMMIT TRANSACTION;
END
GO

-- Venta 3: anulada, Universidad Mayor; la registra vendedor y la anula admin.
IF NOT EXISTS (SELECT 1 FROM dbo.Venta WHERE PublicId = 'e0000000-0000-0000-0000-000000000003')
BEGIN
    BEGIN TRANSACTION;

    INSERT INTO dbo.Venta (PublicId, ClientePublicId, ClienteCiNit, ClienteRazonSocial,
                           Estado, Total, MotivoAnulacion, VendedorIniciales,
                           FechaCreacion, UsuarioCreacionId, FechaAnulacion, UsuarioAnulacionId)
    VALUES ('e0000000-0000-0000-0000-000000000003',
            'c0000000-0000-0000-0000-000000000001', N'9999999', N'Universidad Mayor',
            N'Anulada', 85.00, N'Error en caja al cobrar la venta.', N'V.M.',
            '2026-11-04T09:05:00', 'a0000000-0000-0000-0000-000000000002',
            '2026-11-04T09:30:00', 'a0000000-0000-0000-0000-000000000001');
    DECLARE @Venta3 INT = SCOPE_IDENTITY();

    INSERT INTO dbo.DetalleVenta (VentaId, ProductoPublicId, ProductoNombre, Cantidad,
                                  PrecioUnitarioVenta, CostoAdquisicionUnitario, Importe, Ganancia)
    VALUES (@Venta3, 'd0000000-0000-0000-0000-000000000005', N'Calculadora Científica', 1, 85.00, 60.00, 85.00, 25.00);

    INSERT INTO dbo.HistoricoMovimientoStock (VentaId, ProductoPublicId, Cantidad, TipoMovimiento, FechaCreacion)
    VALUES (@Venta3, 'd0000000-0000-0000-0000-000000000005', 1, N'Venta',     '2026-11-04T09:05:00'),
           (@Venta3, 'd0000000-0000-0000-0000-000000000005', 1, N'Anulacion', '2026-11-04T09:30:00');

    COMMIT TRANSACTION;
END
GO
