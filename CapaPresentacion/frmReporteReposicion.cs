using CapaNegocio;
using ClosedXML.Excel;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CapaPresentacion
{
    public partial class frmReporteReposicion : Form
    {
        private readonly CN_Reposicion negocio = new CN_Reposicion();
        private DataTable reporte;

        public frmReporteReposicion()
        {
            InitializeComponent();
            CapaPresentacion.Utilidades.ExperienciaUsuario.Aplicar(this, cboProveedorCotizacion);
            cboProveedorCotizacion.DropDownStyle = ComboBoxStyle.DropDownList;
            btnConsultarCotizacion.Click += (s, e) => CargarReporte();
            btnExportarExcel.Click += (s, e) => Exportar();
            Load += (s, e) => { try { CargarProveedores(); CargarReporte(); } catch (Exception ex) { Error(ex); } };
            cboProveedorCotizacion.SelectedIndexChanged += (s, e) => MostrarFiltrado();
            dgvCotizacion.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvCotizacion.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            dgvCotizacion.ReadOnly = true; dgvCotizacion.AllowUserToAddRows = false; dgvCotizacion.RowHeadersVisible = false;
            dgvCotizacion.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvCotizacion.Size = new System.Drawing.Size(Math.Max(240, ClientSize.Width - dgvCotizacion.Left - 20), Math.Max(150, ClientSize.Height - dgvCotizacion.Top - 20));
        }

        private void CargarProveedores()
        {
            cboProveedorCotizacion.Items.Clear(); cboProveedorCotizacion.Items.Add(new OpcionProveedor(null, "Todos los proveedores"));
            DataTable tabla = negocio.ListarProveedores();
            foreach (DataRow r in tabla.Rows) cboProveedorCotizacion.Items.Add(new OpcionProveedor(Convert.ToInt32(r["IdProveedor"]), Convert.ToString(r["RazonSocial"])));
            cboProveedorCotizacion.SelectedIndex = 0;
        }

        private void CargarReporte()
        {
            reporte = negocio.Reporte();
            if (!reporte.Columns.Contains("RangoObjetivo"))
                reporte.Columns.Add("RangoObjetivo", typeof(string));
            foreach (DataRow fila in reporte.Rows)
                fila["RangoObjetivo"] = Convert.ToString(fila["CantidadMinima"]) + " – " + Convert.ToString(fila["CantidadMaxima"]);
            MostrarFiltrado();
        }

        private void MostrarFiltrado()
        {
            if (reporte == null) return;
            OpcionProveedor op = cboProveedorCotizacion.SelectedItem as OpcionProveedor;
            if (op == null || !op.Id.HasValue) dgvCotizacion.DataSource = reporte;
            else
            {
                DataView vista = new DataView(reporte) { RowFilter = "IdProveedor = " + op.Id.Value };
                dgvCotizacion.DataSource = vista;
            }
            foreach (string n in new[] { "IdGrupoReposicion", "IdGrupoProveedor", "IdProveedor", "IdProducto",
                "CantidadMinima", "CantidadMaxima", "FaltanteReferencia" })
                if (dgvCotizacion.Columns.Contains(n)) dgvCotizacion.Columns[n].Visible = false;
            Encabezado("GrupoReposicion", "Grupo de reposición"); Encabezado("GrupoProveedores", "Perfil proveedor");
            Encabezado("Proveedor", "Proveedor"); Encabezado("Codigo", "Código"); Encabezado("Producto", "Producto");
            Encabezado("StockActual", "Stock actual"); Encabezado("RangoObjetivo", "Rango objetivo (mínimo–máximo)");
            Encabezado("UltimoPrecioConEsteProveedor", "Último precio proveedor");
            Encabezado("FechaUltimaCompraConEsteProveedor", "Fecha última compra"); Encabezado("UltimoPrecioHistorico", "Último precio histórico");
            Encabezado("UltimoProveedorHistorico", "Proveedor histórico"); Encabezado("FechaUltimoPrecioHistorico", "Fecha precio histórico");
            foreach (string n in new[] { "UltimoPrecioConEsteProveedor", "UltimoPrecioHistorico" })
                if (dgvCotizacion.Columns.Contains(n)) dgvCotizacion.Columns[n].DefaultCellStyle.Format = "Q #,##0.00";
        }

        private void Exportar()
        {
            if (reporte == null) { Mensaje("Primero consulta el reporte.", MessageBoxIcon.Information); return; }
            OpcionProveedor op = cboProveedorCotizacion.SelectedItem as OpcionProveedor;
            DataView view = new DataView(reporte);
            if (op != null && op.Id.HasValue) view.RowFilter = "IdProveedor = " + op.Id.Value;
            if (view.Count == 0) { Mensaje("No hay productos pendientes asignados a este proveedor.", MessageBoxIcon.Information); return; }
            DataTable filas = view.ToTable();
            var proveedores = filas.AsEnumerable().GroupBy(r => Convert.ToInt32(r["IdProveedor"]))
                .OrderBy(g => Convert.ToString(g.First()["Proveedor"]));
            string nombreArchivo = op != null && op.Id.HasValue ? op.Nombre : "Todos_los_proveedores";
            foreach (char c in Path.GetInvalidFileNameChars()) nombreArchivo = nombreArchivo.Replace(c, '_');
            using (SaveFileDialog dialogo = new SaveFileDialog { Filter = "Libro de Excel (*.xlsx)|*.xlsx", FileName = "Pedidos_reposicion_" + nombreArchivo + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xlsx" })
            {
                if (dialogo.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    using (XLWorkbook libro = new XLWorkbook())
                    {
                        var hoja = libro.Worksheets.Add("Pedidos");
                        hoja.Column(1).Width = 18; hoja.Column(2).Width = 42; hoja.Column(3).Width = 25;
                        hoja.Column(4).Width = 24; hoja.Column(5).Width = 38;
                        string[] encabezados = { "Código", "Producto", "Rango objetivo (mínimo–máximo)", "Productos a pedir", "Último proveedor histórico" };
                        int fila = 1;
                        foreach (var grupo in proveedores)
                        {
                            DataRow primero = grupo.First();
                            var titulo = hoja.Range(fila, 1, fila, encabezados.Length);
                            titulo.Merge(); titulo.Value = "PROVEEDOR: " + Convert.ToString(primero["Proveedor"]);
                            titulo.Style.Font.Bold = true; titulo.Style.Font.FontSize = 13;
                            titulo.Style.Fill.BackgroundColor = XLColor.FromHtml("#2E5B79"); titulo.Style.Font.FontColor = XLColor.White;
                            titulo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center; hoja.Row(fila).Height = 25; fila++;
                            for (int col = 0; col < encabezados.Length; col++) hoja.Cell(fila, col + 1).Value = encabezados[col];
                            hoja.Range(fila, 1, fila, encabezados.Length).Style.Font.Bold = true;
                            hoja.Range(fila, 1, fila, encabezados.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#D9EAF5");
                            hoja.Range(fila, 1, fila, encabezados.Length).Style.Alignment.WrapText = true;
                            fila++;
                            foreach (DataRow r in grupo.OrderBy(x => Convert.ToString(x["Producto"])))
                            {
                                hoja.Cell(fila, 1).Value = Convert.ToString(r["Codigo"]);
                                hoja.Cell(fila, 2).Value = Convert.ToString(r["Producto"]);
                                hoja.Cell(fila, 3).Value = Convert.ToString(r["CantidadMinima"]) + " – " + Convert.ToString(r["CantidadMaxima"]);
                                hoja.Cell(fila, 4).Value = "";
                                hoja.Cell(fila, 5).Value = r.IsNull("UltimoProveedorHistorico") ? "Sin compra registrada" : Convert.ToString(r["UltimoProveedorHistorico"]);
                                hoja.Row(fila).Height = 23;
                                fila++;
                            }
                            fila++;
                        }
                        hoja.SheetView.FreezeRows(2);
                        hoja.RangeUsed().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        hoja.PageSetup.PageOrientation = XLPageOrientation.Landscape;
                        hoja.PageSetup.PagesWide = 1;
                        hoja.PageSetup.Margins.Top = 0.4; hoja.PageSetup.Margins.Bottom = 0.4;
                        libro.SaveAs(dialogo.FileName);
                    }
                    Mensaje("Se exportó la lista agrupada por proveedor. La columna ‘Productos a pedir’ quedó libre para completar las cantidades.", MessageBoxIcon.Information);
                }
                catch (Exception ex) { Error(ex); }
            }
        }
        private void Encabezado(string col, string texto) { if (dgvCotizacion.Columns.Contains(col)) dgvCotizacion.Columns[col].HeaderText = texto; }
        private void Error(Exception ex) { DialogoSistema.Show("No se pudo consultar. Confirma que esté aplicada la migración de reposición.\n\n" + ex.Message, "Reporte de reposición", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        private void Mensaje(string texto, MessageBoxIcon icono) { DialogoSistema.Show(texto, "Reporte de reposición", MessageBoxButtons.OK, icono); }
        private sealed class OpcionProveedor
        {
            public int? Id { get; private set; } public string Nombre { get; private set; }
            public OpcionProveedor(int? id, string nombre) { Id = id; Nombre = nombre; }
            public override string ToString() { return Nombre; }
        }
    }
}
