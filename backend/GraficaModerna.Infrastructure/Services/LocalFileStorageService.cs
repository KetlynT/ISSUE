using System.Text;
using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace GraficaModerna.Infrastructure.Services;

public class LocalFileStorageService(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
    : IFileStorageService
{
    private static readonly Dictionary<string, List<byte[]>> _fileSignatures = new()
    {
        { ".jpeg", [new byte[] { 0xFF, 0xD8, 0xFF }] },
        { ".jpg", [new byte[] { 0xFF, 0xD8, 0xFF }] },
        { ".png", [new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }] },
        { ".webp", ["RIFF"u8.ToArray(), "WEBP"u8.ToArray()] }
    };

    private readonly IWebHostEnvironment _env = env;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task<string> SaveFileAsync(IFormFile file, string folderName)
    {
        if (file == null || file.Length == 0)
            throw new Exception("Arquivo vazio.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!_fileSignatures.TryGetValue(ext, out var signatures))
            throw new Exception("Formato de arquivo não suportado.");

        var fileName = $"{Guid.NewGuid()}{ext}";
        var folderPath = Path.Combine(_env.WebRootPath, folderName);

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, fileName);

        try
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);

            memoryStream.Position = 0;
            using (var reader = new BinaryReader(memoryStream, Encoding.Default, true))
            {
                var headerBytes = reader.ReadBytes(12);
                if (!signatures.Any(signature =>
                        headerBytes.Take(signature.Length).SequenceEqual(signature)))
                    throw new Exception("O arquivo está corrompido ou a extensão não corresponde ao conteúdo real.");
            }

            memoryStream.Position = 0;
            using var stream = new FileStream(filePath, FileMode.Create);
            await memoryStream.CopyToAsync(stream);
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro de segurança ou E/S ao salvar arquivo: {ex.Message}");
        }

        var request = _httpContextAccessor.HttpContext!.Request;
        return $"{request.Scheme}://{request.Host}/{folderName}/{fileName}";
    }

    public async Task DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return;

        try
        {
            var uri = new Uri(fileUrl);
            var relativePath = Uri.UnescapeDataString(uri.AbsolutePath)
                .TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);

            var webRoot = Path.GetFullPath(_env.WebRootPath);
            if (!webRoot.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                webRoot += Path.DirectorySeparatorChar;
            }

            var fullPath = Path.GetFullPath(Path.Combine(webRoot, relativePath));

            if (!fullPath.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase))
                return;

            if (File.Exists(fullPath))
            {
                try
                {
                    using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Write, FileShare.None);
                    var zero = new byte[8192];
                    var remaining = fs.Length;
                    fs.Position = 0;
                    while (remaining > 0)
                    {
                        var write = (int)Math.Min(zero.Length, remaining);
                        await fs.WriteAsync(zero.AsMemory(0, write));
                        remaining -= write;
                    }
                }
                catch
                {
                }

                File.Delete(fullPath);
            }
        }
        catch
        {
        }
    }
}