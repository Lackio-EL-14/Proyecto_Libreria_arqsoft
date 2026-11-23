# Informe de pruebas manuales ejecutadas (22/11/2026)

**Casos codificados, nombre de cada prueba y pasos reproducibles**

Fecha de las pruebas: 22 de noviembre de 2026 \| macOS · Chrome · .NET
10 · Docker · rama develop

Documento para compartir con el grupo. Contiene únicamente casos para
los que hay ejecución registrada. Todos los casos muestran un solo
resultado: PASS o FAIL. No se agregaron pruebas por hacer.

# Resumen de resultados

| Grupo de pruebas          | PASS | FAIL | Total |
|---------------------------|------|------|-------|
| Plan funcional A–F        | 44   | 5    | 49    |
| Funcionales adicionales X | 13   | 0    | 13    |
| Arquitectura ARQ          | 18   | 0    | 18    |
| TOTAL                     | 75   | 5    | 80    |

Trazabilidad: los códigos A–F conservan la identificación de la matriz
original. Los códigos X son comprobaciones adicionales realizadas. ARQ
identifica verificaciones de arquitectura e infraestructura; algunas
sustentan los mismos flujos funcionales y no se presentan como 80 casos
de negocio independientes.

Repetición de pruebas: no reutilizar identificadores únicos si los
registros anteriores siguen en la BD; para los casos de alta/duplicidad
es preferible una copia controlada de datos o sustituir por
identificadores nuevos, anotando el cambio. No compartir contraseñas,
claves, secretos ni tokens.

# A. Clientes

Casos funcionales del módulo Clientes.

## **A1 \| Listado inicial y estados de clientes \[FAIL\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir Chrome e iniciar sesión con la cuenta Vendedor Mostrador.

**2.** Entrar en el menú «Clientes».

**3.** Seleccionar el filtro «Activos» y revisar los registros
iniciales.

**4.** Cambiar el filtro a «Inactivos» y localizar «Juan Carlos Pérez
Rojas».

**Esperado:** Encontrar los tres clientes semilla activos en un entorno
controlado.

**Observado:** Se registró Juan Carlos Pérez Rojas como Inactivo. La
causa podría ser una baja realizada en pruebas anteriores; repetir con
semillas verificadas antes de atribuirlo al código.

**Alcance de la evidencia:** El estado de los datos semilla pudo haberse
modificado antes de la prueba; no se estableció que el seeder esté
defectuoso.

## **A2 \| Ordenación por razón social \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Iniciar sesión como vendedor y abrir «Clientes».

**2.** Hacer clic una vez en el encabezado «Razón social».

**3.** Verificar el orden y la flecha del encabezado.

**4.** Hacer clic por segunda vez en «Razón social» y comprobar que el
orden se invierte.

**Esperado:** Alternar orden ascendente y descendente.

**Observado:** Se observó el cambio de orden en el listado.

## **A3 \| Búsqueda de cliente y limpieza de filtros \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir «Clientes» con sesión de vendedor.

**2.** Escribir «Quispe» en el campo de búsqueda y aplicar la búsqueda.

**3.** Comprobar que se muestra «María Fernanda Quispe Choque».

**4.** Pulsar «Limpiar» y verificar que vuelve el listado sin ese
filtro.

**Esperado:** Aparece María Fernanda Quispe Choque y luego regresa la
lista.

**Observado:** Búsqueda y restablecimiento verificados en el recorrido.

## **A4 \| Validación de alta de cliente con campos vacíos \[FAIL\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir «Clientes» con sesión válida.

**2.** Pulsar «+ Registrar nuevo cliente».

**3.** Dejar vacíos los campos «CI/NIT» y «Razón social».

**4.** Pulsar «Guardar» y leer el mensaje mostrado por el navegador o
formulario.

**Esperado:** Mensajes propios «El CI/NIT es obligatorio» y «La razón
social es obligatoria», por campo.

**Observado:** Chrome impidió el envío con «Completa este campo»; no se
presentaron de la forma exigida todos los mensajes personalizados.

## **A5 \| Registro y normalización de cliente \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Iniciar sesión como Vendedor Mostrador y abrir «Clientes».

**2.** Seleccionar «+ Registrar nuevo cliente».

**3.** Introducir CI/NIT «13622018» y razón social con espacios
sobrantes, basada en «juan josé pérez»; añadir un correo de prueba
válido.

**4.** Guardar y localizar el nuevo cliente en el listado.

**5.** Comprobar posteriormente su persistencia en MySQL.

**Esperado:** Persistencia con texto normalizado y visualización en el
listado.

**Observado:** Se registró el cliente; posteriormente se confirmó en
MySQL. Se usó CI/NIT 13622018, distinto del 9990001 del caso oficial.

**Alcance de la evidencia:** CI/NIT real de la ejecución: 13622018. El
caso del plan mencionaba otro valor.

## **A6 \| Rechazo de CI/NIT duplicado \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir «Clientes» y seleccionar «+ Registrar nuevo cliente».

**2.** Introducir el CI/NIT «13622018», utilizado previamente en A5;
completar los demás campos requeridos.

**3.** Pulsar «Guardar» y comprobar que se rechaza el identificador
repetido.

**Esperado:** Rechazar el duplicado con mensaje específico.

**Observado:** El sistema rechazó el CI/NIT duplicado.

## **A7 \| Edición de cliente con trazabilidad \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Iniciar sesión como Administrador Sistema.

**2.** Abrir «Clientes» y localizar el cliente que fue registrado como
vendedor en A5.

**3.** Entrar a «Editar», modificar la razón social y guardar.

**4.** Volver a «Editar» y examinar el bloque «Trazabilidad».

**5.** Comprobar los nombres «Vendedor Mostrador» (creación) y
«Administrador Sistema» (modificación).

**Esperado:** Mantener creador vendedor y modificador administrador, con
hora Bolivia.

**Observado:** Se vieron los nombres correctos: Vendedor Mostrador y
Administrador Sistema. La exactitud del huso horario no quedó comprobada
con medición independiente.

**Alcance de la evidencia:** Se comprobaron nombres de auditoría; la
exactitud de la zona horaria no se midió independientemente.

## **A8 \| Baja lógica de cliente \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Con sesión de administrador, abrir «Clientes».

**2.** Localizar el cliente de prueba activo y pulsar «Dar de baja».

**3.** Comprobar que aparece un modal de confirmación propio de la
aplicación.

**4.** Confirmar la operación y observar el aviso de éxito.

**5.** Comprobar que el cliente ya no figura en el filtro «Activos».

**Esperado:** Modal propio, confirmación y cambio de estado.

**Observado:** Baja y mensaje de confirmación observados.

## **A9 \| Reactivación de cliente \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir «Clientes» como administrador.

**2.** Seleccionar filtro «Inactivos».

**3.** Localizar el cliente dado de baja en A8 y pulsar «Reactivar».

