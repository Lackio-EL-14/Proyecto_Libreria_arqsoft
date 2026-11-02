using System.ComponentModel.DataAnnotations;
using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Application.DTOs;
using Libreria.Servicio.Reportes.Domain.Reports;

namespace Libreria.Servicio.Reportes.Application.UseCases;

public sealed class ReadProductReportUseCase(
    IProductReportRepository repository, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<ReportDocument> ExecuteAsync(ProductReportFilter filter, CancellationToken cancellationToken)
    {
        Validator.ValidateObject(filter, new ValidationContext(filter), validateAllProperties: true);
        if (currentUser.UserId is not > 0 || string.IsNullOrWhiteSpace(currentUser.Username))
            throw new UnauthorizedAccessException("Falta el usuario que genera el reporte.");

        var rows = await repository.ReadProductsAsync(filter, cancellationToken);
        return new("Reporte de productos", rows, currentUser.Username, timeProvider.GetUtcNow(),
            IsProductReport: true);
    }
}
