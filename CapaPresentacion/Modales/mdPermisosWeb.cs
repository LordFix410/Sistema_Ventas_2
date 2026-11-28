using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CapaEntidad;
using CapaNegocio;

namespace CapaPresentacion.Modales
{
    public sealed class mdPermisosWeb : Form
    {
        private readonly Usuario actor;
        private readonly CN_PermisoWeb permisos = new CN_PermisoWeb();
        private readonly ListBox usuarios = new ListBox();
        private readonly FlowLayoutPanel paginas = new FlowLayoutPanel();
        private readonly Dictionary<string, CheckBox> checks = new Dictionary<string, CheckBox>(StringComparer.OrdinalIgnoreCase);
        private readonly CheckBox superAdministrador = new CheckBox();
        private readonly Label detalle = new Label();
        private readonly Label mensaje = new Label();
        private readonly Button guardar = new Button();

        public mdPermisosWeb(Usuario usuarioActor)
        {
            actor = usuarioActor ?? throw new ArgumentNullException("usuarioActor");
            Text = "Accesos a páginas web";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(890, 600);
            BackColor = Color.FromArgb(242, 246, 249);
            Font = new Font("Segoe UI", 9F);

            var header = new Panel { Dock = DockStyle.Top, Height = 82, BackColor = Color.FromArgb(20, 61, 88) };
            var title = new Label { Text = "Administrar accesos web", ForeColor = Color.White, Font = new Font("Segoe UI", 17F, FontStyle.Bold), AutoSize = true, Location = new Point(24, 13) };
            var subtitle = new Label { Text = "Elige qué páginas puede abrir cada usuario.", ForeColor = Color.FromArgb(205, 222, 235), AutoSize = true, Location = new Point(27, 51) };
            header.Controls.Add(title); header.Controls.Add(subtitle); Controls.Add(header);

            var left = new Panel { Location = new Point(20, 102), Size = new Size(300, 434), BackColor = Color.White, Padding = new Padding(12) };
            left.Controls.Add(new Label { Text = "USUARIOS", ForeColor = Color.FromArgb(31, 111, 166), Font = new Font("Segoe UI", 9F, FontStyle.Bold), AutoSize = true, Location = new Point(12, 12) });
            usuarios.Location = new Point(12, 42); usuarios.Size = new Size(276, 378); usuarios.BorderStyle = BorderStyle.FixedSingle;
            usuarios.SelectedIndexChanged += UsuarioSeleccionado;
            left.Controls.Add(usuarios); Controls.Add(left);

            var right = new Panel { Location = new Point(335, 102), Size = new Size(535, 434), BackColor = Color.White, Padding = new Padding(16) };
            var heading = new Label { Text = "Páginas habilitadas", ForeColor = Color.FromArgb(20, 61, 88), Font = new Font("Segoe UI", 13F, FontStyle.Bold), AutoSize = true, Location = new Point(16, 14) };
            detalle.Location = new Point(18, 45); detalle.Size = new Size(490, 36); detalle.ForeColor = Color.FromArgb(90, 108, 122);
            superAdministrador.Text = "Puede administrar accesos web de empleados";
            superAdministrador.Location = new Point(18, 86); superAdministrador.Size = new Size(480, 26);
            superAdministrador.Font = new Font("Segoe UI", 9F, FontStyle.Bold); superAdministrador.ForeColor = Color.FromArgb(20, 61, 88);
            superAdministrador.Visible = actor.EsAdministradorPrincipal;
            paginas.Location = new Point(16, actor.EsAdministradorPrincipal ? 123 : 88);
            paginas.Size = new Size(500, actor.EsAdministradorPrincipal ? 286 : 320);
            paginas.AutoScroll = true; paginas.FlowDirection = FlowDirection.TopDown; paginas.WrapContents = false;
            paginas.Padding = new Padding(6);
            right.Controls.Add(heading); right.Controls.Add(detalle); right.Controls.Add(superAdministrador); right.Controls.Add(paginas); Controls.Add(right);

            mensaje.Location = new Point(22, 545); mensaje.Size = new Size(540, 30); mensaje.ForeColor = Color.FromArgb(80, 95, 108);
            guardar.Text = "Guardar accesos"; guardar.Location = new Point(665, 548); guardar.Size = new Size(205, 38);
            guardar.BackColor = Color.FromArgb(31, 111, 166); guardar.ForeColor = Color.White; guardar.FlatStyle = FlatStyle.Flat;
            guardar.FlatAppearance.BorderSize = 0; guardar.Font = new Font("Segoe UI", 9F, FontStyle.Bold); guardar.Click += GuardarAccesos;
            var cerrar = new Button { Text = "Cerrar", Location = new Point(558, 548), Size = new Size(95, 38), FlatStyle = FlatStyle.Flat };
            cerrar.Click += (s, e) => Close();
            Controls.Add(mensaje); Controls.Add(cerrar); Controls.Add(guardar);

            try
            {
                var catalogo = permisos.ListarPaginas();
                foreach (var page in catalogo)
                {
                    var check = new CheckBox { Text = page.Nombre + "   ·   " + page.Codigo, Tag = page.Codigo, Width = 470, Height = 34, Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(30, 55, 72), Margin = new Padding(5) };
                    checks.Add(page.Codigo, check); paginas.Controls.Add(check);
                }
                CargarUsuarios(null);
            }
            catch (Exception ex)
            {
                mensaje.ForeColor = Color.Firebrick;
                mensaje.Text = "No se pudo cargar seguridad web. Aplica migracion_permisos_pagina_web.sql. " + ex.Message;
                guardar.Enabled = false;
            }
        }

