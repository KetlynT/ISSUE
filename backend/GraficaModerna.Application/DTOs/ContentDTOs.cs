using System.ComponentModel.DataAnnotations;

namespace GraficaModerna.Application.DTOs;

public class CreateContentDto
{
    [Required(ErrorMessage = "O título é obrigatório.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "O slug é obrigatório.")]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "O conteúdo é obrigatório.")]
    public string Content { get; set; } = string.Empty;
}

public class UpdateContentDto : CreateContentDto
{
    // Pode adicionar campos específicos de atualização se necessário
}