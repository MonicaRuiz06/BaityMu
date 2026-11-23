<p align="center"><img src="docs/img/logo-baitymu.png" alt="Logo de BaityMu" width="220"></p>

# BaityMu

API Web en .NET con arquitectura en capas para la granja lechera de Baity. Cubre la producción de leche, la transformación en queso y yogurt, el inventario por lotes con salida FIFO, las ventas y los reportes. Incluye un **front en Blazor WebAssembly** para usarla desde el navegador.

Taller final de **Lenguajes de Programación**, Universidad Santiago de Cali.
Docente: Camilo Ernesto Charry Caicedo · Estudiante: Monica Alexandra Ruiz Martinez.

## Estado

**Épicas 0 a 3 terminadas** (HU-01 a HU-10): granja y vacas, producción diaria, transformación en lotes e inventario, con 93 pruebas NUnit en verde.
- Lo siguiente es la HU-11 (procesar vencimientos) y la venta FIFO (HU-13).
- El front cubre todo lo que ya existe en la API; ventas y reportes se agregan cuando estén sus historias.

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
| Blazor WebAssembly (front) | 10.0.15 |

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
- **Client** (front): Blazor WebAssembly. Corre en el navegador y solo conoce la API por HTTP (no referencia ninguna capa), por eso la API tiene CORS habilitado.

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
    │   ├── BaityMu.Web/         API
    │   └── BaityMu.Client/      front (Blazor WebAssembly)
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

### API + front juntos

- **Visual Studio:** en la barra de arriba, junto al botón ▶, elige el perfil de inicio **"API + Front"** y presiona F5. Se abren Swagger (http://localhost:5251/swagger) y el front (http://localhost:5282).
- **Terminal:** en dos terminales, desde la carpeta `BaityMu`:

```powershell
dotnet run --project src\BaityMu.Web --launch-profile http
```

```powershell
dotnet run --project src\BaityMu.Client --launch-profile http
```

La dirección de la API que usa el front está en `src/BaityMu.Client/wwwroot/appsettings.json` (`ApiBaseUrl`).

### El front

| Pantalla | Rol | Qué hace |
|---|---|---|
| Inicio | Todos | n, l, n × l, la leche del día y el inventario vendible |
| Vacas | BaityGanadero | Listar (activas o todas), registrar y retirar vacas; ficha con su historial |
| Producción | BaityGanadero | Registrar el ordeño (n × l o litros por vaca) y ver lo transformado y lo disponible |
| Transformación | BaityQuesero | Convertir leche en queso o yogurt, con vista previa de la cantidad y el vencimiento |
| Inventario | BaityBodeguero | Resumen por producto y lotes en orden FIFO con su estado |
| Configuración | BaityAdministrador | Cambiar l y el rendimiento, la vida útil y el precio de cada producto |

Arriba está el **día de trabajo**: la fecha que usan los formularios y las consultas. Con el botón **"Escenario · 1 dic"** se salta al día 1 del escenario de referencia, y moviendo el día (por ejemplo al 17 de diciembre) se ve cómo los lotes pasan a *Vencido*. El navegador recuerda el día elegido.

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
| GET | `/api/granja` | Configuración de la granja: n, l, n × l y parámetros de queso y yogurt (HU-05) · 200 |
| PUT | `/api/granja` | Actualiza l y los parámetros de los productos; si algo es inválido no cambia nada (HU-06) · 200, 400 |
| GET | `/api/vacas?incluirRetiradas=` | Lista las vacas con los litros que ha dado cada una (HU-02) · 200 |
| GET | `/api/vacas/{id}` | Consulta una vaca con su producción total y la de cada día (HU-03) · 200, 404 |
| DELETE | `/api/vacas/{id}` | Retira una vaca: queda inactiva y conserva su historial (HU-04) · 204, 400, 404, 409 |
| POST | `/api/vacas` | Registra una vaca (HU-01). Sin nombre, se llama BaityCow, BaityCow 2... · 201, 400, 404, 409 |
| POST | `/api/produccion` | Registra la producción de un día: n × l, o litros por vaca; una sola vez por fecha (HU-07) · 201, 400, 404, 409 |
| GET | `/api/produccion/{fecha}` | Producción de un día: cada vaca, total, transformado y disponible (HU-08) · 200, 404 |
| POST | `/api/transformaciones` | Transforma leche de un día en un lote de queso o yogurt; nunca más de lo producido (HU-09) · 201, 400, 409 |
| GET | `/api/inventario?tipoProducto=&desde=&hasta=&fecha=` | Lotes con su estado (Disponible, Agotado, Vencido o Dañado) en orden FIFO (HU-10) · 200, 400 |
| GET | `/api/inventario/lotes/{id}` | Un lote con su estado (HU-10) · 200, 404 |
| POST | `/api/inventario/procesar-vencimientos?fecha=` | Lo vencido pasa a dañado; repetirlo no duplica (HU-11) · 200 |
| GET | `/api/inventario/resumen?fecha=` | Por producto: vendible, vencido sin procesar y dañado (HU-10) · 200 |
| POST | `/api/ventas` | Vende queso o yogurt en orden FIFO, con el detalle de los lotes; 409 si no alcanza (HU-13) · 201, 400, 409 |
| GET | `/api/ventas?desde=&hasta=&tipo=` | Lista las ventas con el detalle de los lotes (HU-14) · 200, 400 |
| GET | `/api/ventas/{id}` | Una venta con los lotes de donde salió (HU-14) · 200, 404 |
| GET | `/api/reportes/ventas?desde=&hasta=&tipo=` | Reporte de ventas con total por producto y total general (HU-15) · 200, 400 |
| GET | `/api/reportes/leche-diaria?desde=&hasta=` | Litros producidos, transformados y no aprovechados de cada día (HU-16) · 200, 400 |
| GET | `/api/reportes/producto-diario?desde=&hasta=` | Queso y yogurt elaborados cada día, con unidad y litros usados (HU-17) · 200, 400 |
| GET | `/api/reportes/producto-total?fecha=` | Balance de cada producto: producido, vendido, disponible y dañado (HU-18) · 200 |
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