**4.** Confirmar la acción en el modal.

**5.** Cambiar a «Activos» y comprobar que el cliente reaparece.

**Esperado:** El cliente vuelve a estar activo.

**Observado:** Reactivación comprobada en la UI.

## **A10 \| Restricción de bajas para el vendedor \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir otra sesión en Chrome con el rol Vendedor Mostrador.

**2.** Entrar a «Clientes» y revisar las acciones disponibles de un
cliente activo.

**3.** Seleccionar el filtro «Inactivos» y revisar las acciones de un
cliente inactivo.

**4.** Confirmar que «Dar de baja» y «Reactivar» no se ofrecen al
vendedor.

**Esperado:** No ofrecer baja ni reactivación al vendedor.

**Observado:** Acciones administrativas no visibles para vendedor.

## **ARQ-01 \| Estado de contenedores Docker \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir Terminal en la raíz del repositorio.

**2.** Ejecutar «docker compose ps».

**3.** Comprobar contenedores y puertos publicados.

**Observado:** Se observaron cinco contenedores saludables: MySQL
clientes 3308, SQL Server proveedores 1433, SQL Server usuarios 1434,
MongoDB 27017 y Keycloak 8080.

## **ARQ-02 \| Persistencia de Clientes en MySQL \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir DBeaver y conectarse a MySQL en «localhost:3308» con
credenciales locales del .env.

**2.** Abrir «libreria_clientes» → tabla «Cliente» → Datos.

**3.** Buscar el CI/NIT «13622018» y revisar la razón social.

**Observado:** Se encontraron cuatro filas, incluido el registro
«13622018» y «juan josé pérez actualizado».

## **ARQ-03 \| Persistencia de Proveedores en SQL Server \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir DBeaver y conectarse a SQL Server «localhost:1433» con
credenciales del entorno.

**2.** Seleccionar base «LibreriaProveedores» → «dbo.Proveedor».

**3.** Consultar los registros y localizar NIT «7770001».

**Observado:** Se observaron cuatro filas, incluido «Librería Prueba»
con NIT 7770001.

## **ARQ-04 \| Persistencia de Usuarios en SQL Server \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir DBeaver y conectarse a SQL Server «localhost:1434» con
credenciales locales.

**2.** Abrir «LibreriaUsuarios» → «dbo.Usuario».

**3.** Consultar el listado de usuarios.

**Observado:** Había cinco cuentas, incluidas «mperezdelacruzn»,
«ptemporal» y «ptemporala».

## **ARQ-05 \| Documentos de Productos en MongoDB \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir MongoDB Compass.

**2.** Conectar con «mongodb://localhost:27017».

**3.** Seleccionar base «libreria_productos» → colección «productos».

**4.** Consultar los documentos de producto.

**Observado:** Se visualizaron cinco documentos con campos de
identificador, costo, stock y auditoría.

## **ARQ-06 \| Separación de colecciones MongoDB \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Desde MongoDB Compass, expandir la base «libreria_productos».

**2.** Revisar los nombres de las colecciones.

**Observado:** Se observaron las colecciones «categorias», «contadores»,
«marcas» y «productos».

## **ARQ-07 \| Persistencia del historial de costos \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** En MongoDB Compass, abrir «libreria_productos» → «productos».

**2.** Buscar el documento «Calculadora Científica».

**3.** Expandir el arreglo «historicoCostos» y sus elementos.

**4.** Comparar el costo inicial y el actualizado.

**Observado:** Se encontraron entradas correspondientes a Bs 60 y Bs 61,
con motivos, fechas y actor.

## **ARQ-08 \| Health check del Gateway \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir Terminal.

**2.** Ejecutar «curl -i http://localhost:5080/salud».

**3.** Revisar el código de respuesta y el JSON.

**Observado:** Se obtuvo HTTP 200 y
«{"servicio":"Gateway","estado":"ok"}».

## **ARQ-09 \| Rutas configuradas en YARP \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Desde la raíz del repositorio, ejecutar «cat
src/Libreria.Gateway/yarp.g1.json src/Libreria.Gateway/yarp.g2.json».

**2.** Revisar las rutas /api de ambos grupos y sus destinos.

**Observado:** Se identificaron Clientes 5103, Proveedores 5101,
Productos 5104 y Usuarios 5102.

## **ARQ-10 \| Middleware YARP de balanceo y salud \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir «src/Libreria.Gateway/Program.cs».

**2.** Buscar «UseSessionAffinity», «UseLoadBalancing» y
«UsePassiveHealthChecks».

**3.** Confirmar la presencia de esos middleware.

**Observado:** Los middleware están configurados; esta comprobación no
equivale, por sí sola, a comprobar reparto real.

**Alcance de la evidencia:** Solo confirma presencia del middleware; el
reparto real se comprobó en ARQ-14.

## **ARQ-11 \| Arranque de una segunda instancia de Clientes \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Preparar las mismas variables de conexión MySQL que usa Clientes
localmente.

**2.** Iniciar una instancia adicional de Clientes con .NET 10 y
«ASPNETCORE_URLS=http://localhost:5203» sin perfil de lanzamiento.

**3.** Revisar el mensaje de escucha en la Terminal.

**Observado:** La segunda instancia escuchó en «localhost:5203» de
manera temporal.

## **ARQ-12 \| Gateway temporal para dos destinos \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Preparar un Gateway adicional en el puerto 5180.

**2.** Configurar por variables de entorno el destino «segunda» de
Clientes a «http://localhost:5203/» y política «RoundRobin».

**3.** Iniciar el Gateway temporal y consultar
«http://localhost:5180/salud».

**Observado:** El Gateway temporal respondió HTTP 200.

**Alcance de la evidencia:** La configuración con dos destinos fue
temporal y no representa los destinos predeterminados.

## **ARQ-13 \| Petición autenticada al Gateway temporal \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Habilitar temporalmente el cliente Keycloak «libreria-pruebas»
mediante la configuración local.

**2.** Obtener un JWT válido autorizado y guardarlo solo en una variable
local.

**3.** Enviar GET «http://localhost:5180/api/clientes» con header
«Authorization: Bearer \<TOKEN\>».

**4.** Revisar el código HTTP.

**Observado:** La solicitud con JWT válido respondió HTTP 200.

## **ARQ-14 \| Balanceo RoundRobin con doce solicitudes \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Mantener activas las instancias de Clientes en puertos 5103 y
5203 y el Gateway temporal 5180.

**2.** Enviar doce GET sucesivos a «http://localhost:5180/api/clientes»
con el JWT autorizado.

**3.** Registrar el HTTP de cada respuesta.

**4.** Comprobar en los logs del Gateway el destino seleccionado en cada
petición.

**Observado:** Las doce peticiones devolvieron HTTP 200 y YARP dirigió
seis a 5103 y seis a 5203.

