using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Libreria.Web.Data.Factories;
using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Catalogs;
using Libreria.Web.Domain.Entities;
using Libreria.Web.Business.Validators;
using Libreria.Web.Pages.Categorias.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Categorias;

public class EditModel : PageModel
{
    private readonly ICrudRepository<Categoria> _repository;
    private readonly CategoriaValidator _validator;

    public EditModel(
        CrudRepositoryFactory<Categoria> factory,
        CategoriaValidator validator)
    {
        _repository = factory.CrearRepositorio();
        _validator = validator;
    }

    [BindProperty]
    public Guid PublicId { get; set; }

    [BindProperty]
    public CategoriaInput Input { get; set; } = new();

    public IReadOnlyList<string> Ubicaciones => UbicacionesCategoria.Todas;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var categoria = await _repository.ObtenerPorPublicIdAsync(id, true);
        if (categoria is null)
        {
            return NotFound();
        }

        PublicId = categoria.PublicId;
        Input = new CategoriaInput
        {
            Codigo = categoria.Codigo,
            Nombre = categoria.Nombre,
            Descripcion = categoria.Descripcion,
            Ubicacion = categoria.Ubicacion
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        _validator.Normalizar(Input);
        var errores = await _validator.ValidarEdicionAsync(Input, PublicId);
        AgregarErrores(errores);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var actualizada = await _repository.ActualizarAsync(new Categoria
        {
            PublicId = PublicId,
            Codigo = Input.Codigo,
            Nombre = Input.Nombre,
            Descripcion = Input.Descripcion,
            Ubicacion = Input.Ubicacion
        });

        if (!actualizada)
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Categoría actualizada correctamente.";
        return RedirectToPage("./Index");
    }

    private void AgregarErrores(IReadOnlyDictionary<string, string> errores)
    {
        foreach (var error in errores)
        {
            ModelState.AddModelError(error.Key, error.Value);
        }
    }
}
