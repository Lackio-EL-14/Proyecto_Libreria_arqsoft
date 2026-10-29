using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Libreria.Client.Clients.Common;

public sealed class ServicioNoDisponibleFilter : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var result = await next();

        if (result.Exception is not ServicioNoDisponibleException exception || result.ExceptionHandled)
        {
            return;
        }

        result.ExceptionHandled = true;
        result.Result = new RedirectToPageResult("/ServicioNoDisponible", new { servicio = exception.Servicio });
    }
}
