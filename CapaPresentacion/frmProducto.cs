using CapaEntidad;
using CapaNegocio;
using CapaPresentacion.Utilidades;
using ClosedXML.Excel;
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
    public partial class frmProducto : Form
    {
        public frmProducto()
        {
            InitializeComponent();
            ExperienciaUsuario.Aplicar(this, txtCodigo, txtBusqueda);
            ConfigurarOrdenColumnas();
        }

        private void ConfigurarOrdenColumnas()
        {
            string[] orden = { "btnSeleccionar", "Codigo", "Nombre", "Descripcion", "Categoria", "Stock",
                "CantidadMinima", "CantidadMaxima", "PRECIOCOMPRA", "PRECIOVENTA", "PORCENTAJE",
                "GrupoReposicion", "GrupoProveedores", "Estado" };
            for (int i = 0; i < orden.Length; i++)
                if (dgvData.Columns.Contains(orden[i])) dgvData.Columns[orden[i]].DisplayIndex = i;
        }

        private int AgregarFilaProducto(Producto producto, int idProducto, string codigo, string nombre,
            string descripcion, int idCategoria, string categoria, int stock, decimal precioCompra,
            decimal precioVenta, decimal porcentaje, int? minimo, int? maximo, string grupoReposicion,
            string grupoProveedores, bool activo)
        {
            int indice = dgvData.Rows.Add();
            DataGridViewRow fila = dgvData.Rows[indice];
            fila.Cells["IdProducto"].Value = idProducto;
            fila.Cells["Codigo"].Value = codigo;
            fila.Cells["Nombre"].Value = nombre;
            fila.Cells["Descripcion"].Value = descripcion;
            fila.Cells["IDCATEGORIA"].Value = idCategoria;
            fila.Cells["Categoria"].Value = categoria;
            fila.Cells["Stock"].Value = stock;
            fila.Cells["PRECIOCOMPRA"].Value = precioCompra;
            fila.Cells["PRECIOVENTA"].Value = precioVenta;
            fila.Cells["PORCENTAJE"].Value = FormatoPorcentaje.ParaMostrar(porcentaje, precioCompra);
            fila.Cells["GrupoReposicion"].Value = grupoReposicion;
            fila.Cells["CantidadMinima"].Value = minimo.HasValue ? minimo.Value.ToString() : "";
            fila.Cells["CantidadMaxima"].Value = maximo.HasValue ? maximo.Value.ToString() : "";
            fila.Cells["GrupoProveedores"].Value = grupoProveedores;
            fila.Cells["EstadoValor"].Value = activo ? 1 : 0;
            fila.Cells["Estado"].Value = activo ? "Activo" : "Inactivo";
            fila.Tag = producto;
            return indice;
        }

        private void frmProducto_Load(object sender, EventArgs e)
        {
            cboEstado.Items.Add(new OpcionCombo() { Valor = 1, Texto = "Activo" });
            cboEstado.Items.Add(new OpcionCombo() { Valor = 0, Texto = "Inactivo" });
            cboEstado.DisplayMember = "Texto";
            cboEstado.ValueMember = "Valor";
            cboEstado.SelectedIndex = 0;
            txtPorcentaje.Text = "0";

            CargarGruposReposicion();
            CargarPerfilesProveedor();
            cboGrupoReposicion.SelectedIndexChanged += (s, ev) => ActualizarLimitesReposicion();

            List<Categoria> lista = new CN_Categoria().Listar();

            foreach (Categoria producto in lista)
            {
                cboCategoria.Items.Add(new OpcionCombo() { Valor = producto.IdCategoria, Texto = producto.Descripcion });
            }
            cboCategoria.DisplayMember = "Texto";
            cboCategoria.ValueMember = "Valor";
            cboCategoria.SelectedIndex = 0;

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

            //Mostrar todos los productos:
            List<Producto> listaProductos;
            try
            {
                listaProductos = new CN_Producto().Listar();
            }
            catch (Exception ex)
            {
                CapaPresentacion.DialogoSistema.Show(ex.Message + Environment.NewLine + ex.InnerException?.Message,
                    "No se pudieron cargar productos", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            foreach (Producto item in listaProductos)
            {
                AgregarFilaProducto(item, item.IdProducto, item.Codigo, item.Nombre, item.Descripcion,
                    item.oCategoria.IdCategoria, item.oCategoria.Descripcion, item.Stock, item.PrecioCompra,
                    item.PrecioVenta, item.Porcentaje, item.CantidadMinima, item.CantidadMaxima,
                    item.GrupoReposicion ?? "", item.GrupoProveedores ?? "", item.Estado);
            }
            dgvData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            ActualizarLimitesReposicion();

        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            string mensaje = string.Empty;
            if (!decimal.TryParse(txtPorcentaje.Text, out decimal porcentaje) || porcentaje < 0)
            {
                CapaPresentacion.DialogoSistema.Show("Ingrese un porcentaje válido igual o mayor a cero.", "Porcentaje inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPorcentaje.Focus();
                txtPorcentaje.SelectAll();
                return;
            }

            int? idGrupo = IdCombo(cboGrupoReposicion);
            int? minimo = null, maximo = null;
            if (!idGrupo.HasValue && (txtCantidadMinima.Text.Trim().Length > 0 || txtCantidadMaxima.Text.Trim().Length > 0))
            {
                int minVal, maxVal;
                if (!int.TryParse(txtCantidadMinima.Text, out minVal) || !int.TryParse(txtCantidadMaxima.Text, out maxVal) || minVal < 0 || maxVal < minVal)
                {
                    CapaPresentacion.DialogoSistema.Show("Indica mínimo y máximo válidos (enteros) o deja ambos vacíos.", "Límites inválidos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCantidadMinima.Focus(); return;
                }
                minimo = minVal; maximo = maxVal;
            }

            Producto objProducto = new Producto()
            {
                IdProducto = Convert.ToInt32(txtId.Text),
                Codigo = txtCodigo.Text,
                Nombre = txtNombre.Text,
                Descripcion = txtDescripcion.Text,
                oCategoria = new Categoria() { IdCategoria = Convert.ToInt32(((OpcionCombo)cboCategoria.SelectedItem).Valor) },
                Estado = Convert.ToInt32(((OpcionCombo)cboEstado.SelectedItem).Valor) == 1,
                Porcentaje = porcentaje,
                IdGrupoReposicion = idGrupo,
                CantidadMinima = minimo,
                CantidadMaxima = maximo,
                IdGrupoProveedor = IdCombo(cboGrupoProveedor)
            };
            if (objProducto.IdProducto == 0)
            {
                int idnuevo = new CN_Producto().Registrar(objProducto, out mensaje);

                if (mensaje == string.Empty && idnuevo != 0)
                {
                    CapaPresentacion.DialogoSistema.Show("se ha registrado correctamente");
                    objProducto.IdProducto = idnuevo;
                    AgregarFilaProducto(objProducto, idnuevo, txtCodigo.Text, txtNombre.Text, txtDescripcion.Text,
                        Convert.ToInt32(((OpcionCombo)cboCategoria.SelectedItem).Valor),
                        ((OpcionCombo)cboCategoria.SelectedItem).Texto.ToString(), 0, 0m, 0m, porcentaje,
                        minimo, maximo, GrupoNombre(cboGrupoReposicion), GrupoNombre(cboGrupoProveedor), objProducto.Estado);
                    LimpiarTxt();
                }
                else
                {
                    CapaPresentacion.DialogoSistema.Show(mensaje, "Error del Registro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                bool resultado = new CN_Producto().Editar(objProducto, out mensaje);
                if (resultado)
                {
                    DataGridViewRow row = dgvData.Rows[Convert.ToInt32(txtIndice.Text)];
                    row.Cells["IdProducto"].Value = txtId.Text;
                    row.Cells["Codigo"].Value = txtCodigo.Text;
                    row.Cells["Nombre"].Value = txtNombre.Text;
                    row.Cells["Descripcion"].Value = txtDescripcion.Text;
                    row.Cells["IDCATEGORIA"].Value = ((OpcionCombo)cboCategoria.SelectedItem).Valor.ToString();
                    row.Cells["Categoria"].Value = ((OpcionCombo)cboCategoria.SelectedItem).Texto.ToString();
                    row.Cells["EstadoValor"].Value = ((OpcionCombo)cboEstado.SelectedItem).Valor.ToString();
                    row.Cells["Estado"].Value = ((OpcionCombo)cboEstado.SelectedItem).Texto.ToString();
                    row.Cells["PORCENTAJE"].Value = objProducto.Porcentaje;
                    decimal precioCompraActual = Convert.ToDecimal(row.Cells["PRECIOCOMPRA"].Value);
                    decimal precioVentaSugerido = precioCompraActual == 0 ? 0 :
                        Math.Ceiling(precioCompraActual * (1m + objProducto.Porcentaje / 100m) * 4m) / 4m;
                    row.Cells["PRECIOVENTA"].Value = precioVentaSugerido;
                    decimal porcentajeReal = precioCompraActual == 0 ? objProducto.Porcentaje :
                        decimal.Ceiling(((precioVentaSugerido - precioCompraActual) / precioCompraActual) * 10000m) / 10000m;
                    row.Cells["PORCENTAJE"].Value = FormatoPorcentaje.ParaMostrar(porcentajeReal, precioCompraActual);
                    row.Cells["GrupoReposicion"].Value = GrupoNombre(cboGrupoReposicion);
                    row.Cells["CantidadMinima"].Value = minimo.HasValue ? minimo.Value.ToString() : "";
                    row.Cells["GrupoProveedores"].Value = GrupoNombre(cboGrupoProveedor);
                    row.Cells["CantidadMaxima"].Value = maximo.HasValue ? maximo.Value.ToString() : "";
                    row.Tag = objProducto;

                    LimpiarTxt();
                }
                else
                {
                    CapaPresentacion.DialogoSistema.Show(mensaje);
                }
            }

        }
        public void LimpiarTxt()
        {
            txtId.Text = "0";
            txtCodigo.Text = "";
            txtNombre.Text = "";
            txtDescripcion.Text = "";
            txtPorcentaje.Text = "0";
            cboCategoria.SelectedIndex = 0;
            cboEstado.SelectedIndex = 0;
            txtCantidadMinima.Clear(); txtCantidadMaxima.Clear();
            if (cboGrupoReposicion.Items.Count > 0) cboGrupoReposicion.SelectedIndex = 0;
            if (cboGrupoProveedor.Items.Count > 0) cboGrupoProveedor.SelectedIndex = 0;
            ActualizarLimitesReposicion();

        }

        private void CargarGruposReposicion()
        {
            cboGrupoReposicion.Items.Clear(); cboGrupoReposicion.Items.Add(new OpcionReposicion(null, "Sin grupo (límites propios)"));
            foreach (DataRow fila in new CN_Reposicion().ListarGrupos().Rows)
                if (Convert.ToBoolean(fila["Activo"])) cboGrupoReposicion.Items.Add(new OpcionReposicion(Convert.ToInt32(fila["IdGrupoReposicion"]), Convert.ToString(fila["Nombre"])));
            cboGrupoReposicion.SelectedIndex = 0;
        }

        private void CargarPerfilesProveedor()
        {
            cboGrupoProveedor.Items.Clear(); cboGrupoProveedor.Items.Add(new OpcionReposicion(null, "Sin perfil de proveedores"));
            foreach (DataRow fila in new CN_Reposicion().ListarPerfiles().Rows)
                if (Convert.ToBoolean(fila["Activo"])) cboGrupoProveedor.Items.Add(new OpcionReposicion(Convert.ToInt32(fila["IdGrupoProveedor"]), Convert.ToString(fila["Nombre"])));
            cboGrupoProveedor.SelectedIndex = 0;
        }

        private void ActualizarLimitesReposicion()
        {
            bool independiente = IdCombo(cboGrupoReposicion) == null;
            txtCantidadMinima.Enabled = independiente; txtCantidadMaxima.Enabled = independiente;
            if (!independiente) { txtCantidadMinima.Clear(); txtCantidadMaxima.Clear(); }
        }

        private static int? IdCombo(ComboBox combo)
        {
            OpcionReposicion opcion = combo.SelectedItem as OpcionReposicion;
            return opcion == null ? (int?)null : opcion.Id;
        }

        private static string GrupoNombre(ComboBox combo)
        {
            OpcionReposicion opcion = combo.SelectedItem as OpcionReposicion;
            return opcion == null || !opcion.Id.HasValue ? "" : opcion.Nombre;
        }

        private static void SeleccionarCombo(ComboBox combo, int? id)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                OpcionReposicion opcion = combo.Items[i] as OpcionReposicion;
                if (opcion != null && opcion.Id == id) { combo.SelectedIndex = i; return; }
            }
            combo.SelectedIndex = 0;
        }

        private sealed class OpcionReposicion
        {
            public int? Id { get; private set; }
            public string Nombre { get; private set; }
            public OpcionReposicion(int? id, string nombre) { Id = id; Nombre = nombre; }
            public override string ToString() { return Nombre; }
        }

        private void dgvData_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            if (dgvData.Columns[e.ColumnIndex].Name == "btnSeleccionar")
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

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            if (Convert.ToInt32(txtId.Text) != 0)
            {
                if (CapaPresentacion.DialogoSistema.Show("¿Desea Eliminar el Producto?", "Mensaje del sistema", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                {
                    string mensaje = string.Empty;
                    Producto oproducto = new Producto
                    {
                        IdProducto = Convert.ToInt32(txtId.Text)
                    };

                    bool respuesta = new CN_Producto().Eliminar(oproducto, out mensaje);

                    if (respuesta)
                    {
                        dgvData.Rows.RemoveAt(Convert.ToInt32(txtIndice.Text));
                        LimpiarTxt();
                    }
                    else CapaPresentacion.DialogoSistema.Show(mensaje, "Mensaje del sistema", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        private void btnCancelarUsuario_Click(object sender, EventArgs e)
        {
            LimpiarTxt();
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            string columnaFiltro = ((OpcionCombo)cboBuscar.SelectedItem).Valor.ToString();
            if (dgvData.Rows.Count > 0 && ((OpcionCombo)cboBuscar.SelectedItem).Valor.ToString() != "Estado")
            {
                foreach (DataGridViewRow row in dgvData.Rows)
                {
                    if (row.Cells[columnaFiltro].Value.ToString().Trim().ToUpper().Contains(txtBusqueda.Text.Trim().ToUpper()))
                        row.Visible = true;
                    else
                        row.Visible = false;
                }
            }
            else if (((OpcionCombo)cboBuscar.SelectedItem).Valor.ToString() == "Estado")
            {

                bool buscado;
                if (txtBusqueda.Text.ToString().Trim().ToUpper() == "INACTIVO")
                    buscado = false;
                else
                    buscado = true;

                foreach (DataGridViewRow row in dgvData.Rows)
                {
                    bool estado;
                    if (row.Cells["Estado"].Value.ToString().Trim().ToUpper() == "INACTIVO")
                    {
                        estado = false;
                    }
                    else
                        estado = true;

                    if (buscado == estado)
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

        private void dgvData_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvData.Columns[e.ColumnIndex].Name == "btnSeleccionar")
            {
                int indice = e.RowIndex;

                if (indice >= 0)
                {
                    txtIndice.Text = indice.ToString();
                    txtId.Text = dgvData.Rows[indice].Cells["IdProducto"].Value.ToString();
                    txtCodigo.Text = dgvData.Rows[indice].Cells["Codigo"].Value.ToString();
                    txtNombre.Text = dgvData.Rows[indice].Cells["Nombre"].Value.ToString();
                    txtDescripcion.Text = dgvData.Rows[indice].Cells["Descripcion"].Value.ToString();
                    decimal porcentajeVisible = Convert.ToDecimal(dgvData.Rows[indice].Cells["PORCENTAJE"].Value);
                    txtPorcentaje.Text = decimal.Ceiling(porcentajeVisible).ToString("0");
                    Producto productoFila = dgvData.Rows[indice].Tag as Producto;
                    SeleccionarCombo(cboGrupoReposicion, productoFila == null ? (int?)null : productoFila.IdGrupoReposicion);
                    SeleccionarCombo(cboGrupoProveedor, productoFila == null ? (int?)null : productoFila.IdGrupoProveedor);
                    txtCantidadMinima.Text = Convert.ToString(dgvData.Rows[indice].Cells["CantidadMinima"].Value);
                    txtCantidadMaxima.Text = Convert.ToString(dgvData.Rows[indice].Cells["CantidadMaxima"].Value);
                    ActualizarLimitesReposicion();

                    foreach (OpcionCombo oc in cboCategoria.Items)
                    {
                        if (Convert.ToInt32(oc.Valor) == Convert.ToInt32(dgvData.Rows[indice].Cells["IDCATEGORIA"].Value))
                        {
                            cboCategoria.SelectedIndex = cboCategoria.Items.IndexOf(oc); ;
                            break;
                        }
                    }

                    foreach (OpcionCombo oc in cboEstado.Items)
                    {
                        if (Convert.ToInt32(oc.Valor) == Convert.ToInt32(dgvData.Rows[indice].Cells["EstadoValor"].Value))
                        {
                            cboEstado.SelectedIndex = cboEstado.Items.IndexOf(oc);
                            break;
                        }

                    }
                }
            }
        }

        private void iconButton1_Click(object sender, EventArgs e)
        {
            if (dgvData.Rows.Count < 1)
            {
                CapaPresentacion.DialogoSistema.Show("No hay datos para exportar", "Mensaje del sistema", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                DataTable dt = new DataTable();

                List<DataGridViewColumn> columnasExportar = dgvData.Columns.Cast<DataGridViewColumn>()
                    .Where(c => c.Visible && c.HeaderText != "").ToList();
                foreach (DataGridViewColumn columna in columnasExportar)
                {
                    dt.Columns.Add(columna.HeaderText, typeof(string));
                }

                foreach(DataGridViewRow row in dgvData.Rows)
                {
                    if (row.Visible)
                        dt.Rows.Add(columnasExportar.Select(c => Convert.ToString(row.Cells[c.Name].Value)).ToArray());
                }

                SaveFileDialog savefile = new SaveFileDialog();
                savefile.FileName = string.Format("ReporteProducto_{0}.xlsx", DateTime.Now.ToString("ddMMyyyyHHmmss"));
                savefile.Filter = "Excel Files | *.xlsx";

                if(savefile.ShowDialog()== DialogResult.OK)
                {
                    try
                    {
                        XLWorkbook wb = new XLWorkbook();
                        var hoja = wb.Worksheets.Add(dt, "Informe");
                        hoja.ColumnsUsed().AdjustToContents();
                        wb.SaveAs(savefile.FileName);
                        CapaPresentacion.DialogoSistema.Show("Reporte Generado", "mensaje", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch
                    {
                        CapaPresentacion.DialogoSistema.Show("Error al generar el reporte", "mensaje del sistema", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

            }
        }
    }
}
