using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace GraficaModerna.Infrastructure.Security;

public class MetadataSecurityService
{
    private readonly byte[] _encryptionKey;
    private readonly byte[] _hmacKey;

    public MetadataSecurityService(IConfiguration configuration)
    {
        var encKeyString = Environment.GetEnvironmentVariable("METADATA_ENC_KEY")
                           ?? configuration["Security:MetadataEncryptionKey"];
        var hmacKeyString = Environment.GetEnvironmentVariable("METADATA_HMAC_KEY")
                            ?? configuration["Security:MetadataHmacKey"];

        if (string.IsNullOrEmpty(encKeyString) || string.IsNullOrEmpty(hmacKeyString))
            throw new Exception("Chaves de segurança de metadados não configuradas.");

        _encryptionKey = Convert.FromBase64String(encKeyString);
        _hmacKey = Convert.FromBase64String(hmacKeyString);
    }

    public (string encryptedData, string signature) Protect(string plainText)
    {
        string encryptedData;

        using (var aes = Aes.Create())
        {
            aes.Key = _encryptionKey;
            aes.GenerateIV();
            var iv = aes.IV;

            using var ms = new MemoryStream();
            ms.Write(iv, 0, iv.Length);

            using (var encryptor = aes.CreateEncryptor(aes.Key, iv))
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            encryptedData = Convert.ToBase64String(ms.ToArray());
        }

        string signature;
        using (var hmac = new HMACSHA256(_hmacKey))
        {
            var dataBytes = Encoding.UTF8.GetBytes(encryptedData);
            var hashBytes = hmac.ComputeHash(dataBytes);
            signature = Convert.ToBase64String(hashBytes);
        }

        return (encryptedData, signature);
    }

    public string Unprotect(string encryptedData, string signature)
    {
        using (var hmac = new HMACSHA256(_hmacKey))
        {
            var dataBytes = Encoding.UTF8.GetBytes(encryptedData);
            var computedHash = hmac.ComputeHash(dataBytes);
            var computedSignature = Convert.ToBase64String(computedHash);

            if (computedSignature != signature)
                throw new System.Security.SecurityException("Assinatura dos metadados inválida.");
        }

        var fullCipher = Convert.FromBase64String(encryptedData);

        using var aes = Aes.Create();
        aes.Key = _encryptionKey;

        var iv = new byte[16];
        Array.Copy(fullCipher, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var ms = new MemoryStream(fullCipher, 16, fullCipher.Length - 16);
        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }
}