using CapaDatos;
using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    public class CN_Producto
    {
        private CD_Producto objcd_producto = new CD_Producto();

        public int RegistrarTemporal(Producto obj, int cantidad, out string Mensaje)
        {
            if (string.IsNullOrWhiteSpace(obj.Nombre))
            {
                Mensaje = "Escribe el nombre o descripción del artículo temporal.";
                return 0;
            }
            if (cantidad <= 0)
            {
                Mensaje = "La cantidad debe ser mayor que cero.";
                return 0;
            }
            return objcd_producto.RegistrarTemporal(obj, cantidad, out Mensaje);
        }

        public void EliminarTemporalNoVendido(int idProducto)
        {
            objcd_producto.EliminarTemporalNoVendido(idProducto);
        }

        public List<Producto> Listar()
        {
            return objcd_producto.Listar();
        }

        public Producto BuscarPorCodigo(string codigo)
        {
            return objcd_producto.BuscarPorCodigo(codigo);
        }

        public Producto BuscarPorId(int idProducto)
        {
            return objcd_producto.BuscarPorId(idProducto);
        }

        public List<Producto> Buscar(string columna, string termino, bool soloActivos = false)
        {
            return objcd_producto.Buscar(columna, termino, soloActivos);
        }

        public int Registrar(Producto obj, out string Mensaje)
        {
            Mensaje = string.Empty;
            if (obj.Codigo == "") Mensaje += "Es necesario el código del Producto\n";

            if (obj.Nombre == "") Mensaje += "Es necesario el nombre del Producto\n";

            if (obj.Descripcion == "") Mensaje += "Es necesario una descripcion\n";
            if (obj.Porcentaje < 0) Mensaje += "El porcentaje no puede ser negativo\n";

            if (Mensaje != string.Empty) return 0;
            else return objcd_producto.Registrar(obj, out Mensaje);
        }
        public bool Editar(Producto obj, out string Mensaje)
        {
            Mensaje = string.Empty;
            if (obj.Codigo == "") Mensaje += "Es necesario el código del Producto\n";

            if (obj.Nombre == "") Mensaje += "Es necesario el nombre del Producto\n";

            if (obj.Descripcion == "") Mensaje += "Es necesario una descripcion\n";
            if (obj.Porcentaje < 0) Mensaje += "El porcentaje no puede ser negativo\n";

            if (Mensaje != string.Empty) return false;
            return objcd_producto.Editar(obj, out Mensaje);
        }
        public bool Eliminar(Producto obj, out string Mensaje)
        {
            Mensaje = string.Empty;
            if (obj.IdProducto <= 0) Mensaje += "Es necesario seleccionar un producto válido\n";

            if (Mensaje != string.Empty) return false;
            return objcd_producto.Eliminar(obj, out Mensaje);
        }
    }
}
