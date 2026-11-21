SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE LibreriaProveedores;
GO

IF NOT EXISTS(SELECT 1 FROM dbo.Proveedor WHERE PublicId='f0000000-0000-0000-0000-000000000001')
BEGIN
    INSERT dbo.Proveedor(PublicId,Nit,RazonSocial,Telefono,Correo,Estado,UsuarioCreacionId)
    VALUES('f0000000-0000-0000-0000-000000000001',N'1020304050',N'Distribuidora Escolar Andina',N'33600001',N'contacto@andina.bo',1,'a0000000-0000-0000-0000-000000000001');
    INSERT dbo.ContactoProveedor(ProveedorId,Nombre,Cargo,Telefono,Correo,EsPrincipal,UsuarioCreacionId)
    VALUES(SCOPE_IDENTITY(),N'Ana Pérez',N'Ejecutiva comercial',N'70010001',N'ana.perez@andina.bo',1,'a0000000-0000-0000-0000-000000000001');
END
GO

IF NOT EXISTS(SELECT 1 FROM dbo.Proveedor WHERE PublicId='f0000000-0000-0000-0000-000000000002')
BEGIN
    INSERT dbo.Proveedor(PublicId,Nit,RazonSocial,Telefono,Correo,Estado,UsuarioCreacionId)
    VALUES('f0000000-0000-0000-0000-000000000002',N'2040608011',N'Editorial del Sur S.R.L.',N'33400002',N'pedidos@editorialsur.bo',1,'a0000000-0000-0000-0000-000000000001');
    INSERT dbo.ContactoProveedor(ProveedorId,Nombre,Cargo,Telefono,Correo,EsPrincipal,UsuarioCreacionId)
    VALUES(SCOPE_IDENTITY(),N'Carlos Suárez',N'Encargado de pedidos',N'70020002',N'carlos@editorialsur.bo',1,'a0000000-0000-0000-0000-000000000001');
END
GO

IF NOT EXISTS(SELECT 1 FROM dbo.Proveedor WHERE PublicId='f0000000-0000-0000-0000-000000000003')
BEGIN
    INSERT dbo.Proveedor(PublicId,Nit,RazonSocial,Telefono,Correo,Estado,UsuarioCreacionId)
    VALUES('f0000000-0000-0000-0000-000000000003',N'3050709012',N'Importadora Técnica Nacional',N'33500003',N'contacto@itn.bo',1,'a0000000-0000-0000-0000-000000000001');
    INSERT dbo.ContactoProveedor(ProveedorId,Nombre,Cargo,Telefono,Correo,EsPrincipal,UsuarioCreacionId)
    VALUES(SCOPE_IDENTITY(),N'Lucía Rojas',N'Asesora comercial',N'70030003',N'lucia@itn.bo',1,'a0000000-0000-0000-0000-000000000001');
END
GO
