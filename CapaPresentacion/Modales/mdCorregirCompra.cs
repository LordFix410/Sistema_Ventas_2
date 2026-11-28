using CapaEntidad;
using CapaNegocio;
using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace CapaPresentacion.Modales
{
    public partial class mdCorregirCompra : Form
    {
        private readonly Usuario _usuario;
        private readonly CN_Compra _compras = new CN_Compra();
        private Compra _compra;
        private bool _actualizandoGrid;
        private DataGridViewTextBoxColumn IdProductoNuevo;
        private DataGridViewTextBoxColumn CodigoNuevo;
        private DataGridViewButtonColumn ProductoNuevo;
        private DataGridViewTextBoxColumn CantidadNueva;
        private DataGridViewTextBoxColumn CostoNuevo;
        private DataGridViewTextBoxColumn TotalNuevo;

        // Constructor vacío requerido por el diseñador de Windows Forms.
        public mdCorregirCompra() : this(null) { }

        public mdCorregirCompra(Usuario usuario)
        {
            _usuario = usuario;
            InitializeComponent();
            txtNumeroCompra.MaxLength = 50;
            txtNumeroFactura.MaxLength = 100;
            txtMotivoCorreccion.MaxLength = 500;
            ConfigurarGrid();
            ConstruirDisenoVisual();
            CargarProveedores();
            LimpiarCompra();
            btnBuscarCompra.Click += (s, e) => BuscarCompra();
            txtNumeroCompra.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    BuscarCompra();
                }
            };
            btnGuardarCorreccion.Click += (s, e) => GuardarCorreccion();
            btnAnularCompra.Click += (s, e) => AnularCompra();
            btnCerrar.Click += (s, e) => Close();
            Shown += (s, e) => txtNumeroCompra.Focus();
        }

        private void ConstruirDisenoVisual()
        {
            Color fondo = Color.FromArgb(27, 34, 39);
            Color panel = Color.FromArgb(39, 48, 55);
            Color campo = Color.FromArgb(51, 62, 70);
            Color texto = Color.FromArgb(248, 250, 252);
            Color tenue = Color.FromArgb(207, 216, 222);
            Color azul = Color.FromArgb(32, 112, 163);

            SuspendLayout();
            Controls.Clear();
            groupBox1.Controls.Remove(txtIdCompra);
            groupBox1.Controls.Remove(cboProveedor);
            groupBox1.Controls.Remove(label2);
            groupBox1.Controls.Remove(txtNumeroFactura);
            groupBox1.Controls.Remove(label3);
            groupBox1.Dispose();
            label6.Dispose();

            Text = "Corregir o anular compra";
            BackColor = fondo;
            ForeColor = texto;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(960, 650);
            MaximizeBox = true;
            MinimizeBox = false;
            ShowIcon = false;

            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            ClientSize = new Size(Math.Min(1400, Math.Max(960, area.Width - 80)),
                Math.Min(900, Math.Max(650, area.Height - 50)));

            txtNumeroCompra.TextAlign = HorizontalAlignment.Left;
            txtNumeroCompra.Font = new Font("Segoe UI", 11F);
            txtNumeroCompra.BackColor = campo;
            txtNumeroCompra.ForeColor = texto;
            txtNumeroCompra.BorderStyle = BorderStyle.FixedSingle;
            txtNumeroCompra.Size = new Size(380, 38);

            txtIdCompra.ReadOnly = true;
            txtIdCompra.Visible = true;
            txtNumeroFactura.ReadOnly = false;
            cboProveedor.DropDownStyle = ComboBoxStyle.DropDownList;
            cboProveedor.FlatStyle = FlatStyle.Flat;

            EstilizarEntrada(txtIdCompra, campo, texto);
            EstilizarEntrada(txtNumeroFactura, campo, texto);
            EstilizarEntrada(cboProveedor, campo, texto);
            cboProveedor.Font = new Font("Segoe UI", 12F, FontStyle.Bold);

            label1.Text = "Compra";
            label1.AutoSize = true;
            label1.ForeColor = texto;
            label1.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            label2.Text = "Proveedor";
            label3.Text = "Número de factura";
            label4.Text = "Motivo de la corrección";
            label7.Text = "Detalle de la compra";
            EstilizarEtiqueta(label2, texto);
            EstilizarEtiqueta(label3, texto);
            EstilizarEtiqueta(label4, texto);
            EstilizarEtiqueta(label7, texto);
            label4.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            label7.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            label2.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);

            EstilizarBoton(btnBuscarCompra, azul, texto);
            EstilizarBoton(btnGuardarCorreccion, azul, texto);
            EstilizarBoton(btnAnularCompra, Color.FromArgb(174, 54, 60), Color.White);
            EstilizarBoton(btnCerrar, Color.FromArgb(70, 83, 91), Color.White);
            btnBuscarCompra.Text = "Buscar compra";
            btnGuardarCorreccion.Text = "Guardar corrección";
            btnAnularCompra.Text = "Anular compra";
            btnCerrar.Text = "Cerrar";

            EstilizarGrid(fondo, campo, texto, tenue, azul);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = fondo,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(16)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 174F));

            var encabezado = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = azul,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(22, 6, 16, 6)
            };
            encabezado.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            encabezado.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230F));
            encabezado.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var titulo = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Corregir o anular compra",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var btnHistorial = new FontAwesome.Sharp.IconButton
            {
                IconChar = FontAwesome.Sharp.IconChar.None,
                Text = "Historial de anulaciones",
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 2, 0, 2)
            };
            EstilizarBoton(btnHistorial, Color.FromArgb(31, 96, 139), Color.White);
            btnHistorial.Click += (s, e) => MostrarHistorialAnulaciones();
            encabezado.Controls.Add(titulo, 0, 0);
            encabezado.Controls.Add(btnHistorial, 1, 0);

            var contenido = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = fondo,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(16, 14, 16, 8)
            };
            contenido.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 86F));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var busqueda = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = fondo,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(0, 4, 0, 4)
            };
            busqueda.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            busqueda.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 390F));
            busqueda.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
            busqueda.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            var ayudaBusqueda = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Busca por Id o número de factura",
                ForeColor = tenue,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            label1.Dock = DockStyle.Fill;
            label1.TextAlign = ContentAlignment.MiddleLeft;
            txtNumeroCompra.Dock = DockStyle.Fill;
            txtNumeroCompra.Margin = new Padding(0, 1, 12, 1);
            btnBuscarCompra.Dock = DockStyle.Fill;
            btnBuscarCompra.Margin = new Padding(0, 0, 10, 0);
            busqueda.Controls.Add(label1, 0, 0);
            busqueda.Controls.Add(txtNumeroCompra, 1, 0);
            busqueda.Controls.Add(btnBuscarCompra, 2, 0);
            busqueda.Controls.Add(ayudaBusqueda, 3, 0);

            var metadatos = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = fondo,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 4, 0, 6)
            };
            metadatos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            metadatos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37.5F));
            metadatos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37.5F));
            metadatos.Controls.Add(CrearCampo(new Label { Text = "Id de compra" }, txtIdCompra, panel, campo, texto, tenue), 0, 0);
            metadatos.Controls.Add(CrearCampo(label2, cboProveedor, panel, campo, texto, tenue), 1, 0);
            metadatos.Controls.Add(CrearCampo(label3, txtNumeroFactura, panel, campo, texto, tenue), 2, 0);

            label7.Dock = DockStyle.Fill;
            label7.TextAlign = ContentAlignment.MiddleLeft;

            dgvDetalleCompra.Dock = DockStyle.Fill;
            dgvDetalleCompra.Margin = new Padding(0);
            contenido.Controls.Add(busqueda, 0, 0);
            contenido.Controls.Add(metadatos, 0, 1);
            contenido.Controls.Add(label7, 0, 2);
            contenido.Controls.Add(dgvDetalleCompra, 0, 3);

            var pie = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = panel,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(16, 10, 16, 8)
            };
            pie.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pie.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            pie.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));

            var motivo = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = panel, ColumnCount = 1, RowCount = 2 };
            motivo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            motivo.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            motivo.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            label4.Dock = DockStyle.Fill;
            label4.TextAlign = ContentAlignment.MiddleLeft;
            txtMotivoCorreccion.Dock = DockStyle.Fill;
            txtMotivoCorreccion.Multiline = true;
            txtMotivoCorreccion.ScrollBars = ScrollBars.Vertical;
            txtMotivoCorreccion.BackColor = campo;
            txtMotivoCorreccion.ForeColor = texto;
            txtMotivoCorreccion.BorderStyle = BorderStyle.FixedSingle;
            txtMotivoCorreccion.Font = new Font("Segoe UI", 10F);
            motivo.Controls.Add(label4, 0, 0);
            motivo.Controls.Add(txtMotivoCorreccion, 0, 1);

            var acciones = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = panel,
                Padding = new Padding(0, 6, 0, 0)
            };
            btnCerrar.Size = new Size(136, 42);
            btnAnularCompra.Size = new Size(164, 42);
            btnGuardarCorreccion.Size = new Size(190, 42);
            btnCerrar.Margin = new Padding(0, 0, 0, 0);
            btnAnularCompra.Margin = new Padding(0, 0, 12, 0);
            btnGuardarCorreccion.Margin = new Padding(0, 0, 12, 0);
            acciones.Controls.Add(btnGuardarCorreccion);
            acciones.Controls.Add(btnAnularCompra);
            acciones.Controls.Add(btnCerrar);
            pie.Controls.Add(motivo, 0, 0);
            pie.Controls.Add(acciones, 0, 1);

            layout.Controls.Add(encabezado, 0, 0);
            layout.Controls.Add(contenido, 0, 1);
            layout.Controls.Add(pie, 0, 2);
            Controls.Add(layout);
            ResumeLayout(true);
        }

        private static Panel CrearCampo(Label etiqueta, Control entrada, Color fondoPanel, Color fondoEntrada, Color colorTexto, Color colorEtiqueta)
        {
            var campo = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = fondoPanel,
                Padding = new Padding(12, 8, 12, 8),
                Margin = new Padding(4, 0, 4, 0)
            };
            var distribucion = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = fondoPanel,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            distribucion.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            distribucion.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            distribucion.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            EstilizarEtiqueta(etiqueta, colorEtiqueta);
            etiqueta.Dock = DockStyle.Fill;
            etiqueta.TextAlign = ContentAlignment.MiddleLeft;
            entrada.Dock = DockStyle.Fill;
            entrada.Margin = new Padding(0, 2, 0, 0);
            EstilizarEntrada(entrada, fondoEntrada, colorTexto);
            distribucion.Controls.Add(etiqueta, 0, 0);
            distribucion.Controls.Add(entrada, 0, 1);
            campo.Controls.Add(distribucion);
            return campo;
        }

        private static void EstilizarEntrada(Control entrada, Color fondo, Color texto)
        {
            entrada.Font = new Font("Segoe UI", 10F);
            entrada.BackColor = fondo;
            entrada.ForeColor = texto;
            if (entrada is TextBox caja)
                caja.BorderStyle = BorderStyle.FixedSingle;
        }

        private static void EstilizarEtiqueta(Label etiqueta, Color texto)
        {
            etiqueta.ForeColor = texto;
            etiqueta.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        }

        private static void EstilizarBoton(FontAwesome.Sharp.IconButton boton, Color fondo, Color texto)
        {
            boton.FlatStyle = FlatStyle.Flat;
            boton.FlatAppearance.BorderSize = 0;
            boton.UseVisualStyleBackColor = false;
            boton.BackColor = fondo;
            boton.ForeColor = texto;
            boton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            boton.TextAlign = ContentAlignment.MiddleCenter;
            boton.Cursor = Cursors.Hand;
            boton.IconColor = texto;
        }

        private void EstilizarGrid(Color fondo, Color campo, Color texto, Color tenue, Color azul)
        {
            dgvDetalleCompra.EnableHeadersVisualStyles = false;
            dgvDetalleCompra.BackgroundColor = fondo;
            dgvDetalleCompra.BorderStyle = BorderStyle.None;
            dgvDetalleCompra.GridColor = Color.FromArgb(76, 90, 99);
            dgvDetalleCompra.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvDetalleCompra.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dgvDetalleCompra.ColumnHeadersHeight = 48;
            dgvDetalleCompra.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvDetalleCompra.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(49, 64, 73);
            dgvDetalleCompra.ColumnHeadersDefaultCellStyle.ForeColor = texto;
            dgvDetalleCompra.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgvDetalleCompra.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
            dgvDetalleCompra.DefaultCellStyle.BackColor = fondo;
            dgvDetalleCompra.DefaultCellStyle.ForeColor = texto;
            dgvDetalleCompra.DefaultCellStyle.SelectionBackColor = azul;
            dgvDetalleCompra.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvDetalleCompra.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            dgvDetalleCompra.DefaultCellStyle.Padding = new Padding(8, 5, 8, 5);
            dgvDetalleCompra.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(46, 57, 65);
            dgvDetalleCompra.RowTemplate.Height = 44;
            dgvDetalleCompra.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvDetalleCompra.AllowUserToResizeRows = false;
            dgvDetalleCompra.SelectionMode = DataGridViewSelectionMode.CellSelect;

            foreach (DataGridViewColumn columna in dgvDetalleCompra.Columns)
            {
                columna.HeaderCell.Style.WrapMode = DataGridViewTriState.True;
                columna.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            }

            foreach (string nombre in new[] { "Codigo", "Producto", "CantidadActual", "CostoActual", "TotalActual" })
            {
                if (dgvDetalleCompra.Columns.Contains(nombre))
                    dgvDetalleCompra.Columns[nombre].DefaultCellStyle.ForeColor = tenue;
            }

            foreach (string nombre in new[] { "CodigoNuevo", "ProductoNuevo", "CantidadNueva", "CostoNuevo", "TotalNuevo" })
            {
                if (dgvDetalleCompra.Columns.Contains(nombre))
                    dgvDetalleCompra.Columns[nombre].DefaultCellStyle.BackColor = campo;
            }

            var selectorProducto = dgvDetalleCompra.Columns["ProductoNuevo"] as DataGridViewButtonColumn;
            if (selectorProducto != null)
            {
                selectorProducto.FlatStyle = FlatStyle.Flat;
                selectorProducto.DefaultCellStyle.BackColor = azul;
                selectorProducto.DefaultCellStyle.ForeColor = Color.White;
                selectorProducto.DefaultCellStyle.SelectionBackColor = azul;
                selectorProducto.DefaultCellStyle.SelectionForeColor = Color.White;
            }
        }

        private void MostrarHistorialAnulaciones()
        {
            DataTable historial;
            try
            {
                historial = _compras.ObtenerHistorialAnulaciones();
            }
            catch (Exception ex)
            {
                Aviso("No se pudo cargar el historial de anulaciones.\n\n" + ex.Message,
                    "Historial de compras", MessageBoxIcon.Warning);
                return;
            }

            Color fondo = Color.FromArgb(27, 34, 39);
            Color panel = Color.FromArgb(39, 48, 55);
            Color texto = Color.FromArgb(248, 250, 252);
            Color tenue = Color.FromArgb(207, 216, 222);
            Color azul = Color.FromArgb(32, 112, 163);
            Color panelEventos = Color.FromArgb(39, 53, 61);
            Color panelDetalle = Color.FromArgb(47, 62, 70);

            using (Form ventana = new Form())
            {
                ventana.Text = "Historial de anulaciones de compras";
                ventana.BackColor = fondo;
                ventana.ForeColor = texto;
                ventana.Font = new Font("Segoe UI", 9F);
                ventana.StartPosition = FormStartPosition.CenterParent;
                ventana.FormBorderStyle = FormBorderStyle.Sizable;
                ventana.MinimizeBox = false;
                ventana.ShowIcon = false;
                ventana.MinimumSize = new Size(900, 600);
                ventana.ClientSize = new Size(1180, 720);

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    BackColor = fondo,
                    ColumnCount = 1,
                    RowCount = 4,
                    Padding = new Padding(16)
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));

                var encabezado = new Panel { Dock = DockStyle.Fill, BackColor = azul, Padding = new Padding(18, 0, 12, 0) };
                var titulo = new Label
                {
                    Dock = DockStyle.Fill,
                    Text = "Historial de anulaciones de compras",
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                encabezado.Controls.Add(titulo);

                var ayuda = new Label
                {
                    Dock = DockStyle.Fill,
                    Text = historial.Rows.Count == 0
                        ? "Todavía no hay compras anuladas o restauradas para mostrar."
                        : "Cada fila conserva fecha, usuario, motivo y compra. Selecciona un evento para revisar las cantidades de inventario.",
                    ForeColor = tenue,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9F)
                };

                var split = new SplitContainer
                {
                    Dock = DockStyle.Fill,
                    Orientation = Orientation.Horizontal,
                    SplitterWidth = 7,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = fondo,
                    Size = new Size(1100, 500)
                };
                split.SplitterDistance = 340;
                split.Panel1MinSize = 160;
                split.Panel2MinSize = 140;
                var eventos = new DataGridView { Dock = DockStyle.Fill };
                ConfigurarGridHistorial(eventos, fondo, panelEventos, texto, azul);
                eventos.DataSource = historial;
                DataGridViewColumn columnaIdCorreccion = BuscarColumnaEnlazada(eventos, "IdCorreccion");
                DataGridViewColumn columnaIdCompra = BuscarColumnaEnlazada(eventos, "IdCompra");
                if (columnaIdCorreccion != null) columnaIdCorreccion.Visible = false;
                if (columnaIdCompra != null) columnaIdCompra.Visible = false;
                ConfigurarColumnaHistorial(eventos, "Fecha", "Fecha y hora", 90, "dd/MM/yyyy HH:mm");
                ConfigurarColumnaHistorial(eventos, "Operacion", "Movimiento", 75);
                ConfigurarColumnaHistorial(eventos, "Compra", "Compra", 55);
                ConfigurarColumnaHistorial(eventos, "FacturaProveedor", "Factura proveedor", 95);
                ConfigurarColumnaHistorial(eventos, "Proveedor", "Proveedor", 125);
                ConfigurarColumnaHistorial(eventos, "Total", "Total", 65, "N2");
                ConfigurarColumnaHistorial(eventos, "Usuario", "Responsable", 115);
                ConfigurarColumnaHistorial(eventos, "Motivo", "Motivo", 155);

                var seccionEventos = CrearPanelGridHistorial("Eventos registrados", eventos, panelEventos, texto);
                split.Panel1.Controls.Add(seccionEventos);

                var detalle = new DataGridView { Dock = DockStyle.Fill };
                ConfigurarGridHistorial(detalle, fondo, panelDetalle, texto, azul);
                var etiquetaDetalle = new Label
                {
                    Dock = DockStyle.Fill,
                    Text = "Detalle de la compra seleccionada",
                    ForeColor = texto,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                var seccionDetalle = CrearPanelGridHistorial(etiquetaDetalle, detalle, panelDetalle, texto);
                split.Panel2.Controls.Add(seccionDetalle);

                if (eventos.Rows.Count > 0)
                {
                    DataGridViewCell primeraCeldaVisible = eventos.Rows[0].Cells
                        .Cast<DataGridViewCell>()
                        .FirstOrDefault(celda => celda.Visible);
                    if (primeraCeldaVisible != null)
                        eventos.CurrentCell = primeraCeldaVisible;
                }
                else
                {
                    etiquetaDetalle.Text = "Todavía no hay anulaciones o restauraciones registradas.";
                }
                eventos.SelectionChanged += (s, e) => CargarDetalleHistorial(eventos, detalle, etiquetaDetalle);
                if (eventos.Rows.Count > 0)
                    CargarDetalleHistorial(eventos, detalle, etiquetaDetalle);

                var pie = new Panel { Dock = DockStyle.Fill, BackColor = fondo, Padding = new Padding(0, 7, 0, 0) };
                var cerrar = new FontAwesome.Sharp.IconButton
                {
                    IconChar = FontAwesome.Sharp.IconChar.None,
                    Text = "Cerrar historial",
                    Size = new Size(170, 42),
                    Dock = DockStyle.Right
                };
                EstilizarBoton(cerrar, Color.FromArgb(70, 83, 91), Color.White);
                cerrar.Click += (s, e) => ventana.Close();
                pie.Controls.Add(cerrar);

                layout.Controls.Add(encabezado, 0, 0);
                layout.Controls.Add(ayuda, 0, 1);
                layout.Controls.Add(split, 0, 2);
                layout.Controls.Add(pie, 0, 3);
                ventana.Controls.Add(layout);
                ventana.AcceptButton = cerrar;
                ventana.ShowDialog(this);
            }
        }

        private static Panel CrearPanelGridHistorial(string titulo, DataGridView grid, Color fondo, Color texto)
        {
            var etiqueta = new Label
            {
                Dock = DockStyle.Fill,
                Text = titulo,
                ForeColor = texto,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            return CrearPanelGridHistorial(etiqueta, grid, fondo, texto);
        }

        private static Panel CrearPanelGridHistorial(Label etiqueta, DataGridView grid, Color fondo, Color texto)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = fondo,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(0, 3, 0, 3)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            etiqueta.ForeColor = texto;
            etiqueta.BackColor = fondo;
            etiqueta.Padding = new Padding(10, 0, 0, 0);
            panel.Controls.Add(etiqueta, 0, 0);
            panel.Controls.Add(grid, 0, 1);
            return panel;
        }

        private static void ConfigurarGridHistorial(DataGridView grid, Color fondo, Color panel, Color texto, Color azul)
        {
            grid.AutoGenerateColumns = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.ReadOnly = true;
            grid.MultiSelect = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = fondo;
            grid.BorderStyle = BorderStyle.None;
            grid.GridColor = Color.FromArgb(76, 90, 99);
            grid.ColumnHeadersHeight = 52;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(49, 64, 73);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = texto;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
            grid.DefaultCellStyle.BackColor = panel;
            grid.DefaultCellStyle.ForeColor = texto;
            grid.DefaultCellStyle.SelectionBackColor = azul;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            grid.DefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(47, 58, 66);
            grid.RowTemplate.Height = 40;
        }

        private static DataGridViewColumn BuscarColumnaEnlazada(DataGridView grid, string nombreCampo)
        {
            return grid.Columns
                .Cast<DataGridViewColumn>()
                .FirstOrDefault(columna =>
                    string.Equals(columna.DataPropertyName, nombreCampo, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(columna.Name, nombreCampo, StringComparison.OrdinalIgnoreCase));
        }

        private static void ConfigurarColumnaHistorial(DataGridView grid, string nombreCampo, string encabezado, float peso, string formato = null)
        {
            DataGridViewColumn columna = BuscarColumnaEnlazada(grid, nombreCampo);
            if (columna == null) return;

            columna.HeaderText = encabezado;
            columna.FillWeight = peso;
            if (!string.IsNullOrEmpty(formato))
                columna.DefaultCellStyle.Format = formato;
        }

        private void CargarDetalleHistorial(DataGridView eventos, DataGridView detalle, Label etiqueta)
        {
            DataRowView registro = eventos.CurrentRow?.DataBoundItem as DataRowView;
            if (registro == null || !registro.Row.Table.Columns.Contains("IdCorreccion") ||
                registro["IdCorreccion"] == DBNull.Value)
            {
                detalle.DataSource = null;
                return;
            }

            try
            {
                int idCorreccion = Convert.ToInt32(registro["IdCorreccion"]);
                string operacion = Convert.ToString(registro["Operacion"]);
                string compra = Convert.ToString(registro["Compra"]);
                etiqueta.Text = "Detalle de " + operacion.ToLowerInvariant() + " · Compra " + compra;
                detalle.DataSource = _compras.ObtenerDetalleHistorialAnulacion(idCorreccion);
                string[] encabezados = { "Codigo|Código", "Producto|Producto", "CantidadAnterior|Cantidad antes", "CantidadNueva|Cantidad después", "CostoAnterior|Costo antes", "CostoNuevo|Costo después", "TotalAnterior|Total antes", "TotalNuevo|Total después" };
                foreach (string mapeo in encabezados)
                {
                    string[] partes = mapeo.Split('|');
                    DataGridViewColumn columna = BuscarColumnaEnlazada(detalle, partes[0]);
                    if (columna != null) columna.HeaderText = partes[1];
                }
                foreach (string nombre in new[] { "CostoAnterior", "CostoNuevo", "TotalAnterior", "TotalNuevo" })
                {
                    DataGridViewColumn columna = BuscarColumnaEnlazada(detalle, nombre);
                    if (columna != null) columna.DefaultCellStyle.Format = "N2";
                }
            }
            catch (Exception ex)
            {
                detalle.DataSource = null;
                etiqueta.Text = "No se pudo cargar el detalle: " + ex.Message;
            }
        }

        private void ConfigurarGrid()
        {
            dgvDetalleCompra.AllowUserToAddRows = false;
            dgvDetalleCompra.AllowUserToDeleteRows = false;
            dgvDetalleCompra.AutoGenerateColumns = false;
            dgvDetalleCompra.MultiSelect = false;
            dgvDetalleCompra.RowHeadersVisible = false;
            dgvDetalleCompra.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgvDetalleCompra.Columns["IdDetalleCompra"].Visible = false;
            dgvDetalleCompra.Columns["IdProducto"].Visible = false;
            dgvDetalleCompra.Columns["Codigo"].HeaderText = "Código actual";
            dgvDetalleCompra.Columns["Producto"].HeaderText = "Producto actual";
            dgvDetalleCompra.Columns["CantidadActual"].HeaderText = "Cantidad actual";
            dgvDetalleCompra.Columns["CostoActual"].HeaderText = "Costo actual";
            dgvDetalleCompra.Columns["TotalActual"].HeaderText = "Total actual";
            dgvDetalleCompra.Columns["Codigo"].ReadOnly = true;
            dgvDetalleCompra.Columns["Producto"].ReadOnly = true;
            dgvDetalleCompra.Columns["CantidadActual"].ReadOnly = true;
            dgvDetalleCompra.Columns["CostoActual"].ReadOnly = true;
            dgvDetalleCompra.Columns["TotalActual"].ReadOnly = true;
            dgvDetalleCompra.Columns["CantidadActual"].DefaultCellStyle.Format = "N0";
            dgvDetalleCompra.Columns["CostoActual"].DefaultCellStyle.Format = "N2";
            dgvDetalleCompra.Columns["TotalActual"].DefaultCellStyle.Format = "N2";

            IdProductoNuevo = new DataGridViewTextBoxColumn { Name = "IdProductoNuevo", HeaderText = "Id nuevo", Visible = false };
            CodigoNuevo = new DataGridViewTextBoxColumn { Name = "CodigoNuevo", HeaderText = "Código nuevo", ReadOnly = true };
            ProductoNuevo = new DataGridViewButtonColumn { Name = "ProductoNuevo", HeaderText = "Producto nuevo", Text = "Elegir producto", UseColumnTextForButtonValue = false };
            CantidadNueva = new DataGridViewTextBoxColumn { Name = "CantidadNueva", HeaderText = "Cantidad nueva" };
            CostoNuevo = new DataGridViewTextBoxColumn { Name = "CostoNuevo", HeaderText = "Costo nuevo" };
            TotalNuevo = new DataGridViewTextBoxColumn { Name = "TotalNuevo", HeaderText = "Total nuevo", ReadOnly = true };
            dgvDetalleCompra.Columns.AddRange(IdProductoNuevo, CodigoNuevo, ProductoNuevo, CantidadNueva, CostoNuevo, TotalNuevo);
            CantidadNueva.DefaultCellStyle.Format = "N0";
            CostoNuevo.DefaultCellStyle.Format = "N2";
            TotalNuevo.DefaultCellStyle.Format = "N2";
            dgvDetalleCompra.Columns["CantidadNueva"].MinimumWidth = 85;
            dgvDetalleCompra.Columns["CostoNuevo"].MinimumWidth = 85;
            dgvDetalleCompra.Columns["ProductoNuevo"].MinimumWidth = 130;
            dgvDetalleCompra.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDetalleCompra.Columns["Codigo"].FillWeight = 75;
            dgvDetalleCompra.Columns["Producto"].FillWeight = 130;
            dgvDetalleCompra.Columns["CantidadActual"].FillWeight = 70;
            dgvDetalleCompra.Columns["CostoActual"].FillWeight = 75;
            dgvDetalleCompra.Columns["TotalActual"].FillWeight = 75;
            CodigoNuevo.FillWeight = 75;
            ProductoNuevo.FillWeight = 110;
            CantidadNueva.FillWeight = 70;
            CostoNuevo.FillWeight = 75;
            TotalNuevo.FillWeight = 75;
            dgvDetalleCompra.CellContentClick += dgvDetalleCompra_CellContentClick;
            dgvDetalleCompra.CellEndEdit += (s, e) => ActualizarTotalFila(e.RowIndex);
            dgvDetalleCompra.CellValueChanged += (s, e) =>
            {
                if (!_actualizandoGrid && (e.ColumnIndex == CantidadNueva.Index || e.ColumnIndex == CostoNuevo.Index))
                    ActualizarTotalFila(e.RowIndex);
            };
            dgvDetalleCompra.DataError += (s, e) => { e.ThrowException = false; };
        }

        private void CargarProveedores()
        {
            var proveedores = new CN_Proveedor().Listar();
            cboProveedor.DisplayMember = "RazonSocial";
            cboProveedor.ValueMember = "IdProveedor";
            cboProveedor.DataSource = proveedores;
            cboProveedor.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private void BuscarCompra()
        {
            string numero = txtNumeroCompra.Text.Trim();
            if (string.IsNullOrWhiteSpace(numero))
            {
                Aviso("Escribe el número interno de compra para buscarla.", "Buscar compra", MessageBoxIcon.Information);
                txtNumeroCompra.Focus();
                return;
            }

            try
            {
                Compra compra = _compras.ObtenerCompraParaCorreccion(numero);
                if (compra == null)
                {
                    LimpiarCompra();
                    Aviso("No se encontró una compra con ese número.", "Compra no encontrada", MessageBoxIcon.Information);
                    return;
                }
                _compra = compra;
                txtIdCompra.Text = compra.IdCompra.ToString(CultureInfo.InvariantCulture);
                txtNumeroFactura.Text = compra.NumeroFacturaProveedor ?? string.Empty;
                if (!cboProveedor.Items.Cast<object>().Any(x => ((Proveedor)x).IdProveedor == compra.oProveedor.IdProveedor))
                {
                    var lista = new CN_Proveedor().Listar();
                    cboProveedor.DataSource = lista;
                }
                cboProveedor.SelectedValue = compra.oProveedor.IdProveedor;
                label2.Text = "Proveedor:";
                label3.Text = "Factura proveedor:";
                label4.Text = compra.Anulada ? "Motivo para restaurar:" : "Motivo de corrección:";
                txtMotivoCorreccion.Text = string.Empty;
                CargarDetalle(compra);
                bool editable = !compra.Anulada;
                dgvDetalleCompra.ReadOnly = !editable;
                cboProveedor.Enabled = editable;
                txtNumeroFactura.ReadOnly = !editable;
                txtMotivoCorreccion.ReadOnly = false;
                btnGuardarCorreccion.Enabled = editable;
                btnAnularCompra.Enabled = true;
                btnAnularCompra.Text = compra.Anulada ? "Restaurar compra" : "Anular compra";
                if (compra.Anulada)
                    Aviso("Esta compra está anulada. Motivo registrado: " + compra.MotivoAnulacion +
                        "\n\nPuedes restaurarla para devolver sus unidades al inventario, dejando un nuevo movimiento de auditoría.",
                        "Compra anulada", MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Aviso("No se pudo cargar la compra.\n\n" + ex.Message, "Error", MessageBoxIcon.Error);
            }
        }

        private void CargarDetalle(Compra compra)
        {
            _actualizandoGrid = true;
            try
            {
                dgvDetalleCompra.Rows.Clear();
                foreach (Detalle_Compra detalle in _compras.ObtenerDetalleCompraParaCorreccion(compra.IdCompra))
                {
                    int fila = dgvDetalleCompra.Rows.Add();
                    DataGridViewRow row = dgvDetalleCompra.Rows[fila];
                    row.Cells["IdDetalleCompra"].Value = detalle.IdDetalleCompra;
                    row.Cells["IdProducto"].Value = detalle.IdProducto;
                    row.Cells["Codigo"].Value = detalle.oProducto.Codigo;
                    row.Cells["Producto"].Value = detalle.oProducto.Nombre;
                    row.Cells["CantidadActual"].Value = detalle.Cantidad;
                    row.Cells["CostoActual"].Value = detalle.PrecioCompra;
                    row.Cells["TotalActual"].Value = detalle.MontoTotal;
                    row.Cells["IdProductoNuevo"].Value = detalle.IdProducto;
                    row.Cells["CodigoNuevo"].Value = detalle.oProducto.Codigo;
                    row.Cells["ProductoNuevo"].Value = detalle.oProducto.Nombre;
                    row.Cells["CantidadNueva"].Value = detalle.Cantidad;
                    row.Cells["CostoNuevo"].Value = detalle.PrecioCompra;
                    row.Cells["TotalNuevo"].Value = detalle.MontoTotal;
                }
            }
            finally { _actualizandoGrid = false; }
        }

        private void dgvDetalleCompra_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != ProductoNuevo.Index || _compra == null || _compra.Anulada) return;
            using (var modal = new mdProducto(false))
            {
                if (modal.ShowDialog(this) != DialogResult.OK || modal._Producto == null) return;
                DataGridViewRow row = dgvDetalleCompra.Rows[e.RowIndex];
                row.Cells["IdProductoNuevo"].Value = modal._Producto.IdProducto;
                row.Cells["CodigoNuevo"].Value = modal._Producto.Codigo;
                row.Cells["ProductoNuevo"].Value = modal._Producto.Nombre;
            }
        }

        private void ActualizarTotalFila(int fila)
        {
            if (_actualizandoGrid || fila < 0 || fila >= dgvDetalleCompra.Rows.Count) return;
            DataGridViewRow row = dgvDetalleCompra.Rows[fila];
            if (int.TryParse(Convert.ToString(row.Cells["CantidadNueva"].Value), out int cantidad) && cantidad > 0 &&
                decimal.TryParse(Convert.ToString(row.Cells["CostoNuevo"].Value), NumberStyles.Number,
                    CultureInfo.CurrentCulture, out decimal costo) && costo >= 0)
                row.Cells["TotalNuevo"].Value = Math.Round(cantidad * costo, 2, MidpointRounding.AwayFromZero);
            else
                row.Cells["TotalNuevo"].Value = null;
        }

        private DataTable CrearDetalleCorreccion()
        {
            var tabla = new DataTable();
            tabla.Columns.Add("IdDetalleCompra", typeof(int));
            tabla.Columns.Add("IdProductoActual", typeof(int));
            tabla.Columns.Add("IdProductoNuevo", typeof(int));
            tabla.Columns.Add("CantidadActual", typeof(int));
            tabla.Columns.Add("CantidadNueva", typeof(int));
            tabla.Columns.Add("PrecioCompraActual", typeof(decimal));
            tabla.Columns.Add("PrecioCompraNuevo", typeof(decimal));
            foreach (DataGridViewRow row in dgvDetalleCompra.Rows)
            {
                if (!int.TryParse(Convert.ToString(row.Cells["IdDetalleCompra"].Value), out int idDetalle) ||
                    !int.TryParse(Convert.ToString(row.Cells["IdProducto"].Value), out int idProductoActual) ||
                    !int.TryParse(Convert.ToString(row.Cells["IdProductoNuevo"].Value), out int idProductoNuevo) ||
                    !int.TryParse(Convert.ToString(row.Cells["CantidadActual"].Value), out int cantidadActual) ||
                    !int.TryParse(Convert.ToString(row.Cells["CantidadNueva"].Value), out int cantidadNueva) ||
                    !decimal.TryParse(Convert.ToString(row.Cells["CostoActual"].Value), NumberStyles.Number, CultureInfo.CurrentCulture, out decimal costoActual) ||
                    !decimal.TryParse(Convert.ToString(row.Cells["CostoNuevo"].Value), NumberStyles.Number, CultureInfo.CurrentCulture, out decimal costoNuevo))
                    throw new InvalidOperationException("Revisa los datos actual y nuevo de cada renglón.");
                if (cantidadNueva <= 0 || costoNuevo < 0)
                    throw new InvalidOperationException("La cantidad nueva debe ser mayor que cero y el costo no puede ser negativo.");
                tabla.Rows.Add(idDetalle,idProductoActual,idProductoNuevo,cantidadActual,cantidadNueva,costoActual,costoNuevo);
            }
            if (tabla.Rows.Count == 0) throw new InvalidOperationException("La compra no tiene renglones cargados.");
            return tabla;
        }

        private void GuardarCorreccion()
        {
            if (_usuario == null || _usuario.IdUsuario <= 0)
            {
                Aviso("No se identificó al usuario que realizará la corrección.", "Usuario requerido", MessageBoxIcon.Warning);
                return;
            }
            if (_compra == null || _compra.Anulada) return;
            if (string.IsNullOrWhiteSpace(txtMotivoCorreccion.Text))
            {
                Aviso("Escribe por qué se corrige esta compra para dejarlo en auditoría.", "Motivo requerido", MessageBoxIcon.Warning);
                txtMotivoCorreccion.Focus();
                return;
            }
            if (cboProveedor.SelectedValue == null || !int.TryParse(Convert.ToString(cboProveedor.SelectedValue), out int idProveedor))
            {
                Aviso("Selecciona el proveedor correcto.", "Proveedor requerido", MessageBoxIcon.Warning);
                return;
            }

            try
            {
                dgvDetalleCompra.EndEdit();
                DataTable detalle = CrearDetalleCorreccion();
                decimal totalCorregido = detalle.AsEnumerable().Sum(r =>
                    Math.Round(r.Field<decimal>("PrecioCompraNuevo") * r.Field<int>("CantidadNueva"), 2, MidpointRounding.AwayFromZero));
                decimal diferencia = _compra.MontoTotal - _compra.MontoNoInventariable - totalCorregido;
                if (Math.Abs(diferencia) > 1.00m)
                {
                    Aviso("Los renglones corregidos suman Q " + totalCorregido.ToString("N2") +
                        ", pero la factura es Q " + _compra.MontoTotal.ToString("N2") +
                        ". La diferencia supera la tolerancia de Q1.00. Revisa cantidades y costos.",
                        "La compra no cuadra", MessageBoxIcon.Warning);
                    return;
                }
                if (CapaPresentacion.DialogoSistema.Show(this,
                    "Compra interna: " + _compra.NumeroDocumento + "\n" +
                    "Factura del proveedor: " + (txtNumeroFactura.Text.Trim().Length == 0 ? "(sin referencia)" : txtNumeroFactura.Text.Trim()) + "\n" +
                    "Nuevo total de productos: Q " + totalCorregido.ToString("N2") + "\n" +
                    "Motivo: " + txtMotivoCorreccion.Text.Trim() + "\n\n¿Guardar la corrección de inventario?",
                    "Confirmar corrección", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

                string mensaje;
                bool ok = _compras.CorregirCompra(_compra.IdCompra,_usuario.IdUsuario,idProveedor,
                    txtNumeroFactura.Text.Trim(),txtMotivoCorreccion.Text,detalle,out mensaje);
                Aviso(mensaje, ok ? "Compra corregida" : "No se pudo corregir", ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                if (ok) BuscarCompra();
            }
            catch (Exception ex)
            {
                Aviso(ex.Message, "Revisa la corrección", MessageBoxIcon.Warning);
            }
        }

        private void AnularCompra()
        {
            if (_usuario == null || _usuario.IdUsuario <= 0 || _compra == null) return;
            if (string.IsNullOrWhiteSpace(txtMotivoCorreccion.Text))
            {
                Aviso(_compra.Anulada
                    ? "Escribe por qué se restaura esta compra para dejarlo en auditoría."
                    : "Escribe el motivo de la anulación para dejarlo en auditoría.",
                    "Motivo requerido", MessageBoxIcon.Warning);
                txtMotivoCorreccion.Focus();
                return;
            }
            if (_compra.Anulada)
            {
                if (CapaPresentacion.DialogoSistema.Show(this,
                    "Se restaurará la compra " + _compra.NumeroDocumento + " y sus unidades volverán al inventario.\n" +
                    "Motivo: " + txtMotivoCorreccion.Text.Trim() + "\n\n¿Continuar?",
                    "Confirmar restauración", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                string mensajeRestauracion;
                bool restaurada = _compras.ReactivarCompra(_compra.IdCompra,_usuario.IdUsuario,
                    txtMotivoCorreccion.Text,out mensajeRestauracion);
                Aviso(mensajeRestauracion, restaurada ? "Compra restaurada" : "No se pudo restaurar",
                    restaurada ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                if (restaurada) BuscarCompra();
                return;
            }
            if (CapaPresentacion.DialogoSistema.Show(this,
                "Se anulará la compra " + _compra.NumeroDocumento + " y se restarán sus unidades del inventario. " +
                "La operación quedará auditada y no se podrá deshacer desde este formulario.\n\n¿Continuar?",
                "Confirmar anulación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            string mensaje;
            bool ok = _compras.AnularCompra(_compra.IdCompra,_usuario.IdUsuario,txtMotivoCorreccion.Text,out mensaje);
            Aviso(mensaje, ok ? "Compra anulada" : "No se pudo anular", ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            if (ok) BuscarCompra();
        }

        private void LimpiarCompra()
        {
            _compra = null;
            txtIdCompra.Clear();
            txtNumeroFactura.Clear();
            txtMotivoCorreccion.Clear();
            dgvDetalleCompra.Rows.Clear();
            btnGuardarCorreccion.Enabled = false;
            btnAnularCompra.Enabled = false;
            btnAnularCompra.Text = "Anular compra";
            cboProveedor.Enabled = false;
        }

        private static void Aviso(string mensaje, string titulo, MessageBoxIcon icono)
        {
            CapaPresentacion.DialogoSistema.Show(mensaje, titulo, MessageBoxButtons.OK, icono);
        }

        private void btnGuardarCorreccion_Click(object sender, EventArgs e)
        {

        }

        private void btnAnularCompra_Click(object sender, EventArgs e)
        {

        }

        private void btnCerrar_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void txtMotivoCorreccion_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnBuscarCompra_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void txtNumeroCompra_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
