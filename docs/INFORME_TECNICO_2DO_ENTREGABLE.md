# INFORME TÉCNICO DE ARQUITECTURA DE SOFTWARE
## SEGUNDO ELEMENTO DE COMPETENCIA: ESCALAMIENTO HORIZONTAL DE UNA APLICACIÓN MONOLÍTICA
### PROYECTO: SISTEMA DE GESTIÓN DE INVENTARIO Y VENTAS — LIBRERÍA

---

## 1. INTRODUCCIÓN Y OBJETIVOS

### 1.1. Introducción
El desarrollo moderno de software empresarial exige que los sistemas no solo satisfagan requerimientos funcionales de negocio, sino que también garanticen atributos de calidad esenciales como la disponibilidad continua, la tolerancia a fallos, el rendimiento bajo alta concurrencia y la escalabilidad operativa.

En las fases iniciales del proyecto, se implementó un sistema monolítico modular organizado por responsabilidad técnica bajo los principios de la Arquitectura Hexagonal (Puertos y Adaptadores), persistencia relacional con ADO.NET y una interfaz web construida sobre ASP.NET Core Razor Pages. Si bien esta estructura garantizó un alto desacoplamiento y apego a los principios SOLID, su ejecución dependía de un único proceso de servidor en memoria. Esta dependencia introducía un punto único de falla (*Single Point of Failure*, SPOF) y limitaba la capacidad de procesamiento concurrente a los recursos físicos de una sola instancia de cómputo.

El presente entregable documenta la transición e ingeniería de escalamiento horizontal del monolito, transformándolo en un clúster contenedorizado de alta disponibilidad distribuido a través de Docker Compose, balanceado mediante Nginx como proxy inverso, acelerado con Redis como capa de almacenamiento en caché distribuida y blindado mediante la sincronización descentralizada de estado con ASP.NET Core Data Protection.

### 1.2. Objetivos

#### Objetivo General
Evolucionar la arquitectura monolítica modular existente hacia una solución escalada horizontalmente y contenerizada, garantizando alta disponibilidad, distribución de carga entre múltiples réplicas activas, sincronización segura de sesiones y optimización de lectura de datos mediante caché distribuida, preservando íntegramente las reglas de negocio, transacciones ACID y patrones de diseño previamente implementados.

#### Objetivos Específicos
1. **Contenerizar y orquestar el clúster de ejecución:** Diseñar imágenes Docker reproducibles y orquestar mediante Docker Compose un entorno integrado por la base de datos relacional, el motor de caché distribuida, el proxy inverso y cuatro réplicas simultáneas e independientes del monolito web.
2. **Implementar balanceo de carga y tolerancia a fallos:** Configurar Nginx bajo una política de balanceo Round-Robin con detección activa y pasiva de nodos caídos (*health checking* agresivo), garantizando la continuidad inmediata del servicio y exponiendo de manera verificable el identificador del nodo que resuelve cada petición.
3. **Externalizar y sincronizar el estado de sesión y seguridad:** Desacoplar la criptografía de autenticación mediante un anillo compartido de claves criptográficas persistidas a través de un volumen común en ASP.NET Core Data Protection, permitiendo el consumo transparente de Cookies y Claims entre réplicas heterogéneas.
4. **Implementar el patrón Cache-Aside con Redis:** Diseñar un adaptador secundario desacoplado para almacenar en memoria el catálogo de productos activos de alta concurrencia, estableciendo políticas de expiración temporal (TTL) e invalidación atómica ante mutaciones de datos.
5. **Construir el módulo de administración y seguridad de usuarios:** Desarrollar sobre ADO.NET puro un subsistema completo de gestión de usuarios que garantice generación determinista de nombres de usuario únicos, flujos de activación asíncrona mediante tokens seguros por correo electrónico, baja lógica transaccional serializable e inmunidad ante la exposición de credenciales en consultas de lectura.

---

## 2. DESCRIPCIÓN DEL SISTEMA Y EVOLUCIÓN

### 2.1. Descripción del Sistema y Proceso Principal
El sistema tiene como propósito resolver de manera integral la administración operativa de una librería comercial. Su proceso principal radica en el **Flujo Transaccional de Ventas**, el cual involucra operaciones de alta criticidad:
* Consulta dinámica y selección reactiva de clientes y productos.
* Validación estricta de existencias en almacén.
* Registro atómico multilínea en tablas relacionales de cabecera y detalle (`Venta` y `DetalleVenta`).
* Deducción y restauración matemática de existencias físicas mediante fachadas de inventario.
* Auditoría formal de transacciones (usuario creador, usuario anulador, marcas temporales).
* Emisión automática del comprobante de venta fiscal.

### 2.2. Evolución Arquitectónica hacia el Segundo Entregable
Durante el primer entregable, la aplicación operaba bajo un esquema monolítico en un único entorno de ejecución local (`localhost:5000`), donde el ciclo de vida del servidor web concentraba la gestión de memoria, la emisión de cookies y las conexiones a base de datos.

La evolución implementada para este segundo hito transformó radicalmente el modelo operacional sin fracturar el código de dominio ni la distribución por proyectos:

```
+-----------------------------------------------------------------------------------+
| PRIMER ENTREGABLE: Monolito Local                                                 |
| [Navegador] ---> [Instancia Única Kestrel] ---> [Base de Datos Relacional]         |
|                  * Claves criptográficas en RAM local                             |
|                  * Consultas repetitivas sin caché                                |
|                  * Si el proceso cae, el sistema colapsa                          |
+-----------------------------------------------------------------------------------+
                                          |
                                          v
+-----------------------------------------------------------------------------------+
| SEGUNDO ENTREGABLE: Monolito Escalado Horizontalmente                             |
|                                                                                   |
|                  +------------------ [ Nginx Proxy ] <--- [Navegador Web]         |
|                  |                     (:3000)                                    |
|                  v                                                                |
|       +--------------------+--------------------+--------------------+            |
|       |                    |                    |                    |            |
|       v                    v                    v                    v            |
|  [Réplica App 1]      [Réplica App 2]      [Réplica App 3]      [Réplica App 4]   |
|       |                    |                    |                    |            |
|       +----------+---------+----------+---------+----------+---------+            |
|                  |                    |                    |                      |
|                  v                    v                    v                      |
|         [Volumen Compartido]   [Cluster Redis]   [SQL Server 2022]                |
|           (/keys DataProt)       (Caché TTL)     (Persistencia ACID)              |
+-----------------------------------------------------------------------------------+
```

