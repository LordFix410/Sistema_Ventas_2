/*
    INSTALACIÓN LIMPIA: ejecutar este único archivo en SQL Server Management Studio.
    Crea DBSISTEMA_VENTA, estructura, procedimientos y datos iniciales.
    No lo ejecutes sobre una base ya creada ni contiene DROP DATABASE.
    Incluye las tablas de obligaciones personales y permisos de Finanzas.
    Al final crea o actualiza el login de aplicación ventas_app2.
    Ejecutar con una cuenta administradora del servidor SQL.
    Para una base ya existente ejecuta migracion_finanzas.sql y migracion_reposicion_cotizaciones.sql según corresponda.
*/
create database DBSISTEMA_VENTA
go

USE DBSISTEMA_VENTA
go

-- =============================================
-- CREACIÓN DE TABLAS
-- =============================================

create table ROL(
IdRol int primary key identity,
Descripcion varchar(50),
FechaRegistro datetime default getdate()
)
go

create table PERMISO(
IdPermiso int primary key identity,
IdRol int references ROL(IdRol),
NombreMenu varchar(100),
FechaRegistro datetime default getdate()
)
go

create table PROVEEDOR(
IdProveedor int primary key identity,
Documento varchar(50),
RazonSocial varchar(50),
Correo varchar(50),
Telefono varchar(50),
Estado bit,
FechaRegistro datetime default getdate()
)
go

create table CLIENTE(
IdCliente int primary key identity,
Documento varchar(50),
NombreCompleto varchar(50),
Correo varchar(50),
Telefono varchar(50),
Estado bit,
FechaRegistro datetime default getdate()
)
go

create table USUARIO(
IdUsuario int primary key identity,
Documento varchar(50),
NombreCompleto varchar(50),
Correo varchar(50),
Clave varchar(50),
IdRol int references ROL(IdRol),
Estado bit,
FechaRegistro datetime default getdate()
)
go

create table CATEGORIA(
IdCategoria int primary key identity,
Descripcion varchar(100),
Estado bit,
FechaRegistro datetime default getdate()
)
go

create table PRODUCTO(
IdProducto int primary key identity,
Codigo varchar(50),
Nombre varchar(50),
Descripcion varchar(50),
IdCategoria int references CATEGORIA(IdCategoria),
IdGrupoReposicion int NULL,
CantidadMinima int NULL,
CantidadMaxima int NULL,
IdGrupoProveedor int NULL,
Stock int not null default 0,
PrecioCompra decimal(10,2) default 0,
PrecioVenta decimal(10,2) default 0,
Porcentaje decimal(9,4) NOT NULL CONSTRAINT DF_PRODUCTO_Porcentaje DEFAULT (0),
Estado bit,
FechaRegistro datetime default getdate()
)
go

-- Grupos de productos sustituibles para reposición. Los límites pertenecen
-- al grupo completo y el stock se suma entre todos sus productos.
CREATE TABLE GRUPO_REPOSICION(
    IdGrupoReposicion int IDENTITY(1,1) PRIMARY KEY,
    Nombre varchar(100) NOT NULL,
    CantidadMinima int NOT NULL,
    CantidadMaxima int NOT NULL,
    Activo bit NOT NULL CONSTRAINT DF_GRUPO_REPOSICION_Activo DEFAULT(1),
    FechaRegistro datetime2(0) NOT NULL CONSTRAINT DF_GRUPO_REPOSICION_Fecha DEFAULT(SYSDATETIME()),
    CONSTRAINT UQ_GRUPO_REPOSICION_Nombre UNIQUE(Nombre),
    CONSTRAINT CK_GRUPO_REPOSICION_Cantidades CHECK(CantidadMinima >= 0 AND CantidadMaxima >= CantidadMinima)
)
GO

-- Perfiles reutilizables de proveedores. Un perfil contiene varios proveedores;
-- cada producto se vincula al perfil cuyos proveedores son válidos para cotizarlo.
CREATE TABLE GRUPO_PROVEEDOR(
    IdGrupoProveedor int IDENTITY(1,1) PRIMARY KEY,
    Nombre varchar(100) NOT NULL,
    Activo bit NOT NULL CONSTRAINT DF_GRUPO_PROVEEDOR_Activo DEFAULT(1),
    FechaRegistro datetime2(0) NOT NULL CONSTRAINT DF_GRUPO_PROVEEDOR_Fecha DEFAULT(SYSDATETIME()),
    CONSTRAINT UQ_GRUPO_PROVEEDOR_Nombre UNIQUE(Nombre)
)
GO

CREATE TABLE GRUPO_PROVEEDOR_DETALLE(
    IdGrupoProveedor int NOT NULL REFERENCES GRUPO_PROVEEDOR(IdGrupoProveedor),
    IdProveedor int NOT NULL REFERENCES PROVEEDOR(IdProveedor),
    FechaRegistro datetime2(0) NOT NULL CONSTRAINT DF_GRUPO_PROVEEDOR_DETALLE_Fecha DEFAULT(SYSDATETIME()),
    CONSTRAINT PK_GRUPO_PROVEEDOR_DETALLE PRIMARY KEY(IdGrupoProveedor, IdProveedor)
)
GO

ALTER TABLE dbo.PRODUCTO ADD CONSTRAINT FK_PRODUCTO_GrupoReposicion
    FOREIGN KEY(IdGrupoReposicion) REFERENCES dbo.GRUPO_REPOSICION(IdGrupoReposicion)
GO
ALTER TABLE dbo.PRODUCTO ADD CONSTRAINT FK_PRODUCTO_GrupoProveedor
    FOREIGN KEY(IdGrupoProveedor) REFERENCES dbo.GRUPO_PROVEEDOR(IdGrupoProveedor)
GO
ALTER TABLE dbo.PRODUCTO ADD CONSTRAINT CK_PRODUCTO_Reposicion
    CHECK (
        (IdGrupoReposicion IS NOT NULL AND CantidadMinima IS NULL AND CantidadMaxima IS NULL)
        OR
        (IdGrupoReposicion IS NULL AND
            ((CantidadMinima IS NULL AND CantidadMaxima IS NULL)
             OR (CantidadMinima IS NOT NULL AND CantidadMaxima IS NOT NULL
                 AND CantidadMinima >= 0 AND CantidadMaxima >= CantidadMinima)))
    )
GO
CREATE INDEX IX_PRODUCTO_GrupoReposicion ON dbo.PRODUCTO(IdGrupoReposicion, Estado, Stock)
GO
CREATE INDEX IX_PRODUCTO_GrupoProveedor ON dbo.PRODUCTO(IdGrupoProveedor, Estado)
GO

create table COMPRA(
IdCompra int primary key identity,
IdUsuario int references USUARIO(IdUsuario),
IdProveedor int references PROVEEDOR(IdProveedor),
TipoDocumento varchar(50),
NumeroDocumento varchar(50),
MontoTotal decimal(10,2),
MontoNoInventariable decimal(10,2) NOT NULL CONSTRAINT DF_COMPRA_MontoNoInventariable DEFAULT (0),
FechaRegistro datetime default getdate()
)
go

create table DETALLE_COMPRA(
IdDetalleCompra int primary key identity,
IdCompra int references COMPRA(IdCompra),
IdProducto int references PRODUCTO(IdProducto),
PrecioCompra decimal(10,2) default 0,
PrecioVenta decimal(10,2) default 0,
Porcentaje decimal(9,4) NOT NULL CONSTRAINT DF_DETALLE_COMPRA_Porcentaje DEFAULT (0),
Cantidad int,
MontoTotal decimal(10,2),
FechaRegistro datetime default getdate()
)
go

