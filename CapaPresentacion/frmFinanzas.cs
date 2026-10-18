using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using CapaEntidad;
using CapaNegocio;

namespace CapaPresentacion
{
    /// <summary>
    /// Ventana independiente para cierre de caja y resumen financiero.
    /// </summary>
    public class frmFinanzas : Form
    {
        private readonly Color colorAzul = Color.SteelBlue;
        private readonly Color colorFondo = Color.FromArgb(242, 245, 248);

        private DateTimePicker dtpDesde;
        private DateTimePicker dtpHasta;
        private Button btnConsultar;
        private Label lblGastosMensuales;
        private Label lblMetaVentaMensual;
        private Label lblMetaVentaDiaria;
        private Label lblVentaAcumuladaHoy;
        private NumericUpDown nudMargenMeta;
        private ProgressBar prgMetaDiaria;

        private DataGridView dgvFacturasPendientes;
        private Label lblCapitalComprometido;
        private TextBox txtFondoInicial;
        private TextBox txtEfectivoEsperado;
        private TextBox txtEfectivoContado;
        private TextBox txtDiferencia;
        private Button btnCalcularCierre;
        private Button btnRegistrarCierre;

        private Label lblCostoProductos;
        private Label lblReservaCapital;
        private Label lblCapitalADepositar;
        private Label lblGananciaADepositar;
        private TextBox txtCapitalDepositado;
        private TextBox txtGananciaDepositada;
        private TextBox txtReferenciaDeposito;
        private ComboBox cboCierreDeposito;
        private Button btnRegistrarDepositos;

        private ComboBox cboTipoMovimiento;
        private ComboBox cboFacturaProveedor;
        private ComboBox cboObligacionPersonal;
        private TextBox txtMontoMovimiento;
        private TextBox txtMotivoMovimiento;
        private ComboBox cboMedioPago;
        private DateTimePicker dtpFechaMovimiento;
        private Button btnRegistrarMovimiento;
        private DataGridView dgvMovimientos;

        private Label lblCapitalDisponible;
        private Label lblGananciaAcumulada;
        private Label lblDeudaProveedores;
        private Label lblDeudaGastosPersonales;
        private readonly Usuario usuarioActual;
        private readonly CN_Finanzas finanzas = new CN_Finanzas();
        private int ultimoIdCierre;
        private int idCierreDeposito;
        private decimal capitalCalculado;
        private decimal gananciaCalculada;
        private decimal efectivoEsperadoCalculado;
        private decimal gastosMensualesActuales;
        private decimal ventaHoyActual;

        // Allows Visual Studio to instantiate the form in its WinForms Designer.
        public frmFinanzas() : this(null)
        {
        }

        public frmFinanzas(Usuario usuario)
        {
            usuarioActual = usuario;
            InicializarInterfaz();
            cboTipoMovimiento_SelectedIndexChanged(cboTipoMovimiento, EventArgs.Empty);
            Load += frmFinanzas_Load;
            cboCierreDeposito.SelectedIndexChanged += cboCierreDeposito_SelectedIndexChanged;
            btnConsultar.Click += delegate { CargarPanel(); };
            btnCalcularCierre.Click += delegate { CalcularCierre(); };
            btnRegistrarCierre.Click += delegate { RegistrarCierre(); };
            btnRegistrarDepositos.Click += delegate { RegistrarDeposito(); };
            btnRegistrarMovimiento.Click += delegate { RegistrarMovimiento(); };
        }

        private void InicializarInterfaz()
        {
            Text = "Control financiero";
            StartPosition = FormStartPosition.Manual;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = true;
            MaximizeBox = true;
            MinimumSize = new Size(900, 620);
            Size = new Size(1480, 960);
            BackColor = colorFondo;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            var encabezado = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 48,
                BackColor = colorAzul
            };
            encabezado.Controls.Add(new Label
            {
                Text = "Control financiero y cierre de caja",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0)
            });

