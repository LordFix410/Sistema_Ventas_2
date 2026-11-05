using CapaEntidad;
using CapaNegocio;
using CapaPresentacion.Modales;
using CapaPresentacion.Utilidades;
using DocumentFormat.OpenXml.Office.CustomUI;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CapaPresentacion
{
    public partial class frmVentas : Form
    {
        private Usuario _Usuario;
        private Cliente _clienteConsumidorFinal;
        private int _productoEnEdicionId;
        private Producto _productoActualSeleccionado;
        private int _siguienteIdTemporal = -1;
        public frmVentas(Usuario oUsuario = null)
        {
            _Usuario = oUsuario;
            InitializeComponent();
            txtCodigoProducto.KeyDown += TxtCodigoProducto_KeyDown;
            txtDocumentoCliente.KeyDown += TxtCliente_KeyDown;
            txtNombreCliente.KeyDown += TxtCliente_KeyDown;
            txtCantidad.KeyDown += TxtCantidad_KeyDown;
            txtPagaCon.Enter += (s, e) => txtPagaCon.SelectAll();
            Shown += frmVentas_Shown;
            ExperienciaUsuario.Aplicar(this, txtCodigoProducto, txtCodigoProducto, txtDocumentoCliente, txtNombreCliente, txtCantidad);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F2)
            {
                CrearProductoTemporal();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void CrearProductoTemporal()
        {
            using (var dialogo = new DialogoProductoTemporal())
            {
                if (dialogo.ShowDialog(this) != DialogResult.OK) return;

                int idTemporal = _siguienteIdTemporal--;
                dataGridView1.Rows.Add(new object[]
                {
                    idTemporal,
                    null,
                    dialogo.Nombre,
                    dialogo.Precio,
                    dialogo.Cantidad,
                    (dialogo.Precio * dialogo.Cantidad).ToString("0.00", CultureInfo.CurrentCulture)
                });
                calculatTotal();
                txtCodigoProducto.Focus();
            }
        }

        private void TxtCodigoProducto_KeyDown(object sender, KeyEventArgs e)
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

        private void TxtCliente_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                BuscarClientePorDocumento();
            }
            else if (e.KeyCode == Keys.F5)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                btnBuscarProveedor.PerformClick();
            }
        }

        private void BuscarClientePorDocumento()
        {
            string documentoAnterior = txtDocumentoCliente.Text;
            string nombreAnterior = txtNombreCliente.Text;
            string documento = txtDocumentoCliente.Text.Trim();
            if (documento.Length == 0)
            {
                btnBuscarProveedor.PerformClick();
                return;
            }
            if (documento.Length > 50)
            {
                CapaPresentacion.DialogoSistema.Show(this, "El NIT o documento no puede exceder 50 caracteres.",
                    "Documento inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDocumentoCliente.Focus();
                txtDocumentoCliente.SelectAll();
                return;
            }

            List<Cliente> coincidencias;
            try
            {
                coincidencias = new CN_Cliente().BuscarPorDocumento(documento);
            }
            catch (Exception ex)
            {
                CapaPresentacion.DialogoSistema.Show(this, "No se pudo consultar el cliente: " + ex.Message,
                    "Error al buscar cliente", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtDocumentoCliente.Focus();
                txtDocumentoCliente.SelectAll();
                return;
            }

            if (coincidencias.Count > 1)
            {
                CapaPresentacion.DialogoSistema.Show(this,
                    "Hay más de un cliente activo con ese NIT (con o sin guion). Corrige los registros antes de continuar.",
                    "NIT duplicado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDocumentoCliente.Focus();
                txtDocumentoCliente.SelectAll();
                return;
            }

            if (coincidencias.Count == 0)
            {
                DialogResult agregar = CapaPresentacion.DialogoSistema.Show(this,
                    "No se encontró un cliente con el documento '" + documento + "'.\n\n¿Desea agregarlo ahora?",
                    "Cliente no encontrado", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (agregar == DialogResult.Yes)
                {
                    using (var dialogo = new DialogoRegistrarCliente(documento))
                    {
                        if (dialogo.ShowDialog(this) == DialogResult.OK)
                        {
                            txtDocumentoCliente.Text = dialogo.ClienteRegistrado.Documento;
                            txtNombreCliente.Text = dialogo.ClienteRegistrado.NombreCompleto;
                            txtCodigoProducto.Focus();
                            return;
                        }
                    }
                }
                txtDocumentoCliente.Text = documentoAnterior;
                txtNombreCliente.Text = nombreAnterior;
                txtDocumentoCliente.Focus();
                txtDocumentoCliente.SelectAll();
                return;
            }

            Cliente cliente = coincidencias[0];
            txtDocumentoCliente.Text = cliente.Documento;
            txtNombreCliente.Text = cliente.NombreCompleto;
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
                    "No se pudo buscar el producto", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtCodigoProducto.Focus();
                txtCodigoProducto.SelectAll();
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

            _productoActualSeleccionado = producto;
            txtIdProducto.Text = producto.IdProducto.ToString();
            txtCodigoProducto.Text = producto.Codigo;
            txtProducto.Text = producto.Nombre;
            txtStock.Text = producto.Stock.ToString();
            txtPrecioCompra.Text = producto.PrecioVenta.ToString("0.00");
            txtCantidad.Focus();
            txtCantidad.Select(0, txtCantidad.Text.Length);
        }

        private void TxtCantidad_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                btnAgregar.PerformClick();
            }
        }

        private void frmVentas_Load(object sender, EventArgs e)
        {
            cboTipoDocumento.Items.Add(new OpcionCombo() { Valor = "Boleta", Texto = "Boleta" });
            cboTipoDocumento.Items.Add(new OpcionCombo() { Valor = "Factura", Texto = "Factura" });
            cboTipoDocumento.DisplayMember = "Texto";
            cboTipoDocumento.ValueMember = "Valor";
            cboTipoDocumento.SelectedIndex = 0;

            txtFecha.Text = DateTime.Now.ToString("dd/MM/yyyy");
            txtIdProducto.Text = "0";

            txtTotalPagar.Text = "0";
            txtPagaCon.Text = "0";
            txtCambio.Text = "0";

            txtCantidad.Minimum = 1;       
            txtCantidad.Maximum = 100000;  
            txtCantidad.DecimalPlaces = 0; 
            txtCantidad.Increment = 1;     

            // En ventas se propone por defecto al cliente Consumidor Final (CF).
            CargarClienteConsumidorFinal();

            ConfigurarTablaVenta();


        }

        private void frmVentas_Shown(object sender, EventArgs e)
        {
            txtCodigoProducto.Focus();
        }

        private void CargarClienteConsumidorFinal()
        {
            _clienteConsumidorFinal = new CN_Cliente().Listar()
                .FirstOrDefault(cliente => cliente.Estado &&
                    string.Equals(Convert.ToString(cliente.Documento).Trim(), "CF", StringComparison.OrdinalIgnoreCase));

            AplicarClienteConsumidorFinal();
        }

        private void AplicarClienteConsumidorFinal()
        {
            if (_clienteConsumidorFinal != null)
            {
                txtDocumentoCliente.Text = _clienteConsumidorFinal.Documento;
                txtNombreCliente.Text = _clienteConsumidorFinal.NombreCompleto;
            }
        }

        private void ConfigurarTablaVenta()
        {
            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            if (!dataGridView1.Columns.Contains("Foto"))
                dataGridView1.Columns.Insert(1, new DataGridViewImageColumn
                {
                    Name = "Foto", HeaderText = "FOTO", ImageLayout = DataGridViewImageCellLayout.Zoom,
                    Width = 68, MinimumWidth = 60, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable
                });
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.RowTemplate.Height = 48;
            dataGridView1.RowTemplate.Height = 68;
            dataGridView1.ColumnHeadersHeight = 44;
            dataGridView1.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            dataGridView1.DefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Bold);

            IdProducto.Visible = false;
            Producto.FillWeight = 190;
            dataGridView1.Columns["Foto"].FillWeight = 45;
            dataGridView1.Columns["Foto"].MinimumWidth = 64;
            Precio.FillWeight = 75;
            Cantidad.FillWeight = 75;
            SubTotal.FillWeight = 85;
            btnEliminar.FillWeight = 80;
            btnEliminar.MinimumWidth = 82;
            btnEliminar.HeaderText = "Acción";
            btnEliminar.Text = "Quitar";
            btnEliminar.UseColumnTextForButtonValue = true;
            btnEliminar.FlatStyle = FlatStyle.Flat;
            btnEliminar.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(198, 40, 40);
            btnEliminar.DefaultCellStyle.ForeColor = System.Drawing.Color.White;
            btnEliminar.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(150, 25, 25);
            btnEliminar.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.White;
            btnEliminar.ToolTipText = "Quitar este producto de la venta";

            foreach (System.Windows.Forms.Control control in new System.Windows.Forms.Control[] { txtTotalPagar, txtPagaCon, txtCambio, label12, label13, lblCambio, btnRegistrar })
                control.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            txtTotalPagar.TabStop = false;
            txtCambio.TabStop = false;
            txtTotalPagar.ReadOnly = true;
            txtCambio.ReadOnly = true;
            groupBox3.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnAgregar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            dataGridView1.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || dataGridView1.Columns[e.ColumnIndex].Name != "Foto") return;
                Producto p = dataGridView1.Rows[e.RowIndex].Tag as Producto;
                if (p != null && !string.IsNullOrWhiteSpace(p.ImagenRuta))
                    GestorImagenProducto.MostrarVistaPrevia(this, p.ImagenRuta, p.Nombre);
            };
        }

        private void btnBuscarProveedor_Click(object sender, EventArgs e)
        {
            using (var modal = new mdCliente())
            {
                var result = modal.ShowDialog();

                if (result == DialogResult.OK)
                {
                    txtDocumentoCliente.Text = modal._Cliente.Documento.ToString();
                    txtNombreCliente.Text = modal._Cliente.NombreCompleto.ToString();
                }
                else
                {
                    txtDocumentoCliente.Select();
                }
            }
        }

        private void btnBuscarProducto_Click(object sender, EventArgs e)
        {
            using (var modal = new mdProducto(false))
            {
                var result = modal.ShowDialog();

                if (result == DialogResult.OK)
                {
                    _productoActualSeleccionado = modal._Producto;
                    txtIdProducto.Text = modal._Producto.IdProducto.ToString();
                    txtCodigoProducto.Text = modal._Producto.Codigo;
                    txtProducto.Text = modal._Producto.Nombre;
                    txtStock.Text = modal._Producto.Stock.ToString();
                    txtPrecioCompra.Text = modal._Producto.PrecioVenta.ToString("0.00");

                    txtCantidad.Select();
                    txtCantidad.Select(0, txtCantidad.Text.Length);
                }
                else
                {
                    txtCodigoProducto.Select();
                }
            }
        }

        private void btnAgregar_Click(object sender, EventArgs e)
        {
            decimal precioCompra = 0;
            int indiceProductoExistente = -1;
            int idProductoActual;

            if (!int.TryParse(txtIdProducto.Text, out idProductoActual) || idProductoActual == 0)
            {
                CapaPresentacion.DialogoSistema.Show("Debe de seleccionar un producto", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtPrecioCompra.Text, out precioCompra))
            {
                CapaPresentacion.DialogoSistema.Show("Precio Compra - Formato incorrecto", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            for (int indice = 0; indice < dataGridView1.Rows.Count; indice++)
            {
                if (Convert.ToString(dataGridView1.Rows[indice].Cells["IdProducto"].Value) == txtIdProducto.Text)
                {
                    indiceProductoExistente = indice;
                    break;
                }
            }

            if (indiceProductoExistente >= 0 && _productoEnEdicionId == idProductoActual)
            {
                DataGridViewRow fila = dataGridView1.Rows[indiceProductoExistente];
                fila.Cells["Producto"].Value = txtProducto.Text;
                fila.Cells["Precio"].Value = precioCompra;
                fila.Cells["Cantidad"].Value = txtCantidad.Value.ToString();
                fila.Cells["SubTotal"].Value = (txtCantidad.Value * precioCompra).ToString("0.00");
                fila.Cells["Foto"].Value = GestorImagenProducto.CargarMiniatura(_productoActualSeleccionado?.ImagenRuta, 52, 52);
                fila.Tag = _productoActualSeleccionado;
                calculatTotal();
                limpiarProducto();
                _productoEnEdicionId = 0;
                txtCodigoProducto.Focus();
            }
            else if (indiceProductoExistente < 0 && idProductoActual > 0)
            {
                   int indiceFila = dataGridView1.Rows.Add(new object[]
                   {
                       txtIdProducto.Text.ToString(),
                        null,
                        txtProducto.Text,
                        precioCompra,
                        txtCantidad.Value.ToString(),
                        (txtCantidad.Value*precioCompra).ToString("0.00")
                   });
                    dataGridView1.Rows[indiceFila].Cells["Foto"].Value = GestorImagenProducto.CargarMiniatura(_productoActualSeleccionado?.ImagenRuta, 52, 52);
                    dataGridView1.Rows[indiceFila].Tag = _productoActualSeleccionado;

                    calculatTotal();
                    limpiarProducto();
                    _productoEnEdicionId = 0;
                    txtCodigoProducto.Focus();
            }
            else
            {
                CapaPresentacion.DialogoSistema.Show("El producto ya está agregado a la venta", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void limpiarProducto()
        {
            txtIdProducto.Text = "0";
            txtCodigoProducto.Text = "";
            txtProducto.Text = "";
            txtPrecioCompra.Text = "";
            txtStock.Text = "";
            txtCantidad.Value = 1;
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

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dataGridView1.Rows.Count ||
                (e.ColumnIndex >= 0 && (dataGridView1.Columns[e.ColumnIndex].Name == "btnEliminar" || dataGridView1.Columns[e.ColumnIndex].Name == "Foto")))
                return;

            DataGridViewRow filaVenta = dataGridView1.Rows[e.RowIndex];
            int idProducto = Convert.ToInt32(filaVenta.Cells["IdProducto"].Value);
            Producto producto = null;
            if (idProducto > 0)
            {
                try
                {
                    producto = new CN_Producto().BuscarPorId(idProducto);
                }
                catch (Exception ex)
                {
                    CapaPresentacion.DialogoSistema.Show(ex.Message + Environment.NewLine + ex.InnerException?.Message,
                        "No se pudo cargar el producto", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (producto == null)
                {
                    CapaPresentacion.DialogoSistema.Show("No se encontró el producto en la base de datos.", "Producto no disponible",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            txtIdProducto.Text = idProducto.ToString();
            _productoActualSeleccionado = producto;
            txtCodigoProducto.Text = producto == null ? "TEMPORAL" : producto.Codigo;
            txtProducto.Text = producto == null
                ? Convert.ToString(filaVenta.Cells["Producto"].Value)
                : producto.Nombre;
            txtStock.Text = producto == null ? "0" : producto.Stock.ToString();
            txtPrecioCompra.Text = Convert.ToDecimal(filaVenta.Cells["Precio"].Value).ToString("0.00");
            txtCantidad.Value = Convert.ToDecimal(filaVenta.Cells["Cantidad"].Value);
            _productoEnEdicionId = idProducto;
            txtCantidad.Focus();
            txtCantidad.Select(0, txtCantidad.Text.Length);
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dataGridView1.Columns[e.ColumnIndex].Name == "btnEliminar")
            {
                string nombreProducto = Convert.ToString(dataGridView1.Rows[e.RowIndex].Cells["Producto"].Value);
                DialogResult respuesta = CapaPresentacion.DialogoSistema.Show(
                    "¿Desea quitar '" + nombreProducto + "' de la venta?",
                    "Confirmar acción", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (respuesta == DialogResult.Yes)
                {
                    dataGridView1.Rows.RemoveAt(e.RowIndex);
                    calculatTotal();
                }
            }
        }

        private void txtPagaCon_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (Char.IsDigit(e.KeyChar))
            {
                e.Handled = false;
            }
            else
            {
                if (txtPagaCon.Text.Trim().Length == 0 && e.KeyChar.ToString() == ".")
                {
                    e.Handled = true;
                }
                else
                {
                    if(char.IsControl(e.KeyChar) || e.KeyChar.ToString() == ".")
                    {
                        e.Handled = false;
                    }
                    else
                    {
                        e.Handled = true;
                    }
                }
            }

            
        }

        private void txtTotalPagar_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtTotalPagar_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (Char.IsDigit(e.KeyChar))
            {
                e.Handled = false;
            }
            else
            {
                if (txtTotalPagar.Text.Trim().Length == 0 && e.KeyChar.ToString() == ".")
                {
                    e.Handled = true;
                }
                else
                {
                    if (char.IsControl(e.KeyChar) || e.KeyChar.ToString() == ".")
                    {
                        e.Handled = false;
                    }
                    else
                    {
                        e.Handled = true;
                    }
                }
            }
        }
        private bool calcularCambio()
        {
            if (txtTotalPagar.Text.Trim() == "")
            {
                CapaPresentacion.DialogoSistema.Show("No existe producto en la venta", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            decimal pagacon;
            decimal total = Convert.ToDecimal(txtTotalPagar.Text);

            if (txtPagaCon.Text.Trim() == "")
            {
                txtPagaCon.Text = "0";
            }

            if (!decimal.TryParse(txtPagaCon.Text.Trim(), out pagacon))
            {
                CapaPresentacion.DialogoSistema.Show("El monto recibido tiene un formato incorrecto", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (pagacon < total)
            {
                CapaPresentacion.DialogoSistema.Show("Falta Dinero", "DINERITO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            decimal cambio = pagacon - total;
            txtCambio.Text = cambio.ToString("0.00");
            return true;
        }

        private void txtPagaCon_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                calcularCambio();
                btnRegistrar.Focus();
            }
        }

        private void btnRegistrar_Click(object sender, EventArgs e)
        {
            if (txtDocumentoCliente.Text == "")
            {
                CapaPresentacion.DialogoSistema.Show("Debe ingresar documento del cliente", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (txtNombreCliente.Text == "")
            {
                CapaPresentacion.DialogoSistema.Show("Debe ingresar el nombre del cliente", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }


            if (dataGridView1.Rows.Count <1)
            {
                CapaPresentacion.DialogoSistema.Show("Debe ingresar producto en la venta", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            DataTable detalle_venta = new DataTable();

            detalle_venta.Columns.Add("IdProducto", typeof(int));
            detalle_venta.Columns.Add("PrecioVenta", typeof(decimal));
            detalle_venta.Columns.Add("Cantidad", typeof(int));
            detalle_venta.Columns.Add("SubTotal", typeof(decimal));


            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                detalle_venta.Rows.Add(new object[]
                {
                    row.Cells["IdProducto"].Value.ToString(),
                    row.Cells["Precio"].Value.ToString(),
                    row.Cells["Cantidad"].Value.ToString(),
                    row.Cells["SubTotal"].Value.ToString()
                });
            }

            if (calcularCambio())
            {
                DialogResult confirmar = CapaPresentacion.DialogoSistema.Show(
                    this,
                    "¿Está seguro de registrar esta venta?",
                    "Confirmar venta",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);

                if (confirmar != DialogResult.Yes)
                {
                    txtCodigoProducto.Focus();
                    return;
                }

                Usuario usuarioVendedor;
                using (var dialogo = new DialogoConfirmarVendedor())
                {
                    if (dialogo.ShowDialog(this) != DialogResult.OK)
                    {
                        txtCodigoProducto.Focus();
                        return;
                    }

                    List<Usuario> coincidencias;
                    try
                    {
                        coincidencias = new CN_Usuario().ListarActivosPorClave(dialogo.Clave);
                    }
                    catch (Exception ex)
                    {
                        CapaPresentacion.DialogoSistema.Show(this, "No fue posible validar al vendedor: " + ex.Message,
                            "Error de validación", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        txtCodigoProducto.Focus();
                        return;
                    }
                    if (coincidencias.Count != 1)
                    {
                        string aviso = coincidencias.Count == 0
                            ? "La contraseña no corresponde a un usuario activo. No se registró la venta."
                            : "Esa contraseña pertenece a más de un usuario activo. Asigna contraseñas diferentes para identificar correctamente al vendedor.";
                        CapaPresentacion.DialogoSistema.Show(this, aviso, "No se pudo validar al vendedor",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtCodigoProducto.Focus();
                        return;
                    }
                    usuarioVendedor = coincidencias[0];
                }

                string mensaje = string.Empty;
                List<KeyValuePair<int, int>> productosTemporales;
                if (!PrepararProductosTemporales(detalle_venta, out productosTemporales, out mensaje))
                {
                    CapaPresentacion.DialogoSistema.Show(mensaje, "No se pudo preparar la venta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Venta oVenta = new Venta()
                {
                    oUsuario = new Usuario() { IdUsuario = usuarioVendedor.IdUsuario },
                    TipoDocumento = ((OpcionCombo)cboTipoDocumento.SelectedItem).Texto,
                    NumeroDocumento = string.Empty,
                    DocumentoCliente = txtDocumentoCliente.Text,
                    NombreCliente = txtNombreCliente.Text,
                    MontoPago = Convert.ToDecimal(txtPagaCon.Text),
                    MontoCambio = Convert.ToDecimal(txtCambio.Text),
                    MontoTotal = Convert.ToDecimal(txtTotalPagar.Text)
                };

                bool respuesta = new CN_Venta().Registrar(oVenta, detalle_venta, out mensaje);

                if (respuesta)
                {
                    var result = CapaPresentacion.DialogoSistema.Show("Numero de venta generado: \n" + oVenta.NumeroDocumento + "\n\n¿Desea copiar al portapapeles?", "Mensaje",
                        MessageBoxButtons.YesNo, MessageBoxIcon.None);

                    if (result == DialogResult.Yes)
                    {
                        Clipboard.SetText(oVenta.NumeroDocumento);
                    }

                    AplicarClienteConsumidorFinal();
                    dataGridView1.Rows.Clear();
                    calculatTotal();
                    txtPagaCon.Text = "";
                    txtCambio.Text = "";
                }
                else
                {
                    RevertirProductosTemporales(detalle_venta, productosTemporales);
                    CapaPresentacion.DialogoSistema.Show(mensaje, "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
           
                
            

            

            
        }

        private bool PrepararProductosTemporales(DataTable detalleVenta,
            out List<KeyValuePair<int, int>> productosTemporales, out string mensaje)
        {
            productosTemporales = new List<KeyValuePair<int, int>>();
            mensaje = string.Empty;
            CN_Producto negocioProducto = new CN_Producto();

            for (int indice = 0; indice < dataGridView1.Rows.Count; indice++)
            {
                DataGridViewRow fila = dataGridView1.Rows[indice];
                int idTemporal = Convert.ToInt32(fila.Cells["IdProducto"].Value);
                if (idTemporal >= 0) continue;

                int cantidad = Convert.ToInt32(fila.Cells["Cantidad"].Value);
                string nombre = Convert.ToString(fila.Cells["Producto"].Value).Trim();
                string codigo = "T" + Guid.NewGuid().ToString("N").Substring(0, 19);
                int idProducto = negocioProducto.RegistrarTemporal(new Producto()
                {
                    Codigo = codigo,
                    Nombre = nombre,
                    Descripcion = "Articulo temporal de venta"
                }, cantidad, out mensaje);

                if (idProducto <= 0)
                {
                    foreach (KeyValuePair<int, int> creado in productosTemporales)
                        negocioProducto.EliminarTemporalNoVendido(creado.Value);
                    productosTemporales.Clear();
                    return false;
                }

                productosTemporales.Add(new KeyValuePair<int, int>(idTemporal, idProducto));
                detalleVenta.Rows[indice]["IdProducto"] = idProducto;
            }

            return true;
        }

        private void RevertirProductosTemporales(DataTable detalleVenta,
            List<KeyValuePair<int, int>> productosTemporales)
        {
            CN_Producto negocioProducto = new CN_Producto();
            foreach (KeyValuePair<int, int> producto in productosTemporales)
            {
                negocioProducto.EliminarTemporalNoVendido(producto.Value);
                for (int indice = 0; indice < detalleVenta.Rows.Count; indice++)
                {
                    if (Convert.ToInt32(dataGridView1.Rows[indice].Cells["IdProducto"].Value) == producto.Key)
                    {
                        detalleVenta.Rows[indice]["IdProducto"] = producto.Key;
                        break;
                    }
                }
            }
        }

        private void txtCambio_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                
                btnRegistrar.Select();
            }

        }

        private void txtCantidad_ValueChanged(object sender, EventArgs e)
        {
            if (txtCantidad.Value < 1)
            {
                txtCantidad.Value = 1;
            }
        }

        private sealed class DialogoRegistrarCliente : Form
        {
            private readonly TextBox txtDocumento = new TextBox();
            private readonly TextBox txtNombre = new TextBox();
            private readonly TextBox txtCorreo = new TextBox();
            private readonly TextBox txtTelefono = new TextBox();

            public Cliente ClienteRegistrado { get; private set; }

            public DialogoRegistrarCliente(string documento)
            {
                Text = "Registrar cliente";
                Font = new System.Drawing.Font("Segoe UI", 9F);
                BackColor = System.Drawing.Color.White;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                MinimizeBox = false;
                MaximizeBox = false;
                ShowInTaskbar = false;
                ClientSize = new Size(430, 355);

                Controls.Add(new Label { Text = "Nuevo cliente", Font = new System.Drawing.Font("Segoe UI", 15F, FontStyle.Bold), Left = 20, Top = 14, Width = 380, Height = 30 });
                Controls.Add(new Label { Text = "NIT / documento:", Left = 20, Top = 56, Width = 380 });
                ConfigurarCampo(txtDocumento, 20, 78, 390, 50, documento);
                Controls.Add(txtDocumento);
                Controls.Add(new Label { Text = "Nombre o razón social:", Left = 20, Top = 112, Width = 380 });
                ConfigurarCampo(txtNombre, 20, 134, 390, 50);
                Controls.Add(txtNombre);
                Controls.Add(new Label { Text = "Correo (opcional):", Left = 20, Top = 168, Width = 380 });
                ConfigurarCampo(txtCorreo, 20, 190, 390, 50);
                Controls.Add(txtCorreo);
                Controls.Add(new Label { Text = "Teléfono:", Left = 20, Top = 224, Width = 380 });
                ConfigurarCampo(txtTelefono, 20, 246, 390, 50);
                Controls.Add(txtTelefono);

                var cancelar = new System.Windows.Forms.Button { Text = "Cancelar", DialogResult = DialogResult.Cancel };
                cancelar.SetBounds(218, 304, 90, 32);
                Controls.Add(cancelar);
                var guardar = new System.Windows.Forms.Button { Text = "Guardar cliente", BackColor = System.Drawing.Color.FromArgb(43, 105, 157), ForeColor = System.Drawing.Color.White, FlatStyle = FlatStyle.Flat };
                guardar.SetBounds(314, 304, 96, 32);
                guardar.Click += GuardarCliente_Click;
                Controls.Add(guardar);

                AcceptButton = guardar;
                CancelButton = cancelar;
                Shown += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtDocumento.Text)) txtDocumento.Focus();
                    else txtNombre.Focus();
                };
            }

            private static void ConfigurarCampo(TextBox campo, int left, int top, int width, int maxLength, string valorInicial = "")
            {
                campo.SetBounds(left, top, width, 25);
                campo.MaxLength = maxLength;
                campo.CharacterCasing = CharacterCasing.Upper;
                campo.Text = valorInicial;
            }

            private void GuardarCliente_Click(object sender, EventArgs e)
            {
                Cliente nuevo = new Cliente
                {
                    Documento = txtDocumento.Text.Trim(),
                    NombreCompleto = txtNombre.Text.Trim(),
                    Correo = txtCorreo.Text.Trim(),
                    Telefono = txtTelefono.Text.Trim(),
                    Estado = true
                };
                string mensaje;
                int id = new CN_Cliente().Registrar(nuevo, out mensaje);
                if (id <= 0)
                {
                    CapaPresentacion.DialogoSistema.Show(this,
                        string.IsNullOrWhiteSpace(mensaje) ? "No se pudo registrar el cliente." : mensaje,
                        "No se pudo guardar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                nuevo.IdCliente = id;
                ClienteRegistrado = nuevo;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private sealed class DialogoConfirmarVendedor : Form
        {
            private readonly TextBox txtClave = new TextBox();

            public string Clave { get { return txtClave.Text; } }

            public DialogoConfirmarVendedor()
            {
                Text = "Confirmar vendedor";
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                MinimizeBox = false;
                MaximizeBox = false;
                ShowInTaskbar = false;
                ClientSize = new Size(360, 145);

                Controls.Add(new Label
                {
                    Text = "Ingresa tu contraseña para registrar la venta a tu nombre:",
                    Left = 18,
                    Top = 18,
                    Width = 324,
                    Height = 38
                });

                txtClave.SetBounds(18, 62, 324, 26);
                txtClave.MaxLength = 50;
                txtClave.UseSystemPasswordChar = true;
                Controls.Add(txtClave);

                var cancelar = new System.Windows.Forms.Button { Text = "Cancelar", DialogResult = DialogResult.Cancel };
                cancelar.SetBounds(164, 101, 85, 30);
                Controls.Add(cancelar);

                var aceptar = new System.Windows.Forms.Button { Text = "Confirmar", DialogResult = DialogResult.OK };
                aceptar.SetBounds(257, 101, 85, 30);
                Controls.Add(aceptar);

                AcceptButton = aceptar;
                CancelButton = cancelar;
                Shown += (s, e) => txtClave.Focus();
            }
        }

        private sealed class DialogoProductoTemporal : Form
        {
            private readonly TextBox txtNombre = new TextBox();
            private readonly TextBox txtPrecio = new TextBox();
            private readonly NumericUpDown numCantidad = new NumericUpDown();

            public string Nombre { get; private set; }
            public decimal Precio { get; private set; }
            public int Cantidad { get; private set; }

            public DialogoProductoTemporal()
            {
                Text = "Artículo temporal (F2)";
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                MinimizeBox = false;
                MaximizeBox = false;
                ShowInTaskbar = false;
                ClientSize = new Size(390, 205);

                Controls.Add(new Label { Text = "Nombre o descripción", Left = 18, Top = 18, Width = 350 });
                txtNombre.SetBounds(18, 42, 350, 25);
                txtNombre.MaxLength = 30;
                Controls.Add(txtNombre);

                Controls.Add(new Label { Text = "Precio unitario", Left = 18, Top = 78, Width = 170 });
                txtPrecio.SetBounds(18, 102, 170, 25);
                txtPrecio.Text = "0.00";
                Controls.Add(txtPrecio);

                Controls.Add(new Label { Text = "Cantidad", Left = 205, Top = 78, Width = 160 });
                numCantidad.SetBounds(205, 102, 160, 25);
                numCantidad.Minimum = 1;
                numCantidad.Maximum = 100000;
                numCantidad.Value = 1;
                Controls.Add(numCantidad);

                System.Windows.Forms.Button cancelar = new System.Windows.Forms.Button { Text = "Cancelar", DialogResult = DialogResult.Cancel };
                cancelar.SetBounds(185, 155, 88, 30);
                Controls.Add(cancelar);

                System.Windows.Forms.Button agregar = new System.Windows.Forms.Button { Text = "Agregar", UseVisualStyleBackColor = true };
                agregar.SetBounds(280, 155, 88, 30);
                agregar.Click += Agregar_Click;
                Controls.Add(agregar);

                AcceptButton = agregar;
                CancelButton = cancelar;
            }

            private void Agregar_Click(object sender, EventArgs e)
            {
                decimal precio;
                if (string.IsNullOrWhiteSpace(txtNombre.Text))
                {
                    CapaPresentacion.DialogoSistema.Show(this, "Escribe el nombre o descripción del artículo.", "Dato requerido",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtNombre.Focus();
                    return;
                }

                if (!decimal.TryParse(txtPrecio.Text.Trim(), NumberStyles.Number,
                    CultureInfo.CurrentCulture, out precio) || precio < 0)
                {
                    CapaPresentacion.DialogoSistema.Show(this, "Escribe un precio válido igual o mayor que cero.", "Precio inválido",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPrecio.Focus();
                    txtPrecio.SelectAll();
                    return;
                }

                Nombre = txtNombre.Text.Trim();
                Precio = precio;
                Cantidad = Convert.ToInt32(numCantidad.Value);
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }

    
}
