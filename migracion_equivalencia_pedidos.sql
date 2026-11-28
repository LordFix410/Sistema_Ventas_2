-- Ejecutar conectado a la base de datos de Sistema_Ventas.
-- Agrega la equivalencia al listado de reposición/cotizaciones para la app actual.
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
        WHERE dc.IdProducto = pp.IdProducto AND c.IdProveedor = pr.IdProveedor
        ORDER BY c.FechaRegistro DESC, dc.IdDetalleCompra DESC
    ) ultimo
    OUTER APPLY
    (
        SELECT TOP (1) dc.PrecioCompra, prh.RazonSocial, c.FechaRegistro AS FechaCompra
        FROM dbo.DETALLE_COMPRA dc
        INNER JOIN dbo.COMPRA c ON c.IdCompra = dc.IdCompra
        INNER JOIN dbo.PROVEEDOR prh ON prh.IdProveedor = c.IdProveedor
        WHERE dc.IdProducto = pp.IdProducto
        ORDER BY c.FechaRegistro DESC, dc.IdDetalleCompra DESC
    ) historico
    ORDER BY gp.Nombre, pr.RazonSocial, pp.GrupoReposicion, pp.Nombre;
END;
GO
