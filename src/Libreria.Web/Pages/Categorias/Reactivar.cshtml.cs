using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Libreria.Web.Pages.Categorias.Models;
using Libreria.Web.Pages.Categorias.Repositories;

namespace Libreria.Web.Pages.Categorias
{
    public class ReactivarModel : PageModel
    {
        private readonly ICategoriaRepository _repository;

        public ReactivarModel(ICategoriaRepository repository)
        {
            _repository = repository;
        }

        [BindProperty]
        public int CategoriaId { get; set; }

        public Categoria? Categoria { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Categoria = await _repository.ObtenerPorIdAsync(id);
            if (Categoria == null)
            {
                return NotFound();
            }

            CategoriaId = Categoria.CategoriaId;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await _repository.ReactivarAsync(CategoriaId);
            
            TempData["MensajeExito"] = "Categoría reactivada correctamente.";
            return RedirectToPage("./Index");
        }
    }
}
