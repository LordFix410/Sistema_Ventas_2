using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CapaPresentacion.Utilidades
{
    /// <summary>Pequeñas mejoras comunes de teclado y lectura para los formularios.</summary>
    internal static class ExperienciaUsuario
    {
        public static void Aplicar(Form formulario, Control campoInicial = null, params Control[] excepcionesEnter)
        {
            if (formulario == null) return;
            var excluidos = excepcionesEnter ?? new Control[0];
            foreach (Control control in Controles(formulario))
            {
                if (control is DataGridView tabla)
                    EstilizarTabla(tabla);

                if (EsCampoEditable(control))
                {
                    if (excluidos.Contains(control))
                    {
                        if (control.Name.IndexOf("Busqueda", StringComparison.OrdinalIgnoreCase) >= 0 &&
                            !formulario.GetType().Name.StartsWith("md", StringComparison.OrdinalIgnoreCase))
                            control.KeyDown += BuscarConEnter;
                    }
                    else control.KeyDown += NavegarConEnter;
                }
            }

            formulario.Shown += (s, e) =>
            {
                Control destino = campoInicial;
                if (destino == null || !destino.CanSelect || !destino.Visible || !destino.Enabled)
                    destino = Controles(formulario).FirstOrDefault(c => c is TextBox && c.CanSelect && c.Visible && c.Enabled);
                if (destino != null)
                {
                    destino.Focus();
                    if (destino is TextBox texto) texto.SelectAll();
                }
            };
        }

        private static bool EsCampoEditable(Control control)
        {
            if (control is TextBox texto && texto.Multiline) return false;
            return control is TextBox || control is ComboBox || control is NumericUpDown;
        }

        private static void NavegarConEnter(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter || e.Handled || e.SuppressKeyPress) return;
            var actual = sender as Control;
            if (actual == null) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            actual.FindForm()?.SelectNextControl(actual, true, true, true, true);
        }

        private static void BuscarConEnter(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter || e.Handled || e.SuppressKeyPress) return;
            var formulario = (sender as Control)?.FindForm();
            if (formulario == null) return;
            var boton = Controles(formulario).OfType<Button>().FirstOrDefault(b =>
                b.Name.Equals("btnBuscar", StringComparison.OrdinalIgnoreCase) ||
                b.Name.Equals("btnBuscarProducto", StringComparison.OrdinalIgnoreCase));
            if (boton == null) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            boton.PerformClick();
        }

        private static void EstilizarTabla(DataGridView tabla)
        {
            tabla.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabla.EnableHeadersVisualStyles = false;
            tabla.BackgroundColor = Color.FromArgb(242, 245, 248);
            tabla.BorderStyle = BorderStyle.FixedSingle;
            tabla.GridColor = Color.FromArgb(220, 226, 232);
            tabla.RowHeadersVisible = false;
            tabla.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tabla.MultiSelect = false;
            tabla.AllowUserToResizeRows = false;
            tabla.RowTemplate.Height = 48;
            tabla.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            tabla.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            tabla.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            tabla.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            tabla.ColumnHeadersHeight = 48;
            tabla.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(48, 91, 125);
            tabla.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            tabla.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            tabla.ColumnHeadersDefaultCellStyle.Padding = new Padding(5, 0, 5, 0);
            tabla.DefaultCellStyle.BackColor = Color.White;
            tabla.DefaultCellStyle.ForeColor = Color.FromArgb(35, 45, 55);
            tabla.DefaultCellStyle.SelectionBackColor = Color.FromArgb(45, 120, 180);
            tabla.DefaultCellStyle.SelectionForeColor = Color.White;
            tabla.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            tabla.DefaultCellStyle.Padding = new Padding(4, 2, 4, 2);
            tabla.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 246, 250);
        }

        private static System.Collections.Generic.IEnumerable<Control> Controles(Control raiz)
        {
            foreach (Control hijo in raiz.Controls)
            {
                yield return hijo;
                foreach (Control descendiente in Controles(hijo)) yield return descendiente;
            }
        }
    }
}
