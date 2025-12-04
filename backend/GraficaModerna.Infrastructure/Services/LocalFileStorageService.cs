using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace GraficaModerna.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // DEFINIÇÃO CENTRALIZADA DE SEGURANÇA (Magic Numbers)
    private static readonly Dictionary<string, List<byte[]>> _fileSignatures = new()
    {
        { ".jpeg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".jpg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
        { ".webp", new List<byte[]> { new byte[] { 0x52, 0x49, 0x46, 0x46 }, new byte[] { 0x57, 0x45, 0x42, 0x50 } } }
    };

    public LocalFileStorageService(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
    {
        _env = env;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string folderName)
    {
        if (file == null || file.Length == 0)
            throw new Exception("Arquivo vazio.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        // 1. Validação de Extensão
        if (!_fileSignatures.ContainsKey(ext))
            throw new Exception("Formato de arquivo não suportado.");

        var fileName = $"{Guid.NewGuid()}{ext}";
        var folderPath = Path.Combine(_env.WebRootPath, folderName);

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, fileName);

        try
        {
            // 2. Validação de Conteúdo (Magic Numbers) e Cópia Segura
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);

                // Verifica Assinatura
                memoryStream.Position = 0;
                using (var reader = new BinaryReader(memoryStream, System.Text.Encoding.Default, leaveOpen: true))
                {
                    var headerBytes = reader.ReadBytes(12);
                    var signatures = _fileSignatures[ext];

                    if (!signatures.Any(signature =>
                        headerBytes.Take(signature.Length).SequenceEqual(signature)))
                    {
                        throw new Exception("O arquivo está corrompido ou a extensão não corresponde ao conteúdo real.");
                    }
                }

                // Salva no disco
                memoryStream.Position = 0;
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await memoryStream.CopyToAsync(stream);
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro de segurança ou E/S ao salvar arquivo: {ex.Message}");
        }

        var request = _httpContextAccessor.HttpContext!.Request;
        return $"{request.Scheme}://{request.Host}/{folderName}/{fileName}";
    }

    public Task DeleteFileAsync(string fileUrl)
    {
        // TODO: Implementar exclusão física segura (verificar se o path está dentro do root permitido)
        return Task.CompletedTask;
    }
}