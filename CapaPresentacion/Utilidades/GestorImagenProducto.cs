using CapaDatos;
using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Windows.Forms;

namespace CapaPresentacion.Utilidades
{
    /// <summary>Envía y obtiene las fotos a través del servidor web; la PC cliente no necesita la carpeta física.</summary>
    internal static class GestorImagenProducto
    {
        private const int LadoMaximo = 1200;
        private const long MaximoBytes = 15L * 1024 * 1024;
        private const long CalidadJpeg = 86L;
        private static readonly HttpClient ClienteHttp = CrearCliente();

        private static HttpClient CrearCliente()
        {
            var cliente = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
            return cliente;
        }

        private static Uri UrlApi
        {
            get
            {
                var builder = new SqlConnectionStringBuilder(Conexion.cadena);
                string host = (builder.DataSource ?? string.Empty).Trim();
                if (host.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase)) host = host.Substring(4);
                int coma = host.IndexOf(',');
                if (coma >= 0) host = host.Substring(0, coma);
                int instancia = host.IndexOf('\\');
                if (instancia >= 0) host = host.Substring(0, instancia);
                if (host == "." || host.Equals("(local)", StringComparison.OrdinalIgnoreCase)) host = "localhost";
                if (string.IsNullOrWhiteSpace(host)) throw new InvalidDataException("No se pudo obtener el servidor desde conexion.txt.");
                return new Uri("http://" + host + ":5170/");
            }
        }

