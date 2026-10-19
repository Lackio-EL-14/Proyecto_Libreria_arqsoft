Guía de comprobación de US-71

La baja es lógica y no incluye reactivación. Utiliza cuentas de prueba que puedas dejar Inactivas. No necesitas cambiar el esquema. No modifiques usuarios reales para preparar estos casos.

Preparación

1. Abre PowerShell en la raíz del proyecto, donde está LibreriaSystem.slnx.
2. Inicia SQL Server con `docker compose up -d db`.
3. Con las aplicaciones detenidas, ejecuta:

```powershell
dotnet build LibreriaSystem.slnx
dotnet test tests/Libreria.Application.Tests/Libreria.Application.Tests.csproj
```

La suite normal ejecuta 96 pruebas y omite las cuatro de integración SQL hasta configurarlas explícitamente. No necesita modificar la base local.

4. Inicia dos procesos desde dos terminales distintas, ambas en la raíz del proyecto:

```powershell
# Terminal A
dotnet run --project src/Libreria.Client/Libreria.Client.csproj --no-build --no-restore --urls http://localhost:5081
```

```powershell
# Terminal B
dotnet run --project src/Libreria.Client/Libreria.Client.csproj --no-build --no-restore --urls http://localhost:5082
```

Ambos procesos usan la misma configuración local de SQL, el mismo nombre de aplicación de Data Protection y el mismo directorio de claves ya configurado. No cambies DB_CONNECTION_STRING para uno solo de los procesos. Usa siempre localhost en ambos puertos.

Criterio 1: modal compartido y baja lógica

1. En una ventana normal abre http://localhost:5081 y entra como administrador. En los datos de ejemplo: `admin` / `Admin2026!`.
2. Pulsa Usuarios. Identifica una cuenta de prueba Activa o Pendiente distinta de la tuya.
3. Pulsa Dar de baja en su fila. Debe abrirse el modal compartido, con el nombre correcto y botones Cancelar y Confirmar baja.
4. Pulsa Cancelar. El usuario debe conservar su estado. Repite y confirma la baja.
5. Debe aparecer Usuario dado de baja correctamente. En el filtro Todos o Inactivo debe seguir existiendo la fila con estado Inactivo, sin botón de baja.
6. Comprueba en las herramientas del navegador que el POST va a `/Usuarios?handler=DarDeBaja` y utiliza un identificador protegido, no un UsuarioId numérico. El formulario también incluye un token antifalsificación.

Si tienes un cliente SQL, comprueba la fila sin consultar credenciales:

```sql
SELECT PublicId, NombreUsuario, Estado, FechaModificacion, UsuarioModificacionId
FROM dbo.Usuario
WHERE NombreUsuario = N'nombre_del_usuario_de_prueba';
```

La fila y su PublicId deben conservarse. Estado debe ser Inactivo y debe existir auditoría de modificación. Esta consulta es de verificación local; los IDs internos no se muestran en la página.

Criterio 2: login genérico

1. Cierra cualquier sesión de esa cuenta y entra a /Cuenta/Login.
2. Introduce el usuario que acabas de dar de baja y su contraseña anterior correcta.
3. Debe rechazarse el acceso con Usuario o contraseña incorrectos.
4. Repite con una cuenta Pendiente ya registrada, sin activarla. El mensaje debe ser exactamente el mismo y no debe mencionar Inactivo ni Pendiente.
5. Como comparación, una cuenta Activa con contraseña correcta debe seguir entrando.

No se ha añadido una pantalla para activar usuarios ni modificar contraseñas. Si no dispones de una cuenta Pendiente para este caso manual, ejecuta la prueba de integración indicada al final: prepara ambos estados en una base temporal y verifica el login real por HTTP.

Criterio 3: invalidación de sesión entre réplicas

Haz este caso ANTES de confirmar la baja del criterio 1, o usa otra cuenta de prueba Activa.

1. Conserva al administrador en la ventana normal, en http://localhost:5081/Usuarios.
2. Abre una ventana privada o un navegador con perfil separado y entra como la cuenta de prueba Activa en http://localhost:5082. Por ejemplo, `vendedor` / `Vendedor2026!` si esa cuenta todavía está Activa.
3. En esa ventana entra a /Ventas y confirma que accede. Deja la sesión abierta; no cierres sesión.
4. Desde la ventana del administrador, en el puerto 5081, da de baja a esa cuenta con el modal.
5. Vuelve a la ventana privada del puerto 5082 y recarga /Ventas.
6. Debe redirigirse al login y eliminarse la cookie de sesión. No debes seguir viendo contenido protegido.

La invalidación se comprueba en la siguiente petición que valida la cookie; una página ya cargada no desaparece automáticamente. Una actualización de una página estática tampoco sustituye la petición a /Ventas.

Los dos puertos corresponden a procesos independientes. La consulta del estado se hace en SQL en cada validación de cookie, sin temporizador ni caché local. En contenedores, todas las réplicas deben ejecutar la versión nueva y compartir la misma base y las claves de Data Protection, como establece el Compose existente.

Criterio 4: baja propia y último administrador

1. Entra como administrador y abre Usuarios.
2. Intenta dar de baja tu propia fila con el modal.
3. Si hay otro administrador Activo, debe aparecer No puede darse de baja a sí mismo. Tu estado debe seguir siendo Activo y tu sesión debe continuar funcionando.
4. Si eres el único administrador Activo, debe aparecer No se puede dar de baja al último administrador activo. Tu fila y tu acceso deben conservarse.
5. Si dispones de dos administradores de prueba, abre sus sesiones en perfiles distintos e intenta que se den de baja mutuamente al mismo tiempo. Como máximo una baja debe confirmarse; debe conservarse al menos un administrador Activo. El segundo operador puede recibir un rechazo de la operación o perder su sesión si ya fue dado de baja.

La prueba automatizada de concurrencia hace exactamente el último caso en SQL, sin desactivar administradores de LibreriaDb. No necesitas crear otra funcionalidad ni modificar roles reales para comprobarla.

Integración reproducible con SQL y dos procesos

Detén las instancias anteriores con Ctrl+C antes de compilar. Requiere SQL Server encendido y permisos para crear bases de prueba. Este comando usa la conexión local de appsettings.json, crea únicamente bases US71_Test_<guid>, inicia dos procesos de prueba y elimina las bases y procesos creados al finalizar. No cambia LibreriaDb. La variable de conexión se elimina de la terminal al terminar y no se imprime.

```powershell
$env:LIBRERIA_US71_SQL_TEST_CONNECTION = (Get-Content -Raw -Encoding UTF8 src/Libreria.Client/appsettings.json | ConvertFrom-Json).ConnectionStrings.LibreriaDb
try {
    dotnet test tests/Libreria.Application.Tests/Libreria.Application.Tests.csproj --filter FullyQualifiedName~UsuarioSeguridadSqlTests
} finally {
    Remove-Item Env:LIBRERIA_US71_SQL_TEST_CONNECTION -ErrorAction SilentlyContinue
}
```

Deben aprobarse cuatro pruebas. Incluyen el diagnóstico del alta sin configuración, la creación de un usuario Pendiente con correo local y su baja, la conservación de filas y auditoría, el rechazo sin antifalsificación, la protección del identificador, la baja del último administrador, el mismo mensaje de login para Pendiente e Inactivo, la revocación de la misma cookie anterior en ambas réplicas y dos bajas administrativas concurrentes.

Si una comprobación falla, conserva la URL, el rol utilizado, el mensaje y el estado HTTP. No elimines volúmenes ni reinicies los datos. Asegúrate de que ambos procesos usan el build nuevo y la misma base de datos. Las cuentas desactivadas durante las pruebas manuales permanecen Inactivas; reactivarlas no forma parte de US-71.
