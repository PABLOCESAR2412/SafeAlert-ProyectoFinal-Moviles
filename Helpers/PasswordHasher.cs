using System.Security.Cryptography;
using System.Text;

namespace SafeAlert.Helpers;

public static class PasswordHasher
{
    private const string Pepper = "SafeAlert::UISRAEL::2026";

    public static string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(Pepper + password));
        return Convert.ToHexString(bytes);
    }

    public static bool Verify(string password, string hash) =>
        string.Equals(Hash(password), hash, StringComparison.OrdinalIgnoreCase);
}
