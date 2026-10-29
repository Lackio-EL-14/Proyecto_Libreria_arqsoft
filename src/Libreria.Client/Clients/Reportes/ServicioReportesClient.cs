using Libreria.Client.Clients.Common;

namespace Libreria.Client.Clients.Reportes;

public sealed class ServicioReportesClient : ApiClientBase, IServicioReportesClient
{
    public ServicioReportesClient(HttpClient httpClient)
        : base(httpClient, "Reportes")
    {
    }
}
