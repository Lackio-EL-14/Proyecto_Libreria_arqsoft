USE LibreriaDb;
GO

-- =========================================================
-- US-74: Total persistido en Venta
-- =========================================================

IF COL_LENGTH('dbo.Venta', 'Total') IS NULL
BEGIN
    ALTER TABLE dbo.Venta
    ADD Total DECIMAL(18,2) NULL;
END
GO

-- Rellenar ventas existentes usando el importe de sus líneas.
UPDATE v
SET Total = ISNULL(det.Total, 0)
FROM dbo.Venta v
OUTER APPLY (
    SELECT SUM(d.Importe) AS Total
    FROM dbo.DetalleVenta d
    WHERE d.VentaId = v.VentaId
) det
WHERE v.Total IS NULL;
GO

-- Después de migrar los datos, el total pasa a ser obligatorio.
IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Venta')
      AND name = 'Total'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE dbo.Venta
    ALTER COLUMN Total DECIMAL(18,2) NOT NULL;
END
GO

-- Valor seguro para cualquier inserción externa que no lo especifique.
IF OBJECT_ID('dbo.DF_Venta_Total', 'D') IS NULL
BEGIN
    ALTER TABLE dbo.Venta
    ADD CONSTRAINT DF_Venta_Total
        DEFAULT (0) FOR Total;
END
GO