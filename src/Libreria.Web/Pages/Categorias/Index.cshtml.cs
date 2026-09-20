using System.Collections.Generic;
using System.Threading.Tasks;
using Libreria.Web.Data.Factories;
using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Categorias;

public class IndexModel : PageModel
{
    private readonly ICrudRepository<Categoria> _repository;

    public IndexModel(CrudRepositoryFactory<Categoria> factory)
    {
        _repository = factory.CrearRepositorio();
    }

    public IReadOnlyList<Categoria> Categorias { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Busqueda { get; set; }

    public async Task OnGetAsync()
    {
        Categorias = await _repository.ObtenerActivasAsync(Busqueda);
    }
}
