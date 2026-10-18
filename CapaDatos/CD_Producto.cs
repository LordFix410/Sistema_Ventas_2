using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Reflection;

namespace CapaDatos
{
    public class CD_Producto
    {
        private static void AgregarParametroPorcentaje(SqlCommand comando, decimal porcentaje)
        {
            SqlParameter parametro = comando.Parameters.Add("Porcentaje", SqlDbType.Decimal);
            parametro.Precision = 9;
            parametro.Scale = 4;
            parametro.Value = porcentaje;
        }

        public int RegistrarTemporal(Producto obj, int cantidad, out string Mensaje)
        {
            int idProducto = 0;
            Mensaje = string.Empty;

            using (SqlConnection conexion = new SqlConnection(Conexion.cadena))
            {
                SqlTransaction transaccion = null;
                try
                {
                    conexion.Open();
                    transaccion = conexion.BeginTransaction();

                    int idCategoria;
                    using (SqlCommand buscarCategoria = new SqlCommand(
                        "SELECT TOP (1) IdCategoria FROM CATEGORIA WHERE Estado = 1 " +
                        "ORDER BY CASE WHEN UPPER(Descripcion) IN ('VARIOS','OTROS','MISCELANEOS') THEN 0 ELSE 1 END, IdCategoria",
                        conexion, transaccion))
                    {
                        object categoria = buscarCategoria.ExecuteScalar();
                        if (categoria == null || categoria == DBNull.Value)
                            throw new InvalidOperationException("No hay una categoría activa para registrar el artículo temporal.");
                        idCategoria = Convert.ToInt32(categoria);
                    }

                    using (SqlCommand comando = new SqlCommand("sp_RegistrarProducto", conexion, transaccion))
                    {
                        comando.CommandType = CommandType.StoredProcedure;
                        comando.Parameters.Add("Codigo", SqlDbType.VarChar, 20).Value = obj.Codigo;
                        comando.Parameters.Add("Nombre", SqlDbType.VarChar, 30).Value = obj.Nombre;
                        comando.Parameters.Add("Descripcion", SqlDbType.VarChar, 30).Value = obj.Descripcion;
                        comando.Parameters.Add("IdCategoria", SqlDbType.Int).Value = idCategoria;
                        AgregarParametroPorcentaje(comando, obj.Porcentaje);
                        comando.Parameters.Add("Estado", SqlDbType.Bit).Value = false;
                        comando.Parameters.Add("Resultado", SqlDbType.Int).Direction = ParameterDirection.Output;
                        comando.Parameters.Add("Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;
                        comando.ExecuteNonQuery();
                        idProducto = Convert.ToInt32(comando.Parameters["Resultado"].Value);
                        Mensaje = Convert.ToString(comando.Parameters["Mensaje"].Value);
                    }

                    if (idProducto <= 0)
                        throw new InvalidOperationException(string.IsNullOrWhiteSpace(Mensaje)
                            ? "No se pudo crear el artículo temporal."
                            : Mensaje);

                    using (SqlCommand actualizarStock = new SqlCommand(
                        "UPDATE PRODUCTO SET Stock = @Stock, Estado = 0 WHERE IdProducto = @IdProducto",
                        conexion, transaccion))
                    {
                        actualizarStock.Parameters.Add("@Stock", SqlDbType.Int).Value = cantidad;
                        actualizarStock.Parameters.Add("@IdProducto", SqlDbType.Int).Value = idProducto;
                        if (actualizarStock.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("No se pudo preparar el artículo temporal para la venta.");
                    }

                    transaccion.Commit();
                    return idProducto;
                }
                catch (Exception ex)
                {
                    if (transaccion != null)
                    {
                        try { transaccion.Rollback(); }
                        catch { }
                    }
                    Mensaje = ex.Message;
                    return 0;
                }
            }
        }

        public void EliminarTemporalNoVendido(int idProducto)
        {
            try
            {
                using (SqlConnection conexion = new SqlConnection(Conexion.cadena))
                using (SqlCommand comando = new SqlCommand(
                    "DELETE FROM PRODUCTO WHERE IdProducto = @IdProducto AND Estado = 0 AND Codigo LIKE 'T%' " +
                    "AND NOT EXISTS (SELECT 1 FROM DETALLE_VENTA WHERE IdProducto = @IdProducto) " +
                    "AND NOT EXISTS (SELECT 1 FROM DETALLE_COMPRA WHERE IdProducto = @IdProducto)", conexion))
                {
                    comando.Parameters.Add("@IdProducto", SqlDbType.Int).Value = idProducto;
                    conexion.Open();
                    comando.ExecuteNonQuery();
                }
            }
            catch
            {
                // Si falla la compensación, el producto permanece inactivo y no se usa en búsquedas.
            }
        }

        public List<Producto> Listar()
        {
            List<Producto> lista = new List<Producto>();

            using (SqlConnection oconexion = new SqlConnection(Conexion.cadena))

            {
                try
                {
                    

                    StringBuilder query = new StringBuilder();
                    query.AppendLine("select p.IdProducto, p.Codigo, p.Nombre, p.Descripcion, c.IdCategoria, c.DESCRIPCION[DescripcionCategoria], p.Stock, p.PrecioCompra, p.PrecioVenta, p.Porcentaje, p.Estado, p.IdGrupoReposicion, COALESCE(p.CantidadMinima, gr.CantidadMinima) CantidadMinima, COALESCE(p.CantidadMaxima, gr.CantidadMaxima) CantidadMaxima, p.IdGrupoProveedor, ISNULL(gr.Nombre,'') GrupoReposicion, ISNULL(gp.Nombre,'') GrupoProveedores from PRODUCTO p");
                    query.AppendLine("inner join CATEGORIA c on c.IdCategoria = p.IdCategoria");
                    query.AppendLine("left join dbo.GRUPO_REPOSICION gr on gr.IdGrupoReposicion=p.IdGrupoReposicion left join dbo.GRUPO_PROVEEDOR gp on gp.IdGrupoProveedor=p.IdGrupoProveedor");

                    SqlCommand cmd = new SqlCommand(query.ToString(), oconexion);
                    cmd.CommandType = CommandType.Text;

                    oconexion.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new Producto()
                            {
                                IdProducto = Convert.ToInt32(dr["IdProducto"]),
                                Codigo = dr["Codigo"].ToString(),
                                Nombre = dr["Nombre"].ToString(),
                                Descripcion = dr["Descripcion"].ToString(),
                                oCategoria = new Categoria() { IdCategoria = Convert.ToInt32(dr["IdCategoria"]), Descripcion = dr["DescripcionCategoria"].ToString() },
                                Stock = Convert.ToInt32( dr["Stock"].ToString()),
                                PrecioCompra = Convert.ToDecimal(dr["PrecioCompra"].ToString()),
                                PrecioVenta = Convert.ToDecimal(dr["PrecioVenta"].ToString()),
                                Porcentaje = Convert.ToDecimal(dr["Porcentaje"]),
                                Estado = Convert.ToBoolean(dr["Estado"]),
                                IdGrupoReposicion = dr["IdGrupoReposicion"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["IdGrupoReposicion"]),
                                CantidadMinima = dr["CantidadMinima"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["CantidadMinima"]),
                                CantidadMaxima = dr["CantidadMaxima"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["CantidadMaxima"]),
                                IdGrupoProveedor = dr["IdGrupoProveedor"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["IdGrupoProveedor"]),
                                GrupoReposicion = dr["GrupoReposicion"].ToString(), GrupoProveedores = dr["GrupoProveedores"].ToString()
                            });
                        }
                    }

                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("No se pudieron cargar los productos. Verifica que hayas ejecutado migracion_reposicion_cotizaciones.sql en DBSISTEMA_VENTA.", ex);
                }
            }
            return lista;
        }

