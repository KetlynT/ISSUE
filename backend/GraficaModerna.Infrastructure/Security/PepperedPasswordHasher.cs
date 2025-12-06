using GraficaModerna.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace GraficaModerna.Infrastructure.Security
{
    public class PepperedPasswordHasher<TUser> : IPasswordHasher<TUser> where TUser : class
    {
        private readonly string _pepper;

        public PepperedPasswordHasher(IConfiguration configuration)
        {
            // CORREÇÃO: Lemos para uma variável temporária que aceita nulo (string?)
            string? tempPepper = Environment.GetEnvironmentVariable("PASSWORD_PEPPER")
                                 ?? configuration["Security:PasswordPepper"];

            // Validamos a variável temporária
            if (string.IsNullOrEmpty(tempPepper) || tempPepper.Length < 32)
            {
                throw new Exception("FATAL: PASSWORD_PEPPER não configurado ou inseguro (mínimo 32 caracteres).");
            }

            // Agora que sabemos que não é nulo, atribuímos ao campo definitivo
            _pepper = tempPepper;
        }

        public string HashPassword(TUser user, string password)
        {
            var passwordWithPepper = password + _pepper;
            return new PasswordHasher<TUser>().HashPassword(user, passwordWithPepper);
        }

        public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
        {
            var passwordWithPepper = providedPassword + _pepper;
            return new PasswordHasher<TUser>().VerifyHashedPassword(user, hashedPassword, passwordWithPepper);
        }
    }
}