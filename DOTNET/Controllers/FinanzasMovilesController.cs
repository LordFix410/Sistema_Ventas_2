using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ImagenPDF.Controllers
{
    [ApiController]
    [Route("api/finanzas")]
    public sealed class FinanzasMovilesController : ControllerBase
    {
        private const decimal CapitalShare = 0.61m;
        private const decimal QuarterPeso = 0.25m;
        private const decimal MaxAmount = 999999999.99m;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FinanzasMovilesController> _logger;

        public FinanzasMovilesController(IConfiguration configuration, ILogger<FinanzasMovilesController> logger)
        { _configuration = configuration; _logger = logger; }

        private string? ConnectionString => _configuration.GetConnectionString("ProductDb");
        private int? UserId => HttpContext.Session.GetInt32("ProductUserId");
        private string UserName => HttpContext.Session.GetString("ProductUserName") ?? "Usuario";

        [HttpGet("resumen")]
        public async Task<IActionResult> Summary()
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "Inicia sesión para abrir Finanzas." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return StatusCode(503, new { mensaje = "Falta configurar la conexión de SQL Server." });
            var databaseName = "desconocida";
            try
            {
                await using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();
                databaseName = connection.Database;
                await EnsureCurrentPersonalPeriods(connection);

                var response = new FinanceSummary { Responsable = UserName, Fecha = DateTime.Today.ToString("yyyy-MM-dd"), Mes = DateTime.Today.ToString("yyyy-MM") };
                const string totalsSql = @"
DECLARE @inicio date=DATEFROMPARTS(YEAR(GETDATE()),MONTH(GETDATE()),1), @fin date=DATEADD(month,1,DATEFROMPARTS(YEAR(GETDATE()),MONTH(GETDATE()),1));
DECLARE @ultimoCierre datetime2(0)=(SELECT MAX(FechaCierre) FROM dbo.FIN_CIERRE_TEMPORAL);
IF @ultimoCierre IS NULL SET @ultimoCierre=CONVERT(datetime2(0),CONVERT(date,GETDATE()));
DECLARE @ultimaConciliacion datetime2(0)=(SELECT MAX(FechaRegistro) FROM dbo.FIN_CONCILIACION);
IF @ultimaConciliacion>@ultimoCierre SET @ultimoCierre=@ultimaConciliacion;
SELECT
  CAST(150.00+ISNULL((SELECT SUM(CapitalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL WHERE Cuenta='Capital'),0)+ISNULL((SELECT SUM(Ajuste) FROM dbo.FIN_CONCILIACION_DETALLE WHERE Fondo='Capital'),0)-ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE CuentaOrigen IN ('Capital','Caja') AND Anulado=0),0)-ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE CuentaOrigen='Capital' AND Anulado=0),0) AS decimal(12,2)) Capital,
 CAST(ISNULL((SELECT SUM(CapitalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL WHERE Cuenta='Capital'),0) AS decimal(12,2)) CapitalGeneradoTotal,
 CAST(100.00+ISNULL((SELECT SUM(PersonalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL WHERE Cuenta='Personal'),0)+ISNULL((SELECT SUM(Ajuste) FROM dbo.FIN_CONCILIACION_DETALLE WHERE Fondo='Ganancia'),0)-ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE CuentaOrigen='Personal' AND Anulado=0),0)-ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE CuentaOrigen='Personal' AND Anulado=0),0) AS decimal(12,2)) Personal,
 CAST(ISNULL((SELECT SUM(PersonalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL WHERE Cuenta='Personal'),0) AS decimal(12,2)) PersonalGeneradoTotal,
 CAST(ISNULL((SELECT SUM(b.SaldoCapitalInicial)+SUM(ISNULL(x.Depositos,0))-SUM(ISNULL(x.TransferenciasSalientes,0))+SUM(ISNULL(x.TransferenciasEntrantes,0))-SUM(ISNULL(x.Salidas,0))+SUM(ISNULL(x.Ajustes,0)) FROM dbo.FIN_BANCO_TEMPORAL b OUTER APPLY(SELECT (SELECT SUM(c.CapitalDepositado) FROM dbo.FIN_DEPOSITO_LOTE_CIERRE c JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=c.IdLote WHERE l.IdBanco=b.IdBanco AND l.Anulado=0) Depositos,(SELECT SUM(Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA WHERE IdBancoOrigen=b.IdBanco AND Fondo='Capital' AND AfectaSaldo=1) TransferenciasSalientes,(SELECT SUM(Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA WHERE IdBancoDestino=b.IdBanco AND Fondo='Capital' AND AfectaSaldo=1) TransferenciasEntrantes,ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE IdBanco=b.IdBanco AND CuentaOrigen IN ('Capital','Caja') AND Anulado=0),0)+ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE IdBanco=b.IdBanco AND CuentaOrigen='Capital' AND Anulado=0),0) Salidas,(SELECT SUM(Ajuste) FROM dbo.FIN_CONCILIACION_DETALLE WHERE IdBanco=b.IdBanco AND Fondo='Capital') Ajustes) x),0) AS decimal(12,2)) DepositadoCapital,
 CAST(ISNULL((SELECT SUM(b.SaldoGananciaInicial)+SUM(ISNULL(x.Depositos,0))-SUM(ISNULL(x.TransferenciasSalientes,0))+SUM(ISNULL(x.TransferenciasEntrantes,0))-SUM(ISNULL(x.Salidas,0))+SUM(ISNULL(x.Ajustes,0)) FROM dbo.FIN_BANCO_TEMPORAL b OUTER APPLY(SELECT (SELECT SUM(c.GananciaDepositada) FROM dbo.FIN_DEPOSITO_LOTE_CIERRE c JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=c.IdLote WHERE l.IdBanco=b.IdBanco AND l.Anulado=0) Depositos,(SELECT SUM(Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA WHERE IdBancoOrigen=b.IdBanco AND Fondo='Personal' AND AfectaSaldo=1) TransferenciasSalientes,(SELECT SUM(Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA WHERE IdBancoDestino=b.IdBanco AND Fondo='Personal' AND AfectaSaldo=1) TransferenciasEntrantes,ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE IdBanco=b.IdBanco AND CuentaOrigen='Personal' AND Anulado=0),0)+ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE IdBanco=b.IdBanco AND CuentaOrigen='Personal' AND Anulado=0),0) Salidas,(SELECT SUM(Ajuste) FROM dbo.FIN_CONCILIACION_DETALLE WHERE IdBanco=b.IdBanco AND Fondo='Ganancia') Ajustes) x),0) AS decimal(12,2)) DepositadoPersonal,
 CAST(150.00+ISNULL((SELECT SUM(CapitalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL WHERE Cuenta='Capital'),0)+ISNULL((SELECT SUM(Ajuste) FROM dbo.FIN_CONCILIACION_DETALLE WHERE Fondo='Capital'),0)-ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE CuentaOrigen IN ('Capital','Caja') AND Anulado=0),0)-ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE CuentaOrigen='Capital' AND Anulado=0),0) AS decimal(12,2)) CapitalDepositosMasCaja,
 CAST(100.00+ISNULL((SELECT SUM(PersonalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL WHERE Cuenta='Personal'),0)+ISNULL((SELECT SUM(Ajuste) FROM dbo.FIN_CONCILIACION_DETALLE WHERE Fondo='Ganancia'),0)-ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE CuentaOrigen='Personal' AND Anulado=0),0)-ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE CuentaOrigen='Personal' AND Anulado=0),0) AS decimal(12,2)) GananciaDepositosMasCaja,
 CAST(ISNULL((SELECT SUM(MontoProgramado) FROM dbo.GASTO_PERSONAL_PERIODO WHERE Anio=YEAR(GETDATE()) AND Mes=MONTH(GETDATE())),0) AS decimal(12,2)) ObligacionesMes,
 CAST(ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL p JOIN dbo.GASTO_PERSONAL_PERIODO per ON per.IdPeriodoGastoPersonal=p.IdPeriodoGastoPersonal WHERE per.Anio=YEAR(GETDATE()) AND per.Mes=MONTH(GETDATE())),0) AS decimal(12,2)) PagadoPersonal,
 CAST(ISNULL((SELECT SUM(PersonalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL WHERE FechaCierre>=@inicio AND FechaCierre<@fin),0)+ISNULL((SELECT SUM(GananciaGenerada) FROM dbo.FIN_CIERRE_CAJA WHERE FechaCierre>=@inicio AND FechaCierre<@fin),0) AS decimal(12,2)) AsignadoPersonalMes,
 CAST(ISNULL((SELECT SUM(Ventas) FROM dbo.FIN_CIERRE_TEMPORAL WHERE FechaCierre>=@inicio AND FechaCierre<@fin),0) AS decimal(12,2)) VentasManualMes,
 CAST(ISNULL((SELECT SUM(Ventas) FROM dbo.FIN_CIERRE_TEMPORAL WHERE FechaCierre>=@inicio AND FechaCierre<@fin),0) AS decimal(12,2)) VentasMesTotal,
 CAST(ISNULL((SELECT SUM(CapitalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL WHERE FechaCierre>=@inicio AND FechaCierre<@fin),0) AS decimal(12,2)) CapitalGeneradoMes,
  CAST(ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE CuentaOrigen='Capital' AND Anulado=0 AND Fecha>=@inicio AND Fecha<@fin),0)+ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE CuentaOrigen='Capital' AND Anulado=0 AND FechaPago>=@inicio AND FechaPago<@fin),0) AS decimal(12,2)) SalidasCapitalMes,
  CAST(ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE CuentaOrigen='Personal' AND Anulado=0 AND Fecha>=@inicio AND Fecha<@fin),0)+ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE CuentaOrigen='Personal' AND Anulado=0 AND FechaPago>=@inicio AND FechaPago<@fin),0) AS decimal(12,2)) SalidasPersonalMes,
 CAST(ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE IdBanco IS NULL AND OrigenEfectivo IN ('Caja','Capital','Ganancia') AND Anulado=0 AND FechaRegistro>@ultimoCierre),0)+ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE IdBanco IS NULL AND OrigenEfectivo IN ('Caja','Capital','Ganancia') AND Anulado=0 AND FechaRegistro>@ultimoCierre),0) AS decimal(12,2)) SalidasEfectivoPendientes,
  CAST(ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE IdBanco IS NULL AND OrigenEfectivo IN ('Caja','Capital','Ganancia') AND CuentaOrigen<>'Personal' AND Anulado=0 AND FechaRegistro>@ultimoCierre),0)+ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE IdBanco IS NULL AND OrigenEfectivo IN ('Caja','Capital','Ganancia') AND CuentaOrigen='Capital' AND Anulado=0 AND FechaRegistro>@ultimoCierre),0) AS decimal(12,2)) SalidasCapitalEfectivoPendientes,
  CAST(ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE IdBanco IS NULL AND OrigenEfectivo IN ('Caja','Capital','Ganancia') AND CuentaOrigen='Personal' AND Anulado=0 AND FechaRegistro>@ultimoCierre),0)+ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE IdBanco IS NULL AND OrigenEfectivo IN ('Caja','Capital','Ganancia') AND CuentaOrigen='Personal' AND Anulado=0 AND FechaRegistro>@ultimoCierre),0) AS decimal(12,2)) SalidasGananciaEfectivoPendientes,
 CAST(ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE PagadoDesdeCaja=1 AND OrigenEfectivo='Caja' AND Anulado=0 AND FechaRegistro>@ultimoCierre),0)+ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE PagadoDesdeCaja=1 AND OrigenEfectivo='Caja' AND Anulado=0 AND FechaRegistro>@ultimoCierre),0) AS decimal(12,2)) SalidasCajaVentasPendientes,
 (SELECT TOP (1) g.Nombre FROM dbo.GASTO_PERSONAL_PERIODO p JOIN dbo.GASTO_PERSONAL g ON g.IdGastoPersonal=p.IdGastoPersonal LEFT JOIN (SELECT IdPeriodoGastoPersonal,SUM(Monto) Pagado FROM dbo.PAGO_GASTO_PERSONAL GROUP BY IdPeriodoGastoPersonal) x ON x.IdPeriodoGastoPersonal=p.IdPeriodoGastoPersonal WHERE p.Anio=YEAR(GETDATE()) AND p.Mes=MONTH(GETDATE()) AND p.MontoProgramado>ISNULL(x.Pagado,0) AND g.Activo=1 ORDER BY p.FechaVencimiento,p.IdPeriodoGastoPersonal) ProximoGasto,
 (SELECT TOP (1) CONVERT(varchar(10),p.FechaVencimiento,23) FROM dbo.GASTO_PERSONAL_PERIODO p JOIN dbo.GASTO_PERSONAL g ON g.IdGastoPersonal=p.IdGastoPersonal LEFT JOIN (SELECT IdPeriodoGastoPersonal,SUM(Monto) Pagado FROM dbo.PAGO_GASTO_PERSONAL GROUP BY IdPeriodoGastoPersonal) x ON x.IdPeriodoGastoPersonal=p.IdPeriodoGastoPersonal WHERE p.Anio=YEAR(GETDATE()) AND p.Mes=MONTH(GETDATE()) AND p.MontoProgramado>ISNULL(x.Pagado,0) AND g.Activo=1 ORDER BY p.FechaVencimiento,p.IdPeriodoGastoPersonal) FechaProximoGasto;";
                await using (var command = new SqlCommand(totalsSql, connection))
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        response.Capital = Money(reader, "Capital"); response.CapitalGeneradoTotal = Money(reader, "CapitalGeneradoTotal"); response.Personal = Money(reader, "Personal"); response.PersonalGeneradoTotal = Money(reader, "PersonalGeneradoTotal");
                        response.DepositadoCapital = Money(reader, "DepositadoCapital"); response.DepositadoPersonal = Money(reader, "DepositadoPersonal");
                        response.CapitalDepositosMasCaja = Money(reader, "CapitalDepositosMasCaja"); response.GananciaDepositosMasCaja = Money(reader, "GananciaDepositosMasCaja");
                        response.ObligacionesMes = Money(reader, "ObligacionesMes"); response.PagadoPersonal = Money(reader, "PagadoPersonal");
                        response.AsignadoPersonalMes = Money(reader, "AsignadoPersonalMes"); response.VentasManualMes = Money(reader, "VentasManualMes");
                        response.VentasMesTotal = Money(reader, "VentasMesTotal"); response.CapitalGeneradoMes = Money(reader, "CapitalGeneradoMes");
                        response.SalidasCapitalMes = Money(reader, "SalidasCapitalMes"); response.SalidasPersonalMes = Money(reader, "SalidasPersonalMes");
                        response.SalidasEfectivoPendientes = Money(reader, "SalidasEfectivoPendientes");
                        response.SalidasCapitalEfectivoPendientes = Money(reader, "SalidasCapitalEfectivoPendientes");
                        response.SalidasGananciaEfectivoPendientes = Money(reader, "SalidasGananciaEfectivoPendientes");
                        response.SalidasCajaVentasPendientes = Money(reader, "SalidasCajaVentasPendientes");
                        response.ProximoGasto = reader.IsDBNull(reader.GetOrdinal("ProximoGasto")) ? "Sin pendientes" : reader.GetString(reader.GetOrdinal("ProximoGasto"));
                        response.FechaProximoGasto = reader.IsDBNull(reader.GetOrdinal("FechaProximoGasto")) ? "" : reader.GetString(reader.GetOrdinal("FechaProximoGasto"));
                    }
                }

                const string bankSummarySql = @"
