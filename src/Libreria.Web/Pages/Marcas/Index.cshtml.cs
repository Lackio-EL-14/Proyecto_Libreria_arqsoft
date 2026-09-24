using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Libreria.Web.Data.Factories;
using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
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
    public IReadOnlySet<Guid> MarcasConProductosActivos { get; private set; } = new HashSet<Guid>();
    public string? NombreBusqueda { get; private set; }

    public async Task OnGetAsync(string? nombre)
    {
        NombreBusqueda = nombre?.Trim();
        Marcas = await _repository.ObtenerActivasAsync(NombreBusqueda);

        var marcasConProductos = new HashSet<Guid>();
        foreach (var marca in Marcas)
        {
            if (await _repository.TieneRelacionesAsync(marca.PublicId))
            {
                marcasConProductos.Add(marca.PublicId);
            }
        }
        MarcasConProductosActivos = marcasConProductos;
    }

    public async Task<IActionResult> OnPostDarDeBajaAsync(Guid publicId)
    {
        if (!await _repository.CambiarEstadoAsync(publicId, false))
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Marca dada de baja correctamente.";
        return RedirectToPage("./Index");
    }
}
