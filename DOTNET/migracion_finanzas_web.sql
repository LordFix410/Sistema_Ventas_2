/*
    MIGRACIÓN DE FINANZAS WEB PARA UNA BASE DBSISTEMA_VENTA YA EXISTENTE.

    Úsala si la base se creó con query.sql de la raíz o con una versión anterior
    que no tiene FIN_CONCILIACION y los campos nuevos de cierres/depositos.
    Este archivo conserva los registros existentes y se puede ejecutar más de una vez.

    NO ejecutes DOTNET/query.sql sobre una base existente: ese archivo es para
    instalación limpia y empieza creando la base completa.
*/
USE [DBSISTEMA_VENTA];
GO

SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.USUARIO', N'U') IS NULL
       OR OBJECT_ID(N'dbo.FIN_CIERRE_TEMPORAL', N'U') IS NULL
       OR OBJECT_ID(N'dbo.FIN_MOVIMIENTO', N'U') IS NULL
       OR OBJECT_ID(N'dbo.PAGO_GASTO_PERSONAL', N'U') IS NULL
       OR OBJECT_ID(N'dbo.FIN_BANCO_TEMPORAL', N'U') IS NULL
       OR OBJECT_ID(N'dbo.FIN_DEPOSITO_LOTE', N'U') IS NULL
       OR OBJECT_ID(N'dbo.FIN_DEPOSITO_LOTE_CIERRE', N'U') IS NULL
       OR OBJECT_ID(N'dbo.FIN_TRANSFERENCIA_BANCARIA', N'U') IS NULL
    BEGIN
        ;THROW 51401, N'Faltan tablas base de Finanzas Web. No ejecutes el instalador completo sobre esta base; primero aplica las migraciones financieras anteriores.', 1;
    END;

    IF COL_LENGTH(N'dbo.FIN_CIERRE_TEMPORAL', N'SalidasCaja') IS NULL
       OR COL_LENGTH(N'dbo.FIN_CIERRE_TEMPORAL', N'FechaCierre') IS NULL
       OR COL_LENGTH(N'dbo.FIN_MOVIMIENTO', N'PagadoDesdeCaja') IS NULL
       OR COL_LENGTH(N'dbo.FIN_MOVIMIENTO', N'CuentaOrigen') IS NULL
       OR COL_LENGTH(N'dbo.FIN_MOVIMIENTO', N'FechaRegistro') IS NULL
       OR COL_LENGTH(N'dbo.FIN_MOVIMIENTO', N'IdBanco') IS NULL
       OR COL_LENGTH(N'dbo.PAGO_GASTO_PERSONAL', N'PagadoDesdeCaja') IS NULL
       OR COL_LENGTH(N'dbo.PAGO_GASTO_PERSONAL', N'CuentaOrigen') IS NULL
       OR COL_LENGTH(N'dbo.PAGO_GASTO_PERSONAL', N'FechaRegistro') IS NULL
       OR COL_LENGTH(N'dbo.PAGO_GASTO_PERSONAL', N'IdBanco') IS NULL
       OR COL_LENGTH(N'dbo.FIN_BANCO_TEMPORAL', N'SaldoCapitalInicial') IS NULL
       OR COL_LENGTH(N'dbo.FIN_BANCO_TEMPORAL', N'SaldoGananciaInicial') IS NULL
    BEGIN
        ;THROW 51402, N'La base necesita primero las migraciones anteriores de cierres y salidas de Finanzas Web.', 1;
    END;

    IF COL_LENGTH(N'dbo.FIN_MOVIMIENTO', N'Anulado') IS NULL
        ALTER TABLE dbo.FIN_MOVIMIENTO ADD Anulado bit NOT NULL CONSTRAINT DF_FIN_MOVIMIENTO_Anulado DEFAULT(0) WITH VALUES;
    IF COL_LENGTH(N'dbo.PAGO_GASTO_PERSONAL', N'Anulado') IS NULL
        ALTER TABLE dbo.PAGO_GASTO_PERSONAL ADD Anulado bit NOT NULL CONSTRAINT DF_PAGO_GASTO_PERSONAL_Anulado DEFAULT(0) WITH VALUES;
    IF COL_LENGTH(N'dbo.FIN_MOVIMIENTO', N'OrigenEfectivo') IS NULL
        ALTER TABLE dbo.FIN_MOVIMIENTO ADD OrigenEfectivo varchar(20) NULL;
    IF COL_LENGTH(N'dbo.PAGO_GASTO_PERSONAL', N'OrigenEfectivo') IS NULL
        ALTER TABLE dbo.PAGO_GASTO_PERSONAL ADD OrigenEfectivo varchar(20) NULL;

    DECLARE @nuevaSalidaCapital bit = 0, @nuevaSalidaGanancia bit = 0;
    DECLARE @nuevoCapitalDepositado bit = 0, @nuevaGananciaDepositada bit = 0;

    IF COL_LENGTH(N'dbo.FIN_CIERRE_TEMPORAL', N'SalidasCapitalCaja') IS NULL
    BEGIN
        ALTER TABLE dbo.FIN_CIERRE_TEMPORAL ADD SalidasCapitalCaja decimal(12,2) NOT NULL
            CONSTRAINT DF_FIN_CIERRE_TEMPORAL_SalidasCapitalCaja DEFAULT(0) WITH VALUES;
        SET @nuevaSalidaCapital = 1;
    END;
    IF COL_LENGTH(N'dbo.FIN_CIERRE_TEMPORAL', N'SalidasGananciaCaja') IS NULL
    BEGIN
        ALTER TABLE dbo.FIN_CIERRE_TEMPORAL ADD SalidasGananciaCaja decimal(12,2) NOT NULL
            CONSTRAINT DF_FIN_CIERRE_TEMPORAL_SalidasGananciaCaja DEFAULT(0) WITH VALUES;
        SET @nuevaSalidaGanancia = 1;
    END;
    IF COL_LENGTH(N'dbo.FIN_CIERRE_TEMPORAL', N'Conciliado') IS NULL
        ALTER TABLE dbo.FIN_CIERRE_TEMPORAL ADD Conciliado bit NOT NULL
            CONSTRAINT DF_FIN_CIERRE_TEMPORAL_Conciliado DEFAULT(0) WITH VALUES;

    /* Asigna a cierres antiguos las salidas de caja según su fondo registrado. */
    IF @nuevaSalidaCapital = 1 OR @nuevaSalidaGanancia = 1
    EXEC sys.sp_executesql N'