DECLARE @corteId int=(SELECT MAX(IdConciliacion) FROM dbo.FIN_CONCILIACION),@corte datetime2(0);
SELECT @corte=FechaRegistro FROM dbo.FIN_CONCILIACION WHERE IdConciliacion=@corteId;
SELECT b.IdBanco,b.Nombre,cap.Capital,gan.Ganancia
FROM dbo.FIN_BANCO_TEMPORAL b
OUTER APPLY(SELECT COALESCE((SELECT TOP(1) d.SaldoReal FROM dbo.FIN_CONCILIACION_DETALLE d WHERE d.IdConciliacion=@corteId AND d.IdBanco=b.IdBanco AND d.Fondo='Capital'),b.SaldoCapitalInicial)
 +ISNULL((SELECT SUM(x.CapitalDepositado) FROM dbo.FIN_DEPOSITO_LOTE_CIERRE x JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=x.IdLote WHERE l.IdBanco=b.IdBanco AND l.Anulado=0 AND (@corte IS NULL OR l.FechaRegistro>@corte)),0)
 -ISNULL((SELECT SUM(Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA WHERE IdBancoOrigen=b.IdBanco AND Fondo='Capital' AND AfectaSaldo=1 AND (@corte IS NULL OR FechaRegistro>@corte)),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA WHERE IdBancoDestino=b.IdBanco AND Fondo='Capital' AND AfectaSaldo=1 AND (@corte IS NULL OR FechaRegistro>@corte)),0)
 -ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE IdBanco=b.IdBanco AND CuentaOrigen IN ('Capital','Caja') AND Anulado=0 AND (@corte IS NULL OR FechaRegistro>@corte)),0)-ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE IdBanco=b.IdBanco AND CuentaOrigen='Capital' AND Anulado=0 AND (@corte IS NULL OR FechaRegistro>@corte)),0) Capital) cap
