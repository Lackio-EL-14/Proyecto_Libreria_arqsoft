using Libreria.Web.Data.Factories;
using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Entities;
using Libreria.Web.Pages.Marcas.Models;

namespace Libreria.Web.Business.Validators;

public class MarcaValidator
{
    private readonly ICrudRepository<Marca> _repository;

    public MarcaValidator(CrudRepositoryFactory<Marca> factory)
    {
        _repository = factory.CrearRepositorio();
    }

    public void Normalizar(MarcaInput input)
    {
        input.Nombre = NormalizarTexto(input.Nombre ?? string.Empty);
        input.Descripcion = NormalizarTextoOpcional(input.Descripcion);
        input.PaisOrigen = NormalizarTexto(input.PaisOrigen ?? string.Empty);
        input.SitioWeb = input.SitioWeb?.Trim();

        if (string.IsNullOrWhiteSpace(input.SitioWeb))
        {
            input.SitioWeb = null;
        }
    }

    public async Task<IReadOnlyDictionary<string, string>> ValidarAsync(
        MarcaInput input,
        Guid? excluirPublicId = null)
    {
        var errores = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(input.Nombre))
        {
            errores["Input.Nombre"] =
                "El nombre de la marca es obligatorio.";
        }
        else if (input.Nombre.Length > 100)
        {
            errores["Input.Nombre"] =
                "El nombre no puede superar los 100 caracteres.";
        }
        else if (await _repository.ExisteValorAsync(
                     "Nombre",
                     input.Nombre,
                     excluirPublicId))
        {
            errores["Input.Nombre"] =
                "Ya existe una marca registrada con ese nombre.";
        }

        if (input.Descripcion?.Length > 255)
        {
            errores["Input.Descripcion"] =
                "La descripción no puede superar los 255 caracteres.";
        }

        if (string.IsNullOrWhiteSpace(input.PaisOrigen))
        {
            errores["Input.PaisOrigen"] =
                "El país de origen es obligatorio.";
        }
        else if (input.PaisOrigen.Length > 100)
        {
            errores["Input.PaisOrigen"] =
                "El país de origen no puede superar los 100 caracteres.";
        }

        if (input.SitioWeb?.Length > 200)
        {
            errores["Input.SitioWeb"] =
                "El sitio web no puede superar los 200 caracteres.";
        }
        else if (input.SitioWeb is not null &&
                 !EsUrlValida(input.SitioWeb))
        {
            errores["Input.SitioWeb"] =
                "Ingrese una dirección web válida que comience con http:// o https://.";
        }

        return errores;
    }

    private static bool EsUrlValida(string valor)
    {
        return Uri.TryCreate(
                   valor,
                   UriKind.Absolute,
                   out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp ||
                   uri.Scheme == Uri.UriSchemeHttps);
    }

    private static string NormalizarTexto(string texto)
    {
        return string.Join(
            ' ',
            texto.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? NormalizarTextoOpcional(string? texto)
    {
        return string.IsNullOrWhiteSpace(texto)
            ? null
            : NormalizarTexto(texto);
    }
}