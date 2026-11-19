using System.Net.Mail;
using Libreria.Servicio.Clientes.Domain;
using Libreria.Servicio.Clientes.Application.Factories;
using Libreria.Servicio.Clientes.Application.Models;
using Libreria.Servicio.Clientes.Application.Ports.Secondary;

namespace Libreria.Servicio.Clientes.Application.Validators;

/// <summary>
/// Reglas de negocio del cliente. Devuelve <see cref="Result"/>: los datos inválidos son
/// <see cref="TipoError.Validacion"/> y el CI/NIT repetido es <see cref="TipoError.Conflicto"/>.
/// </summary>
public class ClienteValidator
{
    public const string CampoCiNit = nameof(ClienteInput.CiNit);
    public const string CampoRazonSocial = nameof(ClienteInput.RazonSocial);
    public const string CampoCorreo = nameof(ClienteInput.Correo);

    public const string MensajeCiNitDuplicado = "Ya existe un cliente registrado con ese CI/NIT.";

    private const int LargoMaximoCiNit = 30;
    private const int LargoMaximoRazonSocial = 200;
    private const int LargoMaximoCorreo = 254;

    private readonly IClienteRepository _repositorio;

    public ClienteValidator(ClienteRepositoryFactory fabrica)
    {
        _repositorio = fabrica.CrearRepositorio();
    }

    public void Normalizar(ClienteInput input)
    {
        input.CiNit = input.CiNit?.Trim() ?? string.Empty;
        input.RazonSocial = NormalizarEspacios(input.RazonSocial ?? string.Empty);
        input.Correo = string.IsNullOrWhiteSpace(input.Correo) ? null : input.Correo.Trim();
    }

    /// <summary>
    /// Valida un input ya normalizado. <paramref name="excluirPublicId"/> es el propio
    /// cliente en la edición, para que no choque consigo mismo.
    /// </summary>
    public async Task<Result> ValidarAsync(ClienteInput input, Guid? excluirPublicId = null)
    {
        var errores = ValidarFormato(input);
        if (errores.Count > 0)
        {
            return Result.Validacion(errores);
        }

        var ciNitOcupado = await _repositorio.ExisteValorAsync(
            IClienteRepository.ColumnaCiNit, input.CiNit, excluirPublicId);

        return ciNitOcupado
            ? Result.Conflicto(CampoCiNit, MensajeCiNitDuplicado)
            : Result.Exito();
    }

    private static Dictionary<string, string> ValidarFormato(ClienteInput input)
    {
        var errores = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(input.CiNit))
        {
            errores[CampoCiNit] = "El CI/NIT es obligatorio.";
        }
        else if (input.CiNit.Length > LargoMaximoCiNit)
        {
            errores[CampoCiNit] = "El CI/NIT no puede superar los 30 caracteres.";
        }

        if (string.IsNullOrWhiteSpace(input.RazonSocial))
        {
            errores[CampoRazonSocial] = "La razón social es obligatoria.";
        }
        else if (input.RazonSocial.Length > LargoMaximoRazonSocial)
        {
            errores[CampoRazonSocial] = "La razón social no puede superar los 200 caracteres.";
        }

        if (input.Correo?.Length > LargoMaximoCorreo)
        {
            errores[CampoCorreo] = "El correo no puede superar los 254 caracteres.";
        }
        else if (input.Correo is not null && !EsCorreoValido(input.Correo))
        {
            errores[CampoCorreo] = "Ingrese un correo electrónico válido.";
        }

        return errores;
    }

    private static bool EsCorreoValido(string correo) =>
        MailAddress.TryCreate(correo, out var direccion) && direccion.Address == correo;

    private static string NormalizarEspacios(string texto) =>
        string.Join(' ', texto.Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
