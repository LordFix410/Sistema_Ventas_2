using System;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ImagenPDF.Infrastructure
{
    public sealed class WebPageAuthorizationMiddleware
    {
        private readonly RequestDelegate next;
        private readonly IConfiguration configuration;

        public WebPageAuthorizationMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            this.next = next;
            this.configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            if (string.Equals(path, "/api/productos/sesion", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }
            var summaryEndpoint = string.Equals(path, "/api/finanzas/resumen", StringComparison.OrdinalIgnoreCase);
            var expenseHistoryEndpoint = string.Equals(path, "/api/finanzas/operaciones/movimientos", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(context.Request.Query["tipo"], "salidas", StringComparison.OrdinalIgnoreCase);
            var page = summaryEndpoint ? "dashboard.html" : expenseHistoryEndpoint ? "salidas.html" : PageFor(path);
            if (page == null)
            {
                await next(context);
                return;
            }

            var userId = context.Session.GetInt32("ProductUserId");
            if (!userId.HasValue)
            {
                if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { mensaje = "Inicia sesión para usar este módulo web." }));
                    return;
                }
                await next(context);
                return;
            }

            try
            {
                if (await CanAccessAsync(userId.Value, page, summaryEndpoint ? "finanzas.html" : null))
                {
                    await next(context);
                    return;
                }

                context.Session.Clear();
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                if (path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync("<!doctype html><html lang='es'><meta charset='utf-8'><title>Acceso restringido</title><body style='font:16px Segoe UI,Arial;padding:36px;color:#183b53'><h1>Acceso restringido</h1><p>Tu usuario no tiene permiso para abrir esta página. Pide al administrador principal que revise tus accesos.</p><a href='/index.html'>Volver al inicio</a></body></html>");
                }
                else
                {
                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { mensaje = "Tu usuario no tiene permiso para usar este módulo web. Se cerró la sesión." }));
                }
            }
            catch (Exception)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { mensaje = "No se pudo validar el acceso. Aplica migracion_permisos_pagina_web.sql en la base de datos y verifica la conexión." }));
            }
        }

        private async Task<bool> CanAccessAsync(int userId, string page, string? alternatePage = null)
        {
            var connectionString = configuration.GetConnectionString("ProductDb");
            if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Falta la conexión SQL.");
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(@"SELECT CASE WHEN u.EsAdministradorPrincipal=1 OR EXISTS(
    SELECT 1 FROM dbo.PERMISO_PAGINA_WEB p INNER JOIN dbo.PAGINA_WEB w ON w.CodigoPagina=p.CodigoPagina
    WHERE p.IdUsuario=u.IdUsuario AND (p.CodigoPagina=@pagina OR (@paginaAlterna IS NOT NULL AND p.CodigoPagina=@paginaAlterna)) AND w.Activa=1
) THEN 1 ELSE 0 END
FROM dbo.USUARIO u WHERE u.IdUsuario=@usuario AND u.Estado=1;", connection);
            command.Parameters.Add("@usuario", SqlDbType.Int).Value = userId;
            command.Parameters.Add("@pagina", SqlDbType.VarChar, 100).Value = page;
            command.Parameters.Add("@paginaAlterna", SqlDbType.VarChar, 100).Value = (object?)alternatePage ?? DBNull.Value;
            var result = await command.ExecuteScalarAsync();
            return result != null && result != DBNull.Value && Convert.ToInt32(result) == 1;
        }

        private static string? PageFor(string path)
        {
            string? file = Path.GetFileName(path);
            switch ((file ?? string.Empty).ToLowerInvariant())
            {
                case "dashboard.html": return "dashboard.html";
                case "finanzas.html": return "finanzas.html";
                case "depositos.html": return "depositos.html";
                case "salidas.html": return "salidas.html";
                case "conciliacion.html": return "conciliacion.html";
                case "productos.html": return "productos.html";
                case "venta.html": return "venta.html";
            }
            if (path.StartsWith("/api/productos", StringComparison.OrdinalIgnoreCase)) return "productos.html";
            if (path.StartsWith("/api/ventas-moviles", StringComparison.OrdinalIgnoreCase)) return "venta.html";
            if (path.StartsWith("/api/finanzas/conciliaciones", StringComparison.OrdinalIgnoreCase)) return "conciliacion.html";
            if (path.StartsWith("/api/finanzas/operaciones/salidas", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/finanzas/salidas", StringComparison.OrdinalIgnoreCase)) return "salidas.html";
            if (path.StartsWith("/api/finanzas/operaciones", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/finanzas/depositos", StringComparison.OrdinalIgnoreCase)) return "depositos.html";
            if (path.StartsWith("/api/finanzas", StringComparison.OrdinalIgnoreCase)) return "finanzas.html";
            return null;
        }
    }
}
