# Keycloak y JWT (US-124)

Keycloak es el proveedor de identidad: emite los JWT y se encarga del login, las sesiones, las contraseñas y el cambio obligatorio en el primer ingreso. Este archivo **reemplaza** a `jwt.md` (contrato de la versión 1, con emisor propio).

Archivos: `keycloak/realm-libreria.json` (realm versionado, **sin secretos**), `keycloak/usuarios-semilla.json` (admin y vendedor con sus GUID fijos) y `keycloak/configurar.sh` (secretos y contraseñas desde `.env`). Cómo levantarlo: sección «Keycloak» del `README`.

## 1. Datos del realm

| Aspecto | Valor |
|---|---|
| Imagen | `quay.io/keycloak/keycloak:26.8.1` (modo desarrollo, contenedor `keycloak`, puerto 8080) |
| Realm | `libreria` · nombre visible **«Librería»** · idioma `es` (internacionalización activa, único idioma) |
| Authority (`Keycloak:Authority`) | `http://localhost:8080/realms/libreria` |
| Emisor (`iss`) | `http://localhost:8080/realms/libreria` (fijo con `KC_HOSTNAME`, aunque se llame desde otro contenedor) |
| Discovery / JWKS | `{Authority}/.well-known/openid-configuration` · `{Authority}/protocol/openid-connect/certs` |
| Audiencia (`Keycloak:Audience`) | `libreria-api` |
| Firma | **RS256** (claves del realm; el gateway y los MS las toman solos del JWKS) |
| Roles del realm | `Administrador` · `Vendedor` |
| Token de acceso | 60 minutos (`accessTokenLifespan = 3600`) |
| Sesión SSO | 60 minutos de inactividad, 10 horas como máximo |
| Refresh token | Rotación: cada refresh token sirve **una sola vez** (`revokeRefreshToken`, `refreshTokenMaxReuse = 0`) |
| Fuerza bruta | Activa: 5 fallos → espera de 60 s que crece hasta 15 min; el bloqueo no es permanente |
| Política de contraseñas | `length(8) and digits(1) and upperCase(1) and lowerCase(1) and specialChars(1) and notUsername(undefined)` |
| Primer ingreso | Contraseña **temporal** → acción `UPDATE_PASSWORD`: Keycloak **no emite el token** hasta que el usuario la cambia (`invalid_grant: Account is not fully set up`) |
| Registro, «olvidé mi contraseña», login con correo | Desactivados (las cuentas las crea el Administrador, US-130) |

## 2. Clientes

| Cliente | Tipo | Flujos | Lo usa |
|---|---|---|---|
| `libreria-front` | Confidencial | **Solo** código de autorización con **PKCE S256**. Sin implícito, sin direct grants, sin cuenta de servicio | Front (US-125) |
| `libreria-usuarios-admin` | Confidencial | **Solo** cuenta de servicio, con `manage-users` y `view-users` de `realm-management`. Sin acceso a `master` | Adaptador de Keycloak del MS Usuarios |
| `libreria-usuarios-verificacion` *(nuevo en US-124)* | Confidencial | **Solo** direct grant. Su token **no** lleva `aud = libreria-api` ni roles: no sirve contra las API | `VerificarContrasenaAsync` del MS Usuarios (US-129) |
| `libreria-pruebas` | Público | **Solo** direct grant. **SOLO DESARROLLO**: `configurar.sh` lo deshabilita si `KEYCLOAK_HABILITAR_CLIENTE_PRUEBAS` ≠ `true` | Swagger, `.http`, pruebas manuales |

URIs exactas de `libreria-front`, sin comodines:

| Campo | Valor |
|---|---|
| Redirect URI | `http://localhost:5000/signin-oidc` |
| Post-logout redirect URI | `http://localhost:5000/signout-callback-oidc` |
| Web origin | `http://localhost:5000` |

`libreria-front` y `libreria-pruebas` tienen `fullScopeAllowed = false` y solo `Administrador` y `Vendedor` en su alcance, así que `roles` no trae `offline_access` ni `default-roles-libreria`.

