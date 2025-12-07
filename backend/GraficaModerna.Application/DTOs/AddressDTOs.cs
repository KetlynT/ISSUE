using System.ComponentModel.DataAnnotations;

namespace GraficaModerna.Application.DTOs;

public record AddressDto(
    Guid Id,
    string Name,
    string ReceiverName,
    string ZipCode,
    string Street,
    string Number,
    string Complement,
    string Neighborhood,
    string City,
    string State,
    string Reference,
    string PhoneNumber,
    bool IsDefault
);

public record CreateAddressDto(
    [Required(ErrorMessage = "O nome é obrigatório")] 
    string Name,

    [Required(ErrorMessage = "O nome do destinatário é obrigatório")] 
    string ReceiverName,

    [Required(ErrorMessage = "O CEP é obrigatório")]
    [StringLength(8, MinimumLength = 8, ErrorMessage = "O CEP deve ter exatamente 8 caracteres")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "O CEP deve conter apenas números (8 dígitos)")]
    string ZipCode,

    [Required(ErrorMessage = "A rua é obrigatória")] 
    string Street,

    [Required(ErrorMessage = "O número é obrigatório")] 
    string Number,

    string? Complement,

    [Required(ErrorMessage = "O bairro é obrigatório")] 
    string Neighborhood,

    [Required(ErrorMessage = "A cidade é obrigatória")] 
    string City,

    [Required(ErrorMessage = "O estado é obrigatório")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "O estado deve ser a sigla de 2 letras")]
    string State,

    string? Reference,

    [Required(ErrorMessage = "O telefone é obrigatório")]
    [Phone(ErrorMessage = "Formato de telefone inválido")]
    string PhoneNumber,

    bool IsDefault
);