OUTER APPLY(SELECT COALESCE((SELECT TOP(1) d.SaldoReal FROM dbo.FIN_CONCILIACION_DETALLE d WHERE d.IdConciliacion=@corteId AND d.IdBanco=b.IdBanco AND d.Fondo='Ganancia'),b.SaldoGananciaInicial)
 +ISNULL((SELECT SUM(x.GananciaDepositada) FROM dbo.FIN_DEPOSITO_LOTE_CIERRE x JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=x.IdLote WHERE l.IdBanco=b.IdBanco AND l.Anulado=0 AND (@corte IS NULL OR l.FechaRegistro>@corte)),0)
 -ISNULL((SELECT SUM(Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA WHERE IdBancoOrigen=b.IdBanco AND Fondo IN ('Personal','Ganancia') AND AfectaSaldo=1 AND (@corte IS NULL OR FechaRegistro>@corte)),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA WHERE IdBancoDestino=b.IdBanco AND Fondo IN ('Personal','Ganancia') AND AfectaSaldo=1 AND (@corte IS NULL OR FechaRegistro>@corte)),0)
 -ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE IdBanco=b.IdBanco AND CuentaOrigen='Personal' AND Anulado=0 AND (@corte IS NULL OR FechaRegistro>@corte)),0)-ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE IdBanco=b.IdBanco AND CuentaOrigen='Personal' AND Anulado=0 AND (@corte IS NULL OR FechaRegistro>@corte)),0) Ganancia) gan
WHERE b.Activo=1 ORDER BY b.Nombre;";
                await using (var command = new SqlCommand(bankSummarySql, connection))
                await using (var reader = await command.ExecuteReaderAsync())
                    while (await reader.ReadAsync()) response.Bancos.Add(new FinanceBankBalance { IdBanco = reader.GetInt32(0), Nombre = reader.GetString(1), Capital = reader.GetDecimal(2), Ganancia = reader.GetDecimal(3) });

                const string reconciledTotalsSql = @"
DECLARE @corteId int=(SELECT MAX(IdConciliacion) FROM dbo.FIN_CONCILIACION),@corte datetime2(0);
SELECT @corte=FechaRegistro FROM dbo.FIN_CONCILIACION WHERE IdConciliacion=@corteId;
SELECT CAST(COALESCE((SELECT SUM(SaldoReal) FROM dbo.FIN_CONCILIACION_DETALLE WHERE IdConciliacion=@corteId AND Fondo='Capital'),(SELECT ISNULL(SUM(SaldoCapitalInicial),0)+150.00 FROM dbo.FIN_BANCO_TEMPORAL WHERE Activo=1))
 +ISNULL((SELECT SUM(CapitalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL WHERE @corte IS NULL OR FechaRegistro>@corte),0)
 +ISNULL((SELECT SUM(Monto) FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL WHERE Cuenta='Capital' AND (@corte IS NULL OR Fecha>@corte)),0)
 -ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE CuentaOrigen IN ('Capital','Caja') AND Anulado=0 AND (@corte IS NULL OR FechaRegistro>@corte)),0)
 -ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE CuentaOrigen='Capital' AND Anulado=0 AND (@corte IS NULL OR FechaRegistro>@corte)),0) AS decimal(12,2)) Capital,
 CAST(COALESCE((SELECT SUM(SaldoReal) FROM dbo.FIN_CONCILIACION_DETALLE WHERE IdConciliacion=@corteId AND Fondo='Ganancia'),(SELECT ISNULL(SUM(SaldoGananciaInicial),0)+100.00 FROM dbo.FIN_BANCO_TEMPORAL WHERE Activo=1))
 +ISNULL((SELECT SUM(PersonalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL WHERE @corte IS NULL OR FechaRegistro>@corte),0)
 +ISNULL((SELECT SUM(Monto) FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL WHERE Cuenta='Personal' AND (@corte IS NULL OR Fecha>@corte)),0)
 -ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE CuentaOrigen='Personal' AND Anulado=0 AND (@corte IS NULL OR FechaRegistro>@corte)),0)
 -ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE CuentaOrigen='Personal' AND Anulado=0 AND (@corte IS NULL OR FechaRegistro>@corte)),0) AS decimal(12,2)) Ganancia,
 ISNULL((SELECT SUM(CapitalAsignado-SalidasCapitalCaja) FROM dbo.FIN_CIERRE_TEMPORAL c WHERE c.Conciliado=0 AND (@corte IS NULL OR c.FechaRegistro>@corte) AND NOT EXISTS(SELECT 1 FROM dbo.FIN_DEPOSITO_LOTE_CIERRE x JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=x.IdLote WHERE x.IdCierreTemporal=c.IdCierreTemporal AND l.Anulado=0)),0) PendienteCapital,
 ISNULL((SELECT SUM(PersonalAsignado-SalidasGananciaCaja) FROM dbo.FIN_CIERRE_TEMPORAL c WHERE c.Conciliado=0 AND (@corte IS NULL OR c.FechaRegistro>@corte) AND NOT EXISTS(SELECT 1 FROM dbo.FIN_DEPOSITO_LOTE_CIERRE x JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=x.IdLote WHERE x.IdCierreTemporal=c.IdCierreTemporal AND l.Anulado=0)),0) PendienteGanancia;";
                await using (var command = new SqlCommand(reconciledTotalsSql, connection))
                await using (var reader = await command.ExecuteReaderAsync())
                    if (await reader.ReadAsync())
                    {
                        response.Capital = response.CapitalDepositosMasCaja = reader.GetDecimal(0);
                        response.Personal = response.GananciaDepositosMasCaja = reader.GetDecimal(1);
                        response.PendienteDepositoCapital = reader.GetDecimal(2);
                        response.PendienteDepositoGanancia = reader.GetDecimal(3);
                        response.DepositadoCapital = response.Bancos.Sum(bank => bank.Capital);
                        response.DepositadoPersonal = response.Bancos.Sum(bank => bank.Ganancia);
                    }

                const string cashOutflowDetailsSql = @"
