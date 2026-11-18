using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Application.Models.Categorias;
using Libreria.Servicio.Productos.Domain;

namespace Libreria.Servicio.Productos.Application.Validators;

public sealed class CategoriaValidator
{
    private readonly ICrudRepository<Domain.Entities.Categoria> _repository;

    public CategoriaValidator(
        ICrudRepository<Domain.Entities.Categoria> repository)
    {
        _repository = repository;
    }

    public async Task<Result> ValidarAsync(
        CategoriaInput input,
        Guid? excluirPublicId = null)
    {
        var errores = new Dictionary<string, string>();

        input.Nombre = input.Nombre?.Trim() ?? string.Empty;
        input.Descripcion = input.Descripcion?.Trim();
        input.Ubicacion = input.Ubicacion?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(input.Nombre))
        {
            errores["Nombre"] = "El nombre de la categoría es obligatorio.";
        }

        if (!UbicacionesCategoria.EsValida(input.Ubicacion))
        {
            errores["Ubicacion"] = "Seleccione una ubicación válida.";
        }

        if (!string.IsNullOrWhiteSpace(input.Nombre)
            && await _repository.ExisteValorAsync(
                "Nombre",
                input.Nombre,
                excluirPublicId))
        {
            errores["Nombre"] =
                "Ya existe una categoría con ese nombre.";
        }

        return errores.Count == 0
            ? Result.Exito()
            : Result.Validacion(errores);
    }
}