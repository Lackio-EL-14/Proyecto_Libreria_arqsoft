using System.Text.RegularExpressions;

namespace Libreria.Client.Tests.Infraestructura;

/// <summary>Revisa que el HTML no muestre textos en inglés ni nombres internos de propiedades.</summary>
public static class TextosVisibles
{
    private static readonly string[] Ingles =
    [
        "The value", "is invalid", "field is required", "must be a number", "The field",
        "is not valid", "Completa este campo", "Please enter"
    ];

    private static readonly string[] NombresInternos =
    [
        "DescripcionEspecifica", "CategoriaPublicId", "MarcaPublicId", "PrecioVenta", "CostoAdquisicion",
        "EsPerecedero", "FechaVencimiento", "CiNit", "RazonSocial", "NombreContacto", "CargoContacto",
        "TelefonoContacto", "CorreoContacto", "ApellidoMaterno", "ContrasenaActual", "NuevaContrasena"
    ];

    public static void SinNombresInternosNiIngles(string html)
    {
        foreach (var texto in Ingles)
        {
            Assert.DoesNotContain(texto, html, StringComparison.OrdinalIgnoreCase);
        }

        var visible = TextoVisible(html);
        foreach (var nombre in NombresInternos)
        {
            Assert.DoesNotMatch($@"\b{nombre}\b", visible);
        }
    }

    /// <summary>El texto entre etiquetas y los atributos que lee el usuario (mensajes, title, placeholder).</summary>
    public static string TextoVisible(string html)
    {
        var sinScripts = Regex.Replace(html, "<script[\\s\\S]*?</script>", " ", RegexOptions.IgnoreCase);
        var texto = Regex.Replace(sinScripts, "<[^>]+>", " ");
        var atributos = Regex.Matches(sinScripts, "(?:data-val-[a-z]+|title|placeholder|aria-label)=\"([^\"]*)\"")
            .Select(m => m.Groups[1].Value);
        return texto + " " + string.Join(" ", atributos);
    }

    /// <summary>Solo el texto entre etiquetas, sin scripts ni estilos (para buscar palabras pegadas).</summary>
    public static string TextoSinCodigo(string html)
    {
        var sinCodigo = Regex.Replace(html, "<(script|style|svg)[\\s\\S]*?</\\1>", " ", RegexOptions.IgnoreCase);
        return Regex.Replace(sinCodigo, "<[^>]+>", " ");
    }
}
