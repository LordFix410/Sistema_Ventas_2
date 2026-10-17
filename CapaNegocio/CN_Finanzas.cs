using CapaDatos;
using System;
using System.Data;

namespace CapaNegocio
{
    public class CN_Finanzas
    {
        private readonly CD_Finanzas datos = new CD_Finanzas();

        public DataSet ObtenerPanel(DateTime desde, DateTime hasta) { return datos.ObtenerPanel(desde, hasta); }
        public DataTable ObtenerCifrasCierre(DateTime inicio, decimal fondo) { return datos.ObtenerCifrasCierre(inicio, fondo); }
        public bool RegistrarMovimiento(string tipo, int? relacionado, int usuario, DateTime fecha,
            string concepto, decimal monto, string medio, string referencia, out string mensaje)
        { return datos.RegistrarMovimiento(tipo, relacionado, usuario, fecha, concepto, monto, medio, referencia, out mensaje); }
        public bool RegistrarCierre(int usuario, DateTime inicio, decimal fondo, decimal contado, out string mensaje, out int id)
        { return datos.RegistrarCierre(usuario, inicio, fondo, contado, out mensaje, out id); }
        public bool RegistrarDeposito(int cierre, int usuario, decimal capital, decimal ganancia, string referencia, out string mensaje)
        { return datos.RegistrarDeposito(cierre, usuario, capital, ganancia, referencia, out mensaje); }
    }
}
