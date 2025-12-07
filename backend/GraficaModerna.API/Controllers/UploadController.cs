using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableRateLimiting("UploadPolicy")]
public class UploadController : ControllerBase
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private const int MaxImageDimension = 2048;

    private static readonly Dictionary<string, string> _validMimeTypes = new()
    {
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".webp", "image/webp" }
    };

    private readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Nenhum ficheiro enviado.");

        if (file.Length > MaxFileSize)
            return BadRequest("O ficheiro excede o tamanho máximo permitido de 5MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return BadRequest("Formato de ficheiro não permitido.");

        if (!_validMimeTypes.TryGetValue(ext, out var expectedMime) ||
            !file.ContentType.Equals(expectedMime, StringComparison.CurrentCultureIgnoreCase))
            return BadRequest($"Tipo MIME inválido. Esperado: {expectedMime}, Recebido: {file.ContentType}");

        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(folderPath, fileName);

        try
        {
            using var stream = file.OpenReadStream();
            using var image = await Image.LoadAsync(stream);

            var format = image.Metadata.DecodedImageFormat;
            if (format == null || !_validMimeTypes[ext].Contains(format.DefaultMimeType))
            {
                return BadRequest("O conteúdo da imagem não corresponde à extensão declarada.");
            }

            if (image.Width > MaxImageDimension || image.Height > MaxImageDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(MaxImageDimension, MaxImageDimension),
                    Mode = ResizeMode.Max
                }));
            }

            await image.SaveAsync(filePath);
        }
        catch (ImageFormatException)
        {
            return BadRequest("O arquivo não é uma imagem válida ou está corrompido.");
        }
        catch (Exception)
        {
            return StatusCode(500, "Erro interno ao processar o ficheiro.");
        }

        var imageUrl = $"{Request.Scheme}://{Request.Host}/images/{fileName}";
        return Ok(new { url = imageUrl });
    }
}