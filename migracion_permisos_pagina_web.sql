/*
    Seguridad de acceso a páginas web desde la administración de usuarios de Forms.
    Ejecutar una vez sobre DBSISTEMA_VENTA. Es idempotente y conserva datos existentes.
*/
USE DBSISTEMA_VENTA;
GO

IF COL_LENGTH(N'dbo.USUARIO',N'EsAdministradorPrincipal') IS NULL
    ALTER TABLE dbo.USUARIO ADD EsAdministradorPrincipal bit NOT NULL
        CONSTRAINT DF_USUARIO_EsAdministradorPrincipal DEFAULT(0) WITH VALUES;
GO
IF COL_LENGTH(N'dbo.USUARIO',N'EsSuperAdministradorWeb') IS NULL
    ALTER TABLE dbo.USUARIO ADD EsSuperAdministradorWeb bit NOT NULL
        CONSTRAINT DF_USUARIO_EsSuperAdministradorWeb DEFAULT(0) WITH VALUES;
GO

IF OBJECT_ID(N'dbo.PAGINA_WEB',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PAGINA_WEB(
        CodigoPagina varchar(100) NOT NULL CONSTRAINT PK_PAGINA_WEB PRIMARY KEY,
        Nombre varchar(100) NOT NULL,
        Orden int NOT NULL,
        Activa bit NOT NULL CONSTRAINT DF_PAGINA_WEB_Activa DEFAULT(1)
    );
END;
GO

IF OBJECT_ID(N'dbo.PERMISO_PAGINA_WEB',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PERMISO_PAGINA_WEB(
        IdUsuario int NOT NULL CONSTRAINT FK_PERMISO_PAGINA_WEB_USUARIO REFERENCES dbo.USUARIO(IdUsuario),
        CodigoPagina varchar(100) NOT NULL CONSTRAINT FK_PERMISO_PAGINA_WEB_PAGINA REFERENCES dbo.PAGINA_WEB(CodigoPagina),
        FechaRegistro datetime NOT NULL CONSTRAINT DF_PERMISO_PAGINA_WEB_Fecha DEFAULT(getdate()),
        CONSTRAINT PK_PERMISO_PAGINA_WEB PRIMARY KEY(IdUsuario,CodigoPagina)
    );
END;
GO

MERGE dbo.PAGINA_WEB AS destino
USING (VALUES
 ('dashboard.html','Dashboard',1),
 ('finanzas.html','Cierres',2),
 ('depositos.html','Depósitos y transferencias',3),
 ('salidas.html','Salidas',4),
 ('conciliacion.html','Conciliación',5),
 ('productos.html','Productos',6),
 ('venta.html','Ventas web',7)
) AS origen(CodigoPagina,Nombre,Orden)
ON destino.CodigoPagina=origen.CodigoPagina
WHEN MATCHED THEN UPDATE SET Nombre=origen.Nombre,Orden=origen.Orden,Activa=1
WHEN NOT MATCHED THEN INSERT(CodigoPagina,Nombre,Orden) VALUES(origen.CodigoPagina,origen.Nombre,origen.Orden);
GO

/* Mantiene una cuenta principal única; prioriza la cuenta inicial 'admin'. */
IF NOT EXISTS(SELECT 1 FROM dbo.USUARIO WHERE EsAdministradorPrincipal=1)
BEGIN
    DECLARE @principalId int;
    SELECT TOP(1) @principalId=u.IdUsuario
    FROM dbo.USUARIO u INNER JOIN dbo.ROL r ON r.IdRol=u.IdRol
    WHERE u.Estado=1 AND UPPER(r.Descripcion)='ADMINISTRADOR'
    ORDER BY CASE WHEN LOWER(ISNULL(u.Documento,''))='admin' OR LOWER(ISNULL(u.Correo,''))='admin@sistema.com'
                       OR UPPER(ISNULL(u.NombreCompleto,''))='ADMINISTRADOR GENERAL' THEN 0 ELSE 1 END,u.IdUsuario;
    IF @principalId IS NULL
        THROW 51110,'No hay un usuario activo con rol ADMINISTRADOR para designar como administrador principal.',1;
    UPDATE dbo.USUARIO SET EsAdministradorPrincipal=1,EsSuperAdministradorWeb=1 WHERE IdUsuario=@principalId;
END;
ELSE
    UPDATE dbo.USUARIO SET EsSuperAdministradorWeb=1 WHERE EsAdministradorPrincipal=1;
GO

/* La migración mantiene acceso previo para administradores y limita empleados a ventas web. */
INSERT dbo.PERMISO_PAGINA_WEB(IdUsuario,CodigoPagina)
SELECT u.IdUsuario,p.CodigoPagina
FROM dbo.USUARIO u CROSS JOIN dbo.PAGINA_WEB p
WHERE u.Estado=1 AND (u.EsAdministradorPrincipal=1 OR EXISTS(
    SELECT 1 FROM dbo.ROL r WHERE r.IdRol=u.IdRol AND UPPER(r.Descripcion)='ADMINISTRADOR'))
  AND NOT EXISTS(SELECT 1 FROM dbo.PERMISO_PAGINA_WEB x WHERE x.IdUsuario=u.IdUsuario AND x.CodigoPagina=p.CodigoPagina);

INSERT dbo.PERMISO_PAGINA_WEB(IdUsuario,CodigoPagina)
SELECT u.IdUsuario,'venta.html'
FROM dbo.USUARIO u INNER JOIN dbo.ROL r ON r.IdRol=u.IdRol
WHERE u.Estado=1 AND UPPER(r.Descripcion)='EMPLEADO'
  AND NOT EXISTS(SELECT 1 FROM dbo.PERMISO_PAGINA_WEB x WHERE x.IdUsuario=u.IdUsuario AND x.CodigoPagina='venta.html');
GO

CREATE OR ALTER PROC dbo.SP_REGISTRARUSUARIO(
    @IdUsuarioActor int,
    @Documento varchar(50),
    @NombreCompleto varchar(100),
    @Correo varchar(100),
    @Clave varchar(100),
    @IdRol int,
    @Estado bit,
    @IdUsuarioResultado int OUTPUT,
    @Mensaje varchar(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET @IdUsuarioResultado=0; SET @Mensaje='';
    DECLARE @principal bit=0,@puede bit=0;
    SELECT @principal=EsAdministradorPrincipal,@puede=CASE WHEN Estado=1 AND (EsAdministradorPrincipal=1 OR EsSuperAdministradorWeb=1) THEN 1 ELSE 0 END
    FROM dbo.USUARIO WHERE IdUsuario=@IdUsuarioActor;
    IF ISNULL(@puede,0)=0 BEGIN SET @Mensaje='No tienes permiso para administrar usuarios.'; RETURN; END;
    IF @principal=0 AND NOT EXISTS(SELECT 1 FROM dbo.ROL WHERE IdRol=@IdRol AND UPPER(Descripcion)='EMPLEADO')
    BEGIN SET @Mensaje='Solo el administrador principal puede asignar el rol Administrador.'; RETURN; END;
    IF EXISTS(SELECT 1 FROM dbo.USUARIO WHERE Documento=@Documento)
    BEGIN SET @Mensaje='No se puede repetir el documento para más de un usuario'; RETURN; END;
    INSERT dbo.USUARIO(Documento,NombreCompleto,Correo,Clave,IdRol,Estado) VALUES(@Documento,@NombreCompleto,@Correo,@Clave,@IdRol,@Estado);
    SET @IdUsuarioResultado=CONVERT(int,SCOPE_IDENTITY());
END;
GO

IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.USUARIO') AND name=N'UX_USUARIO_AdministradorPrincipal')
    CREATE UNIQUE INDEX UX_USUARIO_AdministradorPrincipal ON dbo.USUARIO(EsAdministradorPrincipal) WHERE EsAdministradorPrincipal=1;
GO

CREATE OR ALTER PROC dbo.SP_EDITARUSUARIO(
    @IdUsuarioActor int,
    @IdUsuario int,
    @Documento varchar(50),
    @NombreCompleto varchar(100),
    @Correo varchar(100),
    @Clave varchar(100),
    @IdRol int,
    @Estado bit,
    @Respuesta bit OUTPUT,
    @Mensaje varchar(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET @Respuesta=0; SET @Mensaje='';
    DECLARE @principal bit=0,@puede bit=0,@objetivoPrincipal bit=0,@rolActual int,@estadoActual bit,@objetivoSuper bit=0;
    SELECT @principal=EsAdministradorPrincipal,@puede=CASE WHEN Estado=1 AND (EsAdministradorPrincipal=1 OR EsSuperAdministradorWeb=1) THEN 1 ELSE 0 END
    FROM dbo.USUARIO WHERE IdUsuario=@IdUsuarioActor;
    IF ISNULL(@puede,0)=0 BEGIN SET @Mensaje='No tienes permiso para administrar usuarios.'; RETURN; END;
    SELECT @objetivoPrincipal=EsAdministradorPrincipal,@objetivoSuper=EsSuperAdministradorWeb,@rolActual=IdRol,@estadoActual=Estado
    FROM dbo.USUARIO WHERE IdUsuario=@IdUsuario;
    IF @rolActual IS NULL BEGIN SET @Mensaje='No se encontró el usuario seleccionado.'; RETURN; END;
    IF @principal=0 AND @IdUsuario<>@IdUsuarioActor AND
       (@objetivoPrincipal=1 OR @objetivoSuper=1 OR EXISTS(SELECT 1 FROM dbo.ROL WHERE IdRol=@rolActual AND UPPER(Descripcion)='ADMINISTRADOR'))
    BEGIN SET @Mensaje='Solo el administrador principal puede modificar otro administrador.'; RETURN; END;
    IF @principal=0 AND NOT EXISTS(SELECT 1 FROM dbo.ROL WHERE IdRol=@IdRol AND UPPER(Descripcion)='EMPLEADO')
    BEGIN SET @Mensaje='Solo el administrador principal puede asignar el rol Administrador.'; RETURN; END;
    IF @IdUsuario=@IdUsuarioActor AND (@IdRol<>@rolActual OR @Estado=0)
    BEGIN SET @Mensaje='No puedes cambiar tu propio rol ni desactivar tu cuenta.'; RETURN; END;
    IF @objetivoPrincipal=1 AND (@IdRol<>@rolActual OR @Estado=0)
    BEGIN SET @Mensaje='La cuenta del administrador principal no se puede cambiar de rol ni desactivar.'; RETURN; END;
    IF EXISTS(SELECT 1 FROM dbo.USUARIO WHERE Documento=@Documento AND IdUsuario<>@IdUsuario)
    BEGIN SET @Mensaje='No se puede repetir el documento para más de un usuario'; RETURN; END;
    UPDATE dbo.USUARIO SET Documento=@Documento,NombreCompleto=@NombreCompleto,Correo=@Correo,Clave=@Clave,IdRol=@IdRol,Estado=@Estado
    WHERE IdUsuario=@IdUsuario;
    SET @Respuesta=1;
END;
GO

CREATE OR ALTER PROC dbo.SP_ELIMINARUSUARIO(
    @IdUsuarioActor int,
    @IdUsuario int,
    @Respuesta bit OUTPUT,
    @Mensaje varchar(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET @Respuesta=0; SET @Mensaje='';
    DECLARE @principal bit=0,@puede bit=0,@objetivoPrincipal bit=0,@objetivoSuper bit=0,@rolActual int;
    SELECT @principal=EsAdministradorPrincipal,@puede=CASE WHEN Estado=1 AND (EsAdministradorPrincipal=1 OR EsSuperAdministradorWeb=1) THEN 1 ELSE 0 END
    FROM dbo.USUARIO WHERE IdUsuario=@IdUsuarioActor;
    SELECT @objetivoPrincipal=EsAdministradorPrincipal,@objetivoSuper=EsSuperAdministradorWeb,@rolActual=IdRol FROM dbo.USUARIO WHERE IdUsuario=@IdUsuario;
    IF ISNULL(@puede,0)=0 OR @IdUsuario=@IdUsuarioActor OR @objetivoPrincipal=1 OR
       (@principal=0 AND (@objetivoSuper=1 OR EXISTS(SELECT 1 FROM dbo.ROL WHERE IdRol=@rolActual AND UPPER(Descripcion)='ADMINISTRADOR')))
    BEGIN SET @Mensaje='No tienes permiso para eliminar este usuario.'; RETURN; END;
    DECLARE @pasaReglas bit=1;
    IF EXISTS(SELECT 1 FROM dbo.COMPRA WHERE IdUsuario=@IdUsuario)
    BEGIN SET @pasaReglas=0; SET @Mensaje=@Mensaje+'No se puede eliminar porque el usuario se encuentra relacionado a una COMPRA. '; END;
    IF EXISTS(SELECT 1 FROM dbo.VENTA WHERE IdUsuario=@IdUsuario)
    BEGIN SET @pasaReglas=0; SET @Mensaje=@Mensaje+'No se puede eliminar porque el usuario se encuentra relacionado a una VENTA. '; END;
    IF @pasaReglas=1
    BEGIN
        DELETE FROM dbo.PERMISO_PAGINA_WEB WHERE IdUsuario=@IdUsuario;
        DELETE FROM dbo.USUARIO WHERE IdUsuario=@IdUsuario;
        SET @Respuesta=1;
    END;
END;
GO
