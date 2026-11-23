using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;
using Libreria.Client.DTOs.Clientes;
using Libreria.Client.DTOs.Productos;
using Libreria.Client.DTOs.Proveedores;
using Libreria.Client.DTOs.Usuarios;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Client.Tests.Formularios;

/// <summary>
/// Pruebas del 22/11: cada campo de formulario tiene un nombre visible en español y cada regla de
/// validación su propio mensaje (sin los textos por defecto en inglés de DataAnnotations).
/// </summary>
public sealed class ModelosFormularioTests
{
    public static TheoryData<string, PropertyInfo> Campos()
    {
        var datos = new TheoryData<string, PropertyInfo>();
        var modelos = new[]
        {
            typeof(ClienteInput), typeof(ProveedorInput), typeof(ProductoInput), typeof(CambioContrasenaInput)
        };

        foreach (var propiedad in modelos.SelectMany(modelo => modelo.GetProperties()))
        {
            datos.Add($"{propiedad.DeclaringType!.Name}.{propiedad.Name}", propiedad);
        }

        // Alta de usuarios: las propiedades [BindProperty] de la propia página.
        foreach (var propiedad in typeof(Libreria.Client.Pages.Usuarios.CreateModel).GetProperties()
                     .Where(p => p.GetCustomAttribute<BindPropertyAttribute>() is not null))
        {
            datos.Add($"Usuarios.CreateModel.{propiedad.Name}", propiedad);
        }

        return datos;
    }

    [Theory]
    [MemberData(nameof(Campos))]
    public void El_campo_tiene_nombre_visible_y_mensajes_propios(string campo, PropertyInfo propiedad)
    {
        var nombre = propiedad.GetCustomAttribute<DisplayAttribute>()?.Name;
        Assert.False(string.IsNullOrWhiteSpace(nombre), $"{campo} no tiene [Display(Name)].");
        Assert.DoesNotMatch("[a-záéíóú][A-Z]", nombre!);

        foreach (var regla in propiedad.GetCustomAttributes<ValidationAttribute>())
        {
            Assert.False(string.IsNullOrWhiteSpace(regla.ErrorMessage), $"{campo}: [{regla.GetType().Name}] sin ErrorMessage.");
            Assert.DoesNotMatch(new Regex(@"\b(the|field|must|is|invalid|required)\b", RegexOptions.IgnoreCase), regla.ErrorMessage!);
            Assert.EndsWith(".", regla.ErrorMessage);
        }
    }
}
