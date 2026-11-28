using CapaDatos;
using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    public class CN_Compra
    {

        public CD_Compra objcd_compra = new CD_Compra();

        public int obtenerCorrelativo()
        {
            return objcd_compra.obtenerCorrelativo();
        }
       
        public bool Registrar(Compra obj,DataTable detalleCompra, out string Mensaje)
        {
            return objcd_compra.Registrar(obj, detalleCompra, out Mensaje);
        }

        public Compra ObtenerCompra(string numero)
        {
            Compra oCompra = objcd_compra.obtenerCompra(numero);

            if (oCompra.IdCompra != 0)
            {
                List<Detalle_Compra> oDetalleCompras = objcd_compra.ObtenerDetalleCompra(oCompra.IdCompra);
                oCompra.oDetalleCompras= oDetalleCompras;
            }
            return oCompra;

        }

        public Compra ObtenerCompraParaCorreccion(string numero)
        {
            return objcd_compra.ObtenerCompraParaCorreccion(numero);
        }

        public List<Detalle_Compra> ObtenerDetalleCompraParaCorreccion(int idCompra)
        {
            return objcd_compra.ObtenerDetalleCompraParaCorreccion(idCompra);
        }

        public bool CorregirCompra(int idCompra, int idUsuario, int idProveedor, string numeroFacturaProveedor,
            string motivo, DataTable detalle, out string mensaje)
        {
            return objcd_compra.CorregirCompra(idCompra, idUsuario, idProveedor, numeroFacturaProveedor,
                motivo, detalle, out mensaje);
        }

        public bool AnularCompra(int idCompra, int idUsuario, string motivo, out string mensaje)
        {
            return objcd_compra.AnularCompra(idCompra, idUsuario, motivo, out mensaje);
        }

        public bool ReactivarCompra(int idCompra, int idUsuario, string motivo, out string mensaje)
        {
            return objcd_compra.ReactivarCompra(idCompra, idUsuario, motivo, out mensaje);
        }

        public DataTable ObtenerHistorialAnulaciones()
        {
            return objcd_compra.ObtenerHistorialAnulaciones();
        }

        public DataTable ObtenerDetalleHistorialAnulacion(int idCorreccion)
        {
            return objcd_compra.ObtenerDetalleHistorialAnulacion(idCorreccion);
        }

    
    }
}
