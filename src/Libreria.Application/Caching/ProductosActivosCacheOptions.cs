namespace Libreria.Application.Caching;

public sealed class ProductosActivosCacheOptions
{
    public const string SectionName = "Cache:ProductosActivos";

    public int ExpiracionMinutos { get; set; } = 5;
}