**Alcance de la evidencia:** El 50/50 se observó únicamente con el
Gateway temporal y dos destinos activos.

## **ARQ-15 \| Restauración del entorno después de balanceo \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Detener los dos procesos temporales en puertos 5180 y 5203.

**2.** Ejecutar «KEYCLOAK_HABILITAR_CLIENTE_PRUEBAS=false docker compose
run --rm keycloak-configurar».

**3.** Consultar «/salud» de los servicios originales en 5080 y
5101–5104.

**4.** Comprobar que los puertos temporales ya no están escuchando.

**Observado:** Los cinco servicios originales respondieron HTTP 200;
5180/5203 no respondieron y el cliente de pruebas quedó deshabilitado.

## **ARQ-16 \| Registro de adaptadores en Razor Pages \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir
«src/Libreria.Client/Adapters/DependencyInjection/ApiGateway.cs».

**2.** Abrir «src/Libreria.Client/Program.cs».

**3.** Comprobar registros de «IClientesApi», «IProveedoresApi»,
«IProductosApi», «IUsuariosApi» y del cliente HTTP de Gateway.

**Observado:** Se comprobó «IHttpClientFactory»/HttpClient nombrado,
«BearerTokenDelegatingHandler» y los registros de los cuatro módulos.

## **ARQ-17 \| Inyección de interfaces en páginas Razor \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Buscar las interfaces «IClientesApi», «IProveedoresApi»,
«IProductosApi» e «IUsuariosApi» en las clases bajo
«src/Libreria.Client/Pages».

**2.** Abrir las páginas listadas en los resultados.

**3.** Comprobar la inyección y el uso de esas interfaces en acciones de
listados, alta, edición y detalle.

**Observado:** Las páginas utilizan las interfaces de los adaptadores;
también se había comprobado su funcionamiento en UI.

## **ARQ-18 \| Consulta de nombres para auditoría vía Gateway \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir «AuditoriaService.cs» y «UsuariosApiAdapter.cs» del
frontend.

**2.** Revisar la operación de obtención de nombres de usuario por
identificador.

**3.** Comprobar la petición «api/usuarios/nombres» usando el HttpClient
«ApiGateway».

**Observado:** La interfaz puede mostrar nombres de
creación/modificación; el flujo demostrado es Front → Gateway →
Usuarios.

**Alcance de la evidencia:** No demuestra comunicación HTTP directa
entre microservicios; confirma consulta desde el frontend mediante
Gateway.

# B. Proveedores

Casos funcionales del módulo Abastecimiento / Proveedores.

## **B1 \| Listado, búsqueda y filtros de proveedores \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Iniciar sesión como Administrador Sistema.

**2.** Abrir «Abastecimiento» → «Proveedores».

**3.** Verificar que se carga el listado de proveedores.

**4.** Identificar el buscador, el filtro de estado y los encabezados de
ordenación.

**5.** Revisar la información de los contactos visibles.

**Esperado:** Listado y herramientas disponibles, incluyendo contactos.

**Observado:** Listado funcional; observación adicional: la búsqueda de
contacto fue sensible a mayúsculas/acentos. No se certificó cada
combinación de búsqueda.

**Alcance de la evidencia:** No se probaron exhaustivamente todas las
combinaciones de búsqueda por mayúsculas o tildes.

## **B2 \| Validación de alta de proveedor vacío \[FAIL\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Iniciar sesión como administrador y abrir «Abastecimiento» →
«Proveedores».

**2.** Pulsar «Registrar proveedor».

**3.** Dejar vacíos NIT, razón social, nombre de contacto, teléfono y
correo.

**4.** Pulsar «Guardar» y examinar todos los mensajes de validación
visibles.

**Esperado:** Mensajes individuales para NIT, razón social, contacto y
un medio de contacto.

**Observado:** El navegador bloqueó con «Completa este campo» sin
desplegar conjuntamente los mensajes personalizados del plan.

## **B3 \| Registro de proveedor y normalización del contacto \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Con sesión de administrador, abrir «Proveedores» → «Registrar
proveedor».

**2.** Introducir NIT «7770001» y razón social «Librería Prueba».

**3.** Escribir el contacto con espacios repetidos a partir de «Ana
María Rojas»; completar un correo válido para el contacto.

**4.** Guardar y localizar «Librería Prueba» en el listado.

**5.** Verificar que el contacto se normaliza y el proveedor existe en
SQL Server.

**Esperado:** Alta exitosa, nombre de contacto normalizado.

**Observado:** Se registró y se confirmó su existencia en SQL Server.

## **B4 \| Rechazo de NIT de proveedor duplicado \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir «Proveedores» → «Registrar proveedor».

**2.** Introducir NIT «7770001», que ya corresponde a «Librería Prueba».

**3.** Completar los demás campos obligatorios con datos de prueba
distintos.

**4.** Pulsar «Guardar» y comprobar el rechazo por NIT duplicado.

**Esperado:** Error de NIT ya registrado.

**Observado:** El duplicado fue rechazado.

## **B5 \| Edición de contacto y trazabilidad \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Como administrador, abrir «Abastecimiento» → «Proveedores».

**2.** Localizar «Librería Prueba» y entrar a «Editar».

**3.** Cambiar el cargo del contacto principal y pulsar «Guardar».

**4.** Abrir nuevamente «Editar» y verificar que el nuevo cargo
permanece.

**5.** Revisar el nombre del usuario que modificó el registro en la
trazabilidad.

**Esperado:** Nuevo cargo persistido y Trazabilidad de modificación.

**Observado:** Cambio y autoría observados; hora local exacta no se
contrastó independientemente.

**Alcance de la evidencia:** Se verificaron el cargo y la autoría; la
exactitud del huso horario no quedó medida.

## **B6 \| Baja lógica y bloqueo de edición de proveedor \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Como administrador, abrir «Proveedores».

**2.** Localizar «Librería Prueba» y pulsar «Dar de baja».

**3.** Confirmar en el modal y seleccionar el filtro «Inactivos».

**4.** Localizar el proveedor y tratar de editarlo.

**5.** Comprobar el aviso que exige reactivarlo antes de modificarlo.

**Esperado:** Baja e impedimento de modificar mientras está inactivo.

**Observado:** Apareció el aviso de que debe reactivarse antes de
editar.

## **B7 \| Bloqueo de acceso de vendedor a Proveedores \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Iniciar sesión como Vendedor Mostrador.

**2.** Escribir directamente «http://localhost:5000/Proveedores» en la
barra de Chrome.

**3.** Presionar Enter y comprobar que se muestra «Acceso denegado».

**Esperado:** Acceso denegado al módulo.

**Observado:** Se observó redirección a Acceso denegado.

# C. Productos, Categorías y Marcas

Casos del módulo Inventario.

## **C1 \| Listado de productos y filtros \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Iniciar sesión como Administrador Sistema.

**2.** Entrar a «Inventario» → «Productos».

