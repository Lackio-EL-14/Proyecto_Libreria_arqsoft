namespace Libreria.Application.Ports.Secondary;

// US-40: el núcleo pregunta "quién está operando" sin saber que existe
// una cookie, un HttpContext o claims. El adaptador vive en Infrastructure.
public interface IUsuarioActual
{
    int? UsuarioId { get; }
}
