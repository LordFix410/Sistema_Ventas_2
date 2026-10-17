/*
  Actualiza una DBSISTEMA_VENTA existente para el módulo financiero.
  Ejecutar en SSMS conectado a DBSISTEMA_VENTA como administrador.
  No elimina ni modifica ventas, compras, ni los gastos personales cargados.
*/
USE [DBSISTEMA_VENTA];
GO

IF EXISTS (SELECT 1 FROM dbo.ROL WHERE Descripcion='ADMINISTRADOR')
   AND NOT EXISTS (SELECT 1 FROM dbo.PERMISO p JOIN dbo.ROL r ON r.IdRol=p.IdRol
                   WHERE r.Descripcion='ADMINISTRADOR' AND p.NombreMenu='menuFinanzas')
    INSERT dbo.PERMISO(IdRol,NombreMenu)
    SELECT IdRol,'menuFinanzas' FROM dbo.ROL WHERE Descripcion='ADMINISTRADOR';

DELETE p FROM dbo.PERMISO p JOIN dbo.ROL r ON r.IdRol=p.IdRol
WHERE r.Descripcion='EMPLEADO' AND p.NombreMenu='menuFinanzas';
GO

IF OBJECT_ID(N'dbo.FIN_CIERRE_CAJA', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FIN_CIERRE_CAJA(
        IdCierre int primary key identity(1,1),
        IdUsuario int null references dbo.USUARIO(IdUsuario),
        FechaInicio datetime2(0) not null,
        FechaCierre datetime2(0) not null,
        FondoInicial decimal(12,2) not null,
        Ventas decimal(12,2) not null,
        CostoProductos decimal(12,2) not null,
        GananciaBruta decimal(12,2) not null,
        ReservaCapital decimal(12,2) not null,
        CapitalGenerado decimal(12,2) not null,
        GananciaGenerada decimal(12,2) not null,
        EfectivoEsperado decimal(12,2) not null,
        EfectivoContado decimal(12,2) not null,
        Diferencia decimal(12,2) not null,
        DescuentoGanancia decimal(12,2) not null,
        DescuentoCapital decimal(12,2) not null,
        Sobrante decimal(12,2) not null,
        Observaciones varchar(250) null,
        CONSTRAINT CK_FIN_CIERRE_Rango CHECK(FechaCierre > FechaInicio),
        CONSTRAINT CK_FIN_CIERRE_NoNegativos CHECK(FondoInicial >= 0 AND Ventas >= 0 AND CostoProductos >= 0 AND EfectivoContado >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.FIN_MOVIMIENTO', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FIN_MOVIMIENTO(
        IdMovimiento int primary key identity(1,1),
        Tipo varchar(20) not null,
        IdCompra int null references dbo.COMPRA(IdCompra),
        IdUsuario int null references dbo.USUARIO(IdUsuario),
        Fecha datetime2(0) not null CONSTRAINT DF_FIN_MOVIMIENTO_Fecha default(sysdatetime()),
        Concepto varchar(150) not null,
        Monto decimal(12,2) not null,
        MedioPago varchar(20) not null CONSTRAINT DF_FIN_MOVIMIENTO_Medio default('Efectivo'),
        Referencia varchar(100) null,
        CONSTRAINT CK_FIN_MOVIMIENTO_Tipo CHECK(Tipo in ('Empresa','Proveedor')),
        CONSTRAINT CK_FIN_MOVIMIENTO_Monto CHECK(Monto > 0),
        CONSTRAINT CK_FIN_MOVIMIENTO_Medio CHECK(MedioPago in ('Efectivo','Transferencia','Tarjeta','Otro'))
    );
END;
GO

IF OBJECT_ID(N'dbo.FIN_DEPOSITO', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FIN_DEPOSITO(
        IdDeposito int primary key identity(1,1),
        IdCierre int not null references dbo.FIN_CIERRE_CAJA(IdCierre),
        IdUsuario int null references dbo.USUARIO(IdUsuario),
        Fecha datetime2(0) not null CONSTRAINT DF_FIN_DEPOSITO_Fecha default(sysdatetime()),
        Capital decimal(12,2) not null CONSTRAINT DF_FIN_DEPOSITO_Capital default(0),
        Ganancia decimal(12,2) not null CONSTRAINT DF_FIN_DEPOSITO_Ganancia default(0),
        Referencia varchar(100) null,
        CONSTRAINT CK_FIN_DEPOSITO_Montos CHECK(Capital >= 0 AND Ganancia >= 0 AND Capital + Ganancia > 0)
    );
END;
GO

IF COL_LENGTH(N'dbo.FIN_DEPOSITO', N'IdUsuario') IS NULL
    ALTER TABLE dbo.FIN_DEPOSITO ADD IdUsuario int NULL
        CONSTRAINT FK_FIN_DEPOSITO_USUARIO REFERENCES dbo.USUARIO(IdUsuario);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.FIN_CIERRE_CAJA') AND name=N'IX_FIN_CIERRE_Fecha')
    CREATE INDEX IX_FIN_CIERRE_Fecha ON dbo.FIN_CIERRE_CAJA(FechaCierre);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.FIN_MOVIMIENTO') AND name=N'IX_FIN_MOVIMIENTO_Fecha')
    CREATE INDEX IX_FIN_MOVIMIENTO_Fecha ON dbo.FIN_MOVIMIENTO(Fecha);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.FIN_MOVIMIENTO') AND name=N'IX_FIN_MOVIMIENTO_Compra')
    CREATE INDEX IX_FIN_MOVIMIENTO_Compra ON dbo.FIN_MOVIMIENTO(IdCompra);
GO

IF USER_ID(N'ventas_app2') IS NOT NULL
BEGIN
    GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO [ventas_app2];
    GRANT EXECUTE ON SCHEMA::dbo TO [ventas_app2];
END;
GO
