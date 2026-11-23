using System.ComponentModel.DataAnnotations;
using Libreria.Client.Validacion;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Libreria.Client.TagHelpers;

/// <summary>
/// Criterio único de etiquetas del Front (pruebas del 22/11): un <c>&lt;label asp-for="…"&gt;</c>
/// vacío muestra el <c>[Display(Name)]</c> de la propiedad y, después,
/// <c>*</c> si la propiedad tiene <c>[Required]</c> o «(opcional)» si no lo tiene.
/// Las casillas de verificación y los campos con <see cref="RequeridoAlgunoDeAttribute"/>
/// («al menos uno de los dos») no llevan marca: lo explica el texto de ayuda del formulario.
/// Una etiqueta con texto propio (por ejemplo, un obligatorio condicional) no se toca.
/// </summary>
[HtmlTargetElement("label", Attributes = "asp-for")]
public sealed class EtiquetaCampoTagHelper : TagHelper
{
    public const string MarcaObligatorio = "<span class=\"text-danger\">*</span>";
    public const string MarcaOpcional = "(opcional)";

    // Después del LabelTagHelper de ASP.NET (Order = -1000), que ya puso el nombre visible.
    public override int Order => 0;

    [HtmlAttributeName("asp-for")]
    public ModelExpression For { get; set; } = default!;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var contenidoPropio = await output.GetChildContentAsync();
        if (!contenidoPropio.IsEmptyOrWhiteSpace)
        {
            return;
        }

        var metadata = For.Metadata;
        output.Content.SetContent(metadata.GetDisplayName());

        var marca = Marca(metadata);
        if (marca is not null)
        {
            output.Content.AppendHtml(" ").AppendHtml(marca);
        }
    }

    /// <summary>La marca que sigue al nombre: <c>*</c>, «(opcional)» o ninguna.</summary>
    public static string? Marca(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata)
    {
        if (metadata.UnderlyingOrModelType == typeof(bool))
        {
            return null;
        }

        var atributos = metadata is DefaultModelMetadata predeterminada
            ? predeterminada.Attributes.PropertyAttributes ?? []
            : [];

        if (atributos.OfType<RequiredAttribute>().Any())
        {
            return MarcaObligatorio;
        }

        return atributos.OfType<RequeridoAlgunoDeAttribute>().Any() || EsLaOtraDeUnGrupo(metadata)
            ? null
            : MarcaOpcional;
    }

    // El correo del contacto es «la otra» del [RequeridoAlgunoDe] que lleva el teléfono.
    private static bool EsLaOtraDeUnGrupo(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata) =>
        metadata.ContainerType?.GetProperties()
            .SelectMany(propiedad => propiedad.GetCustomAttributes(typeof(RequeridoAlgunoDeAttribute), true))
            .OfType<RequeridoAlgunoDeAttribute>()
            .Any(atributo => atributo.OtraPropiedad == metadata.PropertyName) == true;
}
