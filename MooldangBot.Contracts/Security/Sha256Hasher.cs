using System.Security.Cryptography;
using System.Text;

namespace MooldangBot.Contracts.Security;

public static class Sha256Hasher
{
    private const string Salt = "MooldangBot_Secure_Salt_2026";

    public static string ComputeHash(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        using var sha256 = SHA256.Create();
        var saltedInput = input + Salt;
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedInput));
        
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }
}
