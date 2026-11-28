/* Migración idempotente: corrección y anulación auditada de compras Forms.
   Ejecutar una sola vez por base existente. No borra compras ni detalles. */
USE DBSISTEMA_VENTA;
GO
IF COL_LENGTH('dbo.COMPRA','MontoNoInventariable') IS NULL
    ALTER TABLE dbo.COMPRA ADD MontoNoInventariable decimal(10,2) NOT NULL CONSTRAINT DF_COMPRA_MontoNoInventariable_Mig DEFAULT (0);
IF COL_LENGTH('dbo.COMPRA','NumeroFacturaProveedor') IS NULL
    ALTER TABLE dbo.COMPRA ADD NumeroFacturaProveedor varchar(100) NULL;
IF COL_LENGTH('dbo.COMPRA','Anulada') IS NULL
    ALTER TABLE dbo.COMPRA ADD Anulada bit NOT NULL CONSTRAINT DF_COMPRA_Anulada_Mig DEFAULT (0);
IF COL_LENGTH('dbo.COMPRA','FechaAnulacion') IS NULL
    ALTER TABLE dbo.COMPRA ADD FechaAnulacion datetime NULL;
IF COL_LENGTH('dbo.COMPRA','IdUsuarioAnulacion') IS NULL
    ALTER TABLE dbo.COMPRA ADD IdUsuarioAnulacion int NULL;
IF COL_LENGTH('dbo.COMPRA','MotivoAnulacion') IS NULL
    ALTER TABLE dbo.COMPRA ADD MotivoAnulacion varchar(500) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_COMPRA_UsuarioAnulacion')
    ALTER TABLE dbo.COMPRA ADD CONSTRAINT FK_COMPRA_UsuarioAnulacion FOREIGN KEY(IdUsuarioAnulacion) REFERENCES dbo.USUARIO(IdUsuario);
