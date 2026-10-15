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
Stock int not null default 0,
PrecioCompra decimal(10,2) default 0,
PrecioVenta decimal(10,2) default 0,
Estado bit,
FechaRegistro datetime default getdate()
)
go

create table COMPRA(
IdCompra int primary key identity,
IdUsuario int references USUARIO(IdUsuario),
IdProveedor int references PROVEEDOR(IdProveedor),
TipoDocumento varchar(50),
NumeroDocumento varchar(50),
MontoTotal decimal(10,2),
FechaRegistro datetime default getdate()
)
go

create table DETALLE_COMPRA(
IdDetalleCompra int primary key identity,
IdCompra int references COMPRA(IdCompra),
IdProducto int references PRODUCTO(IdProducto),
PrecioCompra decimal(10,2) default 0,
PrecioVenta decimal(10,2) default 0,
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
PrecioVenta decimal(10,2),
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
-- CREACIÓN DE TIPOS DE DATOS PERSONALIZADOS
-- =============================================

CREATE TYPE [dbo].[EDetalle_Compra] AS TABLE(
	[IdProducto] int NULL,
	[PrecioCompra] decimal(18,2) NULL,
	[PrecioVenta] decimal(18,2) NULL,
	[Cantidad] int NULL,
	[MontoTotal] decimal(18,2) NULL
)
GO

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

-- PROCEDIMIENTO PARA REGISTRAR COMPRA
CREATE PROCEDURE sp_RegistrarCompra(
@IdUsuario int,
@IdProveedor int,
@TipoDocumento varchar(500),
@NumeroDocumento varchar(500) OUTPUT,
@MontoTotal decimal(18,2),
@DetalleCompra [EDetalle_Compra] READONLY,
@Resultado bit output,
@Mensaje varchar(500) output
)
as
begin
	begin try
		declare @idcompra int = 0
		set @Resultado = 1
		set @Mensaje = ''

		begin transaction registro

		DECLARE @SiguienteCompra bigint;
		SELECT @SiguienteCompra = ISNULL(MAX(TRY_CONVERT(bigint, NumeroDocumento)), 0) + 1
		FROM COMPRA WITH (TABLOCKX, HOLDLOCK);
		SET @NumeroDocumento = CASE WHEN @SiguienteCompra < 100000
			THEN RIGHT('00000' + CONVERT(varchar(20), @SiguienteCompra), 5)
			ELSE CONVERT(varchar(20), @SiguienteCompra) END;

		IF NOT EXISTS (SELECT 1 FROM @DetalleCompra)
			THROW 50001, 'La compra debe incluir al menos un producto', 1;

		IF EXISTS (SELECT 1 FROM @DetalleCompra WHERE IdProducto IS NULL OR Cantidad IS NULL OR Cantidad <= 0 OR PrecioCompra IS NULL OR PrecioVenta IS NULL OR MontoTotal IS NULL OR PrecioCompra < 0 OR PrecioVenta < 0 OR MontoTotal < 0 OR ABS(MontoTotal - ROUND(PrecioCompra * Cantidad, 2)) > 0.01)
			THROW 50002, 'El detalle de compra contiene cantidades o precios inválidos', 1;

		IF EXISTS (SELECT IdProducto FROM @DetalleCompra GROUP BY IdProducto HAVING COUNT(*) > 1)
			THROW 50003, 'No se puede repetir un producto en el detalle de compra', 1;

		IF ABS(@MontoTotal - (SELECT SUM(MontoTotal) FROM @DetalleCompra)) > 0.01
			THROW 50004, 'El total de la compra no coincide con el detalle', 1;

		insert into COMPRA(IdUsuario,IdProveedor,TipoDocumento,NumeroDocumento,MontoTotal)
		values(@IdUsuario,@IdProveedor,@TipoDocumento,@NumeroDocumento,@MontoTotal)

		set @idcompra = SCOPE_IDENTITY()

		insert into DETALLE_COMPRA(IdCompra,IdProducto,PrecioCompra,PrecioVenta,Cantidad,MontoTotal)
		select @idcompra,IdProducto,PrecioCompra,PrecioVenta,Cantidad,MontoTotal from @DetalleCompra

		update p set p.Stock = p.Stock + dc.Cantidad, 
		p.PrecioCompra = dc.PrecioCompra,
		p.PrecioVenta = dc.PrecioVenta
		from PRODUCTO p
		inner join @DetalleCompra dc on dc.IdProducto= p.IdProducto

		commit transaction registro
	end try
	begin catch
		set @Resultado = 0
		set @Mensaje = ERROR_MESSAGE()
		IF XACT_STATE() <> 0 ROLLBACK TRANSACTION registro
	end catch
end
GO

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
(1, 'menuAcercaDe')
GO

-- 3. Permisos para Empleado (IdRol = 2)
INSERT INTO PERMISO(IdRol, NombreMenu) values
(2, 'menuVentas'),
(2, 'menuClientes'),
(2, 'menuAcercaDe')
GO

-- 4. Usuarios (Admin con clave '123' como se solicitó)
insert into USUARIO(Documento, NombreCompleto, Correo, Clave, IdRol, Estado)
values ('admin', 'Administrador General', 'admin@sistema.com', '123', 1, 1)
GO

insert into USUARIO(Documento, NombreCompleto, Correo, Clave, IdRol, Estado)
values ('empleado1', 'Juan Pérez', 'empleado@sistema.com', '123', 2, 1)
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
-- VERIFICACIÓN DE CREDENCIALES
-- =============================================
SELECT documento, clave FROM USUARIO
GO