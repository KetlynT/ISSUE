using GraficaModerna.Application.Validators;
using Xunit;

namespace GraficaModerna.Tests.Validators;

public class DocumentValidatorTests
{
    [Theory]
    [InlineData("111.111.111-11")] // Dígitos iguais (inválido apesar do cálculo)
    [InlineData("123.456.789-00")] // Dígito verificador errado
    [InlineData("")]
    [InlineData("abc")]
    public void IsValid_DeveRetornarFalso_ParaCpfsInvalidos(string cpf)
    {
        var result = DocumentValidator.IsValid(cpf);
        Assert.False(result, $"CPF {cpf} deveria ser inválido");
    }

    [Theory]
    [InlineData("00.000.000/0000-00")] // Zeros
    [InlineData("11.111.111/1111-11")] // Dígitos iguais
    public void IsValid_DeveRetornarFalso_ParaCnpjsInvalidos(string cnpj)
    {
        var result = DocumentValidator.IsValid(cnpj);
        Assert.False(result, $"CNPJ {cnpj} deveria ser inválido");
    }

    [Fact]
    public void IsValid_DeveRetornarVerdadeiro_ParaCpfValido()
    {
        // CPF gerado para teste (válido matematicamente)
        Assert.True(DocumentValidator.IsValid("36337625807"));
    }
}