        //-----------Registrar---------

        public int Registrar(Producto obj, out string Mensaje)
        {
            int idProductoGenerado = 0;
            Mensaje = string.Empty;

            try
            {
                using (SqlConnection oconexion = new SqlConnection(Conexion.cadena))
                {
                    SqlCommand cmd = new SqlCommand("sp_RegistrarProducto", oconexion);
                    //cmd.Parameters.AddWithValue("IdProducto", obj.IdProducto);
                    cmd.Parameters.AddWithValue("Codigo", obj.Codigo);
                    cmd.Parameters.AddWithValue("Nombre", obj.Nombre);
                    cmd.Parameters.AddWithValue("Descripcion", obj.Descripcion);
                    cmd.Parameters.AddWithValue("IdCategoria", obj.oCategoria.IdCategoria);
                    AgregarParametroPorcentaje(cmd, obj.Porcentaje);
                    AgregarParametrosReposicion(cmd, obj);
                    cmd.Parameters.AddWithValue("Estado", obj.Estado);
                    cmd.Parameters.Add("Resultado", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;
                    cmd.CommandType = CommandType.StoredProcedure;

                    oconexion.Open();

                    cmd.ExecuteNonQuery();
                    idProductoGenerado = Convert.ToInt32(cmd.Parameters["Resultado"].Value);
                    Mensaje = cmd.Parameters["Mensaje"].Value.ToString();
                }
            }
            catch (Exception ex)
            {
                idProductoGenerado = 0;
                Mensaje = ex.Message;
            }
            return idProductoGenerado;
        }

        //------------Editar------------

        public bool Editar(Producto obj, out string Mensaje)
        {
            bool respuesta = false;
            Mensaje = string.Empty;

            try
            {
                using (SqlConnection oconexion = new SqlConnection(Conexion.cadena))
                {
                    SqlCommand cmd = new SqlCommand("sp_ModificarProducto", oconexion);
                    cmd.Parameters.AddWithValue("IdProducto", obj.IdProducto);
                    cmd.Parameters.AddWithValue("Codigo", obj.Codigo);
                    cmd.Parameters.AddWithValue("Nombre", obj.Nombre);
                    cmd.Parameters.AddWithValue("Descripcion", obj.Descripcion);
                    cmd.Parameters.AddWithValue("IdCategoria", obj.oCategoria.IdCategoria);
                    AgregarParametroPorcentaje(cmd, obj.Porcentaje);
                    AgregarParametrosReposicion(cmd, obj);
                    cmd.Parameters.AddWithValue("Estado", obj.Estado);
                    cmd.Parameters.Add("Resultado", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;
                    cmd.CommandType = CommandType.StoredProcedure;

                    oconexion.Open();
                    cmd.ExecuteNonQuery();
                    respuesta = Convert.ToBoolean(cmd.Parameters["Resultado"].Value);
                    Mensaje = cmd.Parameters["Mensaje"].Value.ToString();
                }
            }
            catch (Exception ex)
            {
                respuesta = false;
                Mensaje = ex.Message;
            }
            return respuesta;
        }

        private static void AgregarParametrosReposicion(SqlCommand cmd, Producto obj)
        {
            cmd.Parameters.Add("IdGrupoReposicion", SqlDbType.Int).Value = (object)obj.IdGrupoReposicion ?? DBNull.Value;
            cmd.Parameters.Add("CantidadMinima", SqlDbType.Int).Value = (object)obj.CantidadMinima ?? DBNull.Value;
            cmd.Parameters.Add("CantidadMaxima", SqlDbType.Int).Value = (object)obj.CantidadMaxima ?? DBNull.Value;
            cmd.Parameters.Add("IdGrupoProveedor", SqlDbType.Int).Value = (object)obj.IdGrupoProveedor ?? DBNull.Value;
        }

        //------------Eliminar---------

        public bool Eliminar(Producto obj, out string Mensaje)
        {
            bool respuesta = false;
            Mensaje = string.Empty;

            try
            {
                using (SqlConnection oconexion = new SqlConnection(Conexion.cadena))
                {
                    SqlCommand cmd = new SqlCommand("sp_EliminarProducto", oconexion);
                    cmd.Parameters.AddWithValue("IdProducto", obj.IdProducto);
                    cmd.Parameters.Add("Respuesta", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;
                    cmd.CommandType = CommandType.StoredProcedure;

                    oconexion.Open();
                    cmd.ExecuteNonQuery();
                    respuesta = Convert.ToBoolean(cmd.Parameters["Respuesta"].Value);
                    Mensaje = cmd.Parameters["Mensaje"].Value.ToString();
                }
            }
            catch (Exception ex)
            {
                respuesta = false;
                Mensaje = ex.Message;
            }
            return respuesta;
        }
    }
}
