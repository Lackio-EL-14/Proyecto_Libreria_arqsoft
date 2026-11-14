using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Libreria.Client.Pages.Clientes;

internal static class ModelStateClientesExtensions
{
    // Las claves vienen ya como "Input.CiNit" (o vacía para el resumen) desde ResultadoApi.
    public static void AgregarErrores(this ModelStateDictionary modelState, IReadOnlyDictionary<string, string> errores)
    {
        foreach (var error in errores)
        {
            modelState.AddModelError(error.Key, error.Value);
        }
    }
}
