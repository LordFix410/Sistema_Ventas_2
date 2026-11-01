using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharpCompress.Readers;

namespace ImagenPDF.Controllers
{
    [ApiController]
    [Route("api/images")]
    public class ImagesController : ControllerBase
    {
        private const long MaxArchiveBytes = 100L * 1024 * 1024;
        private const long MaxImageBytes = 25L * 1024 * 1024;
        private const long MaxExpandedBytes = 150L * 1024 * 1024;
        private const int MaxImages = 300;

        private static readonly IDictionary<string, string> ImageTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".bmp"] = "image/bmp",
            [".webp"] = "image/webp"
        };

        [HttpPost("unpack-zip")]
        [RequestSizeLimit(MaxArchiveBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxArchiveBytes)]
        public async Task<IActionResult> UnpackZip([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Seleccioná un archivo ZIP." });

            if (file.Length > MaxArchiveBytes)
                return BadRequest(new { message = "El ZIP supera el límite de 100 MB." });

            if (!string.Equals(Path.GetExtension(file.FileName), ".zip", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "El archivo debe tener formato ZIP." });

            try
            {
                var images = new List<ZipImage>();
                long expandedBytes = 0;

                using (var stream = file.OpenReadStream())
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false))
                {
                    foreach (var entry in archive.Entries.OrderBy(item => item.FullName, StringComparer.OrdinalIgnoreCase))
                    {
                        var extension = Path.GetExtension(entry.Name);
                        if (string.IsNullOrEmpty(entry.Name) || !ImageTypes.TryGetValue(extension, out var mimeType))
                            continue;

                        if (images.Count >= MaxImages)
                            return BadRequest(new { message = "El ZIP puede incluir como máximo 300 imágenes." });
                        if (entry.Length > MaxImageBytes)
                            return BadRequest(new { message = $"La imagen {entry.Name} supera el límite de 25 MB." });

                        expandedBytes += entry.Length;
                        if (expandedBytes > MaxExpandedBytes)
                            return BadRequest(new { message = "El contenido del ZIP supera el límite total de 150 MB." });

                        using (var imageStream = entry.Open())
                        using (var memory = new MemoryStream())
                        {
                            await imageStream.CopyToAsync(memory);
                            var safeName = Path.GetFileName(entry.FullName.Replace('/', Path.DirectorySeparatorChar));
                            images.Add(new ZipImage(safeName, mimeType, Convert.ToBase64String(memory.ToArray())));
                        }
                    }
                }

                if (images.Count == 0)
                    return BadRequest(new { message = "El ZIP no contiene imágenes compatibles." });

                return Ok(new { images });
            }
            catch (InvalidDataException)
            {
                return BadRequest(new { message = "No se pudo leer el ZIP. Verificá que el archivo no esté dañado." });
            }
        }

        [HttpPost("unpack-rar")]
        [RequestSizeLimit(MaxArchiveBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxArchiveBytes)]
        public async Task<IActionResult> UnpackRar([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Seleccioná un archivo RAR." });
            if (file.Length > MaxArchiveBytes)
                return BadRequest(new { message = "El RAR supera el límite de 100 MB." });
            if (!string.Equals(Path.GetExtension(file.FileName), ".rar", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "El archivo debe tener formato RAR." });

            try
            {
                var images = new List<ZipImage>();
                long expandedBytes = 0;
                using (var stream = file.OpenReadStream())
                using (var reader = ReaderFactory.OpenReader(stream))
                {
                    while (reader.MoveToNextEntry())
                    {
                        var entry = reader.Entry;
                        if (entry.IsDirectory) continue;
                        var entryName = entry.Key ?? string.Empty;
                        var extension = Path.GetExtension(entryName);
                        if (!ImageTypes.TryGetValue(extension, out var mimeType)) continue;
                        if (images.Count >= MaxImages)
                            return BadRequest(new { message = "El RAR puede incluir como máximo 300 imágenes." });
                        if (entry.Size > MaxImageBytes)
                            return BadRequest(new { message = $"La imagen {Path.GetFileName(entryName)} supera el límite de 25 MB." });
                        expandedBytes += entry.Size;
                        if (expandedBytes > MaxExpandedBytes)
                            return BadRequest(new { message = "El contenido del RAR supera el límite total de 150 MB." });

                        using (var imageStream = reader.OpenEntryStream())
                        using (var memory = new MemoryStream())
                        {
                            await imageStream.CopyToAsync(memory);
                            if (memory.Length > MaxImageBytes || memory.Length + expandedBytes - entry.Size > MaxExpandedBytes)
                                return BadRequest(new { message = "El contenido descomprimido supera los límites permitidos." });
                            var safeName = Path.GetFileName(entryName.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));
                            images.Add(new ZipImage(safeName, mimeType, Convert.ToBase64String(memory.ToArray())));
                        }
                    }
                }
                if (images.Count == 0)
                    return BadRequest(new { message = "El RAR no contiene imágenes compatibles." });
                return Ok(new { images });
            }
            catch (Exception)
            {
                return BadRequest(new { message = "No se pudo abrir el RAR. Verificá que no esté dañado, protegido con contraseña o dividido en varias partes." });
            }
        }

        public sealed class ZipImage
        {
            public ZipImage(string name, string mimeType, string data)
            {
                Name = name;
                MimeType = mimeType;
                Data = data;
            }

            public string Name { get; }
            public string MimeType { get; }
            public string Data { get; }
        }
    }
}
