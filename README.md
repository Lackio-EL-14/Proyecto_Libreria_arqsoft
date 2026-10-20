```markdown
# Sistema de inventario y ventas — Librería (Escalado Horizontalmente)

Proyecto académico: Razor Pages + C# + ADO.NET sobre .NET 10, escalado horizontalmente con Nginx, Redis y SQL Server usando Docker Compose. Aplica Arquitectura Hexagonal (Puertos y Adaptadores), Clean Code y principios SOLID.

## Estructura del repo

```text
Proyecto_Libreria_arqsoft/
├── docker-compose.yml       # Orquestador del clúster (Nginx, 4x Apps, Redis, BD)
├── nginx.conf               # Configuración del proxy inverso y balanceador
├── .env.example             # Plantilla de variables de entorno seguras
├── db/
│   └── init/                # Scripts SQL de inicialización numerados (ej. 00_schema.sql)
└── src/
    ├── Libreria.Client/         # Adaptador Primario (UI - Razor Pages)
    ├── Libreria.Application/    # Núcleo: Casos de uso, Puertos, Fachadas, Validadores
    ├── Libreria.Infrastructure/ # Adaptador Secundario: ADO.NET, Redis, DataProtection
    └── Libreria.Domain/         # Núcleo Puro: Entidades y reglas de dominio

