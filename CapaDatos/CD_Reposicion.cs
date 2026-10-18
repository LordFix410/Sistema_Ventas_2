using System;
using System.Data;
using System.Data.SqlClient;

namespace CapaDatos
{
    public class CD_Reposicion
    {
        public DataTable ListarGrupos()
        {
            return Consultar(@"SELECT IdGrupoReposicion, Nombre, CantidadMinima, CantidadMaxima, Activo
FROM dbo.GRUPO_REPOSICION ORDER BY Nombre;");
        }

        public DataTable ListarPerfiles()
        {
            return Consultar(@"SELECT IdGrupoProveedor, Nombre, Activo
FROM dbo.GRUPO_PROVEEDOR ORDER BY Nombre;");
        }

        public DataTable ListarProveedores()
        {
            return Consultar(@"SELECT IdProveedor, RazonSocial FROM dbo.PROVEEDOR
WHERE Estado = 1 ORDER BY RazonSocial;");
        }

        public DataTable ProveedoresDePerfil(int idPerfil)
        {
            using (SqlConnection cn = new SqlConnection(Conexion.cadena))
            using (SqlCommand cmd = new SqlCommand(@"SELECT IdProveedor FROM dbo.GRUPO_PROVEEDOR_DETALLE
WHERE IdGrupoProveedor = @id;", cn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = idPerfil;
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    DataTable tabla = new DataTable();
                    da.Fill(tabla);
                    return tabla;
                }
            }
        }

        public DataTable ListarProductos()
        {
            return Consultar(@"SELECT p.IdProducto, p.Codigo, p.Nombre, p.Stock,
       p.IdGrupoReposicion, p.CantidadMinima, p.CantidadMaxima,
       p.IdGrupoProveedor, ISNULL(gr.Nombre, '') AS GrupoReposicion,
       ISNULL(gp.Nombre, '') AS GrupoProveedores,
       ISNULL(c.Descripcion, '') AS Categoria
FROM dbo.PRODUCTO p
LEFT JOIN dbo.GRUPO_REPOSICION gr ON gr.IdGrupoReposicion = p.IdGrupoReposicion
LEFT JOIN dbo.GRUPO_PROVEEDOR gp ON gp.IdGrupoProveedor = p.IdGrupoProveedor
LEFT JOIN dbo.CATEGORIA c ON c.IdCategoria = p.IdCategoria
WHERE p.Estado = 1 ORDER BY p.Nombre;");
        }

        public DataTable Reporte()
        {
            using (SqlConnection cn = new SqlConnection(Conexion.cadena))
            using (SqlCommand cmd = new SqlCommand("dbo.sp_ReporteReposicionPorProveedor", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    DataTable tabla = new DataTable();
                    da.Fill(tabla);
                    return tabla;
                }
            }
        }

        public int GuardarGrupo(int? id, string nombre, int minimo, int maximo)
        {
            using (SqlConnection cn = new SqlConnection(Conexion.cadena))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = cn;
                cmd.CommandText = id.HasValue
                    ? @"UPDATE dbo.GRUPO_REPOSICION SET Nombre=@nombre, CantidadMinima=@minimo,
CantidadMaxima=@maximo WHERE IdGrupoReposicion=@id; SELECT @id;"
                    : @"INSERT dbo.GRUPO_REPOSICION(Nombre,CantidadMinima,CantidadMaxima)
VALUES(@nombre,@minimo,@maximo); SELECT CONVERT(int,SCOPE_IDENTITY());";
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = (object)id ?? DBNull.Value;
                cmd.Parameters.Add("@nombre", SqlDbType.VarChar, 100).Value = nombre.Trim();
                cmd.Parameters.Add("@minimo", SqlDbType.Int).Value = minimo;
                cmd.Parameters.Add("@maximo", SqlDbType.Int).Value = maximo;
                cn.Open();
                object resultado = cmd.ExecuteScalar();
                if (resultado == null || resultado == DBNull.Value)
                    throw new InvalidOperationException("No se encontró el grupo que se desea actualizar.");
                return Convert.ToInt32(resultado);
            }
        }

        public int GuardarPerfil(int? id, string nombre, int[] proveedores)
        {
            if (proveedores == null || proveedores.Length == 0)
                throw new InvalidOperationException("Selecciona al menos un proveedor para el perfil.");

            using (SqlConnection cn = new SqlConnection(Conexion.cadena))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction())
                {
                    try
                    {
                        int idPerfil;
                        using (SqlCommand cmd = new SqlCommand())
                        {
                            cmd.Connection = cn;
                            cmd.Transaction = tx;
                            cmd.CommandText = id.HasValue
                                ? @"UPDATE dbo.GRUPO_PROVEEDOR SET Nombre=@nombre WHERE IdGrupoProveedor=@id;
IF @@ROWCOUNT=0 THROW 50010, 'No se encontró el perfil de proveedores.', 1;
SELECT @id;"
                                : @"INSERT dbo.GRUPO_PROVEEDOR(Nombre) VALUES(@nombre);
SELECT CONVERT(int,SCOPE_IDENTITY());";
                            cmd.Parameters.Add("@id", SqlDbType.Int).Value = (object)id ?? DBNull.Value;
                            cmd.Parameters.Add("@nombre", SqlDbType.VarChar, 100).Value = nombre.Trim();
                            idPerfil = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        using (SqlCommand borrar = new SqlCommand(
                            "DELETE dbo.GRUPO_PROVEEDOR_DETALLE WHERE IdGrupoProveedor=@id;", cn, tx))
                        {
                            borrar.Parameters.Add("@id", SqlDbType.Int).Value = idPerfil;
                            borrar.ExecuteNonQuery();
                        }

                        foreach (int idProveedor in proveedores)
                        {
                            using (SqlCommand agregar = new SqlCommand(@"INSERT dbo.GRUPO_PROVEEDOR_DETALLE(IdGrupoProveedor,IdProveedor)
VALUES(@grupo,@proveedor);", cn, tx))
                            {
                                agregar.Parameters.Add("@grupo", SqlDbType.Int).Value = idPerfil;
                                agregar.Parameters.Add("@proveedor", SqlDbType.Int).Value = idProveedor;
                                agregar.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                        return idPerfil;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public void GuardarConfiguracionProducto(int idProducto, int? idGrupoReposicion,
            int? minimo, int? maximo, int? idGrupoProveedor)
        {
            using (SqlConnection cn = new SqlConnection(Conexion.cadena))
            using (SqlCommand cmd = new SqlCommand(@"UPDATE dbo.PRODUCTO
SET IdGrupoReposicion=@grupoReposicion,
    CantidadMinima=@minimo,
    CantidadMaxima=@maximo,
    IdGrupoProveedor=@grupoProveedor
WHERE IdProducto=@producto AND Estado=1;", cn))
            {
                cmd.Parameters.Add("@grupoReposicion", SqlDbType.Int).Value = (object)idGrupoReposicion ?? DBNull.Value;
                cmd.Parameters.Add("@minimo", SqlDbType.Int).Value = (object)minimo ?? DBNull.Value;
                cmd.Parameters.Add("@maximo", SqlDbType.Int).Value = (object)maximo ?? DBNull.Value;
                cmd.Parameters.Add("@grupoProveedor", SqlDbType.Int).Value = (object)idGrupoProveedor ?? DBNull.Value;
                cmd.Parameters.Add("@producto", SqlDbType.Int).Value = idProducto;
                cn.Open();
                if (cmd.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("No se encontró el producto que deseas configurar.");
            }
        }

        private DataTable Consultar(string sql)
        {
            using (SqlConnection cn = new SqlConnection(Conexion.cadena))
            using (SqlDataAdapter da = new SqlDataAdapter(sql, cn))
            {
                DataTable tabla = new DataTable();
                da.Fill(tabla);
                return tabla;
            }
        }
    }
}