**3.** Comprobar el listado de cinco productos iniciales.

**4.** Identificar filtros de búsqueda, categoría, marca y estado,
además del botón «Actualizar».

**Esperado:** Listar catálogo y ofrecer filtros.

**Observado:** Se visualizaron 5 productos semilla y controles de
consulta.

## **C2 \| Ordenación de productos por precio de venta \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir «Inventario» → «Productos» como administrador.

**2.** Hacer clic en «Precio venta» y observar la secuencia de importes.

**3.** Volver a pulsar el mismo encabezado.

**4.** Verificar que la ordenación ascendente/descendente cambia.

**Esperado:** Ascendente y descendente, con orden por backend.

**Observado:** Se observó ordenación de precios.

## **C3 \| Validaciones de registro de producto \[FAIL\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir «Inventario» → «Productos» → «Registrar producto».

**2.** Dejar los campos obligatorios sin completar, incluidos nombre,
categoría y marca.

**3.** Pulsar «Guardar» y registrar los errores mostrados.

**4.** Repetir el intento utilizando números inválidos o negativos
cuando el formulario lo permita.

**5.** Comprobar idioma y nombres de campos en las advertencias.

**Esperado:** Mensajes claros en español por nombre, stock, precio,
costo, categoría y marca.

**Observado:** Surgieron «The value '' is invalid.» y nombres internos
DescripcionEspecifica, CategoriaPublicId, MarcaPublicId, PrecioVenta y
CostoAdquisicion.

## **C4 \| Rechazo de producto con nombre duplicado \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir «Inventario» → «Productos» → «Registrar producto».

**2.** Introducir «Cuaderno Espiral 100 hojas» como nombre, ya existente
en el catálogo.

**3.** Completar los demás campos requeridos con valores válidos.

**4.** Guardar y verificar que el sistema rechaza el nombre duplicado.

**Esperado:** Rechazar nombre repetido.

**Observado:** Se observó validación de duplicado.

## **C5 \| Confirmación de precio inferior al costo \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir «Inventario» → «Productos» → «Registrar producto».

**2.** Completar los campos necesarios con datos válidos de prueba.

**3.** Establecer un precio de venta inferior al costo de adquisición.

**4.** Intentar guardar y observar el modal «Confirmar precio de venta».

**Esperado:** Abrir modal de confirmación específico.

**Observado:** Se mostró «Confirmar precio de venta».

## **C6 \| Actualización e historial de costos de producto \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Iniciar sesión como administrador y abrir «Inventario» →
«Productos».

**2.** Localizar «Calculadora Científica» y seleccionar «Editar».

**3.** Cambiar su costo de adquisición de Bs 60 a Bs 61 y guardar.

**4.** Abrir el detalle/histórico del producto y revisar la
incorporación del cambio.

**5.** En MongoDB Compass, abrir «libreria_productos» → «productos» →
«Calculadora Científica» → «historicoCostos» y comprobar las dos
entradas.

**Esperado:** Nuevo costo e historial con dos entradas, sin borrar la
inicial.

**Observado:** MongoDB mostró costo actual Bs 61 y registros de Bs 60 y
Bs 61, con fecha/motivo de edición y usuario.

## **C7 \| Baja y reactivación de producto \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Iniciar sesión como administrador y abrir «Inventario» →
«Productos».

**2.** Seleccionar un producto activo y pulsar «Dar de baja».

**3.** Confirmar en el modal de la aplicación.

**4.** Aplicar filtro «Inactivos» y localizar el mismo producto.

**5.** Pulsar «Reactivar», confirmar el modal y comprobar el regreso a
«Activos».

**Esperado:** Los dos cambios de estado se confirman.

**Observado:** Desactivación y reactivación comprobadas.

## **C8 \| Consulta de Categorías y Marcas \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Iniciar sesión como administrador y abrir «Inventario» →
«Categorías».

**2.** Revisar el listado y los controles disponibles.

**3.** Abrir «Inventario» → «Marcas» y revisar el listado.

**4.** Comprobar que ambas vistas son de consulta y no ofrecen alta,
edición o baja.

**Esperado:** Pantallas de solo listado/búsqueda sin controles de
alta/edición/baja.

**Observado:** Se observaron ambos listados.

## **C9 \| Bloqueo de acceso de vendedor a Inventario \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Iniciar sesión como vendedor en Chrome.

**2.** Introducir «http://localhost:5000/Productos» en la barra de
dirección.

**3.** Confirmar que se muestra «Acceso denegado».

**Esperado:** Impedir acceso a Inventario.

**Observado:** Acceso denegado observado.

# D. Login, roles, sesiones y JWT

Accesos comprobados con Chrome, Keycloak y peticiones HTTP.

## **D1 \| Inicio de sesión en Keycloak en español \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Cerrar las sesiones de la aplicación.

**2.** Abrir «http://localhost:5000» en Chrome.

**3.** Esperar la redirección a Keycloak.

**4.** Leer los textos de inicio de sesión y verificar «Iniciar sesión
en Librería».

**Esperado:** Textos en español y tratamiento formal.

**Observado:** «Iniciar sesión en Librería» y textos en español
confirmados.

## **D2 \| Rechazo de credenciales incorrectas \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir el login de Keycloak desde «http://localhost:5000».

**2.** Escribir un usuario existente y una contraseña intencionalmente
incorrecta.

**3.** Pulsar «Iniciar sesión».

**4.** Confirmar el mensaje «Usuario o contraseña incorrectos».

**Esperado:** «Usuario o contraseña incorrectos».

**Observado:** Mensaje de rechazo en español observado.

## **D3 \| Acceso y menú del administrador \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir Chrome y acceder a «http://localhost:5000».

**2.** Autenticarse con la cuenta Administrador Sistema.

**3.** Esperar el panel principal.

**4.** Comprobar los grupos de menú «General», «Clientes»,
«Abastecimiento», «Inventario» y «Administración».

**Esperado:** General, Clientes, Abastecimiento, Inventario y
Administración.

**Observado:** Módulos de Administrador visibles.

## **D4 \| Acceso y menú restringido del vendedor \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir una ventana de incógnito de Chrome independiente.

**2.** Acceder a «http://localhost:5000» e iniciar sesión con Vendedor
Mostrador.

**3.** Esperar el panel.

**4.** Verificar que únicamente aparecen «General» y «Clientes» en el
menú.

**Esperado:** Menú restringido a General y Clientes.

**Observado:** Rol Vendedor reconocido y menú restringido.

## **D5 \| Privacidad después de cerrar sesión y volver Atrás \[FAIL\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir Chrome e iniciar sesión como administrador.

**2.** Entrar al Panel o a «Clientes» para cargar una página protegida.

**3.** Pulsar «Cerrar sesión» y confirmar que se muestra la pantalla de
login.

**4.** Pulsar el botón «Atrás» del navegador sin recargar.

