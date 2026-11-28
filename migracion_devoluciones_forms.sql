/* Ejecutar una sola vez en DBSISTEMA_VENTA antes de usar Devoluciones en Forms.
   Registra devoluciones/anulaciones, audita el usuario y repone stock en una transacción.
   El reembolso queda como referencia para el cierre manual; no modifica Finanzas web. */
USE DBSISTEMA_VENTA;
GO

IF OBJECT_ID(N'dbo.DEVOLUCION_VENTA', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DEVOLUCION_VENTA
    (
        IdDevolucion int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DEVOLUCION_VENTA PRIMARY KEY,
        IdVenta int NOT NULL CONSTRAINT FK_DEVOLUCION_VENTA_VENTA REFERENCES dbo.VENTA(IdVenta),
        IdUsuario int NOT NULL CONSTRAINT FK_DEVOLUCION_VENTA_USUARIO REFERENCES dbo.USUARIO(IdUsuario),
        Tipo varchar(15) NOT NULL,
        Motivo varchar(250) NOT NULL,
        MontoDevuelto decimal(18,2) NOT NULL,
        FechaRegistro datetime2(0) NOT NULL CONSTRAINT DF_DEVOLUCION_VENTA_Fecha DEFAULT(SYSDATETIME()),
        CONSTRAINT CK_DEVOLUCION_VENTA_Tipo CHECK(Tipo IN ('PARCIAL','ANULACION')),
        CONSTRAINT CK_DEVOLUCION_VENTA_Monto CHECK(MontoDevuelto >= 0),
        CONSTRAINT CK_DEVOLUCION_VENTA_Motivo CHECK(LEN(LTRIM(RTRIM(Motivo))) > 0)
    );
END;
GO
IF OBJECT_ID(N'dbo.DEVOLUCION_VENTA', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.DEVOLUCION_VENTA') AND name=N'IX_DEVOLUCION_VENTA_VentaFecha')
    CREATE INDEX IX_DEVOLUCION_VENTA_VentaFecha ON dbo.DEVOLUCION_VENTA(IdVenta, FechaRegistro);
GO

IF OBJECT_ID(N'dbo.DETALLE_DEVOLUCION_VENTA', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DETALLE_DEVOLUCION_VENTA
    (
        IdDetalleDevolucion int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DETALLE_DEVOLUCION_VENTA PRIMARY KEY,
        IdDevolucion int NOT NULL CONSTRAINT FK_DETALLE_DEVOLUCION_CABECERA REFERENCES dbo.DEVOLUCION_VENTA(IdDevolucion),
        IdDetalleVenta int NOT NULL CONSTRAINT FK_DETALLE_DEVOLUCION_VENTA REFERENCES dbo.DETALLE_VENTA(IdDetalleVenta),
        Cantidad int NOT NULL,
        PrecioVenta decimal(18,2) NOT NULL,
        SubTotal decimal(18,2) NOT NULL,
        CONSTRAINT CK_DETALLE_DEVOLUCION_CANTIDAD CHECK(Cantidad > 0),
        CONSTRAINT CK_DETALLE_DEVOLUCION_PRECIOS CHECK(PrecioVenta >= 0 AND SubTotal >= 0)
    );
END;
GO
IF OBJECT_ID(N'dbo.DETALLE_DEVOLUCION_VENTA', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.DETALLE_DEVOLUCION_VENTA') AND name=N'IX_DETALLE_DEVOLUCION_VENTA_Detalle')
    CREATE INDEX IX_DETALLE_DEVOLUCION_VENTA_Detalle ON dbo.DETALLE_DEVOLUCION_VENTA(IdDetalleVenta);
GO

IF TYPE_ID(N'dbo.EDetalle_DevolucionVenta') IS NULL
    EXEC(N'CREATE TYPE dbo.EDetalle_DevolucionVenta AS TABLE
    (
        IdDetalleVenta int NOT NULL PRIMARY KEY,
        Cantidad int NOT NULL
    );');
GO

IF DATABASE_PRINCIPAL_ID(N'ventas_app2') IS NOT NULL
    EXEC(N'GRANT EXECUTE, REFERENCES ON TYPE::dbo.EDetalle_DevolucionVenta TO [ventas_app2];');
GO

CREATE OR ALTER PROCEDURE dbo.usp_RegistrarDevolucionVenta
    @IdVenta int,
    @IdUsuario int,
    @Tipo varchar(15),
    @Motivo varchar(250),
    @MontoReembolsado decimal(18,2),
    @Detalle dbo.EDetalle_DevolucionVenta READONLY,
    @Resultado bit OUTPUT,
    @Mensaje varchar(500) OUTPUT,
    @MontoDevuelto decimal(18,2) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @Resultado = 0;
    SET @Mensaje = '';
    SET @MontoDevuelto = 0;
    DECLARE @ValorProductos decimal(18,2)=0;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @Tipo NOT IN ('PARCIAL','ANULACION')
            THROW 50101, 'Tipo de devolución no válido.', 1;
        IF NULLIF(LTRIM(RTRIM(@Motivo)), '') IS NULL
            THROW 50102, 'Escribe el motivo de la devolución o anulación.', 1;
        IF NOT EXISTS (SELECT 1 FROM @Detalle)
            THROW 50103, 'Selecciona al menos un producto y una cantidad.', 1;
        IF EXISTS (SELECT 1 FROM @Detalle WHERE Cantidad <= 0)
            THROW 50104, 'Las cantidades devueltas deben ser mayores que cero.', 1;

        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.VENTA WITH (UPDLOCK, HOLDLOCK)
            WHERE IdVenta=@IdVenta
        )
            THROW 50105, 'No se encontró la venta seleccionada.', 1;

        IF NOT EXISTS (SELECT 1 FROM dbo.USUARIO WHERE IdUsuario=@IdUsuario AND Estado=1)
            THROW 50106, 'El usuario que registra la devolución no está activo.', 1;

        IF EXISTS
        (
            SELECT 1
            FROM @Detalle d
            LEFT JOIN dbo.DETALLE_VENTA dv WITH (UPDLOCK, HOLDLOCK)
                ON dv.IdDetalleVenta=d.IdDetalleVenta AND dv.IdVenta=@IdVenta
            OUTER APPLY
            (
                SELECT SUM(dd.Cantidad) AS CantidadDevuelta
                FROM dbo.DETALLE_DEVOLUCION_VENTA dd WITH (UPDLOCK, HOLDLOCK)
                WHERE dd.IdDetalleVenta=d.IdDetalleVenta
            ) prev
            WHERE dv.IdDetalleVenta IS NULL
               OR d.Cantidad > dv.Cantidad-ISNULL(prev.CantidadDevuelta,0)
        )
            THROW 50107, 'La cantidad supera lo que aún queda disponible para devolver en esta venta.', 1;

        IF @Tipo='ANULACION' AND EXISTS
        (
            SELECT 1
            FROM dbo.DETALLE_VENTA dv WITH (UPDLOCK, HOLDLOCK)
            OUTER APPLY
            (
                SELECT SUM(dd.Cantidad) AS CantidadDevuelta
                FROM dbo.DETALLE_DEVOLUCION_VENTA dd WITH (UPDLOCK, HOLDLOCK)
                WHERE dd.IdDetalleVenta=dv.IdDetalleVenta
            ) prev
            LEFT JOIN @Detalle d ON d.IdDetalleVenta=dv.IdDetalleVenta
            WHERE dv.IdVenta=@IdVenta
              AND ISNULL(d.Cantidad,0) <> dv.Cantidad-ISNULL(prev.CantidadDevuelta,0)
        )
            THROW 50108, 'Para anular la factura, incluye la cantidad pendiente de todos sus productos.', 1;

        SELECT @ValorProductos=CONVERT(decimal(18,2),SUM(CONVERT(decimal(18,2),d.Cantidad)*dv.PrecioVenta))
        FROM @Detalle d
        INNER JOIN dbo.DETALLE_VENTA dv ON dv.IdDetalleVenta=d.IdDetalleVenta AND dv.IdVenta=@IdVenta;

        IF @MontoReembolsado IS NULL OR @MontoReembolsado<0 OR @MontoReembolsado>@ValorProductos
            THROW 50109, 'El reembolso debe estar entre cero y el valor de los productos devueltos.', 1;
        SET @MontoDevuelto=@MontoReembolsado;

        INSERT dbo.DEVOLUCION_VENTA(IdVenta,IdUsuario,Tipo,Motivo,MontoDevuelto)
        VALUES(@IdVenta,@IdUsuario,@Tipo,LEFT(LTRIM(RTRIM(@Motivo)),250),@MontoDevuelto);

        DECLARE @IdDevolucion int=CONVERT(int,SCOPE_IDENTITY());
        INSERT dbo.DETALLE_DEVOLUCION_VENTA(IdDevolucion,IdDetalleVenta,Cantidad,PrecioVenta,SubTotal)
        SELECT @IdDevolucion,d.IdDetalleVenta,d.Cantidad,dv.PrecioVenta,
               CONVERT(decimal(18,2),d.Cantidad*dv.PrecioVenta)
        FROM @Detalle d
        INNER JOIN dbo.DETALLE_VENTA dv ON dv.IdDetalleVenta=d.IdDetalleVenta AND dv.IdVenta=@IdVenta;

        /* Los artículos F2 son renglones de venta sin inventario real. */
        UPDATE p
        SET p.Stock=p.Stock+d.Cantidad
        FROM dbo.PRODUCTO p
        INNER JOIN dbo.DETALLE_VENTA dv ON dv.IdProducto=p.IdProducto
        INNER JOIN @Detalle d ON d.IdDetalleVenta=dv.IdDetalleVenta
        WHERE dv.IdVenta=@IdVenta
          AND NOT (ISNULL(p.Codigo,'') LIKE 'T%' AND UPPER(ISNULL(p.Descripcion,''))='ARTICULO TEMPORAL DE VENTA');

        SET @Resultado=1;
        SET @Mensaje='Devolución registrada; revisa el monto para el cierre manual.';
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
        SET @Resultado=0;
        SET @Mensaje=ERROR_MESSAGE();
        SET @MontoDevuelto=0;
    END CATCH
END;
GO

IF DATABASE_PRINCIPAL_ID(N'ventas_app2') IS NOT NULL
    EXEC(N'GRANT EXECUTE ON OBJECT::dbo.usp_RegistrarDevolucionVenta TO [ventas_app2];');
GO
