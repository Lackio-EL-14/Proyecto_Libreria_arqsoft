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

### Front: `AddOpenIdConnect` (US-125)

```csharp
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "Libreria.Sesion";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);   // igual que el token
        options.SlidingExpiration = false;
        options.AccessDeniedPath = "/Cuenta/AccesoDenegado";
    })
    .AddOpenIdConnect(options =>
    {
        var keycloak = builder.Configuration.GetSection("Keycloak");
        options.Authority = keycloak["Authority"];            // http://localhost:8080/realms/libreria
        options.ClientId = "libreria-front";
        options.ClientSecret = keycloak["ClientSecret"];      // Keycloak__ClientSecret (entorno)
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;                               // S256
        options.SaveTokens = true;                            // access_token para el handler Bearer
        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "preferred_username",
            RoleClaimType = "roles"
        };
        // CallbackPath y SignedOutCallbackPath por defecto: /signin-oidc y /signout-callback-oidc
    });
```

### Gateway y los 4 MS: `AddJwtBearer` (US-126)

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var keycloak = builder.Configuration.GetSection("Keycloak");
        options.Authority = keycloak["Authority"];            // descarga las claves del JWKS
        options.Audience = keycloak["Audience"];              // libreria-api
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = keycloak["Authority"],
            ValidateAudience = true,
            ValidAudience = keycloak["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            NameClaimType = "preferred_username",
            RoleClaimType = "roles"
        };
    });
```

`appsettings.json` (sin secretos):

```json
"Keycloak": {
  "Authority": "http://localhost:8080/realms/libreria",
  "Audience": "libreria-api"
}
```

Respuestas: sin token, firma alterada, vencido o audiencia equivocada → **401**; rol insuficiente → **403**.

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

## 7. Retiro del login transitorio (al cerrar US-125)

US-124 se integró por etapas, así que el login propio sigue funcionando hasta que el Front inicie sesión con OIDC. En el PR de US-125 se retira, con su comentario `TRANSITORIO`:

- [ ] Ruta `g2-auth` (`/api/auth/{**resto}`) de `src/Libreria.Gateway/yarp.g2.json` y sus pruebas en `tests/Libreria.Gateway.Tests`.
- [ ] `Controllers/AuthController.cs`, `ILoginUseCase`, `LoginUseCase`, `LoginValidator`, `DTOs/Auth/*` y `TestDoubles/LoginUseCaseTests.cs`.
- [ ] `IPasswordHasher`, `Argon2PasswordHasher`, el paquete `Konscious.Security.Cryptography.Argon2` y `FakePasswordHasher`.
- [ ] `ChangePasswordUseCase`, que pasa a usar `IProveedorIdentidad` (US-129).
- [ ] `User.PasswordHash` y `User.TemporaryPasswordExpiresAt`, las columnas `PasswordHash` y `ContrasenaTemporalExpiracion` (con un `ALTER TABLE … DROP COLUMN` idempotente) y los hashes de `50_datos_semilla.sql`.
- [ ] `CurrentUserMiddleware` y `X-Usuario-*` en el MS y en el Front (`UsuarioActualDelegatingHandler`), reemplazados por el token (US-125, US-126 y US-127).
- [ ] `api/auth/login` en `ServicioUsuariosClient` y la página `Cuenta/Login` del Front.

Mientras tanto, el login transitorio usa las credenciales de desarrollo anteriores, que el equipo ya conoce, y Keycloak usa las de `.env`. Son **independientes**.
