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
        private bool sincronizandoPreciosProducto;

        public frmProducto()
        {
            InitializeComponent();
            txtPrecioVentaProducto.KeyDown += PrecioVentaProducto_KeyDown;
            txtCostoProducto.Leave += delegate { ActualizarPorcentajeProducto(); };
            txtPorcentaje.KeyDown += PorcentajeProducto_KeyDown;
            txtCostoProducto.Enter += delegate { txtCostoProducto.SelectAll(); };
            txtPrecioVentaProducto.Enter += delegate { txtPrecioVentaProducto.SelectAll(); };
            txtPorcentaje.Enter += delegate { txtPorcentaje.SelectAll(); };
            txtPrecioVentaProducto.Leave += delegate { ActualizarPorcentajeProducto(); };
            txtPorcentaje.Leave += delegate { ActualizarPrecioDesdeMargenSinAviso(); };
            ConfigurarOrdenCaptura();
            ConfigurarOrdenColumnas();
            ExperienciaUsuario.Aplicar(this, txtCodigo, txtBusqueda, txtPorcentaje);
        }

        private void ConfigurarOrdenCaptura()
        {
            Control[] orden = { txtCodigo, txtNombre, txtDescripcion, cboCategoria, txtCostoProducto,
                txtPorcentaje, txtPrecioVentaProducto, cboGrupoProveedor, cboGrupoReposicion,
                txtCantidadMinima, txtCantidadMaxima, cboEstado, btnGuardar, btnCancelarUsuario, btnEliminar };
            for (int i = 0; i < orden.Length; i++)
            {
                orden[i].TabIndex = i;
                orden[i].TabStop = true;
            }
            txtBusqueda.TabIndex = orden.Length + 2;
            cboBuscar.TabIndex = orden.Length + 1;
        }

        private void PrecioVentaProducto_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            if (!decimal.TryParse(txtPrecioVentaProducto.Text, out decimal venta) || venta < 0)
            {
                CapaPresentacion.DialogoSistema.Show("Ingresa un precio de venta válido.", "Precio inválido",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrecioVentaProducto.Focus();
                txtPrecioVentaProducto.SelectAll();
                return;
            }
            ActualizarPorcentajeProducto();
            cboGrupoProveedor.Focus();
        }

        private void ActualizarPrecioDesdeMargenSinAviso()
        {
            if (sincronizandoPreciosProducto ||
                !decimal.TryParse(txtCostoProducto.Text, out decimal costo) || costo <= 0 ||
                !decimal.TryParse(txtPorcentaje.Text, out decimal margen) || margen < 0)
                return;
            decimal venta = Math.Ceiling(costo * (1m + margen / 100m) * 4m) / 4m;
            sincronizandoPreciosProducto = true;
            txtPrecioVentaProducto.Text = venta.ToString("0.00");
            sincronizandoPreciosProducto = false;
        }

        private void ActualizarPorcentajeProducto()
        {
            if (sincronizandoPreciosProducto ||
                !decimal.TryParse(txtCostoProducto.Text, out decimal costo) || costo <= 0 ||
                !decimal.TryParse(txtPrecioVentaProducto.Text, out decimal venta)) return;

            decimal margen = (venta - costo) / costo * 100m;
            sincronizandoPreciosProducto = true;
            txtPorcentaje.Text = margen.ToString("0.##");
            sincronizandoPreciosProducto = false;
        }

        private void PorcentajeProducto_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            if (!decimal.TryParse(txtCostoProducto.Text, out decimal costo) || costo < 0 ||
                !decimal.TryParse(txtPorcentaje.Text, out decimal margen) || margen < 0)
            {
                CapaPresentacion.DialogoSistema.Show("Ingresa un costo y un margen válidos.", "Precios inválidos",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPorcentaje.Focus();
                txtPorcentaje.SelectAll();
                return;
            }

            if (costo == 0)
            {
                if (margen <= 0 || !decimal.TryParse(txtPrecioVentaProducto.Text, out decimal ventaExistente) || ventaExistente <= 0)
                {
                    CapaPresentacion.DialogoSistema.Show(
                        "Para calcular el costo, ingresa un margen mayor que cero y un precio de venta mayor que cero.",
                        "Datos incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    if (margen <= 0)
                    {
                        txtPorcentaje.Focus();
                        txtPorcentaje.SelectAll();
                    }
                    else
                    {
                        txtPrecioVentaProducto.Focus();
                        txtPrecioVentaProducto.SelectAll();
                    }
                    return;
                }

                costo = Math.Round(ventaExistente / (1m + margen / 100m), 2, MidpointRounding.AwayFromZero);
                if (costo <= 0)
                {
                    CapaPresentacion.DialogoSistema.Show("Con esos valores el costo calculado queda en cero. Revisa el precio y el margen.",
                        "Costo calculado inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPrecioVentaProducto.Focus();
                    txtPrecioVentaProducto.SelectAll();
                    return;
                }

                sincronizandoPreciosProducto = true;
                txtCostoProducto.Text = costo.ToString("0.00");
                sincronizandoPreciosProducto = false;
                ActualizarPorcentajeProducto();
                txtPrecioVentaProducto.Focus();
                txtPrecioVentaProducto.SelectAll();
                return;
            }

            decimal venta = Math.Ceiling(costo * (1m + margen / 100m) * 4m) / 4m;
            sincronizandoPreciosProducto = true;
            txtPrecioVentaProducto.Text = venta.ToString("0.00");
            sincronizandoPreciosProducto = false;
            ActualizarPorcentajeProducto();
            txtPrecioVentaProducto.Focus();
            txtPrecioVentaProducto.SelectAll();
        }

        private static decimal PorcentajeGuardado(decimal costo, decimal venta, decimal porcentajeSinCosto)
        {
            return costo <= 0 ? porcentajeSinCosto :
                decimal.Ceiling(((venta - costo) / costo) * 10000m) / 10000m;
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
            txtCostoProducto.Text = "0.00";
            txtPrecioVentaProducto.Text = "0.00";
            txtCostoProducto.Text = "0.00";
            txtPrecioVentaProducto.Text = "0.00";

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

            // La lista se consulta solo cuando el usuario busca; abrir el mantenedor no lee todo PRODUCTO.
            dgvData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            var anchos = new Dictionary<string, int>
            {
                ["Codigo"] = 85, ["Nombre"] = 145, ["Descripcion"] = 160, ["Categoria"] = 105,
                ["Stock"] = 60, ["CantidadMinima"] = 65, ["CantidadMaxima"] = 65,
                ["PRECIOCOMPRA"] = 85, ["PRECIOVENTA"] = 85, ["PORCENTAJE"] = 80,
                ["GrupoReposicion"] = 125, ["GrupoProveedores"] = 125, ["Estado"] = 70
            };
            foreach (var ancho in anchos)
                if (dgvData.Columns.Contains(ancho.Key)) dgvData.Columns[ancho.Key].Width = ancho.Value;
            if (dgvData.Columns.Contains("btnSeleccionar")) dgvData.Columns["btnSeleccionar"].Width = 38;
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

            if (!decimal.TryParse(txtCostoProducto.Text, out decimal costo) || costo < 0 ||
                !decimal.TryParse(txtPrecioVentaProducto.Text, out decimal precioVenta) || precioVenta < 0)
            {
                CapaPresentacion.DialogoSistema.Show("Ingresa un costo y precio de venta válidos, iguales o mayores a cero.",
                    "Precios inválidos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCostoProducto.Focus();
                txtCostoProducto.SelectAll();
                return;
            }
            if (costo > 0 && precioVenta < costo)
            {
                CapaPresentacion.DialogoSistema.Show("El precio de venta no puede ser menor al costo.",
                    "Margen negativo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrecioVentaProducto.Focus();
                txtPrecioVentaProducto.SelectAll();
                return;
            }
            if (Math.Abs(precioVenta * 4m - decimal.Round(precioVenta * 4m, 0)) > 0.0001m)
            {
                CapaPresentacion.DialogoSistema.Show("El precio de venta debe terminar en .00, .25, .50 o .75.",
                    "Precio inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrecioVentaProducto.Focus();
                txtPrecioVentaProducto.SelectAll();
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
                PrecioCompra = costo,
                PrecioVenta = precioVenta,
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
                        ((OpcionCombo)cboCategoria.SelectedItem).Texto.ToString(), 0, costo, precioVenta,
                        PorcentajeGuardado(costo, precioVenta, porcentaje),
                        minimo, maximo, GrupoNombre(cboGrupoReposicion), GrupoNombre(cboGrupoProveedor), objProducto.Estado);
                    objProducto.Porcentaje = PorcentajeGuardado(costo, precioVenta, porcentaje);
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
                    row.Cells["PRECIOCOMPRA"].Value = costo;
                    row.Cells["PRECIOVENTA"].Value = precioVenta;
                    decimal porcentajeReal = PorcentajeGuardado(costo, precioVenta, porcentaje);
                    row.Cells["PORCENTAJE"].Value = FormatoPorcentaje.ParaMostrar(porcentajeReal, costo);
                    row.Cells["GrupoReposicion"].Value = GrupoNombre(cboGrupoReposicion);
                    row.Cells["CantidadMinima"].Value = minimo.HasValue ? minimo.Value.ToString() : "";
                    row.Cells["GrupoProveedores"].Value = GrupoNombre(cboGrupoProveedor);
                    row.Cells["CantidadMaxima"].Value = maximo.HasValue ? maximo.Value.ToString() : "";
                    objProducto.Porcentaje = porcentajeReal;
                    objProducto.PrecioCompra = costo;
                    objProducto.PrecioVenta = precioVenta;
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
            txtCostoProducto.Text = "0.00";
            txtPrecioVentaProducto.Text = "0.00";
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
            string termino = txtBusqueda.Text.Trim();
            if (termino.Length == 0 && CapaPresentacion.DialogoSistema.Show(this,
                "El campo está vacío y se consultará toda la lista de productos. Si el inventario es grande, la carga puede tardar. ¿Deseas continuar?",
                "Confirmar carga completa", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            try
            {
                string columna = ((OpcionCombo)cboBuscar.SelectedItem).Valor.ToString();
                List<Producto> resultados = termino.Length == 0
                    ? new CN_Producto().Listar()
                    : new CN_Producto().Buscar(columna, termino);
                dgvData.Rows.Clear();
                foreach (Producto item in resultados)
                    AgregarFilaProducto(item, item.IdProducto, item.Codigo, item.Nombre, item.Descripcion,
                        item.oCategoria.IdCategoria, item.oCategoria.Descripcion, item.Stock, item.PrecioCompra,
                        item.PrecioVenta, item.Porcentaje, item.CantidadMinima, item.CantidadMaxima,
                        item.GrupoReposicion ?? "", item.GrupoProveedores ?? "", item.Estado);
                if (resultados.Count == 0)
                    CapaPresentacion.DialogoSistema.Show("No se encontraron productos con ese criterio.", "Búsqueda",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                else if (resultados.Count == 500)
                    CapaPresentacion.DialogoSistema.Show("Se muestran los primeros 500 resultados. Especifica más la búsqueda para acotar la lista.",
                        "Resultados limitados", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                CapaPresentacion.DialogoSistema.Show(ex.Message + Environment.NewLine + ex.InnerException?.Message,
                    "No se pudieron buscar productos", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            txtBusqueda.Text = "";
            dgvData.Rows.Clear();
            cboBuscar.SelectedIndex = 0;
            txtBusqueda.Focus();
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
                    sincronizandoPreciosProducto = true;
                    txtCostoProducto.Text = Convert.ToDecimal(dgvData.Rows[indice].Cells["PRECIOCOMPRA"].Value).ToString("0.00");
                    txtPrecioVentaProducto.Text = Convert.ToDecimal(dgvData.Rows[indice].Cells["PRECIOVENTA"].Value).ToString("0.00");
                    decimal porcentajeVisible = Convert.ToDecimal(dgvData.Rows[indice].Cells["PORCENTAJE"].Value);
                    txtPorcentaje.Text = porcentajeVisible.ToString("0.##");
                    sincronizandoPreciosProducto = false;
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