DECLARE @ultimoCierre datetime2(0)=(SELECT MAX(FechaCierre) FROM dbo.FIN_CIERRE_TEMPORAL);
IF @ultimoCierre IS NULL SET @ultimoCierre=CONVERT(datetime2(0),CONVERT(date,GETDATE()));
DECLARE @ultimaConciliacion datetime2(0)=(SELECT MAX(FechaRegistro) FROM dbo.FIN_CONCILIACION);
IF @ultimaConciliacion>@ultimoCierre SET @ultimoCierre=@ultimaConciliacion;
DECLARE @momento datetime2(0)=SYSDATETIME();
SELECT Clave,Concepto,Monto,CuentaOrigen,OrigenEfectivo,FechaRegistro FROM (
 SELECT CAST('M:'+CONVERT(varchar(20),m.IdMovimiento) AS varchar(30)) Clave,m.Concepto,m.Monto,m.CuentaOrigen,m.OrigenEfectivo,m.FechaRegistro
 FROM dbo.FIN_MOVIMIENTO m
 WHERE m.IdBanco IS NULL AND m.OrigenEfectivo IN ('Caja','Capital','Ganancia') AND m.Anulado=0 AND m.FechaRegistro>@ultimoCierre AND m.FechaRegistro<=@momento
 UNION ALL
 SELECT CAST('P:'+CONVERT(varchar(20),p.IdPagoGastoPersonal) AS varchar(30)),COALESCE(NULLIF(p.Observaciones,''),g.Nombre,'Pago registrado'),p.Monto,p.CuentaOrigen,p.OrigenEfectivo,p.FechaRegistro
 FROM dbo.PAGO_GASTO_PERSONAL p
 LEFT JOIN dbo.GASTO_PERSONAL_PERIODO gp ON gp.IdPeriodoGastoPersonal=p.IdPeriodoGastoPersonal
 LEFT JOIN dbo.GASTO_PERSONAL g ON g.IdGastoPersonal=gp.IdGastoPersonal
 WHERE p.IdBanco IS NULL AND p.OrigenEfectivo IN ('Caja','Capital','Ganancia') AND p.Anulado=0 AND p.FechaRegistro>@ultimoCierre AND p.FechaRegistro<=@momento
) s ORDER BY FechaRegistro;";
                await using (var command = new SqlCommand(cashOutflowDetailsSql, connection))
                await using (var reader = await command.ExecuteReaderAsync())
                    while (await reader.ReadAsync()) response.SalidasEfectivoDetalle.Add(new CashOutflowDetail
                    {
                        Clave = reader.GetString(0),
                        Concepto = reader.IsDBNull(1) ? "Pago registrado" : reader.GetString(1),
                        Monto = reader.GetDecimal(2),
                        Fondo = reader.IsDBNull(3) ? "Sin fondo" : reader.GetString(3) == "Personal" ? "Ganancia" : reader.GetString(3),
                        Origen = reader.IsDBNull(4) ? "Caja" : reader.GetString(4),
                        Fecha = reader.GetDateTime(5).ToString("dd/MM HH:mm")
                    });

                const string obligationsSql = @"
