/*
    Migración incremental para DBSISTEMA_VENTA.
    Ejecutar una vez sobre la base existente antes de usar la nueva versión.
    No modifica query.sql, que continúa siendo el script de instalación limpia.
*/
USE DBSISTEMA_VENTA;
GO

IF COL_LENGTH('dbo.PRODUCTO', 'Porcentaje') IS NULL
BEGIN
    ALTER TABLE dbo.PRODUCTO
        ADD Porcentaje decimal(9,4) NOT NULL
            CONSTRAINT DF_PRODUCTO_Porcentaje DEFAULT (0) WITH VALUES;
END;
GO

IF COL_LENGTH('dbo.DETALLE_COMPRA', 'Porcentaje') IS NULL
BEGIN
    ALTER TABLE dbo.DETALLE_COMPRA
        ADD Porcentaje decimal(9,4) NOT NULL
            CONSTRAINT DF_DETALLE_COMPRA_Porcentaje DEFAULT (0) WITH VALUES;
END;
GO

IF COL_LENGTH('dbo.DETALLE_VENTA', 'PrecioCompra') IS NULL
BEGIN
    ALTER TABLE dbo.DETALLE_VENTA ADD PrecioCompra decimal(18,2) NOT NULL
        CONSTRAINT DF_DETALLE_VENTA_PrecioCompra DEFAULT (0) WITH VALUES;
END;
IF COL_LENGTH('dbo.DETALLE_VENTA', 'Porcentaje') IS NULL
BEGIN
    ALTER TABLE dbo.DETALLE_VENTA ADD Porcentaje decimal(9,4) NOT NULL
        CONSTRAINT DF_DETALLE_VENTA_Porcentaje DEFAULT (0) WITH VALUES;
END;
GO

/* Recupera el margen deducible de los precios ya guardados. */
UPDATE dbo.PRODUCTO
SET Porcentaje = CEILING(((PrecioVenta - PrecioCompra) / NULLIF(PrecioCompra, 0)) * 10000) / 10000
WHERE PrecioCompra > 0 AND PrecioVenta >= PrecioCompra;

UPDATE dbo.DETALLE_COMPRA
SET Porcentaje = CEILING(((PrecioVenta - PrecioCompra) / NULLIF(PrecioCompra, 0)) * 10000) / 10000
WHERE PrecioCompra > 0 AND PrecioVenta >= PrecioCompra;

/* Para ventas antiguas usa la última compra registrada hasta la fecha de venta,
   cuando existe. Si no hay compra que permita conocer el costo, queda en cero. */
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
               OR PrecioVenta + 0.0001 < CEILING(PrecioCompra * (1 + Porcentaje / 100.0) * 4) / 4.0
        )
            THROW 50002, 'El detalle de compra contiene precios, porcentajes o cantidades inválidos', 1;

        IF EXISTS (SELECT IdProducto FROM @DetalleCompra GROUP BY IdProducto HAVING COUNT(*) > 1)
            THROW 50003, 'No se puede repetir un producto en el detalle de compra', 1;

        IF ABS(@MontoTotal - (SELECT SUM(MontoTotal) FROM @DetalleCompra)) > 0.01
            THROW 50004, 'El total de la compra no coincide con el detalle', 1;

        INSERT INTO dbo.COMPRA(IdUsuario, IdProveedor, TipoDocumento, NumeroDocumento, MontoTotal)
        VALUES(@IdUsuario, @IdProveedor, @TipoDocumento, @NumeroDocumento, @MontoTotal);
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
