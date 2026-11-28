/* Ejecutar en DBSISTEMA_VENTA sobre una base existente.
   Mantiene el margen en la misma escala que PRODUCTO.Porcentaje:
   0.61 representa 61%. También evita registrar compras con proveedor
   eliminado/inactivo o con productos que ya no están activos. */
USE DBSISTEMA_VENTA;
GO

IF COL_LENGTH('dbo.COMPRA','NumeroFacturaProveedor') IS NULL
    ALTER TABLE dbo.COMPRA ADD NumeroFacturaProveedor varchar(100) NULL;
GO

ALTER PROCEDURE dbo.sp_RegistrarCompra
    @IdUsuario int,
    @IdProveedor int,
    @TipoDocumento varchar(500),
    @NumeroDocumento varchar(500) OUTPUT,
    @MontoTotal decimal(18,2),
    @MontoNoInventariable decimal(18,2),
    @DetalleCompra dbo.EDetalle_Compra READONLY,
    @Resultado bit OUTPUT,
    @Mensaje varchar(500) OUTPUT,
    @NumeroFacturaProveedor varchar(100) = NULL
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
               OR (PrecioCompra > 0 AND ABS(Porcentaje - ((PrecioVenta - PrecioCompra) / PrecioCompra)) > 0.0001)
        )
            THROW 50002, 'El detalle de compra contiene precios, porcentajes o cantidades inválidos', 1;

        IF EXISTS (SELECT IdProducto FROM @DetalleCompra GROUP BY IdProducto HAVING COUNT(*) > 1)
            THROW 50003, 'No se puede repetir un producto en el detalle de compra', 1;

        IF @MontoNoInventariable IS NULL OR @MontoNoInventariable < 0
            THROW 50004, 'El importe no inventariable no es válido', 1;

        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.PROVEEDOR WITH (UPDLOCK, HOLDLOCK)
            WHERE IdProveedor = @IdProveedor AND Estado = 1
        )
            THROW 50006, 'Selecciona un proveedor activo para registrar la compra', 1;

        IF EXISTS
        (
            SELECT 1
            FROM @DetalleCompra dc
            LEFT JOIN dbo.PRODUCTO p WITH (UPDLOCK, HOLDLOCK) ON p.IdProducto = dc.IdProducto
            WHERE p.IdProducto IS NULL OR p.Estado = 0
        )
            THROW 50007, 'La compra contiene un producto que ya no existe o está inactivo. Reactívalo antes de registrar.', 1;

        IF ABS(@MontoTotal - (SELECT SUM(MontoTotal) FROM @DetalleCompra) - @MontoNoInventariable) > 1.00
            THROW 50004, 'El total de la factura no coincide con los productos y el importe no inventariable', 1;

        INSERT INTO dbo.COMPRA(IdUsuario, IdProveedor, TipoDocumento, NumeroDocumento, MontoTotal,
            MontoNoInventariable, NumeroFacturaProveedor)
        VALUES(@IdUsuario, @IdProveedor, @TipoDocumento, @NumeroDocumento, @MontoTotal,
            @MontoNoInventariable, NULLIF(LTRIM(RTRIM(@NumeroFacturaProveedor)), ''));
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
        INNER JOIN @DetalleCompra dc ON dc.IdProducto = p.IdProducto
        WHERE p.Estado = 1;

        IF @@ROWCOUNT <> (SELECT COUNT(*) FROM @DetalleCompra)
            THROW 50005, 'Uno o más productos de la compra ya no existen o están inactivos', 1;

        COMMIT TRANSACTION registro;
    END TRY
    BEGIN CATCH
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION registro;
    END CATCH
END;
GO