Esta transición convierte al sistema en una plataforma resiliente capaz de soportar la pérdida abrupta de nodos de procesamiento sin degradación de cara al usuario final.

---

## 3. ARQUITECTURA EXISTENTE VS. ESCALADA HORIZONTALMENTE: EL RETO DEL ESTADO

### 3.1. Cuadro Comparativo Arquitectónico

| Dimensión Arquitectónica | Arquitectura Anterior (Monolito Aislado) | Arquitectura Escalada (Monolito Distribuido) |
| :--- | :--- | :--- |
| **Topología de Procesos** | Proceso único en ejecución local. | Cuatro procesos independientes en contenedores Docker. |
| **Punto de Entrada** | Acceso directo al puerto de la aplicación. | Proxy inverso Nginx con balanceo Round-Robin. |
| **Tolerancia a Fallos** | Nula. La detención del proceso causa indisponibilidad total. | Alta. Si un nodo falla, Nginx redirige el tráfico en milisegundos. |
| **Gestión de Sesiones** | Anillo criptográfico efímero en memoria de la instancia. | Anillo de claves descentralizado mediante volumen compartido (`keys_data`). |
| **Rendimiento de Lectura** | Cada petición impacta directamente al motor relacional. | Patrón Cache-Aside sobre Redis con TTL e invalidación dirigida. |
| **Persistencia de Identidad** | Conexiones locales no segregadas. | Red virtual aislada bridge con variables de entorno protegidas. |

### 3.2. El Reto Técnico de la Externalización del Estado
El principal obstáculo técnico al escalar horizontalmente aplicaciones basadas en cookies y claims consiste en la **fragmentación del estado criptográfico**.

En una configuración predeterminada de ASP.NET Core, el proveedor de protección de datos (*Data Protection API*) genera una clave simétrica única almacenada en la memoria volátil o en el perfil de usuario del host local. Bajo un esquema balanceado:
1. Una petición inicial de autenticación aterriza en la **Réplica 1**, la cual cifra la cookie de autenticación con su clave privada local.
2. La siguiente petición de navegación es dirigida por Nginx hacia la **Réplica 2**.
3. Al carecer de la clave privada que originó el cifrado, la Réplica 2 es incapaz de desencriptar el payload de la cookie, interpreta la sesión como corrupta, expulsa al usuario del sistema y lo redirige compulsivamente al formulario de inicio de sesión (`/Cuenta/Login`).

Para resolver este desafío y garantizar nodos completamente desprovistos de estado (*stateless*), se desacopló el ciclo de vida de las claves del ciclo de vida de los contenedores. Se configuró un volumen Docker compartido (`keys_data`) montado en la ruta `/keys` de cada réplica, forzando a todas las instancias a converger en un mismo almacén criptográfico centralizado y a identificarse bajo el mismo nombre de aplicación unificado (`LibreriaCompartida`).

---

## 4. DIAGRAMAS DE ARQUITECTURA CON C4 MODEL (NIVELES 1, 2 Y 3)

El modelado formal de la arquitectura se estructuró siguiendo el estándar **C4 Model**, permitiendo descomponer progresivamente el sistema desde el contexto operacional hasta la interacción interna de sus módulos de software.

### 4.1. Nivel 1: Diagrama de Contexto del Sistema
El diagrama de contexto establece los límites del sistema, identificando los actores humanos primarios y los sistemas externos con los que colabora:

```mermaid
graph TB
    subgraph Actores["Usuarios del Sistema"]
        Admin["👤 Administrador<br>[Rol: Gestión total, auditoría, catálogos y usuarios]"]
        Vendedor["👤 Vendedor<br>[Rol: Registro de ventas, consulta y clientes]"]
    end

    subgraph Frontera["Frontera del Sistema: Librería"]
        SistemaLibreria["📚 Sistema de Gestión de Librería<br>[Monolito Escalado Horizontalmente]<br>Permite el control de inventario, facturación y usuarios"]
    end

    subgraph Externos["Sistemas Externos"]
        ServidorCorreo["✉️ Servidor SMTP<br>[Infraestructura de Red]<br>Envío de correos con enlaces de activación"]
    end

    Admin -->|Gestiona catálogos, audita y opera usuarios| SistemaLibreria
    Vendedor -->|Registra transacciones y clientes| SistemaLibreria
    SistemaLibreria -->|Envía credenciales de activación mediante SMTP| ServidorCorreo
```

**Explicación del Nivel 1:**
* **Administrador:** Usuario autenticado con privilegios completos para gestionar el catálogo de inventario, dar de alta y baja usuarios, auditar márgenes de ganancia y anular transacciones.
* **Vendedor:** Usuario de mostrador enfocado exclusivamente en la captura rápida de ventas, consulta de existencias y alta rápida de clientes.
* **Sistema de Gestión de Librería:** Núcleo de software responsable de aplicar las políticas de negocio, transacciones ACID y caché distribuida.
* **Servidor SMTP:** Sistema de transporte de mensajería electrónica utilizado para notificar a nuevos usuarios su enlace de activación de cuenta.

---

### 4.2. Nivel 2: Diagrama de Contenedores
Ilustra la topología de contenedores Docker desplegados, los protocolos de comunicación de red utilizados y las responsabilidades de infraestructura asignadas:

```mermaid
graph TB
    subgraph Clientes["Navegadores Clientes"]
        Browser["🌐 Navegador Web del Usuario"]
    end

    subgraph DMZ["Punto de Entrada / Proxy Inverso"]
        Nginx["📦 libreria-nginx<br>[Nginx:alpine]<br>Proxy inverso, balanceo Round-Robin, cabeceras HTTP"]
    end

    subgraph AppCluster["Clúster de Aplicación"]
        App1["📦 libreria-app1<br>[ASP.NET Core Razor Pages]<br>Puerto interno :8080"]
        App2["📦 libreria-app2<br>[ASP.NET Core Razor Pages]<br>Puerto interno :8080"]
        App3["📦 libreria-app3<br>[ASP.NET Core Razor Pages]<br>Puerto interno :8080"]
        App4["📦 libreria-app4<br>[ASP.NET Core Razor Pages]<br>Puerto interno :8080"]
    end

    subgraph DataServices["Servicios de Persistencia y Caché"]
        Redis["📦 libreria-redis<br>[Redis:alpine]<br>Almacenamiento clave-valor en RAM (:6379)"]
        Database["📦 libreria-db<br>[SQL Server 2022 Express]<br>Motor relacional transaccional (:1433)"]
    end

    subgraph SharedStorage["Almacenamiento Compartido"]
        KeysVol[("📁 keys_data<br>[Volumen Docker]<br>Claves criptográficas sincronizadas")]
    end

    Browser -->|HTTP :3000| Nginx
    Nginx -->|HTTP :8080| App1
    Nginx -->|HTTP :8080| App2
    Nginx -->|HTTP :8080| App3
    Nginx -->|HTTP :8080| App4

    App1 -.->|Lectura/Escritura de claves| KeysVol
    App2 -.->|Lectura/Escritura de claves| KeysVol
    App3 -.->|Lectura/Escritura de claves| KeysVol
    App4 -.->|Lectura/Escritura de claves| KeysVol

    App1 -->|RESP Protocol :6379| Redis
    App2 -->|RESP Protocol :6379| Redis
    App3 -->|RESP Protocol :6379| Redis
    App4 -->|RESP Protocol :6379| Redis

    App1 -->|"TDS Protocol :1433 (ADO.NET)"| Database
    App2 -->|"TDS Protocol :1433 (ADO.NET)"| Database
    App3 -->|"TDS Protocol :1433 (ADO.NET)"| Database
    App4 -->|"TDS Protocol :1433 (ADO.NET)"| Database
```

**Explicación del Nivel 2:**
El tráfico público ingresa por el puerto TCP `3000` del host hacia el contenedor `libreria-nginx`. Este distribuye las peticiones mediante balanceo hacia cuatro réplicas de aplicación (`libreria-app1` a `libreria-app4`), las cuales ejecutan la imagen compilada sobre .NET 10. Las réplicas comparten el volumen `keys_data` para no invalidar sesiones y consumen concurrentemente `libreria-redis` (puerto `6379`) y `libreria-db` (puerto `1433`).

---

### 4.3. Nivel 3: Diagrama de Componentes (Interno de una Réplica)
Detalla la estructuración interna de código dentro de cualquiera de las réplicas activas, evidenciando el desacoplamiento estricto de la Arquitectura Hexagonal:

```mermaid
graph TB
    subgraph ClientLayer["Adaptador Primario (Libreria.Client)"]
        RazorPages["📄 Razor Pages Models<br>(Ventas, Usuarios, Diagnóstico)"]
        ReplicaBadge["🏷️ _ReplicaBadge<br>(Extracción de Hostname/MachineName)"]
        AuthEvents["🛡️ ValidarEstadoUsuarioCookieEvents<br>(Validación de estado activo en BD)"]
    end

    subgraph ApplicationLayer["Núcleo Hexagonal (Libreria.Application)"]
        VentaFacade["⚡ VentaFacade / StockFacade<br>(Patrón Facade - Transacciones)"]
        ProdService["⚡ ProductosActivosService<br>(Patrón Cache-Aside)"]
        UserService["⚡ UsuarioService / SeguridadService<br>(Lógica de Negocio y Bajas)"]
        PrimaryPorts["🚪 Puertos Primarios (Interfaces)<br>(IVentaFacade, IUsuarioService, etc.)"]
        SecondaryPorts["🔌 Puertos Secundarios (Interfaces)<br>(IVentaRepository, ICacheStore, etc.)"]
    end

    subgraph InfraLayer["Adaptador Secundario (Libreria.Infrastructure)"]
        SqlRepos["💾 Repositorios ADO.NET<br>(VentaRepository, UsuarioRepository)"]
        RedisStore["⚡ RedisCacheStore<br>(StackExchange.Redis Adapter)"]
        DataProtectionCfg["🔐 DataProtection Provider<br>(Persistencia física en /keys)"]
    end

    subgraph DomainLayer["Núcleo Puro (Libreria.Domain)"]
        Entities["📦 Entidades & Catálogos<br>(Venta, DetalleVenta, Usuario, Rol)"]
    end

    RazorPages -->|Consume| PrimaryPorts
    AuthEvents -->|Consume| PrimaryPorts
    PrimaryPorts --> VentaFacade
    PrimaryPorts --> ProdService
    PrimaryPorts --> UserService

    VentaFacade -->|Usa| SecondaryPorts
    ProdService -->|Usa| SecondaryPorts
    UserService -->|Usa| SecondaryPorts

    SecondaryPorts -.->|Implementado por| SqlRepos
    SecondaryPorts -.->|Implementado por| RedisStore

    SqlRepos --> Entities
    VentaFacade --> Entities
    UserService --> Entities
```

