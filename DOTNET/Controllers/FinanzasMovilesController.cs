using System;
using System.Collections.Generic;
using System.Data;
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
 CAST(ISNULL((SELECT SUM(CapitalGenerado) FROM dbo.FIN_CIERRE_CAJA),0)+ISNULL((SELECT SUM(CapitalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL WHERE Cuenta='Capital'),0)-ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE CuentaOrigen='Capital'),0)-ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE CuentaOrigen='Capital'),0) AS decimal(12,2)) Capital,
 CAST(ISNULL((SELECT SUM(CapitalGenerado) FROM dbo.FIN_CIERRE_CAJA),0)+ISNULL((SELECT SUM(CapitalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL WHERE Cuenta='Capital'),0) AS decimal(12,2)) CapitalGeneradoTotal,
 CAST(ISNULL((SELECT SUM(GananciaGenerada) FROM dbo.FIN_CIERRE_CAJA),0)+ISNULL((SELECT SUM(PersonalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL WHERE Cuenta='Personal'),0)-ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE CuentaOrigen='Personal'),0)-ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE CuentaOrigen='Personal'),0) AS decimal(12,2)) Personal,
 CAST(ISNULL((SELECT SUM(GananciaGenerada) FROM dbo.FIN_CIERRE_CAJA),0)+ISNULL((SELECT SUM(PersonalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL WHERE Cuenta='Personal'),0) AS decimal(12,2)) PersonalGeneradoTotal,
 CAST(ISNULL((SELECT SUM(Capital) FROM dbo.FIN_DEPOSITO),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_DEPOSITO_TEMPORAL WHERE Cuenta='Capital'),0) AS decimal(12,2)) DepositadoCapital,
 CAST(ISNULL((SELECT SUM(Ganancia) FROM dbo.FIN_DEPOSITO),0)+ISNULL((SELECT SUM(Monto) FROM dbo.FIN_DEPOSITO_TEMPORAL WHERE Cuenta='Personal'),0) AS decimal(12,2)) DepositadoPersonal,
 CAST(ISNULL((SELECT SUM(MontoProgramado) FROM dbo.GASTO_PERSONAL_PERIODO WHERE Anio=YEAR(GETDATE()) AND Mes=MONTH(GETDATE())),0) AS decimal(12,2)) ObligacionesMes,
 CAST(ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL p JOIN dbo.GASTO_PERSONAL_PERIODO per ON per.IdPeriodoGastoPersonal=p.IdPeriodoGastoPersonal WHERE per.Anio=YEAR(GETDATE()) AND per.Mes=MONTH(GETDATE())),0) AS decimal(12,2)) PagadoPersonal,
 CAST(ISNULL((SELECT SUM(PersonalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL WHERE FechaCierre>=@inicio AND FechaCierre<@fin),0)+ISNULL((SELECT SUM(GananciaGenerada) FROM dbo.FIN_CIERRE_CAJA WHERE FechaCierre>=@inicio AND FechaCierre<@fin),0) AS decimal(12,2)) AsignadoPersonalMes,
 CAST(ISNULL((SELECT SUM(Ventas) FROM dbo.FIN_CIERRE_TEMPORAL WHERE FechaCierre>=@inicio AND FechaCierre<@fin),0) AS decimal(12,2)) VentasManualMes,
 CAST(ISNULL((SELECT SUM(Ventas) FROM dbo.FIN_CIERRE_TEMPORAL WHERE FechaCierre>=@inicio AND FechaCierre<@fin),0)+ISNULL((SELECT SUM(Ventas) FROM dbo.FIN_CIERRE_CAJA WHERE FechaCierre>=@inicio AND FechaCierre<@fin),0) AS decimal(12,2)) VentasMesTotal,
 CAST(ISNULL((SELECT SUM(CapitalAsignado) FROM dbo.FIN_CIERRE_TEMPORAL WHERE FechaCierre>=@inicio AND FechaCierre<@fin),0)+ISNULL((SELECT SUM(CapitalGenerado) FROM dbo.FIN_CIERRE_CAJA WHERE FechaCierre>=@inicio AND FechaCierre<@fin),0) AS decimal(12,2)) CapitalGeneradoMes,
 CAST(ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE CuentaOrigen='Capital' AND Fecha>=@inicio AND Fecha<@fin),0)+ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE CuentaOrigen='Capital' AND FechaPago>=@inicio AND FechaPago<@fin),0) AS decimal(12,2)) SalidasCapitalMes,
 CAST(ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE CuentaOrigen='Personal' AND Fecha>=@inicio AND Fecha<@fin),0)+ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE CuentaOrigen='Personal' AND FechaPago>=@inicio AND FechaPago<@fin),0) AS decimal(12,2)) SalidasPersonalMes,
 CAST(ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE PagadoDesdeCaja=1 AND FechaRegistro>@ultimoCierre),0)+ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE PagadoDesdeCaja=1 AND FechaRegistro>@ultimoCierre),0) AS decimal(12,2)) SalidasCajaPendientes,
 (SELECT TOP (1) g.Nombre FROM dbo.GASTO_PERSONAL_PERIODO p JOIN dbo.GASTO_PERSONAL g ON g.IdGastoPersonal=p.IdGastoPersonal LEFT JOIN (SELECT IdPeriodoGastoPersonal,SUM(Monto) Pagado FROM dbo.PAGO_GASTO_PERSONAL GROUP BY IdPeriodoGastoPersonal) x ON x.IdPeriodoGastoPersonal=p.IdPeriodoGastoPersonal WHERE p.Anio=YEAR(GETDATE()) AND p.Mes=MONTH(GETDATE()) AND p.MontoProgramado>ISNULL(x.Pagado,0) AND g.Activo=1 ORDER BY p.FechaVencimiento,p.IdPeriodoGastoPersonal) ProximoGasto,
 (SELECT TOP (1) CONVERT(varchar(10),p.FechaVencimiento,23) FROM dbo.GASTO_PERSONAL_PERIODO p JOIN dbo.GASTO_PERSONAL g ON g.IdGastoPersonal=p.IdGastoPersonal LEFT JOIN (SELECT IdPeriodoGastoPersonal,SUM(Monto) Pagado FROM dbo.PAGO_GASTO_PERSONAL GROUP BY IdPeriodoGastoPersonal) x ON x.IdPeriodoGastoPersonal=p.IdPeriodoGastoPersonal WHERE p.Anio=YEAR(GETDATE()) AND p.Mes=MONTH(GETDATE()) AND p.MontoProgramado>ISNULL(x.Pagado,0) AND g.Activo=1 ORDER BY p.FechaVencimiento,p.IdPeriodoGastoPersonal) FechaProximoGasto;";
                await using (var command = new SqlCommand(totalsSql, connection))
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        response.Capital = Money(reader, "Capital"); response.CapitalGeneradoTotal = Money(reader, "CapitalGeneradoTotal"); response.Personal = Money(reader, "Personal"); response.PersonalGeneradoTotal = Money(reader, "PersonalGeneradoTotal");
                        response.DepositadoCapital = Money(reader, "DepositadoCapital"); response.DepositadoPersonal = Money(reader, "DepositadoPersonal");
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

                const string historySql = @"
