/*
    Corrige los cierres temporales existentes para usar el margen real:
    capital = ventas / 1.61
    ganancia personal = ventas - capital

    Conserva las ventas, fechas y responsables; solo recalcula la distribución.
    Ejecutar una vez en DBSISTEMA_VENTA después de actualizar la aplicación.
*/
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE cierre
       SET CapitalAsignado = calculo.Capital,
           PersonalAsignado = cierre.Ventas - calculo.Capital
    FROM dbo.FIN_CIERRE_TEMPORAL AS cierre
    CROSS APPLY
    (
        SELECT CONVERT(decimal(12,2), ROUND(cierre.Ventas / CONVERT(decimal(4,2), 1.61), 2)) AS Capital
    ) AS calculo;

    DECLARE @CierresActualizados int = @@ROWCOUNT;
    COMMIT TRANSACTION;

    SELECT @CierresActualizados AS CierresRecalculados,
           N'Capital = ventas / 1.61; ganancia = ventas - capital.' AS ReglaAplicada;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
