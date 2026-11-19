using Libreria.Servicio.Clientes.Application.Factories;
using Libreria.Servicio.Clientes.Application.Models;
using Libreria.Servicio.Clientes.Application.Ports.Primary;
using Libreria.Servicio.Clientes.Application.Ports.Secondary;
using Libreria.Servicio.Clientes.Application.Validators;
using Libreria.Servicio.Clientes.Domain;

namespace Libreria.Servicio.Clientes.Application.Services;

/// <summary>
/// Casos de uso de clientes. Pide su repositorio a la fábrica (Factory Method) y
/// devuelve <see cref="Result"/> en todo lo que es regla de negocio.
/// </summary>
public class ClienteService : IClienteService
{
    public const string MensajeNoEncontrado = "El cliente no existe.";
    public const string MensajeInactivo = "El cliente está inactivo. Reactívelo antes de editarlo.";

    private readonly IClienteRepository _repositorio;
    private readonly ClienteValidator _validador;

    public ClienteService(ClienteRepositoryFactory fabrica, ClienteValidator validador)
    {
        _repositorio = fabrica.CrearRepositorio();
        _validador = validador;
    }

    public async Task<IReadOnlyList<ClienteDetalle>> ListarAsync(FiltroClientes filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        var clientes = await _repositorio.ListarAsync(filtro);
        return clientes.Select(ClienteDetalle.Desde).ToList();
    }

    public async Task<Result<ClienteDetalle>> ObtenerAsync(Guid publicId)
    {
        var cliente = await _repositorio.ObtenerPorPublicIdAsync(publicId);

        return cliente is null
            ? Result<ClienteDetalle>.Fallo(Result.NoEncontrado(MensajeNoEncontrado))
            : ClienteDetalle.Desde(cliente);
    }

    public async Task<Result<ClienteDetalle>> RegistrarAsync(ClienteInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        _validador.Normalizar(input);

        var validacion = await _validador.ValidarAsync(input);
        if (!validacion.EsExito)
        {
            return Result<ClienteDetalle>.Fallo(validacion);
        }

        var cliente = new Cliente
        {
            PublicId = Guid.NewGuid(),
            CiNit = input.CiNit,
            RazonSocial = input.RazonSocial,
            Correo = input.Correo,
            Estado = true
        };

        try
        {
            await _repositorio.CrearAsync(cliente);
        }
        catch (ValorDuplicadoException)
        {
            // Otro usuario registró el mismo CI/NIT entre la validación y el INSERT.
            return CiNitDuplicado();
        }

        return await ObtenerAsync(cliente.PublicId);
    }

    public async Task<Result<ClienteDetalle>> ActualizarAsync(Guid publicId, ClienteInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var cliente = await _repositorio.ObtenerPorPublicIdAsync(publicId);
        if (cliente is null)
        {
            return Result<ClienteDetalle>.Fallo(Result.NoEncontrado(MensajeNoEncontrado));
        }

        if (!cliente.Estado)
        {
            return Result<ClienteDetalle>.Fallo(Result.Conflicto(string.Empty, MensajeInactivo));
        }

        _validador.Normalizar(input);
        var validacion = await _validador.ValidarAsync(input, publicId);
        if (!validacion.EsExito)
        {
            return Result<ClienteDetalle>.Fallo(validacion);
        }

        cliente.CiNit = input.CiNit;
        cliente.RazonSocial = input.RazonSocial;
        cliente.Correo = input.Correo;

        try
        {
            if (!await _repositorio.ActualizarAsync(cliente))
            {
                return Result<ClienteDetalle>.Fallo(Result.NoEncontrado(MensajeNoEncontrado));
            }
        }
        catch (ValorDuplicadoException)
        {
            return CiNitDuplicado();
        }

        return await ObtenerAsync(publicId);
    }

    public async Task<Result> CambiarEstadoAsync(Guid publicId, bool activo)
    {
        var cambiado = await _repositorio.CambiarEstadoAsync(publicId, activo);

        return cambiado
            ? Result.Exito()
            : Result.NoEncontrado(MensajeNoEncontrado);
    }

    private static Result<ClienteDetalle> CiNitDuplicado() =>
        Result<ClienteDetalle>.Fallo(
            Result.Conflicto(ClienteValidator.CampoCiNit, ClienteValidator.MensajeCiNitDuplicado));
}
