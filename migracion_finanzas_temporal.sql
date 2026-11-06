/*
  Finanzas temporal web: registrar ventas diarias manuales, depósitos por cuenta
  y auditar salidas sin cargar compras ni modificar inventario.
  Ejecutar una sola vez en DBSISTEMA_VENTA con usuario administrador.
*/
USE DBSISTEMA_VENTA;
GO

IF OBJECT_ID(N'dbo.FIN_CIERRE_TEMPORAL', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FIN_CIERRE_TEMPORAL
    (
        IdCierreTemporal int IDENTITY(1,1) NOT NULL CONSTRAINT PK_FIN_CIERRE_TEMPORAL PRIMARY KEY,
        FechaCierre datetime2(0) NOT NULL,
        Ventas decimal(12,2) NOT NULL,
        CapitalAsignado decimal(12,2) NOT NULL,
        PersonalAsignado decimal(12,2) NOT NULL,
        IdUsuario int NULL CONSTRAINT FK_FIN_CIERRE_TEMPORAL_USUARIO REFERENCES dbo.USUARIO(IdUsuario),
        FechaRegistro datetime2(0) NOT NULL CONSTRAINT DF_FIN_CIERRE_TEMPORAL_FechaRegistro DEFAULT(SYSDATETIME()),
        CONSTRAINT CK_FIN_CIERRE_TEMPORAL_Montos CHECK
          (Ventas >= 0 AND CapitalAsignado >= 0 AND PersonalAsignado >= 0 AND CapitalAsignado + PersonalAsignado = Ventas)
    );
END;
GO

IF OBJECT_ID(N'dbo.FIN_DEPOSITO_TEMPORAL', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FIN_DEPOSITO_TEMPORAL
    (
        IdDepositoTemporal int IDENTITY(1,1) NOT NULL CONSTRAINT PK_FIN_DEPOSITO_TEMPORAL PRIMARY KEY,
        Cuenta varchar(15) NOT NULL,
        Monto decimal(12,2) NOT NULL,
        Fecha date NOT NULL,
        Comprobante varchar(100) NOT NULL,
        IdUsuario int NULL CONSTRAINT FK_FIN_DEPOSITO_TEMPORAL_USUARIO REFERENCES dbo.USUARIO(IdUsuario),
        FechaRegistro datetime2(0) NOT NULL CONSTRAINT DF_FIN_DEPOSITO_TEMPORAL_FechaRegistro DEFAULT(SYSDATETIME()),
        CONSTRAINT CK_FIN_DEPOSITO_TEMPORAL_Cuenta CHECK(Cuenta IN ('Capital','Personal')),
        CONSTRAINT CK_FIN_DEPOSITO_TEMPORAL_Monto CHECK(Monto > 0)
    );
    CREATE INDEX IX_FIN_DEPOSITO_TEMPORAL_CuentaFecha ON dbo.FIN_DEPOSITO_TEMPORAL(Cuenta,Fecha);
END;
GO

PRINT N'Migración temporal de Finanzas aplicada. No se modificó inventario ni compras.';
GO
