/*
    Actualiza únicamente el procedimiento usado por la página de productos.
    Ejecutar una sola vez en la base DBSISTEMA_VENTA.
    No borra ni modifica registros.
*/
USE DBSISTEMA_VENTA;
GO

ALTER PROCEDURE dbo.sp_RegistrarProducto
    @Codigo varchar(20),
    @Nombre varchar(30),
    @Descripcion varchar(30),
    @IdCategoria int,
    @Porcentaje decimal(9,4),
    @PrecioCompra decimal(10,2),
    @PrecioVenta decimal(10,2),
    @Estado bit,
    @Resultado int OUTPUT,
    @Mensaje varchar(500) OUTPUT,
    @IdGrupoReposicion int = NULL,
    @CantidadMinima int = NULL,
    @CantidadMaxima int = NULL,
    @IdGrupoProveedor int = NULL
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

    IF @PrecioCompra < 0 OR @PrecioVenta < 0
       OR (@PrecioCompra > 0 AND @PrecioVenta < @PrecioCompra)
       OR ABS(@PrecioVenta * 4 - ROUND(@PrecioVenta * 4, 0)) > 0.0001
    BEGIN
        SET @Mensaje = 'El costo y el precio de venta no son válidos';
        RETURN;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.PRODUCTO WHERE Codigo = @Codigo)
    BEGIN
        INSERT INTO dbo.PRODUCTO
            (Codigo, Nombre, Descripcion, IdCategoria, PrecioCompra, PrecioVenta, Porcentaje, Estado,
             IdGrupoReposicion, CantidadMinima, CantidadMaxima, IdGrupoProveedor)
        VALUES
            (@Codigo, @Nombre, @Descripcion, @IdCategoria, @PrecioCompra, @PrecioVenta,
             CASE WHEN @PrecioCompra = 0 THEN @Porcentaje
                  ELSE CEILING(((@PrecioVenta - @PrecioCompra) / @PrecioCompra) * 10000) / 10000 END,
             @Estado, @IdGrupoReposicion, @CantidadMinima, @CantidadMaxima, @IdGrupoProveedor);

        SET @Resultado = CONVERT(int, SCOPE_IDENTITY());
    END
    ELSE
        SET @Mensaje = 'Ya existe un producto con el mismo código';
END;
GO