**5.** Si aparece el contenido anterior, pulsar Cmd+R para comprobar el
comportamiento del servidor.

**Esperado:** No volver a visualizar contenido protegido después de
salir.

**Observado:** Atrás mostró una copia previa del panel en caché;
recargar exigió login. No se probó bypass del servidor, sí exposición
visual en caché.

**Alcance de la evidencia:** Al refrescar la página, el servidor volvió
a exigir autenticación. No se demostró un bypass de seguridad del
backend.

## **D6 \| Acceso a API con y sin JWT \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Comprobar primero que los servicios de Keycloak y Clientes están
disponibles.

**2.** Habilitar temporalmente el cliente de pruebas con
«KEYCLOAK_HABILITAR_CLIENTE_PRUEBAS=true docker compose run --rm
keycloak-configurar».

**3.** Obtener localmente un JWT válido para una cuenta autorizada, sin
imprimirlo ni compartirlo.

**4.** Realizar GET a «http://localhost:5103/api/clientes» o al Gateway
con «Authorization: Bearer \<TOKEN\>» y registrar HTTP 200.

**5.** Repetir sin el encabezado Authorization y comprobar HTTP 401.

**6.** Deshabilitar nuevamente el cliente de pruebas con
«KEYCLOAK_HABILITAR_CLIENTE_PRUEBAS=false docker compose run --rm
keycloak-configurar».

**Esperado:** Con token válido HTTP 200; sin token HTTP 401.

**Observado:** Ambas condiciones se comprobaron en API directa/Gateway;
no se repitió exactamente cada paso desde Swagger de la versión formal.

**Alcance de la evidencia:** La prueba se hizo mediante peticiones HTTP
reales; no se reprodujeron todas las acciones previstas en Swagger.

# E. Usuarios, contraseñas y trazabilidad

Casos del área administrativa de Usuarios y Cuenta.

## **E1 \| Listado administrativo de usuarios \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Iniciar sesión como Administrador Sistema.

**2.** Abrir «Administración» → «Usuarios».

**3.** Revisar nombre completo, nombre de usuario, correo, rol y estado.

**4.** Comprobar que no se muestra una columna con contraseñas y
observar los filtros disponibles.

**Esperado:** Nombre, usuario, correo, rol y estado; no contraseña
visible.

**Observado:** Listado y estados confirmados. No se certificó
exhaustivamente cada encabezado de orden.

**Alcance de la evidencia:** No se verificó cada encabezado de
ordenación de la lista.

## **E2 \| Ausencia de contraseñas y hashes en HTML \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Desde la sesión del administrador, entrar a «Administración» →
«Usuarios».

**2.** Abrir el código fuente HTML de la página (en Mac: menú «Ver
código fuente» o atajo del navegador).

**3.** Buscar las cadenas «PasswordHash», «KeycloakId» y posibles
contraseñas temporales.

**4.** Comprobar que no aparecen credenciales ni hashes visibles en el
HTML.

**Esperado:** No exponer credenciales ni hashes en HTML.

**Observado:** No aparecieron PasswordHash, KeycloakId o contraseña
temporal; antiforgery token sí, lo que es normal.

## **E3 \| Reglas dinámicas de contraseña segura \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Iniciar sesión como Vendedor Mostrador.

**2.** Abrir la opción «Cuenta» → «Cambiar contraseña».

**3.** Escribir «abc» en el campo de nueva contraseña.

**4.** Revisar las cinco reglas visibles y confirmar que el botón de
envío permanece deshabilitado.

**Esperado:** Cinco reglas y botón deshabilitado mientras no cumplen.

**Observado:** Reglas mostradas y botón disabled verificado en DevTools.

## **E4 \| Confirmación de contraseña distinta \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Con el formulario «Cambiar contraseña» abierto, escribir una
nueva contraseña que cumpla todas las reglas.

**2.** Escribir una confirmación diferente de la nueva contraseña.

**3.** Comprobar el mensaje «Las contraseñas no coinciden».

**4.** Verificar que el botón de cambio permanece deshabilitado.

**Esperado:** Aviso «Las contraseñas no coinciden» y botón disabled.

**Observado:** Comportamiento comprobado.

## **E5 \| Rechazo de contraseña actual incorrecta \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Con sesión de vendedor, abrir «Cambiar contraseña».

**2.** Introducir una contraseña actual errónea.

**3.** Introducir una nueva contraseña válida y repetirla exactamente en
la confirmación.

**4.** Pulsar el botón de cambio.

**5.** Comprobar «La contraseña actual es incorrecta» y que la sesión
permanece abierta.

**Esperado:** Error de contraseña actual y sesión sin cerrar.

**Observado:** Mensaje «La contraseña actual es incorrecta» y sesión
activa. Error antiguo persistió visualmente después de editar los campos
(observación UX).

**Alcance de la evidencia:** El mensaje anterior permaneció visible al
editar otra vez los campos; observación de experiencia de usuario.

## **E6 \| Cambio correcto y cierre automático de sesión \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Con sesión de vendedor, abrir «Cambiar contraseña».

**2.** Introducir la contraseña actual correcta.

**3.** Introducir una nueva contraseña fuerte y repetirla exactamente.

**4.** Pulsar el botón de cambio y esperar la redirección.

**5.** Comprobar que vuelve a «/Cuenta/Login?motivo=contrasena-cambiada»
y no conserva la sesión anterior.

**Esperado:** Cambio y cierre automático, con aviso de volver a
ingresar.

**Observado:** Redirección a /Cuenta/Login?motivo=contrasena-cambiada.
El aviso de éxito apareció en estilo rojo (observación UX).

**Alcance de la evidencia:** El mensaje de éxito se representó
visualmente en rojo; observación de experiencia de usuario.

## **E7 \| Rechazo de contraseña anterior y aceptación de nueva \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Después de E6, abrir el formulario de login.

**2.** Intentar entrar con la contraseña antigua y confirmar que se
rechaza.

**3.** Volver al login, introducir la nueva contraseña y comprobar que
permite entrar.

**Esperado:** Anterior rechazada y nueva aceptada.

**Observado:** Ambas verificaciones realizadas.

## **E8 \| Restitución de contraseña del usuario de prueba \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Iniciar sesión con la contraseña nueva vigente después de E6/E7.

**2.** Entrar a «Cambiar contraseña».

**3.** Ingresar la contraseña actual correcta y configurar como nueva la
contraseña original del usuario de prueba.

**4.** Guardar, volver a iniciar sesión y confirmar que la contraseña
original funciona.

**Esperado:** Usuario vuelve a autenticarse con clave original.

**Observado:** Se comprobó la restitución con cambio desde UI; no se
ejecutó exactamente el método docker-compose del plan.

**Alcance de la evidencia:** Restitución desde la interfaz; no se
ejecutó el procedimiento exacto de restauración por Docker del plan
original.

