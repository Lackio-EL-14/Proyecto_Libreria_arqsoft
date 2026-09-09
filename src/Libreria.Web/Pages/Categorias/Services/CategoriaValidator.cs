using System.Text.RegularExpressions;
using Libreria.Web.Pages.Categorias.Models;
using Libreria.Web.Pages.Categorias.Repositories;

namespace Libreria.Web.Pages.Categorias.Services;

public class CategoriaValidator
{
    private readonly IValidadorCategoriaRepository _repository;

    public CategoriaValidator(IValidadorCategoriaRepository repository)
    {
        _repository = repository;
    }

    public void Normalizar(CategoriaInput input)
    {
        input.Codigo = (input.Codigo ?? string.Empty).Trim().ToUpperInvariant();
        input.Nombre = NormalizarTexto(input.Nombre ?? string.Empty);
        input.Descripcion = NormalizarTextoOpcional(input.Descripcion);
        input.Ubicacion = NormalizarTexto(input.Ubicacion ?? string.Empty);
    }

    public async Task<IReadOnlyDictionary<string, string>> ValidarAsync(
        CategoriaInput input,
        int? excluirId = null)
    {
        var errores = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(input.Codigo))
        {
            errores["Input.Codigo"] = "El código de la categoría es obligatorio.";
        }
        else if (input.Codigo.Length > 20)
        {
            errores["Input.Codigo"] = "El código no puede exceder los 20 caracteres.";
        }
        else if (!Regex.IsMatch(input.Codigo, "^[A-Z0-9-]+$"))
        {
            errores["Input.Codigo"] = "El código solo puede contener letras, números y guiones.";
        }
        else if (await _repository.ExisteCodigoAsync(input.Codigo, excluirId))
        {
            errores["Input.Codigo"] = "El código de la categoría ya está registrado.";
        }

        if (string.IsNullOrWhiteSpace(input.Nombre))
        {
            errores["Input.Nombre"] = "El nombre de la categoría es obligatorio.";
        }
        else if (input.Nombre.Length > 100)
        {
            errores["Input.Nombre"] = "El nombre no puede exceder los 100 caracteres.";
        }
        else if (await _repository.ExisteNombreAsync(input.Nombre, excluirId))
        {
            errores["Input.Nombre"] = "El nombre de la categoría ya está registrado.";
        }

        if (input.Descripcion?.Length > 255)
        {
            errores["Input.Descripcion"] = "La descripción no puede exceder los 255 caracteres.";
        }

        if (string.IsNullOrWhiteSpace(input.Ubicacion))
        {
            errores["Input.Ubicacion"] = "La ubicación de la categoría es obligatoria.";
        }
        else if (input.Ubicacion.Length > 100)
        {
            errores["Input.Ubicacion"] = "La ubicación no puede exceder los 100 caracteres.";
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
