namespace Libreria.Servicio.Reportes.Application.Exceptions;

public sealed class ReportDataUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