SELECT p.IdPeriodoGastoPersonal,g.Nombre,p.MontoProgramado-ISNULL(x.Pagado,0) Saldo
FROM dbo.GASTO_PERSONAL_PERIODO p JOIN dbo.GASTO_PERSONAL g ON g.IdGastoPersonal=p.IdGastoPersonal
LEFT JOIN (SELECT IdPeriodoGastoPersonal,SUM(Monto) Pagado FROM dbo.PAGO_GASTO_PERSONAL GROUP BY IdPeriodoGastoPersonal) x ON x.IdPeriodoGastoPersonal=p.IdPeriodoGastoPersonal
WHERE p.Anio=YEAR(GETDATE()) AND p.Mes=MONTH(GETDATE()) AND p.MontoProgramado>ISNULL(x.Pagado,0) AND g.Activo=1
ORDER BY p.FechaVencimiento,g.Nombre;";
                await using (var command = new SqlCommand(obligationsSql, connection))
                await using (var reader = await command.ExecuteReaderAsync())
                    while (await reader.ReadAsync()) response.Gastos.Add(new PersonalExpense { IdPeriodo = reader.GetInt32(0), Nombre = reader.GetString(1), Saldo = Money(reader, 2) });

                return Ok(response);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "No se pudo cargar el resumen temporal de Finanzas.");
                var message = ex.Number is 208 or 207
                    ? $"SQL Server no encuentra una tabla o columna requerida en la base '{databaseName}': {ex.Message}"
                    : "No se pudo consultar Finanzas. Revisa la conexión y permisos de SQL Server.";
                return StatusCode(503, new { mensaje = message });
            }
        }

        [HttpGet("cierres")]
        public async Task<IActionResult> AuditClosings([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] string tipo = "Cierres")
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "Inicia sesión para consultar la auditoría." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return StatusCode(503, new { mensaje = "Falta configurar la conexión de SQL Server." });
            if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
                return BadRequest(new { mensaje = "La fecha inicial no puede ser posterior a la fecha final." });
            try
            {
                var cierres = new List<CloseAudit>();
                var filter = (tipo ?? "Cierres").Trim();
                if (!string.Equals(filter, "Cierres", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { mensaje = "Este historial muestra cierres. Consulta depósitos y salidas en sus módulos." });
                await using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();
                await using var command = new SqlCommand(@"
SELECT TOP (10) c.IdCierreTemporal,c.FechaCierre,'Cierre' Tipo,c.Ventas,c.CapitalAsignado,c.PersonalAsignado,u.NombreCompleto,
       CAST(NULL AS varchar(15)) Cuenta,CAST(NULL AS varchar(100)) Comprobante,CAST(0 AS bit) Anulado,c.SalidasCapitalCaja,c.SalidasGananciaCaja
FROM dbo.FIN_CIERRE_TEMPORAL c LEFT JOIN dbo.USUARIO u ON u.IdUsuario=c.IdUsuario
WHERE (@desde IS NULL OR c.FechaCierre>=@desde) AND (@hasta IS NULL OR c.FechaCierre<DATEADD(day,1,@hasta))
ORDER BY c.FechaCierre DESC,c.IdCierreTemporal DESC;", connection);
                command.Parameters.Add("@desde", SqlDbType.Date).Value = desde.HasValue ? (object)desde.Value.Date : DBNull.Value;
                command.Parameters.Add("@hasta", SqlDbType.Date).Value = hasta.HasValue ? (object)hasta.Value.Date : DBNull.Value;
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    cierres.Add(new CloseAudit
                    {
                        Id = Convert.ToInt32(reader[0]),
                        Fecha = Convert.ToDateTime(reader[1]).ToString("dd/MM/yyyy HH:mm"), FechaIso = Convert.ToDateTime(reader[1]).ToString("yyyy-MM-dd"),
                        Tipo = Convert.ToString(reader[2]) ?? "Cierre", Ventas = Convert.ToDecimal(reader[3]),
                        Capital = reader.IsDBNull(4) ? (decimal?)null : Convert.ToDecimal(reader[4]),
                        Ganancia = reader.IsDBNull(5) ? (decimal?)null : Convert.ToDecimal(reader[5]),
                        Responsable = reader.IsDBNull(6) ? "" : Convert.ToString(reader[6]) ?? "",
                        Cuenta = reader.IsDBNull(7) ? "" : Convert.ToString(reader[7]) ?? "",
                        Comprobante = reader.IsDBNull(8) ? "" : Convert.ToString(reader[8]) ?? "",
                        Anulado = Convert.ToBoolean(reader[9]), SalidasCapital = Convert.ToDecimal(reader[10]), SalidasGanancia = Convert.ToDecimal(reader[11])
                    });
                return Ok(new { actividades = cierres });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "No se pudo consultar la auditoría de cierres.");
                return StatusCode(503, new { mensaje = "No se pudo consultar la auditoría. Revisa la conexión y las tablas de Finanzas." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló la lectura de cierres recientes.");
                return StatusCode(500, new { mensaje = "No se pudieron mostrar los cierres recientes. Actualiza la página y, si continúa, revisa el registro del servidor." });
            }
        }

        [HttpPost("cierres")]
        public async Task<IActionResult> SaveClosing([FromBody] ClosingRequest request)
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "La sesión venció. Inicia sesión otra vez." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return StatusCode(503, new { mensaje = "Falta configurar SQL Server." });
            if (request.EfectivoInicial != 250m || request.EfectivoContado < 0 || request.SalidasEfectivoEsperadas < 0 || request.CapitalContado < 0 || request.GananciaContada < 0 || request.EfectivoVentasContado < 0 || request.EfectivoContado > MaxAmount || request.SalidasEfectivoEsperadas > MaxAmount || request.CapitalContado > MaxAmount || request.GananciaContada > MaxAmount || request.EfectivoVentasContado > MaxAmount || Math.Abs(request.EfectivoContado - (request.CapitalContado + request.GananciaContada + request.EfectivoVentasContado)) > 0.01m || request.SalidasVisibles == null || request.SalidasAjustar == null) return BadRequest(new { mensaje = "Revisa los tres conteos y la lista de salidas pendientes." });
            try
            {
                await using var connection = new SqlConnection(ConnectionString); await connection.OpenAsync();
                using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
                try
                {
                    decimal cashOutflows = 0m, cashCapitalOutflows = 0m, cashGainOutflows = 0m;
                    DateTime closeAt;
                    await using (var pendingCommand = new SqlCommand(@"
DECLARE @momento datetime2(0)=SYSDATETIME();
DECLARE @ultimoCierre datetime2(0)=(SELECT MAX(FechaCierre) FROM dbo.FIN_CIERRE_TEMPORAL WITH (UPDLOCK,HOLDLOCK));
IF @ultimoCierre IS NULL SET @ultimoCierre=CONVERT(datetime2(0),CONVERT(date,@momento));
SELECT @momento MomentoCierre;
SELECT Clave,Monto,CuentaOrigen FROM (
 SELECT CAST('M:'+CONVERT(varchar(20),m.IdMovimiento) AS varchar(30)) Clave,m.Monto,m.CuentaOrigen,m.FechaRegistro
 FROM dbo.FIN_MOVIMIENTO m WITH(UPDLOCK,HOLDLOCK)
 WHERE m.IdBanco IS NULL AND m.OrigenEfectivo IN ('Caja','Capital','Ganancia') AND m.Anulado=0 AND m.FechaRegistro>@ultimoCierre AND m.FechaRegistro<=@momento
 UNION ALL
 SELECT CAST('P:'+CONVERT(varchar(20),p.IdPagoGastoPersonal) AS varchar(30)),p.Monto,p.CuentaOrigen,p.FechaRegistro
 FROM dbo.PAGO_GASTO_PERSONAL p WITH(UPDLOCK,HOLDLOCK)
 WHERE p.IdBanco IS NULL AND p.OrigenEfectivo IN ('Caja','Capital','Ganancia') AND p.Anulado=0 AND p.FechaRegistro>@ultimoCierre AND p.FechaRegistro<=@momento
) pending ORDER BY FechaRegistro;", connection, transaction))
                    await using (var reader = await pendingCommand.ExecuteReaderAsync())
                    {
                        await reader.ReadAsync();
                        closeAt = reader.GetDateTime(0);
                        await reader.NextResultAsync();
                        var pending = new Dictionary<string, (decimal Amount, bool IsGain)>(StringComparer.OrdinalIgnoreCase);
                        while (await reader.ReadAsync()) pending[reader.GetString(0)] = (reader.GetDecimal(1), !reader.IsDBNull(2) && reader.GetString(2) == "Personal");
                        var visibleList = request.SalidasVisibles ?? new List<string>();
                        var visibleKeys = new HashSet<string>(visibleList, StringComparer.OrdinalIgnoreCase);
                        var selectedKeys = request.SalidasAjustar ?? new List<string>();
                        if (visibleKeys.Count != visibleList.Count || !visibleKeys.SetEquals(pending.Keys) || selectedKeys.Count != selectedKeys.Distinct(StringComparer.OrdinalIgnoreCase).Count() || selectedKeys.Any(key => string.IsNullOrWhiteSpace(key) || !pending.ContainsKey(key)))
                        {
                            await transaction.RollbackAsync();
                            return Conflict(new { mensaje = "La lista de salidas pendientes cambió. Actualiza el cierre y revisa de nuevo." });
                        }
                        foreach (var key in selectedKeys)
                        {
                            var item = pending[key];
                            cashOutflows += item.Amount;
                            if (item.IsGain) cashGainOutflows += item.Amount; else cashCapitalOutflows += item.Amount;
                        }
                    }
                    if (Math.Abs(cashOutflows - request.SalidasEfectivoEsperadas) >= 0.005m)
                    {
                        await transaction.RollbackAsync();
                        return Conflict(new { mensaje = "Se registraron nuevos pagos en efectivo desde que abriste el cierre. Actualiza el resumen y vuelve a revisar el cálculo." });
                    }

                    var sales = request.EfectivoContado + cashOutflows - request.EfectivoInicial;
                    if (sales < 0 || sales > MaxAmount)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { mensaje = "El cálculo da ventas negativas o supera el límite. Revisa el efectivo contado, las salidas y el efectivo inicial." });
                    }
                    // Capital recibe 61%; se redondea al múltiplo de Q0.25 más cercano.
                    // Ganancia absorbe el residuo para conservar exactamente el total vendido.
                    var capital = Math.Round((sales * CapitalShare) / QuarterPeso, 0, MidpointRounding.AwayFromZero) * QuarterPeso;
                    capital = Math.Min(capital, sales);
                    var personal = sales - capital;
                    await using var command = new SqlCommand(@"INSERT dbo.FIN_CIERRE_TEMPORAL(FechaCierre,Ventas,CapitalAsignado,PersonalAsignado,IdUsuario,EfectivoInicial,EfectivoContado,SalidasCaja,SalidasCapitalCaja,SalidasGananciaCaja)
VALUES(@fecha,@ventas,@capital,@personal,@usuario,@efectivoInicial,@efectivoContado,@salidasCaja,@salidasCapital,@salidasGanancia); SELECT CONVERT(int,SCOPE_IDENTITY());", connection, transaction);
                    command.Parameters.Add("@fecha", SqlDbType.DateTime2).Value = closeAt;
                    AddMoney(command, "@ventas", sales); AddMoney(command, "@capital", capital); AddMoney(command, "@personal", personal);
                    AddMoney(command, "@efectivoInicial", request.EfectivoInicial); AddMoney(command, "@efectivoContado", request.EfectivoContado); AddMoney(command, "@salidasCaja", cashOutflows);
                    AddMoney(command, "@salidasCapital", cashCapitalOutflows); AddMoney(command, "@salidasGanancia", cashGainOutflows);
                    command.Parameters.Add("@usuario", SqlDbType.Int).Value = UserId.Value;
                    var id = Convert.ToInt32(await command.ExecuteScalarAsync());
                    await transaction.CommitAsync();
                    return Ok(new { id, ventas = sales, capital, personal, salidasEfectivo = cashOutflows, salidasIncluidasEnVentas = cashOutflows, mensaje = "Cierre guardado." });
                }
                catch
                {
                    if (transaction.Connection != null) await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (SqlException ex)
            { _logger.LogError(ex, "No se pudo guardar el cierre temporal."); return StatusCode(503, new { mensaje = ex.Number is 207 or 208 ? "Aplica la nueva migración de conciliación de caja antes de guardar cierres." : "No se pudo guardar el cierre. Revisa la conexión y las tablas de Finanzas." }); }
        }

        [HttpPost("depositos")]
        public async Task<IActionResult> SaveDeposit([FromBody] DepositRequest request)
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "La sesión venció. Inicia sesión otra vez." });
            var account = NormalizeAccount(request.Cuenta); var receipt = (request.Comprobante ?? "").Trim();
            if ((account != "Capital" && account != "Personal") || request.Monto <= 0 || request.Monto > MaxAmount || string.IsNullOrWhiteSpace(receipt) || receipt.Length > 100 || request.Fecha.Date > DateTime.Today)
                return BadRequest(new { mensaje = "Indica la cuenta, un monto válido, fecha y número de comprobante." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return StatusCode(503, new { mensaje = "Falta configurar SQL Server." });
            try
            {
                await using var connection = new SqlConnection(ConnectionString); await connection.OpenAsync();
                await using var command = new SqlCommand(@"INSERT dbo.FIN_DEPOSITO_TEMPORAL(Cuenta,Monto,Fecha,Comprobante,IdUsuario) VALUES(@cuenta,@monto,@fecha,@comprobante,@usuario); SELECT CONVERT(int,SCOPE_IDENTITY());", connection);
                command.Parameters.Add("@cuenta", SqlDbType.VarChar, 15).Value = account; AddMoney(command, "@monto", request.Monto);
                command.Parameters.Add("@fecha", SqlDbType.Date).Value = request.Fecha.Date;
                command.Parameters.Add("@comprobante", SqlDbType.VarChar, 100).Value = receipt;
                command.Parameters.Add("@usuario", SqlDbType.Int).Value = UserId.Value;
                var id = Convert.ToInt32(await command.ExecuteScalarAsync());
                return Ok(new { id, responsable = UserName, mensaje = "Depósito guardado." });
            }
            catch (SqlException ex)
            { _logger.LogError(ex, "No se pudo guardar el depósito temporal."); return StatusCode(503, new { mensaje = "No se pudo guardar el depósito. Revisa que esté aplicada la migración." }); }
        }

        [HttpPut("depositos/{id:int}")]
        public async Task<IActionResult> CorrectDeposit(int id, [FromBody] DepositCorrectionRequest request)
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "La sesión venció. Inicia sesión otra vez." });
            var account = NormalizeAccount(request.Cuenta); var receipt = (request.Comprobante ?? "").Trim(); var reason = (request.Motivo ?? "").Trim();
            if (id <= 0 || (account != "Capital" && account != "Personal") || request.Monto <= 0 || request.Monto > MaxAmount || request.Fecha.Date > DateTime.Today || receipt.Length is 0 or > 100 || reason.Length is 0 or > 250)
                return BadRequest(new { mensaje = "Revisa cuenta, monto, fecha, comprobante y motivo de la corrección." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return StatusCode(503, new { mensaje = "Falta configurar SQL Server." });
            try
            {
                await using var connection = new SqlConnection(ConnectionString); await connection.OpenAsync();
                using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
                try
                {
                    await using var command = new SqlCommand(@"
DECLARE @Cuenta varchar(15),@Monto decimal(12,2),@Fecha date,@Comprobante varchar(100),@Anulado bit;
SELECT @Cuenta=Cuenta,@Monto=Monto,@Fecha=Fecha,@Comprobante=Comprobante,@Anulado=Anulado
FROM dbo.FIN_DEPOSITO_TEMPORAL WITH(UPDLOCK,HOLDLOCK) WHERE IdDepositoTemporal=@id;
IF @Cuenta IS NULL THROW 51031,'No se encontró el depósito.',1;
IF @Anulado=1 THROW 51032,'Este depósito ya está anulado.',1;
INSERT dbo.FIN_DEPOSITO_TEMPORAL_AUDITORIA(IdDepositoTemporal,Accion,CuentaAnterior,MontoAnterior,FechaAnterior,ComprobanteAnterior,CuentaNueva,MontoNuevo,FechaNueva,ComprobanteNuevo,Motivo,IdUsuario)
VALUES(@id,'Correccion',@Cuenta,@Monto,@Fecha,@Comprobante,@cuenta,@monto,@fecha,@comprobante,@motivo,@usuario);
UPDATE dbo.FIN_DEPOSITO_TEMPORAL SET Cuenta=@cuenta,Monto=@monto,Fecha=@fecha,Comprobante=@comprobante WHERE IdDepositoTemporal=@id;", connection, (SqlTransaction)transaction);
                    command.Parameters.Add("@id", SqlDbType.Int).Value = id; command.Parameters.Add("@cuenta", SqlDbType.VarChar, 15).Value = account;
                    AddMoney(command, "@monto", request.Monto); command.Parameters.Add("@fecha", SqlDbType.Date).Value = request.Fecha.Date;
                    command.Parameters.Add("@comprobante", SqlDbType.VarChar, 100).Value = receipt; command.Parameters.Add("@motivo", SqlDbType.VarChar, 250).Value = reason;
                    command.Parameters.Add("@usuario", SqlDbType.Int).Value = UserId.Value;
                    await command.ExecuteNonQueryAsync(); await transaction.CommitAsync();
                    return Ok(new { mensaje = "Depósito corregido; el cambio quedó en auditoría." });
                }
                catch { if (transaction.Connection != null) await transaction.RollbackAsync(); throw; }
            }
            catch (SqlException ex) when (ex.Number is 51031 or 51032) { return Conflict(new { mensaje = ex.Message }); }
            catch (SqlException ex) { _logger.LogError(ex, "No se pudo corregir el depósito {DepositId}.", id); return StatusCode(503, new { mensaje = "No se pudo corregir el depósito. Revisa que esté aplicada la migración de auditoría." }); }
        }

        [HttpPost("depositos/{id:int}/anular")]
        public async Task<IActionResult> CancelDeposit(int id, [FromBody] DepositCancelRequest request)
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "La sesión venció. Inicia sesión otra vez." });
            var reason = (request.Motivo ?? "").Trim();
            if (id <= 0 || reason.Length is 0 or > 250) return BadRequest(new { mensaje = "Escribe el motivo de la anulación (máximo 250 caracteres)." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return StatusCode(503, new { mensaje = "Falta configurar SQL Server." });
            try
            {
                await using var connection = new SqlConnection(ConnectionString); await connection.OpenAsync();
                using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
                await using var command = new SqlCommand(@"
DECLARE @Cuenta varchar(15),@Monto decimal(12,2),@Fecha date,@Comprobante varchar(100),@Anulado bit;
SELECT @Cuenta=Cuenta,@Monto=Monto,@Fecha=Fecha,@Comprobante=Comprobante,@Anulado=Anulado
FROM dbo.FIN_DEPOSITO_TEMPORAL WITH(UPDLOCK,HOLDLOCK) WHERE IdDepositoTemporal=@id;
IF @Cuenta IS NULL THROW 51031,'No se encontró el depósito.',1;
IF @Anulado=1 THROW 51032,'Este depósito ya está anulado.',1;
INSERT dbo.FIN_DEPOSITO_TEMPORAL_AUDITORIA(IdDepositoTemporal,Accion,CuentaAnterior,MontoAnterior,FechaAnterior,ComprobanteAnterior,CuentaNueva,MontoNuevo,FechaNueva,ComprobanteNuevo,Motivo,IdUsuario)
VALUES(@id,'Anulacion',@Cuenta,@Monto,@Fecha,@Comprobante,@Cuenta,@Monto,@Fecha,@Comprobante,@motivo,@usuario);
UPDATE dbo.FIN_DEPOSITO_TEMPORAL SET Anulado=1 WHERE IdDepositoTemporal=@id;", connection, (SqlTransaction)transaction);
                command.Parameters.Add("@id", SqlDbType.Int).Value = id; command.Parameters.Add("@motivo", SqlDbType.VarChar, 250).Value = reason;
                command.Parameters.Add("@usuario", SqlDbType.Int).Value = UserId.Value; await command.ExecuteNonQueryAsync(); await transaction.CommitAsync();
                return Ok(new { mensaje = "Depósito anulado; el registro original se conserva en auditoría." });
            }
            catch (SqlException ex) when (ex.Number is 51031 or 51032) { return Conflict(new { mensaje = ex.Message }); }
            catch (SqlException ex) { _logger.LogError(ex, "No se pudo anular el depósito {DepositId}.", id); return StatusCode(503, new { mensaje = "No se pudo anular el depósito. Revisa que esté aplicada la migración de auditoría." }); }
        }

        [HttpPost("salidas")]
        public async Task<IActionResult> SaveExpense([FromBody] ExpenseRequest request)
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "La sesión venció. Inicia sesión otra vez." });
            var source = (request.OrigenPago ?? "").Trim();
            var concept = (request.Concepto ?? "").Trim();
            var receipt = (request.Comprobante ?? "").Trim();
            var isCash = source is "Caja" or "Capital" or "Ganancia";
            var isBank = source is "BAM" or "Banrural";
            var sourceAccount = isBank ? (source == "BAM" ? "Personal" : "Capital")
                : source == "Capital" ? "Capital"
                : source == "Ganancia" ? "Personal"
                : "Personal";
            var cashSource = isCash ? source : null;
            var paidFromCash = source == "Caja";
            if (request.Monto <= 0 || request.Monto > MaxAmount || request.Fecha == default || request.Fecha.Date > DateTime.Today || (!isCash && !isBank) || receipt.Length > 100 || (isBank && (string.IsNullOrWhiteSpace(receipt) || !request.IdBanco.HasValue || request.IdBanco <= 0)) || (isCash && request.IdBanco.HasValue))
                return BadRequest(new { mensaje = "Indica un monto, gasto, origen y fecha válidos. Las salidas bancarias requieren comprobante." });
            if (request.IdPeriodoGastoPersonal <= 0 && (concept.Length is 0 or > 150))
                return BadRequest(new { mensaje = "Si seleccionas Otros, escribe el concepto (máximo 150 caracteres)." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return StatusCode(503, new { mensaje = "Falta configurar SQL Server." });
            try
            {
                await using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
                try
                {
                    if (isBank)
                    {
                        await using var bankCommand = new SqlCommand(@"
SELECT b.Nombre,CASE WHEN UPPER(b.Nombre) LIKE '%BANRURAL%' THEN 'Capital' ELSE 'Ganancia' END Fondo,
 CAST(CASE WHEN UPPER(b.Nombre) LIKE '%BANRURAL%' THEN b.SaldoCapitalInicial
   +ISNULL((SELECT SUM(x.CapitalDepositado) FROM dbo.FIN_DEPOSITO_LOTE_CIERRE x JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=x.IdLote WHERE l.IdBanco=b.IdBanco AND l.Anulado=0),0)
   -ISNULL((SELECT SUM(t.Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA t WHERE t.IdBancoOrigen=b.IdBanco AND t.Fondo='Capital' AND t.AfectaSaldo=1),0)
   +ISNULL((SELECT SUM(t.Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA t WHERE t.IdBancoDestino=b.IdBanco AND t.Fondo='Capital' AND t.AfectaSaldo=1),0)
   -ISNULL((SELECT SUM(m.Monto) FROM dbo.FIN_MOVIMIENTO m WHERE m.IdBanco=b.IdBanco AND m.Anulado=0 AND m.CuentaOrigen='Capital'),0)
   -ISNULL((SELECT SUM(p.Monto) FROM dbo.PAGO_GASTO_PERSONAL p WHERE p.IdBanco=b.IdBanco AND p.Anulado=0 AND p.CuentaOrigen='Capital'),0)
   +ISNULL((SELECT SUM(d.Ajuste) FROM dbo.FIN_CONCILIACION_DETALLE d WHERE d.IdBanco=b.IdBanco AND d.Fondo='Capital'),0)
 ELSE b.SaldoGananciaInicial
   +ISNULL((SELECT SUM(x.GananciaDepositada) FROM dbo.FIN_DEPOSITO_LOTE_CIERRE x JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=x.IdLote WHERE l.IdBanco=b.IdBanco AND l.Anulado=0),0)
   -ISNULL((SELECT SUM(t.Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA t WHERE t.IdBancoOrigen=b.IdBanco AND t.Fondo='Ganancia' AND t.AfectaSaldo=1),0)
   +ISNULL((SELECT SUM(t.Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA t WHERE t.IdBancoDestino=b.IdBanco AND t.Fondo='Ganancia' AND t.AfectaSaldo=1),0)
   -ISNULL((SELECT SUM(m.Monto) FROM dbo.FIN_MOVIMIENTO m WHERE m.IdBanco=b.IdBanco AND m.Anulado=0 AND m.CuentaOrigen='Personal'),0)
   -ISNULL((SELECT SUM(p.Monto) FROM dbo.PAGO_GASTO_PERSONAL p WHERE p.IdBanco=b.IdBanco AND p.Anulado=0 AND p.CuentaOrigen='Personal'),0)
   +ISNULL((SELECT SUM(d.Ajuste) FROM dbo.FIN_CONCILIACION_DETALLE d WHERE d.IdBanco=b.IdBanco AND d.Fondo='Ganancia'),0)
 END AS decimal(12,2)) Disponible
FROM dbo.FIN_BANCO_TEMPORAL b WITH(UPDLOCK,HOLDLOCK)
 WHERE b.IdBanco=@idBanco AND b.Activo=1 AND ((@source='BAM' AND UPPER(b.Nombre) LIKE '%BAM%') OR (@source='Banrural' AND UPPER(b.Nombre) LIKE '%BANRURAL%'));", connection, transaction);
                        bankCommand.Parameters.Add("@idBanco", SqlDbType.Int).Value = request.IdBanco!.Value;
                        bankCommand.Parameters.Add("@source", SqlDbType.VarChar, 20).Value = source;
                        await using var bankReader = await bankCommand.ExecuteReaderAsync();
                        if (!await bankReader.ReadAsync()) throw new InvalidOperationException("No se encontró el banco seleccionado. Revisa la cuenta BAM o Banrural en Depósitos.");
                        var bankFund = bankReader.GetString(1);
                        var available = bankReader.GetDecimal(2);
                        await bankReader.CloseAsync();
                        if (bankFund != sourceAccount) throw new InvalidOperationException("El banco seleccionado no corresponde al fondo configurado.");
                        if (request.Monto > available) throw new InvalidOperationException($"El saldo disponible de {source} es Q {available:N2}; no alcanza para esta salida.");
                    }
                    if (request.IdPeriodoGastoPersonal > 0)
                    {
                        await using var command = new SqlCommand(@"
DECLARE @saldo decimal(12,2),@concepto varchar(250);
SELECT @saldo=p.MontoProgramado-ISNULL(x.Pagado,0),@concepto=g.Nombre
FROM dbo.GASTO_PERSONAL_PERIODO p WITH(UPDLOCK,HOLDLOCK)
JOIN dbo.GASTO_PERSONAL g ON g.IdGastoPersonal=p.IdGastoPersonal AND g.Activo=1
OUTER APPLY(SELECT SUM(Monto) Pagado FROM dbo.PAGO_GASTO_PERSONAL WHERE IdPeriodoGastoPersonal=p.IdPeriodoGastoPersonal AND Anulado=0) x
WHERE p.IdPeriodoGastoPersonal=@periodo AND p.Anio=YEAR(@fecha) AND p.Mes=MONTH(@fecha);
IF @saldo IS NULL THROW 51023,'La fecha debe pertenecer al período actual para registrar un gasto de la lista.',1;
IF @monto>@saldo THROW 51022,'El monto supera el saldo pendiente de ese gasto.',1;
INSERT dbo.PAGO_GASTO_PERSONAL(IdPeriodoGastoPersonal,IdUsuario,Monto,FechaPago,MedioPago,Referencia,Observaciones,CuentaOrigen,PagadoDesdeCaja,IdBanco,OrigenEfectivo)
VALUES(@periodo,@usuario,@monto,@fecha,@medioPago,@referencia,@concepto,@cuentaOrigen,@pagadoDesdeCaja,@idBanco,@origenEfectivo);", connection, transaction);
                        command.Parameters.Add("@periodo", SqlDbType.Int).Value = request.IdPeriodoGastoPersonal;
                        command.Parameters.Add("@usuario", SqlDbType.Int).Value = UserId.Value;
                        AddMoney(command, "@monto", request.Monto);
                        command.Parameters.Add("@fecha", SqlDbType.DateTime2).Value = request.Fecha.Date;
                        command.Parameters.Add("@medioPago", SqlDbType.VarChar, 20).Value = isCash ? "Efectivo" : "Transferencia";
                        command.Parameters.Add("@referencia", SqlDbType.VarChar, 100).Value = receipt.Length == 0 ? (object)DBNull.Value : receipt;
                        command.Parameters.Add("@cuentaOrigen", SqlDbType.VarChar, 15).Value = sourceAccount;
                        command.Parameters.Add("@pagadoDesdeCaja", SqlDbType.Bit).Value = paidFromCash;
                        command.Parameters.Add("@idBanco", SqlDbType.Int).Value = request.IdBanco.HasValue ? (object)request.IdBanco.Value : DBNull.Value;
                        command.Parameters.Add("@origenEfectivo", SqlDbType.VarChar, 20).Value = cashSource == null ? (object)DBNull.Value : cashSource;
                        await command.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        await using var command = new SqlCommand("INSERT dbo.FIN_MOVIMIENTO(Tipo,IdCompra,IdUsuario,Fecha,Concepto,Monto,MedioPago,Referencia,CuentaOrigen,PagadoDesdeCaja,IdBanco,OrigenEfectivo) VALUES('Empresa',NULL,@usuario,@fecha,@concepto,@monto,@medioPago,@referencia,@cuentaOrigen,@pagadoDesdeCaja,@idBanco,@origenEfectivo);", connection, transaction);
                        command.Parameters.Add("@usuario", SqlDbType.Int).Value = UserId.Value;
                        command.Parameters.Add("@fecha", SqlDbType.DateTime2).Value = request.Fecha.Date;
                        command.Parameters.Add("@medioPago", SqlDbType.VarChar, 20).Value = isCash ? "Efectivo" : "Transferencia";
                        command.Parameters.Add("@referencia", SqlDbType.VarChar, 100).Value = receipt.Length == 0 ? (object)DBNull.Value : receipt;
                        command.Parameters.Add("@cuentaOrigen", SqlDbType.VarChar, 15).Value = sourceAccount;
                        command.Parameters.Add("@pagadoDesdeCaja", SqlDbType.Bit).Value = paidFromCash;
                        command.Parameters.Add("@idBanco", SqlDbType.Int).Value = request.IdBanco.HasValue ? (object)request.IdBanco.Value : DBNull.Value;
                        command.Parameters.Add("@origenEfectivo", SqlDbType.VarChar, 20).Value = cashSource == null ? (object)DBNull.Value : cashSource;
                        command.Parameters.Add("@concepto", SqlDbType.VarChar, 150).Value = concept;
                        AddMoney(command, "@monto", request.Monto);
                        await command.ExecuteNonQueryAsync();
                    }
                    await transaction.CommitAsync();
                    return Ok(new { responsable = UserName, mensaje = "Salida registrada." });
                }
                catch { await transaction.RollbackAsync(); throw; }
            }
            catch (SqlException ex) when (ex.Number is 51021 or 51022 or 51023)
            { return Conflict(new { mensaje = ex.Message }); }
            catch (InvalidOperationException ex) { return Conflict(new { mensaje = ex.Message }); }
            catch (SqlException ex)
            { _logger.LogError(ex, "No se pudo registrar la salida de Finanzas."); return StatusCode(503, new { mensaje = "No se pudo registrar la salida. Revisa la conexión y las tablas de Finanzas." }); }
        }

        private static async Task EnsureCurrentPersonalPeriods(SqlConnection connection)
        {
            await using var command = new SqlCommand(@"
INSERT dbo.GASTO_PERSONAL_PERIODO(IdGastoPersonal,Anio,Mes,MontoProgramado,FechaVencimiento)
SELECT g.IdGastoPersonal,YEAR(GETDATE()),MONTH(GETDATE()),g.MontoMensual,
 CASE WHEN g.DiaVencimiento IS NULL THEN NULL ELSE DATEFROMPARTS(YEAR(GETDATE()),MONTH(GETDATE()),CASE WHEN g.DiaVencimiento>DAY(EOMONTH(GETDATE())) THEN DAY(EOMONTH(GETDATE())) ELSE g.DiaVencimiento END) END
FROM dbo.GASTO_PERSONAL g WHERE g.Activo=1 AND NOT EXISTS(SELECT 1 FROM dbo.GASTO_PERSONAL_PERIODO p WHERE p.IdGastoPersonal=g.IdGastoPersonal AND p.Anio=YEAR(GETDATE()) AND p.Mes=MONTH(GETDATE()));", connection);
            await command.ExecuteNonQueryAsync();
        }

        private static decimal Money(SqlDataReader reader, string name) => reader.IsDBNull(reader.GetOrdinal(name)) ? 0m : Convert.ToDecimal(reader[name]);
        private static string NormalizeAccount(string? account) => (account ?? "").Trim() switch { "Ganancia" => "Personal", var value => value };
        private static decimal Money(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal));
        private static void AddMoney(SqlCommand command, string name, decimal value)
        { var p = command.Parameters.Add(name, SqlDbType.Decimal); p.Precision = 12; p.Scale = 2; p.Value = value; }

        public sealed class ClosingRequest { public decimal EfectivoInicial { get; set; } public decimal EfectivoContado { get; set; } public decimal SalidasEfectivoEsperadas { get; set; } public decimal CapitalContado { get; set; } public decimal GananciaContada { get; set; } public decimal EfectivoVentasContado { get; set; } public List<string>? SalidasVisibles { get; set; } public List<string>? SalidasAjustar { get; set; } }
        public class DepositRequest { public string? Cuenta { get; set; } public decimal Monto { get; set; } public DateTime Fecha { get; set; } public string? Comprobante { get; set; } }
        public sealed class DepositCorrectionRequest : DepositRequest { public string? Motivo { get; set; } }
        public sealed class DepositCancelRequest { public string? Motivo { get; set; } }
        public sealed class ExpenseRequest { public string? OrigenPago { get; set; } public int? IdBanco { get; set; } public int IdPeriodoGastoPersonal { get; set; } public decimal Monto { get; set; } public DateTime Fecha { get; set; } public string? Concepto { get; set; } public string? Comprobante { get; set; } }
        public sealed class PersonalExpense { public int IdPeriodo { get; set; } public string Nombre { get; set; } = ""; public decimal Saldo { get; set; } }
        public sealed class CloseAudit { public int Id { get; set; } public string Fecha { get; set; } = ""; public string FechaIso { get; set; } = ""; public string Tipo { get; set; } = ""; public decimal Ventas { get; set; } public decimal? Capital { get; set; } public decimal? Ganancia { get; set; } public string Responsable { get; set; } = ""; public string Cuenta { get; set; } = ""; public string Comprobante { get; set; } = ""; public bool Anulado { get; set; } public decimal SalidasCapital { get; set; } public decimal SalidasGanancia { get; set; } }
        public sealed class FinanceSummary
        {
            public string Responsable { get; set; } = ""; public string Fecha { get; set; } = ""; public string Mes { get; set; } = "";
            public decimal Capital { get; set; } public decimal CapitalGeneradoTotal { get; set; } public decimal Personal { get; set; } public decimal PersonalGeneradoTotal { get; set; } public decimal DepositadoCapital { get; set; } public decimal DepositadoPersonal { get; set; } public decimal CapitalDepositosMasCaja { get; set; } public decimal GananciaDepositosMasCaja { get; set; }
            public decimal ObligacionesMes { get; set; } public decimal PagadoPersonal { get; set; } public decimal AsignadoPersonalMes { get; set; } public decimal VentasManualMes { get; set; }
            public decimal VentasMesTotal { get; set; } public decimal CapitalGeneradoMes { get; set; } public decimal SalidasCapitalMes { get; set; } public decimal SalidasPersonalMes { get; set; } public decimal SalidasEfectivoPendientes { get; set; } public decimal SalidasCapitalEfectivoPendientes { get; set; } public decimal SalidasGananciaEfectivoPendientes { get; set; } public decimal SalidasCajaVentasPendientes { get; set; }
            public decimal PendienteDepositoCapital { get; set; } public decimal PendienteDepositoGanancia { get; set; } public List<FinanceBankBalance> Bancos { get; } = new List<FinanceBankBalance>(); public List<CashOutflowDetail> SalidasEfectivoDetalle { get; } = new List<CashOutflowDetail>();
            public string ProximoGasto { get; set; } = ""; public string FechaProximoGasto { get; set; } = "";
            public List<PersonalExpense> Gastos { get; } = new List<PersonalExpense>();
        }
        public sealed class CashOutflowDetail { public string Clave { get; set; } = ""; public string Concepto { get; set; } = ""; public decimal Monto { get; set; } public string Fondo { get; set; } = ""; public string Origen { get; set; } = ""; public string Fecha { get; set; } = ""; }
        public sealed class FinanceBankBalance { public int IdBanco { get; set; } public string Nombre { get; set; } = ""; public decimal Capital { get; set; } public decimal Ganancia { get; set; } }
    }
}
