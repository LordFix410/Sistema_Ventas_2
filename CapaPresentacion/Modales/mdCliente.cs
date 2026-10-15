using CapaEntidad;
using CapaNegocio;
using CapaPresentacion.Utilidades;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CapaPresentacion.Modales
{
    public partial class mdCliente : Form
    {
        public Cliente _Cliente { get; set; }
        public mdCliente()
        {
            InitializeComponent();
            CapaPresentacion.Utilidades.ExperienciaUsuario.Aplicar(this, txtBusqueda, txtBusqueda);
            txtBusqueda.KeyDown += txtBusqueda_KeyDown;
            dgvData.KeyDown += dgvData_KeyDown;
            Shown += mdCliente_Shown;
        }

        private void mdCliente_Shown(object sender, EventArgs e)
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
                    AceptarCliente(dgvData.CurrentCell.RowIndex);
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

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            string columnaFiltro = ((OpcionCombo)cboBuscar.SelectedItem).Valor.ToString();
            dgvData.CurrentCell = null;
            dgvData.ClearSelection();
            if (dgvData.Rows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvData.Rows)
                {
                    if (Convert.ToString(row.Cells[columnaFiltro].Value).Trim().IndexOf(txtBusqueda.Text.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0)
                        row.Visible = true;
                    else
                        row.Visible = false;
                }
            }

        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            txtBusqueda.Text = "";
            foreach (DataGridViewRow row in dgvData.Rows)
                row.Visible = true;
            cboBuscar.SelectedIndex = 0;
        }

        private void mdCliente_Load(object sender, EventArgs e)
        {
            foreach (DataGridViewColumn columna in dgvData.Columns)
            {
                if (columna.Visible == true && columna.Name != "btnSeleccionar")
                {
                    cboBuscar.Items.Add(new OpcionCombo() { Valor = columna.Name, Texto = columna.HeaderText });
                }
            }
            cboBuscar.DisplayMember = "Texto";
            cboBuscar.ValueMember = "Valor";
            cboBuscar.SelectedIndex = 0;

            
            //mostrar clientes:
            List<Cliente> listCliente = new CN_Cliente().Listar();

            foreach (Cliente item in listCliente)
            {
                if (item.Estado == true)
                {
                    dgvData.Rows.Add(new object[] {item.IdCliente, item.Documento, item.NombreCompleto, item.Correo, item.Telefono}
                    );
                }
            }

            txtBusqueda.Select();
        }

        private void dgvData_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) AceptarCliente(e.RowIndex);
        }

        private void AceptarCliente(int indiceFila)
        {
            if (indiceFila < 0 || indiceFila >= dgvData.Rows.Count || !dgvData.Rows[indiceFila].Visible) return;
            DataGridViewRow fila = dgvData.Rows[indiceFila];
            _Cliente = new Cliente()
            {
                IdCliente = Convert.ToInt32(fila.Cells["Id"].Value),
                Documento = Convert.ToString(fila.Cells["Documento"].Value),
                NombreCompleto = Convert.ToString(fila.Cells["NombreCompleto"].Value)
            };
            DialogResult = DialogResult.OK;
            Close();
        }

        private void dgvData_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
