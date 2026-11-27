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
    [Route("api/finanzas/operaciones")]
    public sealed class FinanzasOperativasController : ControllerBase
    {
        private const decimal MaxAmount = 999999999.99m;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FinanzasOperativasController> _logger;
        public FinanzasOperativasController(IConfiguration configuration, ILogger<FinanzasOperativasController> logger)
        { _configuration = configuration; _logger = logger; }
        private string? ConnectionString => _configuration.GetConnectionString("ProductDb");
        private int? UserId => HttpContext.Session.GetInt32("ProductUserId");
        private string UserName => HttpContext.Session.GetString("ProductUserName") ?? "Usuario";

        [HttpGet("depositos")]
        public async Task<IActionResult> DepositData()
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "Inicia sesión para registrar depósitos." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return StatusCode(503, new { mensaje = "Falta configurar SQL Server." });
            try
            {
                await using var connection = new SqlConnection(ConnectionString); await connection.OpenAsync();
                var response = new DepositDataResponse();
                await using (var command = new SqlCommand(@"
SELECT b.IdBanco,b.Nombre,b.SaldoInicial,b.SaldoCapitalInicial,b.SaldoGananciaInicial,
 b.SaldoInicial+ISNULL((SELECT SUM(l.MontoTotal) FROM dbo.FIN_DEPOSITO_LOTE l WHERE l.IdBanco=b.IdBanco AND l.Anulado=0),0)
 -ISNULL((SELECT SUM(t.Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA t WHERE t.IdBancoOrigen=b.IdBanco),0)
 +ISNULL((SELECT SUM(t.Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA t WHERE t.IdBancoDestino=b.IdBanco),0)
 -ISNULL((SELECT SUM(m.Monto) FROM dbo.FIN_MOVIMIENTO m WHERE m.IdBanco=b.IdBanco AND m.Anulado=0),0)
 -ISNULL((SELECT SUM(p.Monto) FROM dbo.PAGO_GASTO_PERSONAL p WHERE p.IdBanco=b.IdBanco AND p.Anulado=0),0)
 +ISNULL((SELECT SUM(d.Ajuste) FROM dbo.FIN_CONCILIACION_DETALLE d WHERE d.IdBanco=b.IdBanco),0) Saldo
FROM dbo.FIN_BANCO_TEMPORAL b WHERE b.Activo=1 ORDER BY b.Nombre;", connection))
                await using (var reader = await command.ExecuteReaderAsync())
                    while (await reader.ReadAsync()) response.Bancos.Add(new BankItem { Id = reader.GetInt32(0), Nombre = reader.GetString(1), SaldoInicial = reader.GetDecimal(2), SaldoCapitalInicial = reader.GetDecimal(3), SaldoGananciaInicial = reader.GetDecimal(4), Saldo = reader.GetDecimal(5) });

                await using (var command = new SqlCommand(@"
SELECT c.IdCierreTemporal,c.FechaCierre,c.Ventas,c.CapitalAsignado,c.PersonalAsignado,u.NombreCompleto,
       c.CapitalAsignado-c.SalidasCapitalCaja,c.PersonalAsignado-c.SalidasGananciaCaja,c.SalidasCapitalCaja,c.SalidasGananciaCaja
FROM dbo.FIN_CIERRE_TEMPORAL c LEFT JOIN dbo.USUARIO u ON u.IdUsuario=c.IdUsuario
WHERE c.Conciliado=0
 AND NOT EXISTS(SELECT 1 FROM dbo.FIN_DEPOSITO_LOTE_CIERRE x JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=x.IdLote WHERE x.IdCierreTemporal=c.IdCierreTemporal AND l.Anulado=0)
ORDER BY c.FechaCierre,c.IdCierreTemporal;", connection))
                await using (var reader = await command.ExecuteReaderAsync())
                    while (await reader.ReadAsync()) response.Cierres.Add(new ClosingItem { Id = reader.GetInt32(0), Fecha = reader.GetDateTime(1).ToString("dd/MM/yyyy HH:mm"), Ventas = reader.GetDecimal(2), Capital = reader.GetDecimal(3), Ganancia = reader.GetDecimal(4), Responsable = reader.IsDBNull(5) ? "" : reader.GetString(5), CapitalNeto = reader.GetDecimal(6), GananciaNeta = reader.GetDecimal(7), SalidasCapital = reader.GetDecimal(8), SalidasGanancia = reader.GetDecimal(9) });

                await using (var command = new SqlCommand(@"
SELECT b.IdBanco,b.Nombre,
 b.SaldoGananciaInicial+ISNULL((SELECT SUM(x.GananciaDepositada) FROM dbo.FIN_DEPOSITO_LOTE_CIERRE x JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=x.IdLote WHERE l.IdBanco=b.IdBanco AND l.Anulado=0),0)
 -ISNULL((SELECT SUM(t.Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA t WHERE t.IdBancoOrigen=b.IdBanco AND t.Fondo='Ganancia'),0)
 +ISNULL((SELECT SUM(t.Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA t WHERE t.IdBancoDestino=b.IdBanco AND t.Fondo='Ganancia'),0)
 -ISNULL((SELECT SUM(m.Monto) FROM dbo.FIN_MOVIMIENTO m WHERE m.IdBanco=b.IdBanco AND m.Anulado=0 AND m.CuentaOrigen='Personal'),0)
 -ISNULL((SELECT SUM(p.Monto) FROM dbo.PAGO_GASTO_PERSONAL p WHERE p.IdBanco=b.IdBanco AND p.Anulado=0 AND p.CuentaOrigen='Personal'),0)
 +ISNULL((SELECT SUM(d.Ajuste) FROM dbo.FIN_CONCILIACION_DETALLE d WHERE d.IdBanco=b.IdBanco AND d.Fondo='Ganancia'),0) PendienteGanancia,
 b.SaldoCapitalInicial+ISNULL((SELECT SUM(x.CapitalDepositado) FROM dbo.FIN_DEPOSITO_LOTE_CIERRE x JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=x.IdLote WHERE l.IdBanco=b.IdBanco AND l.Anulado=0),0)
 -ISNULL((SELECT SUM(t.Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA t WHERE t.IdBancoOrigen=b.IdBanco AND t.Fondo='Capital'),0)
 +ISNULL((SELECT SUM(t.Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA t WHERE t.IdBancoDestino=b.IdBanco AND t.Fondo='Capital'),0)
 -ISNULL((SELECT SUM(m.Monto) FROM dbo.FIN_MOVIMIENTO m WHERE m.IdBanco=b.IdBanco AND m.Anulado=0 AND m.CuentaOrigen='Capital'),0)
 -ISNULL((SELECT SUM(p.Monto) FROM dbo.PAGO_GASTO_PERSONAL p WHERE p.IdBanco=b.IdBanco AND p.Anulado=0 AND p.CuentaOrigen='Capital'),0)
 +ISNULL((SELECT SUM(d.Ajuste) FROM dbo.FIN_CONCILIACION_DETALLE d WHERE d.IdBanco=b.IdBanco AND d.Fondo='Capital'),0) PendienteCapital
FROM dbo.FIN_BANCO_TEMPORAL b WHERE b.Activo=1 ORDER BY b.Nombre;", connection))
                await using (var reader = await command.ExecuteReaderAsync())
                    while (await reader.ReadAsync()) response.FondosBanco.Add(new BankFundItem { IdBanco = reader.GetInt32(0), Banco = reader.GetString(1), Ganancia = reader.GetDecimal(2), Capital = reader.GetDecimal(3) });
                return Ok(response);
            }
            catch (SqlException ex)
            { _logger.LogError(ex, "No se pudieron cargar datos para depósitos."); return StatusCode(503, new { mensaje = "No se pudieron cargar depósitos y bancos. Aplica la migración de módulos financieros." }); }
        }

        [HttpGet("salidas/catalogo")]
        public async Task<IActionResult> ExpenseCatalog()
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "Inicia sesión para registrar salidas." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return StatusCode(503, new { mensaje = "Falta configurar SQL Server." });
            try
            {
                await using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();
                var expenses = new List<object>();
                await using (var command = new SqlCommand(@"
SELECT g.IdGastoPersonal,g.Nombre,p.IdPeriodoGastoPersonal,
       CAST(CASE WHEN p.IdPeriodoGastoPersonal IS NULL THEN 0 ELSE p.MontoProgramado-ISNULL(x.Pagado,0) END AS decimal(12,2)) Saldo
FROM dbo.GASTO_PERSONAL g
LEFT JOIN dbo.GASTO_PERSONAL_PERIODO p ON p.IdGastoPersonal=g.IdGastoPersonal AND p.Anio=YEAR(GETDATE()) AND p.Mes=MONTH(GETDATE())
LEFT JOIN (SELECT IdPeriodoGastoPersonal,SUM(Monto) Pagado FROM dbo.PAGO_GASTO_PERSONAL WHERE Anulado=0 GROUP BY IdPeriodoGastoPersonal) x ON x.IdPeriodoGastoPersonal=p.IdPeriodoGastoPersonal
WHERE g.Activo=1 ORDER BY g.Nombre;", connection))
                await using (var reader = await command.ExecuteReaderAsync())
                    while (await reader.ReadAsync()) expenses.Add(new { id = reader.GetInt32(0), nombre = reader.GetString(1), idPeriodo = reader.IsDBNull(2) ? 0 : reader.GetInt32(2), saldo = reader.GetDecimal(3) });

                var banks = new List<object>();
                await using (var command = new SqlCommand(@"
SELECT b.IdBanco,b.Nombre,f.Cuenta,
 CAST(CASE WHEN f.Cuenta='Capital' THEN b.SaldoCapitalInicial
   +ISNULL((SELECT SUM(x.CapitalDepositado) FROM dbo.FIN_DEPOSITO_LOTE_CIERRE x JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=x.IdLote WHERE l.IdBanco=b.IdBanco AND l.Anulado=0),0)
   -ISNULL((SELECT SUM(t.Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA t WHERE t.IdBancoOrigen=b.IdBanco AND t.Fondo='Capital'),0)
   +ISNULL((SELECT SUM(t.Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA t WHERE t.IdBancoDestino=b.IdBanco AND t.Fondo='Capital'),0)
   -ISNULL((SELECT SUM(m.Monto) FROM dbo.FIN_MOVIMIENTO m WHERE m.IdBanco=b.IdBanco AND m.Anulado=0 AND m.CuentaOrigen='Capital'),0)
   -ISNULL((SELECT SUM(p.Monto) FROM dbo.PAGO_GASTO_PERSONAL p WHERE p.IdBanco=b.IdBanco AND p.Anulado=0 AND p.CuentaOrigen='Capital'),0)
   +ISNULL((SELECT SUM(d.Ajuste) FROM dbo.FIN_CONCILIACION_DETALLE d WHERE d.IdBanco=b.IdBanco AND d.Fondo='Capital'),0)
 ELSE b.SaldoGananciaInicial
   +ISNULL((SELECT SUM(x.GananciaDepositada) FROM dbo.FIN_DEPOSITO_LOTE_CIERRE x JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=x.IdLote WHERE l.IdBanco=b.IdBanco AND l.Anulado=0),0)
   -ISNULL((SELECT SUM(t.Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA t WHERE t.IdBancoOrigen=b.IdBanco AND t.Fondo='Ganancia'),0)
   +ISNULL((SELECT SUM(t.Monto) FROM dbo.FIN_TRANSFERENCIA_BANCARIA t WHERE t.IdBancoDestino=b.IdBanco AND t.Fondo='Ganancia'),0)
   -ISNULL((SELECT SUM(m.Monto) FROM dbo.FIN_MOVIMIENTO m WHERE m.IdBanco=b.IdBanco AND m.Anulado=0 AND m.CuentaOrigen='Personal'),0)
   -ISNULL((SELECT SUM(p.Monto) FROM dbo.PAGO_GASTO_PERSONAL p WHERE p.IdBanco=b.IdBanco AND p.Anulado=0 AND p.CuentaOrigen='Personal'),0)
   +ISNULL((SELECT SUM(d.Ajuste) FROM dbo.FIN_CONCILIACION_DETALLE d WHERE d.IdBanco=b.IdBanco AND d.Fondo='Ganancia'),0)
 END AS decimal(12,2)) Disponible
FROM dbo.FIN_BANCO_TEMPORAL b
CROSS APPLY (VALUES(CASE WHEN UPPER(b.Nombre) LIKE '%BANRURAL%' THEN 'Capital' ELSE 'Ganancia' END)) f(Cuenta)
WHERE b.Activo=1 AND (UPPER(b.Nombre) LIKE '%BANRURAL%' OR UPPER(b.Nombre) LIKE '%BAM%')
ORDER BY CASE WHEN f.Cuenta='Ganancia' THEN 0 ELSE 1 END;", connection))
                await using (var reader = await command.ExecuteReaderAsync())
                    while (await reader.ReadAsync()) banks.Add(new { id = reader.GetInt32(0), nombre = reader.GetString(1), fondo = reader.GetString(2), disponible = reader.GetDecimal(3) });
                return Ok(new { gastos = expenses, bancos = banks });
            }
            catch (SqlException ex)
            { _logger.LogError(ex, "No se pudo cargar el catálogo de salidas."); return StatusCode(503, new { mensaje = "No se pudo cargar el catálogo. Revisa que esté aplicada la migración de origen del efectivo." }); }
        }

        [HttpPost("bancos")]
        public async Task<IActionResult> CreateBank([FromBody] BankRequest request)
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "La sesión venció." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return StatusCode(503, new { mensaje = "Falta configurar SQL Server." });
            var name = (request.Nombre ?? "").Trim();
            if (name.Length is 0 or > 80 || request.SaldoCapitalInicial < 0 || request.SaldoGananciaInicial < 0 || request.SaldoCapitalInicial + request.SaldoGananciaInicial > MaxAmount) return BadRequest(new { mensaje = "Escribe el nombre del banco y los saldos iniciales válidos." });
            try
            {
                await using var connection = new SqlConnection(ConnectionString); await connection.OpenAsync();
                await using var command = new SqlCommand("INSERT dbo.FIN_BANCO_TEMPORAL(Nombre,SaldoInicial,SaldoCapitalInicial,SaldoGananciaInicial,IdUsuario) VALUES(@nombre,@capital+@ganancia,@capital,@ganancia,@usuario); SELECT CONVERT(int,SCOPE_IDENTITY());", connection);
                command.Parameters.Add("@nombre", SqlDbType.VarChar, 80).Value = name; AddMoney(command, "@capital", request.SaldoCapitalInicial); AddMoney(command, "@ganancia", request.SaldoGananciaInicial); command.Parameters.Add("@usuario", SqlDbType.Int).Value = UserId.Value;
                var id = Convert.ToInt32(await command.ExecuteScalarAsync()); return Ok(new { id, nombre = name });
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627) { return Conflict(new { mensaje = "Ya existe una cuenta con ese nombre." }); }
            catch (SqlException ex) { _logger.LogError(ex, "No se pudo registrar cuenta bancaria."); return StatusCode(503, new { mensaje = "No se pudo guardar la cuenta bancaria." }); }
        }

        [HttpPost("depositos")]
        public async Task<IActionResult> SaveDeposit([FromBody] DepositBatchRequest request)
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "La sesión venció." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return StatusCode(503, new { mensaje = "Falta configurar SQL Server." });
            if (request.IdBanco <= 0 || request.IdsCierre == null || request.IdsCierre.Length == 0 || request.IdsCierre.Length > 100 || request.IdsCierre.Any(id => id <= 0) || request.IdsCierre.Distinct().Count() != request.IdsCierre.Length || request.Fecha == default || request.Fecha.Date > DateTime.Today || string.IsNullOrWhiteSpace(request.Comprobante) || request.Comprobante.Trim().Length > 100)
                return BadRequest(new { mensaje = "Selecciona cierres, banco, fecha y comprobante. Máximo 100 cierres por depósito." });
            try
            {
                await using var connection = new SqlConnection(ConnectionString); await connection.OpenAsync();
                using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
                try
                {
                    var ids = string.Join(",", request.IdsCierre.Distinct());
                    await using (var verify = new SqlCommand($@"
IF NOT EXISTS(SELECT 1 FROM dbo.FIN_BANCO_TEMPORAL WITH(UPDLOCK,HOLDLOCK) WHERE IdBanco=@banco AND Activo=1) THROW 51041,'No se encontró el banco seleccionado.',1;
IF (SELECT COUNT(*) FROM dbo.FIN_CIERRE_TEMPORAL WITH(UPDLOCK,HOLDLOCK) WHERE IdCierreTemporal IN ({ids})) <> @cantidad THROW 51042,'Uno o más cierres ya no están disponibles.',1;
IF EXISTS(SELECT 1 FROM dbo.FIN_CIERRE_TEMPORAL WITH(UPDLOCK,HOLDLOCK) WHERE IdCierreTemporal IN ({ids}) AND Conciliado=1) THROW 51046,'Uno o más cierres ya quedaron incluidos en una conciliación; no se deben depositar otra vez.',1;
IF EXISTS(SELECT 1 FROM dbo.FIN_DEPOSITO_LOTE_CIERRE x WITH(UPDLOCK,HOLDLOCK) JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=x.IdLote WHERE x.IdCierreTemporal IN ({ids}) AND l.Anulado=0) THROW 51043,'Uno o más cierres ya están incluidos en otro depósito.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.FIN_CIERRE_TEMPORAL WHERE IdCierreTemporal IN ({ids}) AND Ventas>0) THROW 51050,'El total seleccionado debe ser mayor que cero.',1;", connection, (SqlTransaction)transaction))
                    { verify.Parameters.Add("@banco", SqlDbType.Int).Value = request.IdBanco; verify.Parameters.Add("@cantidad", SqlDbType.Int).Value = request.IdsCierre.Length; await verify.ExecuteNonQueryAsync(); }
                    await using var insert = new SqlCommand($@"
DECLARE @capital decimal(12,2),@ganancia decimal(12,2),@ventas decimal(12,2),@salidaCapital decimal(12,2),@salidaGanancia decimal(12,2),@netoCapital decimal(12,2),@netoGanancia decimal(12,2),@totalDeposito decimal(12,2);
SELECT @capital=SUM(CapitalAsignado),@ganancia=SUM(PersonalAsignado),@ventas=SUM(Ventas),@salidaCapital=SUM(SalidasCapitalCaja),@salidaGanancia=SUM(SalidasGananciaCaja)
FROM dbo.FIN_CIERRE_TEMPORAL WHERE IdCierreTemporal IN ({ids});
SET @netoCapital=@capital-@salidaCapital; SET @netoGanancia=@ganancia-@salidaGanancia; SET @totalDeposito=@netoCapital+@netoGanancia;
IF @netoCapital<0 OR @netoGanancia<0 THROW 51051,'Las salidas en efectivo superan uno de los fondos seleccionados. Incluye cierres posteriores para cubrir el faltante antes de depositar.',1;
IF @totalDeposito<=0 THROW 51050,'El total neto a depositar debe ser mayor que cero.',1;
INSERT dbo.FIN_DEPOSITO_LOTE(IdBanco,MontoTotal,Fecha,Comprobante,IdUsuario) VALUES(@banco,@totalDeposito,@fecha,@comprobante,@usuario);
DECLARE @lote int=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.FIN_DEPOSITO_LOTE_CIERRE(IdLote,IdCierreTemporal,Ventas,Capital,Ganancia,CapitalDepositado,GananciaDepositada)
 SELECT @lote,IdCierreTemporal,Ventas,CapitalAsignado,PersonalAsignado,CapitalAsignado-SalidasCapitalCaja,PersonalAsignado-SalidasGananciaCaja FROM dbo.FIN_CIERRE_TEMPORAL WHERE IdCierreTemporal IN ({ids});
SELECT @lote,@totalDeposito,@netoCapital,@netoGanancia,@ventas,@capital,@ganancia,@salidaCapital,@salidaGanancia;", connection, (SqlTransaction)transaction);
                    insert.Parameters.Add("@banco", SqlDbType.Int).Value = request.IdBanco; insert.Parameters.Add("@fecha", SqlDbType.Date).Value = request.Fecha.Date;
                    insert.Parameters.Add("@comprobante", SqlDbType.VarChar, 100).Value = request.Comprobante.Trim(); insert.Parameters.Add("@usuario", SqlDbType.Int).Value = UserId.Value;
                    int id; decimal deposited, capital, gain, sales, grossCapital, grossGain, cashCapital, cashGain; await using (var reader = await insert.ExecuteReaderAsync()) { await reader.ReadAsync(); id = reader.GetInt32(0); deposited = reader.GetDecimal(1); capital = reader.GetDecimal(2); gain = reader.GetDecimal(3); sales = reader.GetDecimal(4); grossCapital = reader.GetDecimal(5); grossGain = reader.GetDecimal(6); cashCapital = reader.GetDecimal(7); cashGain = reader.GetDecimal(8); }
                    await transaction.CommitAsync(); return Ok(new { id, ventas = sales, totalDepositado = deposited, capital, ganancia = gain, capitalGenerado = grossCapital, gananciaGenerada = grossGain, salidaCapital = cashCapital, salidaGanancia = cashGain, mensaje = "Depósito neto registrado y vinculado a sus cierres." });
                }
                catch { if (transaction.Connection != null) await transaction.RollbackAsync(); throw; }
            }
            catch (SqlException ex) when (ex.Number is 51041 or 51042 or 51043 or 51046 or 51050 or 51051) { return Conflict(new { mensaje = ex.Message }); }
            catch (SqlException ex) { _logger.LogError(ex, "No se pudo registrar el depósito agrupado."); return StatusCode(503, new { mensaje = "No se pudo registrar el depósito. Revisa la migración y la conexión." }); }
        }

        [HttpPost("transferencias")]
        public async Task<IActionResult> SaveTransfer([FromBody] TransferRequest request)
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "La sesión venció." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return StatusCode(503, new { mensaje = "Falta configurar SQL Server." });
            var fund = NormalizeFund(request.Fondo); var proof = (request.Comprobante ?? "").Trim();
            if (request.IdBancoOrigen <= 0 || request.IdBancoDestino <= 0 || request.IdBancoOrigen == request.IdBancoDestino || fund == null || request.Monto <= 0 || request.Monto > MaxAmount || request.Fecha == default || request.Fecha.Date > DateTime.Today || proof.Length is 0 or > 100)
                return BadRequest(new { mensaje = "Indica dos bancos distintos, fondo, monto, fecha y comprobante de transferencia." });
            try
            {
                await using var connection = new SqlConnection(ConnectionString); await connection.OpenAsync();
                using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
                try
                {
                    await using var command = new SqlCommand(@"
DECLARE @disponible decimal(12,2),@origen int,@destino int;
SELECT @origen=COUNT(*) FROM dbo.FIN_BANCO_TEMPORAL WITH(UPDLOCK,HOLDLOCK) WHERE IdBanco=@origenId AND Activo=1;
SELECT @destino=COUNT(*) FROM dbo.FIN_BANCO_TEMPORAL WITH(UPDLOCK,HOLDLOCK) WHERE IdBanco=@destinoId AND Activo=1;
IF @origen<>1 OR @destino<>1 THROW 51044,'No se encontró uno de los bancos.',1;
SELECT @disponible=CASE WHEN @fondo='Capital' THEN b.SaldoCapitalInicial ELSE b.SaldoGananciaInicial END FROM dbo.FIN_BANCO_TEMPORAL b WITH(UPDLOCK,HOLDLOCK) WHERE b.IdBanco=@origenId;
SELECT @disponible=@disponible+ISNULL(SUM(x.FondoMonto),0) FROM (
 SELECT CASE WHEN @fondo='Capital' THEN c.CapitalDepositado ELSE c.GananciaDepositada END FondoMonto
 FROM dbo.FIN_DEPOSITO_LOTE_CIERRE c JOIN dbo.FIN_DEPOSITO_LOTE l ON l.IdLote=c.IdLote WHERE l.IdBanco=@origenId AND l.Anulado=0
) x;
SELECT @disponible=@disponible-ISNULL(SUM(Monto),0) FROM dbo.FIN_TRANSFERENCIA_BANCARIA WHERE IdBancoOrigen=@origenId AND Fondo=@fondo;
 SELECT @disponible=@disponible+ISNULL(SUM(Monto),0) FROM dbo.FIN_TRANSFERENCIA_BANCARIA WHERE IdBancoDestino=@origenId AND Fondo=@fondo;
 SELECT @disponible=@disponible-ISNULL(SUM(Monto),0) FROM dbo.FIN_MOVIMIENTO WHERE IdBanco=@origenId AND Anulado=0 AND CuentaOrigen=@fondo;
 SELECT @disponible=@disponible-ISNULL(SUM(Monto),0) FROM dbo.PAGO_GASTO_PERSONAL WHERE IdBanco=@origenId AND Anulado=0 AND CuentaOrigen=@fondo;
 SELECT @disponible=@disponible+ISNULL(SUM(Ajuste),0) FROM dbo.FIN_CONCILIACION_DETALLE WHERE IdBanco=@origenId AND Fondo=CASE WHEN @fondo='Personal' THEN 'Ganancia' ELSE 'Capital' END;
IF @monto>@disponible THROW 51045,'El monto supera el fondo disponible en el banco de origen.',1;
INSERT dbo.FIN_TRANSFERENCIA_BANCARIA(IdBancoOrigen,IdBancoDestino,Fondo,Monto,Fecha,Comprobante,IdUsuario) VALUES(@origenId,@destinoId,@fondo,@monto,@fecha,@comprobante,@usuario);", connection, (SqlTransaction)transaction);
                    command.Parameters.Add("@origenId", SqlDbType.Int).Value = request.IdBancoOrigen; command.Parameters.Add("@destinoId", SqlDbType.Int).Value = request.IdBancoDestino;
                    command.Parameters.Add("@fondo", SqlDbType.VarChar, 15).Value = fund; AddMoney(command, "@monto", request.Monto); command.Parameters.Add("@fecha", SqlDbType.Date).Value = request.Fecha.Date;
                    command.Parameters.Add("@comprobante", SqlDbType.VarChar, 100).Value = proof; command.Parameters.Add("@usuario", SqlDbType.Int).Value = UserId.Value;
                    await command.ExecuteNonQueryAsync(); await transaction.CommitAsync(); return Ok(new { mensaje = "Transferencia registrada. El depósito original se mantiene sin cambios." });
                }
                catch { if (transaction.Connection != null) await transaction.RollbackAsync(); throw; }
            }
            catch (SqlException ex) when (ex.Number is 51044 or 51045) { return Conflict(new { mensaje = ex.Message }); }
            catch (SqlException ex) { _logger.LogError(ex, "No se pudo registrar la transferencia bancaria."); return StatusCode(503, new { mensaje = "No se pudo registrar la transferencia." }); }
        }

        [HttpPost("depositos/{id:int}/anular")]
        public async Task<IActionResult> CancelDeposit(int id, [FromBody] CancelBatchRequest request)
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "La sesión venció." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return StatusCode(503, new { mensaje = "Falta configurar SQL Server." });
            var reason = (request.Motivo ?? "").Trim();
            if (id <= 0 || reason.Length is 0 or > 250) return BadRequest(new { mensaje = "Escribe el motivo de anulación (máximo 250 caracteres)." });
            try
            {
                await using var connection = new SqlConnection(ConnectionString); await connection.OpenAsync();
                using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
                try
                {
                    await using var command = new SqlCommand(@"
DECLARE @banco int,@anulado bit;
SELECT @banco=IdBanco,@anulado=Anulado FROM dbo.FIN_DEPOSITO_LOTE WITH(UPDLOCK,HOLDLOCK) WHERE IdLote=@id;
IF @banco IS NULL THROW 51047,'No se encontró el depósito.',1;
IF @anulado=1 THROW 51048,'Este depósito ya fue anulado.',1;
IF EXISTS(SELECT 1 FROM dbo.FIN_TRANSFERENCIA_BANCARIA WITH(UPDLOCK,HOLDLOCK) WHERE IdBancoOrigen=@banco OR IdBancoDestino=@banco)
 OR EXISTS(SELECT 1 FROM dbo.FIN_MOVIMIENTO WITH(UPDLOCK,HOLDLOCK) WHERE IdBanco=@banco AND Anulado=0)
 OR EXISTS(SELECT 1 FROM dbo.PAGO_GASTO_PERSONAL WITH(UPDLOCK,HOLDLOCK) WHERE IdBanco=@banco AND Anulado=0)
 THROW 51049,'El banco ya tiene movimientos relacionados. Registra el movimiento compensatorio en vez de anular la boleta.',1;
UPDATE dbo.FIN_DEPOSITO_LOTE SET Anulado=1,MotivoAnulacion=@motivo,UsuarioAnulacion=@usuario,FechaAnulacion=SYSDATETIME() WHERE IdLote=@id;", connection, (SqlTransaction)transaction);
                    command.Parameters.Add("@id", SqlDbType.Int).Value = id; command.Parameters.Add("@motivo", SqlDbType.VarChar, 250).Value = reason; command.Parameters.Add("@usuario", SqlDbType.Int).Value = UserId.Value;
                    await command.ExecuteNonQueryAsync(); await transaction.CommitAsync(); return Ok(new { mensaje = "Depósito anulado con motivo; sus cierres quedan disponibles nuevamente." });
                }
                catch { if (transaction.Connection != null) await transaction.RollbackAsync(); throw; }
            }
            catch (SqlException ex) when (ex.Number is >= 51047 and <= 51049) { return Conflict(new { mensaje = ex.Message }); }
            catch (SqlException ex) { _logger.LogError(ex, "No se pudo anular lote de depósito {IdLote}.", id); return StatusCode(503, new { mensaje = "No se pudo anular el depósito." }); }
        }

        [HttpGet("movimientos")]
        public async Task<IActionResult> Movements([FromQuery] string? tipo)
        {
            if (!UserId.HasValue) return Unauthorized();
            if (string.IsNullOrWhiteSpace(ConnectionString)) return StatusCode(503, new { mensaje = "Falta configurar SQL Server." });
            try
            {
                await using var connection = new SqlConnection(ConnectionString); await connection.OpenAsync();
                var rows = new List<object>();
                await using var command = new SqlCommand(@"
SELECT TOP(30) Id,Fecha,Tipo,Concepto,Monto,Banco,Detalle,Responsable,Anulado,Comprobante FROM (
 SELECT l.IdLote Id,l.Fecha,CASE WHEN l.Anulado=1 THEN 'Depósito anulado' ELSE 'Depósito' END Tipo,'Boleta '+l.Comprobante Concepto,l.MontoTotal Monto,b.Nombre Banco,CONCAT('Cierres Q ',FORMAT(SUM(c.Ventas),'N2'),' · Salidas efectivo: Capital Q ',FORMAT(SUM(c.Capital-c.CapitalDepositado),'N2'),' y Ganancia Q ',FORMAT(SUM(c.Ganancia-c.GananciaDepositada),'N2'),' · Depósito: Capital Q ',FORMAT(SUM(c.CapitalDepositado),'N2'),' y Ganancia Q ',FORMAT(SUM(c.GananciaDepositada),'N2'),CASE WHEN l.Anulado=1 THEN ' · '+ISNULL(l.MotivoAnulacion,'Anulado') ELSE '' END) Detalle,u.NombreCompleto Responsable,CAST(l.Anulado AS bit) Anulado,'depositos' Grupo,l.Comprobante Comprobante
 FROM dbo.FIN_DEPOSITO_LOTE l JOIN dbo.FIN_BANCO_TEMPORAL b ON b.IdBanco=l.IdBanco JOIN dbo.FIN_DEPOSITO_LOTE_CIERRE c ON c.IdLote=l.IdLote LEFT JOIN dbo.USUARIO u ON u.IdUsuario=l.IdUsuario GROUP BY l.Fecha,l.Comprobante,l.MontoTotal,b.Nombre,u.NombreCompleto,l.IdLote,l.Anulado,l.MotivoAnulacion
 UNION ALL SELECT t.IdTransferencia,t.Fecha,'Transferencia', 'Boleta '+t.Comprobante,t.Monto,bo.Nombre,CONCAT(CASE WHEN t.Fondo='Personal' THEN 'Ganancia' ELSE t.Fondo END,' · ',bo.Nombre,' → ',bd.Nombre),u.NombreCompleto,CAST(0 AS bit),'depositos',t.Comprobante
 FROM dbo.FIN_TRANSFERENCIA_BANCARIA t JOIN dbo.FIN_BANCO_TEMPORAL bo ON bo.IdBanco=t.IdBancoOrigen JOIN dbo.FIN_BANCO_TEMPORAL bd ON bd.IdBanco=t.IdBancoDestino LEFT JOIN dbo.USUARIO u ON u.IdUsuario=t.IdUsuario
 UNION ALL SELECT m.IdMovimiento,CONVERT(date,m.Fecha),'Salida',m.Concepto,m.Monto,CASE WHEN m.IdBanco IS NOT NULL THEN ISNULL(b.Nombre,'Banco') WHEN m.OrigenEfectivo='Capital' THEN 'Capital en efectivo' WHEN m.OrigenEfectivo='Ganancia' THEN 'Ganancia en efectivo' ELSE 'Caja de ventas' END,CONCAT(CASE WHEN m.CuentaOrigen='Personal' THEN 'Ganancia' WHEN m.CuentaOrigen='Caja' THEN 'Sin fondo asignado' ELSE m.CuentaOrigen END,' · ',CASE WHEN m.IdBanco IS NULL THEN 'Efectivo' ELSE 'Banco' END),u.NombreCompleto,CAST(0 AS bit),'salidas',ISNULL(m.Referencia,'')
 FROM dbo.FIN_MOVIMIENTO m LEFT JOIN dbo.FIN_BANCO_TEMPORAL b ON b.IdBanco=m.IdBanco LEFT JOIN dbo.USUARIO u ON u.IdUsuario=m.IdUsuario WHERE m.Anulado=0
 UNION ALL SELECT p.IdPagoGastoPersonal,CONVERT(date,p.FechaPago),'Salida',COALESCE(NULLIF(p.Observaciones,''),g.Nombre),p.Monto,CASE WHEN p.IdBanco IS NOT NULL THEN ISNULL(b.Nombre,'Banco') WHEN p.OrigenEfectivo='Capital' THEN 'Capital en efectivo' WHEN p.OrigenEfectivo='Ganancia' THEN 'Ganancia en efectivo' ELSE 'Caja de ventas' END,CONCAT(CASE WHEN p.CuentaOrigen='Personal' THEN 'Ganancia' WHEN p.CuentaOrigen='Caja' THEN 'Sin fondo asignado' ELSE p.CuentaOrigen END,' · ',CASE WHEN p.IdBanco IS NULL THEN 'Efectivo' ELSE 'Banco' END),u.NombreCompleto,CAST(0 AS bit),'salidas',ISNULL(p.Referencia,'')
 FROM dbo.PAGO_GASTO_PERSONAL p JOIN dbo.GASTO_PERSONAL_PERIODO per ON per.IdPeriodoGastoPersonal=p.IdPeriodoGastoPersonal JOIN dbo.GASTO_PERSONAL g ON g.IdGastoPersonal=per.IdGastoPersonal LEFT JOIN dbo.FIN_BANCO_TEMPORAL b ON b.IdBanco=p.IdBanco LEFT JOIN dbo.USUARIO u ON u.IdUsuario=p.IdUsuario WHERE p.Anulado=0
) m WHERE @tipo IS NULL OR LOWER(@tipo)=m.Grupo ORDER BY Fecha DESC,Id DESC;", connection);
                command.Parameters.Add("@tipo", SqlDbType.VarChar, 20).Value = string.IsNullOrWhiteSpace(tipo) ? (object)DBNull.Value : tipo.Trim();
                await using (var reader = await command.ExecuteReaderAsync()) while (await reader.ReadAsync()) rows.Add(new { id = reader.GetInt32(0), fecha = reader.GetDateTime(1).ToString("dd/MM/yyyy"), tipo = reader.GetString(2), concepto = reader.GetString(3), monto = reader.GetDecimal(4), banco = reader.GetString(5), detalle = reader.GetString(6), responsable = reader.IsDBNull(7) ? "" : reader.GetString(7), anulado = reader.GetBoolean(8), comprobante = reader.IsDBNull(9) ? "" : reader.GetString(9) });
                return Ok(new { movimientos = rows });
            }
            catch (SqlException ex) { _logger.LogError(ex, "No se pudo cargar auditoría de depósitos."); return StatusCode(503, new { mensaje = "No se pudo cargar el historial." }); }
        }

        private static string? NormalizeFund(string? value) => (value ?? "").Trim() switch { "Ganancia" or "Personal" => "Personal", "Capital" => "Capital", _ => null };
        private static void AddMoney(SqlCommand command, string name, decimal value) { var p = command.Parameters.Add(name, SqlDbType.Decimal); p.Precision = 12; p.Scale = 2; p.Value = value; }
        public sealed class BankRequest { public string? Nombre { get; set; } public decimal SaldoCapitalInicial { get; set; } public decimal SaldoGananciaInicial { get; set; } }
        public sealed class DepositBatchRequest { public int IdBanco { get; set; } public int[]? IdsCierre { get; set; } public DateTime Fecha { get; set; } public string? Comprobante { get; set; } }
        public sealed class TransferRequest { public int IdBancoOrigen { get; set; } public int IdBancoDestino { get; set; } public string? Fondo { get; set; } public decimal Monto { get; set; } public DateTime Fecha { get; set; } public string? Comprobante { get; set; } }
        public sealed class CancelBatchRequest { public string? Motivo { get; set; } }
        public sealed class DepositDataResponse { public List<BankItem> Bancos { get; } = new List<BankItem>(); public List<ClosingItem> Cierres { get; } = new List<ClosingItem>(); public List<BankFundItem> FondosBanco { get; } = new List<BankFundItem>(); }
        public sealed class BankItem { public int Id { get; set; } public string Nombre { get; set; } = ""; public decimal SaldoInicial { get; set; } public decimal SaldoCapitalInicial { get; set; } public decimal SaldoGananciaInicial { get; set; } public decimal Saldo { get; set; } }
        public sealed class ClosingItem { public int Id { get; set; } public string Fecha { get; set; } = ""; public decimal Ventas { get; set; } public decimal Capital { get; set; } public decimal Ganancia { get; set; } public decimal CapitalNeto { get; set; } public decimal GananciaNeta { get; set; } public decimal SalidasCapital { get; set; } public decimal SalidasGanancia { get; set; } public string Responsable { get; set; } = ""; }
        public sealed class BankFundItem { public int IdBanco { get; set; } public string Banco { get; set; } = ""; public decimal Capital { get; set; } public decimal Ganancia { get; set; } }
    }
}