## **E9 \| Trazabilidad legible entre módulos \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Iniciar sesión como Administrador Sistema.

**2.** Abrir «Clientes» → «Editar» sobre el registro modificado y
revisar «Trazabilidad».

**3.** Abrir «Proveedores» → «Editar» sobre el proveedor modificado y
revisar «Trazabilidad».

**4.** Abrir el detalle/histórico de «Calculadora Científica» en
Productos.

**5.** Confirmar que los nombres de usuarios son legibles en lugar de
GUID.

**Esperado:** Nombres en trazabilidad e historial, con hora de Bolivia.

**Observado:** Autores legibles observados; el histórico también
persistió en MongoDB. Validación exacta de zona horaria pendiente.

**Alcance de la evidencia:** Se comprobaron autores legibles, no se
efectuó una medición independiente de zona horaria.

# F. Alta de usuario y primer acceso

Pruebas de registro, correo local, activación y baja.

## **F1 \| Validación de formulario de nuevo usuario \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Iniciar sesión como administrador y abrir «Administración» →
«Usuarios».

**2.** Pulsar «+ Registrar usuario» y dejar el formulario vacío.

**3.** Pulsar «Guardar» y observar las validaciones.

**4.** Completar únicamente el campo «Rol», intentar guardar de nuevo y
examinar los avisos restantes.

**Esperado:** Mensajes de obligatorios, incluido apellido paterno.

**Observado:** Los mensajes se observaron en pasos sucesivos;
inicialmente solo se mostró el error del rol. Problema UX: no aparecen
todos juntos.

**Alcance de la evidencia:** Los mensajes aparecieron en intentos
sucesivos; no todos se mostraron juntos desde el primer intento.

## **F2 \| Alta de usuario con nombres compuestos \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Iniciar sesión como administrador y pulsar «Usuarios» → «+
Registrar usuario».

**2.** Ingresar un nombre y apellidos compuestos, correspondientes a
«María José Pérez de la Cruz Núñez», junto a un correo exclusivo.

**3.** Asignar rol «Vendedor» y guardar.

**4.** Buscar el nuevo registro en «Usuarios» y verificar nombre de
usuario «mperezdelacruzn», estado «Pendiente» y ausencia de contraseña
visible.

**Esperado:** Generar username y dejar Pendiente, sin contraseña
visible.

**Observado:** Se creó mperezdelacruzn en Pendiente. Datos/username
distintos de jperezr previstos en el caso original.

**Alcance de la evidencia:** Se usaron nombres reales distintos de los
datos de ejemplo de la matriz formal; usuario generado: mperezdelacruzn.

## **F4 \| Rechazo de correo electrónico repetido \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Iniciar sesión como administrador y abrir «Usuarios» → «+
Registrar usuario».

**2.** Introducir un correo que ya pertenece a «ptemporala» y completar
los demás datos con valores de prueba.

**3.** Guardar y registrar el rechazo por correo duplicado.

**4.** Repetir el intento con ese mismo correo convertido a MAYÚSCULAS.

**5.** Comprobar el mismo rechazo.

**Esperado:** Rechazar por correo duplicado sin distinguir mayúsculas o
estado.

**Observado:** Ambas variantes rechazadas con «Ya existe un usuario
registrado con ese correo».

## **F5 \| Generación de correo de credenciales temporales \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Registrar o localizar la cuenta de prueba «ptemporala» desde el
módulo Usuarios.

**2.** En el equipo donde corre el microservicio Usuarios, abrir
«src/Libreria.Servicio.Usuarios/bin/Development/mail».

**3.** Abrir el archivo «.eml» que se generó para ese registro.

**4.** Verificar asunto, nombre de usuario, contraseña temporal y enlace
de primer ingreso, sin copiar ni compartir la contraseña.

**Esperado:** Credenciales temporales y enlace en correo, sin mostrar
contraseña en UI.

**Observado:** Correo .eml generado en modo PickupDirectory; incluye
username y enlace de primer ingreso. SMTP externo no verificado.

**Alcance de la evidencia:** La entrega observada fue un archivo .eml de
PickupDirectory, no recepción en un correo externo por SMTP.

## **F6 \| Cambio obligatorio de contraseña en primer acceso \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir una nueva ventana de incógnito en Chrome.

**2.** Acceder a «http://localhost:5000».

**3.** Iniciar sesión con el usuario «ptemporala» y la contraseña
temporal del correo «.eml».

**4.** Comprobar que Keycloak exige cambiar la contraseña antes de
mostrar el panel.

**Esperado:** Keycloak exige UPDATE_PASSWORD antes de entrar al panel.

**Observado:** Se mostró «Tiene que cambiar su contraseña para activar
su cuenta».

## **F9 \| Activación después de establecer contraseña definitiva \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Permanecer en la pantalla obligatoria de cambio de contraseña de
Keycloak para «ptemporala».

**2.** Ingresar una nueva contraseña que cumpla la política y repetirla
en la confirmación.

**3.** Guardar el cambio.

**4.** Comprobar el ingreso al panel del sistema con el rol Vendedor.

**Esperado:** Entrar al panel con rol Vendedor.

**Observado:** Se accedió como Prueba Temporal Auditoría, rol VENDEDOR.

## **F10 \| Actualización de estado Pendiente a Activo \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Mantener una ventana Chrome con sesión de Administrador Sistema.

**2.** Después de completar F9, abrir «Administración» → «Usuarios».

**3.** Pulsar «Actualizar».

**4.** Comprobar que «ptemporala» figura «Activo», mientras
«mperezdelacruzn» y «ptemporal» siguen «Pendiente».

**Esperado:** El nuevo usuario aparece Activo y otros no activados
siguen Pendiente.

**Observado:** ptemporala cambió a Activo; mperezdelacruzn y ptemporal
siguieron Pendiente.

## **F12 \| Desactivación de usuario en Front y Keycloak \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Mantener una ventana de administrador y una sesión independiente
de «ptemporala».

**2.** Desde el administrador, abrir «Usuarios», localizar «ptemporala»
y pulsar «Dar de baja».

**3.** Confirmar la operación y verificar el estado «Inactivo».

**4.** En la sesión existente de «ptemporala», intentar entrar a
«/Clientes».

**5.** Intentar un inicio de sesión nuevo como «ptemporala» y revisar el
estado «Disabled» en Keycloak.

**Esperado:** Cuenta Inactiva; Keycloak impide nuevo ingreso; sesión
existente bloqueada.

**Observado:** Ambas protecciones verificadas y cuenta Disabled en
consola Keycloak. Usuario de prueba diferente del jperez2 oficial.

**Alcance de la evidencia:** Se utilizó la cuenta ptemporala, no la
cuenta de ejemplo indicada en la matriz original.

# X. Comprobaciones funcionales adicionales

Verificaciones realizadas fuera de la matriz A–F.

## **X01 \| Contraseña temporal anterior rechazada \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Cerrar la sesión de «ptemporala» después de completar el primer
ingreso.

