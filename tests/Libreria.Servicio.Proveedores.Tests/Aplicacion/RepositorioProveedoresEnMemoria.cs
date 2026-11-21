using Libreria.Servicio.Proveedores.Application.Factories;
using Libreria.Servicio.Proveedores.Application.Models;
using Libreria.Servicio.Proveedores.Application.Ports.Secondary;
using Libreria.Servicio.Proveedores.Domain;

namespace Libreria.Servicio.Proveedores.Tests.Aplicacion;

/// <summary>Puerto secundario en memoria para probar el servicio y el validador sin SQL Server.</summary>
internal sealed class RepositorioProveedoresEnMemoria : IProveedorRepository
{
    public List<Proveedor> Proveedores { get; } = [];

    /// <summary>Simula la carrera en que otro alta gana el índice único del NIT.</summary>
    public bool LanzarDuplicadoAlGuardar { get; set; }

    public Task<Proveedor?> ObtenerPorPublicIdAsync(Guid publicId) =>
        Task.FromResult(Proveedores.FirstOrDefault(p => p.PublicId == publicId));

    public Task<IReadOnlyList<Proveedor>> ListarAsync(FiltroProveedores filtro) =>
        Task.FromResult<IReadOnlyList<Proveedor>>(Proveedores
            .Where(p => filtro.Estado is null || p.Estado == filtro.Estado)
            .Where(p => filtro.Busqueda is null || p.RazonSocial.Contains(filtro.Busqueda, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.RazonSocial)
            .ToList());

    public Task CrearAsync(Proveedor entidad)
    {
        if (LanzarDuplicadoAlGuardar) throw new ValorDuplicadoException("Nit", new Exception());
        entidad.UsuarioCreacionId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
        Proveedores.Add(entidad);
        return Task.CompletedTask;
    }

    public Task<bool> ActualizarAsync(Proveedor entidad)
    {
        if (LanzarDuplicadoAlGuardar) throw new ValorDuplicadoException("Nit", new Exception());
        return Task.FromResult(Proveedores.Any(p => p.PublicId == entidad.PublicId));
    }

    public Task<bool> CambiarEstadoAsync(Guid publicId, bool nuevoEstado)
    {
        var proveedor = Proveedores.FirstOrDefault(p => p.PublicId == publicId && p.Estado != nuevoEstado);
        if (proveedor is null) return Task.FromResult(false);
        proveedor.Estado = nuevoEstado;
        return Task.FromResult(true);
    }

    public Task<bool> ExisteValorAsync(string columna, string valor, Guid? excluirPublicId = null) =>
        Task.FromResult(Proveedores.Any(p => p.Nit == valor && p.PublicId != excluirPublicId));

    public Task<IReadOnlyList<Proveedor>> ObtenerActivasAsync(string? busqueda = null) => throw new NotSupportedException();
    public Task<Proveedor?> ObtenerPorPublicIdAsync(Guid publicId, bool estadoEsperado) => throw new NotSupportedException();
    public Task<bool> TieneRelacionesAsync(Guid publicId) => throw new NotSupportedException();
}

internal sealed class FabricaEnMemoria(RepositorioProveedoresEnMemoria repositorio) : ProveedorRepositoryFactory
{
    public override IProveedorRepository CrearRepositorio() => repositorio;
}
