using Libreria.Web.Pages.Categorias.Models;
using Libreria.Web.Pages.Categorias.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Categorias;

public class IndexModel : PageModel
{
    private readonly IListadoCategoriasRepository _repository;

    public IndexModel(IListadoCategoriasRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<Categoria> Categorias { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Busqueda { get; set; }

    public async Task OnGetAsync()
    {
        Categorias = await _repository.ObtenerActivasAsync(Busqueda);
    }
}
