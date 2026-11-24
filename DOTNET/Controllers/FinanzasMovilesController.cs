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
        // El precio de venta equivale al costo multiplicado por 1.61.
        private const decimal SaleMarkupFactor = 1.61m;
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
SELECT
  CAST(ISNULL((SELECT SUM(CapitalGenerado) FROM dbo.FIN_CIERRE_CAJA),0)+ISNULL((SELECT SUM(CapitalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL WHERE Cuenta='Capital'),0)-ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE CuentaOrigen='Capital' AND Anulado=0),0)-ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE CuentaOrigen='Capital' AND Anulado=0),0) AS decimal(12,2)) Capital,
 CAST(ISNULL((SELECT SUM(CapitalGenerado) FROM dbo.FIN_CIERRE_CAJA),0)+ISNULL((SELECT SUM(CapitalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL WHERE Cuenta='Capital'),0) AS decimal(12,2)) CapitalGeneradoTotal,
  CAST(ISNULL((SELECT SUM(GananciaGenerada) FROM dbo.FIN_CIERRE_CAJA),0)+ISNULL((SELECT SUM(PersonalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL WHERE Cuenta='Personal'),0)-ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE CuentaOrigen='Personal' AND Anulado=0),0)-ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE CuentaOrigen='Personal' AND Anulado=0),0) AS decimal(12,2)) Personal,
 CAST(ISNULL((SELECT SUM(GananciaGenerada) FROM dbo.FIN_CIERRE_CAJA),0)+ISNULL((SELECT SUM(PersonalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL WHERE Cuenta='Personal'),0) AS decimal(12,2)) PersonalGeneradoTotal,
 CAST(ISNULL((SELECT SUM(Capital) FROM dbo.FIN_DEPOSITO),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_DEPOSITO_TEMPORAL WHERE Cuenta='Capital' AND Anulado=0),0)+ISNULL((SELECT SUM(c.Capital) FROM dbo.FIN_DEPOSITO_LOTE_CIERRE c JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=c.IdLote WHERE l.Anulado=0),0) AS decimal(12,2)) DepositadoCapital,
 CAST(ISNULL((SELECT SUM(Ganancia) FROM dbo.FIN_DEPOSITO),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_DEPOSITO_TEMPORAL WHERE Cuenta='Personal' AND Anulado=0),0)+ISNULL((SELECT SUM(c.Ganancia) FROM dbo.FIN_DEPOSITO_LOTE_CIERRE c JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=c.IdLote WHERE l.Anulado=0),0) AS decimal(12,2)) DepositadoPersonal,
 CAST(ISNULL((SELECT SUM(Capital) FROM dbo.FIN_DEPOSITO),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_DEPOSITO_TEMPORAL WHERE Cuenta='Capital' AND Anulado=0),0)+ISNULL((SELECT SUM(c.Capital) FROM dbo.FIN_DEPOSITO_LOTE_CIERRE c JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=c.IdLote WHERE l.Anulado=0),0)+150.00 AS decimal(12,2)) CapitalDepositosMasCaja,
 CAST(ISNULL((SELECT SUM(Ganancia) FROM dbo.FIN_DEPOSITO),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_DEPOSITO_TEMPORAL WHERE Cuenta='Personal' AND Anulado=0),0)+ISNULL((SELECT SUM(c.Ganancia) FROM dbo.FIN_DEPOSITO_LOTE_CIERRE c JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=c.IdLote WHERE l.Anulado=0),0)+100.00 AS decimal(12,2)) GananciaDepositosMasCaja,
 CAST(ISNULL((SELECT SUM(MontoProgramado) FROM dbo.GASTO_PERSONAL_PERIODO WHERE Anio=YEAR(GETDATE()) AND Mes=MONTH(GETDATE())),0) AS decimal(12,2)) ObligacionesMes,
 CAST(ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL p JOIN dbo.GASTO_PERSONAL_PERIODO per ON per.IdPeriodoGastoPersonal=p.IdPeriodoGastoPersonal WHERE per.Anio=YEAR(GETDATE()) AND per.Mes=MONTH(GETDATE())),0) AS decimal(12,2)) PagadoPersonal,
 CAST(ISNULL((SELECT SUM(PersonalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL WHERE FechaCierre>=@inicio AND FechaCierre<@fin),0)+ISNULL((SELECT SUM(GananciaGenerada) FROM dbo.FIN_CIERRE_CAJA WHERE FechaCierre>=@inicio AND FechaCierre<@fin),0) AS decimal(12,2)) AsignadoPersonalMes,
 CAST(ISNULL((SELECT SUM(Ventas) FROM dbo.FIN_CIERRE_TEMPORAL WHERE FechaCierre>=@inicio AND FechaCierre<@fin),0) AS decimal(12,2)) VentasManualMes,
 CAST(ISNULL((SELECT SUM(Ventas) FROM dbo.FIN_CIERRE_TEMPORAL WHERE FechaCierre>=@inicio AND FechaCierre<@fin),0)+ISNULL((SELECT SUM(Ventas) FROM dbo.FIN_CIERRE_CAJA WHERE FechaCierre>=@inicio AND FechaCierre<@fin),0) AS decimal(12,2)) VentasMesTotal,
 CAST(ISNULL((SELECT SUM(CapitalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL WHERE FechaCierre>=@inicio AND FechaCierre<@fin),0)+ISNULL((SELECT SUM(CapitalGenerado) FROM dbo.FIN_CIERRE_CAJA WHERE FechaCierre>=@inicio AND FechaCierre<@fin),0) AS decimal(12,2)) CapitalGeneradoMes,
  CAST(ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE CuentaOrigen='Capital' AND Anulado=0 AND Fecha>=@inicio AND Fecha<@fin),0)+ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE CuentaOrigen='Capital' AND Anulado=0 AND FechaPago>=@inicio AND FechaPago<@fin),0) AS decimal(12,2)) SalidasCapitalMes,
  CAST(ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE CuentaOrigen='Personal' AND Anulado=0 AND Fecha>=@inicio AND Fecha<@fin),0)+ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE CuentaOrigen='Personal' AND Anulado=0 AND FechaPago>=@inicio AND FechaPago<@fin),0) AS decimal(12,2)) SalidasPersonalMes,
  CAST(ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE PagadoDesdeCaja=1 AND Anulado=0 AND FechaRegistro>@ultimoCierre),0)+ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE PagadoDesdeCaja=1 AND Anulado=0 AND FechaRegistro>@ultimoCierre),0) AS decimal(12,2)) SalidasCajaPendientes,
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
                        response.SalidasCajaPendientes = Money(reader, "SalidasCajaPendientes");
                        response.ProximoGasto = reader.IsDBNull(reader.GetOrdinal("ProximoGasto")) ? "Sin pendientes" : reader.GetString(reader.GetOrdinal("ProximoGasto"));
                        response.FechaProximoGasto = reader.IsDBNull(reader.GetOrdinal("FechaProximoGasto")) ? "" : reader.GetString(reader.GetOrdinal("FechaProximoGasto"));
                    }
                }

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
       CAST(NULL AS varchar(15)) Cuenta,CAST(NULL AS varchar(100)) Comprobante,CAST(0 AS bit) Anulado
