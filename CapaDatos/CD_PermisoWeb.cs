using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using CapaEntidad;

namespace CapaDatos
{
    public class CD_PermisoWeb
    {
        public List<PaginaWeb> ListarPaginas()
        {
            var pages = new List<PaginaWeb>();
            using (var connection = new SqlConnection(Conexion.cadena))
            using (var command = new SqlCommand(@"SELECT CodigoPagina, Nombre, Orden
FROM dbo.PAGINA_WEB WHERE Activa=1 ORDER BY Orden, Nombre;", connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                    while (reader.Read()) pages.Add(new PaginaWeb { Codigo = reader.GetString(0), Nombre = reader.GetString(1), Orden = reader.GetInt32(2) });
            }
            return pages;
        }

        public List<Usuario> ListarUsuariosGestionables(int actorId)
        {
            var users = new List<Usuario>();
            using (var connection = new SqlConnection(Conexion.cadena))
            using (var command = new SqlCommand(@"
DECLARE @principal bit,@gestiona bit;
SELECT @principal=EsAdministradorPrincipal,@gestiona=CASE WHEN EsAdministradorPrincipal=1 OR EsSuperAdministradorWeb=1 THEN 1 ELSE 0 END
FROM dbo.USUARIO WHERE IdUsuario=@actor AND Estado=1;
IF ISNULL(@gestiona,0)=0 THROW 51101,'No tienes permiso para administrar accesos web.',1;
SELECT u.IdUsuario,u.Documento,u.NombreCompleto,u.Estado,u.EsAdministradorPrincipal,u.EsSuperAdministradorWeb,r.IdRol,r.Descripcion
FROM dbo.USUARIO u INNER JOIN dbo.ROL r ON r.IdRol=u.IdRol
WHERE u.Estado=1 AND (@principal=1
   OR u.IdUsuario=@actor
   OR (u.EsAdministradorPrincipal=0 AND u.EsSuperAdministradorWeb=0 AND UPPER(ISNULL(r.Descripcion,''))<>'ADMINISTRADOR'))
ORDER BY u.NombreCompleto,u.IdUsuario;", connection))
            {
                command.Parameters.Add("@actor", SqlDbType.Int).Value = actorId;
                connection.Open();
                using (var reader = command.ExecuteReader())
                    while (reader.Read()) users.Add(new Usuario
                    {
                        IdUsuario = reader.GetInt32(0), Documento = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        NombreCompleto = reader.IsDBNull(2) ? "" : reader.GetString(2), Estado = reader.GetBoolean(3),
                        EsAdministradorPrincipal = reader.GetBoolean(4), EsSuperAdministradorWeb = reader.GetBoolean(5),
                        oRol = new Rol { IdRol = reader.GetInt32(6), Descripcion = reader.IsDBNull(7) ? "" : reader.GetString(7) }
                    });
            }
            return users;
        }

        public HashSet<string> ListarPaginasAsignadas(int idUsuario)
        {
            var pages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var connection = new SqlConnection(Conexion.cadena))
            using (var command = new SqlCommand("SELECT CodigoPagina FROM dbo.PERMISO_PAGINA_WEB WHERE IdUsuario=@usuario;", connection))
            {
                command.Parameters.Add("@usuario", SqlDbType.Int).Value = idUsuario;
                connection.Open();
                using (var reader = command.ExecuteReader()) while (reader.Read()) pages.Add(reader.GetString(0));
            }
            return pages;
        }

        public bool Guardar(int actorId, int idUsuario, IEnumerable<string> paginas, bool? superAdministrador, out string mensaje)
        {
            mensaje = "";
            var pages = new HashSet<string>(paginas ?? new string[0], StringComparer.OrdinalIgnoreCase);
            using (var connection = new SqlConnection(Conexion.cadena))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        bool actorPrincipal, objetivoPrincipal, objetivoSuper;
                        using (var command = new SqlCommand(@"
DECLARE @actorPrincipal bit,@actorOk bit=0,@objetivoPrincipal bit,@objetivoSuper bit,@objetivoRol int;
SELECT @actorPrincipal=EsAdministradorPrincipal,@actorOk=CASE WHEN Estado=1 AND (EsAdministradorPrincipal=1 OR EsSuperAdministradorWeb=1) THEN 1 ELSE 0 END
FROM dbo.USUARIO WITH(UPDLOCK,HOLDLOCK) WHERE IdUsuario=@actor;
SELECT @objetivoPrincipal=EsAdministradorPrincipal,@objetivoSuper=EsSuperAdministradorWeb,@objetivoRol=IdRol
FROM dbo.USUARIO WITH(UPDLOCK,HOLDLOCK) WHERE IdUsuario=@objetivo AND Estado=1;
IF ISNULL(@actorOk,0)=0 THROW 51101,'No tienes permiso para administrar accesos web.',1;
IF @objetivoRol IS NULL THROW 51102,'El usuario seleccionado no existe o está inactivo.',1;
IF @actorPrincipal=0 AND @objetivo<>@actor AND
   (@objetivoPrincipal=1 OR @objetivoSuper=1 OR EXISTS(SELECT 1 FROM dbo.ROL WHERE IdRol=@objetivoRol AND UPPER(Descripcion)='ADMINISTRADOR'))
   THROW 51103,'Solo el administrador principal puede modificar otro administrador.',1;
IF @objetivoPrincipal=1 AND @actorPrincipal=0 THROW 51103,'No puedes modificar al administrador principal.',1;
SELECT @actorPrincipal,@objetivoPrincipal,@objetivoSuper;", connection, transaction))
                        {
                            command.Parameters.Add("@actor", SqlDbType.Int).Value = actorId;
                            command.Parameters.Add("@objetivo", SqlDbType.Int).Value = idUsuario;
                            using (var reader = command.ExecuteReader())
                            {
                                if (!reader.Read()) throw new InvalidOperationException("No se pudo validar el usuario.");
                                actorPrincipal = reader.GetBoolean(0); objetivoPrincipal = reader.GetBoolean(1); objetivoSuper = reader.GetBoolean(2);
                            }
                        }
                        if (!actorPrincipal && superAdministrador.HasValue)
                            throw new InvalidOperationException("Solo el administrador principal puede nombrar superadministradores.");
                        if (objetivoPrincipal && superAdministrador.HasValue)
                            throw new InvalidOperationException("La cuenta principal no puede cambiar su nivel de protección.");

                        var validPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        using (var command = new SqlCommand("SELECT CodigoPagina FROM dbo.PAGINA_WEB WHERE Activa=1;", connection, transaction))
                        using (var reader = command.ExecuteReader()) while (reader.Read()) validPages.Add(reader.GetString(0));
                        if (!pages.IsSubsetOf(validPages)) throw new InvalidOperationException("La selección contiene una página desconocida.");

                        using (var command = new SqlCommand("DELETE FROM dbo.PERMISO_PAGINA_WEB WHERE IdUsuario=@usuario;", connection, transaction))
                        { command.Parameters.Add("@usuario", SqlDbType.Int).Value = idUsuario; command.ExecuteNonQuery(); }
                        foreach (var code in pages)
                        {
                            using (var command = new SqlCommand("INSERT dbo.PERMISO_PAGINA_WEB(IdUsuario,CodigoPagina) VALUES(@usuario,@pagina);", connection, transaction))
                            { command.Parameters.Add("@usuario", SqlDbType.Int).Value = idUsuario; command.Parameters.Add("@pagina", SqlDbType.VarChar, 100).Value = code; command.ExecuteNonQuery(); }
                        }
                        if (actorPrincipal && !objetivoPrincipal && superAdministrador.HasValue)
                        {
                            using (var command = new SqlCommand("UPDATE dbo.USUARIO SET EsSuperAdministradorWeb=@super WHERE IdUsuario=@usuario AND EsAdministradorPrincipal=0;", connection, transaction))
                            { command.Parameters.Add("@super", SqlDbType.Bit).Value = superAdministrador.Value; command.Parameters.Add("@usuario", SqlDbType.Int).Value = idUsuario; command.ExecuteNonQuery(); }
                        }
                        transaction.Commit(); mensaje = "Los accesos web se guardaron correctamente."; return true;
                    }
                    catch (Exception ex)
                    {
                        try { transaction.Rollback(); } catch { }
                        mensaje = ex.Message; return false;
                    }
                }
            }
        }
    }
}
