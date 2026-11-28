/* Ejecutar en DBSISTEMA_VENTA para agregar el campo de equivalencia. */
USE [DBSISTEMA_VENTA];
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.PRODUCTO', N'U') IS NULL
    THROW 51501, 'No existe dbo.PRODUCTO. Selecciona la base DBSISTEMA_VENTA.', 1;

IF COL_LENGTH(N'dbo.PRODUCTO', N'Equivalencia') IS NULL
    ALTER TABLE dbo.PRODUCTO ADD Equivalencia varchar(500) NULL;

COMMIT TRANSACTION;
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
    @ImagenRuta nvarchar(260) = NULL,
    @Equivalencia varchar(500) = NULL
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
    SET @Porcentaje = ISNULL(@Porcentaje, 0);
    IF @Estado = 1 AND @PrecioCompra = 0 AND @PrecioVenta = 0
    BEGIN
        SET @Mensaje = 'Ingresa costo o precio de venta para calcular el otro valor.';
        RETURN;
    END;
    IF @Estado = 1 AND @PrecioCompra = 0 AND @PrecioVenta > 0 AND @Porcentaje = 0
    BEGIN
        SET @Mensaje = 'Ingresa un margen mayor que cero para calcular el costo.';
        RETURN;
    END;
    IF @PrecioCompra = 0 AND @PrecioVenta > 0 AND @Porcentaje > 0
        SET @PrecioCompra = ROUND(@PrecioVenta / (1 + @Porcentaje / 100.0), 2);
    IF @PrecioCompra > 0 AND @PrecioVenta = 0
        SET @PrecioVenta = CONVERT(decimal(10,2), CEILING(@PrecioCompra * (1 + @Porcentaje / 100.0) * 4.0) / 4.0);
    IF @PrecioCompra < 0 OR @PrecioVenta < 0
       OR (@PrecioCompra > 0 AND @PrecioVenta < @PrecioCompra)
       OR ABS(@PrecioVenta * 4 - ROUND(@PrecioVenta * 4, 0)) > 0.0001
    BEGIN
        SET @Mensaje = 'El costo y el precio de venta no son válidos';
        RETURN;
    END;
    IF EXISTS (SELECT 1 FROM dbo.PRODUCTO WHERE Codigo = @Codigo)
    BEGIN
        SET @Mensaje = 'Ya existe un producto con el mismo código';
        RETURN;
    END;

    INSERT INTO dbo.PRODUCTO
        (Codigo, Nombre, Descripcion, Equivalencia, IdCategoria, PrecioCompra, PrecioVenta, Porcentaje, Estado,
         IdGrupoReposicion, CantidadMinima, CantidadMaxima, IdGrupoProveedor, ImagenRuta)
    VALUES
        (@Codigo, @Nombre, @Descripcion, @Equivalencia, @IdCategoria, @PrecioCompra, @PrecioVenta,
         CASE WHEN @PrecioCompra = 0 THEN @Porcentaje
              ELSE CEILING(((@PrecioVenta - @PrecioCompra) / @PrecioCompra) * 10000) / 10000 END,
         @Estado, @IdGrupoReposicion, @CantidadMinima, @CantidadMaxima, @IdGrupoProveedor, @ImagenRuta);
    SET @Resultado = SCOPE_IDENTITY();
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
    @ImagenRuta nvarchar(260) = NULL,
    @Equivalencia varchar(500) = NULL
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
    SET @Porcentaje = ISNULL(@Porcentaje, 0);
    IF @Estado = 1 AND @PrecioCompra = 0 AND @PrecioVenta = 0
    BEGIN
        SET @Mensaje = 'Ingresa costo o precio de venta para calcular el otro valor.';
        RETURN;
    END;
    IF @Estado = 1 AND @PrecioCompra = 0 AND @PrecioVenta > 0 AND @Porcentaje = 0
    BEGIN
        SET @Mensaje = 'Ingresa un margen mayor que cero para calcular el costo.';
        RETURN;
    END;
    IF @PrecioCompra = 0 AND @PrecioVenta > 0 AND @Porcentaje > 0
        SET @PrecioCompra = ROUND(@PrecioVenta / (1 + @Porcentaje / 100.0), 2);
    IF @PrecioCompra > 0 AND @PrecioVenta = 0
        SET @PrecioVenta = CONVERT(decimal(10,2), CEILING(@PrecioCompra * (1 + @Porcentaje / 100.0) * 4.0) / 4.0);
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
        Equivalencia = @Equivalencia,
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

