using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Libreria.Web.Pages.Categorias.Models;
using Libreria.Web.Pages.Categorias.Repositories;

namespace Libreria.Web.Pages.Categorias
{
    public class EditModel : PageModel
    {
        private readonly ICategoriaRepository _repository;

        public EditModel(ICategoriaRepository repository)
        {
            _repository = repository;
        }

        [BindProperty]
        public int CategoriaId { get; set; }

        [BindProperty]
        public CategoriaEditInputModel Input { get; set; } = new();

        public class CategoriaEditInputModel
        {
            [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
            [MaxLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
            public string Nombre { get; set; } = string.Empty;

            [MaxLength(255, ErrorMessage = "La descripción no puede exceder los 255 caracteres.")]
            public string? Descripcion { get; set; }

            [Required(ErrorMessage = "El orden es obligatorio.")]
            public int Orden { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var categoria = await _repository.ObtenerPorIdAsync(id);
            if (categoria == null)
            {
                return NotFound();
            }

            CategoriaId = categoria.CategoriaId;
            Input.Nombre = categoria.Nombre;
            Input.Descripcion = categoria.Descripcion;
            Input.Orden = categoria.Orden;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (await _repository.ExisteNombreAsync(Input.Nombre, CategoriaId))
            {
                ModelState.AddModelError("Input.Nombre", "El nombre de la categoría ya está en uso por otra categoría.");
                return Page();
            }

            var categoriaActualizada = new Categoria
            {
                CategoriaId = CategoriaId,
                Nombre = Input.Nombre,
                Descripcion = Input.Descripcion,
                Orden = Input.Orden
            };

            await _repository.ActualizarAsync(categoriaActualizada);
            
            TempData["MensajeExito"] = "Categoría actualizada correctamente.";
            return RedirectToPage("./Index");
        }
    }
}