            var contenido = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = colorFondo,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(10)
            };
            contenido.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 108F));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 270F));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 220F));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 285F));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));

            contenido.Controls.Add(CrearPanelPeriodo(), 0, 0);
            contenido.Controls.Add(CrearPanelMetaDiaria(), 0, 1);
            contenido.Controls.Add(CrearPanelFacturasYCierre(), 0, 2);
            contenido.Controls.Add(CrearPanelDepositos(), 0, 3);
            contenido.Controls.Add(CrearPanelMovimientos(), 0, 4);
            contenido.Controls.Add(CrearPanelSaldos(), 0, 5);

            var distribucionPrincipal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            distribucionPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            distribucionPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            distribucionPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            distribucionPrincipal.Controls.Add(encabezado, 0, 0);
            distribucionPrincipal.Controls.Add(contenido, 0, 1);
            Controls.Add(distribucionPrincipal);
        }

        private Control CrearPanelPeriodo()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(5, 8, 0, 0),
                BackColor = Color.White
            };

            panel.Controls.Add(CrearEtiqueta("Desde:", 58));
            dtpDesde = new DateTimePicker { Width = 135, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            panel.Controls.Add(dtpDesde);
            panel.Controls.Add(CrearEtiqueta("Hasta:", 58));
            dtpHasta = new DateTimePicker { Width = 135, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            panel.Controls.Add(dtpHasta);

            btnConsultar = CrearBoton("Consultar", 105);
            panel.Controls.Add(btnConsultar);
            return panel;
        }

        private Control CrearPanelMetaDiaria()
        {
            var grupo = CrearGrupo("Meta de ventas para cubrir gastos personales");
            var tabla = CrearTabla(6, 2);
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125F));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 205F));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 235F));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tabla.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tabla.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            tabla.Controls.Add(CrearEtiqueta("Gastos personales / mes:", 0), 0, 0);
            lblGastosMensuales = CrearValor("Q 0.00");
            tabla.Controls.Add(lblGastosMensuales, 1, 0);
            tabla.Controls.Add(CrearEtiqueta("Margen bruto planificado (%):", 0), 2, 0);
            nudMargenMeta = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 100,
                Value = 60,
                Width = 70,
                TextAlign = HorizontalAlignment.Right,
                Anchor = AnchorStyles.Left
            };
            nudMargenMeta.ValueChanged += delegate { ActualizarMetaDiaria(); };
            tabla.Controls.Add(nudMargenMeta, 3, 0);
            lblMetaVentaMensual = CrearValor("Meta mensual de ventas: Q 0.00");
            tabla.Controls.Add(lblMetaVentaMensual, 4, 0);

            tabla.Controls.Add(CrearEtiqueta("Meta de venta diaria:", 0), 0, 1);
            lblMetaVentaDiaria = CrearValor("Q 0.00");
            tabla.Controls.Add(lblMetaVentaDiaria, 1, 1);
            tabla.Controls.Add(CrearEtiqueta("Ventas de hoy:", 0), 2, 1);
            lblVentaAcumuladaHoy = CrearValor("Q 0.00");
            tabla.Controls.Add(lblVentaAcumuladaHoy, 3, 1);

            prgMetaDiaria = new ProgressBar { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100, Value = 0, Margin = new Padding(8, 6, 4, 6) };
            tabla.Controls.Add(prgMetaDiaria, 4, 1);
            tabla.SetColumnSpan(prgMetaDiaria, 2);
            grupo.Controls.Add(tabla);
            return grupo;
        }

        private void ActualizarMetaDiaria()
        {
            if (nudMargenMeta == null) return;

            decimal margenBruto = nudMargenMeta.Value / 100m;
            // Del margen bruto, 15% se reserva para capital; el restante cubre gastos personales.
            decimal margenDisponible = margenBruto * 0.85m;
            decimal metaMensual = margenDisponible <= 0 ? 0 : gastosMensualesActuales / margenDisponible;

            DateTime primerDia = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            int diasMes = DateTime.DaysInMonth(primerDia.Year, primerDia.Month);
            decimal diasEquivalentes = 0;
            for (int dia = 0; dia < diasMes; dia++)
            {
                DayOfWeek semana = primerDia.AddDays(dia).DayOfWeek;
                if (semana == DayOfWeek.Saturday) diasEquivalentes += 0.5m;
                else if (semana != DayOfWeek.Sunday) diasEquivalentes += 1m;
            }

            decimal metaDiaria = diasEquivalentes <= 0 ? 0 : metaMensual / diasEquivalentes;
            lblMetaVentaMensual.Text = "Meta mensual: " + FormatearQ(metaMensual) + " | Días efectivos: " + diasEquivalentes.ToString("0.#");
            lblMetaVentaDiaria.Text = FormatearQ(metaDiaria);
            lblVentaAcumuladaHoy.Text = FormatearQ(ventaHoyActual);
            prgMetaDiaria.Value = metaDiaria <= 0 ? 0 : (int)Math.Min(100, Math.Round(ventaHoyActual * 100m / metaDiaria));
        }

        private Control CrearPanelFacturasYCierre()
        {
            var contenedor = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = colorFondo,
                Margin = new Padding(0)
            };
            contenedor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            contenedor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            var grupoFacturas = CrearGrupo("Facturas de proveedores pendientes / próximas a vencer");
            var tablaFacturas = CrearTabla(1, 2);
            tablaFacturas.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tablaFacturas.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            lblCapitalComprometido = CrearValor("Capital comprometido en proveedores: Q 0.00");
            tablaFacturas.Controls.Add(lblCapitalComprometido, 0, 0);
            dgvFacturasPendientes = CrearGrid();
            AgregarColumna(dgvFacturasPendientes, "FechaVencimiento", "Vence", 110);
            AgregarColumna(dgvFacturasPendientes, "Proveedor", "Proveedor", 190);
            AgregarColumna(dgvFacturasPendientes, "NumeroDocumento", "No. factura", 120);
            AgregarColumna(dgvFacturasPendientes, "SaldoPendiente", "Saldo pendiente", 125);
            AgregarColumna(dgvFacturasPendientes, "DiasRestantes", "Días", 65);
            tablaFacturas.Controls.Add(dgvFacturasPendientes, 0, 1);
            grupoFacturas.Controls.Add(tablaFacturas);
            contenedor.Controls.Add(grupoFacturas, 0, 0);

            var grupoCierre = CrearGrupo("Cierre de caja");
            var tablaCierre = CrearTabla(2, 6);
            tablaCierre.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            tablaCierre.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            for (int i = 0; i < 6; i++) tablaCierre.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66F));

            txtFondoInicial = CrearCaja(false);
            txtEfectivoEsperado = CrearCaja(true);
            txtEfectivoContado = CrearCaja(false);
            txtDiferencia = CrearCaja(true);
            AgregarFila(tablaCierre, 0, "Fondo inicial:", txtFondoInicial);
            AgregarFila(tablaCierre, 1, "Efectivo esperado:", txtEfectivoEsperado);
            AgregarFila(tablaCierre, 2, "Efectivo contado:", txtEfectivoContado);
            AgregarFila(tablaCierre, 3, "Diferencia:", txtDiferencia);

            var regla = new Label
            {
                Text = "Si falta dinero, se descuenta primero de la ganancia disponible y luego del capital.",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(70, 70, 70),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            tablaCierre.Controls.Add(regla, 0, 4);
            tablaCierre.SetColumnSpan(regla, 2);

            var botones = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            btnRegistrarCierre = CrearBoton("Registrar cierre", 135);
            btnCalcularCierre = CrearBoton("Calcular", 105);
            botones.Controls.Add(btnRegistrarCierre);
            botones.Controls.Add(btnCalcularCierre);
            tablaCierre.Controls.Add(botones, 0, 5);
            tablaCierre.SetColumnSpan(botones, 2);

            grupoCierre.Controls.Add(tablaCierre);
            contenedor.Controls.Add(grupoCierre, 1, 0);
            return contenedor;
        }

        private Control CrearPanelDepositos()
        {
            var grupo = CrearGrupo("Distribución del cierre y depósitos");
            var tabla = CrearTabla(3, 7);
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 470F));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250F));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 7; i++) tabla.RowStyles.Add(new RowStyle(SizeType.Percent, 14.28F));

            tabla.Controls.Add(CrearEtiqueta("Importe calculado", 0), 1, 0);
            tabla.Controls.Add(CrearEtiqueta("Importe depositado", 0), 2, 0);
            lblCostoProductos = CrearValor("Q 0.00");
            lblReservaCapital = CrearValor("Q 0.00");
            lblCapitalADepositar = CrearValor("Q 0.00");
            lblGananciaADepositar = CrearValor("Q 0.00");
            AgregarFilaDistribucion(tabla, 1, "Costo de productos vendidos:", lblCostoProductos, null);
            AgregarFilaDistribucion(tabla, 2, "Reserva adicional para capital (15%):", lblReservaCapital, null);
            AgregarFilaDistribucion(tabla, 3, "Capital a depositar:", lblCapitalADepositar, txtCapitalDepositado = CrearCaja(false));
            AgregarFilaDistribucion(tabla, 4, "Ganancia a depositar:", lblGananciaADepositar, txtGananciaDepositada = CrearCaja(false));
            ConfigurarCajaDeposito(txtCapitalDepositado);
            ConfigurarCajaDeposito(txtGananciaDepositada);

            tabla.Controls.Add(CrearEtiqueta("Cierre a depositar:", 0), 0, 5);
            cboCierreDeposito = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            tabla.Controls.Add(cboCierreDeposito, 1, 5);
            tabla.SetColumnSpan(cboCierreDeposito, 2);

            tabla.Controls.Add(CrearEtiqueta("Referencia / comprobante:", 0), 0, 6);
            txtReferenciaDeposito = CrearCaja(false);
            tabla.Controls.Add(txtReferenciaDeposito, 1, 6);
            btnRegistrarDepositos = CrearBoton("Registrar depósitos", 155);
            tabla.Controls.Add(btnRegistrarDepositos, 2, 6);

            grupo.Controls.Add(tabla);
            return grupo;
        }

        private Control CrearPanelMovimientos()
        {
            var grupo = CrearGrupo("Gastos y pagos registrados");
            var tabla = CrearTabla(1, 2);
            tabla.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            tabla.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var entradas = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(2, 7, 2, 0)
            };

            entradas.Controls.Add(CrearEtiqueta("Tipo:", 52));
            cboTipoMovimiento = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 155 };
            cboTipoMovimiento.Items.AddRange(new object[] { "Gasto de empresa", "Gasto personal", "Pago a proveedor" });
            cboTipoMovimiento.SelectedIndex = 0;
            cboTipoMovimiento.SelectedIndexChanged += cboTipoMovimiento_SelectedIndexChanged;
            entradas.Controls.Add(cboTipoMovimiento);

            cboFacturaProveedor = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 205, Enabled = false, Visible = false };
            cboFacturaProveedor.Items.Add("Seleccionar factura pendiente");
            cboFacturaProveedor.SelectedIndex = 0;
            entradas.Controls.Add(cboFacturaProveedor);

            cboObligacionPersonal = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 205, Enabled = false, Visible = false };
            cboObligacionPersonal.Items.Add("Seleccionar gasto personal pendiente");
            cboObligacionPersonal.SelectedIndex = 0;
            entradas.Controls.Add(cboObligacionPersonal);

            entradas.Controls.Add(CrearEtiqueta("Monto:", 58));
            txtMontoMovimiento = CrearCaja(false);
            txtMontoMovimiento.Width = 100;
            entradas.Controls.Add(txtMontoMovimiento);
            cboMedioPago = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 118 };
            cboMedioPago.Items.AddRange(new object[] { "Efectivo", "Transferencia", "Tarjeta", "Otro" });
            cboMedioPago.SelectedIndex = 0;
            entradas.Controls.Add(cboMedioPago);
            entradas.Controls.Add(CrearEtiqueta("Concepto:", 78));
            txtMotivoMovimiento = CrearCaja(false);
            txtMotivoMovimiento.Width = 205;
            entradas.Controls.Add(txtMotivoMovimiento);
            dtpFechaMovimiento = new DateTimePicker { Width = 150, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", ShowUpDown = true, Value = DateTime.Now };
            entradas.Controls.Add(dtpFechaMovimiento);
            btnRegistrarMovimiento = CrearBoton("Registrar pago", 125);
            entradas.Controls.Add(btnRegistrarMovimiento);
            tabla.Controls.Add(entradas, 0, 0);

            dgvMovimientos = CrearGrid();
            AgregarColumna(dgvMovimientos, "Fecha", "Fecha", 130);
            AgregarColumna(dgvMovimientos, "Tipo", "Tipo", 145);
            AgregarColumna(dgvMovimientos, "Concepto", "Concepto", 250);
            AgregarColumna(dgvMovimientos, "Factura", "Factura relacionada", 180);
            AgregarColumna(dgvMovimientos, "Monto", "Monto", 110);
            AgregarColumna(dgvMovimientos, "Responsable", "Responsable", 150);
            tabla.Controls.Add(dgvMovimientos, 0, 1);
            grupo.Controls.Add(tabla);
            return grupo;
        }

        private Control CrearPanelSaldos()
        {
            var tabla = CrearTabla(4, 1);
            tabla.BackColor = Color.White;
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            lblCapitalDisponible = CrearSaldo("Capital disponible", "Q 0.00");
            lblGananciaAcumulada = CrearSaldo("Ganancia acumulada", "Q 0.00");
            lblDeudaProveedores = CrearSaldo("Deuda con proveedores", "Q 0.00");
            lblDeudaGastosPersonales = CrearSaldo("Deuda de gastos personales", "Q 0.00");
            tabla.Controls.Add(lblCapitalDisponible, 0, 0);
            tabla.Controls.Add(lblGananciaAcumulada, 1, 0);
            tabla.Controls.Add(lblDeudaProveedores, 2, 0);
            tabla.Controls.Add(lblDeudaGastosPersonales, 3, 0);
            return tabla;
        }

        private GroupBox CrearGrupo(string texto)
        {
            return new GroupBox
            {
                Text = texto,
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(35, 55, 75),
                Padding = new Padding(8),
                Margin = new Padding(4)
            };
        }

        private TableLayoutPanel CrearTabla(int columnas, int filas)
        {
            return new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = columnas,
                RowCount = filas,
                Padding = new Padding(5),
                BackColor = Color.White,
                Margin = new Padding(0)
            };
        }

        private DataGridView CrearGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                MultiSelect = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoGenerateColumns = false,
                ColumnHeadersHeight = 34,
                RowTemplate = { Height = 26 },
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(54, 96, 126),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                },
                DefaultCellStyle =
                {
                    BackColor = Color.White,
                    ForeColor = Color.Black,
                    SelectionBackColor = Color.FromArgb(215, 232, 245),
                    SelectionForeColor = Color.Black
                }
            };
        }

        private void AgregarColumna(DataGridView grid, string nombre, string titulo, int ancho)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nombre,
                DataPropertyName = nombre,
                HeaderText = titulo,
                MinimumWidth = 60,
                Width = ancho,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
        }

        private Label CrearEtiqueta(string texto, int ancho)
        {
            return new Label
            {
                Text = texto,
                Width = ancho > 0 ? ancho : 0,
                AutoSize = ancho <= 0,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(4, 0, 4, 0),
                ForeColor = Color.Black
            };
        }

        private Label CrearValor(string texto)
        {
            return new Label
            {
                Text = texto,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(35, 55, 75)
            };
        }

        private TextBox CrearCaja(bool soloLectura)
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = soloLectura,
                Text = soloLectura ? "Q 0.00" : string.Empty,
                TextAlign = HorizontalAlignment.Right,
                Margin = new Padding(3, 3, 3, 3)
            };
        }

        private Button CrearBoton(string texto, int ancho)
        {
            return new Button
            {
                Text = texto,
                Width = ancho,
                Height = 32,
                BackColor = Color.FromArgb(226, 236, 244),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.Black,
                Margin = new Padding(4, 0, 4, 0)
            };
        }

        private Label CrearSaldo(string titulo, string monto)
        {
            return new Label
            {
                Text = titulo + Environment.NewLine + monto,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(35, 55, 75),
                Margin = new Padding(3),
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private void AgregarFila(TableLayoutPanel tabla, int fila, string texto, Control control)
        {
            tabla.Controls.Add(CrearEtiqueta(texto, 0), 0, fila);
            tabla.Controls.Add(control, 1, fila);
        }

        private void AgregarFilaDistribucion(TableLayoutPanel tabla, int fila, string texto, Control calculado, Control depositado)
        {
            tabla.Controls.Add(CrearEtiqueta(texto, 0), 0, fila);
            tabla.Controls.Add(calculado, 1, fila);
            if (depositado != null)
                tabla.Controls.Add(depositado, 2, fila);
        }

        private static void ConfigurarCajaDeposito(TextBox caja)
        {
            caja.Dock = DockStyle.None;
            caja.Width = 170;
            caja.Height = 24;
            caja.Anchor = AnchorStyles.Left;
            caja.TextAlign = HorizontalAlignment.Right;
        }

        private void cboTipoMovimiento_SelectedIndexChanged(object sender, EventArgs e)
        {
            string tipo = cboTipoMovimiento.SelectedItem == null ? string.Empty : cboTipoMovimiento.SelectedItem.ToString();
            cboFacturaProveedor.Enabled = tipo == "Pago a proveedor";
            cboObligacionPersonal.Enabled = tipo == "Gasto personal";
            cboFacturaProveedor.Visible = tipo == "Pago a proveedor";
            cboObligacionPersonal.Visible = tipo == "Gasto personal";
        }

        private void frmFinanzas_Load(object sender, EventArgs e)
        {
            CargarPanel();
        }

        private void CargarPanel()
        {
            try
            {
                DateTime desde = dtpDesde.Value.Date;
                DateTime hasta = dtpHasta.Value.Date.AddDays(1);
                DataSet ds = finanzas.ObtenerPanel(desde, hasta);
                dgvFacturasPendientes.DataSource = ds.Tables[0];
                dgvMovimientos.DataSource = ds.Tables[5];
                FormatearColumnas();
                decimal comprometido = 0;
                foreach (DataRow factura in ds.Tables[0].Rows)
                    comprometido += Convert.ToDecimal(factura["SaldoPendiente"]);
                lblCapitalComprometido.Text = "Capital comprometido en proveedores: " + FormatearQ(comprometido);

                DataRow meta = ds.Tables[1].Rows[0];
                gastosMensualesActuales = Convert.ToDecimal(meta["GastosMensuales"]);
                ventaHoyActual = Convert.ToDecimal(meta["VentaHoy"]);
                lblGastosMensuales.Text = FormatearQ(gastosMensualesActuales);
                ActualizarMetaDiaria();

                DataRow saldos = ds.Tables[2].Rows[0];
                lblCapitalDisponible.Text = "Capital disponible" + Environment.NewLine + FormatearQ(Convert.ToDecimal(saldos["CapitalDisponible"]));
                lblGananciaAcumulada.Text = "Ganancia acumulada" + Environment.NewLine + FormatearQ(Convert.ToDecimal(saldos["GananciaAcumulada"]));
                lblDeudaProveedores.Text = "Deuda con proveedores" + Environment.NewLine + FormatearQ(Convert.ToDecimal(saldos["DeudaProveedores"]));
                lblDeudaGastosPersonales.Text = "Deuda de gastos personales" + Environment.NewLine + FormatearQ(Convert.ToDecimal(saldos["DeudaPersonal"]));

                cboFacturaProveedor.DataSource = ds.Tables[3];
                cboFacturaProveedor.DisplayMember = "Etiqueta";
                cboFacturaProveedor.ValueMember = "IdCompra";
                cboFacturaProveedor.SelectedIndex = -1;
                cboObligacionPersonal.DataSource = ds.Tables[4];
                cboObligacionPersonal.DisplayMember = "Etiqueta";
                cboObligacionPersonal.ValueMember = "IdPeriodoGastoPersonal";
                cboObligacionPersonal.SelectedIndex = -1;

                ultimoIdCierre = 0;
                if (ds.Tables[6].Rows.Count > 0)
                {
                    DataRow cierre = ds.Tables[6].Rows[0];
                    ultimoIdCierre = Convert.ToInt32(cierre["IdCierre"]);
                    txtFondoInicial.Text = FormatearMonto(Convert.ToDecimal(cierre["EfectivoContado"]));
                    txtFondoInicial.ReadOnly = true;
                    dtpDesde.Value = Convert.ToDateTime(cierre["FechaCierre"]);
                    capitalCalculado = Convert.ToDecimal(cierre["CapitalGenerado"]);
                    gananciaCalculada = Convert.ToDecimal(cierre["GananciaGenerada"]);
                }
                else
                {
                    txtFondoInicial.ReadOnly = false;
                    if (string.IsNullOrWhiteSpace(txtFondoInicial.Text)) txtFondoInicial.Text = "0.00";
                    dtpDesde.Value = DateTime.Today;
                    capitalCalculado = gananciaCalculada = 0;
                }

                cboCierreDeposito.DataSource = ds.Tables[7];
                cboCierreDeposito.DisplayMember = "Etiqueta";
                cboCierreDeposito.ValueMember = "IdCierre";
                cboCierreDeposito.SelectedIndex = ds.Tables[7].Rows.Count > 0 ? 0 : -1;
                btnRegistrarDepositos.Enabled = ds.Tables[7].Rows.Count > 0;
                txtCapitalDepositado.Text = "";
                txtGananciaDepositada.Text = "";
                lblCapitalADepositar.Text = FormatearQ(0);
                lblGananciaADepositar.Text = FormatearQ(0);
                CalcularCierre(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo cargar el panel financiero. Verifica que ejecutaste migracion_finanzas.sql en DBSISTEMA_VENTA y que conexion.txt apunta al servidor correcto.\r\n\r\nDetalle: " + ex.Message,
                    "Control financiero", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormatearColumnas()
        {
            foreach (DataGridViewColumn col in dgvFacturasPendientes.Columns)
            {
                if (col.Name == "FechaVencimiento") col.DefaultCellStyle.Format = "dd/MM/yyyy";
                if (col.Name == "SaldoPendiente") col.DefaultCellStyle.Format = "N2";
                if (col.Name == "IdCompra") col.Visible = false;
            }
            foreach (DataGridViewColumn col in dgvMovimientos.Columns)
            {
                if (col.Name == "Fecha") col.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                if (col.Name == "Monto") col.DefaultCellStyle.Format = "N2";
            }
        }

        private void CalcularCierre()
        {
            CalcularCierre(true);
        }

        private void CalcularCierre(bool mostrarError)
        {
            decimal fondo;
            if (!TryMonto(txtFondoInicial.Text, out fondo)) fondo = 0;
            try
            {
                DataTable dt = finanzas.ObtenerCifrasCierre(dtpDesde.Value.Date, fondo);
                if (dt.Rows.Count == 0) return;
                DataRow r = dt.Rows[0];
                txtFondoInicial.Text = FormatearMonto(Convert.ToDecimal(r["FondoInicial"]));
                efectivoEsperadoCalculado = Convert.ToDecimal(r["EfectivoEsperado"]);
                txtEfectivoEsperado.Text = FormatearQ(efectivoEsperadoCalculado);
                lblCostoProductos.Text = FormatearQ(Convert.ToDecimal(r["Costo"]));
                lblReservaCapital.Text = FormatearQ(Convert.ToDecimal(r["ReservaCapital"]));

                decimal contado;
                if (TryMonto(txtEfectivoContado.Text, out contado))
                    txtDiferencia.Text = FormatearQ(contado - efectivoEsperadoCalculado);
                else txtDiferencia.Text = FormatearQ(0);
            }
            catch (Exception ex)
            {
                if (mostrarError) MessageBox.Show("No se pudieron calcular las cifras del cierre.\r\n" + ex.Message, "Cierre de caja", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RegistrarCierre()
        {
            decimal fondo, contado;
            if (!TryMonto(txtFondoInicial.Text, out fondo) || fondo < 0 || !TryMonto(txtEfectivoContado.Text, out contado) || contado < 0)
            {
                MessageBox.Show("Ingresa valores válidos para el fondo inicial y el efectivo contado.", "Cierre de caja", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEfectivoContado.Focus(); return;
            }
            CalcularCierre(false);
            decimal esperado = efectivoEsperadoCalculado;
            decimal diferencia = contado - esperado;
            string detalle = diferencia < 0
                ? "Faltan " + FormatearQ(Math.Abs(diferencia)) + ". Se descontará primero de la ganancia acumulada y luego del capital. ¿Confirmas el cierre?"
                : diferencia > 0
                    ? "Hay un sobrante de " + FormatearQ(diferencia) + ". Quedará registrado pendiente de clasificar. ¿Confirmas el cierre?"
                    : "El efectivo cuadra. ¿Confirmas el cierre?";
            if (MessageBox.Show(detalle, "Confirmar cierre", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            string mensaje; int id;
            if (!finanzas.RegistrarCierre(usuarioActual.IdUsuario, dtpDesde.Value.Date, fondo, contado, out mensaje, out id))
            {
                MessageBox.Show(mensaje, "No se registró el cierre", MessageBoxButtons.OK, MessageBoxIcon.Error); return;
            }
            MessageBox.Show("Cierre registrado correctamente. Número: " + id, "Cierre guardado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            CargarPanel();
        }

        private void RegistrarDeposito()
        {
            if (idCierreDeposito <= 0) { MessageBox.Show("No hay cierres pendientes de depósito.", "Depósito", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            decimal capital, ganancia;
            if (!TryMonto(txtCapitalDepositado.Text, out capital) || !TryMonto(txtGananciaDepositada.Text, out ganancia))
            {
                MessageBox.Show("Escribe el monto realmente depositado en ambas casillas; usa 0 si no depositaste una parte.", "Depósito", MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
            }
            if (MessageBox.Show("¿Registrar el depósito de capital y ganancia indicado?", "Confirmar depósito", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            Usuario responsable;
            using (var dialogo = new DialogoResponsableDeposito())
            {
                if (dialogo.ShowDialog(this) != DialogResult.OK) return;
                System.Collections.Generic.List<Usuario> coincidencias;
                try { coincidencias = new CN_Usuario().ListarActivosPorClave(dialogo.Clave); }
                catch (Exception ex)
                {
                    MessageBox.Show("No fue posible validar al responsable: " + ex.Message, "Validar depósito", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (coincidencias.Count != 1)
                {
                    string aviso = coincidencias.Count == 0
                        ? "La contraseña no corresponde a un usuario activo. No se registró el depósito."
                        : "La contraseña está asignada a más de un usuario activo. Debe ser única para identificar al responsable.";
                    MessageBox.Show(aviso, "Responsable no validado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                responsable = coincidencias[0];
            }
            string mensaje;
            if (!finanzas.RegistrarDeposito(idCierreDeposito, responsable.IdUsuario, capital, ganancia, txtReferenciaDeposito.Text.Trim(), out mensaje))
            { MessageBox.Show(mensaje, "Depósito", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            MessageBox.Show("Depósito registrado para auditoría.\nResponsable: " + responsable.NombreCompleto, "Depósito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            CargarPanel();
        }

        private sealed class DialogoResponsableDeposito : Form
        {
            private readonly TextBox txtClave = new TextBox();
            public string Clave { get { return txtClave.Text; } }

            public DialogoResponsableDeposito()
            {
                Text = "Validar responsable del depósito";
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                MinimizeBox = false; MaximizeBox = false; ShowInTaskbar = false;
                ClientSize = new Size(390, 155);
                Controls.Add(new Label { Text = "Contraseña de quien realizó o subió el comprobante:", Left = 18, Top = 18, Width = 350, Height = 38 });
                txtClave.SetBounds(18, 58, 350, 27); txtClave.MaxLength = 50; txtClave.UseSystemPasswordChar = true;
                Controls.Add(txtClave);
                var cancelar = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel };
                cancelar.SetBounds(190, 105, 82, 30); Controls.Add(cancelar);
                var confirmar = new Button { Text = "Validar", DialogResult = DialogResult.OK };
                confirmar.SetBounds(280, 105, 88, 30); Controls.Add(confirmar);
                AcceptButton = confirmar; CancelButton = cancelar;
                Shown += (s, e) => txtClave.Focus();
            }
        }

        private void cboCierreDeposito_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboCierreDeposito.SelectedValue == null || cboCierreDeposito.SelectedValue is DataRowView)
            {
                idCierreDeposito = 0;
                return;
            }
            DataRowView fila = cboCierreDeposito.SelectedItem as DataRowView;
            if (fila == null) return;
            idCierreDeposito = Convert.ToInt32(fila["IdCierre"]);
            capitalCalculado = Convert.ToDecimal(fila["CapitalPendiente"]);
            gananciaCalculada = Convert.ToDecimal(fila["GananciaPendiente"]);
            lblCapitalADepositar.Text = FormatearQ(capitalCalculado);
            lblGananciaADepositar.Text = FormatearQ(gananciaCalculada);
        }

        private void RegistrarMovimiento()
        {
            string tipoSeleccionado = cboTipoMovimiento.SelectedItem == null ? "" : cboTipoMovimiento.SelectedItem.ToString();
            string tipo = tipoSeleccionado == "Gasto personal" ? "Personal" : tipoSeleccionado == "Pago a proveedor" ? "Proveedor" : "Empresa";
            int? relacionado = null;
            ComboBox combo = tipo == "Personal" ? cboObligacionPersonal : cboFacturaProveedor;
            if (tipo != "Empresa")
            {
                if (combo.SelectedValue == null || combo.SelectedValue is DataRowView)
                { MessageBox.Show("Selecciona el gasto o la factura pendiente.", "Registrar movimiento", MessageBoxButtons.OK, MessageBoxIcon.Warning); combo.Focus(); return; }
                relacionado = Convert.ToInt32(combo.SelectedValue);
            }
            decimal monto;
            if (!TryMonto(txtMontoMovimiento.Text, out monto) || monto <= 0)
            { MessageBox.Show("Ingresa un monto mayor que cero.", "Registrar movimiento", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtMontoMovimiento.Focus(); return; }
            string concepto = txtMotivoMovimiento.Text.Trim();
            if (string.IsNullOrWhiteSpace(concepto)) concepto = tipo == "Personal" ? cboObligacionPersonal.Text : tipo == "Proveedor" ? "Pago de factura " + cboFacturaProveedor.Text : "Gasto de empresa";
            string mensaje;
            if (!finanzas.RegistrarMovimiento(tipo, relacionado, usuarioActual.IdUsuario, dtpFechaMovimiento.Value, concepto, monto, cboMedioPago.SelectedItem.ToString(), null, out mensaje))
            { MessageBox.Show(mensaje, "No se registró el movimiento", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            MessageBox.Show("Movimiento registrado correctamente.", "Finanzas", MessageBoxButtons.OK, MessageBoxIcon.Information);
            txtMontoMovimiento.Clear(); txtMotivoMovimiento.Clear();
            cboFacturaProveedor.SelectedIndex = -1; cboObligacionPersonal.SelectedIndex = -1;
            CargarPanel();
        }

        private static bool TryMonto(string texto, out decimal monto)
        {
            return decimal.TryParse(texto, NumberStyles.Currency, CultureInfo.CurrentCulture, out monto) ||
                   decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out monto);
        }

        private static string FormatearQ(decimal monto)
        {
            return "Q " + monto.ToString("N2", CultureInfo.GetCultureInfo("es-GT"));
        }

        private static string FormatearMonto(decimal monto)
        {
            return monto.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
