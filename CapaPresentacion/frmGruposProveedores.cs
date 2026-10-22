using CapaNegocio;
using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace CapaPresentacion
{
    public partial class frmGruposProveedores : Form
    {
        private readonly CN_Reposicion negocio = new CN_Reposicion();
        private int? idEdicion;
        private bool cargando;

        public frmGruposProveedores()
        {
            InitializeComponent();
            CapaPresentacion.Utilidades.ExperienciaUsuario.Aplicar(this, txtNombreGrupoProveedor, txtBusqueda);
            dgvGruposProveedores.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            var etiquetaProveedores = new Label
            {
                Name = "lblProveedoresIntegrantes",
                Text = "Proveedores integrantes:",
                AutoSize = true,
                Location = new System.Drawing.Point(52, 108),
                BackColor = System.Drawing.Color.White
            };
            Controls.Add(etiquetaProveedores);
            etiquetaProveedores.BringToFront();
            Load += (s, e) => { cboEstado.Items.Clear(); cboEstado.Items.Add("Activo"); cboEstado.Items.Add("Inactivo"); cboEstado.SelectedIndex = 0; PrepararProveedores(); Cargar(); };
            btnGuardar.Click += (s, e) => Guardar(); btnCancelarUsuario.Click += (s, e) => Limpiar(); btnEliminar.Click += (s, e) => CambiarEstado();
            dgvGruposProveedores.CellClick += (s, e) => Seleccionar(e.RowIndex);
            txtBusqueda.TextChanged += (s, e) => Filtrar();
            cboBuscar.Items.Clear(); cboBuscar.Items.Add("Nombre"); cboBuscar.Items.Add("Proveedor integrante"); cboBuscar.SelectedIndex = 0;
            cboBuscar.SelectedIndexChanged += (s, e) => Filtrar();
        }

        private void PrepararProveedores()
        {
            DataTable proveedores = negocio.ListarProveedores();
            clbProveedores.DataSource = proveedores; clbProveedores.DisplayMember = "RazonSocial"; clbProveedores.ValueMember = "IdProveedor";
            clbProveedores.Location = new System.Drawing.Point(52, 130); clbProveedores.Size = new System.Drawing.Size(210, 320);
            clbProveedores.CheckOnClick = true; clbProveedores.Font = new System.Drawing.Font("Segoe UI", 9F);
            clbProveedores.HorizontalScrollbar = true; clbProveedores.IntegralHeight = false;
        }

        private void Cargar()
        {
            dgvGruposProveedores.Columns.Clear(); dgvGruposProveedores.AutoGenerateColumns = true; dgvGruposProveedores.DataSource = negocio.ListarPerfiles();
            foreach (string n in new[] { "IdGrupoProveedor", "Activo" }) if (dgvGruposProveedores.Columns.Contains(n)) dgvGruposProveedores.Columns[n].Visible = false;
            if (dgvGruposProveedores.Columns.Contains("Nombre")) { dgvGruposProveedores.Columns["Nombre"].HeaderText = "Perfil de proveedores"; }
            if (dgvGruposProveedores.Columns.Contains("Proveedores")) { dgvGruposProveedores.Columns["Proveedores"].HeaderText = "Proveedores integrantes"; }
            if (dgvGruposProveedores.Columns.Contains("Activo")) dgvGruposProveedores.Columns["Activo"].HeaderText = "Estado";
            dgvGruposProveedores.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; dgvGruposProveedores.ReadOnly = true;
            if (dgvGruposProveedores.Columns.Contains("Nombre")) dgvGruposProveedores.Columns["Nombre"].FillWeight = 35;
            if (dgvGruposProveedores.Columns.Contains("Proveedores")) dgvGruposProveedores.Columns["Proveedores"].FillWeight = 65;
            dgvGruposProveedores.SelectionMode = DataGridViewSelectionMode.FullRowSelect; dgvGruposProveedores.RowHeadersVisible = false;
        }

        private void Guardar()
        {
            try
            {
                string nombre = txtNombreGrupoProveedor.Text.Trim();
                if (nombre.Length == 0) throw new InvalidOperationException("Escribe el nombre del perfil.");
                int[] ids = clbProveedores.CheckedItems.Cast<DataRowView>().Select(x => Convert.ToInt32(x["IdProveedor"])).ToArray();
                negocio.GuardarPerfil(idEdicion, nombre, ids); Cargar(); Limpiar(); Aviso("Perfil y proveedores guardados correctamente.", MessageBoxIcon.Information);
            }
            catch (Exception ex) { Aviso(ex.Message, MessageBoxIcon.Error); }
        }

        private void Seleccionar(int row)
        {
            if (cargando || row < 0 || dgvGruposProveedores.Rows[row].IsNewRow) return;
            DataGridViewRow r = dgvGruposProveedores.Rows[row]; idEdicion = Convert.ToInt32(r.Cells["IdGrupoProveedor"].Value);
            txtId.Text = idEdicion.ToString(); txtNombreGrupoProveedor.Text = Convert.ToString(r.Cells["Nombre"].Value);
            cboEstado.SelectedIndex = Convert.ToBoolean(r.Cells["Activo"].Value) ? 0 : 1;
            DataTable asignados = negocio.ProveedoresDePerfil(idEdicion.Value);
            var ids = new System.Collections.Generic.HashSet<int>(asignados.AsEnumerable().Select(x => Convert.ToInt32(x["IdProveedor"])));
            cargando = true;
            for (int i = 0; i < clbProveedores.Items.Count; i++)
            {
                DataRowView proveedor = clbProveedores.Items[i] as DataRowView;
                clbProveedores.SetItemChecked(i, proveedor != null && ids.Contains(Convert.ToInt32(proveedor["IdProveedor"])));
            }
            cargando = false;
        }

        private void CambiarEstado()
        {
            if (!idEdicion.HasValue) return;
            DataGridViewRow seleccionado = dgvGruposProveedores.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => Convert.ToInt32(r.Cells["IdGrupoProveedor"].Value) == idEdicion.Value);
            bool activo = seleccionado != null && !Convert.ToBoolean(seleccionado.Cells["Activo"].Value);
            if (DialogoSistema.Show(activo ? "¿Activar este perfil?" : "¿Desactivar este perfil?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try { negocio.CambiarEstadoPerfil(idEdicion.Value, activo); Cargar(); Limpiar(); }
            catch (Exception ex) { Aviso(ex.Message, MessageBoxIcon.Error); }
        }

        private void Filtrar()
        {
            DataTable t = dgvGruposProveedores.DataSource as DataTable; if (t == null) return;
            string filtro = txtBusqueda.Text.Replace("'", "''");
            string columna = cboBuscar.SelectedIndex == 1 ? "Proveedores" : "Nombre";
            t.DefaultView.RowFilter = filtro.Length == 0 ? "" : "Convert([" + columna + "], 'System.String') LIKE '%" + filtro + "%";
        }
        private void Limpiar()
        {
            idEdicion = null; txtId.Text = "0"; txtNombreGrupoProveedor.Clear(); cboEstado.SelectedIndex = 0;
            cargando = true; for (int i = 0; i < clbProveedores.Items.Count; i++) clbProveedores.SetItemChecked(i, false); cargando = false;
            dgvGruposProveedores.ClearSelection(); txtNombreGrupoProveedor.Focus();
        }
        private void Aviso(string s, MessageBoxIcon i) { DialogoSistema.Show(s, "Perfiles de proveedores", MessageBoxButtons.OK, i); }
    }
}