**2.** Desde el login, intentar autenticarse con la contraseña temporal
del archivo .eml.

**3.** Comprobar que Keycloak la rechaza.

**Observado:** Keycloak rechazó la temporal con «Usuario o contraseña
incorrectos» y la contraseña definitiva continuó funcionando.

## **X02 \| Acceso con contraseña definitiva \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir el login después de haber cambiado la contraseña temporal.

**2.** Ingresar «ptemporala» y su contraseña definitiva.

**3.** Comprobar que se abre directamente el panel del vendedor.

**Observado:** La cuenta entró directamente al panel sin volver a
solicitar el cambio temporal.

## **X03 \| Bloqueo de rutas de Usuarios para vendedor \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Iniciar sesión con la cuenta Vendedor Mostrador.

**2.** Navegar manualmente a «http://localhost:5000/Usuarios».

**3.** Navegar manualmente a «http://localhost:5000/Usuarios/Create».

**Observado:** Ambas direcciones mostraron «Acceso denegado».

## **X04 \| Bloqueo de sesión activa después de baja \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Mantener una ventana incógnita con sesión de «ptemporala».

**2.** Desde otra sesión administrativa, dar de baja «ptemporala».

**3.** Regresar a la sesión incógnita e intentar abrir
«http://localhost:5000/Clientes».

**Observado:** Se redirigió a «/Cuenta/Login?motivo=inactiva».

## **X05 \| Rechazo de nuevo inicio de sesión de usuario inactivo \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Después de la baja de «ptemporala», cerrar su sesión.

**2.** Intentar autenticarse otra vez con la contraseña definitiva.

**Observado:** Keycloak rechazó el nuevo inicio de sesión porque la
cuenta estaba deshabilitada.

## **X06 \| Sincronización de baja con Keycloak \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir la consola «http://localhost:8080» con permisos
administrativos.

**2.** Seleccionar realm «libreria» → «Users».

**3.** Buscar «ptemporala» y abrir «Details».

**4.** Comprobar el estado de habilitación del usuario.

**Observado:** La cuenta se conservó, pero apareció «Disabled» en
Keycloak.

## **X07 \| Correo duplicado contra usuario inactivo \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Con sesión de administrador, abrir «Usuarios» → «+ Registrar
usuario».

**2.** Introducir «Carlos Duplicado Prueba» y reutilizar el correo de la
cuenta inactiva «ptemporala».

**3.** Guardar.

**Observado:** El alta fue rechazada: el correo ya estaba registrado,
aunque la cuenta original estaba inactiva.

## **X08 \| Correo duplicado sin distinguir mayúsculas \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Repetir el alta del caso X07.

**2.** Escribir el mismo correo completamente en MAYÚSCULAS.

**3.** Pulsar Guardar.

**Observado:** La validación volvió a rechazar el correo duplicado.

## **X09 \| Correo mal formado bloqueado por Chrome \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir «Usuarios» → «+ Registrar usuario».

**2.** Completar los demás campos necesarios.

**3.** Escribir «correo-sin-arroba» como dirección de correo.

**4.** Intentar guardar.

**Observado:** Chrome bloqueó el envío porque faltaba «@»; esto
demuestra la validación HTML5 del navegador.

**Alcance de la evidencia:** El bloqueo procede de HTML5 del navegador;
no demuestra por sí mismo rechazo del backend.

## **X10 \| Filtro de Usuarios por estado Activo \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Iniciar sesión como administrador y abrir «Administración» →
«Usuarios».

**2.** Elegir el estado «Activo».

**3.** Pulsar «Filtrar» y revisar el listado.

**Observado:** Aparecieron solo las cuentas «admin» y «vendedor» en el
estado observado.

## **X11 \| Filtro de Usuarios por estado Pendiente \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir «Usuarios» como administrador.

**2.** Seleccionar el estado «Pendiente» y pulsar «Filtrar».

**3.** Revisar los nombres mostrados.

**Observado:** Se mostraron «mperezdelacruzn» y «ptemporal»; no se
incluyeron cuentas activas o inactivas.

## **X12 \| Filtro de Usuarios por estado Inactivo \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir «Usuarios» como administrador.

**2.** Elegir estado «Inactivo» y pulsar «Filtrar».

**3.** Revisar la lista resultante.

**Observado:** Apareció la cuenta «ptemporala» con estado Inactivo.

## **X13 \| Limpiar filtros de Usuarios \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Desde el listado filtrado por «Inactivo», pulsar «Limpiar».

**2.** Verificar el campo Estado y el número de filas.

**Observado:** El filtro regresó a «Todos» y se mostraron cinco
usuarios.

# ARQ. Verificaciones de arquitectura y persistencia

Contenedores, bases de datos, Gateway, balanceo y adaptadores.

## **ARQ-01 \| Estado de contenedores Docker \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir Terminal en la raíz del repositorio.

**2.** Ejecutar «docker compose ps».

**3.** Comprobar contenedores y puertos publicados.

**Observado:** Se observaron cinco contenedores saludables: MySQL
clientes 3308, SQL Server proveedores 1433, SQL Server usuarios 1434,
MongoDB 27017 y Keycloak 8080.

## **ARQ-02 \| Persistencia de Clientes en MySQL \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir DBeaver y conectarse a MySQL en «localhost:3308» con
credenciales locales del .env.

**2.** Abrir «libreria_clientes» → tabla «Cliente» → Datos.

**3.** Buscar el CI/NIT «13622018» y revisar la razón social.

**Observado:** Se encontraron cuatro filas, incluido el registro
«13622018» y «juan josé pérez actualizado».

## **ARQ-03 \| Persistencia de Proveedores en SQL Server \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir DBeaver y conectarse a SQL Server «localhost:1433» con
credenciales del entorno.

**2.** Seleccionar base «LibreriaProveedores» → «dbo.Proveedor».

**3.** Consultar los registros y localizar NIT «7770001».

**Observado:** Se observaron cuatro filas, incluido «Librería Prueba»
con NIT 7770001.

## **ARQ-04 \| Persistencia de Usuarios en SQL Server \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir DBeaver y conectarse a SQL Server «localhost:1434» con
credenciales locales.

**2.** Abrir «LibreriaUsuarios» → «dbo.Usuario».

**3.** Consultar el listado de usuarios.

**Observado:** Había cinco cuentas, incluidas «mperezdelacruzn»,
«ptemporal» y «ptemporala».

## **ARQ-05 \| Documentos de Productos en MongoDB \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir MongoDB Compass.

**2.** Conectar con «mongodb://localhost:27017».

**3.** Seleccionar base «libreria_productos» → colección «productos».

**4.** Consultar los documentos de producto.

**Observado:** Se visualizaron cinco documentos con campos de
identificador, costo, stock y auditoría.

