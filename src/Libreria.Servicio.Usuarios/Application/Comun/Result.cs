namespace Libreria.Servicio.Usuarios.Application.Comun;

/// <summary>
/// Motivo por el que una operación de negocio no se completó.
/// El adaptador HTTP lo traduce a 400, 404 o 409 (contrato 6.5).
/// </summary>
public enum TipoError
{
    Ninguno,
    Validacion,
    NoEncontrado,
    Conflicto
}

/// <summary>
/// Patrón Result: las validaciones de negocio se devuelven como valor,
/// sin lanzar excepciones. Las excepciones quedan para lo inesperado.
/// </summary>
public class Result
{
    private static readonly IReadOnlyDictionary<string, string> SinErrores =
        new Dictionary<string, string>();

    protected Result(TipoError tipo, IReadOnlyDictionary<string, string> errores)
    {
        Tipo = tipo;
        Errores = errores;
    }

    public bool EsExito => Tipo == TipoError.Ninguno;

    /// <summary>Forma del contrato 6.5 (US-139). Igual a <see cref="EsExito"/>.</summary>
    public bool IsSuccess => EsExito;

    /// <summary>La operación falló. Siempre es lo contrario de <see cref="IsSuccess"/>.</summary>
    public bool IsError => !IsSuccess;

    /// <summary>Devuelve el arreglo de mensajes de error acumulados (vacío si tuvo éxito).</summary>
    public string[] Errors() => Errores.Values.ToArray();

    /// <summary>Crea un resultado correcto.</summary>
    public static Result Success() => Exito();

    /// <summary>Crea un error de validación con su mensaje (se traduce a 400).</summary>
    public static Result Error(string mensaje)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mensaje);
        return Validacion(string.Empty, mensaje);
    }

    public TipoError Tipo { get; }

    /// <summary>Campo → mensaje. Vacío cuando la operación tuvo éxito.</summary>
    public IReadOnlyDictionary<string, string> Errores { get; }

    public static Result Exito() => new(TipoError.Ninguno, SinErrores);

    public static Result Validacion(IReadOnlyDictionary<string, string> errores) =>
        new(TipoError.Validacion, ValidarErrores(errores));

    public static Result Validacion(string campo, string mensaje) =>
        Validacion(new Dictionary<string, string> { [campo] = mensaje });

    public static Result NoEncontrado(string mensaje) =>
        new(TipoError.NoEncontrado, new Dictionary<string, string> { [string.Empty] = mensaje });

    public static Result Conflicto(string campo, string mensaje) =>
        new(TipoError.Conflicto, new Dictionary<string, string> { [campo] = mensaje });

    protected static IReadOnlyDictionary<string, string> ValidarErrores(
        IReadOnlyDictionary<string, string> errores)
    {
        if (errores is null || errores.Count == 0)
        {
            throw new ArgumentException("Un resultado fallido necesita al menos un error.", nameof(errores));
        }

        return errores;
    }
}

/// <summary>Result que, en caso de éxito, transporta un valor.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _valor;

    private Result(T valor)
        : base(TipoError.Ninguno, new Dictionary<string, string>())
    {
        _valor = valor;
    }

    private Result(Result fallo)
        : base(fallo.Tipo, fallo.Errores)
    {
    }

    /// <summary>Valor de la operación. Solo existe si <see cref="Result.EsExito"/> es verdadero.</summary>
    public T Valor => EsExito
        ? _valor!
        : throw new InvalidOperationException("Un resultado fallido no tiene valor.");

    public static Result<T> Exito(T valor) => new(valor);

    /// <summary>Crea un resultado correcto con su valor.</summary>
    public static Result<T> Success(T valor) => Exito(valor);

    /// <summary>Crea un error de validación con su mensaje y devuelve un <see cref="Result{T}"/>.</summary>
    public static new Result<T> Error(string mensaje) => Fallo(Result.Error(mensaje));

    /// <summary>Convierte un fallo sin valor (validación, no encontrado o conflicto) en un Result&lt;T&gt;.</summary>
    public static Result<T> Fallo(Result fallo)
    {
        if (fallo.EsExito)
        {
            throw new ArgumentException("Se esperaba un resultado fallido.", nameof(fallo));
        }

        return new Result<T>(fallo);
    }

    public static implicit operator Result<T>(T valor) => Exito(valor);
}