**Explicación del Nivel 3:**
* **`Libreria.Client` (Adaptador Primario):** Gestiona la presentación web. Consume exclusivamente puertos primarios mediante Inyección de Dependencias sin instanciar clases de base de datos ni conocer a Redis.
* **`Libreria.Application` (Lógica de Aplicación):** Contiene la inteligencia del negocio. Las fachadas y servicios coordinan los flujos transaccionales e interactúan hacia el exterior únicamente mediante contratos (puertos secundarios).
* **`Libreria.Infrastructure` (Adaptador Secundario):** Implementa el acceso a datos crudo mediante ADO.NET, la serialización hacia Redis y el cifrado de datos.
* **`Libreria.Domain` (Dominio Puro):** Modelo de objetos sin dependencias de infraestructura ni frameworks.

---

## 5. VISTA INTEGRADA (NIVELES 1, 2 Y 3: DIAGRAMA COMBINADO C4)

A continuación, se presenta la vista arquitectónica unificada que correlaciona a los actores, la capa perimetral de Nginx, el clúster de réplicas y el flujo detallado de componentes hacia la persistencia y la memoria:

```mermaid
graph TB
    subgraph Actores["Actores del Sistema (Nivel 1)"]
        Admin["👤 Administrador"]
        Vendedor["👤 Vendedor"]
    end

    subgraph Perimetro["Proxy Inverso & Balanceador (Nivel 2)"]
        Nginx["🌐 Nginx (Reverse Proxy)<br>[Docker: libreria-nginx]<br>Balanceo Round-Robin :3000"]
    end

    Admin -->|HTTPS / HTTP :3000| Nginx
    Vendedor -->|HTTPS / HTTP :3000| Nginx

    subgraph Clúster["Clúster de Réplicas ASP.NET Core (Nivel 2 y 3)"]
        
        subgraph App1["Réplica 1 (libreria-app1)"]
            subgraph Client1["Libreria.Client (Adaptador Primario)"]
                Razor1["Razor Pages / UI Controllers<br>(Ventas, Usuarios, Diagnóstico)"]
                ReplicaBadge1["_ReplicaBadge<br>(Hostname / MachineName)"]
                AuthEvents1["CookieAuthEvents<br>(ValidarEstadoUsuario)"]
            end

            subgraph AppCore1["Libreria.Application (Núcleo Hexagonal)"]
                VentaFacade1["VentaFacade / StockFacade<br>(Orquestación Transaccional)"]
                CacheService1["ProductosActivosService<br>(Patrón Cache-Aside)"]
                UsuarioService1["UsuarioService / Seguridad<br>(Activación & Baja Lógica)"]
                Ports1["Puertos Primarios & Secundarios<br>(IVentaFacade, ICacheStore, etc.)"]
            end

            subgraph Infra1["Libreria.Infrastructure (Adaptador Secundario)"]
                Repos1["Repositorios ADO.NET<br>(Venta, Usuario, Producto)"]
                RedisAdapter1["RedisCacheStore<br>(StackExchange.Redis)"]
                DataProt1["ASP.NET Core Data Protection<br>(Claves compartidas)"]
            end

            Razor1 --> Ports1
            Ports1 --> VentaFacade1
            Ports1 --> CacheService1
            Ports1 --> UsuarioService1
            VentaFacade1 --> Repos1
            CacheService1 --> RedisAdapter1
            CacheService1 --> Repos1
            AuthEvents1 --> Repos1
        end

        subgraph App2["Réplica 2 (libreria-app2)"]
            App2Core["[Instancia idéntica de componentes C4-N3]<br>Razor Pages + Application + Infrastructure"]
        end

        subgraph App3["Réplica 3 (libreria-app3)"]
            App3Core["[Instancia idéntica de componentes C4-N3]<br>Razor Pages + Application + Infrastructure"]
        end
    end

    Nginx -->|HTTP :8080| App1
    Nginx -->|HTTP :8080| App2
    Nginx -->|HTTP :8080| App3

    subgraph Persistencia["Servicios de Datos y Caché (Nivel 2)"]
        RedisServer[("⚡ Redis Cache<br>[Docker: libreria-redis]<br>Protocolo RESP :6379<br>Catálogo cacheados & TTL")]
        Database[("🗄️ Base de Datos Relacional<br>[Docker: libreria-db]<br>SQL Server 2022 / TDS :1433<br>Tablas: Venta, Detalle, Usuario, etc.")]
    end

    RedisAdapter1 -->|"Cache HIT/MISS (RESP :6379)"| RedisServer
    App2Core -->|"Cache HIT/MISS (RESP :6379)"| RedisServer
    App3Core -->|"Cache HIT/MISS (RESP :6379)"| RedisServer

    Repos1 -->|"Transacciones ADO.NET (TDS :1433)"| Database
    App2Core -->|"Transacciones ADO.NET (TDS :1433)"| Database
    App3Core -->|"Transacciones ADO.NET (TDS :1433)"| Database
```

---

## 6. DIAGRAMA DE DESPLIEGUE (DEPLOYMENT DIAGRAM)

El siguiente diagrama formaliza el mapeo físico entre el hardware anfitrión (*Host OS*), el motor de ejecución de Docker, las redes virtuales tipo bridge, los puntos de montaje de volúmenes persistentes y el aislamiento de puertos:

