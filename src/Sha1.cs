
using System;

namespace Hyperlaunch;

public static class Sha1
{
    public static string Compute(string input)
    {
        using var sha1 = System.Security.Cryptography.SHA1.Create();
        var hashBytes = sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
    public static bool Verify(string input, string expectedHash)
    {
        var computedHash = Compute(input);
        return string.Equals(computedHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }
    public static bool Verify(byte[] input, string expectedHash)
    {
        using var sha1 = System.Security.Cryptography.SHA1.Create();
        var hashBytes = sha1.ComputeHash(input);
        var computedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        return string.Equals(computedHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }
}