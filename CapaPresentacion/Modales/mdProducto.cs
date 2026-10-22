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
        private readonly bool _mostrarMargen;
        public Producto _Producto { get; set; }
        public mdProducto(bool mostrarMargen = true)
        {
            _mostrarMargen = mostrarMargen;
            InitializeComponent();
            dgvData.Columns["PORCENTAJE"].Visible = _mostrarMargen;
            dgvData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvData.RowTemplate.Height = 48;
            if (dgvData.Columns.Contains("Codigo")) dgvData.Columns["Codigo"].FillWeight = 28;
            if (dgvData.Columns.Contains("Nombre")) dgvData.Columns["Nombre"].FillWeight = 48;
            if (dgvData.Columns.Contains("Categoria")) dgvData.Columns["Categoria"].FillWeight = 24;
            CapaPresentacion.Utilidades.ExperienciaUsuario.Aplicar(this, txtBusqueda, txtBusqueda);
            txtBusqueda.KeyDown += txtBusqueda_KeyDown;
            dgvData.KeyDown += dgvData_KeyDown;
            Shown += mdProducto_Shown;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (dgvData.ContainsFocus && (keyData == Keys.Tab || keyData == (Keys.Tab | Keys.Shift)))
            {
                bool anterior = (keyData & Keys.Shift) == Keys.Shift;
                int indice = dgvData.CurrentCell == null ? (anterior ? dgvData.Rows.Count : -1) : dgvData.CurrentCell.RowIndex;
                int paso = anterior ? -1 : 1;
                for (int i = indice + paso; i >= 0 && i < dgvData.Rows.Count; i += paso)
                {
                    if (!dgvData.Rows[i].Visible || dgvData.Rows[i].IsNewRow) continue;
                    DataGridViewColumn primera = dgvData.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.Visible);
                    if (primera == null) break;
                    dgvData.ClearSelection();
                    dgvData.Rows[i].Selected = true;
                    dgvData.CurrentCell = dgvData.Rows[i].Cells[primera.Index];
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
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

            // No se carga el inventario completo al abrir el modal. El usuario
            // puede escribir un criterio y buscarlo directamente en SQL.
            dgvData.Rows.Clear();
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
                Porcentaje = _mostrarMargen
                    ? Convert.ToDecimal(fila.Cells["PORCENTAJE"].Value) /
                        (Convert.ToDecimal(fila.Cells["PRECIOCOMPRA"].Value) > 0 ? 100m : 1m)
                    : 0m
            };
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            string columnaFiltro = ((OpcionCombo)cboBuscar.SelectedItem).Valor.ToString();
            string busqueda = txtBusqueda.Text.Trim();
            bool busquedaVacia = NormalizarBusqueda(busqueda).Length == 0;
            if (busquedaVacia && CapaPresentacion.DialogoSistema.Show(this,
                "El campo está vacío y se consultará toda la lista de productos. Si el inventario es grande, la carga puede tardar. ¿Deseas continuar?",
                "Confirmar carga completa", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            try
            {
                List<Producto> resultados = busquedaVacia
                    ? new CN_Producto().Listar().Where(p => p.Estado).ToList()
                    : new CN_Producto().Buscar(columnaFiltro, busqueda, true);

                dgvData.Rows.Clear();
                foreach (Producto item in resultados)
                    dgvData.Rows.Add(new object[] { item.IdProducto, item.Codigo, item.Nombre,
                        item.oCategoria.Descripcion, item.Stock, item.PrecioCompra, item.PrecioVenta,
                        FormatoPorcentaje.ParaMostrar(item.Porcentaje, item.PrecioCompra) });

                if (resultados.Count == 0)
                    CapaPresentacion.DialogoSistema.Show("No se encontraron productos con ese criterio.", "Búsqueda",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                else if (resultados.Count == 500)
                    CapaPresentacion.DialogoSistema.Show("Se muestran los primeros 500 resultados. Especifica más la búsqueda para acotar la lista.",
                        "Resultados limitados", MessageBoxButtons.OK, MessageBoxIcon.Information);
                EnfocarPrimerResultadoVisible();
            }
            catch (Exception ex)
            {
                CapaPresentacion.DialogoSistema.Show(ex.Message + Environment.NewLine + ex.InnerException?.Message,
                    "No se pudieron buscar productos", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            }
            dgvData.Rows.Clear();
            txtBusqueda.Focus();
        }
    }
}