**Secretos:** nunca en el repositorio. `configurar.sh` los asigna desde `.env` (`KEYCLOAK_FRONT_CLIENT_SECRET`, `KEYCLOAK_USUARIOS_ADMIN_CLIENT_SECRET`, `KEYCLOAK_USUARIOS_VERIFICACION_CLIENT_SECRET`). Cada aplicación .NET recibe el suyo como `Keycloak__ClientSecret` (y el MS Usuarios, además, `Keycloak__VerificacionClientSecret`) por entorno o user-secrets.

## 3. Claims

| Claim | Contenido | Uso |
|---|---|---|
| `sub` | Id del usuario en Keycloak (GUID). En `admin` y `vendedor` es su `PublicId` semilla | Auditoría: `UsuarioCreacionId` / `UsuarioModificacionId`; `KeycloakId` en la BD de Usuarios |
| `preferred_username` | Nombre de usuario (`admin`, `jperezr`) | `NameClaimType` |
| `name` | Nombre completo | Interfaz |
| `email` | Correo | Interfaz |
| `roles` | Arreglo **plano** de roles del realm: `["Administrador"]` | `RoleClaimType` |
| `aud` | `libreria-api` | Validación de audiencia |
| `azp` | Cliente que pidió el token (`libreria-front` o `libreria-pruebas`) | Diagnóstico |

Ejemplo de **payload decodificado** de `admin` (sin firma; `exp`, `iat`, `jti` y `sid` son ilustrativos):

```json
{
  "exp": 1795600800,
  "iat": 1795597200,
  "jti": "00000000-0000-0000-0000-00000000abcd",
  "iss": "http://localhost:8080/realms/libreria",
  "aud": "libreria-api",
  "sub": "a0000000-0000-0000-0000-000000000001",
  "typ": "Bearer",
  "azp": "libreria-front",
  "sid": "00000000-0000-0000-0000-00000000dcba",
  "acr": "1",
  "scope": "openid profile email",
  "email_verified": true,
  "roles": ["Administrador"],
  "name": "Administrador Sistema",
  "preferred_username": "admin",
  "given_name": "Administrador",
  "family_name": "Sistema",
  "email": "admin@libreria.local"
}
```

Encabezado: `{"alg": "RS256", "typ": "JWT", "kid": "<id de la clave del realm>"}`.

## 4. Configuración en .NET

En los dos casos, `MapInboundClaims = false` conserva los nombres originales (`sub`, `roles`, `preferred_username`) en lugar de traducirlos a las URI de `ClaimTypes`.

### Front: `AddOpenIdConnect` (US-125, implementado)

Vive en `src/Libreria.Client/Seguridad/AutenticacionKeycloakExtensions.cs` y se registra con una línea en `Program.cs`:

```csharp
builder.Services.AddAutenticacionKeycloak(builder.Configuration, builder.Environment);
```

Opciones clave (no cambiarlas sin avisar):

| Opción | Valor | Por qué |
|---|---|---|
| `DefaultScheme` / `DefaultChallengeScheme` | Cookie / OpenID Connect | La cookie cifrada `Libreria.Sesion` es la sesión local; sin sesión se va directo a Keycloak |
| `ClientId` / `ClientSecret` | `Keycloak:ClientId` = `libreria-front` / `Keycloak__ClientSecret` (user-secrets o entorno) | Si falta el secreto, el Front no arranca y el error lo nombra |
| `ResponseType` + `UsePkce` | `code` + PKCE S256 | Flujo de código. .NET 10 usa además PAR (`request_uri`), porque Keycloak lo anuncia |
| `ResponseMode` + `CorrelationCookie`/`NonceCookie.SameSite` | `query` + `Lax` | Funciona en `http://localhost` sin cookies `SameSite=None` |
| `SaveTokens` | `true` | `BearerTokenDelegatingHandler` toma el `access_token` de la sesión |
| `UseTokenLifetime` + cookie de 60 min sin renovación | — | La sesión vence con el token |
| `GetClaimsFromUserInfoEndpoint` | `false` | El `id_token` ya trae `name`, `email` y `roles` |
| `MapInboundClaims` | `false` | Conserva `sub`, `preferred_username`, `roles` |
| `NameClaimType` / `RoleClaimType` | `preferred_username` / `roles` | `User.Identity.Name`, `IsInRole` y las políticas por rol |
| `ValidAlgorithms` | `RS256` | Solo la firma del realm |
| `SignedOutRedirectUri` | `/Cuenta/Login?motivo=cerrada` | Después del `end_session` de Keycloak |

