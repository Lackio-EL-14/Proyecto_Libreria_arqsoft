using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Libreria.Web.Pages.Categorias.Models;
using Libreria.Web.Pages.Categorias.Repositories;

namespace Libreria.Web.Pages.Categorias
{
    public class CreateModel : PageModel
    {
        private readonly ICategoriaRepository _repository;

        public CreateModel(ICategoriaRepository repository)
        {
            _repository = repository;
        }

        [BindProperty]
        public CategoriaInputModel Input { get; set; } = new();

        public class CategoriaInputModel
        {
            [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
            [MaxLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
            public string Nombre { get; set; } = string.Empty;

            [MaxLength(255, ErrorMessage = "La descripción no puede exceder los 255 caracteres.")]
            public string? Descripcion { get; set; }

            [Required(ErrorMessage = "El orden es obligatorio.")]
            public int Orden { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (await _repository.ExisteNombreAsync(Input.Nombre))
            {
                ModelState.AddModelError("Input.Nombre", "El nombre de la categoría ya existe. Ingresa uno diferente.");
                return Page();
            }

            var nuevaCategoria = new Categoria
            {
                Nombre = Input.Nombre,
                Descripcion = Input.Descripcion,
                Orden = Input.Orden
            };

            await _repository.CrearAsync(nuevaCategoria);
            
            TempData["MensajeExito"] = "Categoría registrada exitosamente.";
            
            return RedirectToPage("./Index");
        }
    }
}
