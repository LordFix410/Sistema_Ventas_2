using CapaEntidad;
using CapaNegocio;
using CapaPresentacion.Modales;
using CapaPresentacion.Utilidades;
using DocumentFormat.OpenXml.Office2019.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml.Linq;

namespace CapaPresentacion
{
    public partial class frmCompraRegistrar : Form
    {
        private Usuario _Usuario;
        private Producto _productoActualSeleccionado;
        private bool sincronizandoPrecios;
        private bool evaluandoCambioCosto;
        private decimal costoOriginalProducto;
        private decimal precioVentaOriginalProducto;
        private decimal margenOriginalProducto;
        private int indiceProductoEditando = -1;
        private static readonly System.Drawing.Font FuenteMargenBajo = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        // Visual Studio WinForms Designer requires a real parameterless constructor.
        public frmCompraRegistrar() : this(null)
        {
        }

        public frmCompraRegistrar(Usuario oUsuario)
        {
            _Usuario = oUsuario;
            InitializeComponent();
            Resize += (s, e) => AjustarDisenoAdaptable();
            AjustarDisenoAdaptable();
            ConfigurarTablaCompra();
            dataGridView1.CellFormatting += dataGridView1_CellFormatting;
            txtCodigoProducto.KeyDown += TxtCodigoProducto_KeyDown;
            txtNuevoDocumento.KeyDown += TxtProveedor_KeyDown;
            txtRazonSocial.KeyDown += TxtProveedor_KeyDown;
            txtPrecioVenta.TextChanged += PrecioVenta_TextChanged;
            txtPrecioCompra.Leave += PrecioCompra_Leave;
            txtCantidad.Enter += delegate { txtCantidad.Select(0, txtCantidad.Text.Length); };
            txtCantidad.KeyDown += TxtCantidad_KeyDown;
            txtPrecioCompra.Enter += delegate { txtPrecioCompra.SelectAll(); };
            txtPorcentaje.Enter += delegate { txtPorcentaje.SelectAll(); };
            txtPrecioVenta.Enter += delegate { txtPrecioVenta.SelectAll(); };
            txtPrecioCompra.KeyDown += PrecioCompra_KeyDown;
            txtPorcentaje.KeyDown += Porcentaje_KeyDown;
            txtPrecioVenta.KeyDown += PrecioVenta_KeyDown;
            dataGridView1.CellDoubleClick += dataGridView1_CellDoubleClick;
            btnGuardarBorrador.Click += (s, e) => GuardarBorrador();
            btnCargarBorrador.Click += (s, e) => CargarBorrador();
            ExperienciaUsuario.Aplicar(this, txtCodigoProducto, txtCodigoProducto, txtNuevoDocumento, txtRazonSocial);
            if (dataGridView1.Columns.Contains("Porcentaje"))
                dataGridView1.Columns["Porcentaje"].DefaultCellStyle.Format = "P2";
        }

        private void AjustarDisenoAdaptable()
        {
            if (IsDisposed || dataGridView1 == null) return;
            const int margen = 18;
            const int espacio = 12;
            int ancho = Math.Max(900, ClientSize.Width - margen * 2);
            int alto = Math.Max(500, ClientSize.Height);

            label10.SetBounds(0, 0, ClientSize.Width, 286);
            label2.SetBounds(margen, 8, ancho, 42);

            int anchoCompra = (ancho - espacio * 2) * 31 / 100;
            int anchoProveedor = (ancho - espacio * 2) * 36 / 100;
            int anchoBorrador = ancho - espacio * 2 - anchoCompra - anchoProveedor;
            int yCabecera = 55;
            groupBox1.SetBounds(margen, yCabecera, anchoCompra, 104);
            groupBox2.SetBounds(margen + anchoCompra + espacio, yCabecera, anchoProveedor, 104);
            groupBox4.SetBounds(margen + anchoCompra + espacio + anchoProveedor + espacio, yCabecera, anchoBorrador, 104);

            int yProducto = 171;
            groupBox3.SetBounds(margen, yProducto, ancho, 87);
            int anchoBusqueda = 177;
            int xBuscar = 7;
            txtCodigoProducto.SetBounds(xBuscar, 47, anchoBusqueda, 22);
            label7.SetBounds(xBuscar, 27, 145, 16);
            btnBuscarProducto.SetBounds(xBuscar + anchoBusqueda + 10, 33, 44, 39);
            int xProducto = btnBuscarProducto.Right + 14;
            int anchoNombre = Math.Max(150, Math.Min(400, ancho - 1020));
            txtProducto.SetBounds(xProducto, 47, anchoNombre, 22);
            label6.SetBounds(xProducto, 26, anchoNombre, 16);
            int xCantidad = txtProducto.Right + 18;
            txtCantidad.SetBounds(xCantidad, 46, 93, 22);
            label11.SetBounds(xCantidad, 25, 90, 16);
            int xCosto = txtCantidad.Right + 18;
            txtPrecioCompra.SetBounds(xCosto, 48, 101, 22);
            label8.SetBounds(xCosto, 28, 115, 16);
            int xMargen = txtPrecioCompra.Right + 18;
            txtPorcentaje.SetBounds(xMargen, 46, 101, 22);
            labelPorcentaje.SetBounds(xMargen, 25, 95, 16);
            int xVenta = txtPorcentaje.Right + 18;
            txtPrecioVenta.SetBounds(xVenta, 46, 101, 22);
            label9.SetBounds(xVenta, 25, 100, 16);
            btnAgregar.SetBounds(Math.Min(ancho - 112, xVenta + 125), 11, 100, 70);

            int yTabla = Math.Max(yProducto + 103, label10.Height + 18);
            int anchoResumen = 135;
            int xTabla = margen;
            int anchoTabla = Math.Max(400, ancho - anchoResumen - espacio);
            dataGridView1.SetBounds(xTabla, yTabla, anchoTabla, Math.Max(150, alto - yTabla - margen));
            int xResumen = xTabla + anchoTabla + espacio;
            btnRegistrar.SetBounds(xResumen, yTabla + 4, anchoResumen, 79);
            label12.SetBounds(xResumen, yTabla + 108, anchoResumen, 20);
            txtTotalPagar.SetBounds(xResumen, yTabla + 132, anchoResumen, 26);
        }

