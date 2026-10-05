using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
using Libreria.Application.Ports.Primary; 
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Libreria.Application.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Categorias;

public class EditModel : PageModel
{
    private readonly ICrudRepository<Categoria> _repository;
    private readonly CategoriaValidator _validator;
    private readonly IUrlProtector _urlProtector; 

    public EditModel(
        CrudRepositoryFactory<Categoria> factory,
        CategoriaValidator validator,
        IUrlProtector urlProtector) 
    {
        _repository = factory.CrearRepositorio();
        _validator = validator;
        _urlProtector = urlProtector;
    }

    [BindProperty]
    public Guid PublicId { get; set; }

    [BindProperty]
    public CategoriaInput Input { get; set; } = new();

    public IReadOnlyList<string> Ubicaciones => UbicacionesCategoria.Todas;

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var publicIdDescifrado = _urlProtector.Descifrar(id);
        
        if (publicIdDescifrado == null)
        {
            return NotFound();
        }

        var categoria = await _repository.ObtenerPorPublicIdAsync(publicIdDescifrado.Value, true);
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
