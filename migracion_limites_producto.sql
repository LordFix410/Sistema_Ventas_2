/*
    Amplía el nombre y la descripción del producto a varchar(500), y actualiza
    los procedimientos. No modifica ni elimina los registros existentes.
    Ejecutar en la base DBSISTEMA_VENTA con permisos para alterar procedimientos.
*/
USE DBSISTEMA_VENTA;
GO

IF OBJECT_ID(N'dbo.PRODUCTO', N'U') IS NULL
    THROW 50001, 'No existe dbo.PRODUCTO en DBSISTEMA_VENTA.', 1;
IF COL_LENGTH(N'dbo.PRODUCTO', N'Nombre') IS NULL OR COL_LENGTH(N'dbo.PRODUCTO', N'Descripcion') IS NULL
    THROW 50002, 'A dbo.PRODUCTO le falta la columna Nombre o Descripcion.', 1;
GO

IF COLUMNPROPERTY(OBJECT_ID(N'dbo.PRODUCTO'), 'Nombre', 'AllowsNull') = 1
    ALTER TABLE dbo.PRODUCTO ALTER COLUMN Nombre varchar(500) NULL;
ELSE
    ALTER TABLE dbo.PRODUCTO ALTER COLUMN Nombre varchar(500) NOT NULL;
GO

IF COLUMNPROPERTY(OBJECT_ID(N'dbo.PRODUCTO'), 'Descripcion', 'AllowsNull') = 1
    ALTER TABLE dbo.PRODUCTO ALTER COLUMN Descripcion varchar(500) NULL;
ELSE
    ALTER TABLE dbo.PRODUCTO ALTER COLUMN Descripcion varchar(500) NOT NULL;
GO

CREATE OR ALTER PROCEDURE dbo.sp_RegistrarProducto
    @Codigo varchar(50),
    @Nombre varchar(500),
    @Descripcion varchar(500),
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
    @IdGrupoProveedor int = NULL,
    @ImagenRuta nvarchar(260) = NULL
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
        INSERT INTO dbo.PRODUCTO(Codigo, Nombre, Descripcion, IdCategoria, PrecioCompra, PrecioVenta, Porcentaje, Estado,
            IdGrupoReposicion, CantidadMinima, CantidadMaxima, IdGrupoProveedor, ImagenRuta)
        VALUES(@Codigo, @Nombre, @Descripcion, @IdCategoria, @PrecioCompra, @PrecioVenta,
            CASE WHEN @PrecioCompra = 0 THEN @Porcentaje
                 ELSE CEILING(((@PrecioVenta - @PrecioCompra) / @PrecioCompra) * 10000) / 10000 END, @Estado,
            @IdGrupoReposicion, @CantidadMinima, @CantidadMaxima, @IdGrupoProveedor, @ImagenRuta);
        SET @Resultado = SCOPE_IDENTITY();
    END
    ELSE
        SET @Mensaje = 'Ya existe un producto con el mismo código';
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ModificarProducto
    @IdProducto int,
    @Codigo varchar(50),
    @Nombre varchar(500),
    @Descripcion varchar(500),
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
    @IdGrupoProveedor int = NULL,
    @ImagenRuta nvarchar(260) = NULL
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
        IdGrupoReposicion = @IdGrupoReposicion,
        CantidadMinima = @CantidadMinima,
        CantidadMaxima = @CantidadMaxima,
        IdGrupoProveedor = @IdGrupoProveedor,
        ImagenRuta = @ImagenRuta,
        Porcentaje = CASE WHEN @PrecioCompra = 0 THEN @Porcentaje
            ELSE CEILING(((@PrecioVenta - @PrecioCompra) / @PrecioCompra) * 10000) / 10000 END,
        Estado = @Estado
    WHERE IdProducto = @IdProducto;

    SET @Resultado = CASE WHEN @@ROWCOUNT = 1 THEN 1 ELSE 0 END;
    IF @Resultado = 0 SET @Mensaje = 'No se encontró el producto para modificar';
END;
GO