        private void ConfigurarTablaCompra()
        {
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            if (!dataGridView1.Columns.Contains("Foto"))
                dataGridView1.Columns.Insert(1, new DataGridViewImageColumn
                {
                    Name = "Foto", HeaderText = "FOTO", ImageLayout = DataGridViewImageCellLayout.Zoom,
                    Width = 68, MinimumWidth = 60, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable
                });
            dataGridView1.RowTemplate.Height = 48;
            dataGridView1.RowTemplate.Height = 68;
            dataGridView1.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dataGridView1.Columns["IdProducto"].Visible = false;
            dataGridView1.Columns["PrecioVenta"].Visible = true;
            string[] orden = { "Cantidad", "Foto", "Producto", "PrecioCompra", "Porcentaje", "PrecioVenta", "SubTotal", "btnEliminar" };
            for (int i = 0; i < orden.Length; i++)
                if (dataGridView1.Columns.Contains(orden[i])) dataGridView1.Columns[orden[i]].DisplayIndex = i;
            dataGridView1.Columns["Cantidad"].FillWeight = 60;
            dataGridView1.Columns["Producto"].FillWeight = 190;
            dataGridView1.Columns["PrecioCompra"].FillWeight = 90;
            dataGridView1.Columns["Porcentaje"].FillWeight = 85;
            dataGridView1.Columns["PrecioVenta"].FillWeight = 90;
            dataGridView1.Columns["SubTotal"].FillWeight = 95;
            dataGridView1.Columns["btnEliminar"].FillWeight = 60;
            dataGridView1.Columns["Foto"].FillWeight = 45;
            dataGridView1.Columns["Foto"].MinimumWidth = 64;
            dataGridView1.Columns["Porcentaje"].DefaultCellStyle.Format = "P2";
            dataGridView1.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || dataGridView1.Columns[e.ColumnIndex].Name != "Foto") return;
                Producto p = dataGridView1.Rows[e.RowIndex].Tag as Producto;
                if (p != null && !string.IsNullOrWhiteSpace(p.ImagenRuta))
                    GestorImagenProducto.MostrarVistaPrevia(this, p.ImagenRuta, p.Nombre);
            };
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || dataGridView1.Columns[e.ColumnIndex].Name != "Porcentaje" || e.Value == null)
                return;
            if (decimal.TryParse(Convert.ToString(e.Value), out decimal porcentaje))
            {
                bool bajo = porcentaje < 0.50m;
                e.CellStyle.ForeColor = bajo ? System.Drawing.Color.Firebrick : dataGridView1.DefaultCellStyle.ForeColor;
                if (bajo) e.CellStyle.Font = FuenteMargenBajo;
            }
        }

        private void GuardarBorrador()
        {
            if (indiceProductoEditando >= 0)
            {
                CapaPresentacion.DialogoSistema.Show("Confirma o cancela la edición del producto antes de guardar el borrador.",
                    "Edición pendiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (dataGridView1.Rows.Count == 0)
            {
                CapaPresentacion.DialogoSistema.Show("Agrega al menos un producto antes de guardar el borrador.",
                    "Borrador vacío", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow lineaInvalida = dataGridView1.Rows.Cast<DataGridViewRow>().FirstOrDefault(f =>
                f.IsNewRow ||
                !decimal.TryParse(Convert.ToString(f.Cells["PrecioCompra"].Value), out decimal costo) || costo <= 0 ||
                !decimal.TryParse(Convert.ToString(f.Cells["PrecioVenta"].Value), out decimal venta) || venta <= 0);
            if (lineaInvalida != null)
            {
                CapaPresentacion.DialogoSistema.Show(
                    "No se puede guardar el borrador: revisa que cada producto tenga precio de compra y precio de venta mayores que cero. " +
                    "El archivo anterior contiene una línea con ambos precios en Q0.00.",
                    "Precios incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dialogo = new SaveFileDialog
            {
                Filter = "Borrador de compra (*.xml)|*.xml",
                FileName = "Borrador_compra_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xml",
                AddExtension = true,
                DefaultExt = "xml"
            })
            {
                if (dialogo.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    var lineas = new XElement("Productos",
                        dataGridView1.Rows.Cast<DataGridViewRow>().Select(f => new XElement("Producto",
                            new XElement("IdProducto", Convert.ToString(f.Cells["IdProducto"].Value)),
                            new XElement("Codigo", (f.Tag as Producto)?.Codigo ?? string.Empty),
                            new XElement("Nombre", Convert.ToString(f.Cells["Producto"].Value)),
                            new XElement("PrecioCompra", Convert.ToDecimal(f.Cells["PrecioCompra"].Value).ToString(CultureInfo.InvariantCulture)),
                            new XElement("PrecioVenta", Convert.ToDecimal(f.Cells["PrecioVenta"].Value).ToString(CultureInfo.InvariantCulture)),
                            new XElement("Porcentaje", Convert.ToDecimal(f.Cells["Porcentaje"].Value).ToString(CultureInfo.InvariantCulture)),
                            new XElement("Cantidad", Convert.ToString(f.Cells["Cantidad"].Value)),
                            new XElement("Subtotal", Convert.ToDecimal(f.Cells["SubTotal"].Value).ToString(CultureInfo.InvariantCulture)))));
                    new XDocument(new XElement("BorradorCompra", new XAttribute("Version", "2"),
                        new XElement("IdProveedor", txtIdProveedor.Text),
                        new XElement("DocumentoProveedor", txtNuevoDocumento.Text),
                        new XElement("RazonSocial", txtRazonSocial.Text),
                        new XElement("TipoDocumento", (cboTipoDocumento.SelectedItem as OpcionCombo)?.Texto ?? "Boleta"),
                        lineas)).Save(dialogo.FileName);
                    CapaPresentacion.DialogoSistema.ShowSuccess("Borrador guardado correctamente.", "Borrador de compra");
                }
                catch (Exception ex)
                {
                    CapaPresentacion.DialogoSistema.Show("No se pudo guardar el borrador.\n\n" + ex.Message,
                        "Borrador de compra", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void CargarBorrador()
        {
            using (var dialogo = new OpenFileDialog { Filter = "Borrador de compra (*.xml)|*.xml", CheckFileExists = true })
            {
                if (dialogo.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    XDocument documento = XDocument.Load(dialogo.FileName);
                    XElement raiz = documento.Root;
                    if (raiz == null || !string.Equals(raiz.Name.LocalName, "BorradorCompra", StringComparison.OrdinalIgnoreCase))
                    {
                        string elementoRaiz = raiz == null ? "vacío" : raiz.Name.LocalName;
                        throw new InvalidOperationException("El XML seleccionado no es un borrador de compra. Elemento encontrado: " + elementoRaiz + ". Usa un archivo guardado con «Guardar borrador».");
                    }
                    string versionTexto = raiz.Attributes().FirstOrDefault(a =>
                        string.Equals(a.Name.LocalName, "Version", StringComparison.OrdinalIgnoreCase))?.Value;
                    int versionBorrador = string.IsNullOrWhiteSpace(versionTexto) ? 1 :
                        int.TryParse(versionTexto, NumberStyles.Integer, CultureInfo.InvariantCulture, out int versionLeida)
                            ? versionLeida : 0;
                    if (versionBorrador != 1 && versionBorrador != 2)
                        throw new InvalidOperationException("La versión del borrador no es compatible: " + (versionTexto ?? "sin versión") + ". Guarda uno nuevo desde esta versión del sistema.");

                    int idProveedor = EnteroBorrador(raiz, "IdProveedor", 0);
                    string documentoProveedorBorrador = ValorBorrador(raiz, "DocumentoProveedor").Trim();
                    List<Proveedor> proveedoresGuardados = new CN_Proveedor().Listar();
                    Proveedor proveedorGuardado = idProveedor > 0
                        ? proveedoresGuardados.FirstOrDefault(p => p.IdProveedor == idProveedor)
                        : null;
                    if (proveedorGuardado != null && documentoProveedorBorrador.Length > 0 &&
                        !string.Equals(proveedorGuardado.Documento?.Trim(), documentoProveedorBorrador, StringComparison.OrdinalIgnoreCase))
                        proveedorGuardado = null;
                    if (proveedorGuardado == null && documentoProveedorBorrador.Length > 0)
                        proveedorGuardado = proveedoresGuardados.FirstOrDefault(p => p.Estado &&
                            string.Equals(p.Documento?.Trim(), documentoProveedorBorrador, StringComparison.OrdinalIgnoreCase));
                    Proveedor proveedor = proveedorGuardado != null && proveedorGuardado.Estado
                        ? proveedorGuardado
                        : null;
                    bool proveedorRequiereSeleccion = (idProveedor > 0 || documentoProveedorBorrador.Length > 0) && proveedor == null;

                    // Se cargan también los productos inactivos para que el borrador no se pierda.
                    // El procedimiento de compra impedirá registrarlos hasta que se reactiven.
                    List<Producto> productos = new CN_Producto().Listar();
                    var datos = new List<object[]>();
                    var ids = new HashSet<int>();
                    var productosInactivos = new List<string>();
                    foreach (XElement x in ElementoBorrador(raiz, "Productos")?.Elements()
                        .Where(elemento => string.Equals(elemento.Name.LocalName, "Producto", StringComparison.OrdinalIgnoreCase))
                        ?? Enumerable.Empty<XElement>())
                    {
                        int id = EnteroBorrador(x, "IdProducto", 0);
                        int cantidad = EnteroBorrador(x, "Cantidad", 0);
                        decimal costo = DecimalBorrador(x, "PrecioCompra");
                        decimal venta = DecimalBorrador(x, "PrecioVenta");
                        decimal margen = DecimalBorrador(x, "Porcentaje");
                        decimal subtotal = DecimalBorrador(x, "Subtotal");
                        string nombreBorrador = ValorBorrador(x, "Nombre").Trim();
                        string codigoBorrador = ValorBorrador(x, "Codigo").Trim();
                        string linea = "Renglón " + (datos.Count + 1) + (string.IsNullOrWhiteSpace(nombreBorrador) ? string.Empty : " (" + nombreBorrador + ")");
                        if (id <= 0 || cantidad <= 0 || costo <= 0 || venta <= 0 || venta < costo || margen < 0 ||
                            Math.Abs(venta * 4m - decimal.Round(venta * 4m, 0)) > 0.0001m ||
                            (costo > 0 && Math.Abs(margen - ((venta - costo) / costo)) > 0.0001m) ||
                            Math.Abs(subtotal - Math.Round(costo * cantidad, 2)) > 0.01m)
                            throw new InvalidOperationException(linea + ": revisa el ID, la cantidad, los precios o el subtotal. No se cargó ningún dato.");

                        Producto producto = versionBorrador >= 2 && codigoBorrador.Length > 0
                            ? productos.FirstOrDefault(p => string.Equals(p.Codigo?.Trim(), codigoBorrador, StringComparison.OrdinalIgnoreCase))
                            : productos.FirstOrDefault(p => p.IdProducto == id);
                        if (producto == null)
                            throw new InvalidOperationException(linea + ": no se encontró el producto por su código o ID. No se cargó ningún dato.");
                        if (!string.IsNullOrWhiteSpace(nombreBorrador) &&
                            !string.Equals(producto.Nombre?.Trim(), nombreBorrador, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException(linea + ": el ID/código del borrador ahora corresponde a otro nombre de producto. Revisa el catálogo; no se cargó ningún dato.");
                        if (!ids.Add(producto.IdProducto))
                            throw new InvalidOperationException(linea + ": el borrador apunta dos veces al mismo producto. No se cargó ningún dato.");
                        if (!producto.Estado) productosInactivos.Add(producto.Nombre);

                        datos.Add(new object[] { producto.IdProducto, GestorImagenProducto.CargarMiniatura(producto.ImagenRuta, 52, 52), producto.Nombre, costo.ToString("0.00"), venta.ToString("0.00"),
                            margen.ToString("0.####"), cantidad, subtotal.ToString("0.00") });
                    }
                    if (datos.Count == 0) throw new InvalidOperationException("El borrador no contiene productos.");
                    if (dataGridView1.Rows.Count > 0 && CapaPresentacion.DialogoSistema.Show(this,
                        "La compra actual tiene productos. ¿Deseas reemplazarla con el borrador?", "Cargar borrador",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                        return;

                    dataGridView1.Rows.Clear();
                    foreach (object[] fila in datos)
                    {
                        int indiceFila = dataGridView1.Rows.Add(fila);
                        int idProducto = Convert.ToInt32(dataGridView1.Rows[indiceFila].Cells["IdProducto"].Value);
                        dataGridView1.Rows[indiceFila].Tag = productos.FirstOrDefault(p => p.IdProducto == idProducto);
                    }
                    txtIdProveedor.Text = proveedor == null ? "0" : proveedor.IdProveedor.ToString();
                    txtNuevoDocumento.Text = proveedor?.Documento ?? ValorBorrador(raiz, "DocumentoProveedor");
                    txtRazonSocial.Text = proveedor?.RazonSocial ?? ValorBorrador(raiz, "RazonSocial");
                    string tipo = ValorBorrador(raiz, "TipoDocumento");
                    for (int i = 0; i < cboTipoDocumento.Items.Count; i++)
                        if (string.Equals((cboTipoDocumento.Items[i] as OpcionCombo)?.Texto, tipo, StringComparison.OrdinalIgnoreCase))
                        { cboTipoDocumento.SelectedIndex = i; break; }
                    indiceProductoEditando = -1;
                    calculatTotal();
                    txtCodigoProducto.Focus();
                    string aviso = "Borrador cargado. Revisa proveedor, productos, precios y total de factura antes de registrar.";
                    if (proveedorRequiereSeleccion)
                        aviso += "\n\nEl proveedor guardado ya no está activo o fue eliminado. Selecciona un proveedor activo antes de registrar.";
                    if (productosInactivos.Count > 0)
                        aviso += "\n\nReactiva estos productos antes de registrar la compra: " + string.Join(", ", productosInactivos.Distinct()) + ".";
                    CapaPresentacion.DialogoSistema.Show(aviso,
                        "Borrador de compra", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    CapaPresentacion.DialogoSistema.Show("No se pudo cargar el borrador. La compra actual se conservó.\n\n" + ex.Message,
                        "Borrador de compra", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static XElement ElementoBorrador(XElement raiz, string campo) => raiz?.Elements()
            .FirstOrDefault(elemento => string.Equals(elemento.Name.LocalName, campo, StringComparison.OrdinalIgnoreCase));

        private static string ValorBorrador(XElement raiz, string campo) => (string)ElementoBorrador(raiz, campo) ?? string.Empty;
        private static int EnteroBorrador(XElement raiz, string campo, int predeterminado) =>
            int.TryParse(ValorBorrador(raiz, campo), NumberStyles.Integer, CultureInfo.InvariantCulture, out int valor) ? valor : predeterminado;
        private static decimal DecimalBorrador(XElement raiz, string campo) =>
            decimal.TryParse(ValorBorrador(raiz, campo), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal valor)
                ? valor : throw new InvalidOperationException("El borrador contiene un monto inválido en " + campo + ".");

        private void TxtCantidad_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                txtPrecioCompra.Focus();
            }
        }

        private void PrecioCompra_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            txtPorcentaje.Focus();
        }

        private void Porcentaje_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            if (!CalcularPrecioVentaDesdePorcentaje()) return;
            txtPrecioVenta.Focus();
            txtPrecioVenta.SelectAll();
        }

        private void PrecioVenta_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            btnAgregar.PerformClick();
        }

        private void TxtCodigoProducto_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                BuscarProductoPorCodigo();
            }
            else if (e.KeyCode == Keys.F5)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                btnBuscarProducto.PerformClick();
            }
        }

        private void TxtProveedor_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                BuscarProveedorPorDocumento(sender == txtRazonSocial);
            }
            else if (e.KeyCode == Keys.F5)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                btnBuscarProveedor.PerformClick();
            }
        }

        private void BuscarProveedorPorDocumento(bool buscarPorRazonSocial)
        {
            string valor = (buscarPorRazonSocial ? txtRazonSocial.Text : txtNuevoDocumento.Text).Trim();
            if (valor.Length == 0)
            {
                btnBuscarProveedor.PerformClick();
                return;
            }

            Proveedor proveedor = new CN_Proveedor().Listar()
                .FirstOrDefault(item => item.Estado && string.Equals(
                    Convert.ToString(buscarPorRazonSocial ? item.RazonSocial : item.Documento).Trim(),
                    valor, StringComparison.OrdinalIgnoreCase));

            if (proveedor == null)
            {
                CapaPresentacion.DialogoSistema.Show("No se encontró un proveedor con ese " +
                    (buscarPorRazonSocial ? "nombre." : "documento."),
                    "Proveedor no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                TextBox campo = buscarPorRazonSocial ? txtRazonSocial : txtNuevoDocumento;
                campo.Focus();
                campo.SelectAll();
                return;
            }

            txtIdProveedor.Text = proveedor.IdProveedor.ToString();
            txtNuevoDocumento.Text = proveedor.Documento;
            txtRazonSocial.Text = proveedor.RazonSocial;
            txtCodigoProducto.Focus();
        }

        private void BuscarProductoPorCodigo()
        {
            string codigo = txtCodigoProducto.Text.Trim();
            if (codigo.Length == 0)
            {
                btnBuscarProducto.PerformClick();
                return;
            }

            txtIdProducto.Text = "0";
            Producto producto;
            try
            {
                producto = new CN_Producto().BuscarPorCodigo(codigo);
            }
            catch (Exception ex)
            {
                CapaPresentacion.DialogoSistema.Show(ex.Message + Environment.NewLine + ex.InnerException?.Message,
                    "No se pudieron cargar productos", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (producto == null)
            {
                CapaPresentacion.DialogoSistema.Show("No se encontró un producto con el código '" + codigo + "'.",
                    "Producto no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtCodigoProducto.Focus();
                txtCodigoProducto.SelectAll();
                return;
            }

            txtIdProducto.Text = producto.IdProducto.ToString();
            _productoActualSeleccionado = producto;
            txtCodigoProducto.Text = producto.Codigo;
            txtProducto.Text = producto.Nombre;
            sincronizandoPrecios = true;
            txtPrecioCompra.Text = producto.PrecioCompra.ToString("0.00");
            costoOriginalProducto = producto.PrecioCompra;
            precioVentaOriginalProducto = producto.PrecioVenta;
            margenOriginalProducto = producto.Porcentaje;
            evaluandoCambioCosto = false;
            txtPorcentaje.Text = FormatoPorcentaje.ParaMostrar(producto.Porcentaje, producto.PrecioCompra).ToString("0.##");
            txtPrecioVenta.Text = producto.PrecioVenta.ToString("0.00");
            sincronizandoPrecios = false;
            ActualizarMargenDesdePrecios();
            txtCantidad.Focus();
            txtCantidad.Select(0, txtCantidad.Text.Length);
        }

        private void frmCompraRegistrar_Load(object sender, EventArgs e)
        {
            cboTipoDocumento.Items.Add(new OpcionCombo() { Valor = "Boleta", Texto = "Boleta" });
            cboTipoDocumento.Items.Add(new OpcionCombo() { Valor = "Factura", Texto = "Factura" });
            cboTipoDocumento.DisplayMember = "Texto";
            cboTipoDocumento.ValueMember = "Valor";
            cboTipoDocumento.SelectedIndex = 0;

            txtFecha.Text = DateTime.Now.ToString("dd/MM/yyyy");
            txtIdProducto.Text = "0";
            txtIdProveedor.Text = "0";

            txtCantidad.Minimum = 1;
            txtCantidad.Maximum = 100000;
            txtCantidad.DecimalPlaces = 0;
            txtCantidad.Increment = 1;

        }

        private void btnBuscarProveedor_Click(object sender, EventArgs e)
        {
            using (var modal = new mdProveedor()){
                var result = modal.ShowDialog();

                if (result == DialogResult.OK)
                {
                    txtIdProveedor.Text = modal._Proveedor.IdProveedor.ToString();
                    txtNuevoDocumento.Text = modal._Proveedor.Documento.ToString();
                    txtRazonSocial.Text = modal._Proveedor.RazonSocial.ToString();
                    txtCodigoProducto.Focus();
                }
                else
                {
                    txtNuevoDocumento.Select();
                }
            }
                        
            
        }

        private void btnBuscarProducto_Click(object sender, EventArgs e)
        {
            using (var modal = new mdProducto(false, false, true))
            {
                var result = modal.ShowDialog();

                if (result == DialogResult.OK)
                {
                    _productoActualSeleccionado = modal._Producto;
                    txtIdProducto.Text = modal._Producto.IdProducto.ToString();
                    txtCodigoProducto.Text = modal._Producto.Codigo;
                    txtProducto.Text = modal._Producto.Nombre;
                    sincronizandoPrecios = true;
                    txtPrecioCompra.Text = modal._Producto.PrecioCompra.ToString("0.00");
                    costoOriginalProducto = modal._Producto.PrecioCompra;
                    precioVentaOriginalProducto = modal._Producto.PrecioVenta;
                    margenOriginalProducto = modal._Producto.Porcentaje;
                    evaluandoCambioCosto = false;
                    txtPorcentaje.Text = FormatoPorcentaje.ParaMostrar(modal._Producto.Porcentaje, modal._Producto.PrecioCompra).ToString("0.##");
                    txtPrecioVenta.Text = modal._Producto.PrecioVenta.ToString("0.00");
                    sincronizandoPrecios = false;
                    ActualizarMargenDesdePrecios();
                    txtCantidad.Focus();
                    txtCantidad.Select(0, txtCantidad.Text.Length);
                }
                else
                {
                    txtCodigoProducto.Select();
                }
            }
        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void PrecioVenta_TextChanged(object sender, EventArgs e)
        {
            ActualizarMargenDesdePrecios();
        }

        private bool CalcularPrecioVentaDesdePorcentaje()
        {
            if (sincronizandoPrecios) return true;
            if (!decimal.TryParse(txtPrecioCompra.Text, out decimal costo) || costo < 0)
            {
                CapaPresentacion.DialogoSistema.Show("Ingrese un costo válido antes de calcular el precio de venta.",
                    "Costo inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrecioCompra.Focus();
                txtPrecioCompra.SelectAll();
                return false;
            }
            if (!decimal.TryParse(txtPorcentaje.Text, out decimal porcentaje) || porcentaje < 0)
            {
                CapaPresentacion.DialogoSistema.Show("Ingrese un margen válido igual o mayor a cero.",
                    "Margen inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPorcentaje.Focus();
                txtPorcentaje.SelectAll();
                return false;
            }

            if (costo == 0)
            {
                if (porcentaje <= 0 || !decimal.TryParse(txtPrecioVenta.Text, out decimal ventaExistente) || ventaExistente <= 0)
                {
                    CapaPresentacion.DialogoSistema.Show(
                        "Para calcular el costo, ingrese un margen mayor que cero y un precio de venta mayor que cero.",
                        "Datos incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    if (porcentaje <= 0)
                    {
                        txtPorcentaje.Focus();
                        txtPorcentaje.SelectAll();
                    }
                    else
                    {
                        txtPrecioVenta.Focus();
                        txtPrecioVenta.SelectAll();
                    }
                    return false;
                }

                costo = Math.Round(ventaExistente / (1m + porcentaje / 100m), 2, MidpointRounding.AwayFromZero);
                if (costo <= 0)
                {
                    CapaPresentacion.DialogoSistema.Show("Con esos valores el costo calculado queda en cero. Revise el precio y el margen.",
                        "Costo calculado inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPrecioVenta.Focus();
                    txtPrecioVenta.SelectAll();
                    return false;
                }

                sincronizandoPrecios = true;
                txtPrecioCompra.Text = costo.ToString("0.00");
                sincronizandoPrecios = false;
                ActualizarMargenDesdePrecios();
                return true;
            }

            decimal objetivo = costo * (1m + porcentaje / 100m);
            decimal ventaRedondeada = Math.Ceiling(objetivo * 4m) / 4m;
            sincronizandoPrecios = true;
            txtPrecioVenta.Text = ventaRedondeada.ToString("0.00");
            sincronizandoPrecios = false;
            ActualizarMargenDesdePrecios();
            return true;
        }

        private void ActualizarMargenDesdePrecios()
        {
            if (sincronizandoPrecios || !decimal.TryParse(txtPrecioCompra.Text, out decimal costo) || costo <= 0 ||
                !decimal.TryParse(txtPrecioVenta.Text, out decimal venta) || venta < 0)
                return;

            decimal margen = (venta - costo) / costo * 100m;
            sincronizandoPrecios = true;
            txtPorcentaje.Text = margen.ToString("0.##");
            sincronizandoPrecios = false;
        }

        private void PrecioCompra_Leave(object sender, EventArgs e)
        {
            if (evaluandoCambioCosto || !decimal.TryParse(txtPrecioCompra.Text, out decimal costoNuevo) ||
                costoNuevo < 0 || costoNuevo == costoOriginalProducto || costoOriginalProducto <= 0 ||
                !decimal.TryParse(txtPrecioVenta.Text, out decimal precioVentaActual))
                return;

            evaluandoCambioCosto = true;
            if (costoNuevo >= costoOriginalProducto)
            {
                // Si el costo subió, conservamos el precio de venta y solo cambia el margen.
                txtPrecioVenta.Text = precioVentaOriginalProducto.ToString("0.00");
                ActualizarMargenDesdePrecios();
                evaluandoCambioCosto = false;
                return;
            }

            var botones = MessageBoxButtons.YesNo;
            DialogResult actualizarPrecio = CapaPresentacion.DialogoSistema.Show(this,
                "El costo registrado era " + costoOriginalProducto.ToString("Q 0.00") +
                " y el nuevo costo es " + costoNuevo.ToString("Q 0.00") + ".\n\n" +
                "El costo bajó. ¿Desea bajar también el precio de venta?\n" +
                "Sí: actualiza el precio.\n" +
                "No (opción predeterminada): conserva el precio actual y recalcula el margen.",
                "Cambio de costo", botones, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

            if (actualizarPrecio == DialogResult.Yes)
            {
                decimal margenAnterior = margenOriginalProducto * 100m;
                decimal ventaObjetivo = Math.Ceiling((costoNuevo * (1m + margenAnterior / 100m)) * 4m) / 4m;
                txtPrecioVenta.Text = ventaObjetivo.ToString("0.00");
            }
            else
            {
                txtPrecioVenta.Text = precioVentaOriginalProducto.ToString("0.00");
                ActualizarMargenDesdePrecios();
            }
            evaluandoCambioCosto = false;
        }

        private bool SolicitarTotalFactura(decimal totalProductos, out decimal totalFactura, out decimal importeNoInventariable)
        {
            totalFactura = 0;
            importeNoInventariable = 0;
            using (var dialogo = new Form())
            using (var etiqueta = new Label())
            using (var entrada = new TextBox())
            using (var aceptar = new Button())
            using (var cancelar = new Button())
            {
                CapaPresentacion.Utilidades.IconoAplicacion.Aplicar(dialogo);
                dialogo.Text = "Confirmar total de factura";
                dialogo.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialogo.StartPosition = FormStartPosition.CenterParent;
                dialogo.ClientSize = new Size(520, 165);
                dialogo.MinimizeBox = false;
                dialogo.MaximizeBox = false;
                etiqueta.Text = "Revise la factura física e ingrese aquí el total que aparece impreso.";
                etiqueta.AutoSize = false;
                etiqueta.SetBounds(18, 14, 480, 42);
                entrada.SetBounds(20, 62, 210, 28);
                entrada.Text = string.Empty;
                entrada.TextAlign = HorizontalAlignment.Right;
                aceptar.Text = "Continuar";
                aceptar.DialogResult = DialogResult.OK;
                aceptar.SetBounds(308, 112, 95, 32);
                cancelar.Text = "Cancelar";
                cancelar.DialogResult = DialogResult.Cancel;
                cancelar.SetBounds(410, 112, 95, 32);
                dialogo.Controls.AddRange(new System.Windows.Forms.Control[] { etiqueta, entrada, aceptar, cancelar });
                dialogo.AcceptButton = aceptar;
                dialogo.CancelButton = cancelar;
                dialogo.Shown += delegate { entrada.Focus(); entrada.SelectAll(); };

                if (dialogo.ShowDialog(this) != DialogResult.OK)
                    return false;
                if (string.IsNullOrWhiteSpace(entrada.Text) ||
                    !decimal.TryParse(entrada.Text.Trim(), out totalFactura) || totalFactura < 0)
                {
                    CapaPresentacion.DialogoSistema.Show("Ingrese un total de factura válido.", "Total inválido",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }

            decimal diferencia = totalFactura - totalProductos;
            // La cantidad y el costo unitario los ingresa el usuario. La diferencia
            // contra el total de la factura admite hasta Q1.00 por redondeos de centavos.
            if (Math.Abs(diferencia) <= 1.00m)
            {
                importeNoInventariable = Math.Max(0, diferencia);
                return true;
            }

            if (diferencia < 0)
            {
                CapaPresentacion.DialogoSistema.Show("Los productos cargados superan el total de la factura por " +
                    (-diferencia).ToString("Q 0.00") + ". Revise cantidades y precios antes de registrar.",
                    "La compra no cuadra", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            DialogResult productosCompletos = CapaPresentacion.DialogoSistema.Show(this,
                "Total indicado en la factura: Q " + totalFactura.ToString("N2") +
                "\nProductos cargados al inventario: Q " + totalProductos.ToString("N2") +
                "\nDiferencia: Q " + diferencia.ToString("N2") + ".\n\n" +
                "Si esa diferencia corresponde a productos de otra persona o a cargos que no van al inventario, elija Sí y escriba cuánto debe excluir.\n" +
                "Si todavía falta agregar un producto suyo, elija No para volver a la compra.",
                "Revisar productos", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (productosCompletos != DialogResult.Yes) return false;

            using (var dialogo = new Form())
            using (var etiqueta = new Label())
            using (var entrada = new TextBox())
            using (var aceptar = new Button())
            using (var cancelar = new Button())
            {
                CapaPresentacion.Utilidades.IconoAplicacion.Aplicar(dialogo);
                dialogo.Text = "Importe fuera del inventario";
                dialogo.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialogo.StartPosition = FormStartPosition.CenterParent;
                dialogo.ClientSize = new Size(600, 220);
                dialogo.MinimizeBox = false;
                dialogo.MaximizeBox = false;
                etiqueta.AutoSize = false;
                etiqueta.TextAlign = ContentAlignment.MiddleLeft;
                etiqueta.Text = "Escriba cuánto de la diferencia NO se cargará al inventario.\n" +
                    "Ejemplo: si Q 80.00 de la factura son productos de otra persona, ingrese Q 80.00.\n" +
                    "No incluya aquí productos suyos que sí deban registrarse.";
                etiqueta.SetBounds(18, 14, 560, 82);
                entrada.SetBounds(20, 106, 210, 28);
                entrada.Text = string.Empty;
                entrada.TextAlign = HorizontalAlignment.Right;
                aceptar.Text = "Validar";
                aceptar.DialogResult = DialogResult.OK;
                aceptar.SetBounds(378, 170, 95, 32);
                cancelar.Text = "Volver";
                cancelar.DialogResult = DialogResult.Cancel;
                cancelar.SetBounds(480, 170, 95, 32);
                dialogo.Controls.AddRange(new System.Windows.Forms.Control[] { etiqueta, entrada, aceptar, cancelar });
                dialogo.AcceptButton = aceptar;
                dialogo.CancelButton = cancelar;
                dialogo.Shown += delegate { entrada.Focus(); entrada.SelectAll(); };

                if (dialogo.ShowDialog(this) != DialogResult.OK)
                    return false;
                if (string.IsNullOrWhiteSpace(entrada.Text) ||
                    !decimal.TryParse(entrada.Text.Trim(), out importeNoInventariable) || importeNoInventariable < 0)
                {
                    CapaPresentacion.DialogoSistema.Show("Escriba el importe que no se cargó al inventario.",
                        "Importe requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }

            decimal diferenciaFinal = totalFactura - importeNoInventariable - totalProductos;
            if (Math.Abs(diferenciaFinal) > 1.00m)
            {
                CapaPresentacion.DialogoSistema.Show("La compra aún no cuadra.\n\n" +
                    "Factura: Q " + totalFactura.ToString("N2") + "\n" +
                    "Productos cargados: Q " + totalProductos.ToString("N2") + "\n" +
                    "Importe excluido: Q " + importeNoInventariable.ToString("N2") + "\n" +
                    "Diferencia pendiente: Q " + diferenciaFinal.ToString("N2") +
                    ".\nRevise los datos y vuelva a intentarlo.",
                    "La compra no cuadra", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void btnAgregar_Click(object sender, EventArgs e)
        {
            decimal precioCompra = 0;
            decimal precioVenta = 0;
            decimal porcentajeSolicitado = 0;
            bool producto_existente = false;
            int indiceDuplicado = -1;

            if(!int.TryParse(txtIdProducto.Text, out int idProductoSeleccionado) || idProductoSeleccionado <= 0)
            {
                CapaPresentacion.DialogoSistema.Show("Debe de seleccionar un producto", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            if(!decimal.TryParse(txtPrecioCompra.Text, out precioCompra))
            {
                CapaPresentacion.DialogoSistema.Show("Precio Compra - Formato incorrecto", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if(!decimal.TryParse(txtPrecioVenta.Text, out precioVenta))
            {
                CapaPresentacion.DialogoSistema.Show("Precio Venta - Formato incorrecto", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtPorcentaje.Text, out porcentajeSolicitado) || porcentajeSolicitado < 0)
            {
                CapaPresentacion.DialogoSistema.Show("Margen - Ingrese un porcentaje válido igual o mayor a cero", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPorcentaje.Focus();
                txtPorcentaje.SelectAll();
                return;
            }

            if (precioCompra <= 0 || precioVenta <= 0)
            {
                CapaPresentacion.DialogoSistema.Show("El precio de compra y el precio de venta deben ser mayores que cero.", "Precios inválidos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (precioCompra <= 0)
                {
                    txtPrecioCompra.Focus();
                    txtPrecioCompra.SelectAll();
                }
                else
                {
                    txtPrecioVenta.Focus();
                    txtPrecioVenta.SelectAll();
                }
                return;
            }

            if (Math.Abs(precioVenta * 4m - decimal.Round(precioVenta * 4m, 0)) > 0.0001m)
            {
                CapaPresentacion.DialogoSistema.Show("El precio de venta debe terminar en .00, .25, .50 o .75.", "Precio de venta inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrecioVenta.Focus();
                txtPrecioVenta.SelectAll();
                return;
            }

            // PRODUCTO.Porcentaje y EDetalle_Compra almacenan una fracción (0.61 = 61%).
            // El textbox acepta puntos porcentuales, pero el detalle debe coincidir con SQL.
            decimal porcentajeReal = precioCompra == 0
                ? 0
                : decimal.Ceiling(((precioVenta - precioCompra) / precioCompra) * 10000m) / 10000m;

            foreach(DataGridViewRow fila in dataGridView1.Rows)
            {
                if (fila.Cells["IdProducto"].Value.ToString() == txtIdProducto.Text && fila.Index != indiceProductoEditando)
                {
                    producto_existente = true;
                    indiceDuplicado = fila.Index;
                    break;
                }
            }

            if (!producto_existente)
            {
                object[] valores = new object[]
                {
                    txtIdProducto.Text,
                    null,
                    txtProducto.Text,
                    precioCompra.ToString("0.00"),
                    precioVenta.ToString("0.00"),
                    porcentajeReal.ToString("0.####"),
                    txtCantidad.Value.ToString(),
                    (txtCantidad.Value * precioCompra).ToString("0.00")
                };

                int indiceFila;
                if (indiceProductoEditando >= 0 && indiceProductoEditando < dataGridView1.Rows.Count)
                {
                    indiceFila = indiceProductoEditando;
                    string[] columnas = { "IdProducto", "Foto", "Producto", "PrecioCompra", "PrecioVenta", "Porcentaje", "Cantidad", "SubTotal" };
                    for (int columna = 0; columna < valores.Length; columna++)
                        dataGridView1.Rows[indiceFila].Cells[columnas[columna]].Value = valores[columna];
                }
                else
                    indiceFila = dataGridView1.Rows.Add(valores);
                dataGridView1.Rows[indiceFila].Cells["Foto"].Value = GestorImagenProducto.CargarMiniatura(_productoActualSeleccionado?.ImagenRuta, 52, 52);
                dataGridView1.Rows[indiceFila].Tag = _productoActualSeleccionado;

                calculatTotal();
                limpiarProducto();
                txtCodigoProducto.Select();
            }
            else
            {
                CapaPresentacion.DialogoSistema.Show("El producto ya está agregado en la fila " + (indiceDuplicado + 1) +
                    ". Para modificarlo, haga doble clic en esa fila.", "Producto repetido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dataGridView1.Rows.Count ||
                (e.ColumnIndex >= 0 && dataGridView1.Columns[e.ColumnIndex].Name == "Foto")) return;
            if (indiceProductoEditando >= 0 && indiceProductoEditando != e.RowIndex)
            {
                CapaPresentacion.DialogoSistema.Show("Termine de editar el producto seleccionado antes de abrir otro.",
                    "Edición en curso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataGridViewRow fila = dataGridView1.Rows[e.RowIndex];
            int id = Convert.ToInt32(fila.Cells["IdProducto"].Value);
            Producto producto;
            try
            {
                producto = new CN_Producto().BuscarPorId(id);
            }
            catch (Exception ex)
            {
                CapaPresentacion.DialogoSistema.Show(ex.Message + Environment.NewLine + ex.InnerException?.Message,
                    "No se pudo cargar el producto", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (producto == null)
            {
                CapaPresentacion.DialogoSistema.Show("No se pudo volver a cargar el producto seleccionado.",
                    "Producto no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _productoActualSeleccionado = producto;
            indiceProductoEditando = e.RowIndex;
            txtIdProducto.Text = id.ToString();
            txtCodigoProducto.Text = producto.Codigo;
            txtProducto.Text = Convert.ToString(fila.Cells["Producto"].Value);
            sincronizandoPrecios = true;
            txtPrecioCompra.Text = Convert.ToDecimal(fila.Cells["PrecioCompra"].Value).ToString("0.00");
            txtPrecioVenta.Text = Convert.ToDecimal(fila.Cells["PrecioVenta"].Value).ToString("0.00");
            txtPorcentaje.Text = FormatoPorcentaje.ParaMostrar(
                Convert.ToDecimal(fila.Cells["Porcentaje"].Value),
                Convert.ToDecimal(fila.Cells["PrecioCompra"].Value)).ToString("0.##");
            sincronizandoPrecios = false;
            ActualizarMargenDesdePrecios();
            decimal cantidad = Convert.ToDecimal(fila.Cells["Cantidad"].Value);
            txtCantidad.Value = Math.Min(txtCantidad.Maximum, Math.Max(txtCantidad.Minimum, cantidad));
            costoOriginalProducto = Convert.ToDecimal(fila.Cells["PrecioCompra"].Value);
            precioVentaOriginalProducto = Convert.ToDecimal(fila.Cells["PrecioVenta"].Value);
            margenOriginalProducto = costoOriginalProducto <= 0 ? 0 :
                (precioVentaOriginalProducto - costoOriginalProducto) / costoOriginalProducto;
            evaluandoCambioCosto = false;
            txtCantidad.Focus();
            txtCantidad.Select(0, txtCantidad.Text.Length);
        }

        private void limpiarProducto()
        {
            indiceProductoEditando = -1;
            txtIdProducto.Text = "";
            txtProducto.Text = "";
            txtPrecioCompra.Text = "";
            txtPrecioVenta.Text = "";
            txtPorcentaje.Text = "";
            txtCantidad.Value = 1;
            txtCodigoProducto.Text = "";
            _productoActualSeleccionado = null;
        }

        private void calculatTotal()
        {
            decimal total = 0;
            if (dataGridView1.Rows.Count > 0)
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    total += Convert.ToDecimal(row.Cells["SubTotal"].Value.ToString());
                }
                txtTotalPagar.Text = total.ToString("0.00");
            }
            else
            {
                txtTotalPagar.Text = "0.00";
            }
        }

        private void btnRegistrar_Click(object sender, EventArgs e)
        {
            if (Convert.ToInt16(txtIdProveedor.Text)==0)
            {
                CapaPresentacion.DialogoSistema.Show("Debe seleccionar un proveedor", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (dataGridView1.Rows.Count < 1) {
                CapaPresentacion.DialogoSistema.Show("Debe ingresar productos en la compra", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (indiceProductoEditando >= 0)
            {
                CapaPresentacion.DialogoSistema.Show("Confirme la edición del producto con Enter en Precio Venta antes de registrar la compra.",
                    "Edición pendiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrecioVenta.Focus();
                txtPrecioVenta.SelectAll();
                return;
            }

            decimal subtotalProductos = Convert.ToDecimal(txtTotalPagar.Text);
            if (!SolicitarTotalFactura(subtotalProductos, out decimal totalFactura, out decimal montoNoInventariable))
                return;

            DialogResult confirmar = CapaPresentacion.DialogoSistema.Show(this,
                "Revise la compra antes de registrarla:\n\n" +
                "Productos cargados: Q " + subtotalProductos.ToString("N2") + "\n" +
                "Importe fuera de inventario: Q " + montoNoInventariable.ToString("N2") + "\n" +
                "Total de factura: Q " + totalFactura.ToString("N2") + "\n\n" +
                "¿Confirma el registro?",
                "Confirmar compra", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (confirmar != DialogResult.Yes) return;

            DataTable detalle_compra = new DataTable();

            detalle_compra.Columns.Add("IdProducto", typeof(int));
            detalle_compra.Columns.Add("PrecioCompra", typeof(decimal));
            detalle_compra.Columns.Add("PrecioVenta", typeof(decimal));
            detalle_compra.Columns.Add("Porcentaje", typeof(decimal));
            detalle_compra.Columns.Add("Cantidad", typeof(int));
            detalle_compra.Columns.Add("MontoTotal", typeof(decimal));

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                detalle_compra.Rows.Add(
                    new object[]
                    {
                        Convert.ToInt32(row.Cells["IdProducto"].Value.ToString()),
                        Convert.ToDecimal(row.Cells["PrecioCompra"].Value),
                        Convert.ToDecimal(row.Cells["PrecioVenta"].Value),
                        // El TVP y PRODUCTO guardan el margen como fracción (0.61 = 61%).
                        Convert.ToDecimal(row.Cells["Porcentaje"].Value),
                        Convert.ToInt32(row.Cells["Cantidad"].Value),
                        Convert.ToDecimal(row.Cells["SubTotal"].Value),

                    });
            }

            Compra oCompra = new Compra()
            {
                oUsuario = new Usuario() { IdUsuario = _Usuario.IdUsuario },
                oProveedor = new Proveedor() { IdProveedor = Convert.ToInt32(txtIdProveedor.Text) },
                TipoDocumento = ((OpcionCombo)cboTipoDocumento.SelectedItem).Texto,
                NumeroDocumento = string.Empty,
                MontoTotal = totalFactura,
                MontoNoInventariable = montoNoInventariable,
                NumeroFacturaProveedor = string.Empty
            };


            String mensaje = string.Empty;
            bool respuesta = new CN_Compra().Registrar(oCompra, detalle_compra, out mensaje);

            if (respuesta)
            {
                var result = CapaPresentacion.DialogoSistema.Show("Numero de Compra generada: \n"+oCompra.NumeroDocumento+"\n\n¿Desea Copiar al porta papeles?", "Mensaje",
                    MessageBoxButtons.YesNo, MessageBoxIcon.None);
                if(result == DialogResult.Yes)
                {
                    Clipboard.SetText(oCompra.NumeroDocumento);
                }

                    txtIdProducto.Text = "";
                txtNuevoDocumento.Text = "";
                txtIdProveedor.Text = "";
                txtRazonSocial.Text = "";
                dataGridView1.Rows.Clear();
                calculatTotal();

            }
            else
            {
                CapaPresentacion.DialogoSistema.Show(mensaje, "Mensaje del sistema", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            if (dataGridView1.Columns[e.ColumnIndex].Name == "btnEliminar")
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All);

                var w = Properties.Resources.Check.Width;
                var h = Properties.Resources.Check.Height;
                var x = e.CellBounds.Left + (e.CellBounds.Width - w) / 2;
                var y = e.CellBounds.Top + (e.CellBounds.Height - h) / 2;

                e.Graphics.DrawImage(Properties.Resources.Check, new Rectangle(x, y, w, h));
                e.Handled = true;
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || dataGridView1.Columns[e.ColumnIndex].Name != "btnEliminar")
                return;

            int index = e.RowIndex;

            if (CapaPresentacion.DialogoSistema.Show("¿Desea eliminar Producto?", "Mensaje del sistema", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
            {
                dataGridView1.Rows.RemoveAt(index);
                calculatTotal();
                    //string mensaje = string.Empty;
                    //Producto oProducto = new Producto
                    //{
                    //    IdProducto = Convert.ToInt32(dataGridView1.Rows[index].Cells["IdProducto"].Value.ToString())
                    //};    

                    //bool respuesta = new CN_Producto().Eliminar(oProducto, out mensaje);

                    //if (respuesta)
                    //{
                        
                    //}
                    //else CapaPresentacion.DialogoSistema.Show(mensaje, "Mensaje del sistema", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                    
            }
            
        

        private void txtCantidad_ValueChanged(object sender, EventArgs e)
        {
            if (txtCantidad.Value < 1)
            {
                txtCantidad.Value = 1;
            }
        }
    }
}
