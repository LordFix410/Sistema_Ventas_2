using CapaEntidad;
using CapaNegocio;
using CapaPresentacion.Utilidades;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CapaPresentacion
{
    public partial class frmProducto : Form
    {
        private bool sincronizandoPreciosProducto;
        private readonly string claveUsuario;
        private string rutaImagenActual;
        private string archivoImagenPendiente;
        private bool archivoImagenTemporal;
        private bool quitarImagen;
        private int rotacionFotoPendiente;

        public frmProducto() : this(string.Empty) { }

        public frmProducto(string clave)
        {
            claveUsuario = clave ?? string.Empty;
            InitializeComponent();
            ConfigurarSeccionesVisuales();
            ConfigurarFotoProducto();
            dgvData.CellClick += dgvData_CellClick;
            txtPrecioVentaProducto.KeyDown += PrecioVentaProducto_KeyDown;
            txtCostoProducto.Leave += delegate { ActualizarPorcentajeProducto(); };
            txtPorcentaje.KeyDown += PorcentajeProducto_KeyDown;
            cboGrupoProveedor.DropDown += (s, e) => CargarPerfilesProveedor(IdCombo(cboGrupoProveedor));
            txtCostoProducto.Enter += delegate { txtCostoProducto.SelectAll(); };
            txtPrecioVentaProducto.Enter += delegate { txtPrecioVentaProducto.SelectAll(); };
            txtPorcentaje.Enter += delegate { txtPorcentaje.SelectAll(); };
            txtPrecioVentaProducto.Leave += delegate { ActualizarPorcentajeProducto(); };
            txtPorcentaje.Leave += delegate { ActualizarPrecioDesdeMargenSinAviso(); };
            ConfigurarOrdenCaptura();
            ConfigurarOrdenColumnas();
            ExperienciaUsuario.Aplicar(this, txtCodigo, txtBusqueda, txtPorcentaje);
            dgvData.RowTemplate.Height = 88;
            dgvData.ColumnHeadersHeight = 52;
            dgvData.DefaultCellStyle.Font = new Font("Segoe UI", 10F);
            dgvData.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            MejorarTipografiaDetalle();
        }

        private void MejorarTipografiaDetalle()
        {
            foreach (Control control in new Control[] { label2, label3, label4, label5, label8,
                label14, label15, label16, label17 })
                control.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

            foreach (Control control in new Control[] { label18, labelPorcentaje, label19 })
                control.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

            foreach (Control control in new Control[] { txtCodigo, txtNombre, txtDescripcion,
                cboCategoria, cboGrupoProveedor, cboGrupoReposicion, txtCantidadMinima,
                txtCantidadMaxima, cboEstado })
                control.Font = new Font("Segoe UI", 10F, FontStyle.Regular);

            foreach (TextBox precio in new[] { txtCostoProducto, txtPorcentaje, txtPrecioVentaProducto })
                precio.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);

            txtCostoProducto.Width = 60;
            txtPorcentaje.Width = 60;
            txtPrecioVentaProducto.Width = 60;
            txtPorcentaje.Left = 103;
            txtPrecioVentaProducto.Left = 184;

            foreach (Button boton in new Button[] { btnElegir, btnQuitar, btnGirar, btnGuardar, btnCancelarUsuario, btnEliminar })
                boton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        }

        private void ConfigurarSeccionesVisuales()
        {
            MoverVertical(new Control[] { label2, txtCodigo, label3, txtNombre, label4, txtDescripcion, label5, cboCategoria }, 20);
            MoverVertical(new Control[] { label18, txtCostoProducto, labelPorcentaje, txtPorcentaje, label19, txtPrecioVentaProducto,
                picFotoProduc, btnElegir, btnQuitar, btnGirar }, 32);
            MoverVertical(new Control[] { label17, cboGrupoProveedor, label14, cboGrupoReposicion,
                label16, txtCantidadMinima, label15, txtCantidadMaxima }, 35);
            MoverVertical(new Control[] { label8, cboEstado }, 59);
            MoverVertical(new Control[] { btnGuardar, btnCancelarUsuario, btnEliminar }, 52);

            AgregarSeccionVisual("Datos básicos", new Rectangle(12, 40, 250, 234),
                Color.FromArgb(238, 246, 252), Color.FromArgb(43, 105, 157));
            AgregarSeccionVisual("Precio e imagen", new Rectangle(12, 277, 250, 247),
                Color.FromArgb(238, 248, 241), Color.FromArgb(36, 130, 76));
            AgregarSeccionVisual("Reposición", new Rectangle(12, 527, 250, 179),
                Color.FromArgb(255, 248, 228), Color.FromArgb(177, 124, 0));
            AgregarSeccionVisual("Estado y acciones", new Rectangle(12, 709, 250, 216),
                Color.FromArgb(244, 246, 248), Color.FromArgb(75, 91, 108));
            label1.SendToBack();

            label9.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            label9.ForeColor = Color.FromArgb(35, 75, 108);
            label10.BackColor = Color.FromArgb(224, 237, 247);
            label10.ForeColor = Color.FromArgb(35, 75, 108);
            label10.BorderStyle = BorderStyle.FixedSingle;
            label10.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
            label10.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            label10.Width = Math.Max(979, ClientSize.Width - label10.Left - 18);

            dgvData.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvData.Width = Math.Max(700, ClientSize.Width - dgvData.Left - 18);
            dgvData.BackgroundColor = Color.White;
            dgvData.GridColor = Color.FromArgb(211, 222, 232);
        }

        private static void MoverVertical(IEnumerable<Control> controles, int desplazamiento)
        {
            foreach (Control control in controles)
                control.Top += desplazamiento;
        }

        private void AgregarSeccionVisual(string titulo, Rectangle limites, Color fondo, Color acento)
        {
            var panel = new Panel
            {
                Bounds = limites,
                BackColor = fondo,
                BorderStyle = BorderStyle.FixedSingle,
                Name = "panelVisual" + titulo.Replace(" ", string.Empty)
            };
            panel.Paint += (s, e) =>
            {
                using (var pincel = new SolidBrush(acento))
                    e.Graphics.FillRectangle(pincel, 0, 0, 5, panel.Height);
            };
            panel.Controls.Add(new Label
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                ForeColor = acento,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(11, 3),
                Text = titulo.ToUpperInvariant()
            });
            Controls.Add(panel);
            panel.SendToBack();
        }

        private void ConfigurarFotoProducto()
        {
            if (!dgvData.Columns.Contains("Foto"))
            {
                var foto = new DataGridViewImageColumn
                {
                    Name = "Foto", HeaderText = "FOTO", ImageLayout = DataGridViewImageCellLayout.Zoom,
                    Width = 68, MinimumWidth = 60, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable
                };
                dgvData.Columns.Insert(1, foto);
            }
            picFotoProduc.BorderStyle = BorderStyle.FixedSingle;
            picFotoProduc.SizeMode = PictureBoxSizeMode.Zoom;
            picFotoProduc.BackColor = Color.White;
            picFotoProduc.Cursor = Cursors.Hand;
            picFotoProduc.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(rutaImagenActual))
                    GestorImagenProducto.MostrarVistaPrevia(this, rutaImagenActual, txtNombre.Text);
                else if (!string.IsNullOrWhiteSpace(archivoImagenPendiente))
                    MostrarFotoLocal(archivoImagenPendiente);
                else SeleccionarFotoProducto();
            };
            btnElegir.Click += (s, e) => SeleccionarFotoProducto();
            btnGirar.Click += (s, e) => GirarFotoPendiente();
            btnGirar.Enabled = false;
            btnQuitar.Click += (s, e) =>
            {
                LimpiarArchivoTemporal();
                archivoImagenPendiente = null;
                rotacionFotoPendiente = 0;
                btnGirar.Enabled = false;
                quitarImagen = true;
                rutaImagenActual = null;
                MostrarImagen(null);
            };
        }

        private void SeleccionarFotoProducto()
        {
            using (var dialogo = new OpenFileDialog
            {
                Title = "Seleccionar foto del producto",
                Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp",
                CheckFileExists = true
            })
            {
                if (dialogo.ShowDialog(this) != DialogResult.OK) return;
                LimpiarArchivoTemporal();
                Image prueba = GestorImagenProducto.CargarArchivoMiniatura(dialogo.FileName, 160, 176, 0);
                if (prueba == null)
                {
                    CapaPresentacion.DialogoSistema.Show("No se pudo abrir la imagen seleccionada.", "Foto inválida",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                archivoImagenPendiente = dialogo.FileName;
                archivoImagenTemporal = false;
                rotacionFotoPendiente = 0;
                btnGirar.Enabled = true;
                quitarImagen = false;
                MostrarImagen(prueba);
            }
        }

        private void GirarFotoPendiente()
        {
            if (string.IsNullOrWhiteSpace(archivoImagenPendiente))
            {
                if (string.IsNullOrWhiteSpace(rutaImagenActual)) return;
                try
                {
                    archivoImagenPendiente = GestorImagenProducto.CrearCopiaTemporal(rutaImagenActual);
                    archivoImagenTemporal = true;
                    quitarImagen = false;
                }
                catch (Exception ex)
                {
                    CapaPresentacion.DialogoSistema.Show(ex.Message, "No se pudo preparar la foto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            rotacionFotoPendiente = (rotacionFotoPendiente + 90) % 360;
            Image girada = GestorImagenProducto.CargarArchivoMiniatura(archivoImagenPendiente, 160, 176, rotacionFotoPendiente);
            if (girada == null)
            {
                CapaPresentacion.DialogoSistema.Show("No se pudo girar la foto seleccionada.", "Foto del producto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            MostrarImagen(girada);
        }

        private void MostrarFotoLocal(string archivo)
        {
            using (Image imagen = Image.FromFile(archivo))
            using (var ventana = new Form())
            using (var cuadro = new PictureBox())
            {
                ventana.Text = "Vista previa · " + txtNombre.Text;
                ventana.StartPosition = FormStartPosition.CenterParent;
                ventana.Size = new Size(640, 600);
                cuadro.Dock = DockStyle.Fill; cuadro.SizeMode = PictureBoxSizeMode.Zoom; cuadro.Image = new Bitmap(imagen);
                ventana.Controls.Add(cuadro); ventana.ShowDialog(this);
            }
        }

        private void MostrarImagen(Image imagen)
        {
            Image anterior = picFotoProduc.Image;
            picFotoProduc.Image = imagen;
            if (anterior != null) anterior.Dispose();
        }

        private void CargarFoto(string ruta)
        {
            LimpiarArchivoTemporal();
            rutaImagenActual = ruta;
            archivoImagenPendiente = null;
            rotacionFotoPendiente = 0;
            btnGirar.Enabled = !string.IsNullOrWhiteSpace(ruta);
            quitarImagen = false;
            MostrarImagen(GestorImagenProducto.CargarMiniatura(ruta, 160, 176));
        }

        private string PrepararRutaImagen(out bool archivoNuevo)
        {
            archivoNuevo = false;
            if (quitarImagen) return null;
            if (string.IsNullOrWhiteSpace(archivoImagenPendiente)) return rutaImagenActual;
            string ruta = GestorImagenProducto.Guardar(archivoImagenPendiente, claveUsuario, rotacionFotoPendiente);
            archivoNuevo = true;
            return ruta;
        }

        private void ConfigurarOrdenCaptura()
        {
            Control[] orden = { txtCodigo, txtNombre, txtDescripcion, cboCategoria, txtCostoProducto,
                txtPorcentaje, txtPrecioVentaProducto, cboGrupoProveedor, cboGrupoReposicion,
                txtCantidadMinima, txtCantidadMaxima, cboEstado, btnGuardar, btnCancelarUsuario, btnEliminar };
            for (int i = 0; i < orden.Length; i++)
            {
                orden[i].TabIndex = i;
                orden[i].TabStop = true;
            }
            txtBusqueda.TabIndex = orden.Length + 2;
            cboBuscar.TabIndex = orden.Length + 1;
        }

        private void PrecioVentaProducto_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            if (!decimal.TryParse(txtPrecioVentaProducto.Text, out decimal venta) || venta < 0)
            {
                CapaPresentacion.DialogoSistema.Show("Ingresa un precio de venta válido.", "Precio inválido",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrecioVentaProducto.Focus();
                txtPrecioVentaProducto.SelectAll();
                return;
            }
            if (decimal.TryParse(txtCostoProducto.Text, out decimal costoActual) && costoActual == 0 &&
                (!decimal.TryParse(txtPorcentaje.Text, out decimal margenActual) || margenActual <= 0))
            {
                CapaPresentacion.DialogoSistema.Show("Con costo desconocido, ingresa un margen mayor que cero para calcular el costo.",
                    "Margen requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPorcentaje.Focus();
                txtPorcentaje.SelectAll();
                return;
            }
            ActualizarPorcentajeProducto();
            if (decimal.TryParse(txtCostoProducto.Text, out decimal costo) && costo == 0 &&
                decimal.TryParse(txtPorcentaje.Text, out decimal margen) && margen > 0)
            {
                if (!CalcularCostoDesdeVentaYMargen()) return;
            }
            cboGrupoProveedor.Focus();
        }

        private void ActualizarPrecioDesdeMargenSinAviso()
        {
            if (sincronizandoPreciosProducto ||
                !decimal.TryParse(txtCostoProducto.Text, out decimal costo) || costo <= 0 ||
                !decimal.TryParse(txtPorcentaje.Text, out decimal margen) || margen < 0)
                return;
            decimal venta = Math.Ceiling(costo * (1m + margen / 100m) * 4m) / 4m;
            sincronizandoPreciosProducto = true;
            txtPrecioVentaProducto.Text = venta.ToString("0.00");
            sincronizandoPreciosProducto = false;
        }

        private void ActualizarPorcentajeProducto()
        {
            if (sincronizandoPreciosProducto ||
                !decimal.TryParse(txtCostoProducto.Text, out decimal costo) || costo <= 0 ||
                !decimal.TryParse(txtPrecioVentaProducto.Text, out decimal venta)) return;

            decimal margen = (venta - costo) / costo * 100m;
            sincronizandoPreciosProducto = true;
            txtPorcentaje.Text = margen.ToString("0.##");
            sincronizandoPreciosProducto = false;
        }

        private bool CalcularCostoDesdeVentaYMargen()
        {
            if (!decimal.TryParse(txtPrecioVentaProducto.Text, out decimal venta) || venta <= 0 ||
                !decimal.TryParse(txtPorcentaje.Text, out decimal margen) || margen <= 0)
                return true;

            decimal costo = Math.Round(venta / (1m + margen / 100m), 2, MidpointRounding.AwayFromZero);
            if (costo <= 0)
            {
                CapaPresentacion.DialogoSistema.Show("Con esos valores el costo calculado queda en cero. Revisa el precio y el margen.",
                    "Costo calculado inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrecioVentaProducto.Focus();
                txtPrecioVentaProducto.SelectAll();
                return false;
            }

            sincronizandoPreciosProducto = true;
            txtCostoProducto.Text = costo.ToString("0.00");
            sincronizandoPreciosProducto = false;
            ActualizarPorcentajeProducto();
            return true;
        }

        private void PorcentajeProducto_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            if (!decimal.TryParse(txtCostoProducto.Text, out decimal costo) || costo < 0 ||
                !decimal.TryParse(txtPorcentaje.Text, out decimal margen) || margen < 0)
            {
                CapaPresentacion.DialogoSistema.Show("Ingresa un costo y un margen válidos.", "Precios inválidos",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPorcentaje.Focus();
                txtPorcentaje.SelectAll();
                return;
            }

            if (costo == 0)
            {
                if (margen <= 0)
                {
                    CapaPresentacion.DialogoSistema.Show(
                        "Para calcular el costo, ingresa un margen mayor que cero.",
                        "Datos incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPorcentaje.Focus();
                    txtPorcentaje.SelectAll();
                    return;
                }
                if (!decimal.TryParse(txtPrecioVentaProducto.Text, out decimal ventaExistente) || ventaExistente <= 0)
                {
                    txtPrecioVentaProducto.Focus();
                    txtPrecioVentaProducto.SelectAll();
                    return;
                }
                if (!CalcularCostoDesdeVentaYMargen()) return;
                txtPrecioVentaProducto.Focus();
                txtPrecioVentaProducto.SelectAll();
                return;
            }

            decimal venta = Math.Ceiling(costo * (1m + margen / 100m) * 4m) / 4m;
            sincronizandoPreciosProducto = true;
            txtPrecioVentaProducto.Text = venta.ToString("0.00");
            sincronizandoPreciosProducto = false;
            ActualizarPorcentajeProducto();
            txtPrecioVentaProducto.Focus();
            txtPrecioVentaProducto.SelectAll();
        }

        private static decimal PorcentajeGuardado(decimal costo, decimal venta, decimal porcentajeSinCosto)
        {
            return costo <= 0 ? porcentajeSinCosto :
                decimal.Ceiling(((venta - costo) / costo) * 10000m) / 10000m;
        }

        private void ConfigurarOrdenColumnas()
        {
            if (dgvData.Columns.Contains("btnSeleccionar"))
            {
                var columnaAccion = dgvData.Columns["btnSeleccionar"] as DataGridViewButtonColumn;
                if (columnaAccion != null)
                {
                    columnaAccion.FlatStyle = FlatStyle.Flat;
                    columnaAccion.DefaultCellStyle.Padding = new Padding(0);
                }
            }

            string[] orden = { "btnSeleccionar", "Foto", "Codigo", "Nombre", "Descripcion", "Categoria", "Stock",
                "CantidadMinima", "CantidadMaxima", "PRECIOCOMPRA", "PRECIOVENTA", "PORCENTAJE",
                "GrupoReposicion", "GrupoProveedores", "Estado" };
            for (int i = 0; i < orden.Length; i++)
                if (dgvData.Columns.Contains(orden[i])) dgvData.Columns[orden[i]].DisplayIndex = i;
        }

        private int AgregarFilaProducto(Producto producto, int idProducto, string codigo, string nombre,
            string descripcion, int idCategoria, string categoria, int stock, decimal precioCompra,
            decimal precioVenta, decimal porcentaje, int? minimo, int? maximo, string grupoReposicion,
            string grupoProveedores, bool activo)
        {
            int indice = dgvData.Rows.Add();
            DataGridViewRow fila = dgvData.Rows[indice];
            fila.Cells["IdProducto"].Value = idProducto;
            fila.Cells["Codigo"].Value = codigo;
            fila.Cells["Nombre"].Value = nombre;
            fila.Cells["Descripcion"].Value = descripcion;
            fila.Cells["IDCATEGORIA"].Value = idCategoria;
            fila.Cells["Categoria"].Value = categoria;
            fila.Cells["Stock"].Value = stock;
            fila.Cells["PRECIOCOMPRA"].Value = precioCompra;
            fila.Cells["PRECIOVENTA"].Value = precioVenta;
            fila.Cells["PORCENTAJE"].Value = FormatoPorcentaje.ParaMostrar(porcentaje, precioCompra);
            fila.Cells["GrupoReposicion"].Value = grupoReposicion;
            fila.Cells["CantidadMinima"].Value = minimo.HasValue ? minimo.Value.ToString() : "";
            fila.Cells["CantidadMaxima"].Value = maximo.HasValue ? maximo.Value.ToString() : "";
            fila.Cells["GrupoProveedores"].Value = grupoProveedores;
            fila.Cells["EstadoValor"].Value = activo ? 1 : 0;
            fila.Cells["Estado"].Value = activo ? "Activo" : "Inactivo";
            fila.Cells["Foto"].Value = GestorImagenProducto.CargarMiniatura(producto == null ? null : producto.ImagenRuta, 76, 76);
            fila.Tag = producto;
            return indice;
        }

        private void frmProducto_Load(object sender, EventArgs e)
        {
            cboEstado.Items.Add(new OpcionCombo() { Valor = 1, Texto = "Activo" });
            cboEstado.Items.Add(new OpcionCombo() { Valor = 0, Texto = "Inactivo" });
            cboEstado.DisplayMember = "Texto";
            cboEstado.ValueMember = "Valor";
            cboEstado.SelectedIndex = 0;
            txtPorcentaje.Text = "61";
            txtCostoProducto.Text = "0.00";
            txtPrecioVentaProducto.Text = "0.00";
            txtCostoProducto.Text = "0.00";
            txtPrecioVentaProducto.Text = "0.00";

            CargarGruposReposicion();
            CargarPerfilesProveedor();
            cboGrupoReposicion.SelectedIndexChanged += (s, ev) => ActualizarLimitesReposicion();

            List<Categoria> lista = new CN_Categoria().Listar();

            foreach (Categoria producto in lista)
            {
                cboCategoria.Items.Add(new OpcionCombo() { Valor = producto.IdCategoria, Texto = producto.Descripcion });
            }
            cboCategoria.DisplayMember = "Texto";
            cboCategoria.ValueMember = "Valor";
            cboCategoria.SelectedIndex = 0;

            if (dgvData.Columns.Contains("CantidadMinima")) dgvData.Columns["CantidadMinima"].HeaderText = "CANTIDAD\nMÍNIMA";
            if (dgvData.Columns.Contains("CantidadMaxima")) dgvData.Columns["CantidadMaxima"].HeaderText = "CANTIDAD\nMÁXIMA";
            foreach (DataGridViewColumn columna in dgvData.Columns)
            {
                if (columna.Visible == true && columna.Name != "btnSeleccionar" && columna.Name != "Foto")
                {
                    cboBuscar.Items.Add(new OpcionCombo() { Valor = columna.Name, Texto = columna.HeaderText });
                }
            }
            cboBuscar.DisplayMember = "Texto";
            cboBuscar.ValueMember = "Valor";
            cboBuscar.SelectedIndex = 0;

            // La lista se consulta solo cuando el usuario busca; abrir el mantenedor no lee todo PRODUCTO.
            dgvData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvData.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvData.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvData.RowTemplate.Height = 88;
            var anchos = new Dictionary<string, int>
            {
                ["Codigo"] = 85, ["Nombre"] = 145, ["Descripcion"] = 160, ["Categoria"] = 105,
                ["Stock"] = 60, ["CantidadMinima"] = 72, ["CantidadMaxima"] = 72,
                ["PRECIOCOMPRA"] = 85, ["PRECIOVENTA"] = 85, ["PORCENTAJE"] = 80,
                ["GrupoReposicion"] = 125, ["GrupoProveedores"] = 125, ["Estado"] = 70
            };
            foreach (var ancho in anchos)
                if (dgvData.Columns.Contains(ancho.Key)) dgvData.Columns[ancho.Key].Width = ancho.Value;
            if (dgvData.Columns.Contains("btnSeleccionar")) dgvData.Columns["btnSeleccionar"].Width = 38;
            if (dgvData.Columns.Contains("Foto")) dgvData.Columns["Foto"].Width = 88;
            dgvData.RowTemplate.Height = 88;
            foreach (DataGridViewRow fila in dgvData.Rows)
            {
                fila.Height = 88;
                fila.MinimumHeight = 88;
            }
            ActualizarLimitesReposicion();

        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            string mensaje = string.Empty;
            if (!decimal.TryParse(txtPorcentaje.Text, out decimal porcentaje) || porcentaje < 0)
            {
                CapaPresentacion.DialogoSistema.Show("Ingrese un porcentaje válido igual o mayor a cero.", "Porcentaje inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPorcentaje.Focus();
                txtPorcentaje.SelectAll();
                return;
            }

            if (!decimal.TryParse(txtCostoProducto.Text, out decimal costo) || costo < 0 ||
                !decimal.TryParse(txtPrecioVentaProducto.Text, out decimal precioVenta) || precioVenta < 0)
            {
                CapaPresentacion.DialogoSistema.Show("Ingresa un costo y precio de venta válidos, iguales o mayores a cero.",
                    "Precios inválidos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCostoProducto.Focus();
                txtCostoProducto.SelectAll();
                return;
            }
            if (costo == 0)
            {
                if (porcentaje <= 0 || precioVenta <= 0)
                {
                    CapaPresentacion.DialogoSistema.Show("Con costo desconocido, ingresa un porcentaje mayor que cero y un precio de venta para calcular el costo.",
                        "Faltan datos para calcular el costo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPorcentaje.Focus();
                    txtPorcentaje.SelectAll();
                    return;
                }
                if (!CalcularCostoDesdeVentaYMargen()) return;
                decimal.TryParse(txtCostoProducto.Text, out costo);
                decimal.TryParse(txtPorcentaje.Text, out porcentaje);
            }
            if (costo > 0 && precioVenta < costo)
            {
                CapaPresentacion.DialogoSistema.Show("El precio de venta no puede ser menor al costo.",
                    "Margen negativo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrecioVentaProducto.Focus();
                txtPrecioVentaProducto.SelectAll();
                return;
            }
            if (Math.Abs(precioVenta * 4m - decimal.Round(precioVenta * 4m, 0)) > 0.0001m)
            {
                CapaPresentacion.DialogoSistema.Show("El precio de venta debe terminar en .00, .25, .50 o .75.",
                    "Precio inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrecioVentaProducto.Focus();
                txtPrecioVentaProducto.SelectAll();
                return;
            }

            int? idGrupo = IdCombo(cboGrupoReposicion);
            int? minimo = null, maximo = null;
            if (!idGrupo.HasValue)
            {
                int minVal, maxVal;
                if (!int.TryParse(txtCantidadMinima.Text, out minVal) || !int.TryParse(txtCantidadMaxima.Text, out maxVal) || minVal < 0 || maxVal < minVal)
                {
                    CapaPresentacion.DialogoSistema.Show("Sin grupo de productos debes ingresar mínimo y máximo como números enteros no negativos. El máximo debe ser igual o mayor que el mínimo.", "Límites inválidos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    if (!int.TryParse(txtCantidadMinima.Text, out minVal) || minVal < 0) txtCantidadMinima.Focus();
                    else txtCantidadMaxima.Focus();
                    return;
                }
                minimo = minVal; maximo = maxVal;
            }

            Producto objProducto = new Producto()
            {
                IdProducto = Convert.ToInt32(txtId.Text),
                Codigo = txtCodigo.Text,
                Nombre = txtNombre.Text,
                Descripcion = txtDescripcion.Text,
                oCategoria = new Categoria() { IdCategoria = Convert.ToInt32(((OpcionCombo)cboCategoria.SelectedItem).Valor) },
                Estado = Convert.ToInt32(((OpcionCombo)cboEstado.SelectedItem).Valor) == 1,
                Porcentaje = porcentaje,
                PrecioCompra = costo,
                PrecioVenta = precioVenta,
                IdGrupoReposicion = idGrupo,
                CantidadMinima = minimo,
                CantidadMaxima = maximo,
                IdGrupoProveedor = IdCombo(cboGrupoProveedor)
            };
            string imagenAnterior = rutaImagenActual;
            bool archivoFotoNuevo;
            try { objProducto.ImagenRuta = PrepararRutaImagen(out archivoFotoNuevo); }
            catch (Exception ex)
            {
                CapaPresentacion.DialogoSistema.Show("No se pudo enviar la foto al servidor web. Verifica que el servicio esté activo y accesible desde esta PC.\n\n" + ex.Message,
                    "Foto del producto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (objProducto.IdProducto == 0)
            {
                int idnuevo = new CN_Producto().Registrar(objProducto, out mensaje);

                if (mensaje == string.Empty && idnuevo != 0)
                {
                    CapaPresentacion.DialogoSistema.ShowSuccess("Producto registrado correctamente.");
                    objProducto.IdProducto = idnuevo;
                    AgregarFilaProducto(objProducto, idnuevo, txtCodigo.Text, txtNombre.Text, txtDescripcion.Text,
                        Convert.ToInt32(((OpcionCombo)cboCategoria.SelectedItem).Valor),
                        ((OpcionCombo)cboCategoria.SelectedItem).Texto.ToString(), 0, costo, precioVenta,
                        PorcentajeGuardado(costo, precioVenta, porcentaje),
                        minimo, maximo, GrupoNombre(cboGrupoReposicion), GrupoNombre(cboGrupoProveedor), objProducto.Estado);
                    objProducto.Porcentaje = PorcentajeGuardado(costo, precioVenta, porcentaje);
                    ConfirmarCambioFoto(imagenAnterior, objProducto.ImagenRuta, archivoFotoNuevo);
                    LimpiarTxt();
                }
                else
                {
                    DescartarFotoNueva(objProducto.ImagenRuta, archivoFotoNuevo);
                    CapaPresentacion.DialogoSistema.Show(mensaje, "Error del Registro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                bool resultado = new CN_Producto().Editar(objProducto, out mensaje);
                if (resultado)
                {
                    DataGridViewRow row = dgvData.Rows[Convert.ToInt32(txtIndice.Text)];
                    row.Cells["IdProducto"].Value = txtId.Text;
                    row.Cells["Codigo"].Value = txtCodigo.Text;
                    row.Cells["Nombre"].Value = txtNombre.Text;
                    row.Cells["Descripcion"].Value = txtDescripcion.Text;
                    row.Cells["IDCATEGORIA"].Value = ((OpcionCombo)cboCategoria.SelectedItem).Valor.ToString();
                    row.Cells["Categoria"].Value = ((OpcionCombo)cboCategoria.SelectedItem).Texto.ToString();
                    row.Cells["EstadoValor"].Value = ((OpcionCombo)cboEstado.SelectedItem).Valor.ToString();
                    row.Cells["Estado"].Value = ((OpcionCombo)cboEstado.SelectedItem).Texto.ToString();
                    row.Cells["PORCENTAJE"].Value = objProducto.Porcentaje;
                    row.Cells["PRECIOCOMPRA"].Value = costo;
                    row.Cells["PRECIOVENTA"].Value = precioVenta;
                    decimal porcentajeReal = PorcentajeGuardado(costo, precioVenta, porcentaje);
                    row.Cells["PORCENTAJE"].Value = FormatoPorcentaje.ParaMostrar(porcentajeReal, costo);
                    row.Cells["GrupoReposicion"].Value = GrupoNombre(cboGrupoReposicion);
                    row.Cells["CantidadMinima"].Value = minimo.HasValue ? minimo.Value.ToString() : "";
                    row.Cells["GrupoProveedores"].Value = GrupoNombre(cboGrupoProveedor);
                    row.Cells["CantidadMaxima"].Value = maximo.HasValue ? maximo.Value.ToString() : "";
                    objProducto.Porcentaje = porcentajeReal;
                    objProducto.PrecioCompra = costo;
                    objProducto.PrecioVenta = precioVenta;
                    row.Tag = objProducto;
                    row.Cells["Foto"].Value = GestorImagenProducto.CargarMiniatura(objProducto.ImagenRuta, 76, 76);
                    ConfirmarCambioFoto(imagenAnterior, objProducto.ImagenRuta, archivoFotoNuevo);

                    LimpiarTxt();
                }
                else
                {
                    DescartarFotoNueva(objProducto.ImagenRuta, archivoFotoNuevo);
                    CapaPresentacion.DialogoSistema.Show(mensaje, "No se pudo actualizar el producto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

        }

        private void ConfirmarCambioFoto(string anterior, string actual, bool archivoNuevo)
        {
            rutaImagenActual = actual;
            LimpiarArchivoTemporal();
            archivoImagenPendiente = null;
            rotacionFotoPendiente = 0;
            btnGirar.Enabled = !string.IsNullOrWhiteSpace(actual);
            quitarImagen = false;
            if (!string.IsNullOrWhiteSpace(anterior) && !string.Equals(anterior, actual, StringComparison.OrdinalIgnoreCase))
                try { GestorImagenProducto.Eliminar(anterior, claveUsuario); } catch { }
            if (!archivoNuevo) rutaImagenActual = actual;
        }

        private void DescartarFotoNueva(string ruta, bool archivoNuevo)
        {
            if (archivoNuevo && !string.IsNullOrWhiteSpace(ruta))
                try { GestorImagenProducto.Eliminar(ruta, claveUsuario); } catch { }
        }

        private void LimpiarArchivoTemporal()
        {
            if (archivoImagenTemporal && !string.IsNullOrWhiteSpace(archivoImagenPendiente))
                try { System.IO.File.Delete(archivoImagenPendiente); } catch { }
            archivoImagenTemporal = false;
        }

        public void LimpiarTxt()
        {
            LimpiarArchivoTemporal();
            txtId.Text = "0";
            txtCodigo.Text = "";
            txtNombre.Text = "";
            txtDescripcion.Text = "";
            txtPorcentaje.Text = "61";
            txtCostoProducto.Text = "0.00";
            txtPrecioVentaProducto.Text = "0.00";
            cboCategoria.SelectedIndex = 0;
            cboEstado.SelectedIndex = 0;
            txtCantidadMinima.Clear(); txtCantidadMaxima.Clear();
            if (cboGrupoReposicion.Items.Count > 0) cboGrupoReposicion.SelectedIndex = 0;
            if (cboGrupoProveedor.Items.Count > 0) cboGrupoProveedor.SelectedIndex = 0;
            CargarFoto(null);
            ActualizarLimitesReposicion();

        }

        private void CargarGruposReposicion()
        {
            cboGrupoReposicion.Items.Clear(); cboGrupoReposicion.Items.Add(new OpcionReposicion(null, "Sin grupo (límites propios)"));
            foreach (DataRow fila in new CN_Reposicion().ListarGrupos().Rows)
                if (Convert.ToBoolean(fila["Activo"])) cboGrupoReposicion.Items.Add(new OpcionReposicion(Convert.ToInt32(fila["IdGrupoReposicion"]), Convert.ToString(fila["Nombre"])));
            cboGrupoReposicion.SelectedIndex = 0;
        }

        private void CargarPerfilesProveedor(int? idSeleccionado = null)
        {
            try
            {
                cboGrupoProveedor.BeginUpdate();
                cboGrupoProveedor.Items.Clear();
                cboGrupoProveedor.Items.Add(new OpcionReposicion(null, "Sin perfil de proveedores"));
                foreach (DataRow fila in new CN_Reposicion().ListarPerfiles().Rows)
                    if (Convert.ToBoolean(fila["Activo"]))
                        cboGrupoProveedor.Items.Add(new OpcionReposicion(Convert.ToInt32(fila["IdGrupoProveedor"]), Convert.ToString(fila["Nombre"])));
                SeleccionarCombo(cboGrupoProveedor, idSeleccionado);
            }
            catch (Exception ex)
            {
                cboGrupoProveedor.Items.Clear();
                cboGrupoProveedor.Items.Add(new OpcionReposicion(null, "No se pudieron cargar perfiles"));
                cboGrupoProveedor.SelectedIndex = 0;
                CapaPresentacion.DialogoSistema.Show(
                    "No se pudieron cargar los perfiles de proveedores. Verifica que ejecutaste migracion_reposicion_cotizaciones.sql y que la conexión tenga acceso a GRUPO_PROVEEDOR.\n\n" + ex.Message,
                    "Perfiles de proveedores", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { cboGrupoProveedor.EndUpdate(); }
        }

        private void ActualizarLimitesReposicion()
        {
            bool independiente = IdCombo(cboGrupoReposicion) == null;
            txtCantidadMinima.Enabled = independiente; txtCantidadMaxima.Enabled = independiente;
            if (!independiente) { txtCantidadMinima.Clear(); txtCantidadMaxima.Clear(); }
        }

        private static int? IdCombo(ComboBox combo)
        {
            OpcionReposicion opcion = combo.SelectedItem as OpcionReposicion;
            return opcion == null ? (int?)null : opcion.Id;
        }

        private static string GrupoNombre(ComboBox combo)
        {
            OpcionReposicion opcion = combo.SelectedItem as OpcionReposicion;
            return opcion == null || !opcion.Id.HasValue ? "" : opcion.Nombre;
        }

        private static void SeleccionarCombo(ComboBox combo, int? id)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                OpcionReposicion opcion = combo.Items[i] as OpcionReposicion;
                if (opcion != null && opcion.Id == id) { combo.SelectedIndex = i; return; }
            }
            combo.SelectedIndex = 0;
        }

        private sealed class OpcionReposicion
        {
            public int? Id { get; private set; }
            public string Nombre { get; private set; }
            public OpcionReposicion(int? id, string nombre) { Id = id; Nombre = nombre; }
            public override string ToString() { return Nombre; }
        }

        private void dgvData_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            if (dgvData.Columns[e.ColumnIndex].Name == "btnSeleccionar")
            {
                e.PaintBackground(e.CellBounds, true);
                e.Paint(e.CellBounds, DataGridViewPaintParts.Border);

                var w = Properties.Resources.Check.Width;
                var h = Properties.Resources.Check.Height;
                var escala = Math.Min(1f, Math.Min(26f / w, 26f / h));
                var ancho = Math.Max(1, (int)(w * escala));
                var alto = Math.Max(1, (int)(h * escala));
                var x = e.CellBounds.Left + (e.CellBounds.Width - ancho) / 2;
                var y = e.CellBounds.Top + (e.CellBounds.Height - alto) / 2;

                e.Graphics.DrawImage(Properties.Resources.Check, new Rectangle(x, y, ancho, alto));
                e.Handled = true;
            }
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            if (Convert.ToInt32(txtId.Text) != 0)
            {
                if (CapaPresentacion.DialogoSistema.Show("¿Desea Eliminar el Producto?", "Mensaje del sistema", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                {
                    string mensaje = string.Empty;
                    Producto oproducto = new Producto
                    {
                        IdProducto = Convert.ToInt32(txtId.Text)
                    };

                    bool respuesta = new CN_Producto().Eliminar(oproducto, out mensaje);

                    if (respuesta)
                    {
                    Producto productoEliminado = dgvData.Rows[Convert.ToInt32(txtIndice.Text)].Tag as Producto;
                        dgvData.Rows.RemoveAt(Convert.ToInt32(txtIndice.Text));
                    if (productoEliminado != null && !string.IsNullOrWhiteSpace(productoEliminado.ImagenRuta))
                        try { GestorImagenProducto.Eliminar(productoEliminado.ImagenRuta, claveUsuario); } catch { }
                        LimpiarTxt();
                    }
                    else CapaPresentacion.DialogoSistema.Show(mensaje, "Mensaje del sistema", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        private void btnCancelarUsuario_Click(object sender, EventArgs e)
        {
            LimpiarTxt();
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            string termino = txtBusqueda.Text.Trim();
            if (termino.Length == 0 && CapaPresentacion.DialogoSistema.Show(this,
                "El campo está vacío y se consultará toda la lista de productos. Si el inventario es grande, la carga puede tardar. ¿Deseas continuar?",
                "Confirmar carga completa", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            try
            {
                string columna = ((OpcionCombo)cboBuscar.SelectedItem).Valor.ToString();
                List<Producto> resultados = termino.Length == 0
                    ? new CN_Producto().Listar()
                    : new CN_Producto().Buscar(columna, termino);
                LimpiarFilasProductos();
                foreach (Producto item in resultados)
                    AgregarFilaProducto(item, item.IdProducto, item.Codigo, item.Nombre, item.Descripcion,
                        item.oCategoria.IdCategoria, item.oCategoria.Descripcion, item.Stock, item.PrecioCompra,
                        item.PrecioVenta, item.Porcentaje, item.CantidadMinima, item.CantidadMaxima,
                        item.GrupoReposicion ?? "", item.GrupoProveedores ?? "", item.Estado);
                if (resultados.Count == 0)
                    CapaPresentacion.DialogoSistema.Show("No se encontraron productos con ese criterio.", "Búsqueda",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                else if (resultados.Count == 500)
                    CapaPresentacion.DialogoSistema.Show("Se muestran los primeros 500 resultados. Especifica más la búsqueda para acotar la lista.",
                        "Resultados limitados", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                CapaPresentacion.DialogoSistema.Show(ex.Message + Environment.NewLine + ex.InnerException?.Message,
                    "No se pudieron buscar productos", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            txtBusqueda.Text = "";
            LimpiarFilasProductos();
            cboBuscar.SelectedIndex = 0;
            txtBusqueda.Focus();
        }

        private void dgvData_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvData.Columns[e.ColumnIndex].Name == "btnSeleccionar")
            {
                int indice = e.RowIndex;

                if (indice >= 0)
                {
                    txtIndice.Text = indice.ToString();
                    txtId.Text = dgvData.Rows[indice].Cells["IdProducto"].Value.ToString();
                    txtCodigo.Text = dgvData.Rows[indice].Cells["Codigo"].Value.ToString();
                    txtNombre.Text = dgvData.Rows[indice].Cells["Nombre"].Value.ToString();
                    txtDescripcion.Text = dgvData.Rows[indice].Cells["Descripcion"].Value.ToString();
                    sincronizandoPreciosProducto = true;
                    txtCostoProducto.Text = Convert.ToDecimal(dgvData.Rows[indice].Cells["PRECIOCOMPRA"].Value).ToString("0.00");
                    txtPrecioVentaProducto.Text = Convert.ToDecimal(dgvData.Rows[indice].Cells["PRECIOVENTA"].Value).ToString("0.00");
                    decimal porcentajeVisible = Convert.ToDecimal(dgvData.Rows[indice].Cells["PORCENTAJE"].Value);
                    txtPorcentaje.Text = porcentajeVisible.ToString("0.##");
                    sincronizandoPreciosProducto = false;
                    Producto productoFila = dgvData.Rows[indice].Tag as Producto;
                    CargarFoto(productoFila == null ? null : productoFila.ImagenRuta);
                    SeleccionarCombo(cboGrupoReposicion, productoFila == null ? (int?)null : productoFila.IdGrupoReposicion);
                    SeleccionarCombo(cboGrupoProveedor, productoFila == null ? (int?)null : productoFila.IdGrupoProveedor);
                    txtCantidadMinima.Text = Convert.ToString(dgvData.Rows[indice].Cells["CantidadMinima"].Value);
                    txtCantidadMaxima.Text = Convert.ToString(dgvData.Rows[indice].Cells["CantidadMaxima"].Value);
                    ActualizarLimitesReposicion();

                    foreach (OpcionCombo oc in cboCategoria.Items)
                    {
                        if (Convert.ToInt32(oc.Valor) == Convert.ToInt32(dgvData.Rows[indice].Cells["IDCATEGORIA"].Value))
                        {
                            cboCategoria.SelectedIndex = cboCategoria.Items.IndexOf(oc); ;
                            break;
                        }
                    }

                    foreach (OpcionCombo oc in cboEstado.Items)
                    {
                        if (Convert.ToInt32(oc.Valor) == Convert.ToInt32(dgvData.Rows[indice].Cells["EstadoValor"].Value))
                        {
                            cboEstado.SelectedIndex = cboEstado.Items.IndexOf(oc);
                            break;
                        }

                    }
                }
            }
        }

        private void LimpiarFilasProductos()
        {
            foreach (DataGridViewRow fila in dgvData.Rows)
                if (fila.Cells["Foto"].Value is Image imagen) imagen.Dispose();
            dgvData.Rows.Clear();
        }

        private void dgvData_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || dgvData.Columns[e.ColumnIndex].Name != "Foto") return;
            Producto producto = dgvData.Rows[e.RowIndex].Tag as Producto;
            if (producto != null && !string.IsNullOrWhiteSpace(producto.ImagenRuta))
                GestorImagenProducto.MostrarVistaPrevia(this, producto.ImagenRuta, producto.Nombre);
        }

        private void iconButton1_Click(object sender, EventArgs e)
        {
            if (dgvData.Rows.Count < 1)
            {
                CapaPresentacion.DialogoSistema.Show("No hay datos para exportar", "Mensaje del sistema", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                DataTable dt = new DataTable();

                List<DataGridViewColumn> columnasExportar = dgvData.Columns.Cast<DataGridViewColumn>()
                    .Where(c => c.Visible && c.HeaderText != "" && c.Name != "Foto").ToList();
                foreach (DataGridViewColumn columna in columnasExportar)
                {
                    dt.Columns.Add(columna.HeaderText, typeof(string));
                }

                foreach(DataGridViewRow row in dgvData.Rows)
                {
                    if (row.Visible)
                        dt.Rows.Add(columnasExportar.Select(c => Convert.ToString(row.Cells[c.Name].Value)).ToArray());
                }

                SaveFileDialog savefile = new SaveFileDialog();
                savefile.FileName = string.Format("ReporteProducto_{0}.xlsx", DateTime.Now.ToString("ddMMyyyyHHmmss"));
                savefile.Filter = "Excel Files | *.xlsx";

                if(savefile.ShowDialog()== DialogResult.OK)
                {
                    try
                    {
                        XLWorkbook wb = new XLWorkbook();
                        var hoja = wb.Worksheets.Add(dt, "Informe");
                        hoja.ColumnsUsed().AdjustToContents();
                        wb.SaveAs(savefile.FileName);
                        CapaPresentacion.DialogoSistema.ShowSuccess("Reporte generado.", "Productos");
                    }
                    catch
                    {
                        CapaPresentacion.DialogoSistema.Show("Error al generar el reporte", "mensaje del sistema", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

            }
        }
    }
}
