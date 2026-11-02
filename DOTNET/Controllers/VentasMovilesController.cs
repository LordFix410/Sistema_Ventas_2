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
    [Route("api/ventas-moviles")]
    public sealed class VentasMovilesController : ControllerBase
    {
        private const decimal MaxAmount = 99999999.99m;
        private readonly IConfiguration _configuration;
        private readonly ILogger<VentasMovilesController> _logger;

        public VentasMovilesController(IConfiguration configuration, ILogger<VentasMovilesController> logger)
        { _configuration = configuration; _logger = logger; }

        private string? ConnectionString => _configuration.GetConnectionString("ProductDb");
        private int? UserId => HttpContext.Session.GetInt32("ProductUserId");

        [HttpGet("clientes")]
        public async Task<IActionResult> SearchClients([FromQuery] string? q)
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "Inicia sesión para consultar clientes." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return DatabaseNotConfigured();
            var term = (q ?? string.Empty).Trim();
            if (term.Length < 1 || term.Length > 50) return BadRequest(new { mensaje = "Escribe el NIT o documento del cliente." });
            var normalized = NormalizeDocument(term);
            try
            {
                var clients = new List<MobileClient>();
                await using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();
                await using var command = new SqlCommand(@"
SELECT TOP (2) IdCliente, ISNULL(Documento,''), ISNULL(NombreCompleto,''), ISNULL(Correo,''), ISNULL(Telefono,'')
FROM dbo.CLIENTE
WHERE Estado=1 AND REPLACE(REPLACE(UPPER(LTRIM(RTRIM(Documento))),'-',''),' ','')=@documento
ORDER BY IdCliente;", connection);
                command.Parameters.Add("@documento", SqlDbType.VarChar, 50).Value = normalized;
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    clients.Add(new MobileClient(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4)));
                if (clients.Count > 1) return Conflict(new { mensaje = "Hay más de un cliente con ese NIT. Revisa los registros antes de continuar." });
                return Ok(clients);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error SQL buscando cliente por documento para venta móvil.");
                return StatusCode(503, new { mensaje = "No se pudo consultar el cliente en la base de datos." });
            }
        }

        [HttpPost("clientes")]
        public async Task<IActionResult> CreateClient([FromBody] MobileClientRequest request)
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "La sesión venció. Inicia sesión otra vez." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return DatabaseNotConfigured();
            var document = (request.Documento ?? string.Empty).Trim().ToUpperInvariant();
            var name = (request.NombreCompleto ?? string.Empty).Trim().ToUpperInvariant();
            var email = (request.Correo ?? string.Empty).Trim().ToUpperInvariant();
            var phone = (request.Telefono ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(document) || document.Length > 50 || string.IsNullOrWhiteSpace(name) || name.Length > 50 || email.Length > 50 || phone.Length > 50)
                return BadRequest(new { mensaje = "El NIT y el nombre son obligatorios; revisa que ningún dato exceda 50 caracteres." });
            try
            {
                await using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();
                await using (var duplicate = new SqlCommand(@"
SELECT TOP (1) IdCliente FROM dbo.CLIENTE
WHERE REPLACE(REPLACE(UPPER(LTRIM(RTRIM(Documento))),'-',''),' ','')=@documento;", connection))
                {
                    duplicate.Parameters.Add("@documento", SqlDbType.VarChar, 50).Value = NormalizeDocument(document);
                    var existing = await duplicate.ExecuteScalarAsync();
                    if (existing != null && existing != DBNull.Value)
                        return Conflict(new { mensaje = "Ya existe un cliente con ese NIT. Búscalo y selecciónalo." });
                }

                await using var command = new SqlCommand("dbo.sp_RegistrarCliente", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.Add("@Documento", SqlDbType.VarChar, 50).Value = document;
                command.Parameters.Add("@NombreCompleto", SqlDbType.VarChar, 50).Value = name;
                command.Parameters.Add("@Correo", SqlDbType.VarChar, 50).Value = email;
                command.Parameters.Add("@Telefono", SqlDbType.VarChar, 50).Value = phone;
                command.Parameters.Add("@Estado", SqlDbType.Bit).Value = true;
                var result = command.Parameters.Add("@Resultado", SqlDbType.Int); result.Direction = ParameterDirection.Output;
                var message = command.Parameters.Add("@Mensaje", SqlDbType.VarChar, 500); message.Direction = ParameterDirection.Output;
                await command.ExecuteNonQueryAsync();
                var clientId = Convert.ToInt32(result.Value);
                if (clientId <= 0)
                    return Conflict(new { mensaje = string.IsNullOrWhiteSpace(Convert.ToString(message.Value)) ? "No se pudo crear el cliente." : Convert.ToString(message.Value) });

                return Ok(new MobileClient(clientId, document, name, email, phone));
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error SQL creando cliente desde venta móvil.");
                if (ex.Number == 2812 || ex.Number == 208 || ex.Number == 207)
                    return StatusCode(503, new { mensaje = "La base no tiene la tabla o el procedimiento de clientes esperado." });
                if (ex.Number == 229) return StatusCode(403, new { mensaje = "La conexión SQL no tiene permisos para registrar clientes." });
                return StatusCode(503, new { mensaje = $"No se pudo guardar el cliente en SQL Server (error {ex.Number})." });
            }
        }

        [HttpGet("productos")]
        public async Task<IActionResult> SearchProducts([FromQuery] string? q)
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "Inicia sesión para consultar productos." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return DatabaseNotConfigured();
            var term = (q ?? string.Empty).Trim();
            if (term.Length < 1 || term.Length > 100) return BadRequest(new { mensaje = "Escribe un código o nombre de producto." });
            try
            {
                var escaped = EscapeLike(term);
                var results = new List<MobileProduct>();
                await using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();
                await using var command = new SqlCommand(@"
SELECT TOP (20) IdProducto, Codigo, Nombre, ISNULL(Descripcion, ''), PrecioVenta
FROM dbo.PRODUCTO
WHERE Estado = 1
  AND (Codigo = @exacto OR Codigo LIKE @prefijo ESCAPE '!'
       OR Nombre LIKE @contiene ESCAPE '!' OR ISNULL(Descripcion, '') LIKE @contiene ESCAPE '!')
ORDER BY CASE WHEN Codigo = @exacto THEN 0 WHEN Codigo LIKE @prefijo ESCAPE '!'
              THEN 1 WHEN Nombre LIKE @prefijo ESCAPE '!' THEN 2 ELSE 3 END, Nombre;", connection);
                command.Parameters.Add("@exacto", SqlDbType.VarChar, 100).Value = term;
                command.Parameters.Add("@prefijo", SqlDbType.VarChar, 210).Value = escaped + "%";
                command.Parameters.Add("@contiene", SqlDbType.VarChar, 210).Value = "%" + escaped + "%";
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    results.Add(new MobileProduct(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetDecimal(4)));
                return Ok(results);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error SQL buscando productos para venta móvil.");
                return StatusCode(503, new { mensaje = "No se pudieron consultar productos en la base de datos." });
            }
        }

        [HttpPost("registrar")]
        public async Task<IActionResult> RegisterSale([FromBody] MobileSaleRequest request)
        {
            if (!UserId.HasValue) return Unauthorized(new { mensaje = "La sesión venció. Inicia sesión otra vez." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return DatabaseNotConfigured();
            if (request?.Productos == null || request.Productos.Count == 0 || request.Productos.Count > 100)
                return BadRequest(new { mensaje = "Agrega entre 1 y 100 artículos a la venta." });
            if (request.Productos.Any(p => p == null || p.Cantidad < 1 || p.Cantidad > 1000000 || !ValidLine(p)) ||
                request.Productos.Where(p => p.IdProducto.HasValue).GroupBy(p => p.IdProducto).Any(g => g.Count() > 1))
                return BadRequest(new { mensaje = "Revisa los artículos y cantidades. Los productos de catálogo no deben repetirse." });
            if (request.MontoPago < 0 || request.MontoPago > MaxAmount)
                return BadRequest(new { mensaje = "El monto recibido no es válido." });

            var temporaryIds = new List<int>();
            try
            {
                await using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();
                var prepared = new List<PreparedLine>();
                decimal total = 0;

                // Validamos el catálogo y los importes antes de insertar artículos temporales.
                foreach (var item in request.Productos)
                {
                    decimal price;
                    if (item.IdProducto.HasValue)
                    {
                        await using var productCommand = new SqlCommand(
                            "SELECT PrecioVenta FROM dbo.PRODUCTO WHERE IdProducto=@id AND Estado=1", connection);
                        productCommand.Parameters.Add("@id", SqlDbType.Int).Value = item.IdProducto.Value;
                        var priceValue = await productCommand.ExecuteScalarAsync();
                        if (priceValue == null || priceValue == DBNull.Value)
                            return Conflict(new { mensaje = "Uno de los productos dejó de estar activo. Actualiza la venta e intenta de nuevo." });
                        price = Convert.ToDecimal(priceValue);
                        if (price <= 0)
                            return Conflict(new { mensaje = "Uno de los productos no tiene un precio válido. Actualízalo antes de cobrar." });
                    }
                    else price = decimal.Round(item.PrecioUnitarioTemporal!.Value, 2, MidpointRounding.AwayFromZero);

                    var subtotal = decimal.Round(price * item.Cantidad, 2, MidpointRounding.AwayFromZero);
                    total += subtotal;
                    if (total > MaxAmount) return BadRequest(new { mensaje = "El total excede el máximo que permite la base de datos." });
                    prepared.Add(new PreparedLine(item, price, subtotal));
                }

                total = decimal.Round(total, 2, MidpointRounding.AwayFromZero);
                if (request.MontoPago < total) return BadRequest(new { mensaje = "El pago recibido no cubre el total de la venta.", total });
                var change = decimal.Round(request.MontoPago - total, 2, MidpointRounding.AwayFromZero);
                var clientDocument = "CF";
                var clientName = "CONSUMIDOR FINAL";
                if (request.IdCliente.HasValue)
                {
                    await using var clientCommand = new SqlCommand("SELECT ISNULL(Documento,''), ISNULL(NombreCompleto,'') FROM dbo.CLIENTE WHERE IdCliente=@id AND Estado=1", connection);
                    clientCommand.Parameters.Add("@id", SqlDbType.Int).Value = request.IdCliente.Value;
                    await using var clientReader = await clientCommand.ExecuteReaderAsync();
                    if (!await clientReader.ReadAsync())
                        return Conflict(new { mensaje = "El cliente seleccionado ya no está activo. Búscalo nuevamente." });
                    clientDocument = clientReader.GetString(0);
                    clientName = clientReader.GetString(1);
                }

                foreach (var line in prepared.Where(x => !x.Request.IdProducto.HasValue))
                {
                    var productId = await CreateTemporaryProduct(connection, line.Request, line.Request.Cantidad);
                    if (productId <= 0) throw new InvalidOperationException("No se pudo preparar un artículo fuera de catálogo.");
                    temporaryIds.Add(productId);
                    line.ProductId = productId;
                }

                var detail = new DataTable();
                detail.Columns.Add("IdProducto", typeof(int));
                detail.Columns.Add("PrecioVenta", typeof(decimal));
                detail.Columns.Add("Cantidad", typeof(int));
                detail.Columns.Add("SubTotal", typeof(decimal));
                foreach (var line in prepared)
                    detail.Rows.Add(line.ProductId, line.Price, line.Request.Cantidad, line.Subtotal);

                await using var command = new SqlCommand("dbo.usp_RegistrarVenta", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = UserId.Value;
                command.Parameters.Add("@TipoDocumento", SqlDbType.VarChar, 500).Value = "Boleta";
                var invoice = command.Parameters.Add("@NumeroDocumento", SqlDbType.VarChar, 500);
                invoice.Direction = ParameterDirection.InputOutput;
                invoice.Value = string.Empty;
                command.Parameters.Add("@DocumentoCliente", SqlDbType.VarChar, 500).Value = clientDocument;
                command.Parameters.Add("@NombreCliente", SqlDbType.VarChar, 500).Value = clientName;
                AddDecimal(command, "@MontoPago", request.MontoPago);
                AddDecimal(command, "@MontoCambio", change);
                AddDecimal(command, "@MontoTotal", total);
                var table = command.Parameters.Add("@DetalleVenta", SqlDbType.Structured);
                table.TypeName = "dbo.EDetalle_Venta";
                table.Value = detail;
                var result = command.Parameters.Add("@Resultado", SqlDbType.Bit);
                result.Direction = ParameterDirection.Output;
                var message = command.Parameters.Add("@Mensaje", SqlDbType.VarChar, 500);
                message.Direction = ParameterDirection.Output;

                await command.ExecuteNonQueryAsync();
                if (!Convert.ToBoolean(result.Value))
                {
                    await CleanupTemporaryProducts(connection, temporaryIds);
                    return Conflict(new { mensaje = Convert.ToString(message.Value) ?? "No se pudo registrar la venta." });
                }

                // Conservamos los productos inactivos: cada línea especial queda vinculada a su venta para auditoría.
                return Ok(new MobileSaleResponse(Convert.ToString(invoice.Value) ?? string.Empty, total, request.MontoPago, change));
            }
            catch (Exception ex) when (ex is SqlException || ex is InvalidOperationException)
            {
                _logger.LogError(ex, "Error registrando venta móvil.");
                try
                {
                    await using var cleanupConnection = new SqlConnection(ConnectionString);
                    await cleanupConnection.OpenAsync();
                    await CleanupTemporaryProducts(cleanupConnection, temporaryIds);
                }
                catch (Exception cleanupError) { _logger.LogWarning(cleanupError, "No se pudieron limpiar todos los artículos temporales sin venta."); }

                if (ex is SqlException sqlEx)
                {
                    if (sqlEx.Number == 2812 || sqlEx.Number == 2715 || sqlEx.Number == 208)
                        return StatusCode(503, new { mensaje = "La base no tiene el procedimiento o tipo de detalle de ventas esperado. Verifica query.sql." });
                    if (sqlEx.Number == 229) return StatusCode(403, new { mensaje = "El usuario SQL no tiene permisos para registrar la venta." });
                    return StatusCode(503, new { mensaje = $"No se pudo registrar la venta en SQL Server (error {sqlEx.Number})." });
                }
                return StatusCode(503, new { mensaje = ex.Message });
            }
        }

        private static bool ValidLine(MobileSaleLineRequest item)
        {
            if (item.IdProducto.HasValue)
                return item.IdProducto.Value > 0 && string.IsNullOrWhiteSpace(item.DescripcionTemporal) && !item.PrecioUnitarioTemporal.HasValue;
            return !string.IsNullOrWhiteSpace(item.DescripcionTemporal) && item.DescripcionTemporal.Trim().Length <= 30 &&
                item.PrecioUnitarioTemporal.HasValue && item.PrecioUnitarioTemporal.Value > 0 &&
                item.PrecioUnitarioTemporal.Value <= MaxAmount && decimal.Round(item.PrecioUnitarioTemporal.Value, 2) == item.PrecioUnitarioTemporal.Value;
        }

        private static async Task<int> CreateTemporaryProduct(SqlConnection connection, MobileSaleLineRequest item, int quantity)
        {
            int categoryId;
            await using (var categoryCommand = new SqlCommand(@"
SELECT TOP (1) IdCategoria FROM dbo.CATEGORIA WHERE Estado=1
ORDER BY CASE WHEN UPPER(Descripcion) IN ('VARIOS','OTROS','MISCELANEOS') THEN 0 ELSE 1 END, IdCategoria;", connection))
            {
                var value = await categoryCommand.ExecuteScalarAsync();
                if (value == null || value == DBNull.Value) throw new InvalidOperationException("No hay una categoría activa para registrar el artículo temporal.");
                categoryId = Convert.ToInt32(value);
            }

            var code = "T" + Guid.NewGuid().ToString("N").Substring(0, 19);
            var description = item.DescripcionTemporal!.Trim().ToUpperInvariant();
            await using var command = new SqlCommand("dbo.sp_RegistrarProducto", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.Add("@Codigo", SqlDbType.VarChar, 20).Value = code;
            command.Parameters.Add("@Nombre", SqlDbType.VarChar, 30).Value = description;
            command.Parameters.Add("@Descripcion", SqlDbType.VarChar, 30).Value = "ARTICULO TEMPORAL VENTA MOVIL";
            command.Parameters.Add("@IdCategoria", SqlDbType.Int).Value = categoryId;
            AddDecimal(command, "@Porcentaje", 0m, 9, 4);
            AddDecimal(command, "@PrecioCompra", 0m, 10, 2);
            AddDecimal(command, "@PrecioVenta", 0m, 10, 2);
            command.Parameters.Add("@Estado", SqlDbType.Bit).Value = false;
            var result = command.Parameters.Add("@Resultado", SqlDbType.Int); result.Direction = ParameterDirection.Output;
            var message = command.Parameters.Add("@Mensaje", SqlDbType.VarChar, 500); message.Direction = ParameterDirection.Output;
            await command.ExecuteNonQueryAsync();
            var productId = Convert.ToInt32(result.Value);
            if (productId <= 0) throw new InvalidOperationException(Convert.ToString(message.Value) ?? "No se pudo crear el artículo temporal.");

            await using var stockCommand = new SqlCommand("UPDATE dbo.PRODUCTO SET Stock=@stock, Estado=0 WHERE IdProducto=@id", connection);
            stockCommand.Parameters.Add("@stock", SqlDbType.Int).Value = quantity;
            stockCommand.Parameters.Add("@id", SqlDbType.Int).Value = productId;
            if (await stockCommand.ExecuteNonQueryAsync() != 1)
            {
                await CleanupTemporaryProducts(connection, new[] { productId });
                throw new InvalidOperationException("No se pudo preparar el artículo temporal.");
            }
            return productId;
        }

        private static async Task CleanupTemporaryProducts(SqlConnection connection, IEnumerable<int> ids)
        {
            foreach (var id in ids)
            {
                await using var command = new SqlCommand(@"
DELETE FROM dbo.PRODUCTO WHERE IdProducto=@id AND Estado=0 AND Codigo LIKE 'T%'
AND NOT EXISTS (SELECT 1 FROM dbo.DETALLE_VENTA WHERE IdProducto=@id)
AND NOT EXISTS (SELECT 1 FROM dbo.DETALLE_COMPRA WHERE IdProducto=@id);", connection);
                command.Parameters.Add("@id", SqlDbType.Int).Value = id;
                await command.ExecuteNonQueryAsync();
            }
        }

        private static void AddDecimal(SqlCommand command, string name, decimal value, byte precision = 18, byte scale = 2)
        {
            var parameter = command.Parameters.Add(name, SqlDbType.Decimal);
            parameter.Precision = precision; parameter.Scale = scale; parameter.Value = value;
        }

        private static string EscapeLike(string value) => value.Replace("!", "!!").Replace("%", "!%").Replace("_", "!_").Replace("[", "![");
        private static string NormalizeDocument(string value) => value.Trim().ToUpperInvariant().Replace("-", string.Empty).Replace(" ", string.Empty);
        private IActionResult DatabaseNotConfigured() => StatusCode(503, new { mensaje = "El servidor no tiene configurada la conexión de productos." });

        private sealed class PreparedLine
        {
            public PreparedLine(MobileSaleLineRequest request, decimal price, decimal subtotal)
            { Request = request; Price = price; Subtotal = subtotal; ProductId = request.IdProducto.GetValueOrDefault(); }
            public MobileSaleLineRequest Request { get; }
            public decimal Price { get; }
            public decimal Subtotal { get; }
            public int ProductId { get; set; }
        }
    }

    public sealed class MobileProduct
    {
        public MobileProduct(int id, string codigo, string nombre, string descripcion, decimal precioVenta)
        { Id = id; Codigo = codigo; Nombre = nombre; Descripcion = descripcion; PrecioVenta = precioVenta; }
        public int Id { get; }
        public string Codigo { get; }
        public string Nombre { get; }
        public string Descripcion { get; }
        public decimal PrecioVenta { get; }
    }

    public sealed class MobileSaleRequest
    {
        public List<MobileSaleLineRequest>? Productos { get; set; }
        public decimal MontoPago { get; set; }
        public int? IdCliente { get; set; }
    }

    public sealed class MobileClientRequest
    {
        public string? Documento { get; set; }
        public string? NombreCompleto { get; set; }
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
    }

    public sealed class MobileClient
    {
        public MobileClient(int id, string documento, string nombreCompleto, string correo, string telefono)
        { Id = id; Documento = documento; NombreCompleto = nombreCompleto; Correo = correo; Telefono = telefono; }
        public int Id { get; }
        public string Documento { get; }
        public string NombreCompleto { get; }
        public string Correo { get; }
        public string Telefono { get; }
    }

    public sealed class MobileSaleLineRequest
    {
        public int? IdProducto { get; set; }
        public string? DescripcionTemporal { get; set; }
        public decimal? PrecioUnitarioTemporal { get; set; }
        public int Cantidad { get; set; }
    }

    public sealed class MobileSaleResponse
    {
        public MobileSaleResponse(string numeroDocumento, decimal total, decimal pago, decimal cambio)
        { NumeroDocumento = numeroDocumento; Total = total; Pago = pago; Cambio = cambio; }
        public string NumeroDocumento { get; }
        public decimal Total { get; }
        public decimal Pago { get; }
        public decimal Cambio { get; }
        public string Mensaje => $"Venta {NumeroDocumento} registrada. Total Q{Total:N2}; vuelto Q{Cambio:N2}.";
    }
}
