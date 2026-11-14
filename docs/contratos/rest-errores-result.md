# REST, errores y `Result`

## Convenciones

- JSON; rutas `api/<recurso>`; fechas ISO 8601; dinero `decimal`.
- **Solo `PublicId` (GUID) hacia afuera**, nunca ids numéricos.
- **Contrato estable:** los endpoints que hoy existen **conservan ruta y DTO**; solo cambia el servicio que los atiende (el gateway enruta). Ver [rest-por-servicio.md](rest-por-servicio.md).

## `Result` / `Result<T>`

Cada MS tiene su copia en `Comun/Result.cs`, dentro de la capa más interna que lo use (no hay bibliotecas compartidas).

| Miembro | Significado |
|---|---|
| `EsExito` | `true` si la operación se completó |
| `Valor` | Resultado (solo en `Result<T>`) |
| `Errores` | Diccionario campo → mensaje |
| `Tipo` | `Validacion`, `NoEncontrado` o `Conflicto` |

Las validaciones de negocio devuelven `Result`; las excepciones quedan solo para errores inesperados.

## Traducción en el controlador

| Situación | HTTP | Cuerpo |
|---|---|---|
| `Validacion` | **400** | `ValidationProblemDetails` |
| `NoEncontrado` | **404** | `ProblemDetails` |
| `Conflicto` | **409** | `ProblemDetails` |
| Servicio dependiente caído | **503** | `ProblemDetails` |
| Inesperado | **500** | `ProblemDetails` |

## Lo que espera el Front

`ApiClientBase` (Front) lee las respuestas así:

- **Lecturas tipadas**: 404 → `null` · 502/503/504 o error de red → `ServicioNoDisponibleException` (página `ServicioNoDisponible`) · otro error → `HttpRequestException`.
- **Escrituras tipadas** (`ResultadoApi`): 2xx → correcto · 404 → `NoEncontrado` (o el `detail`/`mensaje` si viene) · 409 → mensaje (`detail` o `mensaje`) · 400 → errores por campo, sea `ValidationProblemDetails` (`errors`, la clave `Nombre` llega a la vista como `Input.Nombre`) o un diccionario plano `campo → mensaje`; si no, el mensaje. Sin conexión → mensaje «No se pudo conectar con el servicio de …». Nunca lanzan.
- **Llamadas genéricas** (`GetAsync`, `PostAsync`, …): 502/503/504 o error de red → `ServicioNoDisponibleException` (página `ServicioNoDisponible`); otro error → `ApiClientException` con `detail`/`title` y `errors` de `ProblemDetails`.