create table VENTA(
IdVenta int primary key identity,
IdUsuario int references USUARIO(IdUsuario),
TipoDocumento varchar(50),
NumeroDocumento varchar(50),
DocumentoCliente varchar(50),
NombreCliente varchar(100),
MontoPago decimal(10,2),
MontoCambio decimal(10,2),
MontoTotal decimal(10,2),
FechaRegistro datetime default getdate()
)
go

create table DETALLE_VENTA(
IdDetalleVenta int primary key identity,
IdVenta int references VENTA(IdVenta),
IdProducto int references PRODUCTO(IdProducto),
PrecioCompra decimal(10,2) NOT NULL CONSTRAINT DF_DETALLE_VENTA_PrecioCompra DEFAULT (0),
PrecioVenta decimal(10,2),
Porcentaje decimal(9,4) NOT NULL CONSTRAINT DF_DETALLE_VENTA_Porcentaje DEFAULT (0),
Cantidad int,
SubTotal decimal(10,2),
FechaRegistro datetime default getdate()
)
go

create table NEGOCIO(
IdNegocio int primary key,
Nombre varchar(60),
NIT varchar(60),
Direccion varchar(60),
Logo varbinary(max) NULL
)
go

-- =============================================
-- OBLIGACIONES Y PAGOS PERSONALES
-- =============================================

-- Catálogo que el administrador llenará con las obligaciones personales
-- mensuales. MontoMensual alimenta la meta mensual de ganancia disponible.
CREATE TABLE GASTO_PERSONAL(
    IdGastoPersonal int primary key identity(1,1),
    Nombre varchar(100) not null,
    MontoMensual decimal(10,2) not null,
    DiaVencimiento tinyint null,
    Activo bit not null constraint DF_GASTO_PERSONAL_Activo default (1),
    Observaciones varchar(250) null,
    FechaRegistro datetime2(0) not null constraint DF_GASTO_PERSONAL_FechaRegistro default (sysdatetime()),
    constraint CK_GASTO_PERSONAL_MontoMensual check (MontoMensual >= 0),
    constraint CK_GASTO_PERSONAL_DiaVencimiento check (DiaVencimiento is null or DiaVencimiento between 1 and 31)
)
go

-- Copia mensual del importe y vencimiento. Así, si cambia el monto del
-- catálogo, los períodos anteriores conservan lo que correspondía pagar.
CREATE TABLE GASTO_PERSONAL_PERIODO(
    IdPeriodoGastoPersonal int primary key identity(1,1),
    IdGastoPersonal int not null references GASTO_PERSONAL(IdGastoPersonal),
    Anio smallint not null,
    Mes tinyint not null,
    MontoProgramado decimal(10,2) not null,
    FechaVencimiento date null,
    FechaRegistro datetime2(0) not null constraint DF_GASTO_PERSONAL_PERIODO_FechaRegistro default (sysdatetime()),
    constraint UQ_GASTO_PERSONAL_PERIODO unique (IdGastoPersonal, Anio, Mes),
    constraint CK_GASTO_PERSONAL_PERIODO_Mes check (Mes between 1 and 12),
    constraint CK_GASTO_PERSONAL_PERIODO_Monto check (MontoProgramado >= 0)
)
go

-- Los pagos pueden ser parciales. El saldo pendiente se calculará como
-- MontoProgramado menos la suma de los pagos asociados al período.
CREATE TABLE PAGO_GASTO_PERSONAL(
    IdPagoGastoPersonal int primary key identity(1,1),
    IdPeriodoGastoPersonal int not null references GASTO_PERSONAL_PERIODO(IdPeriodoGastoPersonal),
    IdUsuario int null references USUARIO(IdUsuario),
    Monto decimal(10,2) not null,
    FechaPago datetime2(0) not null constraint DF_PAGO_GASTO_PERSONAL_FechaPago default (sysdatetime()),
    MedioPago varchar(20) not null constraint DF_PAGO_GASTO_PERSONAL_MedioPago default ('Efectivo'),
    Referencia varchar(100) null,
    Observaciones varchar(250) null,
    FechaRegistro datetime2(0) not null constraint DF_PAGO_GASTO_PERSONAL_FechaRegistro default (sysdatetime()),
    constraint CK_PAGO_GASTO_PERSONAL_Monto check (Monto > 0),
    constraint CK_PAGO_GASTO_PERSONAL_MedioPago check (MedioPago in ('Efectivo', 'Transferencia', 'Tarjeta', 'Otro'))
)
go

CREATE INDEX IX_GASTO_PERSONAL_PERIODO_Vencimiento
    ON GASTO_PERSONAL_PERIODO(FechaVencimiento, IdGastoPersonal)
go

CREATE INDEX IX_PAGO_GASTO_PERSONAL_PeriodoFecha
    ON PAGO_GASTO_PERSONAL(IdPeriodoGastoPersonal, FechaPago)
go

-- Cierres independientes por turno/día y movimientos pagados.
CREATE TABLE FIN_CIERRE_CAJA(
    IdCierre int primary key identity(1,1),
    IdUsuario int null references USUARIO(IdUsuario),
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
)
go

CREATE TABLE FIN_MOVIMIENTO(
    IdMovimiento int primary key identity(1,1),
    Tipo varchar(20) not null,
    IdCompra int null references COMPRA(IdCompra),
    IdUsuario int null references USUARIO(IdUsuario),
    Fecha datetime2(0) not null constraint DF_FIN_MOVIMIENTO_Fecha default(sysdatetime()),
    Concepto varchar(150) not null,
    Monto decimal(12,2) not null,
    MedioPago varchar(20) not null constraint DF_FIN_MOVIMIENTO_Medio default('Efectivo'),
    Referencia varchar(100) null,
    CONSTRAINT CK_FIN_MOVIMIENTO_Tipo CHECK(Tipo in ('Empresa','Proveedor')),
    CONSTRAINT CK_FIN_MOVIMIENTO_Monto CHECK(Monto > 0),
    CONSTRAINT CK_FIN_MOVIMIENTO_Medio CHECK(MedioPago in ('Efectivo','Transferencia','Tarjeta','Otro'))
)
go

CREATE TABLE FIN_DEPOSITO(
    IdDeposito int primary key identity(1,1),
    IdCierre int not null references FIN_CIERRE_CAJA(IdCierre),
    IdUsuario int null references USUARIO(IdUsuario),
    Fecha datetime2(0) not null constraint DF_FIN_DEPOSITO_Fecha default(sysdatetime()),
    Capital decimal(12,2) not null constraint DF_FIN_DEPOSITO_Capital default(0),
    Ganancia decimal(12,2) not null constraint DF_FIN_DEPOSITO_Ganancia default(0),
    Referencia varchar(100) null,
    CONSTRAINT CK_FIN_DEPOSITO_Montos CHECK(Capital >= 0 AND Ganancia >= 0 AND Capital + Ganancia > 0)
)
go

CREATE INDEX IX_FIN_CIERRE_Fecha ON FIN_CIERRE_CAJA(FechaCierre)
go
CREATE INDEX IX_FIN_MOVIMIENTO_Fecha ON FIN_MOVIMIENTO(Fecha)
go
CREATE INDEX IX_FIN_MOVIMIENTO_Compra ON FIN_MOVIMIENTO(IdCompra)
go

-- =============================================
-- CREACIÓN DE TIPOS DE DATOS PERSONALIZADOS
-- =============================================

CREATE TYPE [dbo].[EDetalle_Venta] AS TABLE(
	[IdProducto] int NULL,
	[PrecioVenta] decimal(18,2) NULL,
	[Cantidad] int NULL,
	[SubTotal] decimal(18,2) NULL
)
GO

-- =============================================
-- CREACIÓN DE PROCEDIMIENTOS ALMACENADOS
-- =============================================

