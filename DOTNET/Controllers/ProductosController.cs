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
    [Route("api/productos")]
    public sealed class ProductosController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProductosController> _logger;
        public ProductosController(IConfiguration configuration, ILogger<ProductosController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private string? ConnectionString => _configuration.GetConnectionString("ProductDb");
        private bool IsSignedIn => HttpContext.Session.GetInt32("ProductUserId").HasValue;

        [HttpGet("sesion")]
        public IActionResult SessionStatus() => Ok(new { autenticado = IsSignedIn,
            nombre = HttpContext.Session.GetString("ProductUserName") });

        [HttpPost("sesion")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Clave))
                return BadRequest(new { mensaje = "Escribe tu contraseña." });
            if (string.IsNullOrWhiteSpace(ConnectionString))
                return StatusCode(503, new { mensaje = "Falta configurar la conexión de SQL Server en el servidor." });

            try
            {
                await using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();
                await using var command = new SqlCommand(@"SELECT TOP (2) IdUsuario, NombreCompleto
                    FROM dbo.USUARIO WHERE Estado = 1 AND Clave = @clave ORDER BY IdUsuario", connection);
                command.Parameters.Add("@clave", SqlDbType.VarChar, 50).Value = request.Clave;
                await using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });
                var userId = reader.GetInt32(0);
                var userName = reader.IsDBNull(1) ? "Usuario" : reader.GetString(1);
                if (await reader.ReadAsync()) return Unauthorized(new { mensaje = "Esta contraseña corresponde a más de una cuenta. Pide que las contraseñas de usuario sean distintas." });
                HttpContext.Session.SetInt32("ProductUserId", userId);
                HttpContext.Session.SetString("ProductUserName", userName);
                return Ok(new { autenticado = true, nombre = HttpContext.Session.GetString("ProductUserName") });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error SQL durante el inicio de sesión. Código SQL {SqlErrorNumber}.", ex.Number);
                var detail = ex.Number switch
                {
                    -2 => "La conexión a SQL Server agotó el tiempo de espera.",
                    53 or 11001 => "No se pudo encontrar SQL Server en la dirección configurada.",
                    18456 => "SQL Server rechazó las credenciales configuradas para la aplicación.",
                    229 => "El usuario SQL no tiene permiso para consultar dbo.USUARIO.",
                    207 or 208 => "La tabla o las columnas de usuario no coinciden con el esquema esperado.",
                    _ => $"SQL Server devolvió el error {ex.Number} al validar el usuario."
                };
                return StatusCode(503, new { mensaje = $"No se pudo validar el acceso. {detail} Revisa el registro del servidor para ver el detalle." });
            }
        }

        [HttpDelete("sesion")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(new { autenticado = false });
        }

        [HttpGet("catalogos")]
        public async Task<IActionResult> Catalogs()
        {
            if (!IsSignedIn) return Unauthorized();
            if (string.IsNullOrWhiteSpace(ConnectionString)) return DatabaseNotConfigured();
            var result = new CatalogResponse();
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            result.Categorias.AddRange(await ReadRowsAsync(connection, "SELECT IdCategoria, Descripcion FROM dbo.CATEGORIA WHERE Estado = 1 ORDER BY Descripcion", r => new OptionItem(r.GetInt32(0), r.GetString(1)), "categorías", result.Avisos));
            result.GruposReposicion.AddRange(await ReadRowsAsync(connection, "SELECT IdGrupoReposicion, Nombre, CantidadMinima, CantidadMaxima FROM dbo.GRUPO_REPOSICION WHERE Activo = 1 ORDER BY Nombre", r => new ReplenishmentItem(r.GetInt32(0), r.GetString(1), r.GetInt32(2), r.GetInt32(3)), "grupos de productos", result.Avisos));
            result.GruposProveedor.AddRange(await ReadRowsAsync(connection, "SELECT IdGrupoProveedor, Nombre FROM dbo.GRUPO_PROVEEDOR WHERE Activo = 1 ORDER BY Nombre", r => new OptionItem(r.GetInt32(0), r.GetString(1)), "grupos de proveedores", result.Avisos));
            result.Proveedores.AddRange(await ReadRowsAsync(connection, "SELECT IdProveedor, RazonSocial FROM dbo.PROVEEDOR WHERE Estado = 1 ORDER BY RazonSocial", r => new OptionItem(r.GetInt32(0), r.GetString(1)), "proveedores", result.Avisos));
            return Ok(result);
        }

        [HttpGet("administracion")]
        public async Task<IActionResult> AdministrationCatalogs()
        {
            if (!IsSignedIn) return Unauthorized();
            if (string.IsNullOrWhiteSpace(ConnectionString)) return DatabaseNotConfigured();
            var result = new AdministrationResponse();
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            result.Categorias.AddRange(await ReadRowsAsync(connection, "SELECT IdCategoria, Descripcion, Estado FROM dbo.CATEGORIA ORDER BY Descripcion", r => new ManagedItem(r.GetInt32(0), r.GetString(1), !r.IsDBNull(2) && r.GetBoolean(2)), "categorías", result.Avisos));
            result.GruposProducto.AddRange(await ReadRowsAsync(connection, "SELECT IdGrupoReposicion, Nombre, Activo, CantidadMinima, CantidadMaxima FROM dbo.GRUPO_REPOSICION ORDER BY Nombre", r => new ProductGroupAdminItem(r.GetInt32(0), r.GetString(1), r.GetBoolean(2), r.GetInt32(3), r.GetInt32(4)), "grupos de productos", result.Avisos));
            result.GruposProveedor.AddRange(await ReadRowsAsync(connection, "SELECT IdGrupoProveedor, Nombre, Activo FROM dbo.GRUPO_PROVEEDOR ORDER BY Nombre", r => new SupplierGroupAdminItem(r.GetInt32(0), r.GetString(1), r.GetBoolean(2)), "grupos de proveedores", result.Avisos));
            var details = await ReadRowsAsync(connection, "SELECT IdGrupoProveedor, IdProveedor FROM dbo.GRUPO_PROVEEDOR_DETALLE ORDER BY IdGrupoProveedor, IdProveedor", r => new KeyValuePair<int, int>(r.GetInt32(0), r.GetInt32(1)), "asociaciones de proveedores", result.Avisos);
            var byId = new Dictionary<int, SupplierGroupAdminItem>(); foreach (var item in result.GruposProveedor) byId[item.Id] = item;
            foreach (var detail in details) if (byId.TryGetValue(detail.Key, out var group)) group.Proveedores.Add(detail.Value);
            result.Proveedores.AddRange(await ReadRowsAsync(connection, "SELECT IdProveedor, RazonSocial FROM dbo.PROVEEDOR WHERE Estado = 1 ORDER BY RazonSocial", r => new OptionItem(r.GetInt32(0), r.GetString(1)), "proveedores", result.Avisos));
            return Ok(result);
        }

        private async Task<List<T>> ReadRowsAsync<T>(SqlConnection connection, string sql, Func<SqlDataReader, T> create, string label, List<string> warnings)
        {
            var rows = new List<T>();
            try
            {
                await using var command = new SqlCommand(sql, connection);
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync()) rows.Add(create(reader));
            }
            catch (SqlException ex) when (ex.Number == 208 || ex.Number == 207 || ex.Number == 229)
            {
                warnings.Add(ex.Number == 229
                    ? $"La conexión no tiene permiso para leer {label}. Revisa los permisos de SQL Server."
                    : $"No se pudieron cargar {label}. Revisa que estén aplicadas las migraciones correspondientes.");
            }
            return rows;
        }

        [HttpPut("categorias/{id:int}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] NameRequest request) => await UpdateName(id, request.Nombre, "UPDATE dbo.CATEGORIA SET Descripcion=@nombre, Estado=1 WHERE IdCategoria=@id");

        [HttpPut("grupos-reposicion/{id:int}")]
        public async Task<IActionResult> UpdateProductGroup(int id, [FromBody] ReplenishmentRequest request)
        {
            if (!IsSignedIn) return Unauthorized();
            var name = request.Nombre?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100 || request.CantidadMinima < 0 || request.CantidadMaxima < request.CantidadMinima)
                return BadRequest(new { mensaje = "Revisa el nombre y las cantidades: el máximo debe ser igual o mayor al mínimo." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return DatabaseNotConfigured();
            await using var connection = new SqlConnection(ConnectionString); await connection.OpenAsync();
            await using var command = new SqlCommand("UPDATE dbo.GRUPO_REPOSICION SET Nombre=@nombre, CantidadMinima=@minimo, CantidadMaxima=@maximo, Activo=1 WHERE IdGrupoReposicion=@id", connection);
            command.Parameters.Add("@nombre", SqlDbType.VarChar, 100).Value = name;
            command.Parameters.Add("@minimo", SqlDbType.Int).Value = request.CantidadMinima;
            command.Parameters.Add("@maximo", SqlDbType.Int).Value = request.CantidadMaxima;
            command.Parameters.Add("@id", SqlDbType.Int).Value = id;
            try { return await command.ExecuteNonQueryAsync() == 1 ? Ok(new { mensaje = "Grupo actualizado." }) : NotFound(new { mensaje = "No se encontró el grupo." }); }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601) { return Conflict(new { mensaje = "Ya existe un grupo con ese nombre." }); }
        }

        [HttpPut("grupos-proveedor/{id:int}")]
        public async Task<IActionResult> UpdateSupplierGroup(int id, [FromBody] SupplierGroupRequest request)
        {
            if (!IsSignedIn) return Unauthorized();
            var name = request.Nombre?.Trim(); var ids = request.Proveedores ?? new List<int>();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100 || ids.Count == 0) return BadRequest(new { mensaje = "Escribe el nombre y selecciona al menos un proveedor." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return DatabaseNotConfigured();
            await using var connection = new SqlConnection(ConnectionString); await connection.OpenAsync();
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
            try
            {
                await using (var update = new SqlCommand("UPDATE dbo.GRUPO_PROVEEDOR SET Nombre=@nombre, Activo=1 WHERE IdGrupoProveedor=@id", connection, transaction))
                {
                    update.Parameters.Add("@nombre", SqlDbType.VarChar, 100).Value = name; update.Parameters.Add("@id", SqlDbType.Int).Value = id;
                    if (await update.ExecuteNonQueryAsync() != 1) { await transaction.RollbackAsync(); return NotFound(new { mensaje = "No se encontró el grupo." }); }
                }
                await using (var remove = new SqlCommand("DELETE dbo.GRUPO_PROVEEDOR_DETALLE WHERE IdGrupoProveedor=@id", connection, transaction)) { remove.Parameters.Add("@id", SqlDbType.Int).Value = id; await remove.ExecuteNonQueryAsync(); }
                foreach (var supplierId in ids)
                {
                    await using var detail = new SqlCommand("INSERT dbo.GRUPO_PROVEEDOR_DETALLE(IdGrupoProveedor, IdProveedor) SELECT @grupo, IdProveedor FROM dbo.PROVEEDOR WHERE IdProveedor=@proveedor AND Estado=1", connection, transaction);
                    detail.Parameters.Add("@grupo", SqlDbType.Int).Value = id; detail.Parameters.Add("@proveedor", SqlDbType.Int).Value = supplierId;
                    if (await detail.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException();
                }
                await transaction.CommitAsync(); return Ok(new { mensaje = "Grupo actualizado." });
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601) { await transaction.RollbackAsync(); return Conflict(new { mensaje = "Ya existe un grupo con ese nombre." }); }
            catch { await transaction.RollbackAsync(); return BadRequest(new { mensaje = "No se pudo guardar el grupo o sus proveedores." }); }
        }
        private async Task<IActionResult> UpdateName(int id, string? rawName, string sql)
        {
            if (!IsSignedIn) return Unauthorized();
            var name = rawName?.Trim(); if (string.IsNullOrWhiteSpace(name) || name.Length > 100) return BadRequest(new { mensaje = "Escribe un nombre de hasta 100 caracteres." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return DatabaseNotConfigured();
            await using var connection = new SqlConnection(ConnectionString); await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection); command.Parameters.Add("@nombre", SqlDbType.VarChar, 100).Value = name; command.Parameters.Add("@id", SqlDbType.Int).Value = id;
            try { return await command.ExecuteNonQueryAsync() == 1 ? Ok(new { mensaje = "Registro actualizado." }) : NotFound(new { mensaje = "No se encontró el registro." }); }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601) { return Conflict(new { mensaje = "Ya existe un registro con ese nombre." }); }
        }

        [HttpPost("categorias")]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateRequest request)
        {
            if (!IsSignedIn) return Unauthorized();
            var name = request.Nombre?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100) return BadRequest(new { mensaje = "La categoría debe tener entre 1 y 100 caracteres." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return DatabaseNotConfigured();
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand("INSERT dbo.CATEGORIA(Descripcion, Estado) OUTPUT INSERTED.IdCategoria VALUES(@nombre, @estado)", connection);
            command.Parameters.Add("@nombre", SqlDbType.VarChar, 100).Value = name;
            command.Parameters.Add("@estado", SqlDbType.Bit).Value = request.Estado;
            try { return Ok(new { id = Convert.ToInt32(await command.ExecuteScalarAsync()), nombre = name, activo = request.Estado }); }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601) { return Conflict(new { mensaje = "Ya existe una categoría con ese nombre." }); }
            catch (SqlException ex) when (ex.Number == 229) { return StatusCode(403, new { mensaje = "La conexión SQL no tiene permiso para crear categorías." }); }
            catch (SqlException ex) when (ex.Number == 207 || ex.Number == 208) { return StatusCode(503, new { mensaje = "No se encontró la tabla o columna de categorías esperada en la base de datos." }); }
        }

        [HttpPost("grupos-reposicion")]
        public async Task<IActionResult> CreateReplenishmentGroup([FromBody] ReplenishmentRequest request)
        {
            if (!IsSignedIn) return Unauthorized();
            var name = request.Nombre?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100 || request.CantidadMinima < 0 || request.CantidadMaxima < request.CantidadMinima)
                return BadRequest(new { mensaje = "Revisa el nombre y las cantidades: el máximo debe ser igual o mayor al mínimo." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return DatabaseNotConfigured();
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand("INSERT dbo.GRUPO_REPOSICION(Nombre, CantidadMinima, CantidadMaxima) OUTPUT INSERTED.IdGrupoReposicion VALUES(@nombre, @minimo, @maximo)", connection);
            command.Parameters.Add("@nombre", SqlDbType.VarChar, 100).Value = name;
            command.Parameters.Add("@minimo", SqlDbType.Int).Value = request.CantidadMinima;
            command.Parameters.Add("@maximo", SqlDbType.Int).Value = request.CantidadMaxima;
            try { return Ok(new ReplenishmentItem(Convert.ToInt32(await command.ExecuteScalarAsync()), name, request.CantidadMinima, request.CantidadMaxima)); }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601) { return Conflict(new { mensaje = "Ya existe un grupo con ese nombre." }); }
        }

        [HttpPost("grupos-proveedor")]
        public async Task<IActionResult> CreateSupplierGroup([FromBody] SupplierGroupRequest request)
        {
            if (!IsSignedIn) return Unauthorized();
            var name = request.Nombre?.Trim();
            var supplierIds = request.Proveedores ?? new List<int>();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100 || supplierIds.Count == 0) return BadRequest(new { mensaje = "Escribe el nombre del grupo y selecciona al menos un proveedor." });
            if (string.IsNullOrWhiteSpace(ConnectionString)) return DatabaseNotConfigured();
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
            try
            {
                await using var create = new SqlCommand("INSERT dbo.GRUPO_PROVEEDOR(Nombre) OUTPUT INSERTED.IdGrupoProveedor VALUES(@nombre)", connection, transaction);
                create.Parameters.Add("@nombre", SqlDbType.VarChar, 100).Value = name;
                var id = Convert.ToInt32(await create.ExecuteScalarAsync());
                foreach (var supplierId in supplierIds)
                {
                    await using var detail = new SqlCommand("INSERT dbo.GRUPO_PROVEEDOR_DETALLE(IdGrupoProveedor, IdProveedor) SELECT @grupo, IdProveedor FROM dbo.PROVEEDOR WHERE IdProveedor = @proveedor AND Estado = 1", connection, transaction);
                    detail.Parameters.Add("@grupo", SqlDbType.Int).Value = id;
                    detail.Parameters.Add("@proveedor", SqlDbType.Int).Value = supplierId;
                    if (await detail.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("Uno de los proveedores ya no está disponible.");
                }
                await transaction.CommitAsync();
                return Ok(new OptionItem(id, name));
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                await transaction.RollbackAsync();
                return Conflict(new { mensaje = "Ya existe un grupo con ese nombre o hay proveedores repetidos." });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { mensaje = "No se pudo crear el grupo de proveedores. Revisa los proveedores seleccionados." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductRequest request)
        {
            if (!IsSignedIn) return Unauthorized();
            if (string.IsNullOrWhiteSpace(ConnectionString)) return DatabaseNotConfigured();
            if (string.IsNullOrWhiteSpace(request.Codigo) || request.Codigo.Trim().Length > 20 || string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > 30 || (request.Descripcion?.Length ?? 0) > 30)
                return BadRequest(new { mensaje = "Código (máximo 20), nombre (máximo 30) y descripción (máximo 30) son requeridos." });
            if (request.PrecioCompra < 0 || request.Porcentaje < 0 || request.Porcentaje > 100000)
                return BadRequest(new { mensaje = "El costo y el porcentaje deben ser valores válidos y no negativos." });
            var purchasePrice = request.PrecioCompra;
            var salePrice = request.PrecioVenta;
            if (request.CalcularCosto || purchasePrice == 0)
            {
                if (request.Porcentaje <= 0)
                    return BadRequest(new { mensaje = "Con costo desconocido, el porcentaje debe ser mayor que cero para calcular el costo." });
                if (salePrice <= 0 || Math.Abs(salePrice * 4 - Math.Round(salePrice * 4)) > 0.0001m)
                    return BadRequest(new { mensaje = "Indica un precio de venta válido en intervalos de Q0.25." });
                purchasePrice = decimal.Round(salePrice / (1m + request.Porcentaje / 100m), 2, MidpointRounding.AwayFromZero);
                if (purchasePrice <= 0) return BadRequest(new { mensaje = "Con esos datos, el costo calculado es menor a Q0.01." });
            }
            else
            {
                var target = purchasePrice * (1m + request.Porcentaje / 100m);
                salePrice = decimal.Ceiling(target * 4m) / 4m;
            }
            if (purchasePrice > 99999999.99m || salePrice <= 0 || salePrice > 99999999.99m || Math.Abs(salePrice * 4 - Math.Round(salePrice * 4)) > 0.0001m)
                return BadRequest(new { mensaje = "El precio calculado excede los límites admitidos o no respeta los intervalos de Q0.25." });
            var useGroup = request.IdGrupoReposicion.HasValue;
            if (!useGroup)
            {
                if (!request.CantidadMinima.HasValue || !request.CantidadMaxima.HasValue)
                    return BadRequest(new { mensaje = "Sin grupo de productos, indica la cantidad mínima y máxima." });
                if (request.CantidadMinima.Value < 0 || request.CantidadMaxima.Value < 0 ||
                    request.CantidadMaxima.Value < request.CantidadMinima.Value)
                    return BadRequest(new { mensaje = "El mínimo y el máximo deben ser enteros no negativos, y el máximo debe ser igual o mayor que el mínimo." });
            }
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand("dbo.sp_RegistrarProducto", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.Add("@Codigo", SqlDbType.VarChar, 20).Value = request.Codigo.Trim();
            command.Parameters.Add("@Nombre", SqlDbType.VarChar, 30).Value = request.Nombre.Trim();
            command.Parameters.Add("@Descripcion", SqlDbType.VarChar, 30).Value = (object?)request.Descripcion?.Trim() ?? DBNull.Value;
            command.Parameters.Add("@IdCategoria", SqlDbType.Int).Value = request.IdCategoria;
            var margin = command.Parameters.Add("@Porcentaje", SqlDbType.Decimal); margin.Precision = 9; margin.Scale = 4; margin.Value = request.Porcentaje / 100m;
            var cost = command.Parameters.Add("@PrecioCompra", SqlDbType.Decimal); cost.Precision = 10; cost.Scale = 2; cost.Value = purchasePrice;
            var sale = command.Parameters.Add("@PrecioVenta", SqlDbType.Decimal); sale.Precision = 10; sale.Scale = 2; sale.Value = salePrice;
            command.Parameters.Add("@Estado", SqlDbType.Bit).Value = request.Estado;
            var result = command.Parameters.Add("@Resultado", SqlDbType.Int); result.Direction = ParameterDirection.Output;
            var message = command.Parameters.Add("@Mensaje", SqlDbType.VarChar, 500); message.Direction = ParameterDirection.Output;
            command.Parameters.Add("@IdGrupoReposicion", SqlDbType.Int).Value = (object?)request.IdGrupoReposicion ?? DBNull.Value;
            command.Parameters.Add("@CantidadMinima", SqlDbType.Int).Value = useGroup ? DBNull.Value : (object?)request.CantidadMinima ?? DBNull.Value;
            command.Parameters.Add("@CantidadMaxima", SqlDbType.Int).Value = useGroup ? DBNull.Value : (object?)request.CantidadMaxima ?? DBNull.Value;
            command.Parameters.Add("@IdGrupoProveedor", SqlDbType.Int).Value = (object?)request.IdGrupoProveedor ?? DBNull.Value;
            try { await command.ExecuteNonQueryAsync(); }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error SQL al crear producto. Código SQL {SqlErrorNumber}.", ex.Number);
                if (ex.Number == 8144 || ex.Number == 201 || ex.Number == 2812)
                    return StatusCode(503, new { mensaje = "El procedimiento dbo.sp_RegistrarProducto está desactualizado. Ejecuta migracion_sp_registrar_producto.sql en la base DBSISTEMA_VENTA y vuelve a intentar." });
                if (ex.Number == 229)
                    return StatusCode(403, new { mensaje = "El usuario de SQL no tiene permiso para ejecutar dbo.sp_RegistrarProducto o registrar sus datos." });
                if (ex.Number == 2627 || ex.Number == 2601)
                    return Conflict(new { mensaje = "Ya existe un producto con ese código." });
                if (ex.Number == 547)
                    return Conflict(new { mensaje = "La categoría o el grupo seleccionado ya no existe o no es válido. Actualiza los catálogos e intenta de nuevo." });
                return StatusCode(500, new { mensaje = $"SQL Server no pudo crear el producto (error {ex.Number}). Revisa el registro del servidor." });
            }
            var productId = Convert.ToInt32(result.Value);
            if (productId <= 0) return Conflict(new { mensaje = Convert.ToString(message.Value) ?? "No se pudo registrar el producto." });
            return Ok(new { idProducto = productId, precioCompra = purchasePrice, precioVenta = salePrice, mensaje = $"Producto creado · costo Q{purchasePrice:N2} · venta Q{salePrice:N2}. Existencia inicial: 0." });
        }

        private IActionResult DatabaseNotConfigured() => StatusCode(503, new { mensaje = "El servidor no tiene configurada la conexión de productos." });
    }

    public sealed class LoginRequest { public string Clave { get; set; } = ""; }
    public class NameRequest { public string? Nombre { get; set; } }
    public sealed class CategoryCreateRequest : NameRequest { public bool Estado { get; set; } = true; }
    public sealed class ReplenishmentRequest : NameRequest { public int CantidadMinima { get; set; } public int CantidadMaxima { get; set; } }
    public sealed class SupplierGroupRequest : NameRequest { public List<int>? Proveedores { get; set; } }
    public sealed class ProductRequest
    {
        public string Codigo { get; set; } = ""; public string Nombre { get; set; } = ""; public string? Descripcion { get; set; }
        public int IdCategoria { get; set; } public decimal Porcentaje { get; set; } public decimal PrecioCompra { get; set; } public decimal PrecioVenta { get; set; } public bool CalcularCosto { get; set; }
        public bool Estado { get; set; } = true; public int? IdGrupoReposicion { get; set; } public int? CantidadMinima { get; set; }
        public int? CantidadMaxima { get; set; } public int? IdGrupoProveedor { get; set; }
    }
    public class OptionItem { public OptionItem(int id, string nombre) { Id = id; Nombre = nombre; } public int Id { get; } public string Nombre { get; } }
    public sealed class ReplenishmentItem : OptionItem
    {
        public ReplenishmentItem(int id, string nombre, int minimo, int maximo) : base(id, nombre) { CantidadMinima = minimo; CantidadMaxima = maximo; }
        public int CantidadMinima { get; } public int CantidadMaxima { get; }
    }
    public sealed class CatalogResponse
    {
        public List<OptionItem> Categorias { get; } = new List<OptionItem>(); public List<ReplenishmentItem> GruposReposicion { get; } = new List<ReplenishmentItem>();
        public List<OptionItem> GruposProveedor { get; } = new List<OptionItem>(); public List<OptionItem> Proveedores { get; } = new List<OptionItem>();
        public List<string> Avisos { get; } = new List<string>();
    }
    public class ManagedItem : OptionItem
    {
        public ManagedItem(int id, string nombre, bool activo) : base(id, nombre) { Activo = activo; }
        public bool Activo { get; }
    }
    public sealed class SupplierGroupAdminItem : ManagedItem
    {
        public SupplierGroupAdminItem(int id, string nombre, bool activo) : base(id, nombre, activo) { }
        public List<int> Proveedores { get; } = new List<int>();
    }
    public sealed class ProductGroupAdminItem : ManagedItem
    {
        public ProductGroupAdminItem(int id, string nombre, bool activo, int minimo, int maximo) : base(id, nombre, activo) { CantidadMinima = minimo; CantidadMaxima = maximo; }
        public int CantidadMinima { get; } public int CantidadMaxima { get; }
    }
    public sealed class AdministrationResponse
    {
        public List<ManagedItem> Categorias { get; } = new List<ManagedItem>();
        public List<ProductGroupAdminItem> GruposProducto { get; } = new List<ProductGroupAdminItem>();
        public List<SupplierGroupAdminItem> GruposProveedor { get; } = new List<SupplierGroupAdminItem>();
        public List<OptionItem> Proveedores { get; } = new List<OptionItem>();
        public List<string> Avisos { get; } = new List<string>();
    }
}
