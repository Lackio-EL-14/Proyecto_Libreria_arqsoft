using Libreria.Web.Pages.Marcas.Models;
using Libreria.Web.Pages.Marcas.Repositories;

namespace Libreria.Web.Pages.Marcas.Services;

public class MarcaValidator
{
    private readonly IValidadorMarcaRepository _repository;

    public MarcaValidator(IValidadorMarcaRepository repository)
    {
        _repository = repository;
    }

    public void Normalizar(MarcaInput input)
    {
        input.Nombre = NormalizarTexto(input.Nombre);
        input.Descripcion = NormalizarTextoOpcional(input.Descripcion);
        input.PaisOrigen = NormalizarTexto(input.PaisOrigen);
        input.SitioWeb = input.SitioWeb?.Trim();
        if (string.IsNullOrWhiteSpace(input.SitioWeb))
        {
            input.SitioWeb = null;
        }
    }

    public IReadOnlyDictionary<string, string> Validar(
        MarcaInput input,
        int? excluirId = null)
    {
        var errores = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(input.Nombre))
        {
            errores["Input.Nombre"] = "El nombre de la marca es obligatorio.";
        }
        else if (input.Nombre.Length > 100)
        {
            errores["Input.Nombre"] = "El nombre no puede superar los 100 caracteres.";
        }
        else if (_repository.ExisteNombre(input.Nombre, excluirId))
        {
            errores["Input.Nombre"] = "Ya existe una marca registrada con ese nombre.";
        }

        if (input.Descripcion?.Length > 255)
        {
            errores["Input.Descripcion"] = "La descripción no puede superar los 255 caracteres.";
        }

        if (string.IsNullOrWhiteSpace(input.PaisOrigen))
        {
            errores["Input.PaisOrigen"] = "El país de origen es obligatorio.";
        }
        else if (input.PaisOrigen.Length > 100)
        {
            errores["Input.PaisOrigen"] = "El país de origen no puede superar los 100 caracteres.";
        }

        if (input.SitioWeb?.Length > 200)
        {
            errores["Input.SitioWeb"] = "El sitio web no puede superar los 200 caracteres.";
        }
        else if (input.SitioWeb is not null && !EsUrlValida(input.SitioWeb))
        {
            errores["Input.SitioWeb"] = "Ingrese una dirección web válida que comience con http:// o https://.";
        }

        return errores;
    }

    private static bool EsUrlValida(string valor)
    {
        return Uri.TryCreate(valor, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
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
