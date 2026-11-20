using System;
using System.Drawing;
using System.Windows.Forms;

namespace CapaPresentacion.Utilidades
{
    /// <summary>Asigna el icono de marca incluido en el ejecutable a cada ventana.</summary>
    internal static class IconoAplicacion
    {
        private static readonly Icon iconoBase = CargarIcono();

        public static void Aplicar(Form formulario)
        {
            if (formulario == null || formulario.IsDisposed) return;
            ExperienciaUsuario.AplicarMayusculas(formulario);
            formulario.Icon = (Icon)iconoBase.Clone();
        }

        private static Icon CargarIcono()
        {
            Icon iconoEjecutable = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            return iconoEjecutable == null
                ? (Icon)SystemIcons.Application.Clone()
                : (Icon)iconoEjecutable.Clone();
        }
    }
}
