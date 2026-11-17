namespace Libreria.Servicio.Proveedores.Application.Ports.Secondary;

public sealed class ValorDuplicadoException : Exception
{
    public ValorDuplicadoException(string campo, Exception innerException) : base($"Valor duplicado en {campo}.", innerException) => Campo = campo;
    public string Campo { get; }
}
