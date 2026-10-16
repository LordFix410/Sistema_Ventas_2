using System;

namespace CapaPresentacion.Utilidades
{
    internal static class FormatoPorcentaje
    {
        // PRODUCTO.Porcentaje is a fraction after a purchase (0.61 = 61%),
        // and the requested margin before the first purchase (61 = 61%).
        public static decimal ParaMostrar(decimal porcentajeGuardado, decimal precioCompra)
        {
            return precioCompra > 0 ? porcentajeGuardado * 100m : porcentajeGuardado;
        }

        // The entry field accepts whole percentages. Round upward so the
        // displayed requested margin never falls below the actual margin.
        public static decimal ParaEditar(decimal porcentajeGuardado, decimal precioCompra)
        {
            decimal visible = ParaMostrar(porcentajeGuardado, precioCompra);
            return precioCompra > 0 ? decimal.Ceiling(visible) : visible;
        }
    }
}
