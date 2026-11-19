# REST por microservicio

Todas las rutas del Front pasan por `http://localhost:5080`.

## Clientes

- `GET api/clientes`
- `GET api/clientes/{publicId}`
- `POST api/clientes`
- `PUT api/clientes/{publicId}`
- `PATCH api/clientes/{publicId}/estado`

## Proveedores

- `GET api/proveedores?busqueda=&estado=&orden=&descendente=`
- `GET api/proveedores/{publicId}`
- `POST api/proveedores`
- `PUT api/proveedores/{publicId}`
- `PATCH api/proveedores/{publicId}/estado`

El cuerpo de alta/edición contiene `nit`, `razonSocial`, datos comerciales y el contacto principal. NIT duplicado devuelve 409; errores de campo devuelven 400; un identificador inexistente devuelve 404. La baja es lógica.

## Productos

- CRUD de `api/productos`
- Catálogos de `api/categorias` y `api/marcas`
- Consulta de `api/historico/{publicId}`

Productos es el microservicio no relacional; categorías y marcas son dependencias de sus documentos.

## Usuarios

- CRUD y operaciones de `api/usuarios` (el login lo hace Keycloak; no hay `api/auth`)
- `GET api/usuarios/{keycloakId}/sesion-activa`: el Front la consulta con el `sub` del token
- `PUT api/usuarios/contrasena`: verifica la actual y fija la nueva en Keycloak (204 · 400 con el mensaje · 403)

Todos los microservicios exponen `GET /salud` directamente.