create PROC SP_REGISTRARUSUARIO(
@Documento varchar(50),
@NombreCompleto varchar(100),
@Correo varchar(100),
@Clave varchar(100),
@IdRol int,
@Estado bit,
@IdUsuarioResultado int output,
@Mensaje varchar(500) output
)
as
begin
	set @IdUsuarioResultado = 0
	set @Mensaje = ''

	if not exists(select * from USUARIO where Documento = @Documento)
	begin
		insert into usuario(Documento,NombreCompleto,Correo,Clave,IdRol,Estado) values
		(@Documento,@NombreCompleto,@Correo,@Clave,@IdRol,@Estado)

		set @IdUsuarioResultado = SCOPE_IDENTITY()
	end
	else
		set @Mensaje = 'No se puede repetir el documento para más de un usuario'
end
go

create PROC SP_EDITARUSUARIO(
@IdUsuario int,
@Documento varchar(50),
@NombreCompleto varchar(100),
@Correo varchar(100),
@Clave varchar(100),
@IdRol int,
@Estado bit,
@Respuesta bit output,
@Mensaje varchar(500) output
)
as
begin
	set @Respuesta = 0
	set @Mensaje = ''

	if not exists(select * from USUARIO where Documento = @Documento and idusuario != @IdUsuario)
	begin
		update USUARIO set
		Documento = @Documento,
		NombreCompleto = @NombreCompleto,
		Correo = @Correo,
		Clave = @Clave,
		IdRol = @IdRol,
		Estado = @Estado
		where IdUsuario = @IdUsuario

		set @Respuesta = 1
	end
	else
		set @Mensaje = 'No se puede repetir el documento para más de un usuario'
end
go

create PROC SP_ELIMINARUSUARIO(
@IdUsuario int,
@Respuesta bit output,
@Mensaje varchar(500) output
)
as
begin
	set @Respuesta = 0
	set @Mensaje = ''
	declare @pasoreglas bit = 1

	IF EXISTS (SELECT * FROM COMPRA C 
	INNER JOIN USUARIO U ON U.IdUsuario = C.IdUsuario
	WHERE U.IDUSUARIO = @IdUsuario)
	BEGIN
		set @pasoreglas = 0
		set @Respuesta = 0
		set @Mensaje = @Mensaje + 'No se puede eliminar porque el usuario se encuentra relacionado a una COMPRA\n' 
	END

	IF EXISTS (SELECT * FROM VENTA V
	INNER JOIN USUARIO U ON U.IdUsuario = V.IdUsuario
	WHERE U.IDUSUARIO = @IdUsuario)
	BEGIN
		set @pasoreglas = 0
		set @Respuesta = 0
		set @Mensaje = @Mensaje + 'No se puede eliminar porque el usuario se encuentra relacionado a una VENTA\n' 
	END

	if(@pasoreglas = 1)
	begin
		delete from USUARIO where IdUsuario = @IdUsuario
		set @Respuesta = 1 
	end
end
go

-- PROCEDIMIENTOS PARA CATEGORIA
create PROC SP_RegistrarCategoria(
@Descripcion varchar(50),
@Estado bit,
@Resultado int output,
@Mensaje varchar(500) output
)as
begin
	SET @Resultado = 0
	IF NOT EXISTS (SELECT * FROM CATEGORIA WHERE Descripcion = @Descripcion)
	begin
		insert into CATEGORIA(Descripcion,Estado) values (@Descripcion,@Estado)
		set @Resultado = SCOPE_IDENTITY()
	end
	ELSE
		set @Mensaje = 'No se puede repetir la descripcion de una categoria'
end
go

create procedure sp_EditarCategoria(
@IdCategoria int,
@Descripcion varchar(50),
@Estado bit,
@Resultado bit output,
@Mensaje varchar(500) output
)
as
begin
	SET @Resultado = 1
	IF NOT EXISTS (SELECT * FROM CATEGORIA WHERE Descripcion = @Descripcion and IdCategoria != @IdCategoria)
		update CATEGORIA set
		Descripcion = @Descripcion,
		Estado = @Estado
		where IdCategoria = @IdCategoria
	ELSE
	begin
		SET @Resultado = 0
		set @Mensaje = 'No se puede repetir la descripcion de una categoria'
	end
end
go

create procedure sp_EliminarCategoria(
@IdCategoria int,
@Resultado bit output,
@Mensaje varchar(500) output
)
as
begin
	SET @Resultado = 1
	IF NOT EXISTS (
	 select * from CATEGORIA c
	 inner join PRODUCTO p on p.IdCategoria = c.IdCategoria
	 where c.IdCategoria = @IdCategoria
	)
	begin
	 delete top(1) from CATEGORIA where IdCategoria = @IdCategoria
	end
	ELSE
	begin
		SET @Resultado = 0
		set @Mensaje = 'La categoria se encuentra relacionada a un producto'
	end
end
GO

-- PROCEDIMIENTOS PARA PRODUCTO
create PROC sp_RegistrarProducto(
@Codigo varchar(20),
@Nombre varchar(30),
@Descripcion varchar(30),
@IdCategoria int,
@Estado bit,
@Resultado int output,
@Mensaje varchar(500) output
)as
begin
	SET @Resultado = 0
	IF NOT EXISTS (SELECT * FROM producto WHERE Codigo = @Codigo)
	begin
		insert into producto(Codigo,Nombre,Descripcion,IdCategoria,Estado) values (@Codigo,@Nombre,@Descripcion,@IdCategoria,@Estado)
		set @Resultado = SCOPE_IDENTITY()
	end
	ELSE
	 SET @Mensaje = 'Ya existe un producto con el mismo codigo' 
end
GO

create procedure sp_ModificarProducto(
@IdProducto int,
@Codigo varchar(20),
@Nombre varchar(30),
@Descripcion varchar(30),
@IdCategoria int,
@Estado bit,
@Resultado bit output,
@Mensaje varchar(500) output
)
as
begin
	SET @Resultado = 1
	IF NOT EXISTS (SELECT * FROM PRODUCTO WHERE codigo = @Codigo and IdProducto != @IdProducto)
		update PRODUCTO set
		codigo = @Codigo,
		Nombre = @Nombre,
		Descripcion = @Descripcion,
		IdCategoria = @IdCategoria,
		Estado = @Estado
		where IdProducto = @IdProducto
	ELSE
	begin
		SET @Resultado = 0
		SET @Mensaje = 'Ya existe un producto con el mismo codigo' 
	end
end
go

create PROC SP_EliminarProducto(
@IdProducto int,
@Respuesta bit output,
@Mensaje varchar(500) output
)
as
begin
	set @Respuesta = 0
	set @Mensaje = ''
	declare @pasoreglas bit = 1

	IF EXISTS (SELECT * FROM DETALLE_COMPRA dc 
	INNER JOIN PRODUCTO p ON p.IdProducto = dc.IdProducto
	WHERE p.IdProducto = @IdProducto)
	BEGIN
		set @pasoreglas = 0
		set @Respuesta = 0
		set @Mensaje = @Mensaje + 'No se puede eliminar porque se encuentra relacionado a una COMPRA\n' 
	END

	IF EXISTS (SELECT * FROM DETALLE_VENTA dv
	INNER JOIN PRODUCTO p ON p.IdProducto = dv.IdProducto
	WHERE p.IdProducto = @IdProducto)
	BEGIN
		set @pasoreglas = 0
		set @Respuesta = 0
		set @Mensaje = @Mensaje + 'No se puede eliminar porque se encuentra relacionado a una VENTA\n' 
	END

	if(@pasoreglas = 1)
	begin
		delete from PRODUCTO where IdProducto = @IdProducto
		set @Respuesta = 1 
	end
