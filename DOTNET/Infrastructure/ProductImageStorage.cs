using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ImagenPDF.Infrastructure
{
    /// <summary>Resuelve una sola carpeta física para guardar y publicar las fotos de productos.</summary>
    internal static class ProductImageStorage
    {
        public static string Root(IConfiguration configuration, IWebHostEnvironment environment)
        {
            var configuredPath = configuration["ProductImages:Path"];
            if (!string.IsNullOrWhiteSpace(configuredPath))
                return Path.GetFullPath(configuredPath);

            var contentRoot = Path.GetFullPath(environment.ContentRootPath);
            var contentDirectory = new DirectoryInfo(contentRoot);
            if (contentDirectory.Name.Equals("publicado", StringComparison.OrdinalIgnoreCase) &&
                contentDirectory.Parent != null)
            {
                var projectImages = Path.Combine(contentDirectory.Parent.FullName, "imagenes");
                var publishedImages = Path.Combine(contentRoot, "imagenes");
                if (!ContainsProductImages(projectImages) && ContainsProductImages(publishedImages))
                    return publishedImages;
                return projectImages;
            }

            return Path.Combine(contentRoot, "imagenes");
        }

        public static string ProductsFolder(IConfiguration configuration, IWebHostEnvironment environment) =>
            Path.Combine(Root(configuration, environment), "productos");

        private static bool ContainsProductImages(string root)
        {
            var folder = Path.Combine(root, "productos");
            if (!Directory.Exists(folder)) return false;
            foreach (var file in Directory.EnumerateFiles(folder)) return true;
            return false;
        }
    }
}