```mermaid
flowchart TB
    subgraph Host["💻 Host Físico de Laboratorio (Linux / Windows PC)"]
        subgraph Ports["Puertos Expuestos al Host"]
            P3000["Puerto 3000 (HTTP Web)"]
            P1433["Puerto 1433 (DBMS)"]
            P6379["Puerto 6379 (Redis CLI)"]
        end

        subgraph DockerRuntime["🐳 Docker Engine / Compose Environment"]
            subgraph DockerNet["Red Virtual Compartida (bridge: default)"]

                NginxCont["📦 Contenedor: libreria-nginx<br>Imagen: nginx:alpine<br>IP Interna: Dinámica<br>Escucha: :80"]
                
                App1Cont["📦 Contenedor: libreria-app1<br>Imagen: libreria-app:latest<br>Escucha: :8080"]
                App2Cont["📦 Contenedor: libreria-app2<br>Imagen: libreria-app:latest<br>Escucha: :8080"]
                App3Cont["📦 Contenedor: libreria-app3<br>Imagen: libreria-app:latest<br>Escucha: :8080"]
                
                RedisCont["📦 Contenedor: libreria-redis<br>Imagen: redis:alpine<br>Escucha: :6379"]
                
                DbCont["📦 Contenedor: libreria-db<br>Imagen: mcr.microsoft.com/mssql/server:2022<br>Escucha: :1433"]
            end

            subgraph Volumes["💾 Volúmenes Docker Persistentes"]
                VolKeys[("📁 keys_data<br>[Montado en /keys]<br>Anillo de claves Data Protection compartido")]
                VolData[("📁 mssql_data<br>[Montado en /var/opt/mssql]<br>Archivos físicos MDF/LDF de la BD")]
                VolInit[("📁 ./db/init<br>[Montado en /scripts:ro]<br>Scripts SQL de inicialización")]
                VolNginx[("📁 ./nginx.conf<br>[Montado en /etc/nginx/nginx.conf:ro]")]
            end
        end
    end

    P3000 -->|Mapeo :3000 -> :80| NginxCont
    P1433 -.->|Mapeo :1433 -> :1433| DbCont
    P6379 -.->|Mapeo :6379 -> :6379| RedisCont

    VolNginx -.-> NginxCont
    VolInit -.-> DbCont
    VolData === DbCont

    App1Cont === VolKeys
    App2Cont === VolKeys
    App3Cont === VolKeys

    NginxCont -->|Balanceo Round-Robin :8080| App1Cont
    NginxCont -->|Balanceo Round-Robin :8080| App2Cont
    NginxCont -->|Balanceo Round-Robin :8080| App3Cont

    App1Cont -->|Lectura / Escritura de Caché| RedisCont
    App2Cont -->|Lectura / Escritura de Caché| RedisCont
    App3Cont -->|Lectura / Escritura de Caché| RedisCont

    App1Cont -->|ADO.NET Connections & Queries| DbCont
    App2Cont -->|ADO.NET Connections & Queries| DbCont
    App3Cont -->|ADO.NET Connections & Queries| DbCont
```

---

## 7. PATRONES ARQUITECTÓNICOS Y DE DISEÑO IMPLEMENTADOS

### 7.1. Inversión de Dependencias (IoC / Dependency Injection)
El principio de Inversión de Dependencias (la **D** de SOLID) se cumple estrictamente mediante el contenedor nativo de ASP.NET Core configurado en `Program.cs`. Las capas superiores jamás instancian implementaciones concretas; dependen únicamente de interfaces contractualmente fijadas en la capa de Aplicación.

### 7.2. Patrón Repository (Sobre ADO.NET Puro)
Para desacoplar el motor de base de datos de la lógica de negocio, se diseñaron repositorios tipados (`UsuarioRepository`, `VentaRepository`, `ProductoRepository`) bajo contratos de puertos secundarios (`IUsuarioRepository`, `IVentaRepository`, `IProductoRepository`). Esto asegura que ninguna sentencia SQL o dependencia de `Microsoft.Data.SqlClient` se filtre hacia el dominio.

### 7.3. Patrón Facade (`VentaFacade` y `StockFacade`)
El proceso de ventas representa un flujo de alta complejidad que exige coordinar múltiples servicios heterogéneos: validación de cliente, comprobación de catálogo, apertura de conexión física, inicio de transacción ACID, inserción en cabecera y detalle, descuento de stock y cálculo de márgenes. 
El patrón **Facade** encapsula toda esta orquestación técnica compleja detrás de un método simple y cohesivo (`RegistrarVentaAsync`), entregando a la UI un punto de contacto único, claro y desacoplado.

### 7.4. Cumplimiento de Principios SOLID y Clean Code
* **S (Single Responsibility):** Se separó la lógica de validación de contraseñas (`PasswordPolicyValidator`), generación de identificadores (`GeneradorNombreUsuario`), emisión de correos (`SmtpEmailSender`) y persistencia de seguridad (`UsuarioSeguridadService`).
* **O (Open/Closed):** La arquitectura de repositorios basada en fábricas abstractas (`CrudRepositoryFactory<T>`) permite extender nuevas entidades de catálogo sin modificar el código base.
* **L (Liskov Substitution):** Todas las clases derivadas de repositorios cumplen íntegramente los contratos definidos en `ICrudRepository<T>` sin alterar precondiciones ni lanzar excepciones de falta de implementación.
* **I (Interface Segregation):** Se segregaron puertos extensos en contratos específicos: `IConsultaVentaService` para búsquedas, `IComprobanteVentaService` para lectura fiscal y `IVentaFacade` para mutaciones transaccionales.
* **D (Dependency Inversion):** Los servicios de aplicación solo reciben abstracciones en sus constructores, permitiendo pruebas unitarias con fakes e inversión del flujo de dependencias.

---

## 8. DECISIONES TÉCNICAS Y EVIDENCIAS DE CÓDIGO

### 8.1. Orquestación del Clúster (`docker-compose.yml`)
**Decisión Técnica:** Se optó por levantar el clúster con cuatro réplicas de aplicación (superando el umbral de tres exigido), configurando dependencias de salud estrictas (`condition: service_healthy`) para asegurar que los contenedores web inicien únicamente tras la disponibilidad comprobada de Redis y SQL Server.

