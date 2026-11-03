using System.ComponentModel.DataAnnotations;
using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Application.DTOs;

namespace Libreria.Servicio.Reportes.Application.UseCases;

public sealed class ReadSalesReportUseCase(
    ISalesReportRepository repository, IProductReportRepository products, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<SalesReportResult> ExecuteAsync(SalesReportFilter filter, CancellationToken cancellationToken)
    {
        Validator.ValidateObject(filter, new ValidationContext(filter), validateAllProperties: true);
        if (currentUser.UserId is not > 0 || string.IsNullOrWhiteSpace(currentUser.Username))
            throw new UnauthorizedAccessException("Falta el usuario que genera el reporte.");
        if (filter.CategoryPublicId.HasValue)
        {
            var categories = await products.ReadCategoriesAsync(cancellationToken);
            if (!categories.Any(category => category.PublicId == filter.CategoryPublicId))
                throw new ValidationException("Seleccione una categoría activa existente.");
        }
        var rows = await repository.ReadSalesAsync(filter, cancellationToken);
        return new(filter, rows, currentUser.Username, timeProvider.GetUtcNow());
    }
}
