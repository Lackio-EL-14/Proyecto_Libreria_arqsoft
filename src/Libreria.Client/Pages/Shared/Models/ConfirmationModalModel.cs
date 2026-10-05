using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
namespace Libreria.Client.Pages.Shared.Models;

public sealed class ConfirmationModalModel
{
    public required string Id { get; init; }
    public required string Titulo { get; init; }
    public required string Mensaje { get; init; }
    public required string Handler { get; init; }
    public required string CampoIdentificador { get; init; }
    public string TextoConfirmar { get; init; } = "Confirmar";
    public string TextoCancelar { get; init; } = "Cancelar";
    public string? MensajeAdvertencia { get; init; }
}
