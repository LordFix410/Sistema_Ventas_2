using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CapaEntidad;
using CapaNegocio;
using CapaPresentacion.Modales;
using FontAwesome.Sharp;

namespace CapaPresentacion
{
    public partial class Inicio : Form
    {
        private Usuario usuarioActual;
        private IconMenuItem botonActual = null;
        private Form FormularioActivo = null;
        private frmCompraRegistrar formularioCompraEnCurso;
        private readonly ToolStripMenuItem gruposProveedoresMenu = new ToolStripMenuItem("Perfiles de proveedores");
        private readonly ToolStripMenuItem listaProveedoresMenu = new ToolStripMenuItem("Administrar proveedores");
        private readonly ToolStripMenuItem gruposProductoMenu = new ToolStripMenuItem("Grupos de productos");
        private readonly ToolStripMenuItem reportePedidosMenu = new ToolStripMenuItem("Pedidos");

        public Inicio(Usuario ousuario)
        {
            usuarioActual = ousuario;
            InitializeComponent();
            // Ubicamos cada herramienta en el módulo donde se administra su información.
            menuMantenedor.DropDownItems.Remove(reposicionToolStripMenuItem);
            btnProducto.DropDownItems.AddRange(new ToolStripItem[] { gruposProductoMenu, reportePedidosMenu });
            menuProveedores.Click -= menuProveedores_Click;
            menuProveedores.DropDownItems.AddRange(new ToolStripItem[] { listaProveedoresMenu, gruposProveedoresMenu });
            listaProveedoresMenu.Click += (s, ev) => AbrirFrm(menuProveedores, new frmProveedores());
            gruposProductoMenu.Click += (s, ev) => AbrirFrm(btnProducto, new frmGruposReposicion());
            reportePedidosMenu.Click += (s, ev) => AbrirFrm(btnProducto, new frmReporteReposicion());
            gruposProveedoresMenu.Click += (s, ev) => AbrirFrm(menuProveedores, new frmGruposProveedores());
            menuVentas.DoubleClickEnabled = true;
            menuCompras.DoubleClickEnabled = true;
            menuVentas.DoubleClick += (s, ev) => AbrirFrm(menuVentas, new frmVentas(usuarioActual));
            menuCompras.DoubleClick += (s, ev) => AbrirFrm(menuCompras, new frmCompraRegistrar(usuarioActual));
            
        }

        private void Inicio_Load(object sender, EventArgs e)
        {

            List<Permiso> listaPermisos = new CN_Permiso().Listar(usuarioActual.IdUsuario);

            foreach (IconMenuItem iconmenu in menuTitulo.Items)
            {
                bool encontrado = listaPermisos.Any(m => m.NombreMenu == iconmenu.Name);
                if (encontrado == false)
                {
                    iconmenu.Enabled = false;
                }
            }
            finanzasToolStripMenuItem.Visible = listaPermisos.Any(m => m.NombreMenu == finanzasToolStripMenuItem.Name);
            reposicionToolStripMenuItem.Visible = false;
            menuAcercaDe.Visible = listaPermisos.Any(m => m.NombreMenu == menuAcercaDe.Name);
            lblNombreUsuario.Text = usuarioActual.NombreCompleto.ToString();
        }
        private void AbrirFrm(IconMenuItem menu, Form formulario)
        {
            if (formulario is frmCompraRegistrar nuevaCompra)
            {
                if (formularioCompraEnCurso != null && !formularioCompraEnCurso.IsDisposed)
                {
                    nuevaCompra.Dispose();
                    formulario = formularioCompraEnCurso;
                }
                else
                {
                    formularioCompraEnCurso = nuevaCompra;
                }
            }

            if(botonActual != null)
            {
                botonActual.BackColor = Color.White;
            }
            botonActual = menu;
            menu.BackColor = Color.BlanchedAlmond;

            if(FormularioActivo != null && !FormularioActivo.IsDisposed && !ReferenceEquals(FormularioActivo, formulario))
            {
                if (FormularioActivo is frmCompraRegistrar)
                    FormularioActivo.Hide();
                else
                {
                    contenedor.Controls.Remove(FormularioActivo);
                    FormularioActivo.Close();
                }
            }

            FormularioActivo = formulario;
            if (formulario.Parent != contenedor)
            {
                formulario.TopLevel = false;
                formulario.FormBorderStyle = FormBorderStyle.None;
                formulario.Dock = DockStyle.Fill;
                formulario.BackColor = Color.SteelBlue;
                contenedor.Controls.Add(formulario);
            }
            formulario.Show();
            formulario.BringToFront();
        }
        private void menuUsuarios_Click(object sender, EventArgs e)
        {
            AbrirFrm((IconMenuItem)sender, new frmUsuarios());
        }

        private void menuVentas_Click(object sender, EventArgs e)
        {
            
        }

        private void btnCategoria_Click(object sender, EventArgs e)
        {
            AbrirFrm((IconMenuItem)sender, new frmCategoria());
        }

        private void btnProducto_Click(object sender, EventArgs e)
        {
            AbrirFrm((IconMenuItem)sender, new frmProducto());
        }

        private void menuClientes_Click(object sender, EventArgs e)
        {
            AbrirFrm((IconMenuItem)sender, new frmClientes());
        }

        private void menuProveedores_Click(object sender, EventArgs e)
        {
            AbrirFrm((IconMenuItem)sender,new frmProveedores());
        }

        private void menuReportes_Click(object sender, EventArgs e)
        {
           
        }

        private void submRegistrarCompra_Click(object sender, EventArgs e)
        {
            AbrirFrm(menuCompras, new frmCompraRegistrar(usuarioActual));
        }

        private void iconDetalleCompra_Click(object sender, EventArgs e)
        {
            AbrirFrm(menuCompras, new frmCompraVerDetalle());
        }

        private void menuMantenedor_Click(object sender, EventArgs e)
        {
            
        }

        private void finanzasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AbrirFrm(menuMantenedor, new frmFinanzas(usuarioActual));
        }

        private void reposicionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AbrirFrm(menuMantenedor, new frmReporteReposicion());
        }

        private void menuAcercaDe_Click(object sender, EventArgs e)
        {
            mdAcercade md = new mdAcercade();
            md.ShowDialog();
        }

        private void registrarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AbrirFrm(menuVentas, new frmVentas(usuarioActual));
        }

        private void verDetalleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AbrirFrm(menuVentas, new frmDetalleVenta());
        }

        private void contenedor_Paint(object sender, PaintEventArgs e)
        {

        }

        private void negocioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AbrirFrm(menuMantenedor, new frmNegocio());
        }

        private void reporteComprasToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
        private void vENTASToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AbrirFrm(menuReportes,new frmReportes());
            
        }

        private void cOMPRASToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AbrirFrm(menuReportes, new frmReporteCompras());
        }
    }
}
