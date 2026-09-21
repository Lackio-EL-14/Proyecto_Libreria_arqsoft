using Libreria.Web.Data.Factories;
using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Marcas;

public class IndexModel : PageModel
{
    private readonly ICrudRepository<Marca> _repository;

    public IndexModel(CrudRepositoryFactory<Marca> factory)
    {
        _repository = factory.CrearRepositorio();
    }

    public IReadOnlyList<Marca> Marcas { get; private set; } = [];
    public string? NombreBusqueda { get; private set; }

    public async Task OnGetAsync(string? nombre)
    {
        NombreBusqueda = nombre?.Trim();

        Marcas = await _repository.ObtenerActivasAsync(
            NombreBusqueda);
    }
}