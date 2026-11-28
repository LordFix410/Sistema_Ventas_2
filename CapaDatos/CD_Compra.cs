using CapaEntidad;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CapaDatos
{
    public class CD_Compra
    {
        public int obtenerCorrelativo()
        {
            int idcorrelativo = 0;

            using (SqlConnection oconexion = new SqlConnection(Conexion.cadena))

            {
                try
                {
                    StringBuilder query = new StringBuilder();
                    query.AppendLine("select isnull(max(try_convert(int, NumeroDocumento)), 0) + 1 from COMPRA");

                    SqlCommand cmd = new SqlCommand(query.ToString(), oconexion);
                    cmd.CommandType = CommandType.Text;


                    oconexion.Open();
                    idcorrelativo= Convert.ToInt32(cmd.ExecuteScalar());
                }
                catch (Exception ex)
                {
                    idcorrelativo = 0;
                }
            }

            return idcorrelativo;
        }

        public bool Registrar(Compra obj, DataTable detalleCompra, out string Mensaje)
        {
            bool Respuesta = false;
            Mensaje= string.Empty;

            using (SqlConnection oconexion = new SqlConnection(Conexion.cadena))

            {
                try
                {
                    SqlCommand cmd = new SqlCommand("dbo.sp_RegistrarCompra", oconexion);
                    cmd.Parameters.AddWithValue("IdUsuario",obj.oUsuario.IdUsuario);
                    cmd.Parameters.AddWithValue("IdProveedor", obj.oProveedor.IdProveedor);
                    cmd.Parameters.AddWithValue("TipoDocumento",obj.TipoDocumento);
                    SqlParameter numeroDocumento = cmd.Parameters.Add("NumeroDocumento", SqlDbType.VarChar, 500);
                    numeroDocumento.Direction = ParameterDirection.InputOutput;
                    numeroDocumento.Value = obj.NumeroDocumento;
                    cmd.Parameters.AddWithValue("MontoTotal",obj.MontoTotal);
                    cmd.Parameters.AddWithValue("MontoNoInventariable", obj.MontoNoInventariable);
                    cmd.Parameters.Add("NumeroFacturaProveedor", SqlDbType.VarChar, 100).Value =
                        string.IsNullOrWhiteSpace(obj.NumeroFacturaProveedor) ? (object)DBNull.Value : obj.NumeroFacturaProveedor.Trim();
                    SqlParameter parametroDetalle = cmd.Parameters.AddWithValue("DetalleCompra", detalleCompra);
                    parametroDetalle.SqlDbType = SqlDbType.Structured;
                    parametroDetalle.TypeName = "dbo.EDetalle_Compra";
                    cmd.Parameters.Add("Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;
                   
                    cmd.CommandType = CommandType.StoredProcedure;
                    oconexion.Open();
                    cmd.ExecuteNonQuery();

                   


                    Respuesta = Convert.ToBoolean(cmd.Parameters["Resultado"].Value);
                    Mensaje = cmd.Parameters["Mensaje"].Value.ToString();
                    if (Respuesta) obj.NumeroDocumento = numeroDocumento.Value.ToString();

                }
                catch (Exception ex)
                {
                    Respuesta = false;
                    Mensaje = ex.Message;
                }
            }
            return Respuesta;
        }

        public Compra obtenerCompra(string numero)
        {
            Compra obj = new Compra();


            using (SqlConnection oconexion = new SqlConnection(Conexion.cadena))

            {
                try
                {
                    StringBuilder query = new StringBuilder();
                    query.AppendLine("select c.IdCompra,\r\nu.NombreCompleto,\r\npr.Documento, pr.RazonSocial,\r\nc.TipoDocumento, c.NumeroDocumento, c.MontoTotal, convert(char(10),c.FechaRegistro,103)[FechaRegistro]\r\nfrom COMPRA c\r\ninner join USUARIO u on u.IdUsuario = c.IdUsuario\r\nINNER JOIN proveedor pr on pr.IdProveedor = c.IdProveedor\r\nwhere c.NumeroDocumento = @numero");

                    SqlCommand cmd = new SqlCommand(query.ToString(), oconexion);
                    cmd.Parameters.AddWithValue("@numero", numero);
                    cmd.CommandType = CommandType.Text;

                    oconexion.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            obj = new Compra()
                            {
                                IdCompra = Convert.ToInt32(dr["IdCompra"]),
                                oUsuario = new Usuario() { NombreCompleto = dr["NombreCompleto"].ToString() },
                                oProveedor = new Proveedor() { Documento = dr["Documento"].ToString(), RazonSocial = dr["RazonSocial"].ToString() },
                                TipoDocumento = dr["TipoDocumento"].ToString(),
                                NumeroDocumento = dr["NumeroDocumento"].ToString(),
                                MontoTotal = Convert.ToDecimal(dr["MontoTotal"].ToString()),
                                FechaRegistro = dr["FechaRegistro"].ToString()
                            };

                        }
                    }

                }
                catch (Exception ex)
                {
                    obj = new Compra();
                }

                return obj;
            }


        }

        public Compra ObtenerCompraParaCorreccion(string numero)
        {
            using (SqlConnection conexion = new SqlConnection(Conexion.cadena))
            using (SqlCommand comando = new SqlCommand(@"
SELECT c.IdCompra,c.IdProveedor,c.TipoDocumento,c.NumeroDocumento,
       c.NumeroFacturaProveedor,c.MontoTotal,c.MontoNoInventariable,
       c.Anulada,c.MotivoAnulacion,c.FechaRegistro,pr.RazonSocial
FROM dbo.COMPRA c
INNER JOIN dbo.PROVEEDOR pr ON pr.IdProveedor=c.IdProveedor
WHERE c.NumeroDocumento=@NumeroDocumento;", conexion))
            {
                comando.Parameters.Add("@NumeroDocumento", SqlDbType.VarChar, 50).Value = (numero ?? string.Empty).Trim();
                conexion.Open();
                using (SqlDataReader lector = comando.ExecuteReader())
                {
                    if (!lector.Read()) return null;
                    return new Compra
                    {
                        IdCompra = Convert.ToInt32(lector["IdCompra"]),
                        oProveedor = new Proveedor
                        {
                            IdProveedor = Convert.ToInt32(lector["IdProveedor"]),
                            RazonSocial = Convert.ToString(lector["RazonSocial"])
                        },
                        TipoDocumento = Convert.ToString(lector["TipoDocumento"]),
                        NumeroDocumento = Convert.ToString(lector["NumeroDocumento"]),
                        NumeroFacturaProveedor = lector["NumeroFacturaProveedor"] == DBNull.Value ? string.Empty : Convert.ToString(lector["NumeroFacturaProveedor"]),
                        MontoTotal = Convert.ToDecimal(lector["MontoTotal"]),
                        MontoNoInventariable = Convert.ToDecimal(lector["MontoNoInventariable"]),
                        Anulada = Convert.ToBoolean(lector["Anulada"]),
                        MotivoAnulacion = lector["MotivoAnulacion"] == DBNull.Value ? string.Empty : Convert.ToString(lector["MotivoAnulacion"]),
                        FechaRegistro = Convert.ToDateTime(lector["FechaRegistro"]).ToString("dd/MM/yyyy HH:mm")
                    };
                }
            }
        }

        public List<Detalle_Compra> ObtenerDetalleCompraParaCorreccion(int idCompra)
        {
            List<Detalle_Compra> lista = new List<Detalle_Compra>();
            using (SqlConnection conexion = new SqlConnection(Conexion.cadena))
            using (SqlCommand comando = new SqlCommand(@"
SELECT dc.IdDetalleCompra,dc.IdProducto,p.Codigo,p.Nombre,dc.Cantidad,
       dc.PrecioCompra,dc.PrecioVenta,dc.Porcentaje,dc.MontoTotal
FROM dbo.DETALLE_COMPRA dc
INNER JOIN dbo.PRODUCTO p ON p.IdProducto=dc.IdProducto
WHERE dc.IdCompra=@IdCompra
ORDER BY dc.IdDetalleCompra;", conexion))
            {
                comando.Parameters.Add("@IdCompra", SqlDbType.Int).Value = idCompra;
                conexion.Open();
                using (SqlDataReader lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        lista.Add(new Detalle_Compra
                        {
                            IdDetalleCompra = Convert.ToInt32(lector["IdDetalleCompra"]),
                            IdProducto = Convert.ToInt32(lector["IdProducto"]),
                            oProducto = new Producto
                            {
                                IdProducto = Convert.ToInt32(lector["IdProducto"]),
                                Codigo = Convert.ToString(lector["Codigo"]),
                                Nombre = Convert.ToString(lector["Nombre"])
                            },
                            Cantidad = Convert.ToInt32(lector["Cantidad"]),
                            PrecioCompra = Convert.ToDecimal(lector["PrecioCompra"]),
                            PrecioVenta = Convert.ToDecimal(lector["PrecioVenta"]),
                            Porcentaje = Convert.ToDecimal(lector["Porcentaje"]),
                            MontoTotal = Convert.ToDecimal(lector["MontoTotal"])
                        });
                    }
                }
            }
            return lista;
        }

        public bool CorregirCompra(int idCompra, int idUsuario, int idProveedor, string numeroFacturaProveedor,
            string motivo, DataTable detalle, out string mensaje)
        {
            return EjecutarCorreccionCompra("dbo.sp_CorregirCompra", idCompra, idUsuario, idProveedor,
                numeroFacturaProveedor, motivo, detalle, out mensaje);
        }

        public bool AnularCompra(int idCompra, int idUsuario, string motivo, out string mensaje)
        {
            return EjecutarCorreccionCompra("dbo.sp_AnularCompra", idCompra, idUsuario, 0,
                null, motivo, null, out mensaje);
        }

        public bool ReactivarCompra(int idCompra, int idUsuario, string motivo, out string mensaje)
        {
            return EjecutarCorreccionCompra("dbo.sp_ReactivarCompra", idCompra, idUsuario, 0,
                null, motivo, null, out mensaje);
        }

        public DataTable ObtenerHistorialAnulaciones()
        {
            DataTable historial = new DataTable();
            using (SqlConnection conexion = new SqlConnection(Conexion.cadena))
            using (SqlCommand comando = new SqlCommand(@"
SELECT h.IdCorreccion,h.IdCompra,h.Fecha,h.Operacion,h.Compra,h.FacturaProveedor,
       h.Proveedor,h.Total,h.Usuario,h.Motivo
FROM
(
    SELECT cc.IdCorreccion,cc.IdCompra,cc.FechaRegistro AS Fecha,
           CASE cc.Tipo WHEN 'ANULACION' THEN 'Anulación' ELSE 'Restauración' END AS Operacion,
           c.NumeroDocumento AS Compra,
           COALESCE(NULLIF(cc.NumeroFacturaAnterior,''),'—') AS FacturaProveedor,
           COALESCE(pr.RazonSocial,'Proveedor no disponible') AS Proveedor,
           cc.MontoTotalAnterior AS Total,u.NombreCompleto AS Usuario,cc.Motivo
    FROM dbo.COMPRA_CORRECCION cc
    INNER JOIN dbo.COMPRA c ON c.IdCompra=cc.IdCompra
    LEFT JOIN dbo.PROVEEDOR pr ON pr.IdProveedor=cc.IdProveedorAnterior
    INNER JOIN dbo.USUARIO u ON u.IdUsuario=cc.IdUsuario
    WHERE cc.Tipo IN ('ANULACION','REACTIVACION')

    UNION ALL

    SELECT -c.IdCompra,c.IdCompra,COALESCE(c.FechaAnulacion,c.FechaRegistro),
           'Anulación',c.NumeroDocumento,COALESCE(NULLIF(c.NumeroFacturaProveedor,''),'—'),
           COALESCE(pr.RazonSocial,'Proveedor no disponible'),c.MontoTotal,
           COALESCE(u.NombreCompleto,'Usuario no disponible'),
           COALESCE(NULLIF(c.MotivoAnulacion,''),'Anulación anterior sin motivo registrado')
    FROM dbo.COMPRA c
    LEFT JOIN dbo.PROVEEDOR pr ON pr.IdProveedor=c.IdProveedor
    LEFT JOIN dbo.USUARIO u ON u.IdUsuario=c.IdUsuarioAnulacion
    WHERE c.Anulada=1
      AND NOT EXISTS(SELECT 1 FROM dbo.COMPRA_CORRECCION cc WHERE cc.IdCompra=c.IdCompra AND cc.Tipo='ANULACION')
) h
ORDER BY h.Fecha DESC,h.IdCorreccion DESC;", conexion))
            using (SqlDataAdapter adaptador = new SqlDataAdapter(comando))
            {
                adaptador.Fill(historial);
            }
            return historial;
        }

        public DataTable ObtenerDetalleHistorialAnulacion(int idCorreccion)
        {
            DataTable detalle = new DataTable();
            if (idCorreccion < 0)
            {
                using (SqlConnection conexion = new SqlConnection(Conexion.cadena))
                using (SqlCommand comando = new SqlCommand(@"
SELECT COALESCE(p.Codigo,'') AS Codigo,COALESCE(p.Nombre,'Producto no disponible') AS Producto,
       dc.Cantidad AS CantidadAnterior,0 AS CantidadNueva,
       dc.PrecioCompra AS CostoAnterior,dc.PrecioCompra AS CostoNuevo,
       dc.MontoTotal AS TotalAnterior,0 AS TotalNuevo
FROM dbo.DETALLE_COMPRA dc
LEFT JOIN dbo.PRODUCTO p ON p.IdProducto=dc.IdProducto
WHERE dc.IdCompra=@IdCompra
ORDER BY dc.IdDetalleCompra;", conexion))
                using (SqlDataAdapter adaptador = new SqlDataAdapter(comando))
                {
                    comando.Parameters.Add("@IdCompra", SqlDbType.Int).Value = -idCorreccion;
                    adaptador.Fill(detalle);
                }
                return detalle;
            }

            using (SqlConnection conexion = new SqlConnection(Conexion.cadena))
            using (SqlCommand comando = new SqlCommand(@"
SELECT COALESCE(p.Codigo,'') AS Codigo,
       COALESCE(p.Nombre,'Producto no disponible') AS Producto,
       d.CantidadAnterior,d.CantidadNueva,
       d.PrecioCompraAnterior AS CostoAnterior,d.PrecioCompraNuevo AS CostoNuevo,
       d.MontoAnterior AS TotalAnterior,d.MontoNuevo AS TotalNuevo
FROM dbo.COMPRA_CORRECCION_DETALLE d
INNER JOIN dbo.COMPRA_CORRECCION cc ON cc.IdCorreccion=d.IdCorreccion
LEFT JOIN dbo.PRODUCTO p ON p.IdProducto=d.IdProductoAnterior
WHERE d.IdCorreccion=@IdCorreccion AND cc.Tipo IN ('ANULACION','REACTIVACION')
ORDER BY d.IdAuditoriaDetalle;", conexion))
            using (SqlDataAdapter adaptador = new SqlDataAdapter(comando))
            {
                comando.Parameters.Add("@IdCorreccion", SqlDbType.Int).Value = idCorreccion;
                adaptador.Fill(detalle);
            }
            return detalle;
        }

        private bool EjecutarCorreccionCompra(string procedimiento, int idCompra, int idUsuario, int idProveedor,
            string numeroFacturaProveedor, string motivo, DataTable detalle, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                using (SqlConnection conexion = new SqlConnection(Conexion.cadena))
                using (SqlCommand comando = new SqlCommand(procedimiento, conexion))
                {
                    comando.CommandType = CommandType.StoredProcedure;
                    comando.Parameters.Add("@IdCompra", SqlDbType.Int).Value = idCompra;
                    comando.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
                    comando.Parameters.Add("@Motivo", SqlDbType.VarChar, 500).Value = (motivo ?? string.Empty).Trim();
                    if (procedimiento.EndsWith("sp_CorregirCompra", StringComparison.OrdinalIgnoreCase))
                    {
                        comando.Parameters.Add("@IdProveedorNuevo", SqlDbType.Int).Value = idProveedor;
                        comando.Parameters.Add("@NumeroFacturaProveedorNuevo", SqlDbType.VarChar, 100).Value =
                            string.IsNullOrWhiteSpace(numeroFacturaProveedor) ? (object)DBNull.Value : numeroFacturaProveedor.Trim();
                        SqlParameter parametro = comando.Parameters.Add("@Detalle", SqlDbType.Structured);
                        parametro.TypeName = "dbo.ECompra_Correccion";
                        parametro.Value = detalle;
                    }
                    comando.Parameters.Add("@Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                    comando.Parameters.Add("@Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;
                    conexion.Open();
                    comando.ExecuteNonQuery();
                    mensaje = Convert.ToString(comando.Parameters["@Mensaje"].Value);
                    return Convert.ToBoolean(comando.Parameters["@Resultado"].Value);
                }
            }
            catch (Exception ex)
            {
                mensaje = ex.Message;
                return false;
            }
        }
        public List<Detalle_Compra> ObtenerDetalleCompra(int idcompra)
        {
            List<Detalle_Compra> oLista = new List<Detalle_Compra>();
            try
            {
                using (SqlConnection conexion = new SqlConnection(Conexion.cadena))
                {
                    conexion.Open();
                    StringBuilder query = new StringBuilder();
                    query.AppendLine("select p.Nombre,dc.PrecioCompra,dc.Porcentaje,dc.Cantidad,dc.MontoTotal from DETALLE_COMPRA dc");
                    query.AppendLine("inner join PRODUCTO p on p.IdProducto = dc.IdProducto");
                    query.AppendLine("where dc.IdCompra = @idcompra");

                    SqlCommand cmd = new SqlCommand(query.ToString(), conexion);
                    cmd.Parameters.AddWithValue("@idcompra", idcompra);
                    cmd.CommandType = System.Data.CommandType.Text;

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            oLista.Add(new Detalle_Compra()
                            {
                                oProducto = new Producto() { Nombre = dr["Nombre"].ToString() },
                                PrecioCompra = Convert.ToDecimal(dr["PrecioCompra"].ToString()),
                                Porcentaje = Convert.ToDecimal(dr["Porcentaje"].ToString()),
                                Cantidad = Convert.ToInt32(dr["Cantidad"].ToString()),
                                MontoTotal = Convert.ToDecimal(dr["MontoTotal"].ToString())

                            });
                        }
                    }
                }
            } catch (Exception ex) {
                oLista = new List<Detalle_Compra>();
            }
            return oLista;
        }


    }
}
