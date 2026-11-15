using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Application.Comun;
using Libreria.Servicio.Productos.Application.Models.Productos;

namespace Libreria.Servicio.Productos.Application.Validators;

public sealed class ProductoValidator
{
    private readonly IProductoRepository _productos;
    private readonly ICatalogoProductoRepository _catalogos;

    public ProductoValidator(
        IProductoRepository productos,
        ICatalogoProductoRepository catalogos)
    {
        _productos = productos;
        _catalogos = catalogos;
    }

    public async Task<Result> ValidarAsync(
        ProductoInput input,
        Guid? excluirPublicId = null)
    {
        var errores = new Dictionary<string, string>();

        input.Nombre = input.Nombre?.Trim() ?? string.Empty;
        input.DescripcionEspecifica =
            input.DescripcionEspecifica?.Trim();

        if (string.IsNullOrWhiteSpace(input.Nombre))
        {
            errores["Nombre"] =
                "El nombre del producto es obligatorio.";
        }

        if (input.Stock < 0)
        {
            errores["Stock"] =
                "El stock no puede ser negativo.";
        }

        if (input.PrecioVenta < 0)
        {
            errores["PrecioVenta"] =
                "El precio de venta no puede ser negativo.";
        }

        if (input.CostoAdquisicion < 0)
        {
            errores["CostoAdquisicion"] =
                "El costo de adquisición no puede ser negativo.";
        }

        if (input.EsPerecedero
            && input.FechaVencimiento is null)
        {
            errores["FechaVencimiento"] =
                "Ingrese la fecha de vencimiento del producto perecedero.";
        }

        if (!input.EsPerecedero)
        {
            input.FechaVencimiento = null;
        }

        if (input.CategoriaPublicId == Guid.Empty)
        {
            errores["CategoriaPublicId"] =
                "Seleccione una categoría.";
        }
        else if (!await _catalogos.ExisteCategoriaActivaAsync(
                     input.CategoriaPublicId))
        {
            errores["CategoriaPublicId"] =
                "La categoría seleccionada no existe o está inactiva.";
        }

        if (input.MarcaPublicId == Guid.Empty)
        {
            errores["MarcaPublicId"] =
                "Seleccione una marca.";
        }
        else if (!await _catalogos.ExisteMarcaActivaAsync(
                     input.MarcaPublicId))
        {
            errores["MarcaPublicId"] =
                "La marca seleccionada no existe o está inactiva.";
        }

        if (!string.IsNullOrWhiteSpace(input.Nombre)
            && await _productos.ExisteValorAsync(
                "Nombre",
                input.Nombre,
                excluirPublicId))
        {
            errores["Nombre"] =
                "Ya existe un producto con ese nombre.";
        }

        return errores.Count == 0
            ? Result.Exito()
            : Result.Validacion(errores);
    }
}