        private void CargarUsuarios(int? seleccionarId)
        {
            usuarios.Items.Clear();
            var lista = permisos.ListarUsuariosGestionables(actor.IdUsuario);
            foreach (var item in lista) usuarios.Items.Add(new UsuarioPermisosItem(item));
            if (usuarios.Items.Count == 0) { LimpiarDetalle(); mensaje.Text = "No hay usuarios que puedas administrar."; return; }
            var indice = 0;
            if (seleccionarId.HasValue)
                for (var i = 0; i < usuarios.Items.Count; i++)
                    if (((UsuarioPermisosItem)usuarios.Items[i]).Usuario.IdUsuario == seleccionarId.Value) { indice = i; break; }
            usuarios.SelectedIndex = indice;
        }

        private void UsuarioSeleccionado(object sender, EventArgs e)
        {
            var item = usuarios.SelectedItem as UsuarioPermisosItem;
            if (item == null) { LimpiarDetalle(); return; }
            var target = item.Usuario;
            detalle.Text = target.oRol.Descripcion + (target.EsAdministradorPrincipal ? " · Administrador principal" : target.EsSuperAdministradorWeb ? " · Superadministrador web" : "") + (target.Estado ? "" : " · Inactivo");
            var assigned = target.EsAdministradorPrincipal ? new HashSet<string>(checks.Keys, StringComparer.OrdinalIgnoreCase) : permisos.ListarPaginasAsignadas(target.IdUsuario);
            foreach (var pair in checks) { pair.Value.Checked = assigned.Contains(pair.Key); pair.Value.Enabled = !target.EsAdministradorPrincipal && target.Estado; }
            superAdministrador.Checked = target.EsSuperAdministradorWeb;
            superAdministrador.Enabled = actor.EsAdministradorPrincipal && !target.EsAdministradorPrincipal && target.Estado;
            guardar.Enabled = !target.EsAdministradorPrincipal && target.Estado;
            mensaje.Text = target.EsAdministradorPrincipal ? "La cuenta principal siempre conserva acceso completo." : "Los permisos se aplican al volver a iniciar sesión o al abrir una página.";
        }

        private void GuardarAccesos(object sender, EventArgs e)
        {
            var item = usuarios.SelectedItem as UsuarioPermisosItem;
            if (item == null) return;
            var selected = checks.Where(x => x.Value.Checked).Select(x => x.Key).ToList();
            bool? super = actor.EsAdministradorPrincipal ? (bool?)superAdministrador.Checked : null;
            string result;
            if (permisos.Guardar(actor.IdUsuario, item.Usuario.IdUsuario, selected, super, out result))
            {
                mensaje.ForeColor = Color.FromArgb(22, 125, 78); mensaje.Text = result;
                CargarUsuarios(item.Usuario.IdUsuario);
            }
            else { mensaje.ForeColor = Color.Firebrick; mensaje.Text = result; }
        }

        private void LimpiarDetalle()
        {
            detalle.Text = ""; superAdministrador.Checked = false; superAdministrador.Enabled = false;
            foreach (var check in checks.Values) { check.Checked = false; check.Enabled = false; }
            guardar.Enabled = false;
        }

        private sealed class UsuarioPermisosItem
        {
            public Usuario Usuario { get; private set; }
            public UsuarioPermisosItem(Usuario usuario) { Usuario = usuario; }
            public override string ToString()
            {
                var estado = Usuario.Estado ? "" : " · Inactivo";
                var nivel = Usuario.EsAdministradorPrincipal ? "Administrador principal" : Usuario.EsSuperAdministradorWeb ? "Superadministrador web" : Usuario.oRol.Descripcion;
                return Usuario.NombreCompleto + " · " + nivel + estado;
            }
        }
    }
}
