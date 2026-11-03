namespace Libreria.Servicio.Reportes.Application.DTOs;

public sealed record ExportedReport(byte[] Content, string ContentType, string FileName);
