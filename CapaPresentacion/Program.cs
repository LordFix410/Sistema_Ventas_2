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

            Application.AddMessageFilter(new CtrlBackspaceClearTextFilter());
            Application.Run(new Login());
        }

        private sealed class CtrlBackspaceClearTextFilter : IMessageFilter
        {
            private const int WmKeyDown = 0x0100;
            private const int VkBack = 0x08;

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg != WmKeyDown || m.WParam.ToInt32() != VkBack ||
                    (Control.ModifierKeys & Keys.Control) != Keys.Control)
                    return false;

                TextBoxBase cuadro = Control.FromHandle(m.HWnd) as TextBoxBase;
                if (cuadro == null)
                    cuadro = BuscarCuadroEnfocado(Form.ActiveForm);

                if (cuadro == null || cuadro.IsDisposed || !cuadro.Enabled || cuadro.ReadOnly)
                    return false;

                cuadro.Clear();
                return true;
            }

            private static TextBoxBase BuscarCuadroEnfocado(Control contenedor)
            {
                if (contenedor == null) return null;

                TextBoxBase cuadro = contenedor as TextBoxBase;
                if (cuadro != null && cuadro.Focused) return cuadro;

                foreach (Control hijo in contenedor.Controls)
                {
                    if (!hijo.ContainsFocus) continue;
                    cuadro = BuscarCuadroEnfocado(hijo);
                    if (cuadro != null) return cuadro;
                }

                return null;
            }
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
                    "SELECT COUNT(*) FROM sys.databases WHERE name = @NombreBase",
                    conexion))
                {
                    comando.Parameters.Add("@NombreBase", System.Data.SqlDbType.NVarChar, 128).Value = "DBSISTEMA_VENTA";
                    int existe = Convert.ToInt32(comando.ExecuteScalar());

                    if (existe > 0)
                        return;
                }
            }

            // Si no existe, buscamos el script incluido con el programa.
            string rutaScript = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "query.sql"
            );

            if (!File.Exists(rutaScript))
            {
                throw new FileNotFoundException(
                    "No se encontró el archivo query.sql."
                );
            }

            string script = File.ReadAllText(rutaScript);

            // query.sql utiliza GO para separar bloques.
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
