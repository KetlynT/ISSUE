using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Application.Services;
using GraficaModerna.Domain.Constants;
using GraficaModerna.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace GraficaModerna.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IContentService> _contentServiceMock;
    private readonly Mock<IPasswordHasher<ApplicationUser>> _passwordHasherMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        // Usamos null! para ignorar os argumentos do construtor que não são usados no mock
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _configurationMock = new Mock<IConfiguration>();
        _contentServiceMock = new Mock<IContentService>();
        _passwordHasherMock = new Mock<IPasswordHasher<ApplicationUser>>();
        _emailServiceMock = new Mock<IEmailService>();

        _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        Environment.SetEnvironmentVariable("JWT_SECRET_KEY", "UmaChaveMuitoSecretaEMuitoLongaParaTestesUnitarios123456");

        _service = new AuthService(
            _userManagerMock.Object,
            _configurationMock.Object,
            _contentServiceMock.Object,
            _passwordHasherMock.Object,
            _emailServiceMock.Object
        );
    }

    [Fact]
    public async Task LoginAsync_DeveRetornarTokens_QuandoCredenciaisSaoValidas()
    {
        // Arrange
        var email = "teste@email.com";
        var user = new ApplicationUser { Id = "user1", Email = email, UserName = email };

        _userManagerMock.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync(user);
        _userManagerMock.Setup(u => u.CheckPasswordAsync(user, "SenhaForte123")).ReturnsAsync(true);

        // CORREÇÃO IDE0028: Simplificação da lista
        _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync([Roles.User]);

        _userManagerMock.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        _contentServiceMock.Setup(c => c.GetSettingsAsync())
            .ReturnsAsync(new Dictionary<string, string> { { "purchase_enabled", "true" } });

        var loginDto = new LoginDto(email, "SenhaForte123");

        // Act
        var result = await _service.LoginAsync(loginDto);

        // Assert
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
        Assert.Equal(email, result.Email);
    }

    [Fact]
    public async Task LoginAsync_DeveBloquearUsuario_QuandoLojaEstaEmModoOrcamento_EUsuarioNaoEhAdmin()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", Email = "cliente@email.com" };

        _userManagerMock.Setup(u => u.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(u => u.CheckPasswordAsync(user, "123")).ReturnsAsync(true);

        // CORREÇÃO IDE0028: Simplificação da lista
        _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync([Roles.User]);

        _contentServiceMock.Setup(c => c.GetSettingsAsync())
            .ReturnsAsync(new Dictionary<string, string> { { "purchase_enabled", "false" } });

        var loginDto = new LoginDto(user.Email!, "123", IsAdminLogin: false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _service.LoginAsync(loginDto));
        Assert.Contains("modo orçamento", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_DeveFalhar_QuandoCpfInvalido()
    {
        var dto = new RegisterDto("Nome", "email@teste.com", "123456", "123456", "111.111.111-11", "11999999999");

        _contentServiceMock.Setup(c => c.GetSettingsAsync())
            .ReturnsAsync(new Dictionary<string, string> { { "purchase_enabled", "true" } });

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.RegisterAsync(dto));
        Assert.Contains("documento informado", ex.Message);
    }
}