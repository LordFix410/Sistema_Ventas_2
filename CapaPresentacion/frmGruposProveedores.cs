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
        private readonly Panel panelEdicion = new Panel();
        private readonly Label etiquetaProveedores = new Label();

        public frmGruposProveedores()
        {
            InitializeComponent();
            ConfigurarDisenoAdaptable();
            CapaPresentacion.Utilidades.ExperienciaUsuario.Aplicar(this, txtNombreGrupoProveedor, txtBusqueda);
            etiquetaProveedores.Name = "lblProveedoresIntegrantes";
            etiquetaProveedores.Text = "Proveedores integrantes:";
            etiquetaProveedores.AutoSize = true;
            etiquetaProveedores.BackColor = System.Drawing.Color.White;
            panelEdicion.Controls.Add(etiquetaProveedores);
            Controls.Add(panelEdicion);
            etiquetaProveedores.BringToFront();
            Resize += (s, e) => AjustarDiseno();
            AjustarDiseno();
            Load += (s, e) =>
            {
                cboEstado.Items.Clear(); cboEstado.Items.Add("Activo"); cboEstado.Items.Add("Inactivo"); cboEstado.SelectedIndex = 0;
                try
                {
                    PrepararProveedores();
                    Cargar();
                    AjustarDiseno();
                    if (clbProveedores.Items.Count == 0)
                        Aviso("No hay proveedores activos para agregar a un perfil. Registra o activa proveedores primero en el formulario de Proveedores.", MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Aviso("No se pudieron cargar proveedores o perfiles. Verifica la conexión y que ejecutaste migracion_reposicion_cotizaciones.sql.\n\n" + ex.Message, MessageBoxIcon.Error);
                }
            };
            btnGuardar.Click += (s, e) => Guardar(); btnCancelarUsuario.Click += (s, e) => Limpiar(); btnEliminar.Click += (s, e) => CambiarEstado();
            dgvGruposProveedores.CellClick += (s, e) => Seleccionar(e.RowIndex);
            txtBusqueda.TextChanged += (s, e) => Filtrar();
            cboBuscar.Items.Clear(); cboBuscar.Items.Add("Nombre"); cboBuscar.Items.Add("Proveedor integrante"); cboBuscar.SelectedIndex = 0;
            cboBuscar.SelectedIndexChanged += (s, e) => Filtrar();
        }

        private void ConfigurarDisenoAdaptable()
        {
            panelEdicion.Name = "panelEdicionProveedores";
            panelEdicion.AutoScroll = true;
            panelEdicion.BackColor = System.Drawing.Color.White;
            panelEdicion.BorderStyle = BorderStyle.FixedSingle;
            panelEdicion.Controls.Add(label9);
            panelEdicion.Controls.Add(label2);
            panelEdicion.Controls.Add(txtNombreGrupoProveedor);
            panelEdicion.Controls.Add(etiquetaProveedores);
            panelEdicion.Controls.Add(clbProveedores);
            panelEdicion.Controls.Add(label8);
            panelEdicion.Controls.Add(cboEstado);
            panelEdicion.Controls.Add(btnGuardar);
            panelEdicion.Controls.Add(btnCancelarUsuario);
            panelEdicion.Controls.Add(btnEliminar);
            label1.Visible = false;
        }

        private void AjustarDiseno()
        {
            if (IsDisposed || panelEdicion.IsDisposed) return;
            int margen = 12;
            int anchoLateral = Math.Min(300, Math.Max(255, ClientSize.Width / 4));
            panelEdicion.SetBounds(margen, margen, anchoLateral, Math.Max(180, ClientSize.Height - margen * 2));

            int anchoCampo = Math.Max(180, panelEdicion.ClientSize.Width - 40);
            label9.SetBounds(14, 14, anchoCampo, 24);
            label2.SetBounds(14, 48, anchoCampo, 20);
            txtNombreGrupoProveedor.SetBounds(14, 70, anchoCampo, 28);
            etiquetaProveedores.Location = new System.Drawing.Point(14, 110);
            clbProveedores.SetBounds(14, 132, anchoCampo, Math.Max(100, Math.Min(260, panelEdicion.ClientSize.Height - 340)));
            int y = clbProveedores.Bottom + 12;
            label8.SetBounds(14, y, anchoCampo, 20);
            cboEstado.SetBounds(14, y + 22, anchoCampo, 28);
            btnGuardar.SetBounds(14, y + 60, anchoCampo, 40);
            btnCancelarUsuario.SetBounds(14, y + 106, anchoCampo, 40);
            btnEliminar.SetBounds(14, y + 152, anchoCampo, 40);

            int xContenido = panelEdicion.Right + margen;
            int anchoContenido = Math.Max(300, ClientSize.Width - xContenido - margen);
            label10.SetBounds(xContenido, margen, anchoContenido, 58);
            label11.SetBounds(xContenido + Math.Min(400, anchoContenido / 3), 76, 90, 24);
            cboBuscar.SetBounds(label11.Right + 8, 72, Math.Min(190, Math.Max(120, anchoContenido / 4)), 28);
            txtBusqueda.SetBounds(cboBuscar.Right + 10, 72, Math.Min(220, Math.Max(130, anchoContenido / 4)), 28);
            dgvGruposProveedores.SetBounds(xContenido, 112, anchoContenido, Math.Max(120, ClientSize.Height - 124));
        }

        private void PrepararProveedores()
        {
            DataTable proveedores = negocio.ListarProveedores();
            clbProveedores.DataSource = proveedores; clbProveedores.DisplayMember = "RazonSocial"; clbProveedores.ValueMember = "IdProveedor";
            clbProveedores.Location = new System.Drawing.Point(14, 132); clbProveedores.Size = new System.Drawing.Size(210, 220);
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
                negocio.GuardarPerfil(idEdicion, nombre, ids); Cargar(); Limpiar(); Aviso("Perfil y proveedores guardados correctamente.", MessageBoxIcon.None);
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
