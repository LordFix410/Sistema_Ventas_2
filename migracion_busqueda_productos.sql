/*
    Optimiza la búsqueda de productos para bases ya instaladas.
    Ejecutar una vez en DBSISTEMA_VENTA con un usuario con permisos DDL.
    No modifica ni elimina productos.
*/
USE DBSISTEMA_VENTA;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.PRODUCTO')
      AND name = N'IX_PRODUCTO_Codigo_Estado'
)
    CREATE INDEX IX_PRODUCTO_Codigo_Estado ON dbo.PRODUCTO(Codigo, Estado);
GO

CREATE OR ALTER FUNCTION dbo.fn_NormalizarTextoBusqueda(@Texto varchar(500))
RETURNS varchar(500)
AS
BEGIN
    DECLARE @Resultado varchar(500) = UPPER(ISNULL(@Texto, '')) COLLATE Latin1_General_100_CI_AI;
    DECLARE @Posicion int;
    SET @Posicion = PATINDEX('%[^A-Z0-9]%', @Resultado COLLATE Latin1_General_100_CI_AI);
    WHILE @Posicion > 0
    BEGIN
        SET @Resultado = STUFF(@Resultado, @Posicion, 1, '');
        SET @Posicion = PATINDEX('%[^A-Z0-9]%', @Resultado COLLATE Latin1_General_100_CI_AI);
    END;
    RETURN @Resultado;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_BuscarProductoPorCodigo
    @Codigo varchar(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET @Codigo = LTRIM(RTRIM(@Codigo));
    IF @Codigo IS NULL OR @Codigo = '' RETURN;

    SELECT TOP (2) p.IdProducto, p.Codigo, p.Nombre, p.Descripcion,
           c.IdCategoria, c.Descripcion AS DescripcionCategoria, p.Stock,
           p.PrecioCompra, p.PrecioVenta, p.Porcentaje, p.Estado,
           p.IdGrupoReposicion,
           COALESCE(p.CantidadMinima, gr.CantidadMinima) AS CantidadMinima,
           COALESCE(p.CantidadMaxima, gr.CantidadMaxima) AS CantidadMaxima,
           p.IdGrupoProveedor, ISNULL(gr.Nombre, '') AS GrupoReposicion,
           ISNULL(gp.Nombre, '') AS GrupoProveedores
    FROM dbo.PRODUCTO p
    INNER JOIN dbo.CATEGORIA c ON c.IdCategoria = p.IdCategoria
    LEFT JOIN dbo.GRUPO_REPOSICION gr ON gr.IdGrupoReposicion = p.IdGrupoReposicion
    LEFT JOIN dbo.GRUPO_PROVEEDOR gp ON gp.IdGrupoProveedor = p.IdGrupoProveedor
    WHERE p.Codigo = @Codigo AND p.Estado = 1
    ORDER BY p.IdProducto;
END;
GO