SELECT TOP (10) Fecha,Tipo,Concepto,Monto,Responsable,Capital,Ganancia FROM (
 SELECT c.FechaCierre AS Fecha,'Cierre' Tipo,'Cierre del día' Concepto,c.Ventas Monto,u.NombreCompleto Responsable,c.CapitalAsignado Capital,c.PersonalAsignado Ganancia FROM dbo.FIN_CIERRE_TEMPORAL c LEFT JOIN dbo.USUARIO u ON u.IdUsuario=c.IdUsuario
 UNION ALL SELECT d.Fecha,'Depósito '+d.Cuenta,'Comprobante '+d.Comprobante,d.Monto,u.NombreCompleto,NULL,NULL FROM dbo.FIN_DEPOSITO_TEMPORAL d LEFT JOIN dbo.USUARIO u ON u.IdUsuario=d.IdUsuario
 UNION ALL SELECT m.Fecha,CASE WHEN m.Tipo='Proveedor' THEN 'Pago proveedor · '+m.CuentaOrigen ELSE 'Salida empresa · '+m.CuentaOrigen END,m.Concepto,m.Monto,u.NombreCompleto,NULL,NULL FROM dbo.FIN_MOVIMIENTO m LEFT JOIN dbo.USUARIO u ON u.IdUsuario=m.IdUsuario
 UNION ALL SELECT p.FechaPago,'Pago personal · '+p.CuentaOrigen,g.Nombre,p.Monto,u.NombreCompleto,NULL,NULL FROM dbo.PAGO_GASTO_PERSONAL p JOIN dbo.GASTO_PERSONAL_PERIODO per ON per.IdPeriodoGastoPersonal=p.IdPeriodoGastoPersonal JOIN dbo.GASTO_PERSONAL g ON g.IdGastoPersonal=per.IdGastoPersonal LEFT JOIN dbo.USUARIO u ON u.IdUsuario=p.IdUsuario
 UNION ALL SELECT a.Fecha,'Ajuste de conciliación · '+a.Cuenta,a.Concepto,a.Monto,u.NombreCompleto,NULL,NULL FROM dbo.FIN_AJUSTE_CUENTA_TEMPORAL a LEFT JOIN dbo.USUARIO u ON u.IdUsuario=a.IdUsuario
) h ORDER BY Fecha DESC;";
                await using (var command = new SqlCommand(historySql, connection))
                await using (var reader = await command.ExecuteReaderAsync())
                    while (await reader.ReadAsync()) response.Movimientos.Add(new FinanceMovement { Fecha = Convert.ToDateTime(reader.GetValue(0)).ToString("dd/MM/yyyy HH:mm"), Tipo = reader.GetString(1), Concepto = reader.GetString(2), Monto = Money(reader, 3), Responsable = reader.IsDBNull(4) ? "" : reader.GetString(4), Capital = reader.IsDBNull(5) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(5)), Ganancia = reader.IsDBNull(6) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(6)) });

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
        public async Task<IActionResult> AuditClosings([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "Inicia sesión para consultar la auditoría." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return StatusCode(503, new { mensaje = "Falta configurar la conexión de SQL Server." });
            if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
                return BadRequest(new { mensaje = "La fecha inicial no puede ser posterior a la fecha final." });
            try
            {
                var cierres = new List<CloseAudit>();
                await using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();
                await using var command = new SqlCommand(@"
SELECT TOP (10) c.IdCierreTemporal,c.FechaCierre,c.Ventas,c.CapitalAsignado,c.PersonalAsignado,u.NombreCompleto
FROM dbo.FIN_CIERRE_TEMPORAL c
LEFT JOIN dbo.USUARIO u ON u.IdUsuario=c.IdUsuario
WHERE (@desde IS NULL OR c.FechaCierre>=@desde)
  AND (@hasta IS NULL OR c.FechaCierre<DATEADD(day,1,@hasta))
ORDER BY c.FechaCierre DESC,c.IdCierreTemporal DESC;", connection);
                command.Parameters.Add("@desde", SqlDbType.Date).Value = desde.HasValue ? (object)desde.Value.Date : DBNull.Value;
                command.Parameters.Add("@hasta", SqlDbType.Date).Value = hasta.HasValue ? (object)hasta.Value.Date : DBNull.Value;
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    cierres.Add(new CloseAudit
                    {
                        Id = reader.GetInt32(0),
                        Fecha = reader.GetDateTime(1).ToString("dd/MM/yyyy HH:mm"),
                        Ventas = Money(reader, 2),
                        Capital = Money(reader, 3),
                        Ganancia = Money(reader, 4),
                        Responsable = reader.IsDBNull(5) ? "" : reader.GetString(5)
                    });
                return Ok(new { cierres });
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
            var account = (request.Cuenta ?? "").Trim(); var receipt = (request.Comprobante ?? "").Trim();
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

        [HttpPost("salidas")]
        public async Task<IActionResult> SaveExpense([FromBody] ExpenseRequest request)
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "La sesión venció. Inicia sesión otra vez." });
            var type = (request.Tipo ?? "").Trim(); var concept = (request.Concepto ?? "").Trim();
            var sourceAccount = (request.CuentaOrigen ?? "").Trim();
            if (request.Monto <= 0 || request.Monto > MaxAmount || request.Fecha.Date > DateTime.Today || (sourceAccount != "Capital" && sourceAccount != "Personal")) return BadRequest(new { mensaje = "Indica un monto, una fecha y la cuenta de origen válidos." });
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
INSERT dbo.PAGO_GASTO_PERSONAL(IdPeriodoGastoPersonal,IdUsuario,Monto,FechaPago,MedioPago,Referencia,Observaciones,CuentaOrigen,PagadoDesdeCaja) VALUES(@periodo,@usuario,@monto,@fecha,@medioPago,NULL,@concepto,@cuentaOrigen,@pagadoDesdeCaja);", connection, (SqlTransaction)transaction);
                        command.Parameters.Add("@periodo", SqlDbType.Int).Value = request.IdPeriodoGastoPersonal; command.Parameters.Add("@usuario", SqlDbType.Int).Value = UserId.Value;
                        AddMoney(command, "@monto", request.Monto); command.Parameters.Add("@fecha", SqlDbType.DateTime2).Value = request.Fecha.Date;
                        command.Parameters.Add("@medioPago", SqlDbType.VarChar, 20).Value = request.PagadoDesdeCaja ? "Efectivo" : "Transferencia";
                        command.Parameters.Add("@cuentaOrigen", SqlDbType.VarChar, 15).Value = sourceAccount; command.Parameters.Add("@pagadoDesdeCaja", SqlDbType.Bit).Value = request.PagadoDesdeCaja;
                        command.Parameters.Add("@concepto", SqlDbType.VarChar, 250).Value = string.IsNullOrWhiteSpace(concept) ? "Pago de gasto personal" : concept;
                        await command.ExecuteNonQueryAsync(); transaction.Commit();
                    }
                    catch { transaction.Rollback(); throw; }
                }
                else if (type == "Empresa")
                {
                    if (string.IsNullOrWhiteSpace(concept) || concept.Length > 150) return BadRequest(new { mensaje = "Escribe el concepto de la salida de empresa (máximo 150 caracteres)." });
                    await using var command = new SqlCommand("INSERT dbo.FIN_MOVIMIENTO(Tipo,IdCompra,IdUsuario,Fecha,Concepto,Monto,MedioPago,Referencia,CuentaOrigen,PagadoDesdeCaja) VALUES('Empresa',NULL,@usuario,@fecha,@concepto,@monto,@medioPago,NULL,@cuentaOrigen,@pagadoDesdeCaja); SELECT CONVERT(int,SCOPE_IDENTITY());", connection);
                    command.Parameters.Add("@usuario", SqlDbType.Int).Value = UserId.Value; command.Parameters.Add("@fecha", SqlDbType.DateTime2).Value = request.Fecha.Date;
                    command.Parameters.Add("@medioPago", SqlDbType.VarChar, 20).Value = request.PagadoDesdeCaja ? "Efectivo" : "Transferencia";
                    command.Parameters.Add("@cuentaOrigen", SqlDbType.VarChar, 15).Value = sourceAccount; command.Parameters.Add("@pagadoDesdeCaja", SqlDbType.Bit).Value = request.PagadoDesdeCaja;
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
        private static decimal Money(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal));
        private static void AddMoney(SqlCommand command, string name, decimal value)
        { var p = command.Parameters.Add(name, SqlDbType.Decimal); p.Precision = 12; p.Scale = 2; p.Value = value; }

        public sealed class ClosingRequest { public decimal EfectivoInicial { get; set; } public decimal EfectivoContado { get; set; } public decimal SalidasCajaEsperadas { get; set; } }
        public sealed class DepositRequest { public string? Cuenta { get; set; } public decimal Monto { get; set; } public DateTime Fecha { get; set; } public string? Comprobante { get; set; } }
        public sealed class ExpenseRequest { public string? Tipo { get; set; } public string? CuentaOrigen { get; set; } public bool PagadoDesdeCaja { get; set; } public int IdPeriodoGastoPersonal { get; set; } public decimal Monto { get; set; } public DateTime Fecha { get; set; } public string? Concepto { get; set; } }
        public sealed class PersonalExpense { public int IdPeriodo { get; set; } public string Nombre { get; set; } = ""; public decimal Saldo { get; set; } }
        public sealed class FinanceMovement { public string Fecha { get; set; } = ""; public string Tipo { get; set; } = ""; public string Concepto { get; set; } = ""; public decimal Monto { get; set; } public decimal? Capital { get; set; } public decimal? Ganancia { get; set; } public string Responsable { get; set; } = ""; }
        public sealed class CloseAudit { public int Id { get; set; } public string Fecha { get; set; } = ""; public decimal Ventas { get; set; } public decimal Capital { get; set; } public decimal Ganancia { get; set; } public string Responsable { get; set; } = ""; }
        public sealed class FinanceSummary
        {
            public string Responsable { get; set; } = ""; public string Fecha { get; set; } = ""; public string Mes { get; set; } = "";
            public decimal Capital { get; set; } public decimal CapitalGeneradoTotal { get; set; } public decimal Personal { get; set; } public decimal PersonalGeneradoTotal { get; set; } public decimal DepositadoCapital { get; set; } public decimal DepositadoPersonal { get; set; }
            public decimal ObligacionesMes { get; set; } public decimal PagadoPersonal { get; set; } public decimal AsignadoPersonalMes { get; set; } public decimal VentasManualMes { get; set; }
            public decimal VentasMesTotal { get; set; } public decimal CapitalGeneradoMes { get; set; } public decimal SalidasCapitalMes { get; set; } public decimal SalidasPersonalMes { get; set; } public decimal SalidasCajaPendientes { get; set; }
            public string ProximoGasto { get; set; } = ""; public string FechaProximoGasto { get; set; } = "";
            public List<PersonalExpense> Gastos { get; } = new List<PersonalExpense>(); public List<FinanceMovement> Movimientos { get; } = new List<FinanceMovement>();
        }
    }
}
