/*
    Migración para una DBSISTEMA_VENTA que ya está en producción.
    No ejecutar query.sql sobre la base existente: ese archivo es de instalación limpia.
    Esta migración asegura que exista un cliente CF activo para las ventas.
*/
USE DBSISTEMA_VENTA;
GO

IF OBJECT_ID(N'dbo.CLIENTE', N'U') IS NULL
    THROW 51020, 'No existe la tabla dbo.CLIENTE en DBSISTEMA_VENTA.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @CantidadCF int;
    DECLARE @IdClienteCF int;

    SELECT @CantidadCF = COUNT(*), @IdClienteCF = MIN(IdCliente)
    FROM dbo.CLIENTE WITH (UPDLOCK, HOLDLOCK)
    WHERE UPPER(LTRIM(RTRIM(Documento))) = 'CF';

    IF @CantidadCF > 1
        THROW 51021, 'Hay más de un cliente con documento CF. Revisa los duplicados antes de continuar.', 1;

    IF @CantidadCF = 0
    BEGIN
        INSERT INTO dbo.CLIENTE(Documento, NombreCompleto, Correo, Telefono, Estado)
        VALUES ('CF', 'Consumidor Final', 'cf@gmail.com', '00000000', 1);
    END
    ELSE
    BEGIN
        -- Mantiene sus datos existentes y solo garantiza que el CF predeterminado esté activo.
        UPDATE dbo.CLIENTE SET Estado = 1 WHERE IdCliente = @IdClienteCF AND Estado = 0;
    END;

    COMMIT TRANSACTION;
    PRINT 'Cliente CF disponible y activo.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
