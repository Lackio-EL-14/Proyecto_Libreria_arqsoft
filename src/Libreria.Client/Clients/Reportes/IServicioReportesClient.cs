using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Reportes;

namespace Libreria.Client.Clients.Reportes;

public interface IServicioReportesClient : IApiClient
{
    Task<IReadOnlyList<ReportCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<ProductReportDto?> GetProductsAsync(ProductReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<ReportFileDto> DownloadProductsAsync(ProductReportFilterDto filter, string format, CancellationToken cancellationToken = default);
    Task<SalesReportDto?> GetSalesAsync(SalesReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<ReportFileDto> DownloadSalesPdfAsync(SalesReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<ReportFileDto> DownloadSalesAsync(SalesReportFilterDto filter, string format, CancellationToken cancellationToken = default);
}
