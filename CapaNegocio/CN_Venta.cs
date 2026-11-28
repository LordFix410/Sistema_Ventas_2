using CapaDatos;
using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    public class CN_Venta
    {
        private CD_Venta objcd_venta = new CD_Venta();

        public bool RestarStock(int idproducto, int cantidad)
        {
            return objcd_venta.RestarStock(idproducto, cantidad);
        }

        public bool SumarStock(int idproducto, int cantidad)
        {
            return objcd_venta.SumarStock(idproducto, cantidad);
        }
        public int ObtenerCorrelativo()
        {
            return objcd_venta.obtenerCorrelativo();
        }

        public bool Registrar(Venta obj, DataTable DetalleVenta, out string Mensaje)
        {
            return objcd_venta.Registrar(obj,DetalleVenta, out Mensaje);
        }

        public Venta ObtenerVenta(string numero)
        {
            Venta oVenta = objcd_venta.obtenerVenta(numero);
            if (oVenta.IdVenta != 0)
            {
                List<Detalle_Venta> oDetalleVenta = objcd_venta.obtenerDetalleVenta(oVenta.IdVenta);
                oVenta.oDetalle_Venta = oDetalleVenta;
            }

            return oVenta;
        }

        public DataTable ObtenerDetalleDevolucion(string numeroDocumento, out string mensaje)
        {
            return objcd_venta.ObtenerDetalleDevolucion(numeroDocumento, out mensaje);
        }

        public DataTable ObtenerHistorialDevoluciones(out string mensaje)
        {
            return objcd_venta.ObtenerHistorialDevoluciones(out mensaje);
        }

        public bool RegistrarDevolucion(int idVenta, int idUsuario, string tipo, string motivo,
            decimal montoReembolsado, DataTable detalle, out decimal montoDevuelto, out string mensaje)
        {
            return objcd_venta.RegistrarDevolucion(idVenta, idUsuario, tipo, motivo, montoReembolsado, detalle,
                out montoDevuelto, out mensaje);
        }
    }

}

