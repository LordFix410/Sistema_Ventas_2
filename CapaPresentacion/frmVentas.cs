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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CapaPresentacion
{
    public partial class frmVentas : Form
    {
        private Usuario _Usuario;
        public frmVentas(Usuario oUsuario = null)
        {
            _Usuario = oUsuario;
            InitializeComponent();
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

            ConfigurarTablaVenta();


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
            bool producto_existente = false;

            if (int.Parse(txtIdProducto.Text) == 0)
            {
                MessageBox.Show("Debe de seleccionar un producto", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!decimal.TryParse(txtPrecioCompra.Text, out precioCompra))
            {
                MessageBox.Show("Precio Compra - Formato incorrecto", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!int.TryParse(txtStock.Text, out stock))
            {
                MessageBox.Show("Stock - Formato incorrecto", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (txtCantidad.Value > stock)
            {
                MessageBox.Show("La cantidad solicitada supera el stock disponible", "Mensaje de sistema", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (DataGridViewRow fila in dataGridView1.Rows)
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
                       txtIdProducto.Text.ToString(),
                        txtProducto.Text,
                        precioCompra,
                        txtCantidad.Value.ToString(),
                        (txtCantidad.Value*precioCompra).ToString("0.00")
                   });

                    calculatTotal();
                    limpiarProducto();
                    txtCodigoProducto.Select();
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

        private void dataGridView1_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
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
                Venta oVenta = new Venta()
                {
                    oUsuario = new Usuario() { IdUsuario = _Usuario.IdUsuario },
                    TipoDocumento = ((OpcionCombo)cboTipoDocumento.SelectedItem).Texto,
                    NumeroDocumento = string.Empty,
                    DocumentoCliente = txtDocumentoCliente.Text,
                    NombreCliente = txtNombreCliente.Text,
                    MontoPago = Convert.ToDecimal(txtPagaCon.Text),
                    MontoCambio = Convert.ToDecimal(txtCambio.Text),
                    MontoTotal = Convert.ToDecimal(txtTotalPagar.Text)
                };

                string mensaje = string.Empty;
                bool respuesta = new CN_Venta().Registrar(oVenta, detalle_venta, out mensaje);

                if (respuesta)
                {
                    var result = MessageBox.Show("Numero de venta generado: \n" + oVenta.NumeroDocumento + "\n\n¿Desea copiar al portapapeles?", "Mensaje",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        Clipboard.SetText(oVenta.NumeroDocumento);
                    }

                    txtDocumentoCliente.Text = "";
                    txtNombreCliente.Text = "";
                    dataGridView1.Rows.Clear();
                    calculatTotal();
                    txtPagaCon.Text = "";
                    txtCambio.Text = "";
                }
                else
                {
                    MessageBox.Show(mensaje, "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
    }

    
}
