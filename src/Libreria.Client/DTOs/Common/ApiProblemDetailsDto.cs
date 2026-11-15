namespace Libreria.Client.DTOs.Common;

public sealed class ApiProblemDetailsDto
{
    public string? Title { get; init; }
    public string? Detail { get; init; }
    public int? Status { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }
}
