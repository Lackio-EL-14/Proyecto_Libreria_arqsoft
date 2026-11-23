using System.Text.RegularExpressions;

namespace Libreria.Client.Tests.Formularios;

/// <summary>
/// Revisión de las vistas del Front (pruebas del 22/11): ningún formulario deja la validación al
/// navegador y ningún campo usa el atributo HTML required.
/// </summary>
public sealed class FormulariosFuenteTests
{
    private static readonly string Paginas = Path.Combine(RaizDelRepositorio(), "src", "Libreria.Client", "Pages");

    public static TheoryData<string> Vistas()
    {
        var datos = new TheoryData<string>();
        foreach (var vista in Directory.EnumerateFiles(Paginas, "*.cshtml", SearchOption.AllDirectories))
        {
            datos.Add(Path.GetRelativePath(Paginas, vista));
        }

        return datos;
    }

    [Theory]
    [MemberData(nameof(Vistas))]
    public void Todo_formulario_lleva_novalidate_y_ningun_campo_required(string vista)
    {
        var contenido = File.ReadAllText(Path.Combine(Paginas, vista));

        foreach (Match form in Regex.Matches(contenido, "<form\\b[^>]*>"))
        {
            Assert.Contains("novalidate", form.Value);
        }

        Assert.DoesNotMatch(@"<(input|select|textarea)\b[^>]*\srequired\b", contenido);
    }

    [Fact]
    public void Los_formularios_con_campos_cargan_la_validacion_del_cliente()
    {
        var conCampos = Directory.EnumerateFiles(Paginas, "*.cshtml", SearchOption.AllDirectories)
            .Where(vista => File.ReadAllText(vista).Contains("asp-validation-for"))
            .Where(vista => !Path.GetFileName(vista).StartsWith('_'));

        Assert.All(conCampos, vista =>
            Assert.Contains("_ValidationScriptsPartial", File.ReadAllText(vista)));
    }

    private static string RaizDelRepositorio()
    {
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);
        while (carpeta is not null && !File.Exists(Path.Combine(carpeta.FullName, "LibreriaSystem.slnx")))
        {
            carpeta = carpeta.Parent;
        }

        return carpeta?.FullName ?? throw new DirectoryNotFoundException("No se encontró LibreriaSystem.slnx.");
    }
}
