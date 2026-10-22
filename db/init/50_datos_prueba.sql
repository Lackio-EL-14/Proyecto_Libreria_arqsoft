USE LibreriaDb;
GO

-- 1. Insertar Cliente
IF NOT EXISTS (SELECT 1 FROM Cliente WHERE CiNit = '9999999')
BEGIN
    INSERT INTO Cliente (CiNit, RazonSocial, Correo) 
    VALUES ('9999999', 'Universidad Mayor', 'compras@um.edu.bo');
END
GO

-- 2. Garantizar Categorías y Marcas (Validando por NOMBRE para evitar violación de UNIQUE KEY)
IF NOT EXISTS (SELECT 1 FROM Categoria WHERE Nombre = 'Material escolar')
BEGIN
    INSERT INTO Categoria (PublicId, Codigo, Nombre, Descripcion, Ubicacion, Estado, FechaCreacion) 
    VALUES (NEWID(), 'CAT-PAP', 'Material escolar', 'Lápices, cuadernos, gomas', 'Estante principal', 1, GETUTCDATE());
END

IF NOT EXISTS (SELECT 1 FROM Categoria WHERE Nombre = 'Libros de Texto')
BEGIN
    INSERT INTO Categoria (PublicId, Codigo, Nombre, Descripcion, Ubicacion, Estado, FechaCreacion) 
    VALUES (NEWID(), 'CAT-LIB', 'Libros de Texto', 'Secundaria y superior', 'Bodega', 1, GETUTCDATE());
END

IF NOT EXISTS (SELECT 1 FROM Marca WHERE Nombre = 'Faber-Castell')
BEGIN
    INSERT INTO Marca (PublicId, Nombre, Descripcion, PaisOrigen, SitioWeb, Estado, FechaCreacion) 
    VALUES (NEWID(), 'Faber-Castell', 'Útiles escolares', 'Alemania', 'www.faber.com', 1, GETUTCDATE());
END

IF NOT EXISTS (SELECT 1 FROM Marca WHERE Nombre = 'Editorial Santillana')
BEGIN
    INSERT INTO Marca (PublicId, Nombre, Descripcion, PaisOrigen, SitioWeb, Estado, FechaCreacion) 
    VALUES (NEWID(), 'Editorial Santillana', 'Textos educativos', 'España', 'www.santillana.com', 1, GETUTCDATE());
END
GO

-- 3. Insertar 5 Productos (Validando por NOMBRE)
IF NOT EXISTS (SELECT 1 FROM Producto WHERE Nombre = 'Cuaderno Espiral 100 hojas')
BEGIN
    DECLARE @CatPap INT = (SELECT TOP 1 CategoriaId FROM Categoria WHERE Nombre = 'Material escolar');
    DECLARE @CatLib INT = (SELECT TOP 1 CategoriaId FROM Categoria WHERE Nombre = 'Libros de Texto');
    DECLARE @MarFab INT = (SELECT TOP 1 MarcaId FROM Marca WHERE Nombre = 'Faber-Castell');
    DECLARE @MarSan INT = (SELECT TOP 1 MarcaId FROM Marca WHERE Nombre = 'Editorial Santillana');
    DECLARE @AdminId INT = (SELECT TOP 1 UsuarioId FROM Usuario);

    INSERT INTO Producto (PublicId, Nombre, DescripcionEspecifica, EsPerecedero, Stock, PrecioVenta, CostoAdquisicionActual, CategoriaId, MarcaId, Estado, FechaCreacion, UsuarioCreacionId)
    VALUES 
    (NEWID(), 'Cuaderno Espiral 100 hojas', 'Cuadriculado', 0, 150, 15.00, 10.00, @CatPap, @MarFab, 1, GETUTCDATE(), @AdminId),
    (NEWID(), 'Caja Bolígrafos Azules x12', 'Fino', 0, 80, 25.00, 18.00, @CatPap, @MarFab, 1, GETUTCDATE(), @AdminId),
    (NEWID(), 'Libro Matemáticas 5to Sec', 'Álgebra', 0, 40, 120.00, 90.00, @CatLib, @MarSan, 1, GETUTCDATE(), @AdminId),
    (NEWID(), 'Historia Universal 2026', 'Actualizado', 0, 25, 180.00, 130.00, @CatLib, @MarSan, 1, GETUTCDATE(), @AdminId),
    (NEWID(), 'Calculadora Científica', 'Básica', 0, 30, 85.00, 60.00, @CatPap, @MarFab, 1, GETUTCDATE(), @AdminId);
