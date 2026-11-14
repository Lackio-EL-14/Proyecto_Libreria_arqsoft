# Contratos del sistema

> Publicados en US-113 a partir de la sección 6 de `docs/Planning_Sprint_Microservicios.md`.
> **Cambiar un contrato se avisa en el grupo del equipo antes de hacer el PR.**

| Archivo | Contenido |
|---|---|
| [urls-y-rutas.md](urls-y-rutas.md) | URL única del Front, rutas del gateway (`yarp.g1.json` / `yarp.g2.json`), puertos y cadenas de conexión |
| [rest-por-servicio.md](rest-por-servicio.md) | Endpoints de cada MS: los que existen (contrato estable) y los nuevos de este sprint |
| [rest-errores-result.md](rest-errores-result.md) | Convenciones REST, `Result` y traducción a códigos HTTP |
| [jwt.md](jwt.md) | Algoritmo, claves, claims, duración y respuestas 401/403 |
| [usuario-actual.md](usuario-actual.md) | Puerto de usuario actual, adaptadores y carpeta `Infrastructure/Seguridad/` |
| [front.md](front.md) | Cómo consume el Front a cada MS: clientes tipados, DTOs y módulos de `Program.cs` |
| [datos-semilla.md](datos-semilla.md) | GUID fijos de usuarios, clientes, categorías, marcas, productos y ventas (US-111) |
