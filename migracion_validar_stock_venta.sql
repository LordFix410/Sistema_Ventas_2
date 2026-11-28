/* Ejecutar en DBSISTEMA_VENTA para permitir vender aunque el stock sea cero.
   Conserva la venta rápida F2, que usa temporalmente productos inactivos con código T.
   Las existencias pueden quedar negativas y se reponen al registrar compras. */
USE DBSISTEMA_VENTA;
GO

ALTER PROCEDURE dbo.usp_RegistrarVenta
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

        IF EXISTS
        (
            SELECT 1
            FROM @DetalleVenta dv
            LEFT JOIN dbo.PRODUCTO p WITH (UPDLOCK, HOLDLOCK) ON p.IdProducto = dv.IdProducto
            WHERE p.IdProducto IS NULL
               OR (ISNULL(p.Estado, 0) = 0 AND NOT (ISNULL(p.Codigo, '') LIKE 'T%' AND ISNULL(p.Descripcion, '') = 'ARTICULO TEMPORAL DE VENTA'))
        )
            THROW 50017, 'Uno o más productos ya no existen o están inactivos. Verifica el producto antes de registrar la venta.', 1;

        DECLARE @ProductosSolicitados int = (SELECT COUNT(DISTINCT IdProducto) FROM @DetalleVenta);
        DECLARE @ProductosActualizados int;
        UPDATE p
        SET p.Stock = p.Stock - dv.Cantidad
        FROM dbo.PRODUCTO p
        INNER JOIN @DetalleVenta dv ON dv.IdProducto = p.IdProducto
        WHERE (p.Estado = 1 OR (ISNULL(p.Codigo, '') LIKE 'T%' AND ISNULL(p.Descripcion, '') = 'ARTICULO TEMPORAL DE VENTA'));
        SET @ProductosActualizados = @@ROWCOUNT;
        IF @ProductosActualizados <> @ProductosSolicitados
            THROW 50015, 'Uno o más productos ya no existen o están inactivos', 1;

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
