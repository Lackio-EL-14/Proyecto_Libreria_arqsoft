namespace Libreria.Web.Pages.Shared.Models;

public sealed class ConfirmModalModel
{
    public required string Id { get; init; }
    public required string Titulo { get; init; }
    public required string MensajePrincipal { get; init; }
    public string? MensajeAdvertencia { get; init; }
    public string TextoBotonConfirmar { get; init; } = "Confirmar";
    public string TextoBotonCancelar { get; init; } = "Cancelar";
    public string Handler { get; init; } = "DarDeBaja";
    public string CampoIdentificador { get; init; } = "publicId";
    public string? UrlAccion { get; init; }

    // Compatibilidad con nombres de propiedad alternativos
    public string Mensaje => MensajePrincipal;
    public string TextoConfirmar => TextoBotonConfirmar;
    public string TextoCancelar => TextoBotonCancelar;
}