        public static string Guardar(string rutaOrigen, string claveUsuario, int rotacion = 0)
        {
            if (string.IsNullOrWhiteSpace(rutaOrigen) || !File.Exists(rutaOrigen))
                throw new FileNotFoundException("Selecciona una fotografía existente.", rutaOrigen);
            if (new FileInfo(rutaOrigen).Length > MaximoBytes)
                throw new InvalidDataException("La foto original no puede superar los 15 MB.");
            string extension = Path.GetExtension(rutaOrigen);
            if (!new[] { ".jpg", ".jpeg", ".png", ".bmp" }.Contains(extension, StringComparer.OrdinalIgnoreCase))
                throw new InvalidDataException("La foto debe estar en formato JPG, PNG o BMP.");

            byte[] foto = PrepararJpeg(rutaOrigen, rotacion);
            using (var contenido = new MultipartFormDataContent())
            using (var archivo = new ByteArrayContent(foto))
            {
                archivo.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                contenido.Add(archivo, "file", "foto.jpg");
                contenido.Add(new StringContent(claveUsuario ?? string.Empty, Encoding.UTF8), "clave");
                using (var respuesta = ClienteHttp.PostAsync(new Uri(UrlApi, "api/productos/imagenes"), contenido).GetAwaiter().GetResult())
                {
                    string respuestaTexto = respuesta.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!respuesta.IsSuccessStatusCode)
                        throw new InvalidOperationException(ExtraerMensaje(respuestaTexto, "El servidor rechazó la fotografía (HTTP " + (int)respuesta.StatusCode + ")."));
                    string ruta = respuestaTexto.Trim().Trim('"');
                    if (!ruta.StartsWith("productos/", StringComparison.OrdinalIgnoreCase) || ruta.Contains(".."))
                        throw new InvalidDataException("El servidor devolvió una ruta de imagen inválida.");
                    return ruta;
                }
            }
        }

        private static byte[] PrepararJpeg(string archivo, int rotacion)
        {
            using (Image original = Image.FromFile(archivo))
            {
                rotacion = ((rotacion % 360) + 360) % 360;
                if (rotacion == 90) original.RotateFlip(RotateFlipType.Rotate90FlipNone);
                else if (rotacion == 180) original.RotateFlip(RotateFlipType.Rotate180FlipNone);
                else if (rotacion == 270) original.RotateFlip(RotateFlipType.Rotate270FlipNone);
                double escala = Math.Min(1d, Math.Min((double)LadoMaximo / original.Width, (double)LadoMaximo / original.Height));
                int ancho = Math.Max(1, (int)Math.Round(original.Width * escala));
                int alto = Math.Max(1, (int)Math.Round(original.Height * escala));
                using (var reducida = new Bitmap(ancho, alto, PixelFormat.Format24bppRgb))
                using (Graphics graphics = Graphics.FromImage(reducida))
                using (var memoria = new MemoryStream())
                {
                    graphics.Clear(Color.White);
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.DrawImage(original, new Rectangle(0, 0, ancho, alto));
                    ImageCodecInfo codec = ImageCodecInfo.GetImageEncoders().First(item => item.MimeType == "image/jpeg");
                    using (var parametros = new EncoderParameters(1))
                    {
                        parametros.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, CalidadJpeg);
                        reducida.Save(memoria, codec, parametros);
                    }
                    return memoria.ToArray();
                }
            }
        }

        public static Image CargarMiniatura(string rutaRelativa, int ancho, int alto)
        {
            try
            {
                byte[] bytes = Descargar(rutaRelativa);
                if (bytes == null) return null;
                using (var stream = new MemoryStream(bytes))
                using (Image original = Image.FromStream(stream))
                {
                    var miniatura = new Bitmap(ancho, alto);
                    using (Graphics graphics = Graphics.FromImage(miniatura))
                    {
                        graphics.Clear(Color.White);
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = SmoothingMode.HighQuality;
                        double escala = Math.Min((double)ancho / original.Width, (double)alto / original.Height);
                        int dibujarAncho = Math.Max(1, (int)(original.Width * escala));
                        int dibujarAlto = Math.Max(1, (int)(original.Height * escala));
                        graphics.DrawImage(original, (ancho - dibujarAncho) / 2, (alto - dibujarAlto) / 2, dibujarAncho, dibujarAlto);
                    }
                    return miniatura;
                }
            }
            catch { return null; }
        }

        public static Image CargarArchivoMiniatura(string archivo, int ancho, int alto, int rotacion = 0)
        {
            try
            {
                using (Image original = Image.FromFile(archivo))
                {
                    rotacion = ((rotacion % 360) + 360) % 360;
                    if (rotacion == 90) original.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    else if (rotacion == 180) original.RotateFlip(RotateFlipType.Rotate180FlipNone);
                    else if (rotacion == 270) original.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    var miniatura = new Bitmap(ancho, alto);
                    using (Graphics graphics = Graphics.FromImage(miniatura))
                    {
                        graphics.Clear(Color.White);
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        double escala = Math.Min((double)ancho / original.Width, (double)alto / original.Height);
                        int w = Math.Max(1, (int)(original.Width * escala));
                        int h = Math.Max(1, (int)(original.Height * escala));
                        graphics.DrawImage(original, (ancho - w) / 2, (alto - h) / 2, w, h);
                    }
                    return miniatura;
                }
            }
            catch { return null; }
        }

        public static void MostrarVistaPrevia(IWin32Window owner, string rutaRelativa, string titulo)
        {
            using (Image imagen = CargarCompleta(rutaRelativa))
            {
                if (imagen == null)
                {
                    MessageBox.Show(owner, "No se pudo descargar la foto desde el servidor. Verifica que el servicio web esté activo.",
                        "Fotografía de producto", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                using (var ventana = new Form())
                using (var cuadro = new PictureBox())
                {
                    ventana.Text = titulo ?? "Fotografía del producto";
                    ventana.StartPosition = FormStartPosition.CenterParent;
                    ventana.Size = new Size(640, 600);
                    ventana.MinimumSize = new Size(420, 360);
                    cuadro.Dock = DockStyle.Fill;
                    cuadro.SizeMode = PictureBoxSizeMode.Zoom;
                    cuadro.BackColor = Color.White;
                    cuadro.Image = new Bitmap(imagen);
                    ventana.Controls.Add(cuadro);
                    ventana.ShowDialog(owner);
                }
            }
        }

        public static Image CargarCompleta(string rutaRelativa)
        {
            try
            {
                byte[] bytes = Descargar(rutaRelativa);
                if (bytes == null) return null;
                using (var stream = new MemoryStream(bytes))
                using (Image imagen = Image.FromStream(stream)) return new Bitmap(imagen);
            }
            catch { return null; }
        }

        public static string CrearCopiaTemporal(string rutaRelativa)
        {
            byte[] bytes = Descargar(rutaRelativa);
            if (bytes == null || bytes.Length == 0) throw new InvalidOperationException("No se pudo descargar la foto actual desde el servidor.");
            string archivo = Path.Combine(Path.GetTempPath(), "libreria-foto-" + Guid.NewGuid().ToString("N") + ".jpg");
            File.WriteAllBytes(archivo, bytes);
            return archivo;
        }

        private static byte[] Descargar(string rutaRelativa)
        {
            if (!RutaValida(rutaRelativa)) return null;
            string url = "imagenes/" + string.Join("/", rutaRelativa.Replace('\\', '/').Split('/').Select(Uri.EscapeDataString));
            using (var respuesta = ClienteHttp.GetAsync(new Uri(UrlApi, url)).GetAwaiter().GetResult())
            {
                if (!respuesta.IsSuccessStatusCode) return null;
                return respuesta.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            }
        }

        public static void Eliminar(string rutaRelativa, string claveUsuario)
        {
            if (!RutaValida(rutaRelativa)) return;
            string url = "api/productos/imagenes?ruta=" + Uri.EscapeDataString(rutaRelativa) + "&clave=" + Uri.EscapeDataString(claveUsuario ?? string.Empty);
            using (var respuesta = ClienteHttp.DeleteAsync(new Uri(UrlApi, url)).GetAwaiter().GetResult())
            {
                if (!respuesta.IsSuccessStatusCode && respuesta.StatusCode != System.Net.HttpStatusCode.NotFound)
                    throw new InvalidOperationException("No se pudo quitar la foto del servidor (HTTP " + (int)respuesta.StatusCode + ").");
            }
        }

        private static bool RutaValida(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta)) return false;
            string limpia = ruta.Replace('\\', '/');
            return limpia.StartsWith("productos/", StringComparison.OrdinalIgnoreCase) && !limpia.Contains("..") && limpia.IndexOfAny(Path.GetInvalidPathChars()) < 0;
        }

        private static string ExtraerMensaje(string contenido, string predeterminado)
        {
            int inicio = contenido == null ? -1 : contenido.IndexOf("\"mensaje\"", StringComparison.OrdinalIgnoreCase);
            if (inicio < 0) return string.IsNullOrWhiteSpace(contenido) ? predeterminado : contenido;
            int dosPuntos = contenido.IndexOf(':', inicio), comilla = dosPuntos < 0 ? -1 : contenido.IndexOf('"', dosPuntos + 1), fin = comilla < 0 ? -1 : contenido.IndexOf('"', comilla + 1);
            return fin > comilla ? contenido.Substring(comilla + 1, fin - comilla - 1) : predeterminado;
        }
    }
}
