using Libreria.Web.Pages.Marcas.Models;
using Libreria.Web.Pages.Marcas.Repositories;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Marcas;

public class IndexModel : PageModel
{
    private readonly IMarcaRepository _repository;

    public IndexModel(IMarcaRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<MarcaListItem> Marcas { get; private set; } = [];
    public string? NombreBusqueda { get; private set; }

    public void OnGet(string? nombre)
    {
        NombreBusqueda = nombre?.Trim();
        Marcas = _repository.ObtenerActivas(NombreBusqueda);
    }
}