```yaml
services:
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: libreria-db
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "Libreria2026!"
      MSSQL_PID: "Express"
    ports:
      - "1433:1433"
    volumes:
      - mssql_data:/var/opt/mssql
      - ./db/init:/scripts:ro
    healthcheck:
      test: ["CMD", "/opt/mssql-tools18/bin/sqlcmd", "-S", "localhost", "-U", "sa", "-P", "Libreria2026!", "-C", "-Q", "SELECT 1"]
      interval: 10s
      timeout: 5s
      retries: 10

  redis:
    image: redis:alpine
    container_name: libreria-redis
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  app1:
    image: libreria-app:latest
    container_name: libreria-app1
    env_file:
      - .env
    volumes:
      - keys_data:/keys
    depends_on:
      db:
        condition: service_healthy
      redis:
        condition: service_healthy

  app2:
    image: libreria-app:latest
    container_name: libreria-app2
    env_file:
      - .env
    volumes:
      - keys_data:/keys
    depends_on:
      db:
        condition: service_healthy
      redis:
        condition: service_healthy

  app3:
    image: libreria-app:latest
    container_name: libreria-app3
    env_file:
      - .env
    volumes:
      - keys_data:/keys
    depends_on:
      db:
        condition: service_healthy
      redis:
        condition: service_healthy

  app4:
    image: libreria-app:latest
    container_name: libreria-app4
    env_file:
      - .env
    volumes:
      - keys_data:/keys
    depends_on:
      db:
        condition: service_healthy
      redis:
        condition: service_healthy

  nginx:
    image: nginx:alpine
    container_name: libreria-nginx
    ports:
      - "3000:80"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
    depends_on:
      - app1
      - app2
      - app3
      - app4 

volumes:
  mssql_data:
  keys_data:
```

### 8.2. Balanceo de Carga y Conmutación por Falla (`nginx.conf`)
**Decisión Técnica:** Se configuró un bloque `upstream` con algoritmo Round-Robin. Para garantizar la continuidad de servicio ante la caída deliberada de un nodo (`docker stop`), se fijaron umbrales de detección agresivos (`max_fails=2`, `fail_timeout=5s`, `proxy_connect_timeout 3s`) combinados con la directiva `proxy_next_upstream`, la cual desvía la petición en vuelo a un nodo sano sin arrojar un error HTTP 502 al usuario. Se añadió además el encabezado de diagnóstico `X-Served-By`.

```nginx
events { 
    worker_connections 1024; 
}

http {
    upstream backend {
        server app1:8080 max_fails=2 fail_timeout=5s;
        server app2:8080 max_fails=2 fail_timeout=5s;
        server app3:8080 max_fails=2 fail_timeout=5s;
        server app4:8080 max_fails=2 fail_timeout=5s;
    }

    server {
        listen 80;

        location / {
            proxy_pass http://backend;
            proxy_set_header Host $http_host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            
            add_header X-Served-By $upstream_addr always;
            
            proxy_connect_timeout 3s;
            proxy_next_upstream error timeout http_500 http_502 http_503 http_504;
        }
    }
}
```

### 8.3. Visualización del Identificador Real de Réplica (`_ReplicaBadge.cshtml`)
**Decisión Técnica:** Para transparentar qué nodo del clúster resolvió la transacción y validar el balanceo en la interfaz visual, se implementó una vista parcial que renderiza la propiedad nativa del sistema operativo `@Environment.MachineName`. En entornos contenerizados bajo Linux, esta propiedad devuelve dinámicamente el *Container Hostname*, demostrando que no se trata de un valor estático simulado.

```cshtml
<div style="background-color: #2c3e50; color: #ecf0f1; padding: 5px 15px; text-align: center; font-size: 0.9rem; font-weight: bold; border-bottom: 2px solid #3498db; margin-bottom: 15px;">
    Servidor que atendió la solicitud: <span style="color: #f1c40f;">@Environment.MachineName</span>
</div>
```

### 8.4. Sincronización de Claves Criptográficas en `Program.cs`
**Decisión Técnica:** Se invocó `AddDataProtection` indicando el almacenamiento de claves en la ruta `/keys` (montada sobre el volumen Docker persistente `keys_data`) y asignando un nombre de aplicación homogéneo (`LibreriaCompartida`).

```csharp
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"/keys"))
    .SetApplicationName("LibreriaCompartida");
```

### 8.5. Caché Distribuida con Redis y Patrón Cache-Aside
**Decisión Técnica:** Se seleccionó el listado de productos activos como la consulta de mayor frecuencia de lectura. Se implementó `ProductosActivosService` gestionando las condiciones de HIT, MISS, almacenamiento con expiración de 5 minutos e invalidación atómica ante modificaciones.

```csharp
public async Task<IReadOnlyList<Producto>> ObtenerAsync(
    ICrudRepository<Producto> repository)
{
    var cronometro = Stopwatch.StartNew();

    try
    {
        var productosCacheados = await _cacheStore.ObtenerAsync<List<Producto>>(
            CacheKeys.ProductosActivos);

        if (productosCacheados is not null)
        {
            var ttl = await ObtenerTiempoVidaSeguroAsync();
            cronometro.Stop();
            await RegistrarResultadoAsync(
                "HIT",
                "Redis",
                ttl,
                cronometro.Elapsed.TotalMilliseconds);

            return productosCacheados; 
        }
    }
    catch (Exception exception)
    {
        _logger.LogWarning(exception, "Redis no disponible; fallback a base de datos");
        var productosSinRedis = await repository.ObtenerActivasAsync();
        cronometro.Stop();
        await RegistrarResultadoAsync("MISS", "SQL", null, cronometro.Elapsed.TotalMilliseconds);
        return productosSinRedis;
    }

    var productos = await repository.ObtenerActivasAsync();

    try
    {
        await _cacheStore.GuardarAsync(
            CacheKeys.ProductosActivos,
            productos,
            _expiracionCache);
    }
    catch (Exception exception)
    {
        _logger.LogWarning(exception, "No se pudo almacenar en Redis");
    }

    var tiempoVida = await ObtenerTiempoVidaSeguroAsync();
    cronometro.Stop();
    await RegistrarResultadoAsync("MISS", "SQL", tiempoVida, cronometro.Elapsed.TotalMilliseconds);

    return productos;
}


public async Task InvalidarAsync()
{
    try
    {
        await _cacheStore.EliminarAsync(CacheKeys.ProductosActivos);
        _logger.LogInformation("Caché invalidada Clave={CacheKey} Réplica={Replica}",
            CacheKeys.ProductosActivos, Environment.MachineName);
    }
    catch (Exception exception)
    {
        _logger.LogWarning(exception, "Error al invalidar clave en Redis");
    }
}
```

