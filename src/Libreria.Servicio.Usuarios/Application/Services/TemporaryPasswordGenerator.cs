using System.Security.Cryptography;

namespace Libreria.Servicio.Usuarios.Application.Services;

public sealed class TemporaryPasswordGenerator
{
    private const string UpperChars = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
    private const string LowerChars = "abcdefghijkmnopqrstuvwxyz";
    private const string DigitChars = "23456789";
    private const string SpecialChars = "!@#$%*-_+";
    private const string AllChars = UpperChars + LowerChars + DigitChars + SpecialChars;
    private const int PasswordLength = 12;

    public string Generate()
    {
        var chars = new char[PasswordLength];
        FillRequiredCategories(chars);
        FillRemaining(chars);
        Shuffle(chars);
        return new string(chars);
    }

    private static void FillRequiredCategories(char[] chars)
    {
        chars[0] = GetRandomChar(UpperChars);
        chars[1] = GetRandomChar(LowerChars);
        chars[2] = GetRandomChar(DigitChars);
        chars[3] = GetRandomChar(SpecialChars);
    }

    private static void FillRemaining(char[] chars)
    {
        for (var i = 4; i < chars.Length; i++)
        {
            chars[i] = GetRandomChar(AllChars);
        }
    }

    private static void Shuffle(char[] chars)
    {
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
    }

    private static char GetRandomChar(string charSet)
    {
        var index = RandomNumberGenerator.GetInt32(charSet.Length);
        return charSet[index];
    }
}