GO
IF TYPE_ID(N'dbo.ECompra_Correccion') IS NULL
    EXEC(N'CREATE TYPE dbo.ECompra_Correccion AS TABLE
    (
        IdDetalleCompra int NOT NULL,
        IdProductoActual int NOT NULL,
        IdProductoNuevo int NOT NULL,
        CantidadActual int NOT NULL,
        CantidadNueva int NOT NULL,
        PrecioCompraActual decimal(18,2) NOT NULL,
        PrecioCompraNuevo decimal(18,2) NOT NULL
    )');
GO
IF OBJECT_ID('dbo.COMPRA_CORRECCION','U') IS NULL
BEGIN
    CREATE TABLE dbo.COMPRA_CORRECCION
    (
        IdCorreccion int IDENTITY(1,1) PRIMARY KEY,
        IdCompra int NOT NULL REFERENCES dbo.COMPRA(IdCompra),
        IdUsuario int NOT NULL REFERENCES dbo.USUARIO(IdUsuario),
        Tipo varchar(12) NOT NULL,
        Motivo varchar(500) NOT NULL,
        IdProveedorAnterior int NULL,
        IdProveedorNuevo int NULL,
        NumeroFacturaAnterior varchar(100) NULL,
        NumeroFacturaNuevo varchar(100) NULL,
        MontoTotalAnterior decimal(18,2) NOT NULL,
        MontoTotalNuevo decimal(18,2) NOT NULL,
        FechaRegistro datetime NOT NULL CONSTRAINT DF_COMPRA_CORRECCION_Fecha DEFAULT (GETDATE())
    );
END;
IF OBJECT_ID('dbo.COMPRA_CORRECCION_DETALLE','U') IS NULL
BEGIN
    CREATE TABLE dbo.COMPRA_CORRECCION_DETALLE
    (
        IdAuditoriaDetalle int IDENTITY(1,1) PRIMARY KEY,
        IdCorreccion int NOT NULL REFERENCES dbo.COMPRA_CORRECCION(IdCorreccion),
        IdDetalleCompra int NOT NULL,
        IdProductoAnterior int NOT NULL,
        IdProductoNuevo int NOT NULL,
        CantidadAnterior int NOT NULL,
        CantidadNueva int NOT NULL,
        PrecioCompraAnterior decimal(18,2) NOT NULL,
        PrecioCompraNuevo decimal(18,2) NOT NULL,
        MontoAnterior decimal(18,2) NOT NULL,
        MontoNuevo decimal(18,2) NOT NULL
    );
END;
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
ALTER PROCEDURE dbo.sp_ReporteCompras
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
    WHERE c.Anulada = 0
      AND CONVERT(date, c.FechaRegistro) BETWEEN @fechainicio AND @fechafin
      AND pr.IdProveedor = IIF(@idproveedor = 0, pr.IdProveedor, @idproveedor);
END;
GO
ALTER PROCEDURE dbo.sp_ReporteReposicionPorProveedor
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
        SELECT p.IdProducto, p.Codigo, p.Equivalencia, p.Nombre,
               sg.IdGrupoReposicion, sg.GrupoReposicion,
               sg.StockActual, sg.CantidadMinima, sg.CantidadMaxima,
               sg.CantidadMinima - sg.StockActual AS CantidadFaltante,
               p.IdGrupoProveedor
        FROM StockGrupo sg
        INNER JOIN dbo.PRODUCTO p ON p.IdGrupoReposicion = sg.IdGrupoReposicion
        WHERE sg.StockActual < sg.CantidadMinima AND p.Estado = 1

        UNION ALL

        SELECT p.IdProducto, p.Codigo, p.Equivalencia, p.Nombre,
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
           pp.IdProducto, pp.Codigo, pp.Equivalencia, pp.Nombre AS Producto,
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
        WHERE dc.IdProducto = pp.IdProducto AND c.IdProveedor = pr.IdProveedor AND c.Anulada=0
        ORDER BY c.FechaRegistro DESC, dc.IdDetalleCompra DESC
    ) ultimo
    OUTER APPLY
    (
        SELECT TOP (1) dc.PrecioCompra, prh.RazonSocial, c.FechaRegistro AS FechaCompra
        FROM dbo.DETALLE_COMPRA dc
        INNER JOIN dbo.COMPRA c ON c.IdCompra = dc.IdCompra
        INNER JOIN dbo.PROVEEDOR prh ON prh.IdProveedor = c.IdProveedor
        WHERE dc.IdProducto = pp.IdProducto AND c.Anulada=0
        ORDER BY c.FechaRegistro DESC, dc.IdDetalleCompra DESC
    ) historico
    ORDER BY gp.Nombre, pr.RazonSocial, pp.GrupoReposicion, pp.Nombre;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_CorregirCompra
    @IdCompra int,
    @IdUsuario int,
    @IdProveedorNuevo int,
    @NumeroFacturaProveedorNuevo varchar(100),
    @Motivo varchar(500),
    @Detalle dbo.ECompra_Correccion READONLY,
    @Resultado bit OUTPUT,
    @Mensaje varchar(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @Resultado=0;
    SET @Mensaje='';
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @ProveedorAnterior int, @FacturaAnterior varchar(100),
                @MontoTotal decimal(18,2), @NoInventariable decimal(18,2), @IdCorreccion int;

        IF NULLIF(LTRIM(RTRIM(@Motivo)),'') IS NULL
            THROW 50201, 'Escribe el motivo de la corrección.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.USUARIO WHERE IdUsuario=@IdUsuario AND Estado=1)
            THROW 50202, 'El usuario que corrige la compra no está activo.', 1;
        SELECT @ProveedorAnterior=c.IdProveedor,@FacturaAnterior=c.NumeroFacturaProveedor,
               @MontoTotal=c.MontoTotal,@NoInventariable=c.MontoNoInventariable
        FROM dbo.COMPRA c WITH (UPDLOCK,HOLDLOCK) WHERE c.IdCompra=@IdCompra AND c.Anulada=0;
        IF @ProveedorAnterior IS NULL THROW 50203, 'No se encontró una compra activa para corregir.', 1;
        IF @IdProveedorNuevo<>@ProveedorAnterior AND NOT EXISTS
            (SELECT 1 FROM dbo.PROVEEDOR WHERE IdProveedor=@IdProveedorNuevo AND Estado=1)
            THROW 50204, 'Selecciona un proveedor activo.', 1;
        IF @IdProveedorNuevo<>@ProveedorAnterior AND EXISTS
            (SELECT 1 FROM dbo.FIN_MOVIMIENTO WHERE IdCompra=@IdCompra AND Tipo='Proveedor')
            THROW 50205, 'No se puede cambiar el proveedor porque ya hay pagos relacionados con esta compra.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.DETALLE_COMPRA WHERE IdCompra=@IdCompra)
            THROW 50206, 'La compra no tiene renglones para corregir.', 1;
        IF (SELECT COUNT(*) FROM @Detalle)<>(SELECT COUNT(*) FROM dbo.DETALLE_COMPRA WHERE IdCompra=@IdCompra)
            THROW 50207, 'Conserva todos los renglones de la compra.', 1;
        IF EXISTS (SELECT IdDetalleCompra FROM @Detalle GROUP BY IdDetalleCompra HAVING COUNT(*)>1)
            THROW 50208, 'Hay renglones duplicados en la corrección.', 1;
        IF EXISTS
        (
            SELECT 1 FROM dbo.DETALLE_COMPRA a WITH (UPDLOCK,HOLDLOCK)
            LEFT JOIN @Detalle d ON d.IdDetalleCompra=a.IdDetalleCompra
            WHERE a.IdCompra=@IdCompra AND
                (d.IdDetalleCompra IS NULL OR d.IdProductoActual<>a.IdProducto
                 OR d.CantidadActual<>a.Cantidad OR d.PrecioCompraActual<>a.PrecioCompra)
        ) THROW 50209, 'La compra cambió desde que la abriste. Búscala de nuevo y revisa los valores actuales.', 1;
        IF EXISTS
        (
            SELECT 1 FROM @Detalle d
            LEFT JOIN dbo.DETALLE_COMPRA a ON a.IdDetalleCompra=d.IdDetalleCompra AND a.IdCompra=@IdCompra
            LEFT JOIN dbo.PRODUCTO p ON p.IdProducto=d.IdProductoNuevo
            WHERE a.IdDetalleCompra IS NULL OR d.CantidadNueva<=0 OR d.PrecioCompraNuevo<0
                OR p.IdProducto IS NULL OR (d.IdProductoNuevo<>d.IdProductoActual AND p.Estado=0)
        ) THROW 50210, 'Revisa que los productos, cantidades y costos nuevos sean válidos.', 1;
        IF EXISTS (SELECT IdProductoNuevo FROM @Detalle GROUP BY IdProductoNuevo HAVING COUNT(*)>1)
            THROW 50211, 'No se puede repetir un producto en la misma compra.', 1;
        IF ABS(@MontoTotal-
            (SELECT SUM(CONVERT(decimal(18,2),ROUND(PrecioCompraNuevo*CantidadNueva,2))) FROM @Detalle)
            -@NoInventariable)>1.00
            THROW 50212, 'El total corregido difiere más de Q1.00 del total de factura.', 1;

        INSERT dbo.COMPRA_CORRECCION(IdCompra,IdUsuario,Tipo,Motivo,IdProveedorAnterior,IdProveedorNuevo,
            NumeroFacturaAnterior,NumeroFacturaNuevo,MontoTotalAnterior,MontoTotalNuevo)
        VALUES(@IdCompra,@IdUsuario,'CORRECCION',LEFT(LTRIM(RTRIM(@Motivo)),500),@ProveedorAnterior,@IdProveedorNuevo,
            @FacturaAnterior,NULLIF(LTRIM(RTRIM(@NumeroFacturaProveedorNuevo)),''),@MontoTotal,@MontoTotal);
        SET @IdCorreccion=CONVERT(int,SCOPE_IDENTITY());
        INSERT dbo.COMPRA_CORRECCION_DETALLE(IdCorreccion,IdDetalleCompra,IdProductoAnterior,IdProductoNuevo,
            CantidadAnterior,CantidadNueva,PrecioCompraAnterior,PrecioCompraNuevo,MontoAnterior,MontoNuevo)
        SELECT @IdCorreccion,a.IdDetalleCompra,a.IdProducto,d.IdProductoNuevo,a.Cantidad,d.CantidadNueva,
            a.PrecioCompra,d.PrecioCompraNuevo,a.MontoTotal,
            CONVERT(decimal(18,2),ROUND(d.PrecioCompraNuevo*d.CantidadNueva,2))
        FROM dbo.DETALLE_COMPRA a INNER JOIN @Detalle d ON d.IdDetalleCompra=a.IdDetalleCompra
        WHERE a.IdCompra=@IdCompra;

        DECLARE @Afectados TABLE(IdProducto int PRIMARY KEY);
        INSERT @Afectados SELECT IdProductoActual FROM @Detalle UNION SELECT IdProductoNuevo FROM @Detalle;
        ;WITH M AS
        (
            SELECT IdProductoActual IdProducto,-CantidadActual Cambio FROM @Detalle
            UNION ALL SELECT IdProductoNuevo,CantidadNueva FROM @Detalle
        ), A AS (SELECT IdProducto,SUM(Cambio) Cambio FROM M GROUP BY IdProducto)
        UPDATE p SET p.Stock=p.Stock+A.Cambio FROM dbo.PRODUCTO p INNER JOIN A ON A.IdProducto=p.IdProducto;

        UPDATE a
        SET a.IdProducto=d.IdProductoNuevo,a.Cantidad=d.CantidadNueva,a.PrecioCompra=d.PrecioCompraNuevo,
            a.MontoTotal=CONVERT(decimal(18,2),ROUND(d.PrecioCompraNuevo*d.CantidadNueva,2)),
            a.PrecioVenta=CASE WHEN d.IdProductoNuevo=d.IdProductoActual THEN a.PrecioVenta ELSE p.PrecioVenta END,
            a.Porcentaje=CASE WHEN d.IdProductoNuevo<>d.IdProductoActual THEN p.Porcentaje
                WHEN d.PrecioCompraNuevo<=0 THEN 0
                ELSE CEILING(((a.PrecioVenta-d.PrecioCompraNuevo)/d.PrecioCompraNuevo)*10000)/10000 END
        FROM dbo.DETALLE_COMPRA a INNER JOIN @Detalle d ON d.IdDetalleCompra=a.IdDetalleCompra
        INNER JOIN dbo.PRODUCTO p ON p.IdProducto=d.IdProductoNuevo WHERE a.IdCompra=@IdCompra;
        UPDATE dbo.COMPRA SET IdProveedor=@IdProveedorNuevo,
            NumeroFacturaProveedor=NULLIF(LTRIM(RTRIM(@NumeroFacturaProveedorNuevo)),'') WHERE IdCompra=@IdCompra;

        UPDATE p SET p.PrecioCompra=ISNULL(u.PrecioCompra,0),
            p.Porcentaje=CASE WHEN ISNULL(u.PrecioCompra,0)<=0 THEN 0
                ELSE CEILING(((p.PrecioVenta-u.PrecioCompra)/u.PrecioCompra)*10000)/10000 END
        FROM dbo.PRODUCTO p INNER JOIN @Afectados x ON x.IdProducto=p.IdProducto
        OUTER APPLY
        (
            SELECT TOP (1) dc.PrecioCompra FROM dbo.DETALLE_COMPRA dc
            INNER JOIN dbo.COMPRA c ON c.IdCompra=dc.IdCompra
            WHERE dc.IdProducto=p.IdProducto AND c.Anulada=0
            ORDER BY c.FechaRegistro DESC,dc.IdDetalleCompra DESC
        ) u;
        SET @Resultado=1;
        SET @Mensaje='Compra corregida y auditada. El inventario y costo vigente se actualizaron; se conservó el precio de venta.';
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
        SET @Resultado=0; SET @Mensaje=ERROR_MESSAGE();
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_AnularCompra
    @IdCompra int,
    @IdUsuario int,
    @Motivo varchar(500),
    @Resultado bit OUTPUT,
    @Mensaje varchar(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @Resultado=0; SET @Mensaje='';
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @Proveedor int,@Factura varchar(100),@Monto decimal(18,2),@IdCorreccion int;
        IF NULLIF(LTRIM(RTRIM(@Motivo)),'') IS NULL THROW 50301, 'Escribe el motivo de la anulación.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.USUARIO WHERE IdUsuario=@IdUsuario AND Estado=1)
            THROW 50302, 'El usuario que anula la compra no está activo.', 1;
        SELECT @Proveedor=IdProveedor,@Factura=NumeroFacturaProveedor,@Monto=MontoTotal
        FROM dbo.COMPRA WITH (UPDLOCK,HOLDLOCK) WHERE IdCompra=@IdCompra AND Anulada=0;
        IF @Proveedor IS NULL THROW 50303, 'No se encontró una compra activa para anular.', 1;
        IF EXISTS (SELECT 1 FROM dbo.FIN_MOVIMIENTO WHERE IdCompra=@IdCompra AND Tipo='Proveedor')
            THROW 50304, 'No se puede anular: hay pagos asociados a esta compra. Revisa esos pagos primero.', 1;
        INSERT dbo.COMPRA_CORRECCION(IdCompra,IdUsuario,Tipo,Motivo,IdProveedorAnterior,IdProveedorNuevo,
            NumeroFacturaAnterior,NumeroFacturaNuevo,MontoTotalAnterior,MontoTotalNuevo)
        VALUES(@IdCompra,@IdUsuario,'ANULACION',LEFT(LTRIM(RTRIM(@Motivo)),500),@Proveedor,@Proveedor,@Factura,@Factura,@Monto,@Monto);
        SET @IdCorreccion=CONVERT(int,SCOPE_IDENTITY());
        INSERT dbo.COMPRA_CORRECCION_DETALLE(IdCorreccion,IdDetalleCompra,IdProductoAnterior,IdProductoNuevo,
            CantidadAnterior,CantidadNueva,PrecioCompraAnterior,PrecioCompraNuevo,MontoAnterior,MontoNuevo)
        SELECT @IdCorreccion,IdDetalleCompra,IdProducto,IdProducto,Cantidad,0,PrecioCompra,PrecioCompra,MontoTotal,0
        FROM dbo.DETALLE_COMPRA WHERE IdCompra=@IdCompra;
        DECLARE @Afectados TABLE(IdProducto int PRIMARY KEY);
        INSERT @Afectados SELECT DISTINCT IdProducto FROM dbo.DETALLE_COMPRA WHERE IdCompra=@IdCompra;
        UPDATE p SET p.Stock=p.Stock-x.Cantidad
        FROM dbo.PRODUCTO p INNER JOIN
            (SELECT IdProducto,SUM(Cantidad) Cantidad FROM dbo.DETALLE_COMPRA WHERE IdCompra=@IdCompra GROUP BY IdProducto) x
            ON x.IdProducto=p.IdProducto;
        UPDATE dbo.COMPRA SET Anulada=1,FechaAnulacion=GETDATE(),IdUsuarioAnulacion=@IdUsuario,
            MotivoAnulacion=LEFT(LTRIM(RTRIM(@Motivo)),500) WHERE IdCompra=@IdCompra;
        UPDATE p SET p.PrecioCompra=ISNULL(u.PrecioCompra,0),
            p.Porcentaje=CASE WHEN ISNULL(u.PrecioCompra,0)<=0 THEN 0
                ELSE CEILING(((p.PrecioVenta-u.PrecioCompra)/u.PrecioCompra)*10000)/10000 END
        FROM dbo.PRODUCTO p INNER JOIN @Afectados x ON x.IdProducto=p.IdProducto
        OUTER APPLY
        (
            SELECT TOP (1) dc.PrecioCompra FROM dbo.DETALLE_COMPRA dc
            INNER JOIN dbo.COMPRA c ON c.IdCompra=dc.IdCompra
            WHERE dc.IdProducto=p.IdProducto AND c.Anulada=0
            ORDER BY c.FechaRegistro DESC,dc.IdDetalleCompra DESC
        ) u;
        SET @Resultado=1; SET @Mensaje='Compra anulada y auditada. El inventario se ajustó sin borrar el historial.';
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
        SET @Resultado=0; SET @Mensaje=ERROR_MESSAGE();
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ReactivarCompra
    @IdCompra int,
    @IdUsuario int,
    @Motivo varchar(500),
    @Resultado bit OUTPUT,
    @Mensaje varchar(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @Resultado=0; SET @Mensaje='';
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @Proveedor int,@Factura varchar(100),@Monto decimal(18,2),@IdCorreccion int;
        IF NULLIF(LTRIM(RTRIM(@Motivo)),'') IS NULL THROW 50401, 'Escribe el motivo de la restauración.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.USUARIO WHERE IdUsuario=@IdUsuario AND Estado=1)
            THROW 50402, 'El usuario que restaura la compra no está activo.', 1;
        SELECT @Proveedor=IdProveedor,@Factura=NumeroFacturaProveedor,@Monto=MontoTotal
        FROM dbo.COMPRA WITH (UPDLOCK,HOLDLOCK) WHERE IdCompra=@IdCompra AND Anulada=1;
        IF @Proveedor IS NULL THROW 50403, 'No se encontró una compra anulada para restaurar.', 1;
        IF EXISTS (SELECT 1 FROM dbo.FIN_MOVIMIENTO WHERE IdCompra=@IdCompra AND Tipo='Proveedor')
            THROW 50404, 'La compra tiene pagos relacionados. Revisa esos movimientos antes de restaurarla.', 1;
        INSERT dbo.COMPRA_CORRECCION(IdCompra,IdUsuario,Tipo,Motivo,IdProveedorAnterior,IdProveedorNuevo,
            NumeroFacturaAnterior,NumeroFacturaNuevo,MontoTotalAnterior,MontoTotalNuevo)
        VALUES(@IdCompra,@IdUsuario,'REACTIVACION',LEFT(LTRIM(RTRIM(@Motivo)),500),@Proveedor,@Proveedor,@Factura,@Factura,@Monto,@Monto);
        SET @IdCorreccion=CONVERT(int,SCOPE_IDENTITY());
        INSERT dbo.COMPRA_CORRECCION_DETALLE(IdCorreccion,IdDetalleCompra,IdProductoAnterior,IdProductoNuevo,
            CantidadAnterior,CantidadNueva,PrecioCompraAnterior,PrecioCompraNuevo,MontoAnterior,MontoNuevo)
        SELECT @IdCorreccion,IdDetalleCompra,IdProducto,IdProducto,0,Cantidad,PrecioCompra,PrecioCompra,0,MontoTotal
        FROM dbo.DETALLE_COMPRA WHERE IdCompra=@IdCompra;
        DECLARE @Afectados TABLE(IdProducto int PRIMARY KEY);
        INSERT @Afectados SELECT DISTINCT IdProducto FROM dbo.DETALLE_COMPRA WHERE IdCompra=@IdCompra;
        UPDATE p SET p.Stock=p.Stock+x.Cantidad
        FROM dbo.PRODUCTO p INNER JOIN
            (SELECT IdProducto,SUM(Cantidad) Cantidad FROM dbo.DETALLE_COMPRA WHERE IdCompra=@IdCompra GROUP BY IdProducto) x
            ON x.IdProducto=p.IdProducto;
        UPDATE dbo.COMPRA SET Anulada=0,FechaAnulacion=NULL,IdUsuarioAnulacion=NULL,MotivoAnulacion=NULL
        WHERE IdCompra=@IdCompra;
        UPDATE p SET p.PrecioCompra=ISNULL(u.PrecioCompra,0),
            p.Porcentaje=CASE WHEN ISNULL(u.PrecioCompra,0)<=0 THEN 0
                ELSE CEILING(((p.PrecioVenta-u.PrecioCompra)/u.PrecioCompra)*10000)/10000 END
        FROM dbo.PRODUCTO p INNER JOIN @Afectados x ON x.IdProducto=p.IdProducto
        OUTER APPLY
        (
            SELECT TOP (1) dc.PrecioCompra FROM dbo.DETALLE_COMPRA dc
            INNER JOIN dbo.COMPRA c ON c.IdCompra=dc.IdCompra
            WHERE dc.IdProducto=p.IdProducto AND c.Anulada=0
            ORDER BY c.FechaRegistro DESC,dc.IdDetalleCompra DESC
        ) u;
        SET @Resultado=1; SET @Mensaje='Compra restaurada y auditada. Sus unidades volvieron al inventario.';
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
        SET @Resultado=0; SET @Mensaje=ERROR_MESSAGE();
    END CATCH
END;
GO
GRANT EXECUTE, REFERENCES ON TYPE::dbo.ECompra_Correccion TO [ventas_app2];
GO
