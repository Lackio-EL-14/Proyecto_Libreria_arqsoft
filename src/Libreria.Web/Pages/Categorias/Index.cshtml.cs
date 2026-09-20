using System;
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
    public IReadOnlySet<Guid> CategoriasConProductosActivos { get; private set; } = new HashSet<Guid>();

    [BindProperty(SupportsGet = true)]
    public string? Busqueda { get; set; }

    public async Task OnGetAsync()
    {
        Categorias = await _repository.ObtenerActivasAsync(Busqueda);

        var categoriasConProductosActivos = new HashSet<Guid>();
        foreach (var categoria in Categorias)
        {
            if (await _repository.TieneRelacionesAsync(categoria.PublicId))
            {
                categoriasConProductosActivos.Add(categoria.PublicId);
            }
        }

        CategoriasConProductosActivos = categoriasConProductosActivos;
    }

    public async Task<IActionResult> OnPostDarDeBajaAsync(Guid publicId)
    {
        if (!await _repository.CambiarEstadoAsync(publicId, false))
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Categoría dada de baja correctamente.";
        return RedirectToPage("./Index");
    }
}
