using System;
using System.Data.SqlClient;
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
                VerificarConexionBaseDeDatos();
            }
            catch (Exception ex)
            {
                CapaPresentacion.DialogoSistema.Show(
                    "No se pudo conectar con la base de datos del servidor.\n\n" +
                    ex.GetBaseException().Message,
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

        private static void VerificarConexionBaseDeDatos()
        {
            // La base se instala una sola vez en el servidor. Cada equipo
            // cliente solo valida la conexión; no crea una copia local.
            using (SqlConnection conexion = new SqlConnection(CapaDatos.Conexion.cadena))
            {
                conexion.Open();
            }
        }
    }
}