---

## 9. MÓDULO DE USUARIOS: ADMINISTRACIÓN, SEGURIDAD Y ADO.NET

### 9.1. Consulta SELECT Segura (Sin Exposición de Credenciales)
**Decisión de Seguridad:** Para evitar brechas de seguridad o fugas de información sensible en memoria, la sentencia SQL en ADO.NET dentro de `UsuarioRepository.cs` proyecta estrictamente las columnas públicas y de auditoría, prescindiendo por completo de `PasswordHash` o `TokenActivacionHash`.

```csharp
public async Task<IReadOnlyList<UsuarioListItemModel>> ObtenerListadoAsync(string? estado = null)
{
    await using var connection = await CrearConexionAbiertaAsync();
    await using var command = connection.CreateCommand();

    command.CommandText = @"
        SELECT u.PublicId, u.NombreCompleto, u.NombreUsuario, u.Correo,
               r.Nombre AS Rol, u.Estado, u.FechaCreacion,
               creador.NombreCompleto AS CreadoPor, 
               u.FechaModificacion, 
               modificador.NombreCompleto AS ModificadoPor
        FROM dbo.Usuario u
        INNER JOIN dbo.Rol r ON r.RolId = u.RolId
        LEFT JOIN dbo.Usuario creador ON creador.UsuarioId = u.UsuarioCreacionId
        LEFT JOIN dbo.Usuario modificador ON modificador.UsuarioId = u.UsuarioModificacionId
        WHERE (@Estado IS NULL OR u.Estado = @Estado)
        ORDER BY u.FechaCreacion DESC, u.NombreUsuario ASC";

    AgregarParametro(command, "@Estado", estado is null ? DBNull.Value : estado, DbType.String, 20);

    var usuarios = new List<UsuarioListItemModel>();
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        usuarios.Add(new UsuarioListItemModel(
            reader.GetGuid(reader.GetOrdinal("PublicId")),
            reader.GetString(reader.GetOrdinal("NombreCompleto")),
            reader.GetString(reader.GetOrdinal("NombreUsuario")),
            reader.GetString(reader.GetOrdinal("Correo")),
            reader.GetString(reader.GetOrdinal("Rol")),
            reader.GetString(reader.GetOrdinal("Estado")),
            reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
            reader.IsDBNull(reader.GetOrdinal("CreadoPor")) ? null : reader.GetString(reader.GetOrdinal("CreadoPor")),
            reader.IsDBNull(reader.GetOrdinal("ModificadoPor")) ? null : reader.GetString(reader.GetOrdinal("ModificadoPor")),
            reader.IsDBNull(reader.GetOrdinal("FechaModificacion")) ? null : reader.GetDateTime(reader.GetOrdinal("FechaModificacion"))));
    }

    return usuarios;
}
```

### 9.2. Generación Determinista de Nombre de Usuario Único
**Regla de Negocio:** El sistema compone el nombre de usuario normalizando el texto (remoción de acentos mediante descomposición Unicode `FormD`), extrayendo la inicial del primer nombre y concatenando el apellido completo. Si el nombre base colisiona en base de datos, incrementa un sufijo numérico secuencial a partir del número 2.

```csharp
public async Task<string> GenerarAsync(string nombre, string apellido)
{
    var nombreNormalizado = Normalizar(nombre);
    var apellidoNormalizado = Normalizar(apellido);

    var nombreBase = $"{nombreNormalizado[0]}{apellidoNormalizado}";

    if (!await _usuarioRepository.ExisteNombreUsuarioAsync(nombreBase))
        return nombreBase;

    var numero = 2;
    while (await _usuarioRepository.ExisteNombreUsuarioAsync($"{nombreBase}{numero}"))
    {
        numero++;
    }

    return $"{nombreBase}{numero}";
}
```

### 9.3. Inserción Transaccional y Enlace de Activación por Correo
**Flujo Criptográfico Seguro:** El nuevo usuario se crea en estado inactivo (`Pendiente`) con `PasswordHash` vacío. Se genera un token criptográfico en Base64 URL-safe; **únicamente el hash SHA-256 del token es persistido en base de datos**, garantizando que si la base de datos sufriera una filtración, los enlaces en tránsito no puedan ser reconstruidos. El token original viaja en el cuerpo del correo mediante SMTP.

