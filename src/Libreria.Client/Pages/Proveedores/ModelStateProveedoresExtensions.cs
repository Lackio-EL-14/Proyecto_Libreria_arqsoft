using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Libreria.Client.Pages.Proveedores;

internal static class ModelStateProveedoresExtensions
{
    public static void AgregarErrores(
        this ModelStateDictionary modelState,
        IReadOnlyDictionary<string, string> errores)
    {
        foreach (var error in errores)
        {
            modelState.AddModelError(error.Key, error.Value);
        }
    }
}
