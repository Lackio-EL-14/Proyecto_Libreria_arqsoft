using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Libreria.Web.Data.Factories;
using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Entities;
using Libreria.Web.Pages.Categorias.Models;

namespace Libreria.Web.Business.Validators
{
    public class CategoriaValidator
    {
        private readonly ICrudRepository<Categoria> _repository;

        public CategoriaValidator(CrudRepositoryFactory<Categoria> factory)
        {
            _repository = factory.CrearRepositorio();
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
            Guid? excluirPublicId = null)
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
            else if (await _repository.ExisteValorAsync("Codigo", input.Codigo, excluirPublicId))
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
            else if (await _repository.ExisteValorAsync("Nombre", input.Nombre, excluirPublicId))
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
}
