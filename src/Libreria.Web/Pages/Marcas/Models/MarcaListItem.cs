namespace Libreria.Web.Pages.Marcas.Models;

public record MarcaListItem(
    int MarcaId,
    string Nombre,
    string? Descripcion,
    string PaisOrigen,
    string? SitioWeb);
