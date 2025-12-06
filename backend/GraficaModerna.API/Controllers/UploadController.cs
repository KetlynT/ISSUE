using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SixLabors.ImageSharp; // Requer: dotnet add package SixLabors.ImageSharp
using SixLabors.ImageSharp.Formats;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableRateLimiting("UploadPolicy")]
public class UploadController : ControllerBase
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    // Mapeamento estrito entre Extensão e MIME Type esperado
    private static readonly Dictionary<string, string> _validMimeTypes = new()
    {
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".webp", "image/webp" }
    };

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Nenhum ficheiro enviado.");

        if (file.Length > MaxFileSize)
            return BadRequest("O ficheiro excede o tamanho máximo permitido de 5MB.");

        // 1. Validação de Extensão
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return BadRequest("Formato de ficheiro não permitido.");

        // 2. Validação de MIME Type (Header da Requisição)
        // Isso previne erros honestos e filtra ataques preguiçosos
        if (!_validMimeTypes.TryGetValue(ext, out var expectedMime) ||
            file.ContentType.ToLower() != expectedMime)
        {
            return BadRequest($"Tipo MIME inválido. Esperado: {expectedMime}, Recebido: {file.ContentType}");
        }

        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(folderPath, fileName);

        try
        {
            using (var stream = file.OpenReadStream())
            {
                // 3. Validação Profunda (DEEP INSPECTION) - A CORREÇÃO REAL
                // Tentamos carregar a imagem. Se for um script disfarçado com header falso, 
                // o parser vai falhar ou detectar o formato incorreto.
                try
                {
                    // Detecta o formato real baseando-se no conteúdo completo
                    var format = await Image.DetectFormatAsync(stream);

                    if (format == null)
                        return BadRequest("O arquivo não é uma imagem reconhecida.");

                    // Verifica se o formato detectado bate com a extensão
                    // Ex: Impede que um arquivo GIF seja renomeado para .jpg
                    if (!_validMimeTypes[ext].Contains(format.DefaultMimeType))
                    {
                        return BadRequest($"Conteúdo do arquivo ({format.DefaultMimeType}) não corresponde à extensão ({ext}).");
                    }

                    // (Opcional) Re-encode: Carregar e salvar novamente remove metadados maliciosos (Exif)
                    // stream.Position = 0;
                    // using var image = await Image.LoadAsync(stream);
                    // await image.SaveAsync(filePath); 
                }
                catch (Exception)
                {
                    return BadRequest("O arquivo está corrompido ou não é uma imagem válida.");
                }

                // Se passou na validação profunda, salvamos o stream original (ou a versão sanitizada acima)
                stream.Position = 0;
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro crítico no upload: {ex}");
            return StatusCode(500, "Erro interno ao processar o ficheiro.");
        }

        var imageUrl = $"{Request.Scheme}://{Request.Host}/images/{fileName}";
        return Ok(new { url = imageUrl });
    }
}