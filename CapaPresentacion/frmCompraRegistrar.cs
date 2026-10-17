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
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace CapaPresentacion
{
    public partial class frmCompraRegistrar : Form
    {
        private Usuario _Usuario;
        private bool sincronizandoPrecios;
        private bool evaluandoCambioCosto;
        private decimal costoOriginalProducto;
        private decimal precioVentaOriginalProducto;
        private decimal margenOriginalProducto;
        private int indiceProductoEditando = -1;
        public frmCompraRegistrar(Usuario oUsuario = null)
        {
            _Usuario = oUsuario;
            InitializeComponent();
            txtCodigoProducto.KeyDown += TxtCodigoProducto_KeyDown;
            txtNuevoDocumento.KeyDown += TxtProveedor_KeyDown;
            txtRazonSocial.KeyDown += TxtProveedor_KeyDown;
            txtPrecioCompra.TextChanged += PrecioCompra_TextChanged;
            txtPrecioVenta.TextChanged += PrecioVenta_TextChanged;
            txtPorcentaje.TextChanged += Porcentaje_TextChanged;
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
            ExperienciaUsuario.Aplicar(this, txtCodigoProducto, txtCodigoProducto, txtNuevoDocumento, txtRazonSocial);
        }

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
            txtPrecioVenta.Focus();
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
                producto = new CN_Producto().Listar()
                    .FirstOrDefault(item => item.Estado &&
                        string.Equals(Convert.ToString(item.Codigo).Trim(), codigo, StringComparison.OrdinalIgnoreCase));
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
                }
                else
                {
                    txtNuevoDocumento.Select();
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

        private void PrecioCompra_TextChanged(object sender, EventArgs e)
        {
            ActualizarMargenDesdePrecios();
        }

        private void PrecioVenta_TextChanged(object sender, EventArgs e)
        {
            ActualizarMargenDesdePrecios();
        }

        private void Porcentaje_TextChanged(object sender, EventArgs e)
        {
            if (sincronizandoPrecios || !decimal.TryParse(txtPrecioCompra.Text, out decimal costo) || costo < 0 ||
                !decimal.TryParse(txtPorcentaje.Text, out decimal porcentaje) || porcentaje < 0)
                return;

            decimal objetivo = costo * (1m + porcentaje / 100m);
            decimal ventaRedondeada = Math.Ceiling(objetivo * 4m) / 4m;
            sincronizandoPrecios = true;
            txtPrecioVenta.Text = ventaRedondeada.ToString("0.00");
            sincronizandoPrecios = false;
            ActualizarMargenDesdePrecios();
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
            var botones = MessageBoxButtons.YesNo;
            DialogResult actualizarPrecio = CapaPresentacion.DialogoSistema.Show(this,
                "El costo registrado era " + costoOriginalProducto.ToString("Q 0.00") +
                " y el nuevo costo es " + costoNuevo.ToString("Q 0.00") + ".\n\n" +
                "¿Desea ajustar también el precio de venta conservando el margen anterior?\n" +
                "Si elige No, se conservará el precio de venta actual y se recalculará el margen.",
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
                dialogo.Text = "Confirmar total de factura";
                dialogo.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialogo.StartPosition = FormStartPosition.CenterParent;
                dialogo.ClientSize = new Size(520, 180);
                dialogo.MinimizeBox = false;
                dialogo.MaximizeBox = false;
                etiqueta.Text = "Total de productos cargados: Q " + totalProductos.ToString("N2") +
                    "\nEscriba el total que aparece en la factura:";
                etiqueta.AutoSize = false;
                etiqueta.SetBounds(18, 14, 480, 52);
                entrada.SetBounds(20, 76, 210, 28);
                entrada.Text = string.Empty;
                entrada.TextAlign = HorizontalAlignment.Right;
                aceptar.Text = "Continuar";
                aceptar.DialogResult = DialogResult.OK;
                aceptar.SetBounds(308, 132, 95, 32);
                cancelar.Text = "Cancelar";
                cancelar.DialogResult = DialogResult.Cancel;
                cancelar.SetBounds(410, 132, 95, 32);
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
                "La factura supera en Q " + diferencia.ToString("N2") + " los productos cargados.\n\n" +
                "¿Ya agregó todos los productos que deben entrar al inventario?\n" +
                "Si falta alguno, elija No para volver a la compra y agregarlo.",
                "Revisar productos", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (productosCompletos != DialogResult.Yes) return false;

            using (var dialogo = new Form())
            using (var etiqueta = new Label())
            using (var entrada = new TextBox())
            using (var aceptar = new Button())
            using (var cancelar = new Button())
            {
                dialogo.Text = "Importe fuera del inventario";
                dialogo.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialogo.StartPosition = FormStartPosition.CenterParent;
                dialogo.ClientSize = new Size(600, 220);
                dialogo.MinimizeBox = false;
                dialogo.MaximizeBox = false;
                etiqueta.AutoSize = false;
                etiqueta.TextAlign = ContentAlignment.MiddleLeft;
                etiqueta.Text = "La factura supera en Q " + diferencia.ToString("N2") + " el total de productos cargados.\n" +
                    "Escriba el importe exacto de los artículos o cargos que no se controlan en inventario.\n" +
                    "No incluya aquí productos que sí deban registrarse en el inventario.";
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
                CapaPresentacion.DialogoSistema.Show("La factura aún difiere en Q " + diferenciaFinal.ToString("N2") +
                    ". Corrija el importe no inventariable o vuelva a revisar los productos.",
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
                CapaPresentacion.DialogoSistema.Show("Debe de seleccionar un producto", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            if(!decimal.TryParse(txtPrecioCompra.Text, out precioCompra))
            {
                CapaPresentacion.DialogoSistema.Show("Precio Compra - Formato incorrecto", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if(!decimal.TryParse(txtPrecioVenta.Text, out precioVenta))
            {
                CapaPresentacion.DialogoSistema.Show("Precio Venta - Formato incorrecto", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!decimal.TryParse(txtPorcentaje.Text, out porcentajeSolicitado) || porcentajeSolicitado < 0)
            {
                CapaPresentacion.DialogoSistema.Show("Margen - Ingrese un porcentaje válido igual o mayor a cero", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtPorcentaje.Focus();
                txtPorcentaje.SelectAll();
                return;
            }

            if (precioCompra < 0 || precioVenta < 0)
            {
                CapaPresentacion.DialogoSistema.Show("Los precios no pueden ser negativos", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (Math.Abs(precioVenta * 4m - decimal.Round(precioVenta * 4m, 0)) > 0.0001m)
            {
                CapaPresentacion.DialogoSistema.Show("El precio de venta debe terminar en .00, .25, .50 o .75.", "Precio de venta inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrecioVenta.Focus();
                txtPrecioVenta.SelectAll();
                return;
            }

            decimal porcentajeReal = precioCompra == 0
                ? 0
                : decimal.Ceiling(((precioVenta - precioCompra) / precioCompra * 100m) * 10000m) / 10000m;

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
                    txtProducto.Text,
                    precioCompra.ToString("0.00"),
                    precioVenta.ToString("0.00"),
                    porcentajeReal.ToString("0.####"),
                    txtCantidad.Value.ToString(),
                    (txtCantidad.Value * precioCompra).ToString("0.00")
                };

                if (indiceProductoEditando >= 0 && indiceProductoEditando < dataGridView1.Rows.Count)
                    for (int columna = 0; columna < valores.Length; columna++)
                        dataGridView1.Rows[indiceProductoEditando].Cells[columna].Value = valores[columna];
                else
                    dataGridView1.Rows.Add(valores);

                calculatTotal();
                limpiarProducto();
                txtCodigoProducto.Select();
            }
            else
            {
                CapaPresentacion.DialogoSistema.Show("El producto ya está agregado en la fila " + (indiceDuplicado + 1) +
                    ". Para modificarlo, haga doble clic en esa fila.", "Producto repetido", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dataGridView1.Rows.Count) return;
            if (indiceProductoEditando >= 0 && indiceProductoEditando != e.RowIndex)
            {
                CapaPresentacion.DialogoSistema.Show("Termine de editar el producto seleccionado antes de abrir otro.",
                    "Edición en curso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow fila = dataGridView1.Rows[e.RowIndex];
            int id = Convert.ToInt32(fila.Cells["IdProducto"].Value);
            Producto producto = new CN_Producto().Listar().FirstOrDefault(p => p.IdProducto == id);
            if (producto == null)
            {
                CapaPresentacion.DialogoSistema.Show("No se pudo volver a cargar el producto seleccionado.",
                    "Producto no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            indiceProductoEditando = e.RowIndex;
            txtIdProducto.Text = id.ToString();
            txtCodigoProducto.Text = producto.Codigo;
            txtProducto.Text = Convert.ToString(fila.Cells["Producto"].Value);
            sincronizandoPrecios = true;
            txtPrecioCompra.Text = Convert.ToDecimal(fila.Cells["PrecioCompra"].Value).ToString("0.00");
            txtPrecioVenta.Text = Convert.ToDecimal(fila.Cells["PrecioVenta"].Value).ToString("0.00");
            txtPorcentaje.Text = Convert.ToDecimal(fila.Cells["Porcentaje"].Value).ToString("0.##");
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
                    "Edición pendiente", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MontoNoInventariable = montoNoInventariable
            };


            String mensaje = string.Empty;
            bool respuesta = new CN_Compra().Registrar(oCompra, detalle_compra, out mensaje);

            if (respuesta)
            {
                var result = CapaPresentacion.DialogoSistema.Show("Numero de Compra generada: \n"+oCompra.NumeroDocumento+"\n\n¿Desea Copiar al porta papeles?", "Mensaje",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information);
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
