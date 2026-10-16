using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CapaPresentacion
{
    /// <summary>Ventanas uniformes para avisos, errores y confirmaciones del sistema.</summary>
    internal static class DialogoSistema
    {
        public static DialogResult Show(string mensaje)
        {
            return Show(null, mensaje, "Sistema de ventas", MessageBoxButtons.OK,
                MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
        }

        public static DialogResult Show(string mensaje, string titulo)
        {
            return Show(null, mensaje, titulo, MessageBoxButtons.OK,
                MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
        }

        public static DialogResult Show(string mensaje, string titulo,
            MessageBoxButtons botones, MessageBoxIcon icono)
        {
            return Show(null, mensaje, titulo, botones, icono, MessageBoxDefaultButton.Button1);
        }

        public static DialogResult Show(string mensaje, string titulo,
            MessageBoxButtons botones, MessageBoxIcon icono, MessageBoxDefaultButton predeterminado)
        {
            return Show(null, mensaje, titulo, botones, icono, predeterminado);
        }

        public static DialogResult Show(IWin32Window propietario, string mensaje, string titulo,
            MessageBoxButtons botones, MessageBoxIcon icono)
        {
            return Show(propietario, mensaje, titulo, botones, icono, MessageBoxDefaultButton.Button1);
        }

        public static DialogResult Show(IWin32Window propietario, string mensaje, string titulo,
            MessageBoxButtons botones, MessageBoxIcon icono, MessageBoxDefaultButton predeterminado)
        {
            using (var ventana = new VentanaMensaje(mensaje, titulo, botones, icono, predeterminado))
            {
                return propietario == null ? ventana.ShowDialog() : ventana.ShowDialog(propietario);
            }
        }

        private sealed class VentanaMensaje : Form
        {
            private readonly MessageBoxButtons tipoBotones;

            public VentanaMensaje(string mensaje, string titulo, MessageBoxButtons botones,
                MessageBoxIcon icono, MessageBoxDefaultButton predeterminado)
            {
                tipoBotones = botones;
                Text = string.IsNullOrWhiteSpace(titulo) ? "Sistema de ventas" : titulo;
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                BackColor = Color.White;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                ShowInTaskbar = false;
                ShowIcon = false;
                MaximizeBox = false;
                MinimizeBox = false;
                ClientSize = new Size(540, 190);
                KeyPreview = true;

                Color acento = Color.FromArgb(43, 105, 157);
                Color suave = Color.FromArgb(232, 241, 249);
                string simbolo = "i";
                Color colorSimbolo = acento;

                if (icono == MessageBoxIcon.Error || icono == MessageBoxIcon.Hand || icono == MessageBoxIcon.Stop)
                {
                    simbolo = "×";
                    colorSimbolo = Color.FromArgb(190, 55, 55);
                    suave = Color.FromArgb(252, 235, 235);
                }
                else if (icono == MessageBoxIcon.Warning || icono == MessageBoxIcon.Exclamation)
                {
                    simbolo = "!";
                    colorSimbolo = Color.FromArgb(190, 125, 20);
                    suave = Color.FromArgb(253, 245, 224);
                }
                else if (icono == MessageBoxIcon.Question)
                {
                    simbolo = "?";
                    colorSimbolo = Color.FromArgb(43, 105, 157);
                }
                else if (icono == MessageBoxIcon.Information || icono == MessageBoxIcon.Asterisk)
                {
                    simbolo = "✓";
                    colorSimbolo = Color.FromArgb(34, 137, 100);
                    suave = Color.FromArgb(230, 246, 239);
                }

                var encabezado = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 48,
                    BackColor = Color.FromArgb(38, 77, 112)
                };
                var etiquetaTitulo = new Label
                {
                    Text = Text,
                    Dock = DockStyle.Fill,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(18, 0, 12, 0),
                    AutoEllipsis = true
                };
                encabezado.Controls.Add(etiquetaTitulo);

                string texto = mensaje ?? string.Empty;
                Size medida = TextRenderer.MeasureText(texto, Font, new Size(390, 1000),
                    TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
                int altoTexto = Math.Max(44, medida.Height);
                int altoCuerpo = Math.Min(330, altoTexto + 30);
                ClientSize = new Size(540, 48 + altoCuerpo + 68);

                var cuerpo = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    Padding = new Padding(18, 14, 18, 10),
                    AutoScroll = altoTexto + 30 > 330
                };
                var etiquetaIcono = new Label
                {
                    Text = simbolo,
                    Location = new Point(0, 12),
                    Size = new Size(48, 48),
                    BackColor = suave,
                    ForeColor = colorSimbolo,
                    Font = new Font("Segoe UI Symbol", 25f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                var areaTexto = new Panel
                {
                    Location = new Point(68, 4),
                    Size = new Size(424, altoCuerpo - 20),
                    AutoScroll = altoTexto > altoCuerpo - 28,
                    BackColor = Color.White
                };
                var etiquetaMensaje = new Label
                {
                    Text = texto,
                    Location = Point.Empty,
                    AutoSize = true,
                    MaximumSize = new Size(400, 0),
                    Font = Font,
                    ForeColor = Color.FromArgb(48, 55, 62)
                };
                areaTexto.Controls.Add(etiquetaMensaje);
                cuerpo.Controls.Add(etiquetaIcono);
                cuerpo.Controls.Add(areaTexto);

                var pie = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 68,
                    BackColor = Color.FromArgb(244, 246, 248),
                    Padding = new Padding(12)
                };
                var contenedorBotones = new FlowLayoutPanel
                {
                    Dock = DockStyle.Right,
                    AutoSize = true,
                    FlowDirection = FlowDirection.RightToLeft,
                    WrapContents = false,
                    BackColor = Color.Transparent,
                    Padding = new Padding(0, 3, 0, 0)
                };

                List<Button> listaBotones = CrearBotones(botones);
                int indicePredeterminado = Math.Max(0, Math.Min(listaBotones.Count - 1,
                    (int)predeterminado));
                for (int i = 0; i < listaBotones.Count; i++)
                {
                    Button boton = listaBotones[i];
                    bool esPredeterminado = i == indicePredeterminado;
                    boton.Margin = new Padding(8, 0, 0, 0);
                    boton.Size = new Size(104, 36);
                    boton.FlatStyle = FlatStyle.Flat;
                    boton.FlatAppearance.BorderSize = esPredeterminado ? 0 : 1;
                    boton.FlatAppearance.BorderColor = Color.FromArgb(205, 212, 219);
                    boton.BackColor = esPredeterminado ? acento : Color.White;
                    boton.ForeColor = esPredeterminado ? Color.White : Color.FromArgb(55, 64, 72);
                    boton.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
                    boton.UseVisualStyleBackColor = false;
                    contenedorBotones.Controls.Add(boton);
                    if (esPredeterminado) AcceptButton = boton;
                }

                Button botonEscape = listaBotones.Find(b => b.DialogResult == DialogResult.Cancel)
                    ?? listaBotones.Find(b => b.DialogResult == DialogResult.No);
                if (botonEscape != null) CancelButton = botonEscape;

                Controls.Add(cuerpo);
                Controls.Add(pie);
                Controls.Add(encabezado);
                pie.Controls.Add(contenedorBotones);

                FormClosing += (s, e) =>
                {
                    if (this.DialogResult == System.Windows.Forms.DialogResult.None)
                        this.DialogResult = botonEscape == null
                            ? System.Windows.Forms.DialogResult.OK
                            : botonEscape.DialogResult;
                };
            }

            private static List<Button> CrearBotones(MessageBoxButtons botones)
            {
                var resultado = new List<Button>();
                switch (botones)
                {
                    case MessageBoxButtons.OKCancel:
                        Agregar(resultado, "Aceptar", DialogResult.OK);
                        Agregar(resultado, "Cancelar", DialogResult.Cancel);
                        break;
                    case MessageBoxButtons.YesNo:
                        Agregar(resultado, "Sí", DialogResult.Yes);
                        Agregar(resultado, "No", DialogResult.No);
                        break;
                    case MessageBoxButtons.YesNoCancel:
                        Agregar(resultado, "Sí", DialogResult.Yes);
                        Agregar(resultado, "No", DialogResult.No);
                        Agregar(resultado, "Cancelar", DialogResult.Cancel);
                        break;
                    case MessageBoxButtons.RetryCancel:
                        Agregar(resultado, "Reintentar", DialogResult.Retry);
                        Agregar(resultado, "Cancelar", DialogResult.Cancel);
                        break;
                    case MessageBoxButtons.AbortRetryIgnore:
                        Agregar(resultado, "Anular", DialogResult.Abort);
                        Agregar(resultado, "Reintentar", DialogResult.Retry);
                        Agregar(resultado, "Omitir", DialogResult.Ignore);
                        break;
                    default:
                        Agregar(resultado, "Aceptar", DialogResult.OK);
                        break;
                }
                return resultado;
            }

            private static void Agregar(List<Button> botones, string texto, DialogResult resultado)
            {
                botones.Add(new Button { Text = texto, DialogResult = resultado });
            }
        }
    }
}
