/*
  Reinicio de MOVIMIENTOS financieros de prueba.

  Conserva el catálogo GASTO_PERSONAL (Luz, Internet, Agua, etc.).
  Elimina cierres, depósitos, pagos y salidas registrados tanto por Forms
  como por Finanzas web; también borra las copias mensuales de obligaciones,
  que la aplicación vuelve a generar desde GASTO_PERSONAL.

  Seguridad: por defecto NO borra nada. Revisa el resumen y cambia
  @ConfirmarReset a 1 únicamente si quieres borrar todo ese historial.
  Haz un respaldo de DBSISTEMA_VENTA antes de confirmar.
*/
USE DBSISTEMA_VENTA;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ConfirmarReset bit = 0;

SELECT DB_NAME() AS BaseDeDatos,
       N'GASTO_PERSONAL (catálogo; se conserva)' AS Tabla,
       COUNT_BIG(*) AS Registros
FROM dbo.GASTO_PERSONAL
UNION ALL SELECT DB_NAME(), N'FIN_CIERRE_CAJA', COUNT_BIG(*) FROM dbo.FIN_CIERRE_CAJA
UNION ALL SELECT DB_NAME(), N'FIN_DEPOSITO', COUNT_BIG(*) FROM dbo.FIN_DEPOSITO
UNION ALL SELECT DB_NAME(), N'FIN_MOVIMIENTO', COUNT_BIG(*) FROM dbo.FIN_MOVIMIENTO
UNION ALL SELECT DB_NAME(), N'PAGO_GASTO_PERSONAL', COUNT_BIG(*) FROM dbo.PAGO_GASTO_PERSONAL
UNION ALL SELECT DB_NAME(), N'GASTO_PERSONAL_PERIODO', COUNT_BIG(*) FROM dbo.GASTO_PERSONAL_PERIODO
UNION ALL SELECT DB_NAME(), N'FIN_CIERRE_TEMPORAL', COUNT_BIG(*) FROM dbo.FIN_CIERRE_TEMPORAL
UNION ALL SELECT DB_NAME(), N'FIN_DEPOSITO_TEMPORAL', COUNT_BIG(*) FROM dbo.FIN_DEPOSITO_TEMPORAL;

IF @ConfirmarReset = 0
BEGIN
    PRINT N'Vista previa únicamente: no se borró ningún registro. Para confirmar, respalda la base y cambia @ConfirmarReset a 1.';
END
ELSE
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM dbo.FIN_DEPOSITO;
        DELETE FROM dbo.PAGO_GASTO_PERSONAL;
        DELETE FROM dbo.GASTO_PERSONAL_PERIODO;
        DELETE FROM dbo.FIN_MOVIMIENTO;
        DELETE FROM dbo.FIN_CIERRE_CAJA;
        DELETE FROM dbo.FIN_DEPOSITO_TEMPORAL;
        DELETE FROM dbo.FIN_CIERRE_TEMPORAL;

        COMMIT TRANSACTION;
        PRINT N'Reinicio completado. El catálogo GASTO_PERSONAL se conservó.';
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
