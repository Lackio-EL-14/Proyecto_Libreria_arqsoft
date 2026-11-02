using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Application.Facades;
using Libreria.Servicio.Reportes.Application.Factories;
using Libreria.Servicio.Reportes.Application.UseCases;
using Libreria.Servicio.Reportes.Infrastructure.Charts;
using Libreria.Servicio.Reportes.Infrastructure.Connection;
using Libreria.Servicio.Reportes.Infrastructure.Factories;
using Libreria.Servicio.Reportes.Infrastructure.Repositories;
using Libreria.Servicio.Reportes.Presentation.Security;
using QuestPDF.Infrastructure;

namespace Libreria.Servicio.Reportes.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServicioReportes(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Uso académico/de aprendizaje conforme a la licencia de la versión instalada.
        QuestPDF.Settings.License = LicenseType.Community;
        var connectionStrings = ConnectionStringSingleton.Instancia;
        var configured = configuration.GetConnectionString("LibreriaDb");
        if (!string.IsNullOrWhiteSpace(configured)) connectionStrings.Configurar(configured);
        services.AddSingleton(connectionStrings);
        services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IExampleReportRepository, ExampleReportRepository>();
        services.AddScoped<IProductReportRepository, ProductReportRepository>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IReportChartRenderer, ScottPlotChartRenderer>();
        services.AddScoped<ReportGeneratorFactory, PdfReportGeneratorFactory>();
        services.AddScoped<ReportGeneratorFactory, ExcelReportGeneratorFactory>();
        services.AddScoped<ReporteFacade>();
        services.AddScoped<ReadExampleReportUseCase>();
        services.AddScoped<ReadProductReportUseCase>();
        return services;
    }
}
