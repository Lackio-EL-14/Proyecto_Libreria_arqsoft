using Libreria.Servicio.Clientes.Application.Factories;
using Libreria.Servicio.Clientes.Application.Models;
using Libreria.Servicio.Clientes.Application.Ports.Secondary;
using Libreria.Servicio.Clientes.Domain;

namespace Libreria.Servicio.Clientes.Tests.Fakes;

/// <summary>Repositorio en memoria que imita las reglas de la tabla (CI/NIT único).</summary>
public sealed class ClienteRepositoryFalso : IClienteRepository
{
    public static readonly Guid UsuarioPrueba = Guid.Parse("a0000000-0000-0000-0000-000000000001");

    private readonly List<Cliente> _clientes = new();

    /// <summary>Simula que otro usuario ganó la carrera: el siguiente INSERT/UPDATE choca con 1062.</summary>
    public bool SimularDuplicadoEnEscritura { get; set; }

    public IReadOnlyList<Cliente> Clientes => _clientes;

    public Cliente Agregar(string ciNit, string razonSocial, bool estado = true, string? correo = null)
    {
        var cliente = new Cliente
        {
            ClienteId = _clientes.Count + 1,
            PublicId = Guid.NewGuid(),
            CiNit = ciNit,
            RazonSocial = razonSocial,
            Correo = correo,
            Estado = estado,
            FechaCreacion = DateTime.Now
        };
        _clientes.Add(cliente);
        return cliente;
    }

    public Task<IReadOnlyList<Cliente>> ListarAsync(FiltroClientes filtro)
    {
        var consulta = _clientes.Where(c => filtro.Estado is null || c.Estado == filtro.Estado);
        if (!string.IsNullOrWhiteSpace(filtro.Busqueda))
        {
            consulta = consulta.Where(c => c.CiNit.Contains(filtro.Busqueda) || c.RazonSocial.Contains(filtro.Busqueda));
        }

        return Task.FromResult<IReadOnlyList<Cliente>>(consulta.OrderBy(c => c.RazonSocial).ToList());
    }

    public Task<IReadOnlyList<Cliente>> ObtenerActivasAsync(string? busqueda = null) =>
        ListarAsync(new FiltroClientes { Busqueda = busqueda, Estado = true });

    public Task<Cliente?> ObtenerPorPublicIdAsync(Guid publicId) =>
        Task.FromResult(Copia(_clientes.SingleOrDefault(c => c.PublicId == publicId)));

    public Task<Cliente?> ObtenerPorPublicIdAsync(Guid publicId, bool estadoEsperado) =>
        Task.FromResult(Copia(_clientes.SingleOrDefault(c => c.PublicId == publicId && c.Estado == estadoEsperado)));

    public Task CrearAsync(Cliente entidad)
    {
        LanzarSiDuplicado(entidad.CiNit, excluir: null);
        entidad.ClienteId = _clientes.Count + 1;
        entidad.FechaCreacion = DateTime.Now;
        entidad.UsuarioCreacionId = UsuarioPrueba;
        _clientes.Add(entidad);
        return Task.CompletedTask;
    }

    public Task<bool> ActualizarAsync(Cliente entidad)
    {
        var actual = _clientes.SingleOrDefault(c => c.PublicId == entidad.PublicId && c.Estado);
        if (actual is null)
        {
            return Task.FromResult(false);
        }

        LanzarSiDuplicado(entidad.CiNit, excluir: entidad.PublicId);
        actual.CiNit = entidad.CiNit;
        actual.RazonSocial = entidad.RazonSocial;
        actual.Correo = entidad.Correo;
        actual.FechaModificacion = DateTime.Now;
        actual.UsuarioModificacionId = UsuarioPrueba;
        return Task.FromResult(true);
    }

    public Task<bool> CambiarEstadoAsync(Guid publicId, bool nuevoEstado)
    {
        var actual = _clientes.SingleOrDefault(c => c.PublicId == publicId);
        if (actual is null)
        {
            return Task.FromResult(false);
        }

        actual.Estado = nuevoEstado;
        actual.UsuarioModificacionId = UsuarioPrueba;
        return Task.FromResult(true);
    }

    public Task<bool> ExisteValorAsync(string columna, string valor, Guid? excluirPublicId = null) =>
        Task.FromResult(_clientes.Any(c =>
            string.Equals(c.CiNit, valor, StringComparison.OrdinalIgnoreCase) && c.PublicId != excluirPublicId));

    public Task<bool> TieneRelacionesAsync(Guid publicId) => Task.FromResult(false);

    private void LanzarSiDuplicado(string ciNit, Guid? excluir)
    {
        if (SimularDuplicadoEnEscritura || _clientes.Any(c => c.CiNit == ciNit && c.PublicId != excluir))
        {
            throw new ValorDuplicadoException(IClienteRepository.ColumnaCiNit);
        }
    }

    // Devuelve una copia, como haría la BD: el servicio no puede cambiar la «fila» sin llamar al repositorio.
    private static Cliente? Copia(Cliente? c) => c is null ? null : new Cliente
    {
        ClienteId = c.ClienteId,
        PublicId = c.PublicId,
        CiNit = c.CiNit,
        RazonSocial = c.RazonSocial,
        Correo = c.Correo,
        Estado = c.Estado,
        FechaCreacion = c.FechaCreacion,
        UsuarioCreacionId = c.UsuarioCreacionId,
        FechaModificacion = c.FechaModificacion,
        UsuarioModificacionId = c.UsuarioModificacionId
    };
}

/// <summary>Fábrica de prueba: siempre entrega el mismo repositorio en memoria.</summary>
public sealed class ClienteRepositoryFactoryFalsa : ClienteRepositoryFactory
{
    public ClienteRepositoryFactoryFalsa(ClienteRepositoryFalso repositorio)
    {
        Repositorio = repositorio;
    }

    public ClienteRepositoryFalso Repositorio { get; }

    public int VecesUsada { get; private set; }

    public override IClienteRepository CrearRepositorio()
    {
        VecesUsada++;
        return Repositorio;
    }
}
