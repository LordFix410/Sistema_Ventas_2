using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using CapaNegocio;
using CapaEntidad;

namespace CapaPresentacion
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
            CapaPresentacion.Utilidades.ExperienciaUsuario.Aplicar(this, txtContrasena, txtContrasena);
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnIngresar_Click(object sender, EventArgs e)
        {
            List<Usuario> coincidencias;
            try
            {
                coincidencias = new CN_Usuario().ListarActivosPorClave(txtContrasena.Text);
            }
            catch (Exception ex)
            {
                CapaPresentacion.DialogoSistema.Show("No se pudo validar el acceso. Verifica la conexión con la base de datos.\n\n" + ex.Message,
                    "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (coincidencias.Count == 1)
            {
                Usuario ousuario = coincidencias[0];
                Inicio frmInicio = new Inicio(ousuario);
                frmInicio.WindowState = FormWindowState.Maximized;

                frmInicio.Show();
                this.Hide();
                 
                frmInicio.FormClosing += Frm_Closing;

            }
            else if (coincidencias.Count > 1)
            {
                CapaPresentacion.DialogoSistema.Show("La contraseña está asignada a más de un usuario activo. Asigna una contraseña única a cada usuario.",
                    "Contraseña duplicada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtContrasena.Focus();
                txtContrasena.SelectAll();
            }
            else
            {
                CapaPresentacion.DialogoSistema.Show("Usuario incorrecto", "Ingreso Sistema", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
           
        }

        private void Frm_Closing(object sender, FormClosingEventArgs e)
        {
            txtContrasena.Text = "";
            txtNumeroDoc.Text = "";
            this.Show();
        }

        private void txtContrasena_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                btnIngresar.PerformClick();
            }
        }

        private void iconPictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
