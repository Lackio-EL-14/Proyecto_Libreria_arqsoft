namespace Libreria.Servicio.Clientes.Application.Ports.Secondary;

/// <summary>
/// La lanza el adaptador de persistencia cuando la BD rechaza un valor único repetido
/// (en MySQL, el error 1062). Así el núcleo no depende del proveedor.
/// </summary>
public sealed class ValorDuplicadoException : Exception
{
    public ValorDuplicadoException(string campo, Exception? interna = null)
        : base($"Ya existe un registro con el mismo valor en '{campo}'.", interna)
    {
        Campo = campo;
    }

    public string Campo { get; }
}