Piezas del Front (`Seguridad/` y `Clients/Common/`):

- **`BearerTokenDelegatingHandler`:** agrega `Authorization: Bearer` a las llamadas de los 4 adaptadores. Si el token venció o un servicio responde **401**, lanza `SesionExpiradaException`; `SesionExpiradaFilter` cierra la sesión y lleva a `/Cuenta/Login?motivo=vencida`. *Transitorio hasta US-126/127:* solo hacia `/api/usuarios` envía también `X-Usuario-Id` (= `sub`) y `X-Usuario-Nombre` (= `preferred_username`); descarta cualquier `X-Usuario-*` que venga de antes.
- **`ValidarEstadoUsuarioCookieEvents`:** en cada petición comprueba el vencimiento del token y, con una caché de `Sesion:ValidacionEstadoSegundos` (20 s), si el `sub` sigue sin baja en el MS Usuarios. Una baja corta la sesión con el mensaje «Su cuenta está inactiva…».
- **`/Cuenta/Login`:** sin motivo, redirige a Keycloak. Con un motivo (`vencida`, `cerrada`, `inactiva`, `no-disponible`, `error`; textos en `MotivosSesion`), muestra el mensaje y el botón «Iniciar sesión» (con `prompt=login`). Así un rechazo nunca entra en bucle con la sesión SSO de Keycloak.
- **`/Cuenta/Logout`:** solo por POST con antiforgery. Cierra la cookie **y** la sesión de Keycloak.
- **`ClaimsKeycloak`:** `User.ObtenerSub()`, `User.NombreVisible()` y `User.RolesVisibles()` para las páginas y el layout.

### Gateway y los 4 MS: `AddJwtBearer` (US-126, implementado)

Cada MS tiene **su copia** en `Infrastructure/Seguridad/SeguridadJwtExtensions.cs`, y el gateway la suya en `Seguridad/SeguridadJwtExtensions.cs` (sin bibliotecas compartidas). En `Program.cs`:

```csharp
builder.Services.AddSwaggerGen(o => o.AgregarBearerDeKeycloak());           // botón «Authorize»
builder.Services.AddSeguridadJwt(builder.Configuration, builder.Environment);
// ...
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/salud", ...).AllowAnonymous();
```

En el gateway: `app.MapReverseProxy(...).RequireAuthorization()`. Sin un token válido no se reenvía nada.

Validación del token (idéntica en los 5):

| Parámetro | Valor |
|---|---|
| `Authority` | `Keycloak:Authority`: claves públicas del JWKS del realm, que el MS descarga y cachea |
| `ValidIssuer` / `ValidAudience` | `http://localhost:8080/realms/libreria` / `libreria-api` |
| `ValidateLifetime`, `RequireExpirationTime`, `RequireSignedTokens` | `true`, con `ClockSkew` de 30 s |
| `ValidAlgorithms` | solo `RS256` |
| `MapInboundClaims` | `false` |
| `NameClaimType` / `RoleClaimType` | `preferred_username` / `roles` |
| Política por defecto (`FallbackPolicy`) | **todo exige usuario autenticado**; solo `/salud` es anónima (y Swagger en Development) |
| Políticas | `SoloAdministrador` (rol Administrador) · `GestionClientes` (Administrador o Vendedor) |

`appsettings.json` de cada MS y del gateway (sin secretos):

```json
"Keycloak": {
  "Authority": "http://localhost:8080/realms/libreria",
  "Audience": "libreria-api"
}
```

Respuestas, como `application/problem+json` en español, con `WWW-Authenticate: Bearer`:

| Caso | Estado | `detail` |
|---|---|---|
| Sin token | 401 | «Falta el token de acceso (Authorization: Bearer).» |
| Firma alterada, otra clave, emisor o audiencia equivocados | 401 | «El token de acceso no es válido (firma, emisor o audiencia).» |
| Vencido | 401 | «El token de acceso venció. Inicie sesión de nuevo.» |
| Rol insuficiente | 403 | «Su rol no tiene permiso para esta operación.» |

**Matriz de roles en el servidor** (es la misma del Front):

| MS | Administrador | Vendedor |
|---|---|---|
| Clientes | todo | listar, ver, crear y editar; **no** `PATCH …/estado` |
| Proveedores | todo | 403 |
| Productos (`productos`, `categorias`, `marcas`, `historico`) | todo | 403 |
| Usuarios | todo | `PUT contrasena`, `GET {sub}/sesion-activa`, `GET nombres`, `GET nombres-publicos`; 403 en listar, crear, `roles` y baja |

**Usuario actual:** en Usuarios sale del token validado (`UsuarioActualDesdeToken`: `sub` y `preferred_username`); las cabeceras `X-Usuario-*` ya no existen. Clientes, Proveedores y Productos siguen con `UsuarioActualDesarrollo` hasta US-127.

**Pruebas sin Keycloak:** cada proyecto de pruebas tiene `Seguridad/TokensDePrueba.cs`, que firma tokens RS256 con una clave local, y `UsarJwtDePrueba()`, que hace que el servicio confíe en esa clave en lugar del JWKS. El resto de la validación es la real. Evidencia en vivo: `docs/evidencias/US-126-seguridad-jwt.md` y `.http`.

## 5. Token de prueba (solo desarrollo)

Con Keycloak levantado y `KEYCLOAK_HABILITAR_CLIENTE_PRUEBAS=true`. La contraseña se lee de una variable para que no quede en el historial de la consola:

```bash
# Bash (Linux, macOS, Git Bash)
read -rs KC_PASS && export KC_PASS     # escriba la contraseña de admin o vendedor (no se muestra)
TOKEN=$(curl -s http://localhost:8080/realms/libreria/protocol/openid-connect/token \
  -d grant_type=password -d client_id=libreria-pruebas \
  -d username=admin --data-urlencode "password=$KC_PASS" | sed -E 's/.*"access_token":"([^"]+)".*/\1/')
curl -s -H "Authorization: Bearer $TOKEN" http://localhost:5080/api/usuarios
```

```powershell
# PowerShell
$pass = Read-Host -AsSecureString "Contraseña"
$plano = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($pass))
$TOKEN = (Invoke-RestMethod -Method Post http://localhost:8080/realms/libreria/protocol/openid-connect/token `
  -Body @{ grant_type = "password"; client_id = "libreria-pruebas"; username = "admin"; password = $plano }).access_token
