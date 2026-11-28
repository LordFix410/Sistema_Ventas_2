using CapaEntidad;
using CapaNegocio;
using CapaPresentacion.Modales;
using CapaPresentacion.Utilidades;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CapaPresentacion
{
    public partial class frmProducto : Form
    {
        private readonly string claveUsuario;
        private const int MargenDerechoContenido = 17;
        private const int MargenInferiorContenido = 17;

        public frmProducto() : this(string.Empty) { }

        public frmProducto(string clave)
        {
            claveUsuario = clave ?? string.Empty;
            InitializeComponent();
            ConfigurarFotoProducto();
            ConfigurarOrdenColumnas();
            btnCrearProducto.Click += (s, e) => AbrirModalDatosProducto(new Producto { Porcentaje = 61m, Estado = true });
            dgvData.CellDoubleClick += dgvData_CellDoubleClick;
            dgvData.CellClick += dgvData_CellClick;
            txtBusqueda.KeyDown += txtBusqueda_KeyDown;
            checkBox1.CheckedChanged += (s, e) => AplicarModoVista();
            dgvData.RowHeadersVisible = false;
            dgvData.RowTemplate.Height = 88;
            dgvData.ColumnHeadersHeight = 52;
            dgvData.DefaultCellStyle.Font = new Font("Segoe UI", 10F);
            dgvData.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnCrearProducto.Text = "Crear producto";
            btnCrearProducto.Width = 164;
            btnCrearProducto.BackColor = Color.FromArgb(31, 111, 168);
            btnCrearProducto.ForeColor = Color.White;
            btnCrearProducto.IconColor = Color.White;
            btnCrearProducto.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnCrearProducto.TextAlign = ContentAlignment.MiddleCenter;
            btnCrearProducto.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnCrearProducto.FlatStyle = FlatStyle.Flat;
            btnCrearProducto.FlatAppearance.BorderSize = 0;
            btnCrearProducto.UseVisualStyleBackColor = false;
            Resize += (s, e) => AjustarDisenoAdaptable();
            AjustarDisenoAdaptable();
        }

        private void AjustarDisenoAdaptable()
        {
            if (IsDisposed || dgvData == null) return;
            // Respeta las coordenadas del diseñador; solo adapta el tamaño al formulario.
            label10.Width = Math.Max(250, ClientSize.Width - label10.Left - MargenDerechoContenido);
            dgvData.Width = Math.Max(250, ClientSize.Width - dgvData.Left - MargenDerechoContenido);
            dgvData.Height = Math.Max(180, ClientSize.Height - dgvData.Top - MargenInferiorContenido);
        }

        private void ConfigurarFotoProducto()
        {
            if (dgvData.Columns.Contains("Foto")) return;
            var foto = new DataGridViewImageColumn
            {
                Name = "Foto",
                HeaderText = "FOTO",
                ImageLayout = DataGridViewImageCellLayout.Zoom,
                Width = 88,
                MinimumWidth = 60,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            dgvData.Columns.Insert(1, foto);
        }

        private void ConfigurarOrdenColumnas()
        {
            string[] orden = { "Foto", "Codigo", "Equivalencia", "Nombre", "Descripcion", "Categoria", "Stock",
                "CantidadMinima", "CantidadMaxima", "PRECIOCOMPRA", "PRECIOVENTA", "PORCENTAJE",
                "GrupoReposicion", "GrupoProveedores", "Estado" };
            for (int i = 0; i < orden.Length; i++)
                if (dgvData.Columns.Contains(orden[i])) dgvData.Columns[orden[i]].DisplayIndex = i;
        }

        private int AgregarFilaProducto(Producto producto)
        {
            int indice = dgvData.Rows.Add();
            EscribirFilaProducto(dgvData.Rows[indice], producto);
            return indice;
        }

        private void EscribirFilaProducto(DataGridViewRow fila, Producto producto)
        {
            fila.Cells["IdProducto"].Value = producto.IdProducto;
            fila.Cells["Codigo"].Value = producto.Codigo;
            fila.Cells["Equivalencia"].Value = producto.Equivalencia ?? string.Empty;
            fila.Cells["Nombre"].Value = producto.Nombre;
            fila.Cells["Descripcion"].Value = producto.Descripcion;
            fila.Cells["IDCATEGORIA"].Value = producto.oCategoria.IdCategoria;
            fila.Cells["Categoria"].Value = producto.oCategoria.Descripcion;
            fila.Cells["Stock"].Value = producto.Stock;
            fila.Cells["PRECIOCOMPRA"].Value = producto.PrecioCompra;
            fila.Cells["PRECIOVENTA"].Value = producto.PrecioVenta;
            fila.Cells["PORCENTAJE"].Value = FormatoPorcentaje.ParaMostrar(producto.Porcentaje, producto.PrecioCompra);
            fila.Cells["GrupoReposicion"].Value = producto.GrupoReposicion ?? string.Empty;
            fila.Cells["CantidadMinima"].Value = producto.CantidadMinima?.ToString() ?? string.Empty;
            fila.Cells["CantidadMaxima"].Value = producto.CantidadMaxima?.ToString() ?? string.Empty;
            fila.Cells["GrupoProveedores"].Value = producto.GrupoProveedores ?? string.Empty;
            fila.Cells["EstadoValor"].Value = producto.Estado ? 1 : 0;
            fila.Cells["Estado"].Value = producto.Estado ? "Activo" : "Inactivo";

            Image anterior = fila.Cells["Foto"].Value as Image;
            fila.Cells["Foto"].Value = null;
            if (anterior != null) anterior.Dispose();
            fila.Cells["Foto"].Value = GestorImagenProducto.CargarMiniatura(producto.ImagenRuta, 76, 76);
            fila.Tag = producto;
        }

        private void frmProducto_Load(object sender, EventArgs e)
        {
            cboBuscar.Items.Clear();
            foreach (DataGridViewColumn columna in dgvData.Columns)
            {
                if (columna.Visible && columna.Name != "Foto" &&
                    !string.IsNullOrWhiteSpace(columna.HeaderText))
                    cboBuscar.Items.Add(new OpcionCombo { Valor = columna.Name, Texto = columna.HeaderText });
            }
            cboBuscar.DisplayMember = "Texto";
            cboBuscar.ValueMember = "Valor";
            int indiceNombre = cboBuscar.Items.Cast<OpcionCombo>().ToList()
                .FindIndex(opcion => string.Equals(Convert.ToString(opcion.Valor), "Nombre", StringComparison.OrdinalIgnoreCase));
            if (cboBuscar.Items.Count > 0) cboBuscar.SelectedIndex = indiceNombre >= 0 ? indiceNombre : 0;
            checkBox1.Text = "Vista para cliente";
            checkBox1.Checked = true;

            dgvData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvData.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvData.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvData.RowTemplate.Height = 88;
            var anchos = new Dictionary<string, int>
            {
                ["Codigo"] = 85, ["Equivalencia"] = 120, ["Nombre"] = 145, ["Descripcion"] = 160, ["Categoria"] = 105,
                ["Stock"] = 60, ["CantidadMinima"] = 72, ["CantidadMaxima"] = 72,
                ["PRECIOCOMPRA"] = 85, ["PRECIOVENTA"] = 85, ["PORCENTAJE"] = 80,
                ["GrupoReposicion"] = 125, ["GrupoProveedores"] = 125, ["Estado"] = 70
            };
            if (dgvData.Columns.Contains("CantidadMinima")) dgvData.Columns["CantidadMinima"].HeaderText = "MIN";
            if (dgvData.Columns.Contains("CantidadMaxima")) dgvData.Columns["CantidadMaxima"].HeaderText = "MAX";
            if (dgvData.Columns.Contains("PORCENTAJE"))
            {
                dgvData.Columns["PORCENTAJE"].HeaderText = "%";
                dgvData.Columns["PORCENTAJE"].DefaultCellStyle.Format = "0.##'%'";
            }
            foreach (var ancho in anchos)
                if (dgvData.Columns.Contains(ancho.Key)) dgvData.Columns[ancho.Key].Width = ancho.Value;
            if (dgvData.Columns.Contains("Foto")) dgvData.Columns["Foto"].Width = 88;
            if (dgvData.Columns.Contains("Foto"))
                dgvData.Columns["Foto"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            foreach (DataGridViewColumn columna in dgvData.Columns)
                if (columna.Visible && columna.Name != "Foto")
                {
                    columna.FillWeight = Math.Max(25, columna.Width);
                    columna.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }
            AplicarModoVista();
        }

        private void AplicarModoVista()
        {
            bool vistaCliente = checkBox1.Checked;
            foreach (string columna in new[] { "PRECIOCOMPRA", "PORCENTAJE", "GrupoProveedores",
                "GrupoReposicion", "CantidadMinima", "CantidadMaxima" })
                if (dgvData.Columns.Contains(columna)) dgvData.Columns[columna].Visible = !vistaCliente;
        }

        private void AbrirModalDatosProducto(Producto datos)
        {
            using (var modal = new mdDatosProducto())
            {
                modal.CargarProducto(datos);
                modal.GuardarSolicitado = producto => GuardarDesdeModal(producto, modal);
                modal.EliminarSolicitado = EliminarDesdeModal;
                modal.ShowDialog(this);
            }
        }

        private int BuscarIndiceFilaPorId(int idProducto)
        {
            if (idProducto <= 0) return -1;
            foreach (DataGridViewRow fila in dgvData.Rows)
                if (int.TryParse(Convert.ToString(fila.Cells["IdProducto"].Value), out int idFila) && idFila == idProducto)
                    return fila.Index;
            return -1;
        }

        private bool GuardarDesdeModal(Producto producto, mdDatosProducto modal)
        {
            bool esNuevo = producto.IdProducto <= 0;
            int indice = producto.IdProducto > 0 ? BuscarIndiceFilaPorId(producto.IdProducto) : -1;
            if (producto.IdProducto > 0 && indice < 0)
            {
                DialogoSistema.Show("Ese producto ya no está en la lista actual. Vuelve a buscarlo antes de editarlo.",
                    "Producto fuera de la lista", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            Producto anterior = indice >= 0 ? dgvData.Rows[indice].Tag as Producto : null;
            producto.Stock = anterior == null ? 0 : anterior.Stock;
            string imagenAnterior = anterior == null ? producto.ImagenRuta : anterior.ImagenRuta;
            bool imagenNueva = false;
            string mensaje;
            try
            {
                if (modal.QuitarImagen)
                    producto.ImagenRuta = null;
                else if (!string.IsNullOrWhiteSpace(modal.ArchivoImagenSeleccionado))
                {
                    producto.ImagenRuta = GestorImagenProducto.Guardar(
                        modal.ArchivoImagenSeleccionado, claveUsuario, modal.RotacionImagen);
                    imagenNueva = true;
                    if (string.IsNullOrWhiteSpace(producto.ImagenRuta))
                        throw new InvalidOperationException("El servicio no devolvió la ruta de la imagen guardada.");
                }
            }
            catch (Exception ex)
            {
                DialogoSistema.Show("No se pudo guardar la foto. Verifica que el servicio esté activo y accesible desde esta PC.\n\n" + ex.Message,
                    "Foto del producto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            bool guardado;
            if (producto.IdProducto == 0)
            {
                int id = new CN_Producto().Registrar(producto, out mensaje);
                guardado = id > 0;
                if (guardado) producto.IdProducto = id;
            }
            else
                guardado = new CN_Producto().Editar(producto, out mensaje);

            if (!guardado)
            {
                if (imagenNueva) EliminarImagen(producto.ImagenRuta);
                DialogoSistema.Show(string.IsNullOrWhiteSpace(mensaje) ? "No se pudo guardar el producto." : mensaje,
                    producto.IdProducto == 0 ? "Error al registrar" : "No se pudo actualizar el producto",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            producto.Porcentaje = PorcentajeGuardado(producto.PrecioCompra, producto.PrecioVenta, producto.Porcentaje);
            if (indice < 0)
                AgregarFilaProducto(producto);
            else
                EscribirFilaProducto(dgvData.Rows[indice], producto);

            if (!string.IsNullOrWhiteSpace(imagenAnterior) &&
                !string.Equals(imagenAnterior, producto.ImagenRuta, StringComparison.OrdinalIgnoreCase))
                EliminarImagen(imagenAnterior);

            DialogoSistema.ShowSuccess(esNuevo
                ? "Producto registrado correctamente."
                : "Producto actualizado correctamente.");
            return true;
        }

        private bool EliminarDesdeModal(int idProducto)
        {
            int indice = BuscarIndiceFilaPorId(idProducto);
            if (indice < 0)
            {
                DialogoSistema.Show("El producto ya no está en la lista. Vuelve a buscarlo antes de eliminarlo.",
                    "Producto fuera de la lista", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (DialogoSistema.Show("¿Desea eliminar el producto seleccionado?", "Confirmar eliminación",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
                return false;

            Producto producto = dgvData.Rows[indice].Tag as Producto;
            if (producto == null) return false;
            bool respuesta = new CN_Producto().Eliminar(new Producto { IdProducto = idProducto }, out string mensaje);
            if (!respuesta)
            {
                DialogoSistema.Show(mensaje, "No se pudo eliminar el producto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            Image miniatura = dgvData.Rows[indice].Cells["Foto"].Value as Image;
            dgvData.Rows.RemoveAt(indice);
            if (miniatura != null) miniatura.Dispose();
            EliminarImagen(producto.ImagenRuta);
            DialogoSistema.ShowSuccess("Producto eliminado correctamente.");
            return true;
        }

        private void EliminarImagen(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta)) return;
            try { GestorImagenProducto.Eliminar(ruta, claveUsuario); } catch { }
        }

        private static decimal PorcentajeGuardado(decimal costo, decimal venta, decimal porcentajeSinCosto)
        {
            return costo <= 0 ? porcentajeSinCosto :
                decimal.Ceiling(((venta - costo) / costo) * 10000m) / 10000m;
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            string termino = txtBusqueda.Text.Trim();
            if (termino.Length == 0 && DialogoSistema.Show(this,
                "El campo está vacío y se consultará toda la lista de productos. Si el inventario es grande, la carga puede tardar. ¿Deseas continuar?",
                "Confirmar carga completa", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            try
            {
                if (!(cboBuscar.SelectedItem is OpcionCombo seleccion)) return;
                string columna = Convert.ToString(seleccion.Valor);
                List<Producto> resultados = termino.Length == 0
                    ? new CN_Producto().Listar()
                    : new CN_Producto().Buscar(columna, termino);
                LimpiarFilasProductos();
                foreach (Producto item in resultados) AgregarFilaProducto(item);
                if (resultados.Count == 0)
                    DialogoSistema.Show("No se encontraron productos con ese criterio.", "Búsqueda",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                else if (resultados.Count == 500)
                    DialogoSistema.Show("Se muestran los primeros 500 resultados. Especifica más la búsqueda para acotar la lista.",
                        "Resultados limitados", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                DialogoSistema.Show(ex.Message + Environment.NewLine + ex.InnerException?.Message,
                    "No se pudieron buscar productos", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            txtBusqueda.Clear();
            LimpiarFilasProductos();
            if (cboBuscar.Items.Count > 0) cboBuscar.SelectedIndex = 0;
            txtBusqueda.Focus();
        }

        private void txtBusqueda_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            e.Handled = true;
            btnBuscar_Click(sender, EventArgs.Empty);
        }

        private void dgvData_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            Producto producto = dgvData.Rows[e.RowIndex].Tag as Producto;
            if (producto != null) AbrirModalDatosProducto(producto);
        }

        private void dgvData_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
                !string.Equals(dgvData.Columns[e.ColumnIndex].Name, "Foto", StringComparison.Ordinal))
                return;

            Producto producto = dgvData.Rows[e.RowIndex].Tag as Producto;
            if (producto != null && !string.IsNullOrWhiteSpace(producto.ImagenRuta))
                GestorImagenProducto.MostrarVistaPrevia(this, producto.ImagenRuta, producto.Nombre);
        }

        private void LimpiarFilasProductos()
        {
            foreach (DataGridViewRow fila in dgvData.Rows)
                if (fila.Cells["Foto"].Value is Image imagen) imagen.Dispose();
            dgvData.Rows.Clear();
        }

        private void iconButton1_Click(object sender, EventArgs e)
        {
            if (dgvData.Rows.Count < 1)
            {
                DialogoSistema.Show("No hay datos para exportar", "Mensaje del sistema", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataTable datos = new DataTable();
            List<DataGridViewColumn> columnas = dgvData.Columns.Cast<DataGridViewColumn>()
                .Where(c => c.Visible && c.HeaderText != "" && c.Name != "Foto").ToList();
            foreach (DataGridViewColumn columna in columnas) datos.Columns.Add(columna.HeaderText, typeof(string));
            foreach (DataGridViewRow fila in dgvData.Rows)
                if (fila.Visible) datos.Rows.Add(columnas.Select(c => Convert.ToString(fila.Cells[c.Name].Value)).ToArray());

            using (var dialogo = new SaveFileDialog
            {
                FileName = string.Format("ReporteProducto_{0}.xlsx", DateTime.Now.ToString("ddMMyyyyHHmmss")),
                Filter = "Excel Files | *.xlsx"
            })
            {
                if (dialogo.ShowDialog() != DialogResult.OK) return;
                try
                {
                    using (var libro = new XLWorkbook())
                    {
                        var hoja = libro.Worksheets.Add(datos, "Informe");
                        hoja.ColumnsUsed().AdjustToContents();
                        libro.SaveAs(dialogo.FileName);
                    }
                    DialogoSistema.ShowSuccess("Reporte generado.", "Productos");
                }
                catch (Exception ex)
                {
                    DialogoSistema.Show(ex.Message, "Error al exportar", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private sealed class OpcionCombo
        {
            public object Valor { get; set; }
            public object Texto { get; set; }
            public override string ToString() => Convert.ToString(Texto);
        }
    }
}
