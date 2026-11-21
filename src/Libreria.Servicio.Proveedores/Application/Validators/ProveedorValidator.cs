using System.Net.Mail;
using Libreria.Servicio.Proveedores.Domain;
using Libreria.Servicio.Proveedores.Application.Factories;
using Libreria.Servicio.Proveedores.Application.Models;
using Libreria.Servicio.Proveedores.Application.Ports.Secondary;

namespace Libreria.Servicio.Proveedores.Application.Validators;

public sealed class ProveedorValidator
{
    public const string CampoNit = nameof(ProveedorInput.Nit);
    public const string MensajeNitDuplicado = "Ya existe un proveedor registrado con ese NIT.";
    private readonly IProveedorRepository _repositorio;

    public ProveedorValidator(ProveedorRepositoryFactory fabrica) => _repositorio = fabrica.CrearRepositorio();

    public void Normalizar(ProveedorInput input)
    {
        input.Nit = input.Nit?.Trim() ?? string.Empty;
        input.RazonSocial = Espacios(input.RazonSocial);
        input.Telefono = Opcional(input.Telefono);
        input.Correo = Opcional(input.Correo);
        input.NombreContacto = Espacios(input.NombreContacto);
        input.CargoContacto = Opcional(input.CargoContacto);
        input.TelefonoContacto = Opcional(input.TelefonoContacto);
        input.CorreoContacto = Opcional(input.CorreoContacto);
    }

    public async Task<Result> ValidarAsync(ProveedorInput input, Guid? excluirPublicId = null)
    {
        var errores = ValidarFormato(input);
        if (errores.Count > 0) return Result.Validacion(errores);

        return await _repositorio.ExisteValorAsync(IProveedorRepository.ColumnaNit, input.Nit, excluirPublicId)
            ? Result.Conflicto(CampoNit, MensajeNitDuplicado)
            : Result.Exito();
    }

    private static Dictionary<string, string> ValidarFormato(ProveedorInput input)
    {
        var errores = new Dictionary<string, string>();
        RequeridoMaximo(errores, nameof(input.Nit), input.Nit, "El NIT", 30);
        RequeridoMaximo(errores, nameof(input.RazonSocial), input.RazonSocial, "La razón social", 200, femenino: true);
        OpcionalMaximo(errores, nameof(input.Telefono), input.Telefono, "El teléfono", 30);
        ValidarCorreo(errores, nameof(input.Correo), input.Correo, "El correo del proveedor");
        RequeridoMaximo(errores, nameof(input.NombreContacto), input.NombreContacto, "El nombre del contacto", 150);
        OpcionalMaximo(errores, nameof(input.CargoContacto), input.CargoContacto, "El cargo del contacto", 100);
        OpcionalMaximo(errores, nameof(input.TelefonoContacto), input.TelefonoContacto, "El teléfono del contacto", 30);
        ValidarCorreo(errores, nameof(input.CorreoContacto), input.CorreoContacto, "El correo del contacto");

        if (string.IsNullOrWhiteSpace(input.TelefonoContacto) && string.IsNullOrWhiteSpace(input.CorreoContacto))
        {
            errores[nameof(input.TelefonoContacto)] = "Ingrese al menos un teléfono o correo para el contacto principal.";
        }

        return errores;
    }

    private static void RequeridoMaximo(IDictionary<string, string> errores, string campo, string? valor, string etiqueta, int maximo, bool femenino = false)
    {
        if (string.IsNullOrWhiteSpace(valor)) errores[campo] = $"{etiqueta} es {(femenino ? "obligatoria" : "obligatorio")}.";
        else if (valor.Length > maximo) errores[campo] = $"{etiqueta} no puede superar los {maximo} caracteres.";
    }

    private static void OpcionalMaximo(IDictionary<string, string> errores, string campo, string? valor, string etiqueta, int maximo)
    {
        if (valor?.Length > maximo) errores[campo] = $"{etiqueta} no puede superar los {maximo} caracteres.";
    }

    private static void ValidarCorreo(IDictionary<string, string> errores, string campo, string? valor, string etiqueta)
    {
        if (valor?.Length > 254) errores[campo] = $"{etiqueta} no puede superar los 254 caracteres.";
        else if (valor is not null && (!MailAddress.TryCreate(valor, out var direccion) || direccion.Address != valor))
            errores[campo] = $"{etiqueta} no es válido.";
    }

    private static string Espacios(string? texto) => string.Join(' ', (texto ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    private static string? Opcional(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
