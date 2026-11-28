using CapaEntidad;
using CapaNegocio;
using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace CapaPresentacion
{
    public class frmDevoluciones : Form
    {
        private readonly Usuario usuario;
        private readonly CN_Venta negocio = new CN_Venta();
        private readonly Color azul = Color.FromArgb(35, 83, 119);
        private readonly Color fondo = Color.FromArgb(242, 245, 248);
        private TextBox txtNumeroVenta;
        private TextBox txtMotivo;
        private NumericUpDown nudMontoDevuelto;
        private DataGridView dgvDetalle;
        private DataGridView dgvHistorial;
        private Label lblVenta;
        private Label lblResumen;
        private Label lblEstado;
        private Label lblMontoOriginal;
        private Label lblMontoDevuelto;
        private Label lblSaldoFactura;
        private int idVentaActual;
        private bool cargandoDetalle;

        public frmDevoluciones(Usuario usuarioActual)
        {
            usuario = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
            CapaPresentacion.Utilidades.IconoAplicacion.Aplicar(this);
            Text = "Devoluciones y anulaciones";
            BackColor = fondo;
            Font = new Font("Segoe UI", 9F);
            Dock = DockStyle.Fill;
            MinimumSize = new Size(760, 480);
            InicializarInterfaz();
        }

        private void InicializarInterfaz()
        {
            var raiz = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10)
            };
            raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var encabezado = new Label
            {
                Name = "lblTituloDevoluciones",
                Text = "Devoluciones y anulaciones de ventas",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 61, 88),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0)
            };
            raiz.Controls.Add(encabezado, 0, 0);

            var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F) };
            var registrar = new TabPage("Registrar") { BackColor = fondo, Padding = new Padding(8) };
            var historial = new TabPage("Historial") { BackColor = fondo, Padding = new Padding(8) };
            tabs.TabPages.Add(registrar);
            tabs.TabPages.Add(historial);
            tabs.SelectedIndexChanged += (s, e) => { if (tabs.SelectedTab == historial) CargarHistorial(); };
            registrar.Controls.Add(CrearPanelRegistro());
            historial.Controls.Add(CrearPanelHistorial());
            raiz.Controls.Add(tabs, 0, 1);
            Controls.Add(raiz);
        }

        private Control CrearPanelRegistro()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(4),
                AutoScroll = true
            };

            var detalle = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(2),
                BackColor = fondo
            };
            detalle.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            detalle.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
            detalle.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            detalle.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

            var busqueda = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.White,
                Padding = new Padding(10, 7, 10, 7)
            };
            busqueda.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 66));
            busqueda.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            busqueda.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 138));
            busqueda.Controls.Add(new Label { Text = "Factura:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9F, FontStyle.Bold) }, 0, 0);
            txtNumeroVenta = new TextBox { Dock = DockStyle.Fill, MaxLength = 50, Margin = new Padding(3, 2, 8, 2) };
            txtNumeroVenta.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; BuscarVenta(); } };
            busqueda.Controls.Add(txtNumeroVenta, 1, 0);
            var btnBuscar = Boton("Buscar factura", 130);
            btnBuscar.Dock = DockStyle.Fill;
            btnBuscar.Margin = new Padding(2);
            btnBuscar.Click += (s, e) => BuscarVenta();
            busqueda.Controls.Add(btnBuscar, 2, 0);

            lblVenta = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                BackColor = Color.White,
                ForeColor = azul,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 4, 8, 4),
                Text = "Busca una factura para ver sus productos y cantidades disponibles para devolver."
            };

            dgvDetalle = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                EditMode = DataGridViewEditMode.EditOnEnter,
                EnableHeadersVisualStyles = false,
                GridColor = Color.FromArgb(204, 218, 228),
                ColumnHeadersHeight = 48,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                DefaultCellStyle = { Font = new Font("Segoe UI", 9.5F), Padding = new Padding(8, 5, 8, 5) },
                ColumnHeadersDefaultCellStyle = { BackColor = azul, ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Padding = new Padding(8, 6, 8, 6), WrapMode = DataGridViewTriState.True },
                RowTemplate = { Height = 38 },
                AlternatingRowsDefaultCellStyle = { BackColor = Color.FromArgb(242, 247, 250) }
            };
            dgvDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "IdDetalleVenta", DataPropertyName = "IdDetalleVenta", Visible = false });
            dgvDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "IdProducto", DataPropertyName = "IdProducto", Visible = false });
            dgvDetalle.Columns.Add(Columna("Producto", "Producto", "Producto", 220, true));
            dgvDetalle.Columns.Add(Columna("CantidadVendida", "Vendida", "CantidadVendida", 70, true));
            dgvDetalle.Columns.Add(Columna("CantidadDevuelta", "Devuelta", "CantidadDevuelta", 75, true));
            dgvDetalle.Columns.Add(Columna("CantidadPendiente", "Pendiente", "CantidadPendiente", 80, true));
            var devolver = Columna("CantidadDevolver", "Devolver", "", 85, false);
            devolver.DefaultCellStyle.NullValue = "0";
            devolver.DefaultCellStyle.BackColor = Color.FromArgb(255, 250, 224);
            devolver.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvDetalle.Columns.Add(devolver);
            var precio = Columna("PrecioVenta", "Precio", "PrecioVenta", 85, true);
            precio.DefaultCellStyle.Format = "N2";
            dgvDetalle.Columns.Add(precio);
            dgvDetalle.Columns.Add(new DataGridViewCheckBoxColumn { Name = "EsTemporal", DataPropertyName = "EsTemporal", Visible = false });
            dgvDetalle.CellValueChanged += (s, e) => { if (!cargandoDetalle && e.RowIndex >= 0 && e.ColumnIndex == dgvDetalle.Columns["CantidadDevolver"].Index) ActualizarResumen(); };
            dgvDetalle.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvDetalle.IsCurrentCellDirty) dgvDetalle.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvDetalle.CellValidating += DgvDetalle_CellValidating;
            dgvDetalle.DataError += (s, e) => { e.ThrowException = false; e.Cancel = false; };

            lblEstado = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                BackColor = Color.FromArgb(255, 247, 219),
                ForeColor = Color.FromArgb(105, 78, 13),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 4, 8, 4),
                Text = "Busca una factura para comenzar. El reembolso se ajusta manualmente en el cierre."
            };

            detalle.Controls.Add(busqueda, 0, 0);
            detalle.Controls.Add(lblVenta, 0, 1);
            detalle.Controls.Add(dgvDetalle, 0, 2);
            detalle.Controls.Add(lblEstado, 0, 3);

            var resumen = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(14),
                BackColor = Color.White
            };
            resumen.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            resumen.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
            resumen.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            resumen.RowStyles.Add(new RowStyle(SizeType.Absolute, 138));
            resumen.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            resumen.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            resumen.Controls.Add(new Label
            {
                Text = "Resumen de devolución",
                Dock = DockStyle.Fill,
                ForeColor = azul,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            var datosFactura = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(10), BackColor = Color.FromArgb(246, 249, 251) };
            datosFactura.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            datosFactura.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            for (int i = 0; i < 3; i++) datosFactura.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            string[] etiquetasResumen = { "Original", "Devuelto", "Saldo" };
            Label[] valoresResumen = new Label[3];
            for (int i = 0; i < etiquetasResumen.Length; i++)
            {
                datosFactura.Controls.Add(new Label { Text = etiquetasResumen[i], Dock = DockStyle.Fill, ForeColor = Color.FromArgb(83, 102, 117), TextAlign = ContentAlignment.MiddleLeft }, 0, i);
                valoresResumen[i] = new Label { Text = "Q 0.00", Dock = DockStyle.Fill, ForeColor = azul, Font = new Font("Segoe UI", 10F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight };
                datosFactura.Controls.Add(valoresResumen[i], 1, i);
            }
            lblMontoOriginal = valoresResumen[0];
            lblMontoDevuelto = valoresResumen[1];
            lblSaldoFactura = valoresResumen[2];
            resumen.Controls.Add(datosFactura, 0, 1);

            var seleccion = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(8), BackColor = Color.FromArgb(240, 246, 250) };
            seleccion.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            seleccion.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            seleccion.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            seleccion.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            seleccion.Controls.Add(new Label { Text = "Productos seleccionados", Dock = DockStyle.Fill, ForeColor = Color.FromArgb(83, 102, 117), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            seleccion.Controls.Add(new Label { Text = "Reembolso real", Dock = DockStyle.Fill, ForeColor = Color.FromArgb(83, 102, 117), TextAlign = ContentAlignment.MiddleRight }, 1, 0);
            lblResumen = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = azul, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Text = "0 unidades\nQ 0.00" };
            seleccion.Controls.Add(lblResumen, 0, 1);
            nudMontoDevuelto = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Minimum = 0, Maximum = 999999999.99m, ThousandsSeparator = true, TextAlign = HorizontalAlignment.Right, Margin = new Padding(4, 2, 2, 2) };
            seleccion.Controls.Add(nudMontoDevuelto, 1, 1);
            resumen.Controls.Add(seleccion, 0, 2);

            var motivo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(0, 8, 0, 4) };
            motivo.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            motivo.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
            motivo.Controls.Add(new Label { Text = "Motivo *", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            txtMotivo = new TextBox { Dock = DockStyle.Fill, MaxLength = 250, CharacterCasing = CharacterCasing.Upper, Multiline = true, ScrollBars = ScrollBars.Vertical, MinimumSize = new Size(0, 78) };
            motivo.Controls.Add(txtMotivo, 0, 1);
            resumen.Controls.Add(motivo, 0, 3);

            var btnDevolver = Boton("Devolver selección", 155);
            btnDevolver.Dock = DockStyle.Fill;
            btnDevolver.Margin = new Padding(3);
            btnDevolver.Click += (s, e) => RegistrarDevolucion("PARCIAL");
            var btnAnular = Boton("Anular saldo de factura", 190);
            btnAnular.Dock = DockStyle.Fill;
            btnAnular.Margin = new Padding(3);
            btnAnular.BackColor = Color.FromArgb(190, 42, 42);
            btnAnular.Click += (s, e) => RegistrarAnulacionCompleta();
            var acciones = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 3, 0, 3) };
            acciones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            acciones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            acciones.Controls.Add(btnDevolver, 0, 0);
            acciones.Controls.Add(btnAnular, 1, 0);
            resumen.Controls.Add(acciones, 0, 4);
            resumen.Controls.Add(new Label { Text = "El reembolso queda como referencia; el efectivo se ajusta manualmente en el cierre.", Dock = DockStyle.Fill, ForeColor = Color.FromArgb(85, 100, 115), TextAlign = ContentAlignment.MiddleLeft }, 0, 5);

            bool distribucionCompacta = false;
            Action ajustarDistribucion = delegate
            {
                bool compacta = panel.ClientSize.Width < 1080;
                if (compacta == distribucionCompacta && panel.Controls.Count > 0) return;
                distribucionCompacta = compacta;
                panel.SuspendLayout();
                panel.Controls.Clear();
                panel.ColumnStyles.Clear();
                panel.RowStyles.Clear();
                if (compacta)
                {
                    panel.ColumnCount = 1;
                    panel.RowCount = 2;
                    panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 520));
                    panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 540));
                    panel.AutoScrollMinSize = new Size(0, 1060);
                    panel.Controls.Add(detalle, 0, 0);
                    panel.Controls.Add(resumen, 0, 1);
                }
                else
                {
                    panel.ColumnCount = 2;
                    panel.RowCount = 1;
                    panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
                    panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
                    panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                    panel.AutoScrollMinSize = Size.Empty;
                    panel.Controls.Add(detalle, 0, 0);
                    panel.Controls.Add(resumen, 1, 0);
                }
                panel.ResumeLayout(true);
            };
            panel.SizeChanged += (s, e) => ajustarDistribucion();
            ajustarDistribucion();
            return panel;
        }

        private Control CrearPanelHistorial()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(2) };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var barra = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(8, 7, 4, 3) };
            barra.Controls.Add(new Label { Text = "Últimas 100 devoluciones y anulaciones", AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Margin = new Padding(4, 7, 12, 0) });
            var actualizar = Boton("Actualizar", 110);
            actualizar.Click += (s, e) => CargarHistorial();
            barra.Controls.Add(actualizar);
            dgvHistorial = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = true,
                EnableHeadersVisualStyles = false,
                GridColor = Color.FromArgb(204, 218, 228),
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeight = 64,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersDefaultCellStyle = { BackColor = azul, ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Padding = new Padding(8, 6, 8, 6), WrapMode = DataGridViewTriState.True },
                DefaultCellStyle = { Font = new Font("Segoe UI", 9.5F), Padding = new Padding(8, 6, 8, 6), WrapMode = DataGridViewTriState.True, SelectionBackColor = azul, SelectionForeColor = Color.White },
                RowTemplate = { Height = 38 },
                AlternatingRowsDefaultCellStyle = { BackColor = Color.FromArgb(242, 247, 250) }
            };
            dgvHistorial.DataBindingComplete += (s, e) =>
            {
                if (dgvHistorial.Columns.Contains("IdRegistro"))
                {
                    dgvHistorial.Columns["IdRegistro"].HeaderText = "N.º registro";
                    dgvHistorial.Columns["IdRegistro"].FillWeight = 65;
                    dgvHistorial.Columns["IdRegistro"].MinimumWidth = 125;
                }
                if (dgvHistorial.Columns.Contains("FechaRegistro"))
                {
                    dgvHistorial.Columns["FechaRegistro"].HeaderText = "Fecha";
                    dgvHistorial.Columns["FechaRegistro"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                    dgvHistorial.Columns["FechaRegistro"].FillWeight = 100;
                    dgvHistorial.Columns["FechaRegistro"].MinimumWidth = 130;
                }
                if (dgvHistorial.Columns.Contains("NumeroDocumento"))
                {
                    dgvHistorial.Columns["NumeroDocumento"].HeaderText = "Factura";
                    dgvHistorial.Columns["NumeroDocumento"].FillWeight = 75;
                    dgvHistorial.Columns["NumeroDocumento"].MinimumWidth = 85;
                }
                if (dgvHistorial.Columns.Contains("Tipo"))
                {
                    dgvHistorial.Columns["Tipo"].HeaderText = "Movimiento";
                    dgvHistorial.Columns["Tipo"].FillWeight = 85;
                    dgvHistorial.Columns["Tipo"].MinimumWidth = 95;
                }
                if (dgvHistorial.Columns.Contains("Motivo"))
                {
                    dgvHistorial.Columns["Motivo"].HeaderText = "Motivo";
                    dgvHistorial.Columns["Motivo"].FillWeight = 130;
                    dgvHistorial.Columns["Motivo"].MinimumWidth = 130;
                }
                if (dgvHistorial.Columns.Contains("ProductosDevueltos"))
                {
                    var productos = dgvHistorial.Columns["ProductosDevueltos"];
                    productos.HeaderText = "Productos y cantidades devueltas";
                    productos.FillWeight = 260;
                    productos.MinimumWidth = 230;
                    productos.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                }
                if (dgvHistorial.Columns.Contains("ValorProductos"))
                {
                    dgvHistorial.Columns["ValorProductos"].HeaderText = "Valor productos";
                    dgvHistorial.Columns["ValorProductos"].DefaultCellStyle.Format = "'Q' #,##0.00";
                    dgvHistorial.Columns["ValorProductos"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    dgvHistorial.Columns["ValorProductos"].FillWeight = 85;
                    dgvHistorial.Columns["ValorProductos"].MinimumWidth = 130;
                }
                if (dgvHistorial.Columns.Contains("MontoDevuelto"))
                {
                    dgvHistorial.Columns["MontoDevuelto"].HeaderText = "Reembolso real";
                    dgvHistorial.Columns["MontoDevuelto"].DefaultCellStyle.Format = "'Q' #,##0.00";
                    dgvHistorial.Columns["MontoDevuelto"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    dgvHistorial.Columns["MontoDevuelto"].FillWeight = 90;
                    dgvHistorial.Columns["MontoDevuelto"].MinimumWidth = 105;
                }
                if (dgvHistorial.Columns.Contains("Usuario"))
                {
                    dgvHistorial.Columns["Usuario"].HeaderText = "Registró";
                    dgvHistorial.Columns["Usuario"].FillWeight = 120;
                    dgvHistorial.Columns["Usuario"].MinimumWidth = 120;
                }
            };
            panel.Controls.Add(barra, 0, 0);
            panel.Controls.Add(dgvHistorial, 0, 1);
            return panel;
        }

        private static DataGridViewTextBoxColumn Columna(string nombre, string titulo, string propiedad, int ancho, bool soloLectura)
        {
            return new DataGridViewTextBoxColumn
            {
                Name = nombre,
                HeaderText = titulo,
                DataPropertyName = propiedad,
                FillWeight = ancho,
                MinimumWidth = Math.Min(ancho, 55),
                ReadOnly = soloLectura,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
        }

        private static Button Boton(string texto, int ancho)
        {
            return new Button
            {
                Text = texto,
                Width = ancho,
                Height = 34,
                BackColor = Color.FromArgb(31, 112, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Margin = new Padding(4, 0, 4, 0),
                Cursor = Cursors.Hand
            };
        }

        private void BuscarVenta()
        {
            string mensaje;
            DataTable detalle = negocio.ObtenerDetalleDevolucion(txtNumeroVenta.Text, out mensaje);
            if (!string.IsNullOrEmpty(mensaje))
            {
                CapaPresentacion.DialogoSistema.Show("No se pudo consultar la factura.\n\n" + mensaje,
                    "Devoluciones", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (detalle.Rows.Count == 0)
            {
                idVentaActual = 0;
                dgvDetalle.DataSource = null;
                lblVenta.Text = "No se encontró una factura con ese número.";
                lblResumen.Text = "0 unidades · Q 0.00";
                nudMontoDevuelto.Value = 0;
                lblMontoOriginal.Text = "Q 0.00";
                lblMontoDevuelto.Text = "Q 0.00";
                lblSaldoFactura.Text = "Q 0.00";
                lblEstado.Text = "No se encontró la factura. Verifica el número e inténtalo de nuevo.";
                return;
            }

            cargandoDetalle = true;
            idVentaActual = Convert.ToInt32(detalle.Rows[0]["IdVenta"]);
            dgvDetalle.DataSource = detalle;
            foreach (DataGridViewRow fila in dgvDetalle.Rows)
                fila.Cells["CantidadDevolver"].Value = 0;

            DateTime fecha = Convert.ToDateTime(detalle.Rows[0]["FechaRegistro"]);
            string numero = Convert.ToString(detalle.Rows[0]["NumeroDocumento"]);
            string cliente = Convert.ToString(detalle.Rows[0]["NombreCliente"]);
            decimal total = Convert.ToDecimal(detalle.Rows[0]["TotalVenta"]);
            decimal devuelto = Convert.ToDecimal(detalle.Rows[0]["MontoDevueltoAcumulado"]);
            lblMontoOriginal.Text = string.Format(CultureInfo.CurrentCulture, "Q {0:N2}", total);
            lblMontoDevuelto.Text = string.Format(CultureInfo.CurrentCulture, "Q {0:N2}", devuelto);
            lblSaldoFactura.Text = string.Format(CultureInfo.CurrentCulture, "Q {0:N2}", Math.Max(0m, total - devuelto));
            lblVenta.Text = string.Format(CultureInfo.CurrentCulture,
                "Factura {0} · {1:dd/MM/yyyy HH:mm}\nCliente: {2}",
                numero, fecha, cliente);
            lblEstado.Text = detalle.Select("CantidadPendiente > 0").Length == 0
                ? "Esta factura ya no tiene cantidades disponibles para devolver."
                : "El reembolso queda como referencia; ajusta el efectivo manualmente en el cierre.";
            cargandoDetalle = false;
            ActualizarResumen();
        }

        private void DgvDetalle_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.RowIndex < 0 || dgvDetalle.Columns[e.ColumnIndex].Name != "CantidadDevolver") return;
            string valor = Convert.ToString(e.FormattedValue).Trim();
            if (valor.Length == 0) valor = "0";
            if (!int.TryParse(valor, out int cantidad) || cantidad < 0 ||
                cantidad > Convert.ToInt32(dgvDetalle.Rows[e.RowIndex].Cells["CantidadPendiente"].Value))
            {
                e.Cancel = true;
                CapaPresentacion.DialogoSistema.Show("Ingresa una cantidad entre cero y lo pendiente de devolver.",
                    "Cantidad inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ActualizarResumen()
        {
            int unidades = 0;
            decimal monto = 0m;
            if (dgvDetalle != null)
            {
                foreach (DataGridViewRow fila in dgvDetalle.Rows)
                {
                    if (fila.IsNewRow || fila.Cells["CantidadDevolver"].Value == null) continue;
                    if (!int.TryParse(Convert.ToString(fila.Cells["CantidadDevolver"].Value), out int cantidad) || cantidad <= 0) continue;
                    unidades += cantidad;
                    monto += cantidad * Convert.ToDecimal(fila.Cells["PrecioVenta"].Value);
                }
            }
            if (lblResumen != null) lblResumen.Text = string.Format(CultureInfo.CurrentCulture, "{0} unidades\nQ {1:N2}", unidades, monto);
            if (nudMontoDevuelto != null)
                nudMontoDevuelto.Value = Math.Min(monto, nudMontoDevuelto.Maximum);
        }

        private void RegistrarAnulacionCompleta()
        {
            if (idVentaActual == 0 || dgvDetalle.DataSource == null) return;
            foreach (DataGridViewRow fila in dgvDetalle.Rows)
                fila.Cells["CantidadDevolver"].Value = Convert.ToInt32(fila.Cells["CantidadPendiente"].Value);
            ActualizarResumen();
            RegistrarDevolucion("ANULACION");
        }

        private void RegistrarDevolucion(string tipo)
        {
            if (idVentaActual == 0 || dgvDetalle.DataSource == null)
            {
                CapaPresentacion.DialogoSistema.Show("Busca primero la factura que deseas corregir.", "Devolución", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!dgvDetalle.EndEdit()) return;
            string motivo = txtMotivo.Text.Trim();
            if (motivo.Length == 0)
            {
                CapaPresentacion.DialogoSistema.Show("Escribe por qué se devuelve el producto o se anula la factura.", "Falta el motivo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMotivo.Focus();
                return;
            }

            DataTable detalle = new DataTable();
            detalle.Columns.Add("IdDetalleVenta", typeof(int));
            detalle.Columns.Add("Cantidad", typeof(int));
            int unidades = 0;
            decimal monto = 0m;
            foreach (DataGridViewRow fila in dgvDetalle.Rows)
            {
                if (!int.TryParse(Convert.ToString(fila.Cells["CantidadDevolver"].Value), out int cantidad) || cantidad <= 0) continue;
                detalle.Rows.Add(Convert.ToInt32(fila.Cells["IdDetalleVenta"].Value), cantidad);
                unidades += cantidad;
                monto += cantidad * Convert.ToDecimal(fila.Cells["PrecioVenta"].Value);
            }
            if (detalle.Rows.Count == 0)
            {
                CapaPresentacion.DialogoSistema.Show("Indica cuántas unidades vas a devolver.", "Sin productos seleccionados", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tipoTexto = tipo == "ANULACION" ? "ANULAR LA PARTE PENDIENTE DE LA FACTURA" : "REGISTRAR LA DEVOLUCIÓN";
            string confirmacion = string.Format(CultureInfo.CurrentCulture,
                "{0}\n\nUnidades: {1}\nValor de los productos: Q {2:N2}\nReembolso real: Q {3:N2}\nMotivo: {4}\n\nEl stock se actualizará. El dinero no se moverá automáticamente; ajusta el cierre manualmente.",
                tipoTexto, unidades, monto, nudMontoDevuelto.Value, motivo.ToUpperInvariant());
            if (CapaPresentacion.DialogoSistema.Show(this, confirmacion, "Confirmar devolución",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

            bool resultado = negocio.RegistrarDevolucion(idVentaActual, usuario.IdUsuario, tipo,
                motivo.ToUpperInvariant(), nudMontoDevuelto.Value, detalle, out decimal montoRegistrado, out string mensaje);
            if (!resultado)
            {
                CapaPresentacion.DialogoSistema.Show("No se aplicó ningún cambio.\n\n" + mensaje,
                    "No se pudo registrar", MessageBoxButtons.OK, MessageBoxIcon.Error);
                BuscarVenta();
                return;
            }

            CapaPresentacion.DialogoSistema.ShowSuccess(
                string.Format(CultureInfo.CurrentCulture,
                    "Movimiento guardado. Stock actualizado.\nMonto a considerar en el cierre: Q {0:N2}.", montoRegistrado),
                "Devolución registrada");
            txtMotivo.Clear();
            BuscarVenta();
        }

        private void CargarHistorial()
        {
            if (dgvHistorial == null) return;
            string mensaje;
            DataTable historial = negocio.ObtenerHistorialDevoluciones(out mensaje);
            historial.DefaultView.Sort = "FechaRegistro DESC, IdRegistro DESC";
            dgvHistorial.DataSource = historial.DefaultView;
            if (!string.IsNullOrEmpty(mensaje))
                CapaPresentacion.DialogoSistema.Show("No se pudo cargar el historial.\n\n" + mensaje,
                    "Historial de devoluciones", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
