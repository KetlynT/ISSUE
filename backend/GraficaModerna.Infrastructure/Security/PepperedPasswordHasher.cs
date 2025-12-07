using GraficaModerna.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace GraficaModerna.Infrastructure.Security;

// Classe de configuração para mapear o appsettings
public class PepperSettings
{
    public string ActiveVersion { get; set; } = string.Empty;
    public Dictionary<string, string> Peppers { get; set; } = [];
}

public class PepperedPasswordHasher(IOptions<PepperSettings> options) : PasswordHasher<ApplicationUser>
{
    private readonly PepperSettings _settings = options.Value;

    public override string HashPassword(ApplicationUser user, string password)
    {
        var activeVersion = _settings.ActiveVersion;

        if (!_settings.Peppers.TryGetValue(activeVersion, out var pepper))
        {
            throw new InvalidOperationException($"A versão ativa do pepper '{activeVersion}' não foi encontrada na configuração.");
        }

        // 1. Concatena a senha com o Pepper ATUAL
        // 2. Usa o algoritmo padrão do Identity (PBKDF2/Argon2) para gerar o hash seguro
        var hash = base.HashPassword(user, password + pepper);

        // 3. Armazena no formato: $v{Versao}${HashBase64}
        return $"$v{activeVersion}${hash}";
    }

    public override PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string providedPassword)
    {
        string version;
        string actualHash;

        // 1. Tenta extrair a versão do hash armazenado
        if (hashedPassword.StartsWith("$v"))
        {
            // Formato esperado: $v1$HASH_AQUI...
            // O Split separa em: [v1, HASH_AQUI...]
            var parts = hashedPassword.Split('$', 3, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2) 
            {
                return PasswordVerificationResult.Failed;
            }

            version = parts[0]; 
            actualHash = parts[1];
        }
        else
        {
            // Fallback para Legacy: Se não tiver prefixo $v, assume que é da versão inicial (ex: "v1")
            // Isso garante que as senhas antigas continuem funcionando após o deploy
            version = "v1";
            actualHash = hashedPassword;
        }

        // 2. Busca o pepper correspondente à versão do hash (não necessariamente o ativo)
        if (!_settings.Peppers.TryGetValue(version, out var pepper))
        {
            // Se a chave dessa versão foi excluída do config, a senha é inválida
            return PasswordVerificationResult.Failed;
        }

        // 3. Verifica a senha usando o pepper histórico
        var verificationResult = base.VerifyHashedPassword(user, actualHash, providedPassword + pepper);

        // 4. Rotação Automática (Rehash)
        // Se a senha está correta, MAS a versão usada é diferente da ativa,
        // avisamos o Identity para refazer o hash com a nova chave.
        if (verificationResult == PasswordVerificationResult.Success && version != _settings.ActiveVersion)
        {
            return PasswordVerificationResult.SuccessRehashNeeded;
        }

        return verificationResult;
    }
}