using Libreria.Servicio.Proveedores.Application.Factories;
using Libreria.Servicio.Proveedores.Application.Models;
using Libreria.Servicio.Proveedores.Application.Ports.Primary;
using Libreria.Servicio.Proveedores.Application.Ports.Secondary;
using Libreria.Servicio.Proveedores.Application.Validators;
using Libreria.Servicio.Proveedores.Domain;

namespace Libreria.Servicio.Proveedores.Application.Services;

/// <summary>Fachada de aplicación del agregado Proveedor; coordina validación y persistencia.</summary>
public sealed class ProveedorService : IProveedorService
{
    private const string NoEncontrado = "El proveedor no existe.";
    private const string Inactivo = "El proveedor está inactivo. Reactívelo antes de editarlo.";
    private readonly IProveedorRepository _repositorio;
    private readonly ProveedorValidator _validador;

    public ProveedorService(ProveedorRepositoryFactory fabrica, ProveedorValidator validador)
    {
        _repositorio = fabrica.CrearRepositorio();
        _validador = validador;
    }

    public async Task<IReadOnlyList<ProveedorDetalle>> ListarAsync(FiltroProveedores filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        return (await _repositorio.ListarAsync(filtro)).Select(ProveedorDetalle.Desde).ToList();
    }

    public async Task<Result<ProveedorDetalle>> ObtenerAsync(Guid publicId)
    {
        var proveedor = await _repositorio.ObtenerPorPublicIdAsync(publicId);
        return proveedor is null
            ? Result<ProveedorDetalle>.Fallo(Result.NoEncontrado(NoEncontrado))
            : ProveedorDetalle.Desde(proveedor);
    }

    public async Task<Result<ProveedorDetalle>> RegistrarAsync(ProveedorInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        _validador.Normalizar(input);
        var validacion = await _validador.ValidarAsync(input);
        if (!validacion.EsExito) return Result<ProveedorDetalle>.Fallo(validacion);

        var proveedor = CrearEntidad(input);
        try { await _repositorio.CrearAsync(proveedor); }
        catch (ValorDuplicadoException) { return NitDuplicado(); }
        return await ObtenerAsync(proveedor.PublicId);
    }

    public async Task<Result<ProveedorDetalle>> ActualizarAsync(Guid publicId, ProveedorInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var proveedor = await _repositorio.ObtenerPorPublicIdAsync(publicId);
        if (proveedor is null) return Result<ProveedorDetalle>.Fallo(Result.NoEncontrado(NoEncontrado));
        if (!proveedor.Estado) return Result<ProveedorDetalle>.Fallo(Result.Conflicto(string.Empty, Inactivo));

        _validador.Normalizar(input);
        var validacion = await _validador.ValidarAsync(input, publicId);
        if (!validacion.EsExito) return Result<ProveedorDetalle>.Fallo(validacion);

        Aplicar(proveedor, input);
        try
        {
            if (!await _repositorio.ActualizarAsync(proveedor))
                return Result<ProveedorDetalle>.Fallo(Result.NoEncontrado(NoEncontrado));
        }
        catch (ValorDuplicadoException) { return NitDuplicado(); }

        return await ObtenerAsync(publicId);
    }

    public async Task<Result> CambiarEstadoAsync(Guid publicId, bool activo) =>
        await _repositorio.CambiarEstadoAsync(publicId, activo)
            ? Result.Exito()
            : Result.NoEncontrado(NoEncontrado);

    private static Proveedor CrearEntidad(ProveedorInput input)
    {
        var proveedor = new Proveedor { PublicId = Guid.NewGuid(), Estado = true };
        Aplicar(proveedor, input);
        return proveedor;
    }

    private static void Aplicar(Proveedor proveedor, ProveedorInput input)
    {
        proveedor.Nit = input.Nit;
        proveedor.RazonSocial = input.RazonSocial;
        proveedor.Telefono = input.Telefono;
        proveedor.Correo = input.Correo;
        proveedor.ContactoPrincipal = new ContactoProveedor
        {
            ProveedorId = proveedor.ProveedorId,
            Nombre = input.NombreContacto,
            Cargo = input.CargoContacto,
            Telefono = input.TelefonoContacto,
            Correo = input.CorreoContacto,
            EsPrincipal = true
        };
    }

    private static Result<ProveedorDetalle> NitDuplicado() =>
        Result<ProveedorDetalle>.Fallo(Result.Conflicto(ProveedorValidator.CampoNit, ProveedorValidator.MensajeNitDuplicado));
}
