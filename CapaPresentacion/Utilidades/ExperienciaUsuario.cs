using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace CapaPresentacion.Utilidades
{
    /// <summary>Pequeñas mejoras comunes de teclado y lectura para los formularios.</summary>
    internal static class ExperienciaUsuario
    {
        public static void Aplicar(Form formulario, Control campoInicial = null, params Control[] excepcionesEnter)
        {
            if (formulario == null) return;
            AplicarTemaVisual(formulario);
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

        /// <summary>Aplica colores y estados legibles sin alterar los eventos ni la lógica del formulario.</summary>
        public static void AplicarTemaVisual(Form formulario)
        {
            if (formulario == null) return;

            foreach (Control control in Controles(formulario))
            {
                if (control is Label etiqueta)
                {
                    etiqueta.Font = new Font(etiqueta.Font.FontFamily, etiqueta.Font.Size,
                        etiqueta.Font.Style | FontStyle.Bold);
                    etiqueta.ForeColor = Color.FromArgb(38, 61, 80);
                }
                else if (control is TextBox texto)
                {
                    texto.Font = new Font("Segoe UI", 9F, texto.Font.Style);
                    texto.BorderStyle = BorderStyle.FixedSingle;
                    texto.ForeColor = Color.FromArgb(35, 45, 55);
                    texto.BackColor = texto.ReadOnly || !texto.Enabled
                        ? Color.FromArgb(238, 242, 246)
                        : Color.White;
                }
                else if (control is ComboBox combo)
                {
                    combo.Font = new Font("Segoe UI", 9F, combo.Font.Style);
                    combo.ForeColor = Color.FromArgb(35, 45, 55);
                    if (combo.Enabled) combo.BackColor = Color.White;
                }
                else if (control is GroupBox grupo)
                {
                    string titulo = grupo.Text.ToUpperInvariant();
                    bool esContacto = titulo.Contains("CLIENTE") || titulo.Contains("PROVEEDOR");
                    bool esResumen = titulo.Contains("RESUMEN") || titulo.Contains("TOTAL") || titulo.Contains("PAGO");
                    grupo.ForeColor = esContacto
                        ? Color.FromArgb(38, 112, 78)
                        : esResumen ? Color.FromArgb(143, 101, 0) : Color.FromArgb(43, 91, 126);
                    grupo.BackColor = esContacto
                        ? Color.FromArgb(241, 249, 244)
                        : esResumen ? Color.FromArgb(255, 250, 232) : Color.FromArgb(239, 246, 251);
                    grupo.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }
                else if (control is Button boton)
                {
                    EstilizarBoton(boton);
                }
                else if (control is DataGridView tabla)
                {
                    EstilizarAccionesTabla(tabla);
                }
            }
        }

        private static void EstilizarBoton(Button boton)
        {
            string etiqueta = (boton.Name + " " + boton.Text).Trim().ToUpperInvariant();
            Color fondo;
            Color texto;
            Color borde;
            Color hover;

            if (etiqueta.Contains("ELIMINAR") || etiqueta.Contains("QUITAR") || etiqueta.Contains("BORRAR"))
            {
                fondo = Color.FromArgb(198, 40, 40);
                texto = Color.White;
                borde = Color.FromArgb(150, 25, 25);
                hover = Color.FromArgb(160, 25, 25);
            }
            else if (etiqueta.Contains("CANCELAR"))
            {
                fondo = Color.FromArgb(232, 174, 0);
                texto = Color.FromArgb(45, 42, 25);
                borde = Color.FromArgb(190, 140, 0);
                hover = Color.FromArgb(210, 155, 0);
            }
            else if (etiqueta.Contains("LIMPIAR") || etiqueta.Contains("VACIAR"))
            {
                fondo = Color.FromArgb(226, 232, 238);
                texto = Color.FromArgb(45, 58, 70);
                borde = Color.FromArgb(170, 183, 195);
                hover = Color.FromArgb(205, 216, 226);
            }
            else if (etiqueta.Contains("GUARDAR") || etiqueta.Contains("REGISTRAR") || etiqueta.Contains("AGREGAR"))
            {
                fondo = Color.FromArgb(36, 142, 74);
                texto = Color.White;
                borde = Color.FromArgb(27, 112, 56);
                hover = Color.FromArgb(27, 112, 56);
            }
            else if (etiqueta.Contains("INGRESAR") || etiqueta.Contains("ENTRAR") || etiqueta.Contains("INICIAR SESIÓN") ||
                     etiqueta.Contains("BUSCAR") || etiqueta.Contains("CARGAR") || etiqueta.Contains("CONSULTAR") ||
                     etiqueta.Contains("CALCULAR") || etiqueta.Contains("ACEPTAR") || etiqueta.Contains("DESCARGAR"))
            {
                fondo = Color.FromArgb(35, 112, 177);
                texto = Color.White;
                borde = Color.FromArgb(25, 87, 139);
                hover = Color.FromArgb(25, 87, 139);
            }
            else if (etiqueta.Contains("EDITAR") || etiqueta.Contains("MODIFICAR"))
            {
                fondo = Color.FromArgb(235, 190, 35);
                texto = Color.FromArgb(52, 45, 20);
                borde = Color.FromArgb(194, 147, 0);
                hover = Color.FromArgb(218, 169, 0);
            }
            else
            {
                fondo = Color.FromArgb(219, 232, 243);
                texto = Color.FromArgb(35, 75, 108);
                borde = Color.FromArgb(158, 190, 216);
                hover = Color.FromArgb(194, 217, 235);
            }

            boton.FlatStyle = FlatStyle.Flat;
            boton.FlatAppearance.BorderSize = 1;
            boton.FlatAppearance.BorderColor = borde;
            boton.FlatAppearance.MouseOverBackColor = hover;
            boton.FlatAppearance.MouseDownBackColor = borde;
            boton.UseVisualStyleBackColor = false;
            boton.BackColor = fondo;
            boton.ForeColor = texto;
            boton.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            boton.Cursor = Cursors.Hand;
            if (boton is IconButton icono)
                icono.IconColor = texto;
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

            EstilizarAccionesTabla(tabla);
        }

        private static void EstilizarAccionesTabla(DataGridView tabla)
        {
            foreach (DataGridViewButtonColumn boton in tabla.Columns.OfType<DataGridViewButtonColumn>())
            {
                string etiqueta = (boton.Name + " " + boton.HeaderText + " " + boton.Text).ToUpperInvariant();
                if (etiqueta.Contains("ELIMINAR") || etiqueta.Contains("QUITAR") || etiqueta.Contains("BORRAR") || etiqueta.Contains("ACCIÓN"))
                {
                    boton.FlatStyle = FlatStyle.Flat;
                    boton.DefaultCellStyle.BackColor = Color.FromArgb(198, 40, 40);
                    boton.DefaultCellStyle.ForeColor = Color.White;
                    boton.DefaultCellStyle.SelectionBackColor = Color.FromArgb(150, 25, 25);
                    boton.DefaultCellStyle.SelectionForeColor = Color.White;
                }
            }
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
