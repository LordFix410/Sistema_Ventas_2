using CapaNegocio;
using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace CapaPresentacion
{
    public partial class frmGruposReposicion : Form
    {
        private readonly CN_Reposicion negocio = new CN_Reposicion();
        private int? idEdicion;

        public frmGruposReposicion()
        {
            InitializeComponent();
            Load += (s, e) => { cboEstado.Items.Clear(); cboEstado.Items.Add("Activo"); cboEstado.Items.Add("Inactivo"); cboEstado.SelectedIndex = 0; Cargar(); };
            btnGuardarGrupo.Click += (s, e) => Guardar();
            btnCancelarGrupo.Click += (s, e) => Limpiar();
            btnEliminarGrupo.Click += (s, e) => CambiarEstado();
            dgvGrupos.CellClick += (s, e) => Seleccionar(e.RowIndex);
            txtBusqueda.TextChanged += (s, e) => Filtrar();
            cboBuscar.Items.Clear(); cboBuscar.Items.Add("Nombre"); cboBuscar.SelectedIndex = 0;
            numMinimoGrupo.Maximum = numMaximoGrupo.Maximum = 1000000;
            numMinimoGrupo.Minimum = numMaximoGrupo.Minimum = 0;
            numMinimoGrupo.ThousandsSeparator = numMaximoGrupo.ThousandsSeparator = true;
        }

        private void Cargar()
        {
            dgvGrupos.Columns.Clear(); dgvGrupos.AutoGenerateColumns = true;
            dgvGrupos.DataSource = negocio.ListarGrupos();
            Ocultar("IdGrupoReposicion", "Activo");
            Renombrar("Nombre", "Grupo de reposición"); Renombrar("CantidadMinima", "Mínimo total"); Renombrar("CantidadMaxima", "Máximo objetivo");
            dgvGrupos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvGrupos.RowHeadersVisible = false;
            dgvGrupos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvGrupos.ReadOnly = true; dgvGrupos.MultiSelect = false;
        }

        private void Guardar()
        {
            try
            {
                string nombre = txtNombreGrupo.Text.Trim();
                if (nombre.Length == 0) throw new InvalidOperationException("Escribe el nombre del grupo.");
                if (numMaximoGrupo.Value < numMinimoGrupo.Value) throw new InvalidOperationException("El máximo no puede ser menor que el mínimo.");
                negocio.GuardarGrupo(idEdicion, nombre, (int)numMinimoGrupo.Value, (int)numMaximoGrupo.Value);
                Cargar(); Limpiar(); Aviso("Grupo guardado correctamente.", MessageBoxIcon.Information);
            }
            catch (Exception ex) { Aviso(ex.Message, MessageBoxIcon.Error); }
        }

        private void Seleccionar(int row)
        {
            if (row < 0 || dgvGrupos.Rows[row].IsNewRow) return;
            DataGridViewRow r = dgvGrupos.Rows[row];
            idEdicion = Convert.ToInt32(r.Cells["IdGrupoReposicion"].Value);
            txtId.Text = idEdicion.ToString(); txtNombreGrupo.Text = Convert.ToString(r.Cells["Nombre"].Value);
            numMinimoGrupo.Value = Convert.ToDecimal(r.Cells["CantidadMinima"].Value);
            numMaximoGrupo.Value = Convert.ToDecimal(r.Cells["CantidadMaxima"].Value);
            cboEstado.SelectedIndex = Convert.ToBoolean(r.Cells["Activo"].Value) ? 0 : 1;
        }

        private void CambiarEstado()
        {
            if (!idEdicion.HasValue) return;
            bool nuevoEstado = !Convert.ToBoolean(dgvGrupos.Rows.Cast<DataGridViewRow>().First(r => Convert.ToInt32(r.Cells["IdGrupoReposicion"].Value) == idEdicion.Value).Cells["Activo"].Value);
            if (DialogoSistema.Show(nuevoEstado ? "¿Activar este grupo?" : "¿Desactivar este grupo?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try { negocio.CambiarEstadoGrupo(idEdicion.Value, nuevoEstado); Cargar(); Limpiar(); }
            catch (Exception ex) { Aviso(ex.Message, MessageBoxIcon.Error); }
        }

        private void Filtrar()
        {
            DataTable t = dgvGrupos.DataSource as DataTable;
            if (t == null) return;
            string filtro = txtBusqueda.Text.Replace("'", "''");
            t.DefaultView.RowFilter = filtro.Length == 0 ? "" : "Convert(Nombre, 'System.String') LIKE '%" + filtro + "%";
        }
        private void Limpiar() { idEdicion = null; txtId.Text = "0"; txtNombreGrupo.Clear(); numMinimoGrupo.Value = 0; numMaximoGrupo.Value = 0; cboEstado.SelectedIndex = 0; dgvGrupos.ClearSelection(); txtNombreGrupo.Focus(); }
        private void Ocultar(params string[] names) { foreach (string n in names) if (dgvGrupos.Columns.Contains(n)) dgvGrupos.Columns[n].Visible = false; }
        private void Renombrar(string col, string texto) { if (dgvGrupos.Columns.Contains(col)) dgvGrupos.Columns[col].HeaderText = texto; }
        private void Aviso(string s, MessageBoxIcon i) { DialogoSistema.Show(s, "Grupos de reposición", MessageBoxButtons.OK, i); }
    }
}
