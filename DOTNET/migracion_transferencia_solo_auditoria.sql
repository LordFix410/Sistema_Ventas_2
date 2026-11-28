/* Agrega la marca para registrar transferencias ya reflejadas en saldos.
   No modifica saldos, transferencias ni movimientos existentes.
   Ejecutar una vez (se puede repetir) en DBSISTEMA_VENTA. */
USE [DBSISTEMA_VENTA];
GO

SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.FIN_TRANSFERENCIA_BANCARIA', N'U') IS NULL
    BEGIN
        ;THROW 51501, N'No existe dbo.FIN_TRANSFERENCIA_BANCARIA. Aplica primero la migración de finanzas web.', 1;
    END;

    IF COL_LENGTH(N'dbo.FIN_TRANSFERENCIA_BANCARIA', N'AfectaSaldo') IS NULL
        ALTER TABLE dbo.FIN_TRANSFERENCIA_BANCARIA
            ADD AfectaSaldo bit NOT NULL
                CONSTRAINT DF_FIN_TRANSFERENCIA_BANCARIA_AfectaSaldo DEFAULT(1) WITH VALUES;

    COMMIT TRANSACTION;
    SELECT N'Migración aplicada. Las transferencias existentes siguen afectando saldos; las nuevas podrán guardarse solo para auditoría.' AS Resultado;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
