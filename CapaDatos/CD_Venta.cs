using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CapaDatos
{
    public class CD_Venta
    {
        public int obtenerCorrelativo()
        {
            int idcorrelativo = 0;

            using (SqlConnection oconexion = new SqlConnection(Conexion.cadena))

            {
                try
                {
                    StringBuilder query = new StringBuilder();
                    query.AppendLine("select isnull(max(try_convert(int, NumeroDocumento)), 0) + 1 from VENTA");

                    SqlCommand cmd = new SqlCommand(query.ToString(), oconexion);
                    cmd.CommandType = CommandType.Text;


                    oconexion.Open();
                    idcorrelativo = Convert.ToInt32(cmd.ExecuteScalar());
                }
                catch (Exception ex)
                {
                    idcorrelativo = 0;
                }
            }

            return idcorrelativo;
        }

      public bool RestarStock(int idproducto, int cantidad)
        {
            bool respuesta = true;

            using (SqlConnection oconexion = new SqlConnection(Conexion.cadena))
            {
                try
                {
                    StringBuilder query = new StringBuilder();
                    query.AppendLine("update producto set stock = stock - @cantidad where idproducto = @idproducto and @cantidad > 0 and estado = 1");

                    SqlCommand cmd = new SqlCommand(query.ToString(), oconexion);
                    cmd.Parameters.Add("@cantidad", SqlDbType.Int).Value = cantidad;
                    cmd.Parameters.Add("@idproducto", SqlDbType.Int).Value = idproducto;
                    cmd.CommandType = CommandType.Text;
                    oconexion.Open();

                    respuesta = cmd.ExecuteNonQuery() > 0 ? true : false;
                }
                catch (Exception ex)
                {
                    respuesta = false;
                }
            }
            return respuesta;
      }
        public bool SumarStock(int idproducto, int cantidad)
        {
            bool respuesta = true;

            using (SqlConnection oconexion = new SqlConnection(Conexion.cadena))
            {
                try
                {
                    StringBuilder query = new StringBuilder();
                    query.AppendLine("update producto set stock = stock + @cantidad where idproducto = @idproducto and @cantidad > 0");

                    SqlCommand cmd = new SqlCommand(query.ToString(), oconexion);
                    cmd.Parameters.Add("@cantidad", SqlDbType.Int).Value = cantidad;
                    cmd.Parameters.Add("@idproducto", SqlDbType.Int).Value = idproducto;
                    cmd.CommandType = CommandType.Text;
                    oconexion.Open();

                    respuesta = cmd.ExecuteNonQuery() > 0 ? true : false;
                }
                catch (Exception ex)
                {
                    respuesta = false;
                }
            }
            return respuesta;
        }

        public bool Registrar(Venta obj, DataTable DetalleVenta, out string Mensaje)
        {
            bool respuesta = false;
            Mensaje = string.Empty;


            try
            {
                using (SqlConnection oconexion = new SqlConnection(Conexion.cadena))
                {
                    SqlCommand cmd = new SqlCommand("usp_RegistrarVenta", oconexion);
                    cmd.Parameters.AddWithValue("IdUsuario", obj.oUsuario.IdUsuario);
                    cmd.Parameters.AddWithValue("TipoDocumento", obj.TipoDocumento);
                    SqlParameter numeroDocumento = cmd.Parameters.Add("NumeroDocumento", SqlDbType.VarChar, 500);
                    numeroDocumento.Direction = ParameterDirection.InputOutput;
                    numeroDocumento.Value = obj.NumeroDocumento;
                    cmd.Parameters.AddWithValue("DocumentoCliente", obj.DocumentoCliente);
                    cmd.Parameters.AddWithValue("NombreCliente", obj.NombreCliente);
                    cmd.Parameters.AddWithValue("MontoPago", obj.MontoPago);
                    cmd.Parameters.AddWithValue("MontoCambio", obj.MontoCambio);
                    cmd.Parameters.AddWithValue("MontoTotal", obj.MontoTotal);
                    SqlParameter parametroDetalle = cmd.Parameters.AddWithValue("DetalleVenta", DetalleVenta);
                    parametroDetalle.SqlDbType = SqlDbType.Structured;
                    parametroDetalle.TypeName = "dbo.EDetalle_Venta";
                    cmd.Parameters.Add("Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;
                    cmd.CommandType = CommandType.StoredProcedure;

                    oconexion.Open();
                    cmd.ExecuteNonQuery();

                    respuesta = Convert.ToBoolean(cmd.Parameters["Resultado"].Value);
                    Mensaje = cmd.Parameters["Mensaje"].Value.ToString();
                    if (respuesta) obj.NumeroDocumento = numeroDocumento.Value.ToString();
                }


            }
            catch (Exception ex)
            {
                respuesta =false;
                Mensaje = ex.Message;

            }
            return respuesta;
        }

        public Venta obtenerVenta(string numero)
        {
            Venta obj = new Venta();
            using (SqlConnection oconexion = new SqlConnection(Conexion.cadena))
            {
                try
                {
                    oconexion.Open();
                    StringBuilder query = new StringBuilder();

                    query.AppendLine("select v.IdVenta, u.NombreCompleto,");
                    query.AppendLine("v.DocumentoCliente, v.NombreCliente,");
                    query.AppendLine("v.TipoDocumento,v.NumeroDocumento,");
                    query.AppendLine("v.MontoPago, v.MontoCambio,v.MontoTotal,");
                    query.AppendLine("convert(char(10), v.FechaRegistro, 103)[FechaRegistro]");
                    query.AppendLine("from VENTA v");
                    query.AppendLine("inner");
                    query.AppendLine("join USUARIO u on u.IdUsuario = V.IdUsuario");
                    query.AppendLine("where v.NumeroDocumento = @numero");

                    SqlCommand cmd = new SqlCommand(query.ToString(), oconexion);
                    cmd.Parameters.AddWithValue("@numero", numero);
                    cmd.CommandType = System.Data.CommandType.Text;

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            obj = new Venta()
                            {
                                IdVenta = int.Parse(dr["IdVenta"].ToString()),
                                oUsuario = new Usuario() { NombreCompleto = dr["NombreCompleto"].ToString() },
                                DocumentoCliente = dr["DocumentoCliente"].ToString(),
                                NombreCliente = dr["NombreCliente"].ToString(),
                                TipoDocumento = dr["TipoDocumento"].ToString(),
                                
                                NumeroDocumento = dr["NumeroDocumento"].ToString(),
                                MontoPago = Convert.ToDecimal(dr["MontoPago"].ToString()),
                                MontoCambio = Convert.ToDecimal(dr["MontoCambio"].ToString()),
                                MontoTotal = Convert.ToDecimal(dr["MontoTotal"].ToString()),
                                FechaRegistro  = dr["FechaRegistro"].ToString()
                            };
                            
                        }
                    }

                }
                catch(Exception ex)
                {
                    obj = new Venta();

                }
                return obj;
            }
            
        }
        public List<Detalle_Venta> obtenerDetalleVenta(int idVenta)
        {
            List<Detalle_Venta> oLista = new List<Detalle_Venta>();

            using (SqlConnection oconexion = new SqlConnection(Conexion.cadena))
            {
                try
                {
                    oconexion.Open();
                    StringBuilder query = new StringBuilder();
                    query.AppendLine("select p.Nombre, dv.PrecioVenta, dv.Cantidad, dv.SubTotal from DETALLE_VENTA dv");
                    query.AppendLine("inner join PRODUCTO p on p.IdProducto = dv.IdProducto");
                    query.AppendLine("where dv.IdVenta = @idventa");

                    SqlCommand cmd = new SqlCommand(query.ToString(), oconexion);
                    cmd.Parameters.AddWithValue("@idventa", idVenta);
                    cmd.CommandType = System.Data.CommandType.Text;

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            oLista.Add(new Detalle_Venta()
                            {
                                oProducto = new Producto() { Nombre = dr["Nombre"].ToString() },
                                PrecioVenta = Convert.ToDecimal(dr["PrecioVenta"].ToString()),
                                Cantidad = Convert.ToInt32(dr["Cantidad"].ToString()),
                                SubTotal = Convert.ToDecimal(dr["SubTotal"].ToString())
                            });
                        }
                    }
                }
                catch(Exception ex)
                {
                    oLista = new List<Detalle_Venta>();
                }
            }
            return oLista;
        }

        public DataTable ObtenerDetalleDevolucion(string numeroDocumento, out string mensaje)
        {
            mensaje = string.Empty;
            DataTable tabla = new DataTable();
            using (SqlConnection conexion = new SqlConnection(Conexion.cadena))
            using (SqlCommand comando = new SqlCommand(@"
SELECT v.IdVenta, v.NumeroDocumento, v.FechaRegistro, v.DocumentoCliente, v.NombreCliente, v.MontoTotal AS TotalVenta,
       ISNULL((SELECT SUM(h.MontoDevuelto) FROM dbo.DEVOLUCION_VENTA h WHERE h.IdVenta=v.IdVenta),0) AS MontoDevueltoAcumulado,
       dv.IdDetalleVenta, p.IdProducto, p.Nombre AS Producto, dv.Cantidad AS CantidadVendida,
       ISNULL(dev.CantidadDevuelta,0) AS CantidadDevuelta,
       dv.Cantidad-ISNULL(dev.CantidadDevuelta,0) AS CantidadPendiente,
       dv.PrecioVenta,
       CASE WHEN ISNULL(p.Codigo,'') LIKE 'T%' AND UPPER(ISNULL(p.Descripcion,''))='ARTICULO TEMPORAL DE VENTA' THEN 1 ELSE 0 END AS EsTemporal
FROM dbo.VENTA v
INNER JOIN dbo.DETALLE_VENTA dv ON dv.IdVenta=v.IdVenta
INNER JOIN dbo.PRODUCTO p ON p.IdProducto=dv.IdProducto
OUTER APPLY
(
    SELECT SUM(dd.Cantidad) AS CantidadDevuelta
    FROM dbo.DETALLE_DEVOLUCION_VENTA dd
    WHERE dd.IdDetalleVenta=dv.IdDetalleVenta
) dev
WHERE v.NumeroDocumento=@NumeroDocumento
ORDER BY dv.IdDetalleVenta;", conexion))
            {
                comando.Parameters.Add("@NumeroDocumento", SqlDbType.VarChar, 50).Value = (numeroDocumento ?? string.Empty).Trim();
                try
                {
                    using (SqlDataAdapter adaptador = new SqlDataAdapter(comando))
                        adaptador.Fill(tabla);
                }
                catch (Exception ex)
                {
                    mensaje = ex.Message;
                }
            }
            return tabla;
        }

        public DataTable ObtenerHistorialDevoluciones(out string mensaje)
        {
            mensaje = string.Empty;
            DataTable tabla = new DataTable();
            const string consulta = @"
SELECT TOP (100) h.IdDevolucion AS IdRegistro, h.FechaRegistro, v.NumeroDocumento, h.Tipo, h.Motivo,
       COALESCE(
           STUFF((
               SELECT CHAR(13)+CHAR(10)+ISNULL(p.Nombre,N'Producto')+N' × '+CONVERT(nvarchar(12),dd.Cantidad)+N' (Q '+CONVERT(nvarchar(30),dd.SubTotal)+N')'
               FROM dbo.DETALLE_DEVOLUCION_VENTA dd
               INNER JOIN dbo.DETALLE_VENTA dv ON dv.IdDetalleVenta=dd.IdDetalleVenta
               INNER JOIN dbo.PRODUCTO p ON p.IdProducto=dv.IdProducto
               WHERE dd.IdDevolucion=h.IdDevolucion
               ORDER BY dd.IdDetalleDevolucion
               FOR XML PATH(''), TYPE
           ).value('.', 'nvarchar(max)'),1,2,N''),N'(sin detalle guardado)') AS ProductosDevueltos,
       ISNULL((SELECT SUM(dd.SubTotal) FROM dbo.DETALLE_DEVOLUCION_VENTA dd WHERE dd.IdDevolucion=h.IdDevolucion),0) AS ValorProductos,
       h.MontoDevuelto, u.NombreCompleto AS Usuario
FROM dbo.DEVOLUCION_VENTA h
INNER JOIN dbo.VENTA v ON v.IdVenta=h.IdVenta
INNER JOIN dbo.USUARIO u ON u.IdUsuario=h.IdUsuario
ORDER BY h.FechaRegistro DESC, h.IdDevolucion DESC;";
            using (SqlConnection conexion = new SqlConnection(Conexion.cadena))
            using (SqlCommand comando = new SqlCommand(consulta, conexion))
            {
                try
                {
                    using (SqlDataAdapter adaptador = new SqlDataAdapter(comando))
                        adaptador.Fill(tabla);
                }
                catch (Exception ex)
                {
                    mensaje = ex.Message;
                }
            }
            return tabla;
        }

        public bool RegistrarDevolucion(int idVenta, int idUsuario, string tipo, string motivo,
            decimal montoReembolsado, DataTable detalle, out decimal montoDevuelto, out string mensaje)
        {
            montoDevuelto = 0m;
            mensaje = string.Empty;
            try
            {
                using (SqlConnection conexion = new SqlConnection(Conexion.cadena))
                using (SqlCommand comando = new SqlCommand("dbo.usp_RegistrarDevolucionVenta", conexion))
                {
                    comando.CommandType = CommandType.StoredProcedure;
                    comando.Parameters.Add("@IdVenta", SqlDbType.Int).Value = idVenta;
                    comando.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
                    comando.Parameters.Add("@Tipo", SqlDbType.VarChar, 15).Value = tipo;
                    comando.Parameters.Add("@Motivo", SqlDbType.VarChar, 250).Value = motivo.Trim();
                    SqlParameter parametroReembolso = comando.Parameters.Add("@MontoReembolsado", SqlDbType.Decimal);
                    parametroReembolso.Precision = 18;
                    parametroReembolso.Scale = 2;
                    parametroReembolso.Value = montoReembolsado;
                    SqlParameter parametroDetalle = comando.Parameters.AddWithValue("@Detalle", detalle);
                    parametroDetalle.SqlDbType = SqlDbType.Structured;
                    parametroDetalle.TypeName = "dbo.EDetalle_DevolucionVenta";
                    comando.Parameters.Add("@Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                    comando.Parameters.Add("@Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;
                    SqlParameter parametroMonto = comando.Parameters.Add("@MontoDevuelto", SqlDbType.Decimal);
                    parametroMonto.Precision = 18;
                    parametroMonto.Scale = 2;
                    parametroMonto.Direction = ParameterDirection.Output;

                    conexion.Open();
                    comando.ExecuteNonQuery();
                    bool resultado = Convert.ToBoolean(comando.Parameters["@Resultado"].Value);
                    mensaje = Convert.ToString(comando.Parameters["@Mensaje"].Value);
                    if (resultado && parametroMonto.Value != DBNull.Value)
                        montoDevuelto = Convert.ToDecimal(parametroMonto.Value);
                    return resultado;
                }
            }
            catch (Exception ex)
            {
                mensaje = ex.Message;
                return false;
            }
        }
    }
}
