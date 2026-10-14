using System;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Forms;

namespace CapaPresentacion
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture =
                new System.Globalization.CultureInfo("en-PE");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                PrepararBaseDeDatos();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo preparar la base de datos.\n\n" +
                    ex.Message,
                    "Sistema Ventas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                return;
            }

            Application.Run(new Login());
        }

        private static void PrepararBaseDeDatos()
        {
            string conexionMaster =
                @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True";

            // Primero comprobamos si la base ya existe.
            using (SqlConnection conexion = new SqlConnection(conexionMaster))
            {
                conexion.Open();

                using (SqlCommand comando = new SqlCommand(
                    "SELECT COUNT(*) FROM sys.databases WHERE name = 'DBSVentas'",
                    conexion))
                {
                    int existe = Convert.ToInt32(comando.ExecuteScalar());

                    if (existe > 0)
                        return;
                }
            }

            // Si no existe, buscamos el script incluido con el programa.
            string rutaScript = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "querys.sql"
            );

            if (!File.Exists(rutaScript))
            {
                throw new FileNotFoundException(
                    "No se encontró el archivo querys.sql."
                );
            }

            string script = File.ReadAllText(rutaScript);

            // querys.sql utiliza GO para separar bloques.
            string[] bloques = System.Text.RegularExpressions.Regex.Split(
                script,
                @"^\s*GO\s*$",
                System.Text.RegularExpressions.RegexOptions.Multiline |
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            using (SqlConnection conexion = new SqlConnection(conexionMaster))
            {
                conexion.Open();

                foreach (string bloque in bloques)
                {
                    if (string.IsNullOrWhiteSpace(bloque))
                        continue;

                    using (SqlCommand comando = new SqlCommand(bloque, conexion))
                    {
                        comando.CommandTimeout = 120;
                        comando.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}