using CapaEntidad;
using CapaNegocio;
using CapaPresentacion.Utilidades;
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

namespace CapaPresentacion.Modales
{
    public partial class mdProducto : Form
    {
        public Producto _Producto { get; set; }
        public mdProducto()
        {
            InitializeComponent();
            CapaPresentacion.Utilidades.ExperienciaUsuario.Aplicar(this, txtBusqueda, txtBusqueda);
            txtBusqueda.KeyDown += txtBusqueda_KeyDown;
            dgvData.KeyDown += dgvData_KeyDown;
            Shown += mdProducto_Shown;
        }

        private void mdProducto_Load(object sender, EventArgs e)
        {
            foreach (DataGridViewColumn columna in dgvData.Columns)
            {
                if (columna.Visible == true)
                {
                    cboBuscar.Items.Add(new OpcionCombo() { Valor = columna.Name, Texto = columna.HeaderText });
                }
            }
            cboBuscar.DisplayMember = "Texto";
            cboBuscar.ValueMember = "Valor";
            cboBuscar.SelectedIndex = 1;

            //Mostrar todos los productos:
            List<Producto> listaProductos;
            try
            {
                listaProductos = new CN_Producto().Listar();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + ex.InnerException?.Message,
                    "No se pudieron cargar productos", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            foreach (Producto item in listaProductos.Where(p => p.Estado))
            {
                dgvData.Rows.Add(new object[] { item.IdProducto, item.Codigo, item.Nombre,
                    item.oCategoria.Descripcion, item.Stock, item.PrecioCompra, item.PrecioVenta
                    , item.Porcentaje
                });
            }
            txtBusqueda.Select();
        }

        private void mdProducto_Shown(object sender, EventArgs e)
        {
            txtBusqueda.Focus();
            txtBusqueda.SelectAll();
        }

        private void txtBusqueda_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.F5)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                btnBuscar.PerformClick();
                EnfocarPrimerResultadoVisible();
            }
        }

        private void dgvData_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (dgvData.CurrentCell != null)
                    AceptarProducto(dgvData.CurrentCell.RowIndex);
            }
        }

        private void EnfocarPrimerResultadoVisible()
        {
            foreach (DataGridViewRow fila in dgvData.Rows)
            {
                if (fila.Visible && !fila.IsNewRow)
                {
                    DataGridViewColumn primeraColumna = dgvData.Columns
                        .Cast<DataGridViewColumn>()
                        .FirstOrDefault(columna => columna.Visible);
                    if (primeraColumna == null) return;

                    dgvData.ClearSelection();
                    fila.Selected = true;
                    dgvData.CurrentCell = fila.Cells[primeraColumna.Index];
                    dgvData.Focus();
                    return;
                }
            }
        }

        private void dgvData_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) AceptarProducto(e.RowIndex);
        }

        private void AceptarProducto(int indiceFila)
        {
            if (indiceFila < 0 || indiceFila >= dgvData.Rows.Count || !dgvData.Rows[indiceFila].Visible) return;
            DataGridViewRow fila = dgvData.Rows[indiceFila];
            _Producto = new Producto()
            {
                IdProducto = Convert.ToInt32(fila.Cells["IdProducto"].Value),
                Codigo = Convert.ToString(fila.Cells["Codigo"].Value),
                Nombre = Convert.ToString(fila.Cells["Nombre"].Value),
                Stock = Convert.ToInt32(fila.Cells["Stock"].Value),
                PrecioCompra = Convert.ToDecimal(fila.Cells["PRECIOCOMPRA"].Value),
                PrecioVenta = Convert.ToDecimal(fila.Cells["PRECIOVENTA"].Value),
                Porcentaje = Convert.ToDecimal(fila.Cells["PORCENTAJE"].Value)
            };
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            string columnaFiltro = ((OpcionCombo)cboBuscar.SelectedItem).Valor.ToString();
            string busqueda = NormalizarBusqueda(txtBusqueda.Text);
            dgvData.CurrentCell = null;
            dgvData.ClearSelection();
            if (dgvData.Rows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvData.Rows)
                {
                    string valorCelda = NormalizarBusqueda(Convert.ToString(row.Cells[columnaFiltro].Value));
                    if (busqueda.Length == 0 || valorCelda.Contains(busqueda))
                        row.Visible = true;
                    else
                        row.Visible = false;
                }
            }
        }

        private static string NormalizarBusqueda(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

            string descompuesto = texto.Normalize(NormalizationForm.FormD);
            StringBuilder resultado = new StringBuilder(descompuesto.Length);
            foreach (char caracter in descompuesto)
            {
                UnicodeCategory categoria = CharUnicodeInfo.GetUnicodeCategory(caracter);
                if (categoria != UnicodeCategory.NonSpacingMark && Char.IsLetterOrDigit(caracter))
                    resultado.Append(Char.ToUpperInvariant(caracter));
            }
            return resultado.ToString();
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            if (txtBusqueda.Text != "")
            {
                txtBusqueda.Text = "";
                foreach (DataGridViewRow row in dgvData.Rows)
                {
                    row.Visible = true;
                }
            }
        }
    }
}
