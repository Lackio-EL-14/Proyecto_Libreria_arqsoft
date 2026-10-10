using System.Data.Common;
using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Validators;
using Microsoft.Extensions.Logging;

namespace Libreria.Application.Services;

public class ClienteService : IClienteService
{
    private readonly IClienteRepository _repository;
    private readonly ClienteValidator _validator;
    private readonly ILogger<ClienteService> _logger;

    public ClienteService(
        IClienteRepository repository,
        ClienteValidator validator,
        ILogger<ClienteService> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ClienteRegistroResult> RegistrarRapidoAsync(ClienteInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        _validator.Normalizar(input);

        IReadOnlyDictionary<string, string> errores;
        try
        {
            errores = await _validator.ValidarAsync(input);
        }
        catch (DbException error)
        {
            _logger.LogError(error, "No se pudo validar el alta de cliente.");
            return ErrorDeRegistro();
        }

        if (errores.Count > 0)
        {
            return ClienteRegistroResult.Invalido(errores);
        }

        var cliente = new Cliente
        {
            CiNit = input.CiNit,
            RazonSocial = input.RazonSocial,
            Correo = input.Correo
        };

        try
        {
            var creado = await _repository.CrearRapidoAsync(cliente);
            return ClienteRegistroResult.Correcto(
                new ClienteVentaItem(creado.CiNit, creado.RazonSocial));
        }
        catch (DbException error)
        {
            _logger.LogWarning(error, "No se pudo completar el alta de cliente.");

            // Otro vendedor puede haber registrado el mismo CI/NIT después de validarlo.
            try
            {
                if (await _repository.ExisteCiNitAsync(input.CiNit))
                {
                    return ClienteRegistroResult.Invalido(new Dictionary<string, string>
                    {
                        ["Input.CiNit"] = "Ya existe un cliente registrado con ese CI/NIT."
                    });
                }
            }
            catch (DbException errorConsulta)
            {
                _logger.LogWarning(errorConsulta, "No se pudo comprobar el CI/NIT después del fallo de registro.");
            }

            return ErrorDeRegistro();
        }
    }

    private static ClienteRegistroResult ErrorDeRegistro() =>
        ClienteRegistroResult.Invalido(new Dictionary<string, string>
        {
            [string.Empty] = "No se pudo registrar el cliente. Revise la conexión e intente nuevamente."
        });
}