```

## 1. Requisitos previos (instalar una sola vez)

* [Docker Desktop](https://www.docker.com/products/docker-desktop/) — verificar con `docker --version`
* [Docker Compose](https://docs.docker.com/compose/) — verificar con `docker compose version`
* Git
* Editor: Visual Studio, VS Code (con la extensión C# Dev Kit) o Rider.

## 2. Clonar el repositorio y configuración inicial

```bash
git clone [https://github.com/Lackio-EL-14/Proyecto_Libreria_arqsoft.git](https://github.com/Lackio-EL-14/Proyecto_Libreria_arqsoft.git)
cd Proyecto_Libreria_arqsoft
git switch develop
git pull origin develop

```

Antes de compilar, debes configurar las variables de entorno para que los contenedores puedan comunicarse:

```bash
cp .env.example .env

```

*(Opcional: Edita el archivo `.env` si necesitas cambiar la contraseña de SQL Server o configurar las credenciales SMTP para la activación de usuarios).*

## 3. Levantar la Aplicación (El Clúster)

Abre **Docker Desktop manualmente** y espera hasta que indique que el motor está en ejecución. Construye la imagen optimizada y levanta los 6 contenedores en segundo plano:

```bash
docker compose up -d --build

```

El clúster incluye:

* 1 Proxy Inverso (Nginx) balanceando en Round-Robin.
* 3 Réplicas de la aplicación web (`app1`, `app2`, `app3`).
* 1 Base de datos SQL Server.
* 1 Servidor Redis para caché distribuida.

Verifica que todos estén saludables (`Up` o `Healthy`):

```bash
docker compose ps

```

## 4. Inicializar la Base de Datos

En la primera ejecución (o si destruyes los volúmenes), la base de datos nace vacía. Inyecta el esquema y los datos semilla ejecutando este comando, el cual leerá todos los scripts de la carpeta `db/init/` en orden:

```bash
docker exec -it libreria-db bash -c 'for f in /scripts/*.sql; do echo "Ejecutando $f..."; /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Libreria2026!" -C -i "$f"; done'

```

*(Asegúrate de que la contraseña coincida con la que pusiste en tu archivo `.env`).*

## 5. Acceso y Uso

* **Aplicación Web:** Navega a [http://localhost:3000](http://localhost:3000). Nginx recibirá tu petición y la enviará a una de las réplicas disponibles.
* **Sesiones Compartidas:** Puedes recargar la página y verás que el ID del servidor cambia (en el banner superior), pero tu sesión y el cifrado de URLs se mantienen gracias al volumen compartido de Data Protection.

### Usuarios de prueba

| Usuario | Contraseña | Rol |
| --- | --- | --- |
| `admin` | `Admin2026!` | Administrador |
| `vendedor` | `Vendedor2026!` | Vendedor |

## 6. Comandos Útiles de Mantenimiento

**Ver logs en tiempo real (de todo el clúster):**

```bash
docker compose logs -f

```

**Comprobar la caché distribuida de productos activos:**

La clave lógica `productos:activos` usa el prefijo configurado `libreria:`, por lo que Redis la almacena como `libreria:productos:activos` durante cinco minutos.

```bash
docker exec libreria-redis redis-cli TYPE libreria:productos:activos
docker exec libreria-redis redis-cli TTL libreria:productos:activos
docker exec libreria-redis redis-cli HGET libreria:productos:activos data
docker compose logs app1 app2 app3 app4 | grep "Cache HIT\|Cache MISS"
```

### Visor de diagnóstico Redis (US-64)

1. Inicia sesión como `admin`.
2. Abre [http://localhost:3000/Diagnostico/Cache](http://localhost:3000/Diagnostico/Cache). La vista se actualiza cada dos segundos.
3. En otra pestaña abre **Productos**. La primera consulta después de eliminar o expirar la clave muestra `MISS` con origen `SQL`; la siguiente muestra `HIT` con origen `Redis`.
4. Compara la réplica de cada resultado para demostrar que la caché es compartida entre los contenedores.

La entrada de `IDistributedCache` es un `hash` de Redis. Por eso `GET` informa `WRONGTYPE` y el JSON se inspecciona con `HGET ... data`:

```bash
docker exec libreria-redis redis-cli SCAN 0 MATCH "libreria:*" COUNT 100
docker exec libreria-redis redis-cli TYPE libreria:productos:activos
docker exec libreria-redis redis-cli GET libreria:productos:activos
docker exec libreria-redis redis-cli HGET libreria:productos:activos data
docker exec libreria-redis redis-cli TTL libreria:productos:activos
docker exec libreria-redis redis-cli DEL libreria:productos:activos
```

Para observar los eventos estructurados en tiempo real:

```bash
docker compose logs -f app1 app2 app3 app4
```

**Reiniciar sistema con Base de Datos limpia (Destructivo):**

```bash
docker compose down -v
docker compose up -d
# (Requiere volver a ejecutar el comando del paso 4)

```

**Prueba de Alta Disponibilidad (Failover):**

```bash
docker compose stop app2  # Detiene un nodo; Nginx redireccionará el tráfico
docker compose start app2 # Recupera el nodo

```

## 7. Componente Compartido: Modal de Confirmación (US-28)

Para realizar bajas lógicas o acciones críticas sin redirigir a páginas completas, se utiliza el componente compartido `_ConfirmModal.cshtml`.

### Cómo invocar el Modal desde cualquier vista Razor

1. **Importar el namespace de modelos:**
```cshtml
@using Libreria.Client.Pages.Shared.Models

```


2. **Crear el botón disparador en la tabla o interfaz:**
```cshtml
<button
    type="button"
    class="btn-table btn-table--danger"
    data-confirm-trigger
    data-modal-target="mi-modal-id"
    data-entity-id="@item.PublicId"
    data-entity-name="@item.Nombre"
    data-has-warning="false">
    Dar de baja
</button>

```


3. **Renderizar la vista parcial al final del archivo `.cshtml`:**
```cshtml
@await Html.PartialAsync(
    "_ConfirmModal",
    new ConfirmModalModel
    {
        Id = "mi-modal-id",
        Titulo = "Confirmar baja de registro",
        MensajePrincipal = "¿Está seguro de dar de baja el registro",
        Handler = "DarDeBaja",
        CampoIdentificador = "publicId",
        TextoBotonConfirmar = "Confirmar baja",
        TextoBotonCancelar = "Cancelar",
        MensajeAdvertencia = "Esta acción desactivará el elemento del catálogo activo."
    })

```


4. **Recibir la petición en el `PageModel` (`.cshtml.cs`):**
```csharp
public async Task<IActionResult> OnPostDarDeBajaAsync(string publicId)
{
    // Lógica de descifrado US-36 y baja...
    TempData["MensajeExito"] = "Registro dado de baja correctamente.";
    return RedirectToPage("./Index");
}

```



## 8. Notas de diseño y Arquitectura

* **Escalamiento Horizontal:** El estado de la aplicación no reside en la memoria del contenedor. Las sesiones, tokens Antiforgery y criptografía están centralizados, permitiendo balanceo dinámico sin pérdida de datos.
* **Inversión de Dependencias (SOLID):** El núcleo de la aplicación (`Libreria.Application` y `Libreria.Domain`) no contiene referencias a Entity Framework, SQL Server, Redis o Nginx.
* **Patrón Facade:** Operaciones complejas transaccionales como la Venta se orquestan a través de interfaces (`IVentaFacade`), ocultando la complejidad a la capa de UI.

```

```
