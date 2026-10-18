using ClosedXML.Excel;
using CapaNegocio;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CapaPresentacion
{
    /// <summary>Configuración de reposición y preparación de cotizaciones.</summary>
    public class frmReposicion : Form
    {
        private readonly CN_Reposicion negocio = new CN_Reposicion();
        private readonly Color azul = Color.SteelBlue;
        private readonly Color fondo = Color.FromArgb(242, 245, 248);
        private TabControl pestanas;

        private DataGridView dgvCotizaciones;
        private ComboBox cboProveedorReporte;
        private Label lblEstadoReporte;

        private DataGridView dgvGrupos;
        private TextBox txtNombreGrupo;
        private NumericUpDown nudMinimoGrupo;
        private NumericUpDown nudMaximoGrupo;
        private int? idGrupoEditado;

        private DataGridView dgvPerfiles;
        private TextBox txtNombrePerfil;
        private CheckedListBox clbProveedores;
        private int? idPerfilEditado;
        private bool cargandoPerfil;

        private DataGridView dgvProductos;
        private ComboBox cboGrupoProducto;
        private ComboBox cboPerfilProducto;
        private TextBox txtMinimoProducto;
        private TextBox txtMaximoProducto;
        private Label lblProductoSeleccionado;
        private int idProductoSeleccionado;
        private bool cargandoProducto;

        public frmReposicion()
        {
            Text = "Reposición y cotizaciones";
            BackColor = fondo;
            Font = new Font("Segoe UI", 9F);
            Dock = DockStyle.Fill;
            MinimumSize = new Size(900, 600);
            InicializarInterfaz();
            Load += delegate { RecargarTodo(); };
        }

        private void InicializarInterfaz()
        {
            var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(10) };
            raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            raiz.Controls.Add(new Label
            {
                Text = "Reposición de inventario y cotizaciones",
                Dock = DockStyle.Fill,
                BackColor = azul,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            }, 0, 0);

            pestanas = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F) };
            pestanas.TabPages.Add(CrearPestanaCotizaciones());
            pestanas.TabPages.Add(CrearPestanaConfiguracion());
            raiz.Controls.Add(pestanas, 0, 1);
            Controls.Add(raiz);
        }

        private TabPage CrearPestanaCotizaciones()
        {
            var pagina = new TabPage("Cotizaciones por proveedor") { BackColor = fondo, Padding = new Padding(8) };
            var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
            raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            var barra = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(8, 7, 4, 3), WrapContents = false };
            barra.Controls.Add(new Label { Text = "Proveedor:", AutoSize = true, Margin = new Padding(3, 7, 5, 0) });
            cboProveedorReporte = new ComboBox { Width = 250, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(3) };
            cboProveedorReporte.SelectedIndexChanged += delegate { FiltrarReporte(); };
            barra.Controls.Add(cboProveedorReporte);
            var btnActualizar = Boton("Actualizar lista", 125);
            btnActualizar.Click += delegate { CargarReporte(); };
            barra.Controls.Add(btnActualizar);
            var btnExcel = Boton("Exportar a Excel", 140);
            btnExcel.Click += delegate { ExportarCotizacion(); };
            barra.Controls.Add(btnExcel);
            lblEstadoReporte = new Label { AutoSize = true, Margin = new Padding(12, 7, 0, 0), ForeColor = Color.DimGray };
            barra.Controls.Add(lblEstadoReporte);

            dgvCotizaciones = CrearGrid();
            dgvCotizaciones.MultiSelect = false;
            dgvCotizaciones.DataBindingComplete += delegate { ConfigurarGridCotizaciones(); };
            var nota = new Label
            {
                Text = "En los grupos, el faltante se comparte entre sus productos y es solo una referencia; define las cantidades después de comparar cotizaciones.",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };
            raiz.Controls.Add(barra, 0, 0);
            raiz.Controls.Add(dgvCotizaciones, 0, 1);
            raiz.Controls.Add(nota, 0, 2);
            pagina.Controls.Add(raiz);
            return pagina;
        }

        private TabPage CrearPestanaConfiguracion()
        {
            var pagina = new TabPage("Configuración") { BackColor = fondo, Padding = new Padding(6) };
            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(CrearPestanaGrupos());
            tabs.TabPages.Add(CrearPestanaPerfiles());
            tabs.TabPages.Add(CrearPestanaProductos());
            pagina.Controls.Add(tabs);
            return pagina;
        }

        private TabPage CrearPestanaGrupos()
        {
            var pagina = new TabPage("Grupos de reposición") { BackColor = fondo, Padding = new Padding(8) };
            var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            dgvGrupos = CrearGrid();
            dgvGrupos.SelectionChanged += delegate { SeleccionarGrupo(); };

            var editor = new TableLayoutPanel { Dock = DockStyle.Top, Height = 280, ColumnCount = 2, RowCount = 5, Padding = new Padding(12) };
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            editor.Controls.Add(new Label { Text = "Grupo de productos sustituibles", Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            editor.SetColumnSpan(editor.GetControlFromPosition(0, 0), 2);
            editor.Controls.Add(Etiqueta("Nombre del grupo:"), 0, 1);
            txtNombreGrupo = new TextBox { Dock = DockStyle.Fill, MaxLength = 100 };
            editor.Controls.Add(txtNombreGrupo, 1, 1);
            editor.Controls.Add(Etiqueta("Mínimo conjunto:"), 0, 2);
            nudMinimoGrupo = Cantidad(); editor.Controls.Add(nudMinimoGrupo, 1, 2);
            editor.Controls.Add(Etiqueta("Máximo conjunto:"), 0, 3);
            nudMaximoGrupo = Cantidad(); editor.Controls.Add(nudMaximoGrupo, 1, 3);
            var acciones = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            var btnGuardar = Boton("Guardar grupo", 125); btnGuardar.Click += delegate { GuardarGrupo(); };
            var btnNuevo = Boton("Nuevo", 80); btnNuevo.Click += delegate { LimpiarGrupo(); };
            acciones.Controls.Add(btnGuardar); acciones.Controls.Add(btnNuevo);
            editor.Controls.Add(acciones, 0, 4); editor.SetColumnSpan(acciones, 2);
            var nota = new Label { Text = "El mínimo y máximo se aplican a la suma del stock de todos los productos asociados al grupo.", Dock = DockStyle.Bottom, Height = 54, ForeColor = Color.DimGray };
            var panelEditor = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(5) };
            panelEditor.Controls.Add(editor); panelEditor.Controls.Add(nota);
            raiz.Controls.Add(dgvGrupos, 0, 0); raiz.Controls.Add(panelEditor, 1, 0);
            pagina.Controls.Add(raiz);
            return pagina;
        }

        private TabPage CrearPestanaPerfiles()
        {
            var pagina = new TabPage("Perfiles de proveedores") { BackColor = fondo, Padding = new Padding(8) };
            var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            dgvPerfiles = CrearGrid();
            dgvPerfiles.SelectionChanged += delegate { SeleccionarPerfil(); };

            var editor = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(10) };
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            editor.Controls.Add(new Label { Text = "Proveedores autorizados para cotizar", Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Fill }, 0, 0);
            txtNombrePerfil = new TextBox { Dock = DockStyle.Fill, MaxLength = 100 };
            editor.Controls.Add(txtNombrePerfil, 0, 1);
            clbProveedores = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, BorderStyle = BorderStyle.FixedSingle };
            editor.Controls.Add(clbProveedores, 0, 2);
            var acciones = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            var btnGuardar = Boton("Guardar perfil", 125); btnGuardar.Click += delegate { GuardarPerfil(); };
            var btnNuevo = Boton("Nuevo", 80); btnNuevo.Click += delegate { LimpiarPerfil(); };
            acciones.Controls.Add(btnGuardar); acciones.Controls.Add(btnNuevo);
            editor.Controls.Add(acciones, 0, 3);
            raiz.Controls.Add(dgvPerfiles, 0, 0); raiz.Controls.Add(editor, 1, 0);
            pagina.Controls.Add(raiz);
            return pagina;
        }

        private TabPage CrearPestanaProductos()
        {
            var pagina = new TabPage("Asignación de productos") { BackColor = fondo, Padding = new Padding(8) };
            var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 68));
            raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 32));
            dgvProductos = CrearGrid();
            dgvProductos.SelectionChanged += delegate { SeleccionarProducto(); };
            var editor = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, ColumnCount = 4, RowCount = 3, Padding = new Padding(8) };
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            lblProductoSeleccionado = new Label { Text = "Selecciona un producto", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = azul };
            editor.Controls.Add(lblProductoSeleccionado, 0, 0); editor.SetColumnSpan(lblProductoSeleccionado, 4);
            editor.Controls.Add(Etiqueta("Grupo de reposición:"), 0, 1);
            cboGrupoProducto = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cboGrupoProducto.SelectedIndexChanged += delegate { ActualizarLimitesProducto(); };
            editor.Controls.Add(cboGrupoProducto, 1, 1);
            editor.Controls.Add(Etiqueta("Perfil de proveedores:"), 2, 1);
            cboPerfilProducto = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            editor.Controls.Add(cboPerfilProducto, 3, 1);
            editor.Controls.Add(Etiqueta("Mínimo individual:"), 0, 2);
            txtMinimoProducto = new TextBox { Dock = DockStyle.Left, Width = 90, TextAlign = HorizontalAlignment.Right };
            editor.Controls.Add(txtMinimoProducto, 1, 2);
            editor.Controls.Add(Etiqueta("Máximo individual:"), 2, 2);
            var final = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            txtMaximoProducto = new TextBox { Width = 90, TextAlign = HorizontalAlignment.Right };
            final.Controls.Add(txtMaximoProducto);
            var btnGuardar = Boton("Guardar asignación", 150); btnGuardar.Click += delegate { GuardarConfiguracionProducto(); };
            final.Controls.Add(btnGuardar);
            editor.Controls.Add(final, 3, 2);
            raiz.Controls.Add(dgvProductos, 0, 0); raiz.Controls.Add(editor, 0, 1);
            pagina.Controls.Add(raiz);
            return pagina;
        }

        private void RecargarTodo()
        {
            try
            {
                CargarOpciones();
                CargarGrupos();
                CargarPerfiles();
                CargarProductos();
                CargarReporte();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void CargarOpciones()
        {
            cboGrupoProducto.Items.Clear(); cboGrupoProducto.Items.Add(new Opcion(null, "Sin grupo: usar límites individuales"));
            foreach (DataRow fila in negocio.ListarGrupos().Rows)
                cboGrupoProducto.Items.Add(new Opcion(Convert.ToInt32(fila["IdGrupoReposicion"]), fila["Nombre"].ToString()));
            cboGrupoProducto.SelectedIndex = 0;

            cboPerfilProducto.Items.Clear(); cboPerfilProducto.Items.Add(new Opcion(null, "Sin perfil de proveedores"));
            foreach (DataRow fila in negocio.ListarPerfiles().Rows)
                cboPerfilProducto.Items.Add(new Opcion(Convert.ToInt32(fila["IdGrupoProveedor"]), fila["Nombre"].ToString()));
            cboPerfilProducto.SelectedIndex = 0;

            DataTable proveedores = negocio.ListarProveedores();
            clbProveedores.DataSource = proveedores;
            clbProveedores.DisplayMember = "RazonSocial";
            clbProveedores.ValueMember = "IdProveedor";

            cboProveedorReporte.Items.Clear();
            cboProveedorReporte.Items.Add(new Opcion(null, "Todos los proveedores"));
            foreach (DataRow fila in proveedores.Rows)
                cboProveedorReporte.Items.Add(new Opcion(Convert.ToInt32(fila["IdProveedor"]), fila["RazonSocial"].ToString()));
            cboProveedorReporte.SelectedIndex = 0;
        }

        private void CargarGrupos()
        {
            dgvGrupos.DataSource = negocio.ListarGrupos();
            Ocultar(dgvGrupos, "IdGrupoReposicion", "Activo");
            FormatearCabeceras(dgvGrupos, new[] { "Nombre", "Grupo de reposición" }, new[] { "CantidadMinima", "Mínimo conjunto" }, new[] { "CantidadMaxima", "Máximo conjunto" });
        }

        private void CargarPerfiles()
        {
            dgvPerfiles.DataSource = negocio.ListarPerfiles();
            Ocultar(dgvPerfiles, "IdGrupoProveedor", "Activo");
            FormatearCabeceras(dgvPerfiles, new[] { "Nombre", "Perfil" });
            CargarOpcionesProductos();
        }

        private void CargarOpcionesProductos()
        {
            int? grupoActual = IdSeleccionado(cboGrupoProducto);
            int? perfilActual = IdSeleccionado(cboPerfilProducto);
            var grupos = negocio.ListarGrupos();
            var perfiles = negocio.ListarPerfiles();
            cboGrupoProducto.Items.Clear(); cboGrupoProducto.Items.Add(new Opcion(null, "Sin grupo: usar límites individuales"));
            foreach (DataRow fila in grupos.Rows) cboGrupoProducto.Items.Add(new Opcion(Convert.ToInt32(fila["IdGrupoReposicion"]), fila["Nombre"].ToString()));
            cboPerfilProducto.Items.Clear(); cboPerfilProducto.Items.Add(new Opcion(null, "Sin perfil de proveedores"));
            foreach (DataRow fila in perfiles.Rows) cboPerfilProducto.Items.Add(new Opcion(Convert.ToInt32(fila["IdGrupoProveedor"]), fila["Nombre"].ToString()));
            SeleccionarOpcion(cboGrupoProducto, grupoActual); SeleccionarOpcion(cboPerfilProducto, perfilActual);
        }

        private void CargarProductos()
        {
            dgvProductos.DataSource = negocio.ListarProductos();
            Ocultar(dgvProductos, "IdProducto", "IdGrupoReposicion", "CantidadMinima", "CantidadMaxima", "IdGrupoProveedor");
            FormatearCabeceras(dgvProductos,
                new[] { "Codigo", "Código" }, new[] { "Nombre", "Producto" }, new[] { "Categoria", "Categoría" },
                new[] { "Stock", "Stock actual" }, new[] { "GrupoReposicion", "Grupo de reposición" }, new[] { "GrupoProveedores", "Perfil proveedores" });
            foreach (DataGridViewColumn col in dgvProductos.Columns)
                if (col.Visible) col.MinimumWidth = 100;
        }

        private void CargarReporte()
        {
            dgvCotizaciones.DataSource = negocio.Reporte();
            FiltrarReporte();
            int filas = (dgvCotizaciones.DataSource as DataTable)?.DefaultView.Count ?? 0;
            lblEstadoReporte.Text = filas == 0 ? "No hay productos bajo su mínimo o faltan asignaciones." : "Artículos candidatos: " + filas;
        }

        private void FiltrarReporte()
        {
            var tabla = dgvCotizaciones?.DataSource as DataTable;
            if (tabla == null) return;
            Opcion opcion = cboProveedorReporte.SelectedItem as Opcion;
            tabla.DefaultView.RowFilter = opcion == null || !opcion.Id.HasValue
                ? string.Empty : "IdProveedor = " + opcion.Id.Value;
            ConfigurarGridCotizaciones();
        }

        private void ConfigurarGridCotizaciones()
        {
            if (dgvCotizaciones.Columns.Count == 0) return;
            Ocultar(dgvCotizaciones, "IdGrupoReposicion", "IdGrupoProveedor", "IdProveedor", "IdProducto");
            FormatearCabeceras(dgvCotizaciones,
                new[] { "GrupoReposicion", "Grupo de reposición" }, new[] { "GrupoProveedores", "Perfil proveedor" },
                new[] { "Proveedor", "Proveedor" }, new[] { "Codigo", "Código" }, new[] { "Nombre", "Producto" },
                new[] { "StockActual", "Stock grupo/producto" }, new[] { "CantidadMinima", "Mínimo" },
                new[] { "CantidadMaxima", "Máximo objetivo" }, new[] { "FaltanteReferencia", "Faltante referencia" },
                new[] { "UltimoPrecioConEsteProveedor", "Último precio proveedor" },
                new[] { "FechaUltimaCompraConEsteProveedor", "Fecha compra proveedor" },
                new[] { "UltimoPrecioHistorico", "Último precio histórico" },
                new[] { "UltimoProveedorHistorico", "Último proveedor histórico" },
                new[] { "FechaUltimoPrecioHistorico", "Fecha último precio" });
            foreach (string nombre in new[] { "UltimoPrecioConEsteProveedor", "UltimoPrecioHistorico" })
                if (dgvCotizaciones.Columns.Contains(nombre)) dgvCotizaciones.Columns[nombre].DefaultCellStyle.Format = "Q #,##0.00";
        }

        private void GuardarGrupo()
        {
            try
            {
                string nombre = txtNombreGrupo.Text.Trim();
                if (nombre.Length == 0) throw new InvalidOperationException("Escribe el nombre del grupo.");
                if (nudMaximoGrupo.Value < nudMinimoGrupo.Value) throw new InvalidOperationException("El máximo debe ser igual o mayor que el mínimo.");
                idGrupoEditado = negocio.GuardarGrupo(idGrupoEditado, nombre, (int)nudMinimoGrupo.Value, (int)nudMaximoGrupo.Value);
                CargarGrupos(); CargarOpcionesProductos(); CargarProductos(); CargarReporte();
                Mensaje("El grupo de reposición quedó guardado.", "Configuración guardada", MessageBoxIcon.Information);
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void SeleccionarGrupo()
        {
            if (dgvGrupos.CurrentRow == null || dgvGrupos.CurrentRow.IsNewRow) return;
            DataRowView fila = dgvGrupos.CurrentRow.DataBoundItem as DataRowView;
            if (fila == null) return;
            idGrupoEditado = Convert.ToInt32(fila["IdGrupoReposicion"]);
            txtNombreGrupo.Text = fila["Nombre"].ToString();
            nudMinimoGrupo.Value = Convert.ToDecimal(fila["CantidadMinima"]);
            nudMaximoGrupo.Value = Convert.ToDecimal(fila["CantidadMaxima"]);
        }

        private void LimpiarGrupo()
        {
            idGrupoEditado = null; txtNombreGrupo.Clear(); nudMinimoGrupo.Value = 0; nudMaximoGrupo.Value = 0; dgvGrupos.ClearSelection();
        }

        private void GuardarPerfil()
        {
            try
            {
                string nombre = txtNombrePerfil.Text.Trim();
                if (nombre.Length == 0) throw new InvalidOperationException("Escribe el nombre del perfil.");
                int[] proveedores = clbProveedores.CheckedItems.Cast<DataRowView>().Select(r => Convert.ToInt32(r["IdProveedor"])).ToArray();
                idPerfilEditado = negocio.GuardarPerfil(idPerfilEditado, nombre, proveedores);
                CargarPerfiles(); CargarOpcionesProductos(); CargarProductos(); CargarReporte();
                Mensaje("El perfil y sus proveedores quedaron guardados.", "Configuración guardada", MessageBoxIcon.Information);
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void SeleccionarPerfil()
        {
            if (cargandoPerfil || dgvPerfiles.CurrentRow == null || dgvPerfiles.CurrentRow.IsNewRow) return;
            DataRowView fila = dgvPerfiles.CurrentRow.DataBoundItem as DataRowView;
            if (fila == null) return;
            idPerfilEditado = Convert.ToInt32(fila["IdGrupoProveedor"]);
            txtNombrePerfil.Text = fila["Nombre"].ToString();
            DataTable asignados = negocio.ProveedoresDePerfil(idPerfilEditado.Value);
            var ids = new HashSet<int>(asignados.AsEnumerable().Select(r => Convert.ToInt32(r["IdProveedor"])));
            cargandoPerfil = true;
            for (int i = 0; i < clbProveedores.Items.Count; i++)
            {
                DataRowView proveedor = clbProveedores.Items[i] as DataRowView;
                clbProveedores.SetItemChecked(i, proveedor != null && ids.Contains(Convert.ToInt32(proveedor["IdProveedor"])));
            }
            cargandoPerfil = false;
        }

        private void LimpiarPerfil()
        {
            idPerfilEditado = null; txtNombrePerfil.Clear();
            for (int i = 0; i < clbProveedores.Items.Count; i++) clbProveedores.SetItemChecked(i, false);
            dgvPerfiles.ClearSelection();
        }

        private void SeleccionarProducto()
        {
            if (cargandoProducto || dgvProductos.CurrentRow == null || dgvProductos.CurrentRow.IsNewRow) return;
            DataRowView fila = dgvProductos.CurrentRow.DataBoundItem as DataRowView;
            if (fila == null) return;
            cargandoProducto = true;
            idProductoSeleccionado = Convert.ToInt32(fila["IdProducto"]);
            lblProductoSeleccionado.Text = fila["Codigo"] + "  |  " + fila["Nombre"] + "  |  Stock: " + fila["Stock"];
            int? grupo = fila["IdGrupoReposicion"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["IdGrupoReposicion"]);
            int? perfil = fila["IdGrupoProveedor"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["IdGrupoProveedor"]);
            SeleccionarOpcion(cboGrupoProducto, grupo); SeleccionarOpcion(cboPerfilProducto, perfil);
            txtMinimoProducto.Text = fila["CantidadMinima"] == DBNull.Value ? "" : fila["CantidadMinima"].ToString();
            txtMaximoProducto.Text = fila["CantidadMaxima"] == DBNull.Value ? "" : fila["CantidadMaxima"].ToString();
            cargandoProducto = false;
            ActualizarLimitesProducto();
        }

        private void ActualizarLimitesProducto()
        {
            if (txtMinimoProducto == null || cargandoProducto) return;
            bool individual = IdSeleccionado(cboGrupoProducto) == null;
            txtMinimoProducto.Enabled = individual; txtMaximoProducto.Enabled = individual;
            if (!individual) { txtMinimoProducto.Clear(); txtMaximoProducto.Clear(); }
        }

        private void GuardarConfiguracionProducto()
        {
            try
            {
                if (idProductoSeleccionado <= 0) throw new InvalidOperationException("Selecciona un producto de la lista.");
                int? grupo = IdSeleccionado(cboGrupoProducto);
                int? minimo = null, maximo = null;
                if (!grupo.HasValue)
                {
                    if (txtMinimoProducto.Text.Trim().Length > 0 || txtMaximoProducto.Text.Trim().Length > 0)
                    {
                        int minimoValor, maximoValor;
                        if (!int.TryParse(txtMinimoProducto.Text, out minimoValor) || !int.TryParse(txtMaximoProducto.Text, out maximoValor))
                            throw new InvalidOperationException("Escribe números enteros en ambos límites o deja los dos vacíos.");
                        if (minimoValor < 0 || maximoValor < minimoValor)
                            throw new InvalidOperationException("El mínimo no puede ser negativo y el máximo debe ser igual o mayor.");
                        minimo = minimoValor; maximo = maximoValor;
                    }
                }
                negocio.GuardarConfiguracionProducto(idProductoSeleccionado, grupo, minimo, maximo, IdSeleccionado(cboPerfilProducto));
                CargarProductos(); CargarReporte();
                Mensaje("La configuración del producto quedó guardada.", "Configuración guardada", MessageBoxIcon.Information);
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void ExportarCotizacion()
        {
            DataTable origen = dgvCotizaciones.DataSource as DataTable;
            if (origen == null || origen.DefaultView.Count == 0)
            {
                Mensaje("No hay productos para exportar con este proveedor.", "Cotización vacía", MessageBoxIcon.Information);
                return;
            }
            Opcion opcion = cboProveedorReporte.SelectedItem as Opcion;
            if (opcion == null || !opcion.Id.HasValue)
            {
                Mensaje("Elige un proveedor para generar su lista de cotización.", "Selecciona proveedor", MessageBoxIcon.Information);
                return;
            }
            DataTable exportar = origen.DefaultView.ToTable(false,
                "GrupoReposicion", "GrupoProveedores", "Proveedor", "Codigo", "Nombre", "StockActual",
                "CantidadMinima", "CantidadMaxima", "FaltanteReferencia", "UltimoPrecioConEsteProveedor",
                "FechaUltimaCompraConEsteProveedor", "UltimoPrecioHistorico", "UltimoProveedorHistorico", "FechaUltimoPrecioHistorico");
            CambiarNombreColumna(exportar, "GrupoReposicion", "Grupo de reposición");
            CambiarNombreColumna(exportar, "GrupoProveedores", "Perfil de proveedores");
            CambiarNombreColumna(exportar, "Proveedor", "Proveedor");
            CambiarNombreColumna(exportar, "Codigo", "Código");
            CambiarNombreColumna(exportar, "Nombre", "Producto");
            CambiarNombreColumna(exportar, "StockActual", "Stock actual");
            CambiarNombreColumna(exportar, "CantidadMinima", "Mínimo");
            CambiarNombreColumna(exportar, "CantidadMaxima", "Máximo objetivo");
            CambiarNombreColumna(exportar, "FaltanteReferencia", "Faltante de referencia");
            CambiarNombreColumna(exportar, "UltimoPrecioConEsteProveedor", "Último precio con proveedor");
            CambiarNombreColumna(exportar, "FechaUltimaCompraConEsteProveedor", "Fecha última compra con proveedor");
            CambiarNombreColumna(exportar, "UltimoPrecioHistorico", "Último precio histórico");
            CambiarNombreColumna(exportar, "UltimoProveedorHistorico", "Último proveedor histórico");
            CambiarNombreColumna(exportar, "FechaUltimoPrecioHistorico", "Fecha último precio");
            using (var guardar = new SaveFileDialog { Filter = "Libro de Excel (*.xlsx)|*.xlsx", FileName = "Cotizacion_" + LimpiarNombreArchivo(opcion.Nombre) + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xlsx" })
            {
                if (guardar.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    using (var libro = new XLWorkbook())
                    {
                        var hoja = libro.Worksheets.Add(exportar, "Cotización");
                        hoja.Row(1).Style.Font.Bold = true;
                        hoja.SheetView.FreezeRows(1);
                        hoja.ColumnsUsed().AdjustToContents(10, 42);
                        libro.SaveAs(guardar.FileName);
                    }
                    Mensaje("Se guardó la cotización en Excel.", "Archivo generado", MessageBoxIcon.Information);
                }
                catch (Exception ex) { MostrarError(ex); }
            }
        }

        private static void CambiarNombreColumna(DataTable tabla, string original, string nuevo)
        {
            if (tabla.Columns.Contains(original)) tabla.Columns[original].ColumnName = nuevo;
        }

        private static string LimpiarNombreArchivo(string valor)
        {
            foreach (char invalido in Path.GetInvalidFileNameChars()) valor = valor.Replace(invalido, '_');
            return valor.Replace(' ', '_');
        }

        private static void SeleccionarOpcion(ComboBox combo, int? id)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                Opcion item = combo.Items[i] as Opcion;
                if (item != null && item.Id == id) { combo.SelectedIndex = i; return; }
            }
            combo.SelectedIndex = 0;
        }

        private static int? IdSeleccionado(ComboBox combo)
        {
            Opcion item = combo?.SelectedItem as Opcion;
            return item?.Id;
        }

        private static void Ocultar(DataGridView grid, params string[] columnas)
        {
            foreach (string columna in columnas) if (grid.Columns.Contains(columna)) grid.Columns[columna].Visible = false;
        }

        private static void FormatearCabeceras(DataGridView grid, params string[][] pares)
        {
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.BackgroundColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(46, 91, 121);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grid.EnableHeadersVisualStyles = false;
            foreach (string[] par in pares)
                if (par.Length == 2 && grid.Columns.Contains(par[0])) grid.Columns[par[0]].HeaderText = par[1];
        }

        private static DataGridView CrearGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
        }

        private static Label Etiqueta(string texto)
        {
            return new Label { Text = texto, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false };
        }

        private static NumericUpDown Cantidad()
        {
            return new NumericUpDown { Dock = DockStyle.Left, Width = 110, Minimum = 0, Maximum = 1000000, ThousandsSeparator = true, TextAlign = HorizontalAlignment.Right };
        }

        private static Button Boton(string texto, int ancho)
        {
            return new Button { Text = texto, Width = ancho, Height = 32, BackColor = Color.FromArgb(225, 235, 243), FlatStyle = FlatStyle.Flat, Margin = new Padding(3) };
        }

        private void MostrarError(Exception ex)
        {
            DialogoSistema.Show("No se pudo cargar o guardar la información. Verifica que hayas ejecutado migracion_reposicion_cotizaciones.sql en la base de datos.\n\nDetalle: " + ex.Message,
                "Reposición y cotizaciones", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void Mensaje(string texto, string titulo, MessageBoxIcon icono)
        {
            DialogoSistema.Show(texto, titulo, MessageBoxButtons.OK, icono);
        }

        private sealed class Opcion
        {
            public int? Id { get; private set; }
            public string Nombre { get; private set; }
            public Opcion(int? id, string nombre) { Id = id; Nombre = nombre; }
            public override string ToString() { return Nombre; }
        }
    }
}
