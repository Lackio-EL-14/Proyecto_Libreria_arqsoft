using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using System.Net.Mail;

namespace Libreria.Application.Validators;

public class ClienteValidator
{
    private readonly IClienteRepository _repository;

    public ClienteValidator(IClienteRepository repository)
    {
        _repository = repository;
    }

    public void Normalizar(ClienteInput input)
    {
        input.CiNit = input.CiNit?.Trim() ?? string.Empty;
        input.RazonSocial = NormalizarTexto(input.RazonSocial ?? string.Empty);
        input.Correo = input.Correo?.Trim();

        if (string.IsNullOrWhiteSpace(input.Correo))
        {
            input.Correo = null;
        }
    }

    public async Task<IReadOnlyDictionary<string, string>> ValidarAsync(
        ClienteInput input)
    {
        var errores = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(input.CiNit))
        {
            errores["Input.CiNit"] =
                "El CI/NIT es obligatorio.";
        }
        else if (await _repository.ExisteCiNitAsync(input.CiNit))
        {
            errores["Input.CiNit"] =
                "Ya existe un cliente registrado con ese CI/NIT.";
        }

        if (string.IsNullOrWhiteSpace(input.RazonSocial))
        {
            errores["Input.RazonSocial"] =
                "La razón social es obligatoria.";
        }

        if (input.Correo is not null &&
            !EsCorreoValido(input.Correo))
        {
            errores["Input.Correo"] =
                "Ingrese un correo electrónico válido.";
        }

        return errores;
    }

    private static bool EsCorreoValido(string correo)
    {
        try
        {
            var direccion = new MailAddress(correo);
            return direccion.Address == correo;
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizarTexto(string texto)
    {
        return string.Join(
            ' ',
            texto.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries));
    }
}