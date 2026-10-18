using CapaDatos;
using System.Data;

namespace CapaNegocio
{
    public class CN_Reposicion
    {
        private readonly CD_Reposicion datos = new CD_Reposicion();

        public DataTable ListarGrupos() { return datos.ListarGrupos(); }
        public void CambiarEstadoGrupo(int id, bool activo) { datos.CambiarEstadoGrupo(id, activo); }
        public DataTable ListarPerfiles() { return datos.ListarPerfiles(); }
        public void CambiarEstadoPerfil(int id, bool activo) { datos.CambiarEstadoPerfil(id, activo); }
        public DataTable ListarProveedores() { return datos.ListarProveedores(); }
        public DataTable ProveedoresDePerfil(int id) { return datos.ProveedoresDePerfil(id); }
        public DataTable ListarProductos() { return datos.ListarProductos(); }
        public DataTable Reporte() { return datos.Reporte(); }
        public int GuardarGrupo(int? id, string nombre, int minimo, int maximo)
        { return datos.GuardarGrupo(id, nombre, minimo, maximo); }
        public int GuardarPerfil(int? id, string nombre, int[] proveedores)
        { return datos.GuardarPerfil(id, nombre, proveedores); }
        public void GuardarConfiguracionProducto(int producto, int? grupo, int? minimo, int? maximo, int? perfil)
        { datos.GuardarConfiguracionProducto(producto, grupo, minimo, maximo, perfil); }
    }
}
