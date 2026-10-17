/*
  Migración para bases DBSISTEMA_VENTA existentes.
  Ejecutar una sola vez en DBSISTEMA_VENTA con un usuario administrador.
  No elimina compras ni modifica inventario histórico.
*/
USE DBSISTEMA_VENTA;
GO

IF COL_LENGTH('dbo.COMPRA', 'MontoNoInventariable') IS NULL
BEGIN
    ALTER TABLE dbo.COMPRA
    ADD MontoNoInventariable decimal(10,2) NOT NULL
        CONSTRAINT DF_COMPRA_MontoNoInventariable DEFAULT (0) WITH VALUES;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_RegistrarCompra
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
            THROW 50005, 'El total de la factura no coincide con los productos y el importe no inventariable', 1;

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
            THROW 50006, 'Uno o más productos de la compra ya no existen', 1;

        COMMIT TRANSACTION registro;
    END TRY
    BEGIN CATCH
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION registro;
    END CATCH
END;
GO
