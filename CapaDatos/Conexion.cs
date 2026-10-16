using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SqlClient;

namespace CapaDatos
{
    public class Conexion
    {
        public static readonly string cadena = CargarCadena();

        private static string CargarCadena()
        {
            string ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "conexion.txt");
            if (!File.Exists(ruta))
                throw new FileNotFoundException(
                    "No se encontró conexion.txt junto al programa. Copie conexion.ejemplo.txt como conexion.txt y configure el servidor.",
                    ruta);

            string valor = File.ReadAllText(ruta).Trim().TrimStart('\uFEFF');
            if (string.IsNullOrWhiteSpace(valor))
                throw new InvalidDataException("El archivo conexion.txt está vacío. Pegue allí la cadena de conexión SQL Server.");

            var constructor = new SqlConnectionStringBuilder(valor);
            if (string.IsNullOrWhiteSpace(constructor.InitialCatalog))
                throw new InvalidDataException("La cadena de conexion.txt debe incluir Initial Catalog=DBSISTEMA_VENTA.");

            return constructor.ConnectionString;
        }
    }
}
