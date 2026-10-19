using System.Globalization;
using System.Text;
using Libreria.Application.Ports.Primary;
using Libreria.Application.Ports.Secondary;

namespace Libreria.Application.Services;

public class GeneradorNombreUsuario : IGeneradorNombreUsuario
{
    private readonly IUsuarioRepository _usuarioRepository;

    public GeneradorNombreUsuario(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task<string> GenerarAsync(string nombre, string apellido)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));

        if (string.IsNullOrWhiteSpace(apellido))
            throw new ArgumentException("El apellido es obligatorio.", nameof(apellido));

        var nombreNormalizado = Normalizar(nombre);
        var apellidoNormalizado = Normalizar(apellido);

        var nombreBase = $"{nombreNormalizado[0]}{apellidoNormalizado}";

        if (!await _usuarioRepository.ExisteNombreUsuarioAsync(nombreBase))
            return nombreBase;

        var numero = 2;

        while (await _usuarioRepository.ExisteNombreUsuarioAsync($"{nombreBase}{numero}"))
        {
            numero++;
        }

        return $"{nombreBase}{numero}";
    }

    private static string Normalizar(string texto)
    {
        var textoNormalizado = texto
            .Trim()
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormD);

        var resultado = new StringBuilder();

        foreach (var caracter in textoNormalizado)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(caracter);

            if (categoria == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(caracter))
                resultado.Append(caracter);
        }

        return resultado
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }
}