end
go

-- PROCEDIMIENTOS PARA CLIENTE
create PROC sp_RegistrarCliente(
@Documento varchar(50),
@NombreCompleto varchar(50),
@Correo varchar(50),
@Telefono varchar(50),
@Estado bit,
@Resultado int output,
@Mensaje varchar(500) output
)as
begin
	SET @Resultado = 0
	IF NOT EXISTS (SELECT * FROM CLIENTE WHERE Documento = @Documento)
	begin
		insert into CLIENTE(Documento,NombreCompleto,Correo,Telefono,Estado) values (
		@Documento,@NombreCompleto,@Correo,@Telefono,@Estado)

		set @Resultado = SCOPE_IDENTITY()
	end
	else
		set @Mensaje = 'El numero de documento ya existe'
end
go

create PROC sp_ModificarCliente(
@IdCliente int,
@Documento varchar(50),
@NombreCompleto varchar(50),
@Correo varchar(50),
@Telefono varchar(50),
@Estado bit,
@Resultado bit output,
@Mensaje varchar(500) output
)as
begin
	SET @Resultado = 1
	IF NOT EXISTS (SELECT * FROM CLIENTE WHERE Documento = @Documento and IdCliente != @IdCliente)
	begin
		update CLIENTE set
		Documento = @Documento,
		NombreCompleto = @NombreCompleto,
		Correo = @Correo,
		Telefono = @Telefono,
		Estado = @Estado
		where IdCliente = @IdCliente
	end
	else
	begin
		SET @Resultado = 0
		set @Mensaje = 'El numero de documento ya existe'
	end
end
GO

-- PROCEDIMIENTOS PARA PROVEEDOR
create PROC sp_RegistrarProveedor(
@Documento varchar(50),
@RazonSocial varchar(50),
@Correo varchar(50),
@Telefono varchar(50),
@Estado bit,
@Resultado int output,
@Mensaje varchar(500) output
)as
begin
	SET @Resultado = 0
	IF NOT EXISTS (SELECT * FROM PROVEEDOR WHERE Documento = @Documento)
	begin
		insert into PROVEEDOR(Documento,RazonSocial,Correo,Telefono,Estado) values (
		@Documento,@RazonSocial,@Correo,@Telefono,@Estado)

		set @Resultado = SCOPE_IDENTITY()
	end
	else
		set @Mensaje = 'El numero de documento ya existe'
end
GO

create PROC sp_ModificarProveedor(
@IdProveedor int,
@Documento varchar(50),
@RazonSocial varchar(50),
@Correo varchar(50),
@Telefono varchar(50),
@Estado bit,
@Resultado bit output,
@Mensaje varchar(500) output
)as
begin
	SET @Resultado = 1
	IF NOT EXISTS (SELECT * FROM PROVEEDOR WHERE Documento = @Documento and IdProveedor != @IdProveedor)
	begin
		update PROVEEDOR set
		Documento = @Documento,
		RazonSocial = @RazonSocial,
		Correo = @Correo,
		Telefono = @Telefono,
		Estado = @Estado
		where IdProveedor = @IdProveedor
	end
	else
	begin
		SET @Resultado = 0
		set @Mensaje = 'El numero de documento ya existe'
	end
end
go

create procedure sp_EliminarProveedor(
@IdProveedor int,
@Resultado bit output,
@Mensaje varchar(500) output
)
as
begin
	SET @Resultado = 1
	IF NOT EXISTS (
	 select * from PROVEEDOR p
	 inner join COMPRA c on p.IdProveedor = c.IdProveedor
	 where p.IdProveedor = @IdProveedor
	)
	begin
	 delete top(1) from PROVEEDOR where IdProveedor = @IdProveedor
	end
	ELSE
	begin
		SET @Resultado = 0
		set @Mensaje = 'El proveedor se encuentra relacionado a una compra'
	end
end
go

-- PROCEDIMIENTO PARA REGISTRAR VENTA
CREATE PROCEDURE usp_RegistrarVenta(
@IdUsuario int,
@TipoDocumento varchar(500),
@NumeroDocumento varchar(500) OUTPUT,
@DocumentoCliente varchar(500),
@NombreCliente varchar(500),
@MontoPago decimal(18,2),
@MontoCambio decimal(18,2),
@MontoTotal decimal(18,2),
@DetalleVenta [EDetalle_Venta] READONLY,               
@Resultado bit output,
@Mensaje varchar(500) output
)
as
begin
	begin try
		declare @idventa int = 0
		set @Resultado = 1
		set @Mensaje = ''

		begin transaction registro

		DECLARE @SiguienteVenta bigint;
		SELECT @SiguienteVenta = ISNULL(MAX(TRY_CONVERT(bigint, NumeroDocumento)), 0) + 1
		FROM VENTA WITH (TABLOCKX, HOLDLOCK);
		SET @NumeroDocumento = CASE WHEN @SiguienteVenta < 100000
			THEN RIGHT('00000' + CONVERT(varchar(20), @SiguienteVenta), 5)
			ELSE CONVERT(varchar(20), @SiguienteVenta) END;

		IF NOT EXISTS (SELECT 1 FROM @DetalleVenta)
			THROW 50011, 'La venta debe incluir al menos un producto', 1;

		IF EXISTS (SELECT 1 FROM @DetalleVenta WHERE IdProducto IS NULL OR Cantidad IS NULL OR Cantidad <= 0 OR PrecioVenta IS NULL OR SubTotal IS NULL OR PrecioVenta < 0 OR SubTotal < 0 OR ABS(SubTotal - ROUND(PrecioVenta * Cantidad, 2)) > 0.01)
			THROW 50012, 'El detalle de venta contiene cantidades o precios inválidos', 1;

		IF EXISTS (SELECT IdProducto FROM @DetalleVenta GROUP BY IdProducto HAVING COUNT(*) > 1)
			THROW 50013, 'No se puede repetir un producto en el detalle de venta', 1;

		IF ABS(@MontoTotal - (SELECT SUM(SubTotal) FROM @DetalleVenta)) > 0.01 OR @MontoPago < @MontoTotal OR ABS(@MontoCambio - (@MontoPago - @MontoTotal)) > 0.01
			THROW 50014, 'Los montos de la venta no coinciden con el detalle o el pago', 1;

		DECLARE @ProductosSolicitados int = (SELECT COUNT(DISTINCT IdProducto) FROM @DetalleVenta);
		DECLARE @ProductosActualizados int;

		UPDATE p
		SET p.Stock = p.Stock - dv.Cantidad
		FROM PRODUCTO p
		INNER JOIN @DetalleVenta dv ON dv.IdProducto = p.IdProducto
		WHERE p.Stock >= dv.Cantidad;

		SET @ProductosActualizados = @@ROWCOUNT;
		IF @ProductosActualizados <> @ProductosSolicitados
			THROW 50015, 'Stock insuficiente para uno o más productos', 1;

		insert into VENTA(IdUsuario,TipoDocumento,NumeroDocumento,DocumentoCliente,NombreCliente,MontoPago,MontoCambio,MontoTotal)
		values(@IdUsuario,@TipoDocumento,@NumeroDocumento,@DocumentoCliente,@NombreCliente,@MontoPago,@MontoCambio,@MontoTotal)

		set @idventa = SCOPE_IDENTITY()

		insert into DETALLE_VENTA(IdVenta,IdProducto,PrecioVenta,Cantidad,SubTotal)
		select @idventa,IdProducto,PrecioVenta,Cantidad,SubTotal from @DetalleVenta

		commit transaction registro
	end try
	begin catch
		set @Resultado = 0
		set @Mensaje = ERROR_MESSAGE()
		IF XACT_STATE() <> 0 ROLLBACK TRANSACTION registro
	end catch
