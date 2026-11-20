using System.Globalization;
using System.Text;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;

namespace Libreria.Servicio.Usuarios.Application.Services;

public sealed class UsernameGenerator
{
    private readonly IUserRepository _userRepository;

    public UsernameGenerator(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<string> GenerateAsync(
        string firstName,
        string paternalLastName,
        string? maternalLastName = null)
    {
        ValidateRequiredInputs(firstName, paternalLastName);

        var baseUsername = BuildBaseUsername(firstName, paternalLastName, maternalLastName);

        return await ResolveCollisionAsync(baseUsername);
    }

    private static void ValidateRequiredInputs(string firstName, string paternalLastName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(paternalLastName);
    }

    private static string BuildBaseUsername(
        string firstName,
        string paternalLastName,
        string? maternalLastName)
    {
        var firstLetter = ExtractFirstLetter(firstName);
        var normalizedPaternal = Normalize(paternalLastName);
        var maternalSuffix = ExtractOptionalInitial(maternalLastName);

        EnsureValidComponents(firstLetter, normalizedPaternal);

        return $"{firstLetter}{normalizedPaternal}{maternalSuffix}";
    }

    private static void EnsureValidComponents(string firstLetter, string normalizedPaternal)
    {
        if (firstLetter.Length == 0 || normalizedPaternal.Length == 0)
        {
            throw new ArgumentException(
                "El nombre y el apellido deben contener caracteres válidos.");
        }
    }

    private static string ExtractFirstLetter(string value)
    {
        var normalized = Normalize(value);
        return normalized.Length > 0 ? normalized[..1] : string.Empty;
    }

    private static string ExtractOptionalInitial(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return ExtractFirstLetter(value);
    }

    private async Task<string> ResolveCollisionAsync(string baseUsername)
    {
        if (!await _userRepository.UsernameExistsAsync(baseUsername))
        {
            return baseUsername;
        }

        return await FindNextAvailableUsernameAsync(baseUsername);
    }

    private async Task<string> FindNextAvailableUsernameAsync(string baseUsername)
    {
        var suffix = 2;

        while (await _userRepository.UsernameExistsAsync($"{baseUsername}{suffix}"))
        {
            suffix++;
        }

        return $"{baseUsername}{suffix}";
    }

    private static string Normalize(string value)
    {
        var normalized = value
            .Trim()
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormD);

        var result = new StringBuilder();
        AppendValidCharacters(normalized, result);

        return result
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }

    private static void AppendValidCharacters(string source, StringBuilder builder)
    {
        foreach (var character in source)
        {
            AppendIfLetterOrDigit(character, builder);
        }
    }

    private static void AppendIfLetterOrDigit(char character, StringBuilder builder)
    {
        var category = CharUnicodeInfo.GetUnicodeCategory(character);

        if (category == UnicodeCategory.NonSpacingMark)
        {
            return;
        }

        if (char.IsLetterOrDigit(character))
        {
            builder.Append(character);
        }
    }
}