FROM dbo.FIN_CIERRE_TEMPORAL c LEFT JOIN dbo.USUARIO u ON u.IdUsuario=c.IdUsuario
WHERE (@desde IS NULL OR c.FechaCierre>=@desde) AND (@hasta IS NULL OR c.FechaCierre<DATEADD(day,1,@hasta))
ORDER BY c.FechaCierre DESC,c.IdCierreTemporal DESC;", connection);
                command.Parameters.Add("@tipo", SqlDbType.VarChar, 20).Value = filter;
                command.Parameters.Add("@desde", SqlDbType.Date).Value = desde.HasValue ? (object)desde.Value.Date : DBNull.Value;
                command.Parameters.Add("@hasta", SqlDbType.Date).Value = hasta.HasValue ? (object)hasta.Value.Date : DBNull.Value;
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    cierres.Add(new CloseAudit
                    {
                        Id = reader.GetInt32(0),
                        Fecha = reader.GetDateTime(1).ToString("dd/MM/yyyy HH:mm"), FechaIso = reader.GetDateTime(1).ToString("yyyy-MM-dd"),
                        Tipo = reader.GetString(2), Ventas = Money(reader, 3),
                        Capital = reader.IsDBNull(4) ? (decimal?)null : Money(reader, 4),
                        Ganancia = reader.IsDBNull(5) ? (decimal?)null : Money(reader, 5),
                        Responsable = reader.IsDBNull(6) ? "" : reader.GetString(6),
                        Cuenta = reader.IsDBNull(7) ? "" : reader.GetString(7),
                        Comprobante = reader.IsDBNull(8) ? "" : reader.GetString(8),
                        Anulado = reader.GetBoolean(9)
                    });
                return Ok(new { actividades = cierres });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "No se pudo consultar la auditoría de cierres.");
                return StatusCode(503, new { mensaje = "No se pudo consultar la auditoría. Revisa la conexión y las tablas de Finanzas." });
            }
        }

        [HttpPost("cierres")]
        public async Task<IActionResult> SaveClosing([FromBody] ClosingRequest request)
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "La sesión venció. Inicia sesión otra vez." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return StatusCode(503, new { mensaje = "Falta configurar SQL Server." });
            if (request.EfectivoInicial < 0 || request.EfectivoContado < 0 || request.SalidasCajaEsperadas < 0 || request.EfectivoInicial > MaxAmount || request.EfectivoContado > MaxAmount || request.SalidasCajaEsperadas > MaxAmount) return BadRequest(new { mensaje = "Revisa el efectivo inicial, el efectivo contado y las salidas registradas." });
            try
            {
                await using var connection = new SqlConnection(ConnectionString); await connection.OpenAsync();
                using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
                try
                {
                    decimal cashOutflows;
                    DateTime closeAt;
                    await using (var pendingCommand = new SqlCommand(@"
DECLARE @momento datetime2(0)=SYSDATETIME();
DECLARE @ultimoCierre datetime2(0)=(SELECT MAX(FechaCierre) FROM dbo.FIN_CIERRE_TEMPORAL WITH (UPDLOCK,HOLDLOCK));
IF @ultimoCierre IS NULL SET @ultimoCierre=CONVERT(datetime2(0),CONVERT(date,@momento));
SELECT @momento MomentoCierre,
 CAST(ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE PagadoDesdeCaja=1 AND FechaRegistro>@ultimoCierre AND FechaRegistro<=@momento),0)
     +ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE PagadoDesdeCaja=1 AND FechaRegistro>@ultimoCierre AND FechaRegistro<=@momento),0) AS decimal(12,2)) SalidasCaja;", connection, transaction))
                    await using (var reader = await pendingCommand.ExecuteReaderAsync())
                    {
                        await reader.ReadAsync();
                        closeAt = reader.GetDateTime(0);
                        cashOutflows = Money(reader, 1);
                    }
                    if (Math.Abs(cashOutflows - request.SalidasCajaEsperadas) >= 0.005m)
                    {
                        await transaction.RollbackAsync();
                        return Conflict(new { mensaje = "Se registraron nuevas salidas desde que abriste el cierre. Actualiza el resumen y vuelve a revisar el cálculo." });
                    }

                    var sales = request.EfectivoContado + cashOutflows - request.EfectivoInicial;
                    if (sales < 0 || sales > MaxAmount)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { mensaje = "El cálculo da ventas negativas o supera el límite. Revisa el efectivo contado, las salidas y el efectivo inicial." });
                    }
                    var capitalExact = sales / SaleMarkupFactor;
                    var capitalEntero = decimal.Truncate(capitalExact);
                    // Solo se redondea hacia arriba si la fracción supera 0.50; el empate baja.
                    var capital = Math.Min(capitalEntero + (capitalExact - capitalEntero > 0.5m ? 1m : 0m), decimal.Truncate(sales));
                    var personal = sales - capital;
                    await using var command = new SqlCommand(@"INSERT dbo.FIN_CIERRE_TEMPORAL(FechaCierre,Ventas,CapitalAsignado,PersonalAsignado,IdUsuario,EfectivoInicial,EfectivoContado,SalidasCaja)
VALUES(@fecha,@ventas,@capital,@personal,@usuario,@efectivoInicial,@efectivoContado,@salidasCaja); SELECT CONVERT(int,SCOPE_IDENTITY());", connection, transaction);
                    command.Parameters.Add("@fecha", SqlDbType.DateTime2).Value = closeAt;
                    AddMoney(command, "@ventas", sales); AddMoney(command, "@capital", capital); AddMoney(command, "@personal", personal);
                    AddMoney(command, "@efectivoInicial", request.EfectivoInicial); AddMoney(command, "@efectivoContado", request.EfectivoContado); AddMoney(command, "@salidasCaja", cashOutflows);
                    command.Parameters.Add("@usuario", SqlDbType.Int).Value = UserId.Value;
                    var id = Convert.ToInt32(await command.ExecuteScalarAsync());
                    await transaction.CommitAsync();
                    return Ok(new { id, ventas = sales, capital, personal, salidasCaja = cashOutflows, mensaje = "Cierre guardado." });
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
            var type = (request.Tipo ?? "").Trim(); var concept = (request.Concepto ?? "").Trim();
            var sourceAccount = NormalizeAccount(request.CuentaOrigen);
            var receipt = (request.Comprobante ?? "").Trim();
            if (request.Monto <= 0 || request.Monto > MaxAmount || request.Fecha == default || request.Fecha.Date > DateTime.Today || (sourceAccount != "Capital" && sourceAccount != "Personal") || receipt.Length > 100 || (request.PagadoDesdeCaja && request.IdBanco.HasValue) || (!request.PagadoDesdeCaja && (!request.IdBanco.HasValue || request.IdBanco <= 0))) return BadRequest(new { mensaje = "Indica un monto, fecha, fondo y origen válidos. Si salió de banco, selecciona cuál." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return StatusCode(503, new { mensaje = "Falta configurar SQL Server." });
            try
            {
                await using var connection = new SqlConnection(ConnectionString); await connection.OpenAsync();
                if (type == "Personal")
                {
                    if (request.IdPeriodoGastoPersonal <= 0) return BadRequest(new { mensaje = "Selecciona el gasto personal que se pagó." });
                    using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
                    try
                    {
                        await using var command = new SqlCommand(@"
DECLARE @saldo decimal(12,2);
SELECT @saldo=p.MontoProgramado-ISNULL(SUM(pg.Monto),0) FROM dbo.GASTO_PERSONAL_PERIODO p WITH(UPDLOCK,HOLDLOCK) LEFT JOIN dbo.PAGO_GASTO_PERSONAL pg ON pg.IdPeriodoGastoPersonal=p.IdPeriodoGastoPersonal WHERE p.IdPeriodoGastoPersonal=@periodo GROUP BY p.MontoProgramado;
IF @saldo IS NULL THROW 51021,'No se encontró el gasto personal pendiente.',1;
IF @monto>@saldo THROW 51022,'El monto supera el saldo pendiente de ese gasto.',1;
INSERT dbo.PAGO_GASTO_PERSONAL(IdPeriodoGastoPersonal,IdUsuario,Monto,FechaPago,MedioPago,Referencia,Observaciones,CuentaOrigen,PagadoDesdeCaja,IdBanco) VALUES(@periodo,@usuario,@monto,@fecha,@medioPago,@referencia,@concepto,@cuentaOrigen,@pagadoDesdeCaja,@idBanco);", connection, (SqlTransaction)transaction);
                        command.Parameters.Add("@periodo", SqlDbType.Int).Value = request.IdPeriodoGastoPersonal; command.Parameters.Add("@usuario", SqlDbType.Int).Value = UserId.Value;
                        AddMoney(command, "@monto", request.Monto); command.Parameters.Add("@fecha", SqlDbType.DateTime2).Value = request.Fecha.Date;
                        command.Parameters.Add("@medioPago", SqlDbType.VarChar, 20).Value = request.PagadoDesdeCaja ? "Efectivo" : "Transferencia";
                        command.Parameters.Add("@cuentaOrigen", SqlDbType.VarChar, 15).Value = sourceAccount; command.Parameters.Add("@pagadoDesdeCaja", SqlDbType.Bit).Value = request.PagadoDesdeCaja;
                        command.Parameters.Add("@idBanco", SqlDbType.Int).Value = request.IdBanco.HasValue ? (object)request.IdBanco.Value : DBNull.Value; command.Parameters.Add("@referencia", SqlDbType.VarChar, 100).Value = receipt.Length == 0 ? (object)DBNull.Value : receipt;
                        command.Parameters.Add("@concepto", SqlDbType.VarChar, 250).Value = string.IsNullOrWhiteSpace(concept) ? "Pago de gasto personal" : concept;
                        await command.ExecuteNonQueryAsync(); transaction.Commit();
                    }
                    catch { transaction.Rollback(); throw; }
                }
                else if (type == "Empresa")
                {
                    if (string.IsNullOrWhiteSpace(concept) || concept.Length > 150) return BadRequest(new { mensaje = "Escribe el concepto de la salida de empresa (máximo 150 caracteres)." });
                    await using var command = new SqlCommand("INSERT dbo.FIN_MOVIMIENTO(Tipo,IdCompra,IdUsuario,Fecha,Concepto,Monto,MedioPago,Referencia,CuentaOrigen,PagadoDesdeCaja,IdBanco) VALUES('Empresa',NULL,@usuario,@fecha,@concepto,@monto,@medioPago,@referencia,@cuentaOrigen,@pagadoDesdeCaja,@idBanco); SELECT CONVERT(int,SCOPE_IDENTITY());", connection);
                    command.Parameters.Add("@usuario", SqlDbType.Int).Value = UserId.Value; command.Parameters.Add("@fecha", SqlDbType.DateTime2).Value = request.Fecha.Date;
                    command.Parameters.Add("@medioPago", SqlDbType.VarChar, 20).Value = request.PagadoDesdeCaja ? "Efectivo" : "Transferencia";
                    command.Parameters.Add("@cuentaOrigen", SqlDbType.VarChar, 15).Value = sourceAccount; command.Parameters.Add("@pagadoDesdeCaja", SqlDbType.Bit).Value = request.PagadoDesdeCaja;
                    command.Parameters.Add("@idBanco", SqlDbType.Int).Value = request.IdBanco.HasValue ? (object)request.IdBanco.Value : DBNull.Value; command.Parameters.Add("@referencia", SqlDbType.VarChar, 100).Value = receipt.Length == 0 ? (object)DBNull.Value : receipt;
                    command.Parameters.Add("@concepto", SqlDbType.VarChar, 150).Value = concept; AddMoney(command, "@monto", request.Monto);
                    await command.ExecuteScalarAsync();
                }
                else return BadRequest(new { mensaje = "Selecciona si la salida es personal o de empresa." });
                return Ok(new { responsable = UserName, mensaje = "Salida registrada." });
            }
            catch (SqlException ex) when (ex.Number is 51021 or 51022)
            { return Conflict(new { mensaje = ex.Message }); }
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

        public sealed class ClosingRequest { public decimal EfectivoInicial { get; set; } public decimal EfectivoContado { get; set; } public decimal SalidasCajaEsperadas { get; set; } }
        public class DepositRequest { public string? Cuenta { get; set; } public decimal Monto { get; set; } public DateTime Fecha { get; set; } public string? Comprobante { get; set; } }
        public sealed class DepositCorrectionRequest : DepositRequest { public string? Motivo { get; set; } }
        public sealed class DepositCancelRequest { public string? Motivo { get; set; } }
        public sealed class ExpenseRequest { public string? Tipo { get; set; } public string? CuentaOrigen { get; set; } public bool PagadoDesdeCaja { get; set; } public int? IdBanco { get; set; } public int IdPeriodoGastoPersonal { get; set; } public decimal Monto { get; set; } public DateTime Fecha { get; set; } public string? Concepto { get; set; } public string? Comprobante { get; set; } }
        public sealed class PersonalExpense { public int IdPeriodo { get; set; } public string Nombre { get; set; } = ""; public decimal Saldo { get; set; } }
        public sealed class CloseAudit { public int Id { get; set; } public string Fecha { get; set; } = ""; public string FechaIso { get; set; } = ""; public string Tipo { get; set; } = ""; public decimal Ventas { get; set; } public decimal? Capital { get; set; } public decimal? Ganancia { get; set; } public string Responsable { get; set; } = ""; public string Cuenta { get; set; } = ""; public string Comprobante { get; set; } = ""; public bool Anulado { get; set; } }
        public sealed class FinanceSummary
        {
            public string Responsable { get; set; } = ""; public string Fecha { get; set; } = ""; public string Mes { get; set; } = "";
            public decimal Capital { get; set; } public decimal CapitalGeneradoTotal { get; set; } public decimal Personal { get; set; } public decimal PersonalGeneradoTotal { get; set; } public decimal DepositadoCapital { get; set; } public decimal DepositadoPersonal { get; set; } public decimal CapitalDepositosMasCaja { get; set; } public decimal GananciaDepositosMasCaja { get; set; }
            public decimal ObligacionesMes { get; set; } public decimal PagadoPersonal { get; set; } public decimal AsignadoPersonalMes { get; set; } public decimal VentasManualMes { get; set; }
            public decimal VentasMesTotal { get; set; } public decimal CapitalGeneradoMes { get; set; } public decimal SalidasCapitalMes { get; set; } public decimal SalidasPersonalMes { get; set; } public decimal SalidasCajaPendientes { get; set; }
            public string ProximoGasto { get; set; } = ""; public string FechaProximoGasto { get; set; } = "";
            public List<PersonalExpense> Gastos { get; } = new List<PersonalExpense>();
        }
    }
}
