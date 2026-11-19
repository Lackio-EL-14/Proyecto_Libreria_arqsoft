using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Application.Models.Marcas;
using Libreria.Servicio.Productos.Domain;

namespace Libreria.Servicio.Productos.Application.Validators;

public sealed class MarcaValidator
{
    private readonly ICrudRepository<Domain.Entities.Marca> _repository;

    public MarcaValidator(
        ICrudRepository<Domain.Entities.Marca> repository)
    {
        _repository = repository;
    }

    public async Task<Result> ValidarAsync(
        MarcaInput input,
        Guid? excluirPublicId = null)
    {
        var errores = new Dictionary<string, string>();

        input.Nombre = input.Nombre?.Trim() ?? string.Empty;
        input.Descripcion = input.Descripcion?.Trim();
        input.PaisOrigen = PaisesMarca.Normalizar(input.PaisOrigen);
        input.SitioWeb = input.SitioWeb?.Trim();

        if (string.IsNullOrWhiteSpace(input.Nombre))
        {
            errores["Nombre"] = "El nombre de la marca es obligatorio.";
        }

        if (!PaisesMarca.EsValido(input.PaisOrigen))
        {
            errores["PaisOrigen"] = "Seleccione un país de origen válido.";
        }

        if (!string.IsNullOrWhiteSpace(input.SitioWeb)
            && !Uri.TryCreate(
                input.SitioWeb,
                UriKind.Absolute,
                out var uri))
        {
            errores["SitioWeb"] =
                "Ingrese una dirección web válida.";
        }

        if (!string.IsNullOrWhiteSpace(input.Nombre)
            && await _repository.ExisteValorAsync(
                "Nombre",
                input.Nombre,
                excluirPublicId))
        {
            errores["Nombre"] =
                "Ya existe una marca con ese nombre.";
        }

        return errores.Count == 0
            ? Result.Exito()
            : Result.Validacion(errores);
    }
}