;WITH TodosLosCierres AS (
    SELECT c.IdCierreTemporal,c.FechaCierre,
           ISNULL(LAG(c.FechaCierre) OVER(ORDER BY c.FechaCierre,c.IdCierreTemporal),CONVERT(datetime2(0),CONVERT(date,c.FechaCierre))) FechaAnterior
    FROM dbo.FIN_CIERRE_TEMPORAL c
), Cierres AS (
    SELECT * FROM TodosLosCierres
    WHERE EXISTS(SELECT 1 FROM dbo.FIN_CIERRE_TEMPORAL c
                 WHERE c.IdCierreTemporal=TodosLosCierres.IdCierreTemporal
                   AND c.SalidasCaja>0 AND c.SalidasCapitalCaja=0 AND c.SalidasGananciaCaja=0)
), Desglose AS (
    SELECT c.IdCierreTemporal,
      CAST(ISNULL((SELECT SUM(m.Monto) FROM dbo.FIN_MOVIMIENTO m
                   WHERE m.PagadoDesdeCaja=1 AND (m.OrigenEfectivo=''Caja'' OR m.OrigenEfectivo IS NULL) AND m.Anulado=0
                     AND m.CuentaOrigen<>''Personal'' AND m.FechaRegistro>c.FechaAnterior AND m.FechaRegistro<=c.FechaCierre),0) AS decimal(12,2)) Capital,
      CAST(ISNULL((SELECT SUM(m.Monto) FROM dbo.FIN_MOVIMIENTO m
                   WHERE m.PagadoDesdeCaja=1 AND (m.OrigenEfectivo=''Caja'' OR m.OrigenEfectivo IS NULL) AND m.Anulado=0
                     AND m.CuentaOrigen=''Personal'' AND m.FechaRegistro>c.FechaAnterior AND m.FechaRegistro<=c.FechaCierre),0)
          +ISNULL((SELECT SUM(p.Monto) FROM dbo.PAGO_GASTO_PERSONAL p
                   WHERE p.PagadoDesdeCaja=1 AND (p.OrigenEfectivo=''Caja'' OR p.OrigenEfectivo IS NULL) AND p.Anulado=0
                     AND p.FechaRegistro>c.FechaAnterior AND p.FechaRegistro<=c.FechaCierre),0) AS decimal(12,2)) Ganancia
    FROM Cierres c
)
UPDATE c SET SalidasCapitalCaja=d.Capital,SalidasGananciaCaja=d.Ganancia
FROM dbo.FIN_CIERRE_TEMPORAL c JOIN Desglose d ON d.IdCierreTemporal=c.IdCierreTemporal;';

    IF COL_LENGTH(N'dbo.FIN_DEPOSITO_LOTE_CIERRE', N'CapitalDepositado') IS NULL
    BEGIN
        ALTER TABLE dbo.FIN_DEPOSITO_LOTE_CIERRE ADD CapitalDepositado decimal(12,2) NOT NULL
            CONSTRAINT DF_FIN_DEPOSITO_LOTE_CIERRE_CapitalDepositado DEFAULT(0) WITH VALUES;
        SET @nuevoCapitalDepositado = 1;
    END;
    IF COL_LENGTH(N'dbo.FIN_DEPOSITO_LOTE_CIERRE', N'GananciaDepositada') IS NULL
    BEGIN
        ALTER TABLE dbo.FIN_DEPOSITO_LOTE_CIERRE ADD GananciaDepositada decimal(12,2) NOT NULL
            CONSTRAINT DF_FIN_DEPOSITO_LOTE_CIERRE_GananciaDepositada DEFAULT(0) WITH VALUES;
        SET @nuevaGananciaDepositada = 1;
    END;

    /* Solo se rellenan los campos nuevos en esta ejecución; no se pisan ceros legítimos al repetirla. */
    IF @nuevoCapitalDepositado = 1 AND @nuevaGananciaDepositada = 1
        EXEC(N'UPDATE dbo.FIN_DEPOSITO_LOTE_CIERRE SET CapitalDepositado=Capital,GananciaDepositada=Ganancia');
    ELSE IF @nuevoCapitalDepositado = 1
        EXEC(N'UPDATE dbo.FIN_DEPOSITO_LOTE_CIERRE SET CapitalDepositado=Capital');
    ELSE IF @nuevaGananciaDepositada = 1
        EXEC(N'UPDATE dbo.FIN_DEPOSITO_LOTE_CIERRE SET GananciaDepositada=Ganancia');

    IF OBJECT_ID(N'dbo.FIN_CONCILIACION', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.FIN_CONCILIACION(
            IdConciliacion int IDENTITY(1,1) NOT NULL CONSTRAINT PK_FIN_CONCILIACION PRIMARY KEY,
            Fecha date NOT NULL,
            Motivo varchar(250) NOT NULL,
            IdUsuario int NULL CONSTRAINT FK_FIN_CONCILIACION_USUARIO REFERENCES dbo.USUARIO(IdUsuario),
            FechaRegistro datetime2(0) NOT NULL CONSTRAINT DF_FIN_CONCILIACION_FechaRegistro DEFAULT(sysdatetime())
        );
    END;

    IF OBJECT_ID(N'dbo.FIN_CONCILIACION_DETALLE', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.FIN_CONCILIACION_DETALLE(
            IdDetalle int IDENTITY(1,1) NOT NULL CONSTRAINT PK_FIN_CONCILIACION_DETALLE PRIMARY KEY,
            IdConciliacion int NOT NULL CONSTRAINT FK_FIN_CONCILIACION_DETALLE_CAB REFERENCES dbo.FIN_CONCILIACION(IdConciliacion),
            IdBanco int NULL CONSTRAINT FK_FIN_CONCILIACION_DETALLE_BANCO REFERENCES dbo.FIN_BANCO_TEMPORAL(IdBanco),
            Fondo varchar(12) NOT NULL,
            SaldoEsperado decimal(12,2) NOT NULL,
            SaldoReal decimal(12,2) NOT NULL,
            Ajuste decimal(12,2) NOT NULL,
            CONSTRAINT CK_FIN_CONCILIACION_DETALLE_Fondo CHECK(Fondo IN ('Capital','Ganancia')),
            CONSTRAINT CK_FIN_CONCILIACION_DETALLE_Ajuste CHECK(Ajuste=SaldoReal-SaldoEsperado)
        );
    END;

    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.FIN_CONCILIACION_DETALLE') AND name=N'UX_FIN_CONCILIACION_DETALLE_Banco')
        CREATE UNIQUE INDEX UX_FIN_CONCILIACION_DETALLE_Banco ON dbo.FIN_CONCILIACION_DETALLE(IdConciliacion,IdBanco,Fondo) WHERE IdBanco IS NOT NULL;
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.FIN_CONCILIACION_DETALLE') AND name=N'UX_FIN_CONCILIACION_DETALLE_Caja')
        CREATE UNIQUE INDEX UX_FIN_CONCILIACION_DETALLE_Caja ON dbo.FIN_CONCILIACION_DETALLE(IdConciliacion,Fondo) WHERE IdBanco IS NULL;

    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.FIN_MOVIMIENTO') AND name=N'CK_FIN_MOVIMIENTO_CuentaOrigen')
        EXEC(N'ALTER TABLE dbo.FIN_MOVIMIENTO DROP CONSTRAINT CK_FIN_MOVIMIENTO_CuentaOrigen;');
    EXEC(N'ALTER TABLE dbo.FIN_MOVIMIENTO ADD CONSTRAINT CK_FIN_MOVIMIENTO_CuentaOrigen CHECK(CuentaOrigen IN (''Capital'',''Personal'',''Caja''));');

    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.PAGO_GASTO_PERSONAL') AND name=N'CK_PAGO_GASTO_PERSONAL_CuentaOrigen')
        EXEC(N'ALTER TABLE dbo.PAGO_GASTO_PERSONAL DROP CONSTRAINT CK_PAGO_GASTO_PERSONAL_CuentaOrigen;');
    EXEC(N'ALTER TABLE dbo.PAGO_GASTO_PERSONAL ADD CONSTRAINT CK_PAGO_GASTO_PERSONAL_CuentaOrigen CHECK(CuentaOrigen IN (''Capital'',''Personal'',''Caja''));');

    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.FIN_MOVIMIENTO') AND name=N'CK_FIN_MOVIMIENTO_OrigenEfectivo')
        EXEC(N'ALTER TABLE dbo.FIN_MOVIMIENTO DROP CONSTRAINT CK_FIN_MOVIMIENTO_OrigenEfectivo;');
    EXEC(N'ALTER TABLE dbo.FIN_MOVIMIENTO ADD CONSTRAINT CK_FIN_MOVIMIENTO_OrigenEfectivo CHECK(OrigenEfectivo IS NULL OR OrigenEfectivo IN (''Caja'',''Capital'',''Ganancia''));');
    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.PAGO_GASTO_PERSONAL') AND name=N'CK_PAGO_GASTO_PERSONAL_OrigenEfectivo')
        EXEC(N'ALTER TABLE dbo.PAGO_GASTO_PERSONAL DROP CONSTRAINT CK_PAGO_GASTO_PERSONAL_OrigenEfectivo;');
    EXEC(N'ALTER TABLE dbo.PAGO_GASTO_PERSONAL ADD CONSTRAINT CK_PAGO_GASTO_PERSONAL_OrigenEfectivo CHECK(OrigenEfectivo IS NULL OR OrigenEfectivo IN (''Caja'',''Capital'',''Ganancia''));');

    EXEC(N'UPDATE dbo.FIN_MOVIMIENTO SET OrigenEfectivo=''Caja'' WHERE PagadoDesdeCaja=1 AND OrigenEfectivo IS NULL;');
    EXEC(N'UPDATE dbo.PAGO_GASTO_PERSONAL SET OrigenEfectivo=''Caja'' WHERE PagadoDesdeCaja=1 AND OrigenEfectivo IS NULL;');

    IF COL_LENGTH(N'dbo.FIN_TRANSFERENCIA_BANCARIA', N'AfectaSaldo') IS NULL
        ALTER TABLE dbo.FIN_TRANSFERENCIA_BANCARIA ADD AfectaSaldo bit NOT NULL CONSTRAINT DF_FIN_TRANSFERENCIA_BANCARIA_AfectaSaldo DEFAULT(1) WITH VALUES;

    COMMIT TRANSACTION;
    SELECT N'Migración financiera web aplicada. No se borraron cierres ni movimientos.' AS Resultado;
    SELECT
        CASE WHEN OBJECT_ID(N'dbo.FIN_CONCILIACION', N'U') IS NOT NULL THEN 1 ELSE 0 END AS TieneConciliacion,
        CASE WHEN OBJECT_ID(N'dbo.FIN_CONCILIACION_DETALLE', N'U') IS NOT NULL THEN 1 ELSE 0 END AS TieneDetalleConciliacion,
        CASE WHEN COL_LENGTH(N'dbo.FIN_TRANSFERENCIA_BANCARIA', N'AfectaSaldo') IS NOT NULL THEN 1 ELSE 0 END AS TieneMarcaSoloAuditoria,
        CASE WHEN COL_LENGTH(N'dbo.FIN_CIERRE_TEMPORAL', N'SalidasCapitalCaja') IS NOT NULL
                   AND COL_LENGTH(N'dbo.FIN_CIERRE_TEMPORAL', N'SalidasGananciaCaja') IS NOT NULL
                   AND COL_LENGTH(N'dbo.FIN_CIERRE_TEMPORAL', N'Conciliado') IS NOT NULL
             THEN 1 ELSE 0 END AS TieneCamposCierreWeb,
        CASE WHEN COL_LENGTH(N'dbo.FIN_DEPOSITO_LOTE_CIERRE', N'CapitalDepositado') IS NOT NULL
                   AND COL_LENGTH(N'dbo.FIN_DEPOSITO_LOTE_CIERRE', N'GananciaDepositada') IS NOT NULL
             THEN 1 ELSE 0 END AS TieneCamposDepositoNeto;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
