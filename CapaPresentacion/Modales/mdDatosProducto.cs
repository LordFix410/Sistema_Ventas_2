using CapaEntidad;
using CapaNegocio;
using CapaPresentacion.Utilidades;
using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace CapaPresentacion.Modales
{
    public partial class mdDatosProducto : Form
    {
        private string imagenOriginal;
        private string archivoImagen;
        private int rotacionImagen;
        private bool quitarImagen;

        public Func<Producto, bool> GuardarSolicitado { get; set; }
        public Func<int, bool> EliminarSolicitado { get; set; }
        public string ArchivoImagenSeleccionado => archivoImagen;
        public int RotacionImagen => rotacionImagen;
        public bool QuitarImagen => quitarImagen;
        public bool ArchivoImagenEsTemporal { get; private set; }

        public mdDatosProducto()
        {
            InitializeComponent();
            ConfigurarModal();
            FormClosed += (s, e) => LimpiarArchivoTemporal();
        }

        public int IndiceGrid => int.TryParse(txtIndice.Text, out int indice) ? indice : -1;

        public void CargarProducto(Producto producto, int indiceGrid = -1)
        {
            producto = producto ?? new Producto { Porcentaje = 61m, Estado = true };
            Tag = producto;
            CargarCatalogos(producto.IdGrupoProveedor, producto.IdGrupoReposicion);
            txtId.Text = producto.IdProducto.ToString();
            txtIndice.Text = producto.IdProducto > 0 ? indiceGrid.ToString() : "-1";

            txtCodigo.Text = producto.Codigo ?? string.Empty;
            txtNombre.Text = producto.Nombre ?? string.Empty;
            txtDescripcion.Text = producto.Descripcion ?? string.Empty;
            txtEquivalente.Text = producto.Equivalencia ?? string.Empty;
            txtCostoProducto.Text = producto.PrecioCompra.ToString("0.00", CultureInfo.CurrentCulture);
            txtPrecioVentaProducto.Text = producto.PrecioVenta.ToString("0.00", CultureInfo.CurrentCulture);
            txtPorcentaje.Text = producto.Porcentaje.ToString("0.##", CultureInfo.CurrentCulture);
            txtCantidadMinima.Text = producto.CantidadMinima.HasValue ? producto.CantidadMinima.Value.ToString() : string.Empty;
            txtCantidadMaxima.Text = producto.CantidadMaxima.HasValue ? producto.CantidadMaxima.Value.ToString() : string.Empty;
            SeleccionarCategoria(producto.oCategoria == null ? (int?)null : producto.oCategoria.IdCategoria);
            SeleccionarGrupo(cboGrupoProveedor, producto.IdGrupoProveedor);
            SeleccionarGrupo(cboGrupoReposicion, producto.IdGrupoReposicion);
            cboEstado.SelectedIndex = producto.Estado ? 0 : 1;
            txtCantidadMinima.Enabled = producto.IdGrupoReposicion == null;
            txtCantidadMaxima.Enabled = producto.IdGrupoReposicion == null;

            imagenOriginal = producto.ImagenRuta;
            archivoImagen = null;
            ArchivoImagenEsTemporal = false;
            rotacionImagen = 0;
            quitarImagen = false;
            MostrarImagen(GestorImagenProducto.CargarMiniatura(imagenOriginal, picFotoProduc.Width, picFotoProduc.Height));
            btnGirar.Enabled = !string.IsNullOrWhiteSpace(imagenOriginal);
            btnEliminar.Visible = producto.IdProducto > 0;
            Text = producto.IdProducto > 0 ? "Editar producto" : "Nuevo producto";
        }

        private void ConfigurarModal()
        {
            IconoAplicacion.Aplicar(this);
            AplicarEstiloVisual();
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            txtCodigo.MaxLength = 50;
            txtNombre.MaxLength = 500;
            txtDescripcion.MaxLength = 500;
            txtEquivalente.MaxLength = 500;
            txtCodigo.CharacterCasing = CharacterCasing.Upper;
            txtNombre.CharacterCasing = CharacterCasing.Upper;
            txtDescripcion.CharacterCasing = CharacterCasing.Upper;
            txtEquivalente.CharacterCasing = CharacterCasing.Upper;
            picFotoProduc.SizeMode = PictureBoxSizeMode.Zoom;
            picFotoProduc.BackColor = Color.White;
            btnElegir.Text = "Elegir";
            btnElegir.Click += (s, e) => ElegirImagen();
            btnGirar.Click += (s, e) => GirarImagen();
            btnQuitar.Click += (s, e) => QuitarImagenActual();
            btnGuardar.Click += Guardar_Click;
            btnCancelarUsuario.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            btnEliminar.Click += Eliminar_Click;
            cboGrupoReposicion.SelectedIndexChanged += (s, e) =>
            {
                bool independiente = cboGrupoReposicion.SelectedIndex <= 0;
                txtCantidadMinima.Enabled = independiente;
                txtCantidadMaxima.Enabled = independiente;
                if (!independiente)
                {
                    txtCantidadMinima.Clear();
                    txtCantidadMaxima.Clear();
                }
            };
            txtCostoProducto.Leave += (s, e) => ActualizarPrecioDesdeMargen();
            txtPorcentaje.Leave += (s, e) => ActualizarPrecioDesdeMargen();
            txtPrecioVentaProducto.Leave += (s, e) => ActualizarMargenDesdePrecio();
            txtCostoProducto.KeyDown += Precio_KeyDown;
            txtPorcentaje.KeyDown += Precio_KeyDown;
            txtPrecioVentaProducto.KeyDown += Precio_KeyDown;
        }

        private void CargarCatalogos(int? grupoProveedorActual, int? grupoReposicionActual)
        {
            cboCategoria.Items.Clear();
            foreach (Categoria categoria in new CN_Categoria().Listar())
                cboCategoria.Items.Add(new Opcion(categoria.IdCategoria, categoria.Descripcion));
            cboEstado.Items.Clear();
            cboEstado.Items.Add(new Opcion(1, "Activo"));
            cboEstado.Items.Add(new Opcion(0, "Inactivo"));
            CargarGrupos(cboGrupoProveedor, new CN_Reposicion().ListarPerfiles(), "IdGrupoProveedor", true, grupoProveedorActual);
            CargarGrupos(cboGrupoReposicion, new CN_Reposicion().ListarGrupos(), "IdGrupoReposicion", false, grupoReposicionActual);
            if (cboCategoria.Items.Count > 0 && cboCategoria.SelectedIndex < 0) cboCategoria.SelectedIndex = 0;
            if (cboEstado.Items.Count > 0 && cboEstado.SelectedIndex < 0) cboEstado.SelectedIndex = 0;
        }

        private static void CargarGrupos(ComboBox combo, DataTable datos, string columnaId, bool proveedor, int? idActual)
        {
            combo.Items.Clear();
            combo.Items.Add(new Opcion(null, proveedor ? "Sin perfil de proveedores" : "Sin grupo (límites propios)"));
            foreach (DataRow fila in datos.Rows)
            {
                int id = Convert.ToInt32(fila[columnaId]);
                bool activo = Convert.ToBoolean(fila["Activo"]);
                if (activo || idActual == id)
                {
                    string nombre = Convert.ToString(fila["Nombre"]);
                    if (!activo) nombre += " (INACTIVO · conservar asignación)";
                    combo.Items.Add(new Opcion(id, nombre));
                }
            }
            combo.SelectedIndex = 0;
        }

        private void SeleccionarCategoria(int? id)
        {
            for (int i = 0; i < cboCategoria.Items.Count; i++)
                if (((Opcion)cboCategoria.Items[i]).Id == id) { cboCategoria.SelectedIndex = i; return; }
            if (cboCategoria.Items.Count > 0) cboCategoria.SelectedIndex = 0;
        }

        private static void SeleccionarGrupo(ComboBox combo, int? id)
        {
            for (int i = 0; i < combo.Items.Count; i++)
                if (((Opcion)combo.Items[i]).Id == id) { combo.SelectedIndex = i; return; }
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private Producto ObtenerProducto()
        {
            Opcion categoria = cboCategoria.SelectedItem as Opcion;
            Opcion grupoProveedor = cboGrupoProveedor.SelectedItem as Opcion;
            Opcion grupoReposicion = cboGrupoReposicion.SelectedItem as Opcion;
            Opcion estado = cboEstado.SelectedItem as Opcion;
            int.TryParse(txtId.Text, out int idProducto);
            return new Producto
            {
                IdProducto = idProducto,
                Codigo = txtCodigo.Text.Trim().ToUpperInvariant(),
                Nombre = txtNombre.Text.Trim().ToUpperInvariant(),
                Descripcion = txtDescripcion.Text.Trim().ToUpperInvariant(),
                Equivalencia = string.IsNullOrWhiteSpace(txtEquivalente.Text) ? null : txtEquivalente.Text.Trim().ToUpperInvariant(),
                oCategoria = categoria == null || !categoria.Id.HasValue ? null : new Categoria { IdCategoria = categoria.Id.Value, Descripcion = categoria.Nombre },
                PrecioCompra = LeerDecimal(txtCostoProducto.Text),
                PrecioVenta = LeerDecimal(txtPrecioVentaProducto.Text),
                Porcentaje = LeerDecimal(txtPorcentaje.Text),
                CantidadMinima = grupoReposicion != null && !grupoReposicion.Id.HasValue && int.TryParse(txtCantidadMinima.Text, out int minimo) ? (int?)minimo : null,
                CantidadMaxima = grupoReposicion != null && !grupoReposicion.Id.HasValue && int.TryParse(txtCantidadMaxima.Text, out int maximo) ? (int?)maximo : null,
                IdGrupoProveedor = grupoProveedor == null ? null : grupoProveedor.Id,
                IdGrupoReposicion = grupoReposicion == null ? null : grupoReposicion.Id,
                GrupoProveedores = grupoProveedor == null ? string.Empty : grupoProveedor.Nombre,
                GrupoReposicion = grupoReposicion == null ? string.Empty : grupoReposicion.Nombre,
                Estado = estado != null && estado.Id == 1,
                ImagenRuta = quitarImagen ? null : imagenOriginal
            };
        }

        private static decimal LeerDecimal(string valor)
        {
            return decimal.TryParse(valor, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal numero) ? numero : -1m;
        }

        private void Guardar_Click(object sender, EventArgs e)
        {
            Producto producto = ObtenerProducto();
            if (string.IsNullOrWhiteSpace(producto.Codigo) || string.IsNullOrWhiteSpace(producto.Nombre) ||
                string.IsNullOrWhiteSpace(producto.Descripcion) || producto.oCategoria == null)
            {
                DialogoSistema.Show("Completa código, nombre, descripción y categoría.", "Datos del producto",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (producto.Porcentaje < 0 || producto.PrecioCompra < 0 || producto.PrecioVenta < 0)
            {
                DialogoSistema.Show("Ingresa costo, margen y precio válidos, iguales o mayores que cero.", "Precios inválidos",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (producto.PrecioCompra == 0)
            {
                if (producto.PrecioVenta <= 0 || producto.Porcentaje <= 0)
                {
                    DialogoSistema.Show("Ingresa un costo o un precio junto con un margen mayor que cero para calcular el otro valor.",
                        "Faltan datos de precio", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                producto.PrecioCompra = Math.Round(producto.PrecioVenta / (1m + producto.Porcentaje / 100m),
                    2, MidpointRounding.AwayFromZero);
                txtCostoProducto.Text = producto.PrecioCompra.ToString("0.00", CultureInfo.CurrentCulture);
                if (producto.PrecioCompra <= 0)
                {
                    DialogoSistema.Show("Con esos valores el costo calculado queda en cero. Revisa el precio y el margen.",
                        "Costo inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                producto.Porcentaje = (producto.PrecioVenta - producto.PrecioCompra) / producto.PrecioCompra * 100m;
                txtPorcentaje.Text = producto.Porcentaje.ToString("0.##", CultureInfo.CurrentCulture);
            }
            else if (producto.PrecioVenta == 0)
            {
                producto.PrecioVenta = Math.Ceiling(producto.PrecioCompra *
                    (1m + producto.Porcentaje / 100m) * 4m) / 4m;
                txtPrecioVentaProducto.Text = producto.PrecioVenta.ToString("0.00", CultureInfo.CurrentCulture);
            }
            if (producto.PrecioVenta < producto.PrecioCompra)
            {
                DialogoSistema.Show("El precio de venta no puede ser menor que el costo.", "Margen inválido",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrecioVentaProducto.Focus();
                txtPrecioVentaProducto.SelectAll();
                return;
            }
            if (Math.Abs(producto.PrecioVenta * 4m - decimal.Round(producto.PrecioVenta * 4m, 0)) > 0.0001m)
            {
                DialogoSistema.Show("El precio de venta debe terminar en .00, .25, .50 o .75.", "Precio inválido",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrecioVentaProducto.Focus();
                txtPrecioVentaProducto.SelectAll();
                return;
            }
            if (!producto.IdGrupoReposicion.HasValue &&
                (!producto.CantidadMinima.HasValue || !producto.CantidadMaxima.HasValue ||
                 producto.CantidadMinima.Value < 0 || producto.CantidadMaxima.Value < producto.CantidadMinima.Value))
            {
                DialogoSistema.Show("Sin grupo de productos, ingresa mínimo y máximo válidos. El máximo debe ser igual o mayor que el mínimo.",
                    "Límites de reposición", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCantidadMinima.Focus();
                return;
            }
            if (GuardarSolicitado == null)
            {
                DialogoSistema.Show("No se pudo conectar el guardado del producto.", "Guardar producto",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (GuardarSolicitado(producto))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void Eliminar_Click(object sender, EventArgs e)
        {
            if (EliminarSolicitado != null && EliminarSolicitado(Tag is Producto p ? p.IdProducto : 0))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void ElegirImagen()
        {
            using (var dialogo = new OpenFileDialog { Title = "Seleccionar foto del producto", Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp", CheckFileExists = true })
            {
                if (dialogo.ShowDialog(this) != DialogResult.OK) return;
                Image previa = GestorImagenProducto.CargarArchivoMiniatura(dialogo.FileName, picFotoProduc.Width, picFotoProduc.Height);
                if (previa == null)
                {
                    DialogoSistema.Show("No se pudo abrir la imagen seleccionada.", "Foto inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                LimpiarArchivoTemporal();
                archivoImagen = dialogo.FileName;
                ArchivoImagenEsTemporal = false;
                rotacionImagen = 0;
                quitarImagen = false;
                btnGirar.Enabled = true;
                MostrarImagen(previa);
            }
        }

        private void GirarImagen()
        {
            string archivo = archivoImagen;
            if (string.IsNullOrWhiteSpace(archivo))
            {
                if (string.IsNullOrWhiteSpace(imagenOriginal)) return;
                try { archivoImagen = GestorImagenProducto.CrearCopiaTemporal(imagenOriginal); }
                catch (Exception ex) { DialogoSistema.Show(ex.Message, "Foto del producto", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                archivo = archivoImagen;
                ArchivoImagenEsTemporal = true;
            }
            rotacionImagen = (rotacionImagen + 90) % 360;
            MostrarImagen(GestorImagenProducto.CargarArchivoMiniatura(archivo, picFotoProduc.Width, picFotoProduc.Height, rotacionImagen));
        }

        private void QuitarImagenActual()
        {
            LimpiarArchivoTemporal();
            archivoImagen = null;
            ArchivoImagenEsTemporal = false;
            rotacionImagen = 0;
            quitarImagen = true;
            btnGirar.Enabled = false;
            MostrarImagen(null);
        }

        private void AplicarEstiloVisual()
        {
            BackColor = Color.FromArgb(243, 247, 251);
            Font fuenteBase = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            Font fuenteTitulo = new Font("Segoe UI", 10F, FontStyle.Bold);

            foreach (GroupBox seccion in new[] { gbxDatosBasicos, gbxImagen, gbxPrecio, gbxReposicion })
            {
                seccion.Font = fuenteTitulo;
                seccion.ForeColor = Color.FromArgb(34, 69, 98);
                seccion.BackColor = Color.White;
            }
            gbxDatosBasicos.BackColor = Color.FromArgb(239, 246, 252);
            gbxImagen.BackColor = Color.FromArgb(246, 249, 252);
            gbxPrecio.BackColor = Color.FromArgb(239, 248, 242);
            gbxReposicion.BackColor = Color.FromArgb(255, 249, 235);

            foreach (Label etiqueta in new[] { label1, label2, label3, label4, label5, label6,
                label7, label8, label13, label14, label15, label16, label17, label18, label19, labelPorcentaje })
            {
                etiqueta.Font = fuenteTitulo;
                etiqueta.ForeColor = Color.FromArgb(40, 57, 73);
                etiqueta.BackColor = Color.Transparent;
            }

            foreach (TextBox campo in new[] { txtCodigo, txtNombre, txtDescripcion, txtEquivalente,
                txtCostoProducto, txtPorcentaje, txtPrecioVentaProducto, txtCantidadMinima, txtCantidadMaxima })
            {
                campo.Font = fuenteBase;
                campo.BackColor = Color.White;
                campo.BorderStyle = BorderStyle.FixedSingle;
            }
            foreach (ComboBox lista in new[] { cboCategoria, cboEstado, cboGrupoProveedor, cboGrupoReposicion })
                lista.Font = fuenteBase;

            picFotoProduc.BorderStyle = BorderStyle.FixedSingle;
            picFotoProduc.BackColor = Color.White;
            EstilizarBoton(btnElegir, Color.FromArgb(31, 111, 168), Color.White);
            EstilizarBoton(btnQuitar, Color.FromArgb(192, 57, 57), Color.White);
            EstilizarBoton(btnGirar, Color.White, Color.FromArgb(31, 95, 140));
            EstilizarBoton(btnGuardar, Color.FromArgb(34, 139, 84), Color.White);
            EstilizarBoton(btnCancelarUsuario, Color.FromArgb(235, 171, 0), Color.FromArgb(35, 45, 55));
            EstilizarBoton(btnEliminar, Color.FromArgb(192, 57, 57), Color.White);
        }

        private static void EstilizarBoton(Button boton, Color fondo, Color texto)
        {
            boton.UseVisualStyleBackColor = false;
            boton.BackColor = fondo;
            boton.ForeColor = texto;
            boton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            boton.FlatStyle = FlatStyle.Flat;
            boton.FlatAppearance.BorderSize = 0;
            boton.Cursor = Cursors.Hand;
        }

        private static void EstilizarBoton(FontAwesome.Sharp.IconButton boton, Color fondo, Color texto)
        {
            boton.UseVisualStyleBackColor = false;
            boton.BackColor = fondo;
            boton.ForeColor = texto;
            boton.IconColor = texto;
            boton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            boton.FlatStyle = FlatStyle.Flat;
            boton.FlatAppearance.BorderSize = 0;
            boton.Cursor = Cursors.Hand;
        }

        private void LimpiarArchivoTemporal()
        {
            if (ArchivoImagenEsTemporal && !string.IsNullOrWhiteSpace(archivoImagen))
                try { File.Delete(archivoImagen); } catch { }
            ArchivoImagenEsTemporal = false;
        }

        private void ActualizarPrecioDesdeMargen()
        {
            if (!LeerPrecios(out decimal costo, out decimal margen, out decimal precio)) return;
            if (costo > 0)
                EscribirPrecioVenta(Math.Ceiling(costo * (1m + margen / 100m) * 4m) / 4m);
            else if (costo == 0 && precio > 0 && margen > 0)
                CalcularCostoDesdeVentaYMargen(precio, margen);
        }

        private void ActualizarMargenDesdePrecio()
        {
            if (!LeerPrecios(out decimal costo, out decimal margen, out decimal precio) || precio <= 0) return;
            if (costo == 0)
            {
                if (margen > 0) CalcularCostoDesdeVentaYMargen(precio, margen);
                return;
            }
            txtPorcentaje.Text = ((precio - costo) / costo * 100m).ToString("0.##", CultureInfo.CurrentCulture);
        }

        private void Precio_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            if (!LeerPrecios(out decimal costo, out decimal margen, out decimal precio))
            {
                DialogoSistema.Show("Ingresa costo, margen y precio válidos (iguales o mayores que cero).", "Precios inválidos",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (sender == txtPrecioVentaProducto)
            {
                if (costo == 0)
                {
                    if (margen <= 0)
                    {
                        DialogoSistema.Show("Para calcular el costo, ingresa un margen mayor que cero.", "Falta el margen",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtPorcentaje.Focus();
                        txtPorcentaje.SelectAll();
                        return;
                    }
                    if (!CalcularCostoDesdeVentaYMargen(precio, margen)) return;
                }
                else if (precio > 0)
                    ActualizarMargenDesdePrecio();
            }
            else if (costo == 0)
            {
                if (precio > 0 && margen > 0) CalcularCostoDesdeVentaYMargen(precio, margen);
            }
            else
                EscribirPrecioVenta(Math.Ceiling(costo * (1m + margen / 100m) * 4m) / 4m);

            if (sender == txtCostoProducto) txtPorcentaje.Focus();
            else if (sender == txtPorcentaje) txtPrecioVentaProducto.Focus();
            else cboGrupoProveedor.Focus();
        }

        private bool LeerPrecios(out decimal costo, out decimal margen, out decimal precio)
        {
            bool costoValido = decimal.TryParse(txtCostoProducto.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out costo);
            bool margenValido = decimal.TryParse(txtPorcentaje.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out margen);
            bool precioValido = decimal.TryParse(txtPrecioVentaProducto.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out precio);
            return costoValido && costo >= 0 && margenValido && margen >= 0 && precioValido && precio >= 0;
        }

        private bool CalcularCostoDesdeVentaYMargen(decimal precio, decimal margen)
        {
            if (precio <= 0 || margen <= 0) return false;
            decimal costo = Math.Round(precio / (1m + margen / 100m), 2, MidpointRounding.AwayFromZero);
            if (costo <= 0)
            {
                DialogoSistema.Show("Con esos valores el costo calculado queda en cero. Revisa el precio y el margen.",
                    "Costo inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            txtCostoProducto.Text = costo.ToString("0.00", CultureInfo.CurrentCulture);
            txtPorcentaje.Text = ((precio - costo) / costo * 100m).ToString("0.##", CultureInfo.CurrentCulture);
            return true;
        }

        private void EscribirPrecioVenta(decimal precio)
        {
            txtPrecioVentaProducto.Text = precio.ToString("0.00", CultureInfo.CurrentCulture);
        }

        private void MostrarImagen(Image imagen)
        {
            Image anterior = picFotoProduc.Image;
            picFotoProduc.Image = imagen;
            if (anterior != null) anterior.Dispose();
        }

        private sealed class Opcion
        {
            public int? Id { get; private set; }
            public string Nombre { get; private set; }
            public Opcion(int? id, string nombre) { Id = id; Nombre = nombre; }
            public override string ToString() => Nombre;
        }

        private void label1_Click(object sender, EventArgs e) { }
        private void label8_Click(object sender, EventArgs e) { }

        private void btnQuitar_Click(object sender, EventArgs e)
        {

        }
    }
}
