using System.ComponentModel.DataAnnotations;

namespace GraficaModerna.Application.DTOs;

public record CreateProductDto(
    [Required(ErrorMessage = "Nome é obrigatório")]
    string Name,
    string Description,
    [Range(0.01, double.MaxValue, ErrorMessage = "Preço deve ser maior que zero")]
    decimal Price,
    string ImageUrl,
    [Range(0.001, double.MaxValue, ErrorMessage = "Peso inválido (kg)")]
    decimal Weight,
    [Range(1, int.MaxValue, ErrorMessage = "Largura inválida (cm)")]
    int Width,
    [Range(1, int.MaxValue, ErrorMessage = "Altura inválida (cm)")]
    int Height,
    [Range(1, int.MaxValue, ErrorMessage = "Comprimento inválido (cm)")]
    int Length,
    [Range(0, int.MaxValue, ErrorMessage = "Estoque inválido")]
    int StockQuantity
);

public record ProductResponseDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string ImageUrl,
    decimal Weight,
    int Width,
    int Height,
    int Length,
    int StockQuantity
);