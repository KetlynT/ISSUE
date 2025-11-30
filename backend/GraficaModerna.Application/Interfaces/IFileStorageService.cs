using Microsoft.AspNetCore.Http;

namespace GraficaModerna.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string folderName);
    Task DeleteFileAsync(string fileUrl);
}