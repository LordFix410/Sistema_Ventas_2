/*
  Registra anulaciones y correcciones de depósitos temporales sin borrar historial.
  Ejecutar una sola vez en la misma base que usa la aplicación .NET.
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.FIN_DEPOSITO_TEMPORAL', N'U') IS NULL
    THROW 51201, 'No existe dbo.FIN_DEPOSITO_TEMPORAL. Ejecuta primero las migraciones de Finanzas.', 1;

IF COL_LENGTH(N'dbo.FIN_DEPOSITO_TEMPORAL', N'Anulado') IS NULL
BEGIN
    ALTER TABLE dbo.FIN_DEPOSITO_TEMPORAL
      ADD Anulado bit NOT NULL CONSTRAINT DF_FIN_DEPOSITO_TEMPORAL_Anulado DEFAULT(0) WITH VALUES;
END;

IF OBJECT_ID(N'dbo.FIN_DEPOSITO_TEMPORAL_AUDITORIA', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FIN_DEPOSITO_TEMPORAL_AUDITORIA(
        IdAuditoria int primary key identity(1,1),
        IdDepositoTemporal int not null references dbo.FIN_DEPOSITO_TEMPORAL(IdDepositoTemporal),
        Accion varchar(15) not null,
        CuentaAnterior varchar(15) not null,
        MontoAnterior decimal(12,2) not null,
        FechaAnterior date not null,
        ComprobanteAnterior varchar(100) not null,
        CuentaNueva varchar(15) not null,
        MontoNuevo decimal(12,2) not null,
        FechaNueva date not null,
        ComprobanteNuevo varchar(100) not null,
        Motivo varchar(250) not null,
        IdUsuario int null references dbo.USUARIO(IdUsuario),
        FechaRegistro datetime2(0) not null CONSTRAINT DF_FIN_DEPOSITO_TEMPORAL_AUDITORIA_FechaRegistro DEFAULT(sysdatetime()),
        CONSTRAINT CK_FIN_DEPOSITO_TEMPORAL_AUDITORIA_Accion CHECK(Accion IN ('Correccion','Anulacion')),
        CONSTRAINT CK_FIN_DEPOSITO_TEMPORAL_AUDITORIA_Montos CHECK(MontoAnterior > 0 AND MontoNuevo > 0),
        CONSTRAINT CK_FIN_DEPOSITO_TEMPORAL_AUDITORIA_Cuentas CHECK(CuentaAnterior IN ('Capital','Personal') AND CuentaNueva IN ('Capital','Personal'))
    );
    CREATE INDEX IX_FIN_DEPOSITO_TEMPORAL_AUDITORIA_Deposito ON dbo.FIN_DEPOSITO_TEMPORAL_AUDITORIA(IdDepositoTemporal,FechaRegistro);
END;

COMMIT TRANSACTION;
SELECT N'Migración aplicada: corrección y anulación de depósitos quedan auditadas.' AS Resultado;
