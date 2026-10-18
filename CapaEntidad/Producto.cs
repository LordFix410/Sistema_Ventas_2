using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    public class Producto
    {
        public int IdProducto {  get; set; }
        public string Codigo {  get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }

        public Categoria oCategoria { get; set; }
        public int Stock {  get; set; }
        public decimal PrecioCompra {  get; set; }
        public decimal PrecioVenta {  get; set; }
        public decimal Porcentaje { get; set; }
        public int? IdGrupoReposicion { get; set; }
        public int? CantidadMinima { get; set; }
        public int? CantidadMaxima { get; set; }
        public int? IdGrupoProveedor { get; set; }
        public string GrupoReposicion { get; set; }
        public string GrupoProveedores { get; set; }
        public bool Estado {  get; set; }
        public string FechaRegistro {  get; set; }

    }
}