CREATE OR ALTER PROCEDURE dbo.sp_BuscarProductoPorCodigo
    @Codigo varchar(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET @Codigo = LTRIM(RTRIM(@Codigo));
    IF @Codigo IS NULL OR @Codigo = '' RETURN;

    SELECT TOP (2) p.IdProducto, p.Codigo, p.Nombre, p.Descripcion, p.Equivalencia,
           c.IdCategoria, c.Descripcion AS DescripcionCategoria, p.Stock,
           p.PrecioCompra, p.PrecioVenta, p.Porcentaje, p.Estado,
           p.IdGrupoReposicion,
           COALESCE(p.CantidadMinima, gr.CantidadMinima) AS CantidadMinima,
           COALESCE(p.CantidadMaxima, gr.CantidadMaxima) AS CantidadMaxima,
           p.IdGrupoProveedor, ISNULL(gr.Nombre, '') AS GrupoReposicion,
           ISNULL(gp.Nombre, '') AS GrupoProveedores, p.ImagenRuta
    FROM dbo.PRODUCTO p
    INNER JOIN dbo.CATEGORIA c ON c.IdCategoria = p.IdCategoria
    LEFT JOIN dbo.GRUPO_REPOSICION gr ON gr.IdGrupoReposicion = p.IdGrupoReposicion
    LEFT JOIN dbo.GRUPO_PROVEEDOR gp ON gp.IdGrupoProveedor = p.IdGrupoProveedor
    WHERE p.Codigo = @Codigo AND p.Estado = 1
    ORDER BY p.IdProducto;
END;
GO

DECLARE @ParametrosRegistrar int =
    (SELECT COUNT(*) FROM sys.parameters WHERE object_id = OBJECT_ID(N'dbo.sp_RegistrarProducto'));
DECLARE @ParametrosModificar int =
    (SELECT COUNT(*) FROM sys.parameters WHERE object_id = OBJECT_ID(N'dbo.sp_ModificarProducto'));

IF @ParametrosRegistrar <> 16
    THROW 51502, 'sp_RegistrarProducto no tiene los 16 parámetros esperados. Revisa el resultado y la base de datos seleccionada.', 1;
IF @ParametrosModificar <> 17
    THROW 51503, 'sp_ModificarProducto no tiene los 17 parámetros esperados. Revisa el resultado y la base de datos seleccionada.', 1;
IF COL_LENGTH(N'dbo.PRODUCTO', N'Equivalencia') IS NULL
    THROW 51504, 'No se agregó PRODUCTO.Equivalencia. Confirma que ejecutaste esta migración en DBSISTEMA_VENTA.', 1;

SELECT N'Equivalencia de producto y procedimientos actualizados.' AS Resultado;
SELECT @@SERVERNAME AS Servidor, DB_NAME() AS BaseActual;
SELECT OBJECT_NAME(object_id) AS Procedimiento, parameter_id AS Orden, name AS Parametro
FROM sys.parameters
WHERE object_id IN
(
    OBJECT_ID(N'dbo.sp_RegistrarProducto'),
    OBJECT_ID(N'dbo.sp_ModificarProducto'),
    OBJECT_ID(N'dbo.sp_BuscarProductoPorCodigo')
)
ORDER BY Procedimiento, parameter_id;
