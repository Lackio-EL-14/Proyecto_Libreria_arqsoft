namespace Libreria.Servicio.Clientes.Domain;

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
/// Patrón Result (US-139, contrato 6.5): las validaciones de negocio se devuelven como valor,
/// sin lanzar excepciones. Las excepciones quedan para lo inesperado (BD caída, red, configuración).
/// </summary>
/// <remarks>
/// Forma pedida por el docente: <see cref="IsSuccess"/>, <see cref="IsError"/>, <see cref="Success()"/>,
/// <see cref="Error(string)"/> y <see cref="Errors"/>. Se conservan <see cref="Tipo"/> y las fábricas
/// <see cref="Validacion(string, string)"/>, <see cref="NoEncontrado"/> y <see cref="Conflicto"/>
/// para distinguir 404 y 409, como permite el contrato.
/// </remarks>
public class Result
{
    private static readonly IReadOnlyDictionary<string, string> SinErrores =
        new Dictionary<string, string>();

    protected Result(TipoError tipo, IReadOnlyDictionary<string, string> errores)
    {
        Tipo = tipo;
        Errores = errores;
    }

    /// <summary>La operación salió bien.</summary>
    public bool IsSuccess => Tipo == TipoError.Ninguno;

    /// <summary>La operación falló. Siempre es lo contrario de <see cref="IsSuccess"/>.</summary>
    public bool IsError => !IsSuccess;

    /// <summary>Equivale a <see cref="IsSuccess"/>.</summary>
    public bool EsExito => IsSuccess;

    public TipoError Tipo { get; }

    /// <summary>Campo → mensaje. Vacío cuando la operación tuvo éxito.</summary>
    public IReadOnlyDictionary<string, string> Errores { get; }

    /// <summary>Devuelve el arreglo de mensajes de error acumulados (vacío si tuvo éxito).</summary>
    public string[] Errors() => Errores.Values.ToArray();

    /// <summary>Crea un resultado correcto.</summary>
    public static Result Success() => Exito();

    /// <summary>Crea un error de validación con su mensaje (se traduce a 400).</summary>
    public static Result Error(string mensaje) => Validacion(string.Empty, ValidarMensaje(mensaje));

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

    protected static string ValidarMensaje(string mensaje)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mensaje);
        return mensaje;
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

    /// <summary>Valor de la operación. Solo existe si <see cref="Result.IsSuccess"/> es verdadero.</summary>
    public T Valor => IsSuccess
        ? _valor!
        : throw new InvalidOperationException("Un resultado fallido no tiene valor.");

    /// <summary>Crea un resultado correcto con su valor.</summary>
    public static Result<T> Success(T valor) => Exito(valor);

    /// <summary>Crea un error de validación con su mensaje y devuelve un <see cref="Result{T}"/>.</summary>
    public static new Result<T> Error(string mensaje) => Fallo(Result.Error(mensaje));

    public static Result<T> Exito(T valor) => new(valor);

    /// <summary>Convierte un fallo sin valor (validación, no encontrado o conflicto) en un Result&lt;T&gt;.</summary>
    public static Result<T> Fallo(Result fallo)
    {
        if (fallo.IsSuccess)
        {
            throw new ArgumentException("Se esperaba un resultado fallido.", nameof(fallo));
        }

        return new Result<T>(fallo);
    }

    public static implicit operator Result<T>(T valor) => Exito(valor);
}
