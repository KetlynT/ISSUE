using FluentValidation;
using GraficaModerna.Domain.Entities;

namespace GraficaModerna.Application.Validators;

public class ContentPageValidator : AbstractValidator<ContentPage>
{
    public ContentPageValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(200).WithMessage("O título deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("O conteúdo da página não pode ser vazio.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("O slug é obrigatório.")
            .Matches("^[a-z0-9-]+$").WithMessage("O slug deve conter apenas letras minúsculas, números e hífens (ex: minha-pagina).");
    }
}