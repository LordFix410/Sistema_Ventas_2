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
        private int _siguienteIdTemporal = -1;
        public frmVentas(Usuario oUsuario = null)
        {
            _Usuario = oUsuario;
            InitializeComponent();
            txtCodigoProducto.KeyDown += TxtCodigoProducto_KeyDown;
            txtDocumentoCliente.KeyDown += TxtCliente_KeyDown;
            txtNombreCliente.KeyDown += TxtCliente_KeyDown;
            txtCantidad.KeyDown += TxtCantidad_KeyDown;
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
            string documento = txtDocumentoCliente.Text.Trim();
            if (documento.Length == 0)
            {
                btnBuscarProveedor.PerformClick();
                return;
            }

            Cliente cliente = new CN_Cliente().Listar()
                .FirstOrDefault(item => item.Estado &&
                    string.Equals(Convert.ToString(item.Documento).Trim(), documento, StringComparison.OrdinalIgnoreCase));
            if (cliente == null)
            {
                MessageBox.Show("No se encontró un cliente con el documento '" + documento + "'.",
                    "Cliente no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtDocumentoCliente.Focus();
                txtDocumentoCliente.SelectAll();
                return;
            }

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
            Producto producto = new CN_Producto().Listar()
                .FirstOrDefault(item => item.Estado &&
                    string.Equals(Convert.ToString(item.Codigo).Trim(), codigo, StringComparison.OrdinalIgnoreCase));

            if (producto == null)
            {
                MessageBox.Show("No se encontró un producto con el código '" + codigo + "'.",
                    "Producto no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtCodigoProducto.Focus();
                txtCodigoProducto.SelectAll();
                return;
            }

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
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.RowTemplate.Height = 32;
            dataGridView1.ColumnHeadersHeight = 34;
            dataGridView1.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            dataGridView1.DefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Bold);

            IdProducto.Visible = false;
            Producto.FillWeight = 190;
            Precio.FillWeight = 75;
            Cantidad.FillWeight = 75;
            SubTotal.FillWeight = 85;
            btnEliminar.FillWeight = 80;
            btnEliminar.MinimumWidth = 82;
            btnEliminar.HeaderText = "Acción";
            btnEliminar.Text = "Quitar";
            btnEliminar.UseColumnTextForButtonValue = true;
            btnEliminar.FlatStyle = FlatStyle.Flat;
            btnEliminar.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(255, 235, 235);
            btnEliminar.DefaultCellStyle.ForeColor = System.Drawing.Color.Firebrick;
            btnEliminar.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(255, 220, 220);
            btnEliminar.ToolTipText = "Quitar este producto de la venta";
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
            using (var modal = new mdProducto())
            {
                var result = modal.ShowDialog();

                if (result == DialogResult.OK)
                {
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
            int stock = 0;
            int indiceProductoExistente = -1;
            int idProductoActual;

            if (!int.TryParse(txtIdProducto.Text, out idProductoActual) || idProductoActual == 0)
            {
                MessageBox.Show("Debe de seleccionar un producto", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!decimal.TryParse(txtPrecioCompra.Text, out precioCompra))
            {
                MessageBox.Show("Precio Compra - Formato incorrecto", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (idProductoActual > 0 && !int.TryParse(txtStock.Text, out stock))
            {
                MessageBox.Show("Stock - Formato incorrecto", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (idProductoActual > 0 && txtCantidad.Value > stock)
            {
                MessageBox.Show("La cantidad solicitada supera el stock disponible", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                calculatTotal();
                limpiarProducto();
                _productoEnEdicionId = 0;
                txtCodigoProducto.Focus();
            }
            else if (indiceProductoExistente < 0 && idProductoActual > 0)
            {
                   dataGridView1.Rows.Add(new object[]
                   {
                       txtIdProducto.Text.ToString(),
                        txtProducto.Text,
                        precioCompra,
                        txtCantidad.Value.ToString(),
                        (txtCantidad.Value*precioCompra).ToString("0.00")
                   });

                    calculatTotal();
                    limpiarProducto();
                    _productoEnEdicionId = 0;
                    txtCodigoProducto.Focus();
            }
            else
            {
                MessageBox.Show("El producto ya está agregado a la venta", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                (e.ColumnIndex >= 0 && dataGridView1.Columns[e.ColumnIndex].Name == "btnEliminar"))
                return;

            DataGridViewRow filaVenta = dataGridView1.Rows[e.RowIndex];
            int idProducto = Convert.ToInt32(filaVenta.Cells["IdProducto"].Value);
            Producto producto = null;
            if (idProducto > 0)
            {
                producto = new CN_Producto().Listar()
                    .FirstOrDefault(item => item.IdProducto == idProducto);

                if (producto == null)
                {
                    MessageBox.Show("No se encontró el producto en la base de datos.", "Producto no disponible",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            txtIdProducto.Text = idProducto.ToString();
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
                DialogResult respuesta = MessageBox.Show(
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
                MessageBox.Show("No existe producto en la venta", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show("El monto recibido tiene un formato incorrecto", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            if (pagacon < total)
            {
                MessageBox.Show("Falta Dinero", "DINERITO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            decimal cambio = pagacon - total;
            txtCambio.Text = cambio.ToString("0.00");
            return true;
        }

        private void txtPagaCon_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyData == Keys.Enter)
            {
                calcularCambio();
                txtCambio.Select();
            }
        }

        private void btnRegistrar_Click(object sender, EventArgs e)
        {
            if (txtDocumentoCliente.Text == "")
            {
                MessageBox.Show("Debe ingresar documento del cliente", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (txtNombreCliente.Text == "")
            {
                MessageBox.Show("Debe ingresar el nombre del cliente", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }


            if (dataGridView1.Rows.Count <1)
            {
                MessageBox.Show("Debe ingresar producto en la venta", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
                DialogResult confirmar = MessageBox.Show(
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
                        MessageBox.Show(this, "No fue posible validar al vendedor: " + ex.Message,
                            "Error de validación", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        txtCodigoProducto.Focus();
                        return;
                    }
                    if (coincidencias.Count != 1)
                    {
                        string aviso = coincidencias.Count == 0
                            ? "La contraseña no corresponde a un usuario activo. No se registró la venta."
                            : "Esa contraseña pertenece a más de un usuario activo. Asigna contraseñas diferentes para identificar correctamente al vendedor.";
                        MessageBox.Show(this, aviso, "No se pudo validar al vendedor",
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
                    MessageBox.Show(mensaje, "No se pudo preparar la venta", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    var result = MessageBox.Show("Numero de venta generado: \n" + oVenta.NumeroDocumento + "\n\n¿Desea copiar al portapapeles?", "Mensaje",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information);

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
                    MessageBox.Show(mensaje, "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    MessageBox.Show(this, "Escribe el nombre o descripción del artículo.", "Dato requerido",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtNombre.Focus();
                    return;
                }

                if (!decimal.TryParse(txtPrecio.Text.Trim(), NumberStyles.Number,
                    CultureInfo.CurrentCulture, out precio) || precio < 0)
                {
                    MessageBox.Show(this, "Escribe un precio válido igual o mayor que cero.", "Precio inválido",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
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
