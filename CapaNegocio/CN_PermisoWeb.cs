using System;
using System.Collections.Generic;
using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    public class CN_PermisoWeb
    {
        private readonly CD_PermisoWeb datos = new CD_PermisoWeb();

        public List<PaginaWeb> ListarPaginas() { return datos.ListarPaginas(); }
        public List<Usuario> ListarUsuariosGestionables(int actorId) { return datos.ListarUsuariosGestionables(actorId); }
        public HashSet<string> ListarPaginasAsignadas(int idUsuario) { return datos.ListarPaginasAsignadas(idUsuario); }

        public bool Guardar(int actorId, int idUsuario, IEnumerable<string> paginas, bool? superAdministrador, out string mensaje)
        {
            mensaje = "";
            if (actorId <= 0 || idUsuario <= 0)
            {
                mensaje = "Selecciona un usuario válido.";
                return false;
            }
            return datos.Guardar(actorId, idUsuario, paginas, superAdministrador, out mensaje);
        }
    }
}