Invoke-RestMethod http://localhost:5080/api/usuarios -Headers @{ Authorization = "Bearer $TOKEN" }
```

En Swagger: botón «Authorize» → `Bearer <token>`. En un `.http`: `Authorization: Bearer {{token}}`, con `token` en un archivo de entorno que **no** se sube. No pegue tokens en el chat del equipo ni en capturas.

## 6. Puerto `IProveedorIdentidad` (MS Usuarios)

`Application/Ports/Secondary/IProveedorIdentidad.cs`. Todo devuelve el `Result` de `Domain/`; lo inesperado (Keycloak caído, secreto mal configurado) lanza una excepción.

| Método | Para | Errores previsibles (`Result`) |
|---|---|---|
| `CrearUsuarioAsync(UsuarioIdentidad) → Result<Guid>` | Alta, US-130. Devuelve el `sub` → `KeycloakId` | Conflicto si se repite el usuario o el correo |
| `AsignarRolAsync(id, rol)` | Alta, US-130. Idempotente | Rol inválido · no encontrado |
| `FijarContrasenaAsync(id, contrasena, temporal)` | `temporal: true` en el alta (US-130); `false` en el cambio (US-129) | **Un mensaje en español por regla incumplida** (Keycloak informa la primera) · no encontrado |
| `VerificarContrasenaAsync(usuario, contrasena)` | Contraseña actual, US-129 | «La contraseña actual es incorrecta.» · cambio pendiente · cuenta deshabilitada |
| `HabilitarAsync(id)` / `DeshabilitarAsync(id)` | Baja, US-128. Deshabilitar también **cierra sus sesiones** | No encontrado |
| `TieneCambioDeContrasenaPendienteAsync(id) → Result<bool>` | Estado «Pendiente», US-128 | No encontrado |
| `EliminarUsuarioAsync(id)` | Compensación si el alta falla a medias, US-130. Idempotente | — |

Adaptadores (`Infrastructure/Identidad/`), según `ProveedorIdentidad:Modo`:

- `Keycloak` (por defecto): `KeycloakProveedorIdentidad`. Configuración: `Keycloak:Authority`, `Keycloak:AdminClientId`, `Keycloak:VerificacionClientId` (en `appsettings.json`) + `Keycloak__ClientSecret` y `Keycloak__VerificacionClientSecret` (entorno).
- `EnMemoria` (solo `Development`): `ProveedorIdentidadEnMemoria`. Imita la política, la contraseña temporal y los roles, y trae `admin`/`vendedor` con sus GUID, sin contraseña. Para trabajar sin Keycloak: `dotnet run --project src/Libreria.Servicio.Usuarios --ProveedorIdentidad:Modo=EnMemoria`. En pruebas: `new ProveedorIdentidadEnMemoria()`; `FallarSiguienteLlamada` simula Keycloak caído.

Pruebas: `tests/Libreria.Servicio.Usuarios.Tests/Identidad/`. La de integración contra Keycloak real se activa con `KEYCLOAK_PRUEBAS_INTEGRACION=1` y los dos secretos en el entorno.

## 7. Retiro del login transitorio (hecho en US-125)

El login propio salió por completo en US-125:

- [x] Ruta `g2-auth` (`/api/auth/{**resto}`) de `yarp.g2.json` y sus pruebas: el gateway queda con 7 rutas y `/api/auth` responde 404.
- [x] `AuthController`, `ILoginUseCase`, `LoginUseCase`, `LoginValidator`, `DTOs/Auth/*` y sus pruebas.
- [x] `IPasswordHasher`, `Argon2PasswordHasher`, el paquete Konscious y `FakePasswordHasher`.
- [x] `ChangePasswordUseCase` usa `IProveedorIdentidad`: verifica la contraseña actual y fija la nueva en Keycloak. Devuelve `Result` (US-129, backend).
- [x] `DeactivateUserUseCase` también deshabilita al usuario en Keycloak, lo que cierra sus sesiones. Si Keycloak no responde, se revierte y devuelve 503 (US-128, backend).
- [x] `User.PasswordHash` y `TemporaryPasswordExpiresAt`, y las columnas `PasswordHash` y `ContrasenaTemporalExpiracion` (eliminadas con `DROP COLUMN` idempotente). La semilla ya no tiene hashes.
- [x] En el Front: `api/auth/login`, sus DTO, el formulario de login y `UsuarioActualDelegatingHandler`.

**Identidad del usuario actual = `sub`.** En el MS Usuarios, `IsActiveAdministratorAsync`, `IsSessionActiveAsync(Guid)` y el actor de la baja buscan por `KeycloakId`, no por `PublicId`. `GET api/usuarios/{keycloakId}/sesion-activa` es verdadero si el usuario **no está Inactivo** (Activo o Pendiente) y su rol está activo. «Pendiente» no bloquea la sesión porque Keycloak no emite el token hasta que se cambia la contraseña temporal.

**Cerrado en US-126:** `CurrentUserMiddleware` y las cabeceras `X-Usuario-*` se eliminaron. El MS Usuarios valida el JWT y toma al usuario actual del token; una cabecera falsificada no tiene efecto (hay una prueba que lo demuestra).
