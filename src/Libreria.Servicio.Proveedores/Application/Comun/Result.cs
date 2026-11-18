namespace Libreria.Servicio.Proveedores.Application.Comun;

public enum TipoError
{
    Ninguno,
    Validacion,
    NoEncontrado,
    Conflicto
}

/// <summary>Las reglas de negocio se expresan como valores; las excepciones quedan para fallos inesperados.</summary>
public class Result
{
    private static readonly IReadOnlyDictionary<string, string> SinErrores = new Dictionary<string, string>();

    protected Result(TipoError tipo, IReadOnlyDictionary<string, string> errores)
    {
        Tipo = tipo;
        Errores = errores;
    }

    public bool EsExito => Tipo == TipoError.Ninguno;
    public TipoError Tipo { get; }
    public IReadOnlyDictionary<string, string> Errores { get; }
    public static Result Exito() => new(TipoError.Ninguno, SinErrores);
    public static Result Validacion(IReadOnlyDictionary<string, string> errores) => new(TipoError.Validacion, ValidarErrores(errores));
    public static Result Validacion(string campo, string mensaje) => Validacion(new Dictionary<string, string> { [campo] = mensaje });
    public static Result NoEncontrado(string mensaje) => new(TipoError.NoEncontrado, new Dictionary<string, string> { [string.Empty] = mensaje });
    public static Result Conflicto(string campo, string mensaje) => new(TipoError.Conflicto, new Dictionary<string, string> { [campo] = mensaje });

    protected static IReadOnlyDictionary<string, string> ValidarErrores(IReadOnlyDictionary<string, string> errores)
    {
        if (errores is null || errores.Count == 0)
        {
            throw new ArgumentException("Un resultado fallido necesita al menos un error.", nameof(errores));
        }
        return errores;
    }
}

public sealed class Result<T> : Result
{
    private readonly T? _valor;
    private Result(T valor) : base(TipoError.Ninguno, new Dictionary<string, string>()) => _valor = valor;
    private Result(Result fallo) : base(fallo.Tipo, fallo.Errores) { }
    public T Valor => EsExito ? _valor! : throw new InvalidOperationException("Un resultado fallido no tiene valor.");
    public static Result<T> Exito(T valor) => new(valor);
    public static Result<T> Fallo(Result fallo)
    {
        if (fallo.EsExito) throw new ArgumentException("Se esperaba un resultado fallido.", nameof(fallo));
        return new Result<T>(fallo);
    }
    public static implicit operator Result<T>(T valor) => Exito(valor);
}
