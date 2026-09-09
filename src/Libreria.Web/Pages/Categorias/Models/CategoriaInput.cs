namespace Libreria.Web.Pages.Categorias.Models;

public class CategoriaInput
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
}
