namespace Libreria.Servicio.Productos.Application.Models.Historico;

public sealed record HistoricoProducto(
    Guid PublicId,
    string Nombre,
    bool Activo,
    IReadOnlyList<HistoricoCostoItem> Historial);