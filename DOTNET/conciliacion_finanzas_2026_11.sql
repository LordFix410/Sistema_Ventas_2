/*
   CONCILIACIÓN SOLICITADA PARA NOVIEMBRE 2026

   Ejecutar SOLO después de migracion_finanzas_caja_cuentas.sql y tras revisar
   los importes de la pantalla de resultados. Este script:
   - Corrige el cierre temporal #7 de Q267.25 a Q117.50.
   - Asigna todas las salidas (#2 a #8, Q153) a Capital; las Q45 de #7 y #8
     también salieron de Capital, no de Ganancia/Personal.
   - Registra un ajuste único de conciliación de +Q108 a Capital, con nota;
     no es una venta, un depósito ni una salida.
   - No altera depósitos, comprobantes, fechas ni efectivo físico.

   Resultado esperado en el libro:
   ventas acumuladas Q1,450.50; capital generado Q901.00;
   ganancia generada y disponible Q549.50; disponible capital Q856.00.
*/
USE [DBSISTEMA_VENTA];
GO

SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.FIN_CIERRE_TEMPORAL', 'EfectivoInicial') IS NULL
       OR COL_LENGTH('dbo.FIN_MOVIMIENTO', 'CuentaOrigen') IS NULL
       OR OBJECT_ID('dbo.FIN_AJUSTE_CUENTA_TEMPORAL', 'U') IS NULL
        THROW 51100, 'Primero ejecuta migracion_finanzas_caja_cuentas.sql.', 1;

    IF EXISTS (SELECT 1 FROM dbo.FIN_CIERRE_CAJA)
        THROW 51101, 'Hay cierres del sistema normal; este ajuste temporal no debe mezclarse con ellos.', 1;
    IF EXISTS (SELECT 1 FROM dbo.PAGO_GASTO_PERSONAL)
        THROW 51102, 'Hay pagos personales registrados; revisa su cuenta de origen antes de conciliar.', 1;
    IF (SELECT COUNT(*) FROM dbo.FIN_MOVIMIENTO) <> 7
       OR (SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO) <> 153.00
        THROW 51103, 'Las salidas cambiaron desde la revisión. No se aplicó ningún ajuste; vuelve a revisar los movimientos.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.FIN_CIERRE_TEMPORAL
        WHERE IdCierreTemporal = 7 AND FechaCierre = '2026-11-16T22:40:00'
          AND Ventas = 267.25 AND CapitalAsignado = 166.00 AND PersonalAsignado = 101.25
    ) AND NOT EXISTS (
        SELECT 1 FROM dbo.FIN_CIERRE_TEMPORAL
        WHERE IdCierreTemporal = 7 AND FechaCierre = '2026-11-16T22:40:00'
          AND Ventas = 117.50 AND CapitalAsignado = 73.00 AND PersonalAsignado = 44.50
    )
        THROW 51104, 'El cierre #7 no coincide con el valor revisado ni con el valor ya conciliado.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.FIN_MOVIMIENTO WHERE IdMovimiento=2 AND Tipo='Empresa' AND Monto=35.00)
       OR NOT EXISTS (SELECT 1 FROM dbo.FIN_MOVIMIENTO WHERE IdMovimiento=3 AND Tipo='Empresa' AND Monto=35.00)
       OR NOT EXISTS (SELECT 1 FROM dbo.FIN_MOVIMIENTO WHERE IdMovimiento=4 AND Tipo='Empresa' AND Monto=14.00)
       OR NOT EXISTS (SELECT 1 FROM dbo.FIN_MOVIMIENTO WHERE IdMovimiento=5 AND Tipo='Empresa' AND Monto=12.00)
       OR NOT EXISTS (SELECT 1 FROM dbo.FIN_MOVIMIENTO WHERE IdMovimiento=6 AND Tipo='Empresa' AND Monto=12.00)
        THROW 51105, 'No se encontraron las cinco salidas confirmadas de Q108.', 1;
    IF (SELECT COUNT(*) FROM dbo.FIN_MOVIMIENTO WHERE IdMovimiento IN (2,3,4,5,6) AND CuentaOrigen='Capital') <> 5
        THROW 51109, 'Las cinco salidas confirmadas no están todas asignadas a Capital.', 1;
    IF (SELECT COUNT(*) FROM dbo.FIN_MOVIMIENTO
        WHERE IdMovimiento = 7 AND Monto = 35.00 AND Concepto = '50 papel ladrillo') <> 1
       OR (SELECT COUNT(*) FROM dbo.FIN_MOVIMIENTO
        WHERE IdMovimiento = 8 AND Monto = 10.00 AND Concepto = 'Parqueo papel') <> 1
        THROW 51106, 'Las salidas #7 y #8 no coinciden con las dos salidas de Q45 revisadas.', 1;

    IF EXISTS (SELECT 1 FROM dbo.FIN_CIERRE_TEMPORAL WHERE IdCierreTemporal = 7 AND Ventas = 267.25)
        UPDATE dbo.FIN_CIERRE_TEMPORAL
           SET Ventas = 117.50,
               CapitalAsignado = 73.00,
               PersonalAsignado = 44.50
         WHERE IdCierreTemporal = 7;

    IF (SELECT COUNT(*) FROM dbo.FIN_MOVIMIENTO
        WHERE IdMovimiento IN (7,8) AND CuentaOrigen = 'Personal') = 2
        UPDATE dbo.FIN_MOVIMIENTO SET CuentaOrigen = 'Capital'
         WHERE IdMovimiento IN (7,8);
    ELSE IF (SELECT COUNT(*) FROM dbo.FIN_MOVIMIENTO
        WHERE IdMovimiento IN (7,8) AND CuentaOrigen = 'Capital') <> 2
        THROW 51107, 'Las salidas #7 y #8 tienen cuentas mezcladas; revísalas antes de continuar.', 1;

    IF EXISTS (SELECT 1 FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL
               WHERE Clave='CONCILIACION_2026_11_CAPITAL_108'
                 AND (Cuenta<>'Capital' OR Monto<>108.00))
        THROW 51111, 'La clave del ajuste ya existe con datos distintos. Revisa el ajuste antes de continuar.', 1;
    IF EXISTS (SELECT 1 FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL
               WHERE Clave<>'CONCILIACION_2026_11_CAPITAL_108')
        THROW 51112, 'Ya existen otros ajustes financieros. Revísalos antes de aplicar este cuadre.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL
                   WHERE Clave='CONCILIACION_2026_11_CAPITAL_108')
        INSERT dbo.FIN_AJUSTE_CUENTA_TEMPORAL(Clave,Cuenta,Monto,Concepto,Fecha,IdUsuario)
        VALUES('CONCILIACION_2026_11_CAPITAL_108','Capital',108.00,
               'AJUSTE DE CONCILIACION: se agrega Q108 al saldo de capital para cuadrarlo con el efectivo contado. No corresponde a una venta ni a un deposito.',
               SYSDATETIME(),NULL);

    IF (SELECT SUM(Ventas) FROM dbo.FIN_CIERRE_TEMPORAL) <> 1450.50
       OR (SELECT SUM(CapitalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL) <> 901.00
       OR (SELECT SUM(PersonalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL) <> 549.50
        THROW 51108, 'La distribución resultante no coincide con la conciliación acordada.', 1;
    IF (SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE CuentaOrigen='Capital') <> 153.00
       OR EXISTS (SELECT 1 FROM dbo.FIN_MOVIMIENTO WHERE IdMovimiento IN (7,8) AND CuentaOrigen <> 'Capital')
        THROW 51110, 'Las salidas de Capital no quedaron con la clasificación acordada.', 1;
    IF (SELECT SUM(Monto) FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL WHERE Cuenta='Capital') <> 108.00
        THROW 51113, 'El ajuste total de Capital no coincide con los Q108 documentados.', 1;

    COMMIT TRANSACTION;

    SELECT
        CAST((SELECT SUM(Ventas) FROM dbo.FIN_CIERRE_TEMPORAL) AS decimal(12,2)) VentasReconciliadas,
        CAST((SELECT SUM(CapitalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL) AS decimal(12,2)) CapitalGenerado,
        CAST((SELECT SUM(PersonalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL) AS decimal(12,2)) GananciaGenerada,
        CAST((SELECT SUM(CapitalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL)
           + (SELECT SUM(Monto) FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL WHERE Cuenta='Capital')
           - (SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE CuentaOrigen='Capital') AS decimal(12,2)) CapitalDisponible,
        CAST((SELECT SUM(PersonalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL)
           - (SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE CuentaOrigen='Personal') AS decimal(12,2)) GananciaDisponible,
        CAST(108.00 AS decimal(12,2)) AjusteConciliacionCapital,
        CAST((SELECT SUM(Monto) FROM dbo.FIN_DEPOSITO_TEMPORAL WHERE Cuenta='Capital') AS decimal(12,2)) DepositosCapitalConComprobante;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
