
using System;
using System.Security.Cryptography;

namespace Hyperlaunch;

public static class Sha1
{
    public static string Compute(string input)
    {
        Span<byte> hashBytes = stackalloc byte[20];
        SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(input), hashBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
    public static string Compute(byte[] input)
    {
        Span<byte> hashBytes = stackalloc byte[20];
        SHA1.HashData(input, hashBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static bool Verify(string input, string expectedHash)
    {
        Span<byte> hashBytes = stackalloc byte[20];
        SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(input), hashBytes);
        return EqualsHex(hashBytes, expectedHash);
    }
    public static bool Verify(byte[] input, string expectedHash)
    {
        Span<byte> hashBytes = stackalloc byte[20];
        SHA1.HashData(input, hashBytes);
        return EqualsHex(hashBytes, expectedHash);
    }

    private static bool EqualsHex(ReadOnlySpan<byte> hash, ReadOnlySpan<char> hex)
    {
        if (hex.Length != hash.Length * 2) return false;
        for (int i = 0; i < hash.Length; i++)
        {
            int hi = HexVal(hex[i * 2]);
            int lo = HexVal(hex[i * 2 + 1]);
            if (hi < 0 || lo < 0) return false;
            if (hash[i] != (byte)((hi << 4) | lo)) return false;
        }
        return true;
    }

    private static int HexVal(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1,
    };
}