```csharp
var token = TokenActivacionService.GenerarToken();
var tokenHash = TokenActivacionService.CalcularHash(token);
var expiracion = DateTime.UtcNow.AddHours(24);

var usuario = new Usuario
{
    Nombre = input.Nombre.Trim(),
    Apellido = input.Apellido.Trim(),
    Correo = correo,
    NombreUsuario = nombreUsuario,
    NombreCompleto = $"{input.Nombre.Trim()} {input.Apellido.Trim()}",
    PasswordHash = string.Empty,
    RolId = input.RolId,
    Estado = EstadosUsuario.Pendiente, 
    TokenActivacionHash = tokenHash,   
    TokenActivacionExpiracion = expiracion,
    UsuarioCreacionId = usuarioCreacionId
};

await using var connection = _connectionFactory.CreateConnection();
await using var transaction = await connection.BeginTransactionAsync();

try
{
    await _usuarioRepository.InsertarAsync(usuario, connection, transaction);

    var enlace = ConstruirEnlaceActivacion(tokenHash, token);
    var cuerpoHtml = ConstruirCorreo(usuario.Nombre, usuario.NombreUsuario, enlace);

    await _emailSender.EnviarAsync(usuario.Correo, "Activa tu cuenta - Sistema Librería", cuerpoHtml);

    await transaction.CommitAsync();
    return usuario.NombreUsuario;
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### 9.4. Baja Lógica y Bloqueo Inmediato de Sesión

#### Reglas de Negocio en `UsuarioSeguridadService.cs`
Para evitar condiciones de carrera, la baja lógica se ejecuta bajo un nivel de aislamiento `IsolationLevel.Serializable`. Se prohíbe explícitamente que un usuario se dé de baja a sí mismo y se valida estrictamente que no sea posible eliminar al último administrador activo del sistema.

```csharp
public async Task<ResultadoOperacion> DarDeBajaAsync(Guid publicId)
{
    var actorId = _usuarioActual.UsuarioId;
    if (actorId is not > 0)
        return Error("Debe iniciar sesión como administrador.");

    await using var connection = _connectionFactory.CreateConnection();
    await connection.OpenAsync();
    await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);

    try
    {
        var usuarios = await _repository.BloquearParaBajaAsync(connection, transaction);
        var actor = usuarios.SingleOrDefault(u => u.UsuarioId == actorId);
        if (actor is null || !EsAdministradorActivo(actor))
            return Error("Solo un administrador activo puede dar de baja usuarios.");

        var usuario = usuarios.SingleOrDefault(u => u.PublicId == publicId);
        if (usuario is null) return ResultadoOperacion.NoExiste();
        if (usuario.Estado == EstadosUsuario.Inactivo)
            return Error("El usuario ya se encuentra inactivo.");
        if (EsAdministradorActivo(usuario) && usuarios.Count(EsAdministradorActivo) <= 1)
            return Error("No se puede dar de baja al último administrador activo.");
        if (usuario.UsuarioId == actorId)
            return Error("No puede darse de baja a sí mismo.");

        await _repository.DesactivarAsync(usuario.UsuarioId, actorId.Value, connection, transaction);
        await transaction.CommitAsync();
        return ResultadoOperacion.Correcto();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

#### Revocación de Sesión entre Réplicas (`ValidarEstadoUsuarioCookieEvents.cs`)
Aunque la cookie de sesión del usuario continúe vigente temporalmente en su navegador, el middleware de autenticación consulta en tiempo de ejecución a la base de datos; si el usuario fue cambiado a `Inactivo`, la cookie es destruida y el usuario es expulsado de inmediato en la siguiente petición.

```csharp
public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
{
    var identificador = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (!int.TryParse(identificador, out var usuarioId) ||
        !await _seguridad.SesionActivaAsync(usuarioId))
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(context.Scheme.Name);
    }
}
```

---

## 10. CONCLUSIONES

1. **Eficiencia del Escalamiento Horizontal:** Se comprobó experimentalmente que un monolito arquitectónicamente limpio y estructurado por responsabilidad técnica puede escalar horizontalmente sin requerir una reescritura a microservicios. Desacoplar el estado y el cómputo permitió multiplicar la capacidad transaccional preservando la simplicidad del despliegue.
2. **Superación del Desafío de Sesiones Compartidas:** El uso de ASP.NET Core Data Protection persistido sobre volúmenes Docker demostró ser una solución robusta y transparente, eliminando la necesidad de implementar *sticky sessions* (sesiones pegajosas) en Nginx y permitiendo un balanceo Round-Robin verdaderamente elástico.
3. **Optimización con Caché Distribuida:** La integración de Redis bajo el patrón Cache-Aside redujo sustancialmente la contención sobre el motor relacional en la lectura del catálogo de productos, garantizando tiempos de respuesta mínimos (HIT en memoria RAM) y consistencia eventual inmediata mediante la invalidación de caché ante eventos de inserción, edición o anulación de ventas.
4. **Resiliencia Operativa:** Las pruebas de conmutación por error demostraron que el sistema soporta la desconexión abrupta de una o más réplicas de cómputo sin pérdida de sesiones ni interrupción visible para los usuarios concurrentes.

---

## 11. RECOMENDACIONES

1. **Gestión Segura de Secretos en Producción:** Para entornos productivos reales en la nube, se recomienda migrar las variables de entorno sensibles (contraseñas de base de datos, credenciales SMTP) hacia gestores de secretos dedicados como HashiCorp Vault, Azure Key Vault o AWS Secrets Manager.
2. **Almacenamiento de Claves en Almacenes de Alta Disponibilidad:** Aunque el volumen compartido de Data Protection cumple con creces el alcance local de laboratorio, en infraestructuras multinodo distribuidas se aconseja almacenar el anillo de claves directamente en Redis o en una tabla cifrada de SQL Server con cifrado en reposo mediante certificados X.509.
3. **Monitoreo y Métricas Centralizadas:** Integrar herramientas de observabilidad y telemetría distribuida (como Prometheus y Grafana, o el stack ELK) para capturar métricas de latencia de Nginx, estadísticas de HIT/MISS de Redis y tiempos de respuesta de cada réplica en tiempo real.

---

## 12. BIBLIOGRAFÍA

1. **Brown, S. (2018).** *The C4 model for visualising software architecture*. Technical Report, Software Architecture for Developers.
2. **Cockburn, A. (2005).** *Hexagonal Architecture (Ports and Adapters Pattern)*. Alistair.Cockburn.us.
3. **Fowler, M. (2002).** *Patterns of Enterprise Application Architecture*. Addison-Wesley Professional.
4. **Gamma, E., Helm, R., Johnson, R., & Vlissides, J. (1994).** *Design Patterns: Elements of Reusable Object-Oriented Software*. Addison-Wesley.
5. **Martin, R. C. (2017).** *Clean Architecture: A Craftsman's Guide to Software Structure and Design*. Prentice Hall.
6. **Microsoft Corporation (2026).** *ASP.NET Core Data Protection Overview and Key Storage Providers*. Microsoft Learn Documentation.
7. **Redis Ltd. (2026).** *Redis Documentation: Data structures, Cache-Aside Pattern, and Distributed Caching*. Redis.io.