END
GO

-- 4. Insertar 3 Ventas (2 Activas, 1 Anulada)
IF NOT EXISTS (SELECT 1 FROM Venta WHERE Total = 135.00)
BEGIN
    DECLARE @ClienteId INT = (SELECT TOP 1 ClienteId FROM Cliente WHERE CiNit = '9999999');
    DECLARE @AdminId INT = (SELECT TOP 1 UsuarioId FROM Usuario);
    
    DECLARE @Prod1 INT = (SELECT TOP 1 ProductoId FROM Producto WHERE Nombre = 'Cuaderno Espiral 100 hojas');
    DECLARE @Prod2 INT = (SELECT TOP 1 ProductoId FROM Producto WHERE Nombre = 'Libro Matemáticas 5to Sec');
    DECLARE @Prod3 INT = (SELECT TOP 1 ProductoId FROM Producto WHERE Nombre = 'Calculadora Científica');

    IF @Prod1 IS NOT NULL AND @Prod2 IS NOT NULL AND @Prod3 IS NOT NULL
    BEGIN
        -- Venta 1: Normal (1 Cuaderno, 1 Libro)
        INSERT INTO Venta (PublicId, ClienteId, Estado, FechaCreacion, UsuarioCreacionId, Total)
        VALUES (NEWID(), @ClienteId, 'Activa', GETUTCDATE(), @AdminId, 135.00);
        DECLARE @Venta1 INT = SCOPE_IDENTITY();
        
        INSERT INTO DetalleVenta (VentaId, ProductoId, Cantidad, PrecioUnitarioVenta, CostoAdquisicionUnitario, Importe, Ganancia)
        VALUES 
        (@Venta1, @Prod1, 1, 15.00, 10.00, 15.00, 5.00),
        (@Venta1, @Prod2, 1, 120.00, 90.00, 120.00, 30.00);

        -- Venta 2: Normal
        INSERT INTO Venta (PublicId, ClienteId, Estado, FechaCreacion, UsuarioCreacionId, Total)
        VALUES (NEWID(), @ClienteId, 'Activa', GETUTCDATE(), @AdminId, 30.00);
        DECLARE @Venta2 INT = SCOPE_IDENTITY();
        
        INSERT INTO DetalleVenta (VentaId, ProductoId, Cantidad, PrecioUnitarioVenta, CostoAdquisicionUnitario, Importe, Ganancia)
        VALUES (@Venta2, @Prod1, 2, 15.00, 10.00, 30.00, 10.00);

        -- Venta 3: Anulada (Para demostrar US-75)
        INSERT INTO Venta (PublicId, ClienteId, Estado, FechaCreacion, FechaAnulacion, UsuarioCreacionId, UsuarioAnulacionId, Total, MotivoAnulacion)
        VALUES (NEWID(), @ClienteId, 'Anulada', GETUTCDATE(), GETUTCDATE(), @AdminId, @AdminId, 85.00, 'Error en caja');
        DECLARE @Venta3 INT = SCOPE_IDENTITY();
        
        INSERT INTO DetalleVenta (VentaId, ProductoId, Cantidad, PrecioUnitarioVenta, CostoAdquisicionUnitario, Importe, Ganancia)
        VALUES (@Venta3, @Prod3, 1, 85.00, 60.00, 85.00, 25.00);
    END
END
GO
