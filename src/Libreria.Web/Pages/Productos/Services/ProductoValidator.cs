using Libreria.Web.Pages.Productos.Models;
using Libreria.Web.Pages.Productos.Repositories;

namespace Libreria.Web.Pages.Productos.Services;

public class ProductoValidator
{
    private readonly IValidacionCatalogoProductoRepository _catalogoRepository;

    public ProductoValidator(IValidacionCatalogoProductoRepository catalogoRepository)
    {
        _catalogoRepository = catalogoRepository;
    }

    public void Normalizar(ProductoInput input)
    {
        input.Nombre = NormalizarTexto(input.Nombre ?? string.Empty);
        input.DescripcionEspecifica = NormalizarTextoOpcional(input.DescripcionEspecifica);
    }

    public IReadOnlyDictionary<string, string> Validar(ProductoInput input)
    {
        var errores = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(input.Nombre))
        {
            errores["Input.Nombre"] = "El nombre del producto es obligatorio.";
        }
        else if (input.Nombre.Length > 150)
        {
            errores["Input.Nombre"] = "El nombre no puede exceder los 150 caracteres.";
        }

        if (input.DescripcionEspecifica?.Length > 500)
        {
            errores["Input.DescripcionEspecifica"] = "La descripción no puede exceder los 500 caracteres.";
        }

        if (input.FechaVencimiento.HasValue
            && input.FechaVencimiento.Value.Date < DateTime.Today)
        {
            errores["Input.FechaVencimiento"] = "La fecha de vencimiento no puede estar en el pasado.";
        }

        if (input.Stock < 0)
        {
            errores["Input.Stock"] = "El stock no puede ser negativo.";
        }

        if (input.PrecioVenta <= 0 || input.PrecioVenta > 99_999_999.99m)
        {
            errores["Input.PrecioVenta"] = "El precio de venta debe ser mayor a 0.";
        }

        if (input.CostoAdquisicion < 0 || input.CostoAdquisicion > 99_999_999.99m)
        {
            errores["Input.CostoAdquisicion"] = "El costo de adquisición no puede ser negativo.";
        }

        if (input.CategoriaId <= 0
            || !_catalogoRepository.ExisteCategoriaActiva(input.CategoriaId))
        {
            errores["Input.CategoriaId"] = "La categoría seleccionada no está disponible.";
        }

        if (input.MarcaId <= 0
            || !_catalogoRepository.ExisteMarcaActiva(input.MarcaId))
        {
            errores["Input.MarcaId"] = "La marca seleccionada no está disponible.";
        }

        return errores;
    }

    private static string NormalizarTexto(string texto)
    {
        return string.Join(' ', texto.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? NormalizarTextoOpcional(string? texto)
    {
        return string.IsNullOrWhiteSpace(texto) ? null : NormalizarTexto(texto);
    }
}
