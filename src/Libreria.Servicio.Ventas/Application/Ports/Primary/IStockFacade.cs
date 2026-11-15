namespace Libreria.Servicio.Ventas.Application.Ports.Primary
{
    public interface IStockFacade
    {
        Task DescontarStockAsync(
            Guid productoPublicId,
            int cantidad,
            Guid ventaPublicId);
        Task RestaurarStockAsync(
            Guid productoPublicId,
            int cantidad,
            Guid ventaPublicId);
    }
}
