USE DBSISTEMA_VENTA
GO

ALTER PROCEDURE sp_RegistrarCompra(
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

ALTER PROCEDURE usp_RegistrarVenta(
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

		begin  transaction registro

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