end
go

-- REPORTES
create PROC sp_ReporteCompras(
 @fechainicio varchar(10),
 @fechafin varchar(10),
 @idproveedor int
 )
 as
begin
 SET DATEFORMAT dmy;
 select 
 convert(char(10),c.FechaRegistro,103)[FechaRegistro],c.TipoDocumento,c.NumeroDocumento,c.MontoTotal,
 u.NombreCompleto[UsuarioRegistro],
 pr.Documento[DocumentoProveedor],pr.RazonSocial,
 p.Codigo[CodigoProducto],p.Nombre[NombreProducto],ca.Descripcion[Categoria],dc.PrecioCompra,dc.PrecioVenta,dc.Cantidad,dc.MontoTotal[SubTotal]
 from COMPRA c
 inner join USUARIO u on u.IdUsuario = c.IdUsuario
 inner join PROVEEDOR pr on pr.IdProveedor = c.IdProveedor
 inner join DETALLE_COMPRA dc on dc.IdCompra = c.IdCompra
 inner join PRODUCTO p on p.IdProducto = dc.IdProducto
 inner join CATEGORIA ca on ca.IdCategoria = p.IdCategoria
 where CONVERT(date,c.FechaRegistro) between @fechainicio and @fechafin
 and pr.IdProveedor = iif(@idproveedor=0,pr.IdProveedor,@idproveedor)
end
go

CREATE PROC sp_ReporteVentas(
 @fechainicio varchar(10),
 @fechafin varchar(10)
 )
as
begin
 SET DATEFORMAT dmy;  
 select 
 convert(char(10),v.FechaRegistro,103)[FechaRegistro],v.TipoDocumento,v.NumeroDocumento,v.MontoTotal,
 u.NombreCompleto[UsuarioRegistro],
 v.DocumentoCliente,v.NombreCliente,
 p.Codigo[CodigoProducto],p.Nombre[NombreProducto],ca.Descripcion[Categoria],dv.PrecioVenta,dv.Cantidad,dv.SubTotal
 from VENTA v
 inner join USUARIO u on u.IdUsuario = v.IdUsuario
 inner join DETALLE_VENTA dv on dv.IdVenta = v.IdVenta
 inner join PRODUCTO p on p.IdProducto = dv.IdProducto
 inner join CATEGORIA ca on ca.IdCategoria = p.IdCategoria
 where CONVERT(date,v.FechaRegistro) between @fechainicio and @fechafin
end
GO


-- =============================================
-- INSERTS DE DATOS DE PRUEBA (CONFIGURACIÓN INICIAL)
-- =============================================

-- 1. Roles
insert into ROL (Descripcion) values ('ADMINISTRADOR')
GO
insert into ROL (Descripcion) values ('EMPLEADO')
GO

-- 2. Permisos para Administrador (IdRol = 1)
INSERT INTO PERMISO(IdRol, NombreMenu) VALUES
(1, 'menuUsuarios'),
(1, 'menuMantenedor'),
(1, 'menuVentas'),
(1, 'menuCompras'),
(1, 'menuClientes'),
(1, 'menuProveedores'),
(1, 'menuReportes'),
(1, 'menuFinanzas'),
(1, 'menuAcercaDe')
GO

-- 3. Permisos para Empleado (IdRol = 2)
INSERT INTO PERMISO(IdRol, NombreMenu) values
(2, 'menuVentas'),
(2, 'menuClientes')
GO

-- 4. Usuarios (Admin con clave '123' como se solicitó)
insert into USUARIO(Documento, NombreCompleto, Correo, Clave, IdRol, Estado)
values ('admin', 'Administrador General', 'admin@sistema.com', '123', 1, 1)
GO

insert into USUARIO(Documento, NombreCompleto, Correo, Clave, IdRol, Estado)
values ('empleado1', 'Juan Pérez', 'empleado@sistema.com', '1234', 2, 1)
GO

-- 5. Datos del Negocio
INSERT INTO NEGOCIO(IdNegocio, Nombre, NIT, Direccion, Logo) 
values (1, 'Sistema de Ventas GT', '123456789-1', 'Ciudad de Guatemala', null)
GO

-- 6. Categorías de Prueba
INSERT INTO CATEGORIA(Descripcion, Estado) values ('Bebidas', 1)
GO
INSERT INTO CATEGORIA(Descripcion, Estado) values ('Abarrotes', 1)
GO
INSERT INTO CATEGORIA(Descripcion, Estado) values ('Cuidado Personal', 1)
GO

-- 7. Proveedores de Prueba
INSERT INTO PROVEEDOR(Documento, RazonSocial, Correo, Telefono, Estado) VALUES
('10101010', 'Distribuidora del Sur S.A.', 'ventas@distsur.com', '22334455', 1)
GO
INSERT INTO PROVEEDOR(Documento, RazonSocial, Correo, Telefono, Estado) VALUES
('20202020', 'Comercializadora Global', 'contacto@global.com', '24556677', 1)
GO

-- 8. Clientes de Prueba
INSERT INTO CLIENTE(Documento, NombreCompleto, Correo, Telefono, Estado) VALUES
('CF', 'Consumidor Final', 'cf@gmail.com', '00000000', 1)
GO
INSERT INTO CLIENTE(Documento, NombreCompleto, Correo, Telefono, Estado) VALUES
('30303030', 'María López', 'maria.lopez@gmail.com', '55443322', 1)
GO

-- 9. Productos de Prueba (Asociados a Categorías)
INSERT INTO PRODUCTO(Codigo, Nombre, Descripcion, IdCategoria, Stock, PrecioCompra, PrecioVenta, Estado) VALUES
('P001', 'Coca Cola 2L', 'Gaseosa retornable 2 Litros', 1, 25, 12.00, 15.00, 1)
GO
INSERT INTO PRODUCTO(Codigo, Nombre, Descripcion, IdCategoria, Stock, PrecioCompra, PrecioVenta, Estado) VALUES
('P002', 'Agua Pura 1L', 'Botella de agua pura', 1, 40, 4.00, 6.00, 1)
GO
INSERT INTO PRODUCTO(Codigo, Nombre, Descripcion, IdCategoria, Stock, PrecioCompra, PrecioVenta, Estado) VALUES
('P003', 'Arroz 1kg', 'Arroz blanco selecto', 2, 30, 8.50, 11.00, 1)
GO
INSERT INTO PRODUCTO(Codigo, Nombre, Descripcion, IdCategoria, Stock, PrecioCompra, PrecioVenta, Estado) VALUES
('P004', 'Aceite 800ml', 'Aceite vegetal puro', 2, 20, 14.00, 18.00, 1)
GO
INSERT INTO PRODUCTO(Codigo, Nombre, Descripcion, IdCategoria, Stock, PrecioCompra, PrecioVenta, Estado) VALUES
('P005', 'Jabón de Baño', 'Jabón antibacterial', 3, 50, 5.00, 7.50, 1)
GO

-- =============================================
-- PORCENTAJES, COSTO HISTÓRICO Y REPORTES ACTUALIZADOS
-- =============================================

/* Inicializa el porcentaje deducible en productos y compras de la instalación. */
UPDATE dbo.PRODUCTO
SET Porcentaje = CEILING(((PrecioVenta - PrecioCompra) / NULLIF(PrecioCompra, 0)) * 10000) / 10000
WHERE PrecioCompra > 0 AND PrecioVenta >= PrecioCompra;

UPDATE dbo.DETALLE_COMPRA
SET Porcentaje = CEILING(((PrecioVenta - PrecioCompra) / NULLIF(PrecioCompra, 0)) * 10000) / 10000
WHERE PrecioCompra > 0 AND PrecioVenta >= PrecioCompra;

