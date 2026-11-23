<p align="center"><img src="docs/img/logo-baitymu.png" alt="Logo de BaityMu" width="220"></p>

# BaityMu

API Web en .NET con arquitectura en capas para la granja lechera de Baity. Cubre la producción de leche, la transformación en queso y yogurt, el inventario por lotes con salida FIFO, las ventas y los reportes.

Taller final de **Lenguajes de Programación**, Universidad Santiago de Cali.
Docente: Camilo Ernesto Charry Caicedo · Estudiante: Monica Alexandra Ruiz Martinez.

## Estado

**Épica 0 terminada: base del proyecto.**
- La solución tiene las 4 capas y un proyecto de pruebas.
- Hay persistencia con EF Core y SQL Server, Swagger, un manejador global de errores y pruebas con NUnit.
- Lo siguiente son las historias de usuario, empezando por la [HU-01 Registrar una vaca](https://github.com/MonicaRuiz06/BaityMu/issues/3).

El trabajo se organiza en el GitHub Project **[BaityMu](https://github.com/users/MonicaRuiz06/projects/1)**, con 7 épicas y 21 historias de usuario como [issues](https://github.com/MonicaRuiz06/BaityMu/issues).

## Tecnologías

| Tecnología | Versión |
|---|---|
| .NET · ASP.NET Core Web API · C# | 10 (SDK 10.0.404, fijado con `global.json`) |
| Entity Framework Core (SqlServer, Tools, Design, InMemory) | 10.0.15 |
| SQL Server LocalDB | 2025 (instancia `MSSQLLocalDB`) |
| AutoMapper | 16.3.0 |
| Swashbuckle.AspNetCore (Swagger/OpenAPI) | 10.3.1 |
| NUnit | 4.x |

## Arquitectura

```text
BaityMu.Web → BaityMu.Services → BaityMu.Domain
     └──────→ BaityMu.Infrastructure → BaityMu.Domain
```

- **Domain**: entidades, enums, excepciones de dominio e interfaces (`IRepository<T>`, `IUnitOfWork`, `IClock`). No depende de ninguna otra capa.
- **Infrastructure**: `BaityMuDbContext` (EF Core + SQL Server), repositorios, `UnitOfWork` y `SystemClock`. Implementa las interfaces de Domain.
- **Services**: casos de uso (servicios de negocio) y DTOs.
- **Web**: controllers, `Program.cs` (raíz de composición), Swagger, AutoMapper, CORS y el manejador global de errores. Referencia a Infrastructure solo para registrar dependencias.
- **Tests**: pruebas unitarias con NUnit, `FakeClock` y base de datos EF Core InMemory.

```text
BaityMu/                         ← repositorio
├── docs/                        documentación del proyecto
├── global.json                  fija el SDK de .NET 10
└── BaityMu/
    ├── BaityMu.slnx             solución
    ├── src/
    │   ├── BaityMu.Domain/
    │   ├── BaityMu.Infrastructure/
    │   ├── BaityMu.Services/
    │   └── BaityMu.Web/
    └── tests/
        └── BaityMu.Tests/
```

## Requisitos

- **Visual Studio 2026** con la carga de trabajo **"Desarrollo de ASP.NET y web"** (incluye SQL Server LocalDB), o el **SDK de .NET 10** por línea de comandos.
- **SQL Server LocalDB** con la instancia `MSSQLLocalDB`. Para comprobarlo: `sqllocaldb info`.
- **dotnet-ef 10.0.15**, solo para crear migraciones:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.15
```

## Cómo ejecutar la API

### Desde Visual Studio

1. Abre `BaityMu/BaityMu.slnx`.
2. Haz clic derecho sobre **BaityMu.Web** y elige **Establecer como proyecto de inicio**.
3. Elige el perfil **http** y presiona **▶** (F5).
4. Se abre el navegador en **Swagger**: http://localhost:5251/swagger

### Desde la terminal

Desde la carpeta `BaityMu` (donde está el `.slnx`):

```powershell
dotnet run --project src\BaityMu.Web --launch-profile http
```

Luego abre http://localhost:5251/swagger.

### Base de datos

La cadena de conexión está en `src/BaityMu.Web/appsettings.json`:

```text
Server=(localdb)\MSSQLLocalDB;Database=BaityMu;Trusted_Connection=True;TrustServerCertificate=True
```

**Al arrancar, la API aplica sola las migraciones pendientes** y crea la base de datos `BaityMu` en LocalDB si no existe. No hace falta correr `dotnet ef database update` a mano.

Los valores iniciales de la granja (n, l, rendimiento, vida útil y precio de cada producto) están en la sección `GranjaInicial` del mismo archivo, y no en el código (RNF-09). **La primera vez que arranca**, la API crea la granja con sus n vacas (BaityCow, BaityCow 2... BaityCow 10). Para volver a empezar desde cero, borra la base con `dotnet ef database drop -f --project src\BaityMu.Infrastructure --startup-project src\BaityMu.Web`.

### Crear una migración

Después de agregar o cambiar entidades, desde la carpeta `BaityMu`:

```powershell
dotnet ef migrations add NombreDeLaMigracion --project src\BaityMu.Infrastructure --startup-project src\BaityMu.Web --output-dir Data\Migrations
```

Para aplicarla sin ejecutar la API:

```powershell
dotnet ef database update --project src\BaityMu.Infrastructure --startup-project src\BaityMu.Web
```

## Cómo probar

### Pruebas unitarias (NUnit)

- **Visual Studio:** abre **Prueba → Explorador de pruebas** y elige **Ejecutar todas**.
- **Terminal:** desde la carpeta `BaityMu`:

```powershell
dotnet test
```

Las pruebas no necesitan SQL Server. Usan una base EF Core **InMemory** nueva en cada prueba y un `FakeClock` para simular fechas (RN-10).

### Colección de peticiones (`.http`)

`src/BaityMu.Web/BaityMu.Web.http` tiene las peticiones listas para enviar desde Visual Studio, con la API en ejecución. Cada historia agrega las suyas. Al final quedará el escenario de referencia del taller (días 1 a 17).

### Endpoints disponibles

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/` | Bienvenida de la API (¡Muuu!) |
| GET | `/api/vacas?incluirRetiradas=` | Lista las vacas con los litros que ha dado cada una (HU-02) · 200 |
| GET | `/api/vacas/{id}` | Consulta una vaca con su producción total y la de cada día (HU-03) · 200, 404 |
| DELETE | `/api/vacas/{id}` | Retira una vaca: queda inactiva y conserva su historial (HU-04) · 204, 400, 404, 409 |
| POST | `/api/vacas` | Registra una vaca (HU-01). Sin nombre, se llama BaityCow, BaityCow 2... · 201, 400, 404, 409 |
| GET | `/api/diagnostico/errores/{codigo}` | **Solo en desarrollo.** Lanza un error de ejemplo (400, 404, 409 o 500) para ver la respuesta ProblemDetails |

Los endpoints del negocio (`/api/granja`, `/api/vacas`, `/api/produccion`, `/api/ventas`, `/api/reportes/...`) se agregan con cada historia de usuario.

### Errores

Todos los errores responden en formato **ProblemDetails** (`application/problem+json`):

| Excepción del dominio | Código HTTP |
|---|---|
| `ReglaDeNegocioException` | 400 Bad Request |
| `RecursoNoEncontradoException` | 404 Not Found |
| `ConflictoException` | 409 Conflict |
| Cualquier otro error | 500 (sin detalles internos) |

## Documentación

| Documento | Contenido |
|---|---|
| [Historias de usuario](docs/HistoriasDeUsuario.md) | Problema, reglas de negocio, requisitos funcionales y no funcionales, roles Baity, 21 historias con criterios de aceptación y tareas de desarrollo |
| [Base de datos](docs/BaseDeDatos.md) | Modelo entidad-relación en SQL Server, tablas, restricciones, índices y decisiones de diseño |
| [Preparación del entorno](docs/PreparacionEntorno.md) | Herramientas, versiones y paquetes instalados |
| [Convención de commits](docs/ConvencionCommits.md) | Conventional Commits: tipos, alcances y cómo enlazar los issues |
| [Documentación en PDF](docs/BaityMu-Documentacion.pdf) | Análisis y diseño en un solo documento para entregar |
