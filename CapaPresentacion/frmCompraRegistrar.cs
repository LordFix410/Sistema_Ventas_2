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
        public frmCompraRegistrar(Usuario oUsuario = null)
        {
            _Usuario = oUsuario;
            InitializeComponent();
            txtCodigoProducto.KeyDown += TxtCodigoProducto_KeyDown;
            txtNuevoDocumento.KeyDown += TxtProveedor_KeyDown;
            txtRazonSocial.KeyDown += TxtProveedor_KeyDown;
            txtPrecioCompra.TextChanged += PrecioYPorcentaje_TextChanged;
            txtPorcentaje.TextChanged += PrecioYPorcentaje_TextChanged;
            ExperienciaUsuario.Aplicar(this, txtCodigoProducto, txtCodigoProducto, txtNuevoDocumento, txtRazonSocial);
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
            txtPrecioCompra.Text = producto.PrecioCompra.ToString("0.00");
            txtPorcentaje.Text = FormatoPorcentaje.ParaEditar(producto.Porcentaje, producto.PrecioCompra).ToString("0");
            txtPrecioCompra.Focus();
            txtPrecioCompra.SelectAll();
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
                    txtPrecioCompra.Text = modal._Producto.PrecioCompra.ToString("0.00");
                    txtPorcentaje.Text = FormatoPorcentaje.ParaEditar(modal._Producto.Porcentaje, modal._Producto.PrecioCompra).ToString("0");
                    txtPrecioCompra.Select();
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

        private void PrecioYPorcentaje_TextChanged(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtPrecioCompra.Text, out decimal precioCompra) || precioCompra < 0 ||
                !decimal.TryParse(txtPorcentaje.Text, out decimal porcentaje) || porcentaje < 0)
                return;

            decimal precioObjetivo = precioCompra * (1m + porcentaje / 100m);
            decimal precioRedondeado = Math.Ceiling(precioObjetivo * 4m) / 4m;
            txtPrecioVenta.Text = precioRedondeado.ToString("0.00");
        }

        private void btnAgregar_Click(object sender, EventArgs e)
        {
            decimal precioCompra = 0;
            decimal precioVenta = 0;
            decimal porcentajeSolicitado = 0;
            bool producto_existente = false;

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

            decimal precioVentaMinimo = Math.Ceiling(precioCompra * (1m + porcentajeSolicitado / 100m) * 4m) / 4m;
            if (precioVenta + 0.0001m < precioVentaMinimo || Math.Abs(precioVenta * 4m - decimal.Round(precioVenta * 4m, 0)) > 0.0001m)
            {
                CapaPresentacion.DialogoSistema.Show("El precio de venta debe respetar el margen mínimo y terminar en .00, .25, .50 o .75.", "Precio de venta inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrecioVenta.Focus();
                txtPrecioVenta.SelectAll();
                return;
            }

            decimal porcentajeReal = precioCompra == 0
                ? 0
                : decimal.Ceiling(((precioVenta - precioCompra) / precioCompra * 100m) * 10000m) / 10000m;

            foreach(DataGridViewRow fila in dataGridView1.Rows)
            {
                if (fila.Cells["IdProducto"].Value.ToString() == txtIdProducto.Text)
                {
                    producto_existente = true;
                    break;
                }
            }

            if (!producto_existente)
            {
                dataGridView1.Rows.Add(new object[]
                {
                    txtIdProducto.Text,
                    txtProducto.Text,
                        precioCompra.ToString("0.00"),
                        precioVenta.ToString("0.00"),
                    porcentajeReal.ToString("0.####"),
                    txtCantidad.Value.ToString(),
                    (txtCantidad.Value*precioCompra).ToString("0.00")
                });
                calculatTotal();
                limpiarProducto();
                txtCodigoProducto.Select();
            }
            else
            {
                CapaPresentacion.DialogoSistema.Show("El producto ya está agregado a la compra", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void limpiarProducto()
        {
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
                MontoTotal = Convert.ToDecimal(txtTotalPagar.Text)
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
