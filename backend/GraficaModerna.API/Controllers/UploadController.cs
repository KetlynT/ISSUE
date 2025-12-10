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
    private const long MaxFileSize = 50 * 1024 * 1024; // 50MB
    private const int MaxImageDimension = 2048;

    // Definição das extensões permitidas e seus MIME types esperados
    private static readonly Dictionary<string, string[]> _allowedMimeTypes = new()
    {
        { ".jpg",  ["image/jpeg"] },
        { ".jpeg", ["image/jpeg"] },
        { ".png",  ["image/png"] },
        { ".webp", ["image/webp"] },
        { ".mp4",  ["video/mp4", "application/mp4"] },
        { ".webm", ["video/webm"] },
        { ".mov",  ["video/quicktime", "video/mp4"] }
    };

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Nenhum ficheiro enviado.");

        if (file.Length > MaxFileSize)
            return BadRequest($"O ficheiro excede o tamanho máximo permitido de {MaxFileSize / 1024 / 1024}MB.");

        // 1. Validação de Extensão e MIME Type
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedMimeTypes.TryGetValue(ext, out var validMimes))
            return BadRequest("Formato de ficheiro não permitido.");

        if (!validMimes.Contains(file.ContentType.ToLower()))
            return BadRequest($"O tipo de conteúdo '{file.ContentType}' não corresponde à extensão '{ext}'.");

        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        // Gera nome seguro (evita Directory Traversal e colisão)
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(folderPath, fileName);

        try
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            // 2. Validação Profunda de Assinatura (Magic Numbers)
            if (!await ValidateFileSignatureAsync(memoryStream, ext))
            {
                return BadRequest("O arquivo parece estar corrompido ou falsificado (assinatura inválida).");
            }

            memoryStream.Position = 0;

            // 3. Processamento Seguro
            if (IsVideo(ext))
            {
                // Para vídeos, salvamos diretamente após validar o cabeçalho.
                // Nota: A segurança ideal exigiria re-codificação (ffmpeg), mas é custoso.
                using var stream = new FileStream(filePath, FileMode.Create);
                await memoryStream.CopyToAsync(stream);
            }
            else
            {
                // Para imagens, o re-processamento atua como sanitização (remove scripts em EXIF, etc)
                using var image = await Image.LoadAsync(memoryStream);

                if (image.Width > MaxImageDimension || image.Height > MaxImageDimension)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(MaxImageDimension, MaxImageDimension),
                        Mode = ResizeMode.Max
                    }));
                }

                // Salva a imagem processada (remove metadados maliciosos)
                await image.SaveAsync(filePath);
            }
        }
        catch (UnknownImageFormatException)
        {
            return BadRequest("O arquivo não é uma imagem válida ou está corrompido.");
        }
        catch (ImageFormatException)
        {
            return BadRequest("Erro ao decodificar a imagem.");
        }
        catch (Exception ex)
        {
            // Logar o erro real no servidor, retornar genérico para o cliente
            Console.WriteLine($"Erro Upload: {ex}");
            return StatusCode(500, "Erro interno ao processar o ficheiro.");
        }

        var fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";
        return Ok(new { url = fileUrl });
    }

    private static bool IsVideo(string ext) => ext is ".mp4" or ".webm" or ".mov";

    /// <summary>
    /// Valida os bytes iniciais do arquivo (Magic Numbers) para evitar spoofing de extensão.
    /// </summary>
    private static async Task<bool> ValidateFileSignatureAsync(MemoryStream stream, string ext)
    {
        stream.Position = 0;
        var header = new byte[16]; // Lemos até 16 bytes para cobrir assinaturas maiores
        var bytesRead = await stream.ReadAsync(header);

        if (bytesRead < 4) return false;

        return ext switch
        {
            // Imagens
            ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            
            ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
            
            ".webp" => bytesRead >= 12 && 
                       header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 && // RIFF
                       header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50, // WEBP
            
            // Vídeos (ISO Base Media File Format - MP4/MOV)
            // A validação anterior falhava pois o tamanho do header (bytes 0-3) varia.
            // O padrão correto é verificar se os bytes 4-7 contêm 'ftyp'.
            ".mp4" or ".mov" => bytesRead >= 8 &&
                                header[4] == 0x66 && header[5] == 0x74 && header[6] == 0x79 && header[7] == 0x70, // 'ftyp'
            
            // WebM (Matroska)
            ".webm" => header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3,

            _ => false
        };
    }
}