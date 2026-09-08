namespace Libreria.Web.Pages.Marcas.Models;

public class MarcaInput
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string PaisOrigen { get; set; } = string.Empty;
    public string? SitioWeb { get; set; }
}
