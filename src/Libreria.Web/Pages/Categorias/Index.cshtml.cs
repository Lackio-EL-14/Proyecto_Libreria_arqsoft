using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Libreria.Web.Pages.Categorias.Models;
using Libreria.Web.Pages.Categorias.Repositories;

namespace Libreria.Web.Pages.Categorias
{
    public class IndexModel : PageModel
    {
        private readonly ICategoriaRepository _repository;

        public IndexModel(ICategoriaRepository repository)
        {
            _repository = repository;
        }

        public IEnumerable<Categoria> Categorias { get; private set; } = new List<Categoria>();

        [BindProperty(SupportsGet = true)]
        public string? Busqueda { get; set; }

        public async Task OnGetAsync()
        {
            Categorias = await _repository.ObtenerTodasAsync(Busqueda);
        }
    }
}
