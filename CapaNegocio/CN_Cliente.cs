using CapaDatos;
using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    public class CN_Cliente
    {
        private CD_Cliente objcd_Cliente = new CD_Cliente();

        public List<Cliente> Listar()
        {
            return objcd_Cliente.Listar();
        }

        public List<Cliente> BuscarPorDocumento(string documento)
        {
            return objcd_Cliente.BuscarPorDocumento(documento);
        }

        public int Registrar(Cliente obj, out string Mensaje)
        {
            Mensaje = string.Empty;
            obj.NombreCompleto = (obj.NombreCompleto ?? string.Empty).Trim();
            obj.Documento = (obj.Documento ?? string.Empty).Trim();
            obj.Correo = (obj.Correo ?? string.Empty).Trim();
            obj.Telefono = (obj.Telefono ?? string.Empty).Trim();
            if (obj.NombreCompleto.Length == 0) Mensaje += "Es necesario el nombre del Cliente\n";
            if (obj.Documento.Length == 0) Mensaje += "Es necesario el documento\n";
            if (obj.Telefono.Length == 0) Mensaje += "Es necesario un Telefono\n";

            if (Mensaje != string.Empty) return 0;
            else return objcd_Cliente.Registrar(obj, out Mensaje);
        }
        public bool Editar(Cliente obj, out string Mensaje)
        {
            Mensaje = string.Empty;
            obj.NombreCompleto = (obj.NombreCompleto ?? string.Empty).Trim();
            obj.Documento = (obj.Documento ?? string.Empty).Trim();
            obj.Correo = (obj.Correo ?? string.Empty).Trim();
            obj.Telefono = (obj.Telefono ?? string.Empty).Trim();
            if (obj.NombreCompleto.Length == 0) Mensaje += "Es necesario el nombre del Cliente\n";
            if (obj.Documento.Length == 0) Mensaje += "Es necesario el documento\n";
            if (obj.Telefono.Length == 0) Mensaje += "Es necesario un Telefono\n";

            if (Mensaje != string.Empty) return false;
            return objcd_Cliente.Editar(obj, out Mensaje);
        }
        public bool Eliminar(Cliente obj, out string Mensaje)
        {
            Mensaje = string.Empty;
            if (obj.IdCliente <= 0) Mensaje += "Es necesario seleccionar un cliente válido\n";

            if (Mensaje != string.Empty) return false;
            return objcd_Cliente.Eliminar(obj, out Mensaje);
        }
    }
}
