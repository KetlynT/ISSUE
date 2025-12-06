using GraficaModerna.Application.DTOs;

namespace GraficaModerna.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);

    // NOVO MÉTODO
    Task<AuthResponseDto> RefreshTokenAsync(TokenModel tokenModel);

    Task<UserProfileDto> GetProfileAsync(string userId);
    Task UpdateProfileAsync(string userId, UpdateProfileDto dto);
}