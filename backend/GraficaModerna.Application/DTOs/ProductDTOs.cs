using System.ComponentModel.DataAnnotations;

namespace GraficaModerna.Application.DTOs;

// Input (Request)
public record CreateProductDto(
    [Required(ErrorMessage = "Nome é obrigatório")] string Name,
    string Description,
    [Range(0.01, double.MaxValue, ErrorMessage = "Preço deve ser maior que zero")] decimal Price,
    string ImageUrl,
    [Range(0.001, 1000, ErrorMessage = "Peso inválido (kg)")] decimal Weight,
    [Range(1, 1000, ErrorMessage = "Largura inválida (cm)")] int Width,
    [Range(1, 1000, ErrorMessage = "Altura inválida (cm)")] int Height,
    [Range(1, 1000, ErrorMessage = "Comprimento inválido (cm)")] int Length,
    [Range(0, 10000, ErrorMessage = "Estoque inválido")] int StockQuantity // NOVO
);

// Output (Response)
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
    int StockQuantity // NOVO
);