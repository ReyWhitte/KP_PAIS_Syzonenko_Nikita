using System.Security.Cryptography;
namespace AgroGuid.API.Services;
public static class SessionTokens
{
    public static string Create() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public static string Hash(string token) => Convert.ToHexString(
        SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}
