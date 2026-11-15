using System.Globalization;
using System.Text;
using Libreria.Servicio.Usuarios.Application.Abstractions;

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
        string lastName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        var normalizedFirstName = Normalize(firstName);
        var normalizedLastName = Normalize(lastName);

        if (normalizedFirstName.Length == 0 ||
            normalizedLastName.Length == 0)
        {
            throw new ArgumentException(
                "El nombre y el apellido deben contener caracteres válidos.");
        }

        var baseUsername =
            $"{normalizedFirstName[0]}{normalizedLastName}";

        if (!await _userRepository
                .UsernameExistsAsync(baseUsername))
        {
            return baseUsername;
        }

        var suffix = 2;

        while (await _userRepository.UsernameExistsAsync(
                   $"{baseUsername}{suffix}"))
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

        foreach (var character in normalized)
        {
            var category =
                CharUnicodeInfo.GetUnicodeCategory(character);

            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                result.Append(character);
            }
        }

        return result
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }
}