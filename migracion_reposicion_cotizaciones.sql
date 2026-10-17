/*
    Migración para una DBSISTEMA_VENTA ya existente.
    Ejecutar una vez con una cuenta administradora de la base.
    No borra productos ni cambia stock, precios, compras o ventas existentes.

    Diseño:
      - Producto independiente: CantidadMinima y CantidadMaxima propios.
      - Producto agrupado: límites únicos en GRUPO_REPOSICION; se suma el stock
        de todos los productos activos del grupo.
      - IdGrupoProveedor del producto define el perfil permitido para cotizar.
      - El reporte muestra el último precio de compra con cada proveedor permitido
        y el último precio histórico del producto. No genera órdenes de compra.
*/
USE [DBSISTEMA_VENTA];
GO

IF OBJECT_ID(N'dbo.GRUPO_REPOSICION', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GRUPO_REPOSICION(
        IdGrupoReposicion int IDENTITY(1,1) NOT NULL CONSTRAINT PK_GRUPO_REPOSICION PRIMARY KEY,
        Nombre varchar(100) NOT NULL,
        CantidadMinima int NOT NULL,
        CantidadMaxima int NOT NULL,
        Activo bit NOT NULL CONSTRAINT DF_GRUPO_REPOSICION_Activo DEFAULT(1),
        FechaRegistro datetime2(0) NOT NULL CONSTRAINT DF_GRUPO_REPOSICION_Fecha DEFAULT(SYSDATETIME()),
        CONSTRAINT UQ_GRUPO_REPOSICION_Nombre UNIQUE(Nombre),
        CONSTRAINT CK_GRUPO_REPOSICION_Cantidades CHECK(CantidadMinima >= 0 AND CantidadMaxima >= CantidadMinima)
    );
END;
GO

IF OBJECT_ID(N'dbo.GRUPO_PROVEEDOR', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GRUPO_PROVEEDOR(
        IdGrupoProveedor int IDENTITY(1,1) NOT NULL CONSTRAINT PK_GRUPO_PROVEEDOR PRIMARY KEY,
        Nombre varchar(100) NOT NULL,
        Activo bit NOT NULL CONSTRAINT DF_GRUPO_PROVEEDOR_Activo DEFAULT(1),
        FechaRegistro datetime2(0) NOT NULL CONSTRAINT DF_GRUPO_PROVEEDOR_Fecha DEFAULT(SYSDATETIME()),
        CONSTRAINT UQ_GRUPO_PROVEEDOR_Nombre UNIQUE(Nombre)
    );
END;
GO

IF OBJECT_ID(N'dbo.GRUPO_PROVEEDOR_DETALLE', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GRUPO_PROVEEDOR_DETALLE(
        IdGrupoProveedor int NOT NULL REFERENCES dbo.GRUPO_PROVEEDOR(IdGrupoProveedor),
        IdProveedor int NOT NULL REFERENCES dbo.PROVEEDOR(IdProveedor),
        FechaRegistro datetime2(0) NOT NULL CONSTRAINT DF_GRUPO_PROVEEDOR_DETALLE_Fecha DEFAULT(SYSDATETIME()),
        CONSTRAINT PK_GRUPO_PROVEEDOR_DETALLE PRIMARY KEY(IdGrupoProveedor, IdProveedor)
    );
END;
GO

IF COL_LENGTH(N'dbo.PRODUCTO', N'IdGrupoReposicion') IS NULL
    ALTER TABLE dbo.PRODUCTO ADD IdGrupoReposicion int NULL;
IF COL_LENGTH(N'dbo.PRODUCTO', N'CantidadMinima') IS NULL
    ALTER TABLE dbo.PRODUCTO ADD CantidadMinima int NULL;
IF COL_LENGTH(N'dbo.PRODUCTO', N'CantidadMaxima') IS NULL
    ALTER TABLE dbo.PRODUCTO ADD CantidadMaxima int NULL;
IF COL_LENGTH(N'dbo.PRODUCTO', N'IdGrupoProveedor') IS NULL
    ALTER TABLE dbo.PRODUCTO ADD IdGrupoProveedor int NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PRODUCTO_GrupoReposicion')
    ALTER TABLE dbo.PRODUCTO ADD CONSTRAINT FK_PRODUCTO_GrupoReposicion
        FOREIGN KEY(IdGrupoReposicion) REFERENCES dbo.GRUPO_REPOSICION(IdGrupoReposicion);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PRODUCTO_GrupoProveedor')
    ALTER TABLE dbo.PRODUCTO ADD CONSTRAINT FK_PRODUCTO_GrupoProveedor
        FOREIGN KEY(IdGrupoProveedor) REFERENCES dbo.GRUPO_PROVEEDOR(IdGrupoProveedor);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_PRODUCTO_Reposicion')
    ALTER TABLE dbo.PRODUCTO ADD CONSTRAINT CK_PRODUCTO_Reposicion
    CHECK (
        (IdGrupoReposicion IS NOT NULL AND CantidadMinima IS NULL AND CantidadMaxima IS NULL)
        OR
        (IdGrupoReposicion IS NULL AND
            ((CantidadMinima IS NULL AND CantidadMaxima IS NULL)
             OR (CantidadMinima IS NOT NULL AND CantidadMaxima IS NOT NULL
                 AND CantidadMinima >= 0 AND CantidadMaxima >= CantidadMinima)))
    );
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PRODUCTO') AND name = N'IX_PRODUCTO_GrupoReposicion')
    CREATE INDEX IX_PRODUCTO_GrupoReposicion ON dbo.PRODUCTO(IdGrupoReposicion, Estado, Stock);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PRODUCTO') AND name = N'IX_PRODUCTO_GrupoProveedor')
    CREATE INDEX IX_PRODUCTO_GrupoProveedor ON dbo.PRODUCTO(IdGrupoProveedor, Estado);
GO

CREATE OR ALTER PROCEDURE dbo.sp_ReporteReposicionPorProveedor
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH StockGrupo AS
    (
        SELECT gr.IdGrupoReposicion, gr.Nombre AS GrupoReposicion,
               gr.CantidadMinima, gr.CantidadMaxima, SUM(p.Stock) AS StockActual
        FROM dbo.GRUPO_REPOSICION gr
        INNER JOIN dbo.PRODUCTO p ON p.IdGrupoReposicion = gr.IdGrupoReposicion
        WHERE gr.Activo = 1 AND p.Estado = 1
        GROUP BY gr.IdGrupoReposicion, gr.Nombre, gr.CantidadMinima, gr.CantidadMaxima
    ),
    ProductosPendientes AS
    (
        SELECT p.IdProducto, p.Codigo, p.Nombre,
               sg.IdGrupoReposicion, sg.GrupoReposicion, sg.StockActual,
               sg.CantidadMinima, sg.CantidadMaxima,
               sg.CantidadMinima - sg.StockActual AS CantidadFaltante,
               p.IdGrupoProveedor
        FROM StockGrupo sg
        INNER JOIN dbo.PRODUCTO p ON p.IdGrupoReposicion = sg.IdGrupoReposicion
        WHERE sg.StockActual < sg.CantidadMinima AND p.Estado = 1
        UNION ALL
        SELECT p.IdProducto, p.Codigo, p.Nombre,
               CAST(NULL AS int), CAST(NULL AS varchar(100)), p.Stock,
               p.CantidadMinima, p.CantidadMaxima,
               p.CantidadMinima - p.Stock, p.IdGrupoProveedor
        FROM dbo.PRODUCTO p
        WHERE p.Estado = 1 AND p.IdGrupoReposicion IS NULL
          AND p.CantidadMinima IS NOT NULL AND p.CantidadMaxima IS NOT NULL
          AND p.Stock < p.CantidadMinima
    )
    SELECT pp.IdGrupoReposicion, pp.GrupoReposicion,
           gp.IdGrupoProveedor, gp.Nombre AS GrupoProveedores,
           pr.IdProveedor, pr.RazonSocial AS Proveedor,
           pp.IdProducto, pp.Codigo, pp.Nombre AS Producto,
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
        SELECT TOP (1) dc.PrecioCompra, prv.RazonSocial, c.FechaRegistro AS FechaCompra
        FROM dbo.DETALLE_COMPRA dc
        INNER JOIN dbo.COMPRA c ON c.IdCompra = dc.IdCompra
        INNER JOIN dbo.PROVEEDOR prv ON prv.IdProveedor = c.IdProveedor
        WHERE dc.IdProducto = pp.IdProducto
        ORDER BY c.FechaRegistro DESC, dc.IdDetalleCompra DESC
    ) historico
    ORDER BY gp.Nombre, pr.RazonSocial, pp.GrupoReposicion, pp.Nombre;
END;
GO

/*
    Ejemplo de configuración (ajusta los nombres/IDs a tu base):

    INSERT dbo.GRUPO_REPOSICION(Nombre, CantidadMinima, CantidadMaxima)
    VALUES ('Lápiz económico', 20, 40);

    INSERT dbo.GRUPO_PROVEEDOR(Nombre) VALUES ('Papelería general');
    INSERT dbo.GRUPO_PROVEEDOR_DETALLE(IdGrupoProveedor, IdProveedor)
    SELECT gp.IdGrupoProveedor, pr.IdProveedor
    FROM dbo.GRUPO_PROVEEDOR gp
    CROSS JOIN dbo.PROVEEDOR pr
    WHERE gp.Nombre = 'Papelería general'
      AND pr.RazonSocial IN ('Fátima', 'Copia Lista');

    -- Grupo: los mínimos/máximos individuales quedan NULL.
    UPDATE dbo.PRODUCTO
    SET IdGrupoReposicion = (SELECT IdGrupoReposicion FROM dbo.GRUPO_REPOSICION WHERE Nombre = 'Lápiz económico'),
        IdGrupoProveedor = (SELECT IdGrupoProveedor FROM dbo.GRUPO_PROVEEDOR WHERE Nombre = 'Papelería general')
    WHERE Codigo IN ('CODIGO_A', 'CODIGO_B');

    -- Producto sin grupo: usa límites propios.
    UPDATE dbo.PRODUCTO
    SET CantidadMinima = 10, CantidadMaxima = 50,
        IdGrupoProveedor = (SELECT IdGrupoProveedor FROM dbo.GRUPO_PROVEEDOR WHERE Nombre = 'Papelería general')
    WHERE Codigo = 'FOLDER_MANILA';

    EXEC dbo.sp_ReporteReposicionPorProveedor;
*/
GO
