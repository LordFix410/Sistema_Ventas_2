using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ImagenPDF.Controllers
{
    [ApiController]
    [Route("api/finanzas/conciliaciones")]
    public sealed class FinanzasConciliacionController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FinanzasConciliacionController> _logger;
        public FinanzasConciliacionController(IConfiguration configuration, ILogger<FinanzasConciliacionController> logger)
        { _configuration = configuration; _logger = logger; }
        private string? ConnectionString => _configuration.GetConnectionString("ProductDb");
        private int? UserId => HttpContext.Session.GetInt32("ProductUserId");

        private const string BalancesSql = @"
DECLARE @corteId int=(SELECT MAX(IdConciliacion) FROM dbo.FIN_CONCILIACION);
DECLARE @corte datetime2(0)=(SELECT FechaRegistro FROM dbo.FIN_CONCILIACION WHERE IdConciliacion=@corteId);
WITH BankFunds AS (
 SELECT b.IdBanco,b.Nombre,'Capital' Fondo,CAST(
   COALESCE((SELECT TOP(1) d.SaldoReal FROM dbo.FIN_CONCILIACION_DETALLE d WHERE d.IdConciliacion=@corteId AND d.IdBanco=b.IdBanco AND d.Fondo='Capital'),b.SaldoCapitalInicial)
   +ISNULL((SELECT SUM(x.CapitalDepositado) FROM dbo.FIN_DEPOSITO_LOTE_CIERRE x JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=x.IdLote WHERE l.IdBanco=b.IdBanco AND l.Anulado=0 AND (@corte IS NULL OR l.FechaRegistro>@corte)),0)
   -ISNULL((SELECT SUM(Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA WHERE IdBancoOrigen=b.IdBanco AND Fondo='Capital' AND AfectaSaldo=1 AND (@corte IS NULL OR FechaRegistro>@corte)),0)
   +ISNULL((SELECT SUM(Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA WHERE IdBancoDestino=b.IdBanco AND Fondo='Capital' AND AfectaSaldo=1 AND (@corte IS NULL OR FechaRegistro>@corte)),0)
   -ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE IdBanco=b.IdBanco AND Anulado=0 AND CuentaOrigen IN ('Capital','Caja') AND (@corte IS NULL OR FechaRegistro>@corte)),0)
   -ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE IdBanco=b.IdBanco AND Anulado=0 AND CuentaOrigen='Capital' AND (@corte IS NULL OR FechaRegistro>@corte)),0) AS decimal(12,2)) Esperado
 FROM dbo.FIN_BANCO_TEMPORAL b WHERE b.Activo=1
 UNION ALL
 SELECT b.IdBanco,b.Nombre,'Ganancia',CAST(
   COALESCE((SELECT TOP(1) d.SaldoReal FROM dbo.FIN_CONCILIACION_DETALLE d WHERE d.IdConciliacion=@corteId AND d.IdBanco=b.IdBanco AND d.Fondo='Ganancia'),b.SaldoGananciaInicial)
   +ISNULL((SELECT SUM(x.GananciaDepositada) FROM dbo.FIN_DEPOSITO_LOTE_CIERRE x JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=x.IdLote WHERE l.IdBanco=b.IdBanco AND l.Anulado=0 AND (@corte IS NULL OR l.FechaRegistro>@corte)),0)
   -ISNULL((SELECT SUM(Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA WHERE IdBancoOrigen=b.IdBanco AND Fondo IN ('Ganancia','Personal') AND AfectaSaldo=1 AND (@corte IS NULL OR FechaRegistro>@corte)),0)
   +ISNULL((SELECT SUM(Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA WHERE IdBancoDestino=b.IdBanco AND Fondo IN ('Ganancia','Personal') AND AfectaSaldo=1 AND (@corte IS NULL OR FechaRegistro>@corte)),0)
   -ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE IdBanco=b.IdBanco AND Anulado=0 AND CuentaOrigen='Personal' AND (@corte IS NULL OR FechaRegistro>@corte)),0)
   -ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE IdBanco=b.IdBanco AND Anulado=0 AND CuentaOrigen='Personal' AND (@corte IS NULL OR FechaRegistro>@corte)),0) AS decimal(12,2))
 FROM dbo.FIN_BANCO_TEMPORAL b WHERE b.Activo=1
), FundTotals AS (
 SELECT 'Capital' Fondo,CAST(COALESCE((SELECT SUM(d.SaldoReal) FROM dbo.FIN_CONCILIACION_DETALLE d WHERE d.IdConciliacion=@corteId AND d.Fondo='Capital'),(SELECT ISNULL(SUM(SaldoCapitalInicial),0)+150.00 FROM dbo.FIN_BANCO_TEMPORAL WHERE Activo=1))
   +ISNULL((SELECT SUM(CapitalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL WHERE @corte IS NULL OR FechaRegistro>@corte),0)
   +ISNULL((SELECT SUM(Monto) FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL WHERE Cuenta='Capital' AND (@corte IS NULL OR Fecha>@corte)),0)
   -ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE IdBanco IS NULL AND Anulado=0 AND (@corte IS NULL OR FechaRegistro>@corte)),0)
   -ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE IdBanco IS NOT NULL AND Anulado=0 AND CuentaOrigen IN ('Capital','Caja') AND (@corte IS NULL OR FechaRegistro>@corte)),0)
   -ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE CuentaOrigen='Capital' AND Anulado=0 AND (@corte IS NULL OR FechaRegistro>@corte)),0) AS decimal(12,2)) Total
 UNION ALL
 SELECT 'Ganancia',CAST(COALESCE((SELECT SUM(d.SaldoReal) FROM dbo.FIN_CONCILIACION_DETALLE d WHERE d.IdConciliacion=@corteId AND d.Fondo='Ganancia'),(SELECT ISNULL(SUM(SaldoGananciaInicial),0)+100.00 FROM dbo.FIN_BANCO_TEMPORAL WHERE Activo=1))
   +ISNULL((SELECT SUM(PersonalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL WHERE @corte IS NULL OR FechaRegistro>@corte),0)
   +ISNULL((SELECT SUM(Monto) FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL WHERE Cuenta='Personal' AND (@corte IS NULL OR Fecha>@corte)),0)
   -ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE IdBanco IS NULL AND Anulado=0 AND (@corte IS NULL OR FechaRegistro>@corte)),0)
   -ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE IdBanco IS NOT NULL AND Anulado=0 AND CuentaOrigen='Personal' AND (@corte IS NULL OR FechaRegistro>@corte)),0)
   -ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE CuentaOrigen='Personal' AND Anulado=0 AND (@corte IS NULL OR FechaRegistro>@corte)),0) AS decimal(12,2)) Total
), BankTotals AS (
 SELECT Fondo,SUM(Esperado) Total FROM BankFunds GROUP BY Fondo
)
SELECT IdBanco,Nombre,Fondo,Esperado FROM BankFunds
UNION ALL SELECT CAST(NULL AS int),'Efectivo en caja',t.Fondo,CAST(t.Total-ISNULL(b.Total,0) AS decimal(12,2)) FROM FundTotals t LEFT JOIN BankTotals b ON b.Fondo=t.Fondo;";

        [HttpGet]
        public async Task<IActionResult> Current()
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "Inicia sesión para consultar conciliaciones." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return StatusCode(503, new { mensaje = "Falta configurar SQL Server." });
            try
            {
                await using var connection = new SqlConnection(ConnectionString); await connection.OpenAsync();
                var rows = await ReadBalances(connection, null);
                var banks = rows.Where(x => x.IdBanco.HasValue).GroupBy(x => new { x.IdBanco, x.Nombre }).Select(g => new { idBanco = g.Key.IdBanco, nombre = g.Key.Nombre, capital = g.First(x => x.Fondo == "Capital").Esperado, ganancia = g.First(x => x.Fondo == "Ganancia").Esperado }).ToList();
                var cash = rows.Where(x => !x.IdBanco.HasValue).ToDictionary(x => x.Fondo, x => x.Esperado);
                var audit = new List<object>();
                await using (var command = new SqlCommand(@"SELECT TOP(10) c.IdConciliacion,c.Fecha,c.Motivo,c.FechaRegistro,u.NombreCompleto,
SUM(CASE WHEN d.Ajuste<0 THEN -d.Ajuste ELSE 0 END) Faltante,
SUM(CASE WHEN d.Ajuste>0 THEN d.Ajuste ELSE 0 END) Sobrante,
(SELECT COALESCE(b.Nombre,'Efectivo en caja') Ubicacion,x.Fondo,x.SaldoEsperado Esperado,x.SaldoReal Real,x.Ajuste
 FROM dbo.FIN_CONCILIACION_DETALLE x LEFT JOIN dbo.FIN_BANCO_TEMPORAL b ON b.IdBanco=x.IdBanco
 WHERE x.IdConciliacion=c.IdConciliacion FOR JSON PATH) Detalles
FROM dbo.FIN_CONCILIACION c LEFT JOIN dbo.FIN_CONCILIACION_DETALLE d ON d.IdConciliacion=c.IdConciliacion LEFT JOIN dbo.USUARIO u ON u.IdUsuario=c.IdUsuario
GROUP BY c.IdConciliacion,c.Fecha,c.Motivo,c.FechaRegistro,u.NombreCompleto ORDER BY c.FechaRegistro DESC,c.IdConciliacion DESC;", connection))
                await using (var reader = await command.ExecuteReaderAsync()) while (await reader.ReadAsync())
                {
                    var details = JsonDocument.Parse(reader.GetString(7)).RootElement.Clone();
                    audit.Add(new { id=reader.GetInt32(0),fecha=reader.GetDateTime(1).ToString("dd/MM/yyyy"),motivo=reader.GetString(2),registrada=reader.GetDateTime(3).ToString("dd/MM/yyyy HH:mm"),responsable=reader.IsDBNull(4)?"":reader.GetString(4),faltante=reader.GetDecimal(5),sobrante=reader.GetDecimal(6),detalles=details });
                }
                var unlinkedClosings = new List<object>();
                await using (var command = new SqlCommand(@"SELECT TOP(100) c.IdCierreTemporal,c.FechaCierre,c.Ventas,
 c.CapitalAsignado-c.SalidasCapitalCaja CapitalNeto,
 c.PersonalAsignado-c.SalidasGananciaCaja GananciaNeta,c.Conciliado
FROM dbo.FIN_CIERRE_TEMPORAL c
WHERE NOT EXISTS(SELECT 1 FROM dbo.FIN_DEPOSITO_LOTE_CIERRE x JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=x.IdLote WHERE x.IdCierreTemporal=c.IdCierreTemporal AND l.Anulado=0)
ORDER BY c.FechaCierre DESC,c.IdCierreTemporal DESC;", connection))
                await using (var reader = await command.ExecuteReaderAsync()) while (await reader.ReadAsync())
                    unlinkedClosings.Add(new { id = reader.GetInt32(0), fecha = reader.GetDateTime(1).ToString("yyyy-MM-dd"), ventas = reader.GetDecimal(2), capital = reader.GetDecimal(3), ganancia = reader.GetDecimal(4), marcadoComoDepositado = reader.GetBoolean(5) });
                return Ok(new { bancos = banks, caja = new { capital = cash.GetValueOrDefault("Capital"), ganancia = cash.GetValueOrDefault("Ganancia") }, historial = audit, cierresSinBoleta = unlinkedClosings });
            }
            catch (SqlException ex) { _logger.LogError(ex, "No se pudo consultar conciliación financiera."); return StatusCode(503, new { mensaje = "No se pudo consultar los saldos. Aplica la migración financiera web y vuelve a intentar." }); }
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] ReconciliationRequest request)
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "La sesión venció. Inicia sesión otra vez." });
            var reason = (request.Motivo ?? "").Trim();
            if (reason.Length < 5)
                return BadRequest(new { mensaje = "Escribe un motivo de al menos 5 caracteres para guardar la conciliación." });
            if (reason.Length > 250)
                return BadRequest(new { mensaje = "El motivo no puede superar los 250 caracteres." });
            if (request.Fecha == default || request.Fecha.Date > DateTime.Today || request.Saldos == null || request.Saldos.Count < 2 || request.Saldos.Any(x => x.SaldoCapital < 0 || x.SaldoGanancia < 0) || request.Saldos.Select(x => x.IdBanco).Distinct().Count() != request.Saldos.Count || request.Cierres?.Any(x => x.IdCierre <= 0 || x.Fecha == default || x.Fecha.Date > request.Fecha.Date) == true || request.Cierres?.Select(x => x.IdCierre).Distinct().Count() != request.Cierres?.Count)
                return BadRequest(new { mensaje = "Indica fecha, motivo, caja y saldos reales válidos para cada banco." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return StatusCode(503, new { mensaje = "Falta configurar SQL Server." });
            try
            {
                await using var connection = new SqlConnection(ConnectionString); await connection.OpenAsync();
                using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
                try
                {
                    await using (var lockBanks = new SqlCommand("SELECT IdBanco FROM dbo.FIN_BANCO_TEMPORAL WITH(UPDLOCK,HOLDLOCK) WHERE Activo=1 ORDER BY IdBanco;", connection, transaction))
                    await using (var reader = await lockBanks.ExecuteReaderAsync()) while (await reader.ReadAsync()) { }
                    var expected = await ReadBalances(connection, transaction);
                    var expectedBanks = expected.Where(x => x.IdBanco.HasValue).ToList();
                    if (expectedBanks.Select(x => x.IdBanco).Distinct().Count() != request.Saldos.Count - 1 || request.Saldos.Count(x => !x.IdBanco.HasValue) != 1 || request.Saldos.Any(x => x.IdBanco.HasValue && !expectedBanks.Any(y => y.IdBanco == x.IdBanco)))
                        throw new InvalidOperationException("La lista de bancos cambió. Actualiza la página antes de conciliar.");
                    var closureStates = request.Cierres ?? new List<ReconciliationClosureState>();
                    if (closureStates.Count > 100) throw new InvalidOperationException("Actualiza la página y selecciona cierres de nuevo.");
                    if (closureStates.Count > 0)
                    {
                        var placeholders = closureStates.Select((_, index) => "@cierre" + index).ToArray();
                        await using (var validateClosings = new SqlCommand($@"SELECT COUNT(*) FROM dbo.FIN_CIERRE_TEMPORAL c WITH(UPDLOCK,HOLDLOCK)
WHERE c.IdCierreTemporal IN ({string.Join(",", placeholders)}) AND c.FechaCierre<DATEADD(day,1,@fecha)
 AND NOT EXISTS(SELECT 1 FROM dbo.FIN_DEPOSITO_LOTE_CIERRE x JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=x.IdLote WHERE x.IdCierreTemporal=c.IdCierreTemporal AND l.Anulado=0);", connection, transaction))
                        {
                            validateClosings.Parameters.Add("@fecha", SqlDbType.Date).Value = request.Fecha.Date;
                            for (var index = 0; index < closureStates.Count; index++) validateClosings.Parameters.Add(placeholders[index], SqlDbType.Int).Value = closureStates[index].IdCierre;
                            if (Convert.ToInt32(await validateClosings.ExecuteScalarAsync()) != closureStates.Count) throw new InvalidOperationException("Uno de los cierres cambió o ya tiene una boleta. Actualiza la página.");
                        }
                    }
                    int id;
                    await using (var insert = new SqlCommand("INSERT dbo.FIN_CONCILIACION(Fecha,Motivo,IdUsuario) VALUES(@fecha,@motivo,@usuario); SELECT CONVERT(int,SCOPE_IDENTITY());", connection, transaction))
                    {
                        insert.Parameters.Add("@fecha", SqlDbType.Date).Value = request.Fecha.Date; insert.Parameters.Add("@motivo", SqlDbType.VarChar, 250).Value = reason; insert.Parameters.Add("@usuario", SqlDbType.Int).Value = UserId.Value;
                        id = Convert.ToInt32(await insert.ExecuteScalarAsync());
                    }
                    foreach (var line in request.Saldos)
                    {
                        foreach (var fund in new[] { "Capital", "Ganancia" })
                        {
                            var current = expected.Single(x => x.IdBanco == line.IdBanco && x.Fondo == fund);
                            var real = fund == "Capital" ? line.SaldoCapital : line.SaldoGanancia;
                            await using var detail = new SqlCommand("INSERT dbo.FIN_CONCILIACION_DETALLE(IdConciliacion,IdBanco,Fondo,SaldoEsperado,SaldoReal,Ajuste) VALUES(@id,@banco,@fondo,@esperado,@real,@ajuste);", connection, transaction);
                            detail.Parameters.Add("@id", SqlDbType.Int).Value = id; detail.Parameters.Add("@banco", SqlDbType.Int).Value = (object?)line.IdBanco ?? DBNull.Value; detail.Parameters.Add("@fondo", SqlDbType.VarChar, 12).Value = fund;
                            AddMoney(detail, "@esperado", current.Esperado); AddMoney(detail, "@real", real); AddMoney(detail, "@ajuste", real - current.Esperado); await detail.ExecuteNonQueryAsync();
                        }
                    }
                    // El saldo real guardado es el nuevo punto de partida. Todo cierre anterior
                    // queda absorbido por ese saldo y no puede volver a cobrarse como pendiente.
                    await using (var settleClosings = new SqlCommand(@"UPDATE dbo.FIN_CIERRE_TEMPORAL SET Conciliado=1
WHERE FechaCierre<DATEADD(day,1,@fecha) AND Conciliado=0;", connection, transaction))
                    {
                        settleClosings.Parameters.Add("@fecha", SqlDbType.Date).Value = request.Fecha.Date;
                        await settleClosings.ExecuteNonQueryAsync();
                    }
                    await transaction.CommitAsync(); return Ok(new { id, mensaje = "Conciliación registrada. Solo se actualizaron los estados de cierre que seleccionaste; todo el historial se conserva." });
                }
                catch { if (transaction.Connection != null) await transaction.RollbackAsync(); throw; }
            }
            catch (InvalidOperationException ex) { return Conflict(new { mensaje = ex.Message }); }
            catch (SqlException ex) { _logger.LogError(ex, "No se pudo guardar conciliación financiera."); return StatusCode(503, new { mensaje = "No se pudo guardar la conciliación. Verifica que la migración esté aplicada." }); }
        }

        private static async Task<List<BalanceLine>> ReadBalances(SqlConnection connection, SqlTransaction? transaction)
        {
            var rows = new List<BalanceLine>();
            await using var command = new SqlCommand(BalancesSql, connection, transaction);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) rows.Add(new BalanceLine { IdBanco = reader.IsDBNull(0) ? (int?)null : reader.GetInt32(0), Nombre = reader.GetString(1), Fondo = reader.GetString(2), Esperado = reader.GetDecimal(3) });
            return rows;
        }
        private static void AddMoney(SqlCommand command, string name, decimal value) { var p = command.Parameters.Add(name, SqlDbType.Decimal); p.Precision = 12; p.Scale = 2; p.Value = value; }
        public sealed class ReconciliationRequest { public DateTime Fecha { get; set; } public string? Motivo { get; set; } public List<ReconciliationLine>? Saldos { get; set; } public List<ReconciliationClosureState>? Cierres { get; set; } }
        public sealed class ReconciliationLine { public int? IdBanco { get; set; } public decimal SaldoCapital { get; set; } public decimal SaldoGanancia { get; set; } }
        public sealed class ReconciliationClosureState { public int IdCierre { get; set; } public DateTime Fecha { get; set; } public bool YaDepositado { get; set; } }
        private sealed class BalanceLine { public int? IdBanco { get; set; } public string Nombre { get; set; } = ""; public string Fondo { get; set; } = ""; public decimal Esperado { get; set; } }
    }
}