## **ARQ-06 \| Separación de colecciones MongoDB \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Desde MongoDB Compass, expandir la base «libreria_productos».

**2.** Revisar los nombres de las colecciones.

**Observado:** Se observaron las colecciones «categorias», «contadores»,
«marcas» y «productos».

## **ARQ-07 \| Persistencia del historial de costos \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** En MongoDB Compass, abrir «libreria_productos» → «productos».

**2.** Buscar el documento «Calculadora Científica».

**3.** Expandir el arreglo «historicoCostos» y sus elementos.

**4.** Comparar el costo inicial y el actualizado.

**Observado:** Se encontraron entradas correspondientes a Bs 60 y Bs 61,
con motivos, fechas y actor.

## **ARQ-08 \| Health check del Gateway \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir Terminal.

**2.** Ejecutar «curl -i http://localhost:5080/salud».

**3.** Revisar el código de respuesta y el JSON.

**Observado:** Se obtuvo HTTP 200 y
«{"servicio":"Gateway","estado":"ok"}».

## **ARQ-09 \| Rutas configuradas en YARP \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Desde la raíz del repositorio, ejecutar «cat
src/Libreria.Gateway/yarp.g1.json src/Libreria.Gateway/yarp.g2.json».

**2.** Revisar las rutas /api de ambos grupos y sus destinos.

**Observado:** Se identificaron Clientes 5103, Proveedores 5101,
Productos 5104 y Usuarios 5102.

## **ARQ-10 \| Middleware YARP de balanceo y salud \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir «src/Libreria.Gateway/Program.cs».

**2.** Buscar «UseSessionAffinity», «UseLoadBalancing» y
«UsePassiveHealthChecks».

**3.** Confirmar la presencia de esos middleware.

**Observado:** Los middleware están configurados; esta comprobación no
equivale, por sí sola, a comprobar reparto real.

**Alcance de la evidencia:** Solo confirma presencia del middleware; el
reparto real se comprobó en ARQ-14.

## **ARQ-11 \| Arranque de una segunda instancia de Clientes \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Preparar las mismas variables de conexión MySQL que usa Clientes
localmente.

**2.** Iniciar una instancia adicional de Clientes con .NET 10 y
«ASPNETCORE_URLS=http://localhost:5203» sin perfil de lanzamiento.

**3.** Revisar el mensaje de escucha en la Terminal.

**Observado:** La segunda instancia escuchó en «localhost:5203» de
manera temporal.

## **ARQ-12 \| Gateway temporal para dos destinos \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Preparar un Gateway adicional en el puerto 5180.

**2.** Configurar por variables de entorno el destino «segunda» de
Clientes a «http://localhost:5203/» y política «RoundRobin».

**3.** Iniciar el Gateway temporal y consultar
«http://localhost:5180/salud».

**Observado:** El Gateway temporal respondió HTTP 200.

**Alcance de la evidencia:** La configuración con dos destinos fue
temporal y no representa los destinos predeterminados.

## **ARQ-13 \| Petición autenticada al Gateway temporal \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Habilitar temporalmente el cliente Keycloak «libreria-pruebas»
mediante la configuración local.

**2.** Obtener un JWT válido autorizado y guardarlo solo en una variable
local.

**3.** Enviar GET «http://localhost:5180/api/clientes» con header
«Authorization: Bearer \<TOKEN\>».

**4.** Revisar el código HTTP.

**Observado:** La solicitud con JWT válido respondió HTTP 200.

## **ARQ-14 \| Balanceo RoundRobin con doce solicitudes \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Mantener activas las instancias de Clientes en puertos 5103 y
5203 y el Gateway temporal 5180.

**2.** Enviar doce GET sucesivos a «http://localhost:5180/api/clientes»
con el JWT autorizado.

**3.** Registrar el HTTP de cada respuesta.

**4.** Comprobar en los logs del Gateway el destino seleccionado en cada
petición.

**Observado:** Las doce peticiones devolvieron HTTP 200 y YARP dirigió
seis a 5103 y seis a 5203.

**Alcance de la evidencia:** El 50/50 se observó únicamente con el
Gateway temporal y dos destinos activos.

## **ARQ-15 \| Restauración del entorno después de balanceo \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Detener los dos procesos temporales en puertos 5180 y 5203.

**2.** Ejecutar «KEYCLOAK_HABILITAR_CLIENTE_PRUEBAS=false docker compose
run --rm keycloak-configurar».

**3.** Consultar «/salud» de los servicios originales en 5080 y
5101–5104.

**4.** Comprobar que los puertos temporales ya no están escuchando.

**Observado:** Los cinco servicios originales respondieron HTTP 200;
5180/5203 no respondieron y el cliente de pruebas quedó deshabilitado.

## **ARQ-16 \| Registro de adaptadores en Razor Pages \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir
«src/Libreria.Client/Adapters/DependencyInjection/ApiGateway.cs».

**2.** Abrir «src/Libreria.Client/Program.cs».

**3.** Comprobar registros de «IClientesApi», «IProveedoresApi»,
«IProductosApi», «IUsuariosApi» y del cliente HTTP de Gateway.

**Observado:** Se comprobó «IHttpClientFactory»/HttpClient nombrado,
«BearerTokenDelegatingHandler» y los registros de los cuatro módulos.

## **ARQ-17 \| Inyección de interfaces en páginas Razor \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Buscar las interfaces «IClientesApi», «IProveedoresApi»,
«IProductosApi» e «IUsuariosApi» en las clases bajo
«src/Libreria.Client/Pages».

**2.** Abrir las páginas listadas en los resultados.

**3.** Comprobar la inyección y el uso de esas interfaces en acciones de
listados, alta, edición y detalle.

**Observado:** Las páginas utilizan las interfaces de los adaptadores;
también se había comprobado su funcionamiento en UI.

## **ARQ-18 \| Consulta de nombres para auditoría vía Gateway \[PASS\]**

**Pasos ejecutados o secuencia de reproducción:**

**1.** Abrir «AuditoriaService.cs» y «UsuariosApiAdapter.cs» del
frontend.

**2.** Revisar la operación de obtención de nombres de usuario por
identificador.

**3.** Comprobar la petición «api/usuarios/nombres» usando el HttpClient
«ApiGateway».

**Observado:** La interfaz puede mostrar nombres de
creación/modificación; el flujo demostrado es Front → Gateway →
Usuarios.

**Alcance de la evidencia:** No demuestra comunicación HTTP directa
entre microservicios; confirma consulta desde el frontend mediante
Gateway.

Casos FAIL documentados: A1, A4, B2, C3 y D5. Los resultados de estas
fichas no se consideran corregidos hasta repetir la prueba después de
aplicar los cambios.

Las instancias temporales de YARP y Clientes y el cliente de pruebas de
Keycloak quedaron deshabilitados al terminar el ensayo de balanceo. No
se realizó ninguna modificación de código para redactar el documento.