/* Conserva el costo histórico en ventas, tomando la compra más reciente a esa fecha. */
UPDATE dv
SET dv.PrecioCompra = compra.PrecioCompra,
    dv.Porcentaje = compra.Porcentaje
FROM dbo.DETALLE_VENTA dv
INNER JOIN dbo.VENTA v ON v.IdVenta = dv.IdVenta
OUTER APPLY
(
    SELECT TOP (1) dc.PrecioCompra, dc.Porcentaje
    FROM dbo.DETALLE_COMPRA dc
    INNER JOIN dbo.COMPRA c ON c.IdCompra = dc.IdCompra
    WHERE dc.IdProducto = dv.IdProducto
      AND c.FechaRegistro <= v.FechaRegistro
    ORDER BY c.FechaRegistro DESC, dc.IdDetalleCompra DESC
) compra
WHERE dv.PrecioCompra = 0 AND compra.PrecioCompra IS NOT NULL;
GO

IF OBJECT_ID('dbo.sp_RegistrarProducto', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RegistrarProducto;
GO
CREATE PROCEDURE dbo.sp_RegistrarProducto
    @Codigo varchar(20),
    @Nombre varchar(30),
    @Descripcion varchar(30),
    @IdCategoria int,
    @Porcentaje decimal(9,4),
    @Estado bit,
    @Resultado int OUTPUT,
    @Mensaje varchar(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Resultado = 0;
    SET @Mensaje = '';
    IF @Porcentaje < 0
    BEGIN
        SET @Mensaje = 'El porcentaje no puede ser negativo';
        RETURN;
    END;
    IF NOT EXISTS (SELECT 1 FROM dbo.PRODUCTO WHERE Codigo = @Codigo)
    BEGIN
        INSERT INTO dbo.PRODUCTO(Codigo, Nombre, Descripcion, IdCategoria, Porcentaje, Estado)
        VALUES(@Codigo, @Nombre, @Descripcion, @IdCategoria, @Porcentaje, @Estado);
        SET @Resultado = SCOPE_IDENTITY();
    END
    ELSE
        SET @Mensaje = 'Ya existe un producto con el mismo código';
END;
GO

IF OBJECT_ID('dbo.sp_ModificarProducto', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ModificarProducto;
GO
CREATE PROCEDURE dbo.sp_ModificarProducto
    @IdProducto int,
    @Codigo varchar(20),
    @Nombre varchar(30),
    @Descripcion varchar(30),
    @IdCategoria int,
    @Porcentaje decimal(9,4),
    @Estado bit,
    @Resultado bit OUTPUT,
    @Mensaje varchar(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Resultado = 0;
    SET @Mensaje = '';
    IF @Porcentaje < 0
    BEGIN
        SET @Mensaje = 'El porcentaje no puede ser negativo';
        RETURN;
    END;
    IF EXISTS (SELECT 1 FROM dbo.PRODUCTO WHERE Codigo = @Codigo AND IdProducto <> @IdProducto)
    BEGIN
        SET @Mensaje = 'Ya existe un producto con el mismo código';
        RETURN;
    END;

    UPDATE dbo.PRODUCTO
    SET Codigo = @Codigo,
        Nombre = @Nombre,
        Descripcion = @Descripcion,
        IdCategoria = @IdCategoria,
        PrecioVenta = CASE WHEN PrecioCompra = 0 THEN 0
            ELSE CEILING(PrecioCompra * (1 + @Porcentaje / 100.0) * 4) / 4.0 END,
        Porcentaje = CASE WHEN PrecioCompra = 0 THEN @Porcentaje
            ELSE CEILING(((CEILING(PrecioCompra * (1 + @Porcentaje / 100.0) * 4) / 4.0 - PrecioCompra) / PrecioCompra) * 10000) / 10000 END,
        Estado = @Estado
    WHERE IdProducto = @IdProducto;
    SET @Resultado = CASE WHEN @@ROWCOUNT = 1 THEN 1 ELSE 0 END;
    IF @Resultado = 0 SET @Mensaje = 'No se encontró el producto para modificar';
END;
GO

IF OBJECT_ID('dbo.usp_RegistrarVenta', 'P') IS NOT NULL DROP PROCEDURE dbo.usp_RegistrarVenta;
GO
CREATE PROCEDURE dbo.usp_RegistrarVenta
    @IdUsuario int,
    @TipoDocumento varchar(500),
    @NumeroDocumento varchar(500) OUTPUT,
    @DocumentoCliente varchar(500),
    @NombreCliente varchar(500),
    @MontoPago decimal(18,2),
    @MontoCambio decimal(18,2),
    @MontoTotal decimal(18,2),
    @DetalleVenta dbo.EDetalle_Venta READONLY,
    @Resultado bit OUTPUT,
    @Mensaje varchar(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DECLARE @idventa int = 0;
        SET @Resultado = 1;
        SET @Mensaje = '';
        BEGIN TRANSACTION registro;

        DECLARE @SiguienteVenta bigint;
        SELECT @SiguienteVenta = ISNULL(MAX(TRY_CONVERT(bigint, NumeroDocumento)), 0) + 1
        FROM dbo.VENTA WITH (TABLOCKX, HOLDLOCK);
        SET @NumeroDocumento = CASE WHEN @SiguienteVenta < 100000
            THEN RIGHT('00000' + CONVERT(varchar(20), @SiguienteVenta), 5)
            ELSE CONVERT(varchar(20), @SiguienteVenta) END;

        IF NOT EXISTS (SELECT 1 FROM @DetalleVenta)
            THROW 50011, 'La venta debe incluir al menos un producto', 1;
        IF EXISTS (SELECT 1 FROM @DetalleVenta WHERE IdProducto IS NULL OR Cantidad IS NULL OR Cantidad <= 0 OR PrecioVenta IS NULL OR SubTotal IS NULL OR PrecioVenta < 0 OR SubTotal < 0 OR ABS(SubTotal - ROUND(PrecioVenta * Cantidad, 2)) > 0.01)
            THROW 50012, 'El detalle de venta contiene cantidades o precios inválidos', 1;
        IF EXISTS (SELECT IdProducto FROM @DetalleVenta GROUP BY IdProducto HAVING COUNT(*) > 1)
            THROW 50013, 'No se puede repetir un producto en el detalle de venta', 1;
        IF ABS(@MontoTotal - (SELECT SUM(SubTotal) FROM @DetalleVenta)) > 0.01 OR @MontoPago < @MontoTotal OR ABS(@MontoCambio - (@MontoPago - @MontoTotal)) > 0.01
            THROW 50014, 'Los montos de la venta no coinciden con el detalle o el pago', 1;

        DECLARE @ProductosSolicitados int = (SELECT COUNT(DISTINCT IdProducto) FROM @DetalleVenta);
        DECLARE @ProductosActualizados int;
        UPDATE p
        SET p.Stock = p.Stock - dv.Cantidad
        FROM dbo.PRODUCTO p
        INNER JOIN @DetalleVenta dv ON dv.IdProducto = p.IdProducto
        WHERE p.Stock >= dv.Cantidad;
        SET @ProductosActualizados = @@ROWCOUNT;
        IF @ProductosActualizados <> @ProductosSolicitados
            THROW 50015, 'Stock insuficiente para uno o más productos', 1;

        INSERT INTO dbo.VENTA(IdUsuario, TipoDocumento, NumeroDocumento, DocumentoCliente, NombreCliente, MontoPago, MontoCambio, MontoTotal)
        VALUES(@IdUsuario, @TipoDocumento, @NumeroDocumento, @DocumentoCliente, @NombreCliente, @MontoPago, @MontoCambio, @MontoTotal);
        SET @idventa = SCOPE_IDENTITY();

        INSERT INTO dbo.DETALLE_VENTA(IdVenta, IdProducto, PrecioCompra, PrecioVenta, Porcentaje, Cantidad, SubTotal)
        SELECT @idventa, dv.IdProducto, p.PrecioCompra, dv.PrecioVenta, p.Porcentaje, dv.Cantidad, dv.SubTotal
        FROM @DetalleVenta dv
        INNER JOIN dbo.PRODUCTO p ON p.IdProducto = dv.IdProducto;

        COMMIT TRANSACTION registro;
    END TRY
    BEGIN CATCH
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION registro;
    END CATCH
END;
GO

/* El procedimiento depende del tipo de tabla, por eso se reemplazan en este orden. */
IF OBJECT_ID('dbo.sp_RegistrarCompra', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_RegistrarCompra;
GO

IF TYPE_ID('dbo.EDetalle_Compra') IS NOT NULL
    DROP TYPE dbo.EDetalle_Compra;
GO

CREATE TYPE dbo.EDetalle_Compra AS TABLE
(
    IdProducto int NULL,
    PrecioCompra decimal(18,2) NULL,
    PrecioVenta decimal(18,2) NULL,
    Porcentaje decimal(9,4) NULL, -- margen mínimo solicitado por el usuario
    Cantidad int NULL,
    MontoTotal decimal(18,2) NULL
);
GO

CREATE PROCEDURE dbo.sp_RegistrarCompra
    @IdUsuario int,
    @IdProveedor int,
    @TipoDocumento varchar(500),
    @NumeroDocumento varchar(500) OUTPUT,
    @MontoTotal decimal(18,2),
    @MontoNoInventariable decimal(18,2),
    @DetalleCompra dbo.EDetalle_Compra READONLY,
    @Resultado bit OUTPUT,
    @Mensaje varchar(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DECLARE @idcompra int = 0;
        SET @Resultado = 1;
        SET @Mensaje = '';

        BEGIN TRANSACTION registro;

        DECLARE @SiguienteCompra bigint;
        SELECT @SiguienteCompra = ISNULL(MAX(TRY_CONVERT(bigint, NumeroDocumento)), 0) + 1
        FROM dbo.COMPRA WITH (TABLOCKX, HOLDLOCK);
        SET @NumeroDocumento = CASE WHEN @SiguienteCompra < 100000
            THEN RIGHT('00000' + CONVERT(varchar(20), @SiguienteCompra), 5)
            ELSE CONVERT(varchar(20), @SiguienteCompra) END;

        IF NOT EXISTS (SELECT 1 FROM @DetalleCompra)
            THROW 50001, 'La compra debe incluir al menos un producto', 1;

        IF EXISTS
        (
            SELECT 1 FROM @DetalleCompra
            WHERE IdProducto IS NULL OR Cantidad IS NULL OR Cantidad <= 0
               OR PrecioCompra IS NULL OR PrecioVenta IS NULL OR Porcentaje IS NULL
               OR MontoTotal IS NULL OR PrecioCompra < 0 OR PrecioVenta < 0
               OR Porcentaje < 0 OR MontoTotal < 0
               OR ABS(MontoTotal - ROUND(PrecioCompra * Cantidad, 2)) > 0.01
               OR ABS(PrecioVenta * 4 - ROUND(PrecioVenta * 4, 0)) > 0.0001
               OR (PrecioCompra > 0 AND ABS(Porcentaje - ((PrecioVenta - PrecioCompra) / PrecioCompra * 100.0)) > 0.01)
        )
            THROW 50002, 'El detalle de compra contiene precios, porcentajes o cantidades inválidos', 1;

        IF EXISTS (SELECT IdProducto FROM @DetalleCompra GROUP BY IdProducto HAVING COUNT(*) > 1)
            THROW 50003, 'No se puede repetir un producto en el detalle de compra', 1;

        IF @MontoNoInventariable IS NULL OR @MontoNoInventariable < 0
            THROW 50004, 'El importe no inventariable no es válido', 1;

        IF ABS(@MontoTotal - (SELECT SUM(MontoTotal) FROM @DetalleCompra) - @MontoNoInventariable) > 1.00
            THROW 50004, 'El total de la factura no coincide con los productos y el importe no inventariable', 1;

        INSERT INTO dbo.COMPRA(IdUsuario, IdProveedor, TipoDocumento, NumeroDocumento, MontoTotal, MontoNoInventariable)
        VALUES(@IdUsuario, @IdProveedor, @TipoDocumento, @NumeroDocumento, @MontoTotal, @MontoNoInventariable);
        SET @idcompra = SCOPE_IDENTITY();

        INSERT INTO dbo.DETALLE_COMPRA(IdCompra, IdProducto, PrecioCompra, PrecioVenta, Porcentaje, Cantidad, MontoTotal)
        SELECT @idcompra, IdProducto, PrecioCompra, PrecioVenta,
               CASE WHEN PrecioCompra = 0 THEN 0
                    ELSE CEILING(((PrecioVenta - PrecioCompra) / PrecioCompra) * 10000) / 10000 END,
               Cantidad, MontoTotal
        FROM @DetalleCompra;

        UPDATE p
        SET p.Stock = p.Stock + dc.Cantidad,
            p.PrecioCompra = dc.PrecioCompra,
            p.PrecioVenta = dc.PrecioVenta,
            p.Porcentaje = CASE WHEN dc.PrecioCompra = 0 THEN 0
                ELSE CEILING(((dc.PrecioVenta - dc.PrecioCompra) / dc.PrecioCompra) * 10000) / 10000 END
        FROM dbo.PRODUCTO p
        INNER JOIN @DetalleCompra dc ON dc.IdProducto = p.IdProducto;

        IF @@ROWCOUNT <> (SELECT COUNT(*) FROM @DetalleCompra)
            THROW 50005, 'Uno o más productos de la compra ya no existen', 1;

        COMMIT TRANSACTION registro;
    END TRY
    BEGIN CATCH
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION registro;
    END CATCH
END;
GO

IF OBJECT_ID('dbo.sp_ReporteCompras', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ReporteCompras;
GO
CREATE PROCEDURE dbo.sp_ReporteCompras
    @fechainicio varchar(10),
    @fechafin varchar(10),
    @idproveedor int
AS
BEGIN
    SET DATEFORMAT dmy;
    SELECT CONVERT(char(10), c.FechaRegistro, 103) AS FechaRegistro,
           c.TipoDocumento, c.NumeroDocumento, c.MontoTotal,
           u.NombreCompleto AS UsuarioRegistro,
           pr.Documento AS DocumentoProveedor, pr.RazonSocial,
           p.Codigo AS CodigoProducto, p.Nombre AS NombreProducto,
           ca.Descripcion AS Categoria, dc.PrecioCompra, dc.PrecioVenta,
           dc.Porcentaje, dc.Cantidad, dc.MontoTotal AS SubTotal
    FROM dbo.COMPRA c
    INNER JOIN dbo.USUARIO u ON u.IdUsuario = c.IdUsuario
    INNER JOIN dbo.PROVEEDOR pr ON pr.IdProveedor = c.IdProveedor
    INNER JOIN dbo.DETALLE_COMPRA dc ON dc.IdCompra = c.IdCompra
    INNER JOIN dbo.PRODUCTO p ON p.IdProducto = dc.IdProducto
    INNER JOIN dbo.CATEGORIA ca ON ca.IdCategoria = p.IdCategoria
    WHERE CONVERT(date, c.FechaRegistro) BETWEEN @fechainicio AND @fechafin
      AND pr.IdProveedor = IIF(@idproveedor = 0, pr.IdProveedor, @idproveedor);
END;
GO

IF OBJECT_ID('dbo.sp_ReporteVentas', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ReporteVentas;
GO
CREATE PROCEDURE dbo.sp_ReporteVentas
    @fechainicio varchar(10),
    @fechafin varchar(10)
AS
BEGIN
    SET DATEFORMAT dmy;
    SELECT CONVERT(char(10), v.FechaRegistro, 103) AS FechaRegistro,
           v.TipoDocumento, v.NumeroDocumento, v.MontoTotal,
           u.NombreCompleto AS UsuarioRegistro,
           v.DocumentoCliente, v.NombreCliente,
           p.Codigo AS CodigoProducto, p.Nombre AS NombreProducto,
           ca.Descripcion AS Categoria, dv.PrecioVenta, dv.PrecioCompra,
           dv.Porcentaje, dv.Cantidad, dv.SubTotal,
           ROUND((dv.PrecioVenta - dv.PrecioCompra) * dv.Cantidad, 2) AS GananciaBruta
    FROM dbo.VENTA v
    INNER JOIN dbo.USUARIO u ON u.IdUsuario = v.IdUsuario
    INNER JOIN dbo.DETALLE_VENTA dv ON dv.IdVenta = v.IdVenta
    INNER JOIN dbo.PRODUCTO p ON p.IdProducto = dv.IdProducto
    INNER JOIN dbo.CATEGORIA ca ON ca.IdCategoria = p.IdCategoria
    WHERE CONVERT(date, v.FechaRegistro) BETWEEN @fechainicio AND @fechafin;
END;
GO

-- Lista preliminar para cotizar. CantidadFaltante y CantidadMaxima son
-- referencias; el usuario decide cuánto pedir y no se crea ninguna compra.
IF OBJECT_ID('dbo.sp_ReporteReposicionPorProveedor', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_ReporteReposicionPorProveedor;
GO
CREATE PROCEDURE dbo.sp_ReporteReposicionPorProveedor
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH StockGrupo AS
    (
        SELECT gr.IdGrupoReposicion, gr.Nombre AS GrupoReposicion,
               gr.CantidadMinima, gr.CantidadMaxima,
               SUM(p.Stock) AS StockActual
        FROM dbo.GRUPO_REPOSICION gr
        INNER JOIN dbo.PRODUCTO p ON p.IdGrupoReposicion = gr.IdGrupoReposicion
        WHERE gr.Activo = 1 AND p.Estado = 1
        GROUP BY gr.IdGrupoReposicion, gr.Nombre, gr.CantidadMinima, gr.CantidadMaxima
    ),
    ProductosPendientes AS
    (
        SELECT p.IdProducto, p.Codigo, p.Nombre,
               sg.IdGrupoReposicion, sg.GrupoReposicion,
               sg.StockActual, sg.CantidadMinima, sg.CantidadMaxima,
               sg.CantidadMinima - sg.StockActual AS CantidadFaltante,
               p.IdGrupoProveedor
        FROM StockGrupo sg
        INNER JOIN dbo.PRODUCTO p ON p.IdGrupoReposicion = sg.IdGrupoReposicion
        WHERE sg.StockActual < sg.CantidadMinima AND p.Estado = 1

        UNION ALL

        SELECT p.IdProducto, p.Codigo, p.Nombre,
               CAST(NULL AS int), CAST(NULL AS varchar(100)),
               p.Stock, p.CantidadMinima, p.CantidadMaxima,
               p.CantidadMinima - p.Stock, p.IdGrupoProveedor
        FROM dbo.PRODUCTO p
        WHERE p.Estado = 1 AND p.IdGrupoReposicion IS NULL
          AND p.CantidadMinima IS NOT NULL AND p.CantidadMaxima IS NOT NULL
          AND p.Stock < p.CantidadMinima
    )
    SELECT pp.IdGrupoReposicion, pp.GrupoReposicion,
           gp.IdGrupoProveedor, gp.Nombre AS GrupoProveedores,
           pr.IdProveedor, pr.RazonSocial AS Proveedor,
           pp.IdProducto, pp.Codigo, pp.Nombre AS Producto,
           pp.StockActual, pp.CantidadMinima, pp.CantidadMaxima,
           pp.CantidadFaltante AS FaltanteReferencia,
           ultimo.PrecioCompra AS UltimoPrecioConEsteProveedor,
           ultimo.FechaCompra AS FechaUltimaCompraConEsteProveedor,
           historico.PrecioCompra AS UltimoPrecioHistorico,
           historico.RazonSocial AS UltimoProveedorHistorico,
           historico.FechaCompra AS FechaUltimoPrecioHistorico
    FROM ProductosPendientes pp
    INNER JOIN dbo.GRUPO_PROVEEDOR gp ON gp.IdGrupoProveedor = pp.IdGrupoProveedor AND gp.Activo = 1
    INNER JOIN dbo.GRUPO_PROVEEDOR_DETALLE gpd ON gpd.IdGrupoProveedor = gp.IdGrupoProveedor
    INNER JOIN dbo.PROVEEDOR pr ON pr.IdProveedor = gpd.IdProveedor AND pr.Estado = 1
    OUTER APPLY
    (
        SELECT TOP (1) dc.PrecioCompra, c.FechaRegistro AS FechaCompra
        FROM dbo.DETALLE_COMPRA dc
        INNER JOIN dbo.COMPRA c ON c.IdCompra = dc.IdCompra
        WHERE dc.IdProducto = pp.IdProducto AND c.IdProveedor = pr.IdProveedor
        ORDER BY c.FechaRegistro DESC, dc.IdDetalleCompra DESC
    ) ultimo
    OUTER APPLY
    (
        SELECT TOP (1) dc.PrecioCompra, prh.RazonSocial, c.FechaRegistro AS FechaCompra
        FROM dbo.DETALLE_COMPRA dc
        INNER JOIN dbo.COMPRA c ON c.IdCompra = dc.IdCompra
        INNER JOIN dbo.PROVEEDOR prh ON prh.IdProveedor = c.IdProveedor
        WHERE dc.IdProducto = pp.IdProducto
        ORDER BY c.FechaRegistro DESC, dc.IdDetalleCompra DESC
    ) historico
    ORDER BY gp.Nombre, pr.RazonSocial, pp.GrupoReposicion, pp.Nombre;
END;
GO

-- =============================================
-- LOGIN DE APLICACIÓN PARA CONEXIÓN EN RED
-- =============================================
-- Ejecuta este archivo conectado al servidor con una cuenta administradora.
-- Este login solo tendrá permisos de trabajo dentro de DBSISTEMA_VENTA;
-- no es administrador del servidor.
USE master;
GO

IF SUSER_ID(N'ventas_app2') IS NULL
BEGIN
    CREATE LOGIN [ventas_app2]
    WITH PASSWORD = N'Temporal#Ventas2026!',
         DEFAULT_DATABASE = [DBSISTEMA_VENTA],
         CHECK_POLICY = ON,
         CHECK_EXPIRATION = OFF;
END
ELSE
BEGIN
    ALTER LOGIN [ventas_app2]
    WITH PASSWORD = N'Temporal#Ventas2026!',
         DEFAULT_DATABASE = [DBSISTEMA_VENTA],
         CHECK_POLICY = ON,
         CHECK_EXPIRATION = OFF;
    ALTER LOGIN [ventas_app2] ENABLE;
END;
GO

USE [DBSISTEMA_VENTA];
GO

IF USER_ID(N'ventas_app2') IS NULL
    CREATE USER [ventas_app2] FOR LOGIN [ventas_app2];
ELSE
    ALTER USER [ventas_app2] WITH LOGIN = [ventas_app2];
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[dbo] TO [ventas_app2];
GRANT EXECUTE ON SCHEMA::[dbo] TO [ventas_app2];
GO
