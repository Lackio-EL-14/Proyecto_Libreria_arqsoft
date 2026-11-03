using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Libreria.Servicio.Reportes.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void LayersKeepDependenciesPointingInward()
    {
        var service = Path.Combine(FindRoot(), "src", "Libreria.Servicio.Reportes");
        foreach (var layer in new[] { "Domain", "Application", "Infrastructure" })
        {
            var files = Directory.GetFiles(Path.Combine(service, layer), "*.cs", SearchOption.AllDirectories);
            Assert.NotEmpty(files);
            foreach (var file in files)
            {
                var source = File.ReadAllText(file);
                var forbidden = layer switch
                {
                    "Domain" => new[] { "Application", "Infrastructure", "Presentation", "Microsoft", "QuestPDF", "ClosedXML", "ScottPlot", "System.Data" },
                    "Application" => new[] { "Infrastructure", "Presentation", "Microsoft", "QuestPDF", "ClosedXML", "ScottPlot", "HttpContext", "SqlConnection" },
                    _ => new[] { "Presentation", "HttpContext", "ControllerBase" }
                };
                foreach (var dependency in forbidden)
                    Assert.False(Regex.IsMatch(source, @"\b" + Regex.Escape(dependency) + @"\b"),
                        $"{Path.GetRelativePath(service, file)} depende de {dependency}.");
            }
        }
        var project = XDocument.Load(Path.Combine(service, "Libreria.Servicio.Reportes.csproj"));
        Assert.Empty(project.Descendants("ProjectReference"));
    }

    internal static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "LibreriaSystem.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("No se encontró la raíz del repositorio.");
    }
}
