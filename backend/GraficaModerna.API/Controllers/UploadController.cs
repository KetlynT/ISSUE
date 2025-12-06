using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableRateLimiting("UploadPolicy")]
public class UploadController : ControllerBase
{
    // Limite de segurança: 5MB
    private const long MaxFileSize = 5 * 1024 * 1024;
    private readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    // Assinaturas mágicas dos ficheiros (Magic Numbers)
    private static readonly Dictionary<string, List<byte[]>> _fileSignatures = new()
    {
        { ".jpeg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".jpg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
        { ".webp", new List<byte[]> { new byte[] { 0x52, 0x49, 0x46, 0x46 }, new byte[] { 0x57, 0x45, 0x42, 0x50 } } }
    };

    [HttpPost]
    [Authorize(Roles = "Admin")] // SEGURANÇA: Apenas Admin pode fazer upload
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Nenhum ficheiro enviado.");

        // Validação de Tamanho
        if (file.Length > MaxFileSize)
            return BadRequest("O ficheiro excede o tamanho máximo permitido de 5MB.");

        // Validação de Extensão
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return BadRequest("Formato de ficheiro não permitido. Apenas imagens (.jpg, .png, .webp) são aceites.");

        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(folderPath, fileName);

        try
        {
            // CORREÇÃO: Leitura otimizada do Stream sem carregar tudo em memória (RAM)
            using (var stream = file.OpenReadStream())
            {
                // 1. Validação de Magic Numbers (lendo apenas o cabeçalho)
                var headerBuffer = new byte[12]; // Tamanho suficiente para as assinaturas definidas
                var bytesRead = await stream.ReadAsync(headerBuffer, 0, headerBuffer.Length);

                var signatures = _fileSignatures.ContainsKey(ext) ? _fileSignatures[ext] : null;

                if (signatures == null || !signatures.Any(signature =>
                    headerBuffer.Take(signature.Length).SequenceEqual(signature)))
                {
                    return BadRequest("O ficheiro está corrompido ou é malicioso (assinatura inválida).");
                }

                // 2. Salvar no Disco
                // Resetamos a posição do stream para o início para copiar o conteúdo completo
                stream.Position = 0;

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }
        }
        catch (Exception ex)
        {
            // Em produção, logue 'ex' adequadamente e retorne mensagem genérica para evitar vazamento de detalhes
            // _logger não está injetado aqui; se precisar, adicione ILogger via DI. Por ora, log simples no console.
            Console.WriteLine($"Erro ao processar o ficheiro: {ex}");
            return StatusCode(500, "Erro interno ao processar o ficheiro.");
        }

        var imageUrl = $"{Request.Scheme}://{Request.Host}/images/{fileName}";
        return Ok(new { url = imageUrl });
    }
}