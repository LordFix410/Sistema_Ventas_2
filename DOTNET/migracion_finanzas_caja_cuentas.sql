/*
   Agrega conciliación de efectivo inicial/final y la cuenta real que cubrió
   cada salida temporal. No modifica montos ni distribuciones. Ejecutar una
   vez en DBSISTEMA_VENTA antes de publicar la nueva versión de la aplicación.
*/
USE [DBSISTEMA_VENTA];
GO

IF OBJECT_ID('dbo.FIN_CIERRE_TEMPORAL', 'U') IS NULL
    THROW 51200, 'No existe dbo.FIN_CIERRE_TEMPORAL en DBSISTEMA_VENTA. Ejecuta primero migracion_finanzas_temporal.sql en esta misma base.', 1;
IF OBJECT_ID('dbo.FIN_MOVIMIENTO', 'U') IS NULL
    THROW 51201, 'No existe dbo.FIN_MOVIMIENTO. Esta base no tiene el esquema financiero principal requerido.', 1;
IF OBJECT_ID('dbo.PAGO_GASTO_PERSONAL', 'U') IS NULL
    THROW 51202, 'No existe dbo.PAGO_GASTO_PERSONAL. Esta base no tiene el esquema financiero principal requerido.', 1;

SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.FIN_AJUSTE_CUENTA_TEMPORAL', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.FIN_AJUSTE_CUENTA_TEMPORAL(
            IdAjuste bigint primary key identity(1,1),
            Clave varchar(80) not null unique,
            Cuenta varchar(15) not null,
            Monto decimal(12,2) not null,
            Concepto varchar(250) not null,
            Fecha datetime2(0) not null constraint DF_FIN_AJUSTE_CUENTA_TEMPORAL_Fecha default(sysdatetime()),
            IdUsuario int null references dbo.USUARIO(IdUsuario),
            constraint CK_FIN_AJUSTE_CUENTA_TEMPORAL_Cuenta check(Cuenta in ('Capital','Personal')),
            constraint CK_FIN_AJUSTE_CUENTA_TEMPORAL_Monto check(Monto <> 0)
        );
    END;

    IF COL_LENGTH('dbo.FIN_CIERRE_TEMPORAL', 'EfectivoInicial') IS NULL
        ALTER TABLE dbo.FIN_CIERRE_TEMPORAL ADD EfectivoInicial decimal(12,2) NULL;
    IF COL_LENGTH('dbo.FIN_CIERRE_TEMPORAL', 'EfectivoContado') IS NULL
        ALTER TABLE dbo.FIN_CIERRE_TEMPORAL ADD EfectivoContado decimal(12,2) NULL;
    IF COL_LENGTH('dbo.FIN_CIERRE_TEMPORAL', 'SalidasCaja') IS NULL
        ALTER TABLE dbo.FIN_CIERRE_TEMPORAL ADD SalidasCaja decimal(12,2) NULL;
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.FIN_CIERRE_TEMPORAL') AND name = 'CK_FIN_CIERRE_TEMPORAL_Caja')
        EXEC(N'ALTER TABLE dbo.FIN_CIERRE_TEMPORAL DROP CONSTRAINT CK_FIN_CIERRE_TEMPORAL_Caja');
    EXEC(N'ALTER TABLE dbo.FIN_CIERRE_TEMPORAL ADD CONSTRAINT CK_FIN_CIERRE_TEMPORAL_Caja CHECK
            ((EfectivoInicial IS NULL AND EfectivoContado IS NULL AND SalidasCaja IS NULL) OR
             (EfectivoInicial IS NOT NULL AND EfectivoContado IS NOT NULL AND SalidasCaja IS NOT NULL AND
              EfectivoInicial >= 0 AND EfectivoContado >= 0 AND SalidasCaja >= 0 AND
              Ventas = EfectivoContado + SalidasCaja - EfectivoInicial))');

    IF COL_LENGTH('dbo.FIN_MOVIMIENTO', 'CuentaOrigen') IS NULL
        ALTER TABLE dbo.FIN_MOVIMIENTO ADD CuentaOrigen varchar(15) NOT NULL
            CONSTRAINT DF_FIN_MOVIMIENTO_CuentaOrigen DEFAULT ('Capital') WITH VALUES;
    IF COL_LENGTH('dbo.FIN_MOVIMIENTO', 'PagadoDesdeCaja') IS NULL
    BEGIN
        EXEC(N'ALTER TABLE dbo.FIN_MOVIMIENTO ADD PagadoDesdeCaja bit NULL');
        EXEC(N'UPDATE dbo.FIN_MOVIMIENTO SET PagadoDesdeCaja = CASE WHEN UPPER(LTRIM(RTRIM(MedioPago))) = ''EFECTIVO'' THEN 1 ELSE 0 END');
        EXEC(N'ALTER TABLE dbo.FIN_MOVIMIENTO ALTER COLUMN PagadoDesdeCaja bit NOT NULL');
        EXEC(N'ALTER TABLE dbo.FIN_MOVIMIENTO ADD CONSTRAINT DF_FIN_MOVIMIENTO_PagadoDesdeCaja DEFAULT (0) FOR PagadoDesdeCaja');
    END;
    IF COL_LENGTH('dbo.FIN_MOVIMIENTO', 'FechaRegistro') IS NULL
        ALTER TABLE dbo.FIN_MOVIMIENTO ADD FechaRegistro datetime2(0) NOT NULL
            CONSTRAINT DF_FIN_MOVIMIENTO_FechaRegistro DEFAULT (sysdatetime()) WITH VALUES;
    ELSE
    BEGIN
        EXEC(N'UPDATE dbo.FIN_MOVIMIENTO SET FechaRegistro = COALESCE(Fecha, SYSDATETIME()) WHERE FechaRegistro IS NULL');
        IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FIN_MOVIMIENTO') AND name = 'FechaRegistro' AND is_nullable = 1)
            EXEC(N'ALTER TABLE dbo.FIN_MOVIMIENTO ALTER COLUMN FechaRegistro datetime2(0) NOT NULL');
        IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID('dbo.FIN_MOVIMIENTO') AND name = 'DF_FIN_MOVIMIENTO_FechaRegistro')
            ALTER TABLE dbo.FIN_MOVIMIENTO ADD CONSTRAINT DF_FIN_MOVIMIENTO_FechaRegistro DEFAULT (sysdatetime()) FOR FechaRegistro;
    END;

    IF COL_LENGTH('dbo.PAGO_GASTO_PERSONAL', 'CuentaOrigen') IS NULL
        ALTER TABLE dbo.PAGO_GASTO_PERSONAL ADD CuentaOrigen varchar(15) NOT NULL
            CONSTRAINT DF_PAGO_GASTO_PERSONAL_CuentaOrigen DEFAULT ('Personal') WITH VALUES;
    IF COL_LENGTH('dbo.PAGO_GASTO_PERSONAL', 'PagadoDesdeCaja') IS NULL
    BEGIN
        EXEC(N'ALTER TABLE dbo.PAGO_GASTO_PERSONAL ADD PagadoDesdeCaja bit NULL');
        EXEC(N'UPDATE dbo.PAGO_GASTO_PERSONAL SET PagadoDesdeCaja = CASE WHEN UPPER(LTRIM(RTRIM(MedioPago))) = ''EFECTIVO'' THEN 1 ELSE 0 END');
        EXEC(N'ALTER TABLE dbo.PAGO_GASTO_PERSONAL ALTER COLUMN PagadoDesdeCaja bit NOT NULL');
        EXEC(N'ALTER TABLE dbo.PAGO_GASTO_PERSONAL ADD CONSTRAINT DF_PAGO_GASTO_PERSONAL_PagadoDesdeCaja DEFAULT (0) FOR PagadoDesdeCaja');
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_FIN_MOVIMIENTO_CuentaOrigen')
        EXEC(N'ALTER TABLE dbo.FIN_MOVIMIENTO ADD CONSTRAINT CK_FIN_MOVIMIENTO_CuentaOrigen CHECK (CuentaOrigen IN (''Capital'',''Personal''))');
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PAGO_GASTO_PERSONAL_CuentaOrigen')
        EXEC(N'ALTER TABLE dbo.PAGO_GASTO_PERSONAL ADD CONSTRAINT CK_PAGO_GASTO_PERSONAL_CuentaOrigen CHECK (CuentaOrigen IN (''Capital'',''Personal''))');
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.FIN_CIERRE_TEMPORAL') AND name = 'IX_FIN_CIERRE_TEMPORAL_Fecha')
        CREATE INDEX IX_FIN_CIERRE_TEMPORAL_Fecha ON dbo.FIN_CIERRE_TEMPORAL(FechaCierre);

    COMMIT TRANSACTION;
    SELECT N'Migración aplicada. Los cierres anteriores conservan sus montos y las columnas nuevas quedan vacías para el historial.' AS Resultado;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
