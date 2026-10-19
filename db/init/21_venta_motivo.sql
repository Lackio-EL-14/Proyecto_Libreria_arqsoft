USE LibreriaDb;
GO

-- =========================================================
-- US-75: Motivo de anulación de venta
-- Regla definida: entre 10 y 250 caracteres.
-- =========================================================

IF COL_LENGTH('dbo.Venta', 'MotivoAnulacion') IS NULL
BEGIN
    ALTER TABLE dbo.Venta
    ADD MotivoAnulacion NVARCHAR(250) NULL;
END
GO

IF OBJECT_ID('dbo.CK_Venta_MotivoAnulacion_Longitud', 'C') IS NULL
BEGIN
    ALTER TABLE dbo.Venta
    ADD CONSTRAINT CK_Venta_MotivoAnulacion_Longitud
    CHECK (
        MotivoAnulacion IS NULL
        OR LEN(LTRIM(RTRIM(MotivoAnulacion))) BETWEEN 10 AND 250
    );
END
GO