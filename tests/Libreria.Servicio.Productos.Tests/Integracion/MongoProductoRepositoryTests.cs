using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Infrastructure.Mongo;
using Libreria.Servicio.Productos.Infrastructure.Repositories;
using MongoDB.Driver;

namespace Libreria.Servicio.Productos.Tests.Integracion;

public sealed class MongoProductoRepositoryTests
{
    private static readonly Guid ProductoSemilla =
        Guid.Parse("d0000000-0000-0000-0000-000000000001");

    private static readonly Guid CategoriaSemilla =
        Guid.Parse("ca000000-0000-0000-0000-000000000001");

    private static readonly Guid MarcaSemilla =
        Guid.Parse("b0000000-0000-0000-0000-000000000001");

    [MongoFact]
    public async Task Producto_semilla_se_deserializa_con_guid_y_embebidos()
    {
        MongoSerialization.Configurar();

        var cadena =
            Environment.GetEnvironmentVariable(
                MongoFactAttribute.Variable)!;

        var mongoUrl = new MongoUrl(cadena);

        var cliente = new MongoClient(mongoUrl);

        var database = cliente.GetDatabase(
            mongoUrl.DatabaseName);

        var context = new MongoContext(database);

        var repository = new MongoProductoRepository(
            context,
            new UsuarioFijo());

        var producto =
            await repository.ObtenerPorPublicIdAsync(
                ProductoSemilla,
                true);

        Assert.NotNull(producto);

        Assert.Equal(
            "Cuaderno Espiral 100 hojas",
            producto.Nombre);

        Assert.Equal(150, producto.Stock);
        Assert.Equal(15m, producto.PrecioVenta);
        Assert.Equal(10m, producto.CostoAdquisicionActual);

        Assert.Equal(
            CategoriaSemilla,
            producto.Categoria.PublicId);

        Assert.Equal(
            "Material escolar",
            producto.Categoria.Nombre);

        Assert.Equal(
            MarcaSemilla,
            producto.Marca.PublicId);

        Assert.Equal(
            "Faber-Castell",
            producto.Marca.Nombre);

        var historico =
            Assert.Single(producto.HistoricoCostos);

        Assert.Equal(
            10m,
            historico.CostoAdquisicion);

        Assert.Equal(
            "Registro inicial",
            historico.Motivo);
    }

    private sealed class UsuarioFijo : IUsuarioActual
    {
        public Guid? UsuarioId =>
            Guid.Parse(
                "a0000000-0000-0000-0000-000000000001");

        public string? NombreUsuario => "admin";

        public string? Iniciales => "A.";
    }
}
