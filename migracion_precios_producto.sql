/*
    Migración para bases DBSISTEMA_VENTA existentes.
    Los campos PrecioCompra y PrecioVenta ya existen en dbo.PRODUCTO; esta
    migración actualiza los procedimientos para que el mantenedor pueda guardarlos.
    Aplicar primero en una copia de la base y con respaldo reciente.
*/
USE DBSISTEMA_VENTA;
GO

IF OBJECT_ID(N'dbo.PRODUCTO', N'U') IS NULL
    THROW 51030, 'No existe dbo.PRODUCTO en DBSISTEMA_VENTA.', 1;
GO

CREATE OR ALTER PROCEDURE dbo.sp_RegistrarProducto
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

    IF @Porcentaje < 0 OR @PrecioCompra < 0 OR @PrecioVenta < 0
       OR (@PrecioCompra > 0 AND @PrecioVenta < @PrecioCompra)
       OR ABS(@PrecioVenta * 4 - ROUND(@PrecioVenta * 4, 0)) > 0.0001
    BEGIN
        SET @Mensaje = 'El costo, precio de venta o margen no son válidos';
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM dbo.PRODUCTO WHERE Codigo = @Codigo)
    BEGIN
        SET @Mensaje = 'Ya existe un producto con el mismo código';
        RETURN;
    END;

    INSERT INTO dbo.PRODUCTO
        (Codigo, Nombre, Descripcion, IdCategoria, PrecioCompra, PrecioVenta, Porcentaje,
         Estado, IdGrupoReposicion, CantidadMinima, CantidadMaxima, IdGrupoProveedor)
    VALUES
        (@Codigo, @Nombre, @Descripcion, @IdCategoria, @PrecioCompra, @PrecioVenta,
         CASE WHEN @PrecioCompra = 0 THEN @Porcentaje
              ELSE CEILING(((@PrecioVenta - @PrecioCompra) / @PrecioCompra) * 10000) / 10000 END,
         @Estado, @IdGrupoReposicion, @CantidadMinima, @CantidadMaxima, @IdGrupoProveedor);

    SET @Resultado = CONVERT(int, SCOPE_IDENTITY());
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ModificarProducto
    @IdProducto int,
    @Codigo varchar(20),
    @Nombre varchar(30),
    @Descripcion varchar(30),
    @IdCategoria int,
    @Porcentaje decimal(9,4),
    @PrecioCompra decimal(10,2),
    @PrecioVenta decimal(10,2),
    @Estado bit,
    @Resultado bit OUTPUT,
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

    IF @Porcentaje < 0 OR @PrecioCompra < 0 OR @PrecioVenta < 0
       OR (@PrecioCompra > 0 AND @PrecioVenta < @PrecioCompra)
       OR ABS(@PrecioVenta * 4 - ROUND(@PrecioVenta * 4, 0)) > 0.0001
    BEGIN
        SET @Mensaje = 'El costo, precio de venta o margen no son válidos';
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
        PrecioCompra = @PrecioCompra,
        PrecioVenta = @PrecioVenta,
        Porcentaje = CASE WHEN @PrecioCompra = 0 THEN @Porcentaje
                          ELSE CEILING(((@PrecioVenta - @PrecioCompra) / @PrecioCompra) * 10000) / 10000 END,
        Estado = @Estado,
        IdGrupoReposicion = @IdGrupoReposicion,
        CantidadMinima = @CantidadMinima,
        CantidadMaxima = @CantidadMaxima,
        IdGrupoProveedor = @IdGrupoProveedor
    WHERE IdProducto = @IdProducto;

    SET @Resultado = CASE WHEN @@ROWCOUNT = 1 THEN 1 ELSE 0 END;
    IF @Resultado = 0 SET @Mensaje = 'No se encontró el producto para modificar';
END;
GO

PRINT 'Procedimientos de productos actualizados para guardar costo y precio de venta.';
GO
