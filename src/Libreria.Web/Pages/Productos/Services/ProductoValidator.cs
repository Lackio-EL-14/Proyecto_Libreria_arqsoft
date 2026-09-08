namespace Libreria.Web.Pages.Productos.Services;

public class ProductoValidator
{
    public string NormalizarTexto(string texto)
    {
        return string.Join(
            " ",
            texto.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries
            )
        );
    }

    public string? NormalizarTextoOpcional(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        return NormalizarTexto(texto);
    }

    public bool EsFechaVencimientoValida(DateTime? fechaVencimiento)
    {
        return !fechaVencimiento.HasValue
            || fechaVencimiento.Value.Date >= DateTime.Today;
    }
}