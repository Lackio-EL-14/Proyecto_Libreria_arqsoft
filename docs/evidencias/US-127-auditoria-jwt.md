# US-127: auditoria con la identidad JWT

## Cambios exclusivos de la historia

- Clientes, Proveedores y Productos: Program.cs registra IHttpContextAccessor y el adaptador scoped UsuarioActualDesdeToken, en todos los entornos.
- Cada adaptador lee sub (GUID no vacio) y preferred_username de una identidad autenticada. No usa configuracion UsuarioDesarrollo ni cabeceras X-Usuario-*.
- Usuarios: IUserRepository, GetUserNamesUseCase y UserRepository resuelven los GUID de auditoria por KeycloakId, devolviendo el diccionario indexado por ese mismo GUID. El doble FakeUserRepository se adapta al nuevo nombre del metodo.
- Tres archivos UsuarioActualDesdeTokenTests.cs cubren identidad autenticada, usuario vendedor, sub invalido/vacio, identidad anonima, ausencia de contexto, cabeceras falsas y cambio de actor por peticion.
- AuditoriaService y _AuditoriaInfo ya consumen el diccionario GUID/nombre; no requieren cambios.
- Las clases UsuarioActualDesarrollo se conservan por compatibilidad con sus pruebas antiguas, pero no se registran en las aplicaciones.

## Validacion automatica

```powershell
dotnet build LibreriaSystem.slnx --artifacts-path .artifacts-us127
dotnet test LibreriaSystem.slnx --artifacts-path .artifacts-us127 --no-build --no-restore
```

Resultado: compilacion sin errores; 405 pruebas correctas y 8 omitidas.
Existen advertencias previas NU1902/NU1903 de System.Security.Cryptography.Pkcs.
No se modificaron paquetes ni configuraciones de infraestructura.

## Prueba desde localhost:5000

Reiniciar los seis procesos para cargar la nueva compilacion (Ctrl+C en la consola
que los inicio, luego .\run-all.ps1). No ejecutar dos instancias a la vez.
La compilacion aislada anterior no actualiza procesos que estaban abiertos.

1. Iniciar sesion como vendedor. Crear un cliente identificable como prueba US127.
   Abrir su detalle: Creado por debe mostrar Vendedor Mostrador.
2. Editar ese cliente como vendedor. Modificado por debe mostrar el vendedor y la
   fecha nueva; Creado por debe conservarse.
3. Cerrar sesion e iniciar como admin. Editar el mismo cliente: Modificado por debe
   mostrar Administrador Sistema, manteniendo al vendedor como creador.
4. Dar de baja y reactivar el cliente. Verificar Modificado por y fecha. Usar solo
   registros de prueba para no alterar datos de companeros.
5. Como admin, crear y editar un proveedor y cambiar su estado. Revisar el detalle:
   creador y modificador deben corresponder al administrador autenticado.
6. Como admin, crear y editar Producto, Categoria y Marca. Cambiar el costo del
   producto: revisar tambien el historico y su usuario. Probar baja/reactivacion
   cuando la pantalla lo permita.
7. Como vendedor, verificar que Proveedores, Productos y Usuarios siguen restringidos;
   esta historia no amplia permisos para realizar una prueba cruzada.
8. En Usuarios, las operaciones existentes de alta/baja/cambio de contrasena ya usan
   el actor JWT. Para cambios de contrasena usar una cuenta de prueba, no la de otro
   integrante. US-128 y US-129 no se desarrollan como parte de este cambio.

## Evidencia de persistencia pendiente de ejecucion manual

Comparar UsuarioCreacionId / UsuarioModificacionId y fechas en Cliente (MySQL),
Proveedor (SQL Server) y documentos Producto/Categoria/Marca e historico (MongoDB)
con el sub del actor. En Usuarios, buscar ese GUID en KeycloakId, no en PublicId.
Los datos semilla tienen ambos GUID iguales: para comprobar esta distincion se
necesita un usuario de prueba con PublicId diferente de KeycloakId. No cambiar los
identificadores de usuarios existentes para simularlo.

Esta entrega verifica compilacion y pruebas automatizadas. No se ha ejecutado aun
la prueba cruzada de escrituras reales/consulta de las cuatro bases con este codigo.

## Separacion de la reparacion anterior

No incluir en US-127: .env.example, .gitignore, README.md, docker-compose.yml,
keycloak/configurar.sh, run-all.ps1, AutenticacionKeycloakExtensions.cs,
.gitattributes, db/inicializar-sqlserver.sh ni scripts/verificar-login.mjs.
Estos cambios locales ya existian al empezar esta historia y se conservan intactos.
