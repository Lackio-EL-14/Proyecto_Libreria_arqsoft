# Patrón Result (US-139)

Cada MS tiene **su copia** en `Domain/Result.cs` (namespace `Libreria.Servicio.<Servicio>.Domain`), sin biblioteca compartida.

## Forma

| Miembro | Para qué |
|---|---|
| `bool IsSuccess` | La operación salió bien |
| `bool IsError` | La operación falló (siempre lo contrario de `IsSuccess`) |
| `Result.Success()` · `Result<T>.Success(valor)` | Resultado correcto |
| `Result.Error(mensaje)` · `Result<T>.Error(mensaje)` | Error de validación con su mensaje; devuelve el mismo tipo de objeto → **400** |
| `string[] Errors()` | Arreglo con los mensajes de error acumulados (vacío si tuvo éxito) |
| `T Valor` | Valor de `Result<T>`; solo existe si `IsSuccess` |

Para distinguir otros códigos (lo permite el contrato 6.5) se conservan:

| Miembro | HTTP |
|---|---|
| `Result.Validacion(campo, mensaje)` · `Result.Validacion(diccionario)` | **400**, con el error ligado a su campo del formulario |
| `Result.NoEncontrado(mensaje)` | **404** |
| `Result.Conflicto(campo, mensaje)` | **409** (duplicado) |
| `Result<T>.Fallo(otroResult)` | Pasa un fallo sin valor a un `Result<T>` |
| `Tipo` · `Errores` · `EsExito` | Tipo de error, diccionario campo → mensaje, sinónimo de `IsSuccess` |

`Error(mensaje)` deja el mensaje sin campo (clave vacía), así que la UI lo muestra como error general. Si el mensaje corresponde a un campo, usa `Validacion(campo, mensaje)`.

## ¿Result o excepción?

| Caso | Usar |
|---|---|
| Dato inválido, regla de negocio incumplida, recurso inexistente, duplicado | **`Result`** |
| BD caída, red, tiempo agotado | **Excepción** → `ManejadorExcepciones` responde **503** |
| Configuración faltante, error de programación (`null` donde no debe), estado imposible | **Excepción** → **500** |

Ninguna excepción de negocio debe salir de `Application`. Si el driver de la BD lanza un error de duplicado, el repositorio lo traduce (`ValorDuplicadoException`) y el servicio lo convierte en `Result.Conflicto`.

En el controlador: `return resultado.AActionResult(...)` (`ResultadoHttpExtensions`), que produce `ValidationProblemDetails` (400/409) o `ProblemDetails` (404).

## Antes y después

Antes (excepción para una validación; el controlador necesitaba `try/catch`):

```csharp
if (!string.Equals(request.NewPassword, request.Confirmation, StringComparison.Ordinal))
{
    throw new ArgumentException("La confirmación no coincide con la nueva contraseña.");
}
```

Después:

```csharp
using Libreria.Servicio.Usuarios.Domain;

public async Task<Result> ExecuteAsync(ChangePasswordRequest request)
{
    if (!string.Equals(request.NewPassword, request.Confirmation, StringComparison.Ordinal))
    {
        return Result.Validacion(nameof(request.Confirmation),
            "La confirmación no coincide con la nueva contraseña.");
    }

    // ...
    return Result.Success();
}

// Controlador, sin try/catch:
var resultado = await _changePassword.ExecuteAsync(request);
return resultado.AActionResult();      // 204, o 400 con los mensajes
```

## Estado por MS

| MS | `Domain/Result.cs` | `Application/Comun/Result.cs` | `ManejadorExcepciones` |
|---|---|---|---|
| Clientes | ✅ | retirado | ✅ |
| Proveedores | ✅ | retirado | ✅ |
| Productos | ✅ | retirado | ✅ |
| Usuarios | ✅ | **sigue**, con los mismos miembros (lo usa el código del Grupo 2) | pendiente |

**Usuarios:** la migración se hace **de una vez**, no archivo por archivo, porque `Presentation/Comun/ResultadoHttpExtensions.cs` recibe un solo tipo de `Result`. Hay que reemplazar `using Libreria.Servicio.Usuarios.Application.Comun;` por `using Libreria.Servicio.Usuarios.Domain;` en todos los archivos que lo usan (hoy 5 en `src` y 5 en `tests`) y borrar `Application/Comun/Result.cs`. La API es compatible, así que no cambia nada más. Lo coordina Edson con el Grupo 2 cuando sus PR de Usuarios estén en `develop`. Mientras tanto, el código nuevo sigue usando `Application.Comun`: ese `Result` ya tiene los mismos miembros, incluidos `IsSuccess`, `IsError`, `Success`, `Error` y `Errors()`, así que se puede escribir en la forma nueva desde ya. No mezcles los dos `using` en un mismo archivo: `Result` quedaría ambiguo.
