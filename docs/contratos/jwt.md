# JWT

| Aspecto | Contrato |
|---|---|
| Algoritmo | **RS256**. Solo Usuarios tiene la clave privada; el gateway y los 4 MS tienen la pública |
| Claves | Un par de desarrollo generado una vez por Gabriel. Privada: archivo fuera del repo, ruta en `Jwt__ClavePrivadaRuta` (solo Usuarios), compartida por el canal privado del equipo. Pública: en `Jwt:ClavePublicaPem` del `appsettings.json` de cada servicio y del gateway (se puede subir) |
| Emisor / audiencia | `iss` = `Libreria.Usuarios` · `aud` = `Libreria` |
| Claims | `sub` = `PublicId` del usuario · `name` = nombre de usuario · `role` = `Administrador` \| `Vendedor` · `iniciales` (por ejemplo `J.J.P.R.`) · `debe_cambiar_contrasena` = `true` \| `false` |
| Duración | 60 minutos, sin refresh token. La cookie del Front expira junto con el token |
| Transporte | `Authorization: Bearer <token>` en toda llamada Front → gateway → MS y MS → MS |
| Respuestas | Sin token, firma alterada o vencido → **401**. Token con `debe_cambiar_contrasena = true` → **403** en todo, salvo `PUT api/usuarios/contrasena` |
| Políticas *(propuestas)* | Política por defecto: usuario autenticado y contraseña vigente. `PuedeCambiarContrasena`: solo usuario autenticado. Solo `POST api/auth/login` es anónima |

**Estado actual (antes de US-125/126):** el Front todavía no tiene token; `UsuarioActualDelegatingHandler` envía los encabezados `X-Usuario-Id` y `X-Usuario-Nombre`. Ese handler es el punto donde Gabriel agrega el `Authorization: Bearer` (lo reciben los 4 clientes tipados, porque todos se registran con él).
