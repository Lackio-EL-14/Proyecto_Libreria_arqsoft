namespace Libreria.Client.Pages.Shared.Models;

public sealed record MenuModuleViewModel(
    string CurrentPage,
    bool EsAdministrador,
    bool PuedeVender)
{
    public string ActiveClass(string path)
    {
        if (path == "/Index" && CurrentPage == "/Index")
        {
            return "is-active";
        }

        return path != "/Index" && CurrentPage.StartsWith(path, StringComparison.OrdinalIgnoreCase)
            ? "is-active"
            : string.Empty;
    }
}
