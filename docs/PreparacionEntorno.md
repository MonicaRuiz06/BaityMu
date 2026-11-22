# BaityMu — Entorno de desarrollo

Herramientas y paquetes que quedaron instalados y descargados para empezar a programar.

## Herramientas instaladas

| Herramienta | Versión | Para qué |
|---|---|---|
| Visual Studio Community 2026 | con la carga de trabajo "Desarrollo de ASP.NET y web" | IDE del proyecto |
| SDK de .NET | **10.0.404** (también está el 11.0.100) | Compilar y ejecutar. El taller pide .NET 10 |
| SQL Server LocalDB | instancia `MSSQLLocalDB` (SQL Server 2025) | Base de datos de desarrollo |
| dotnet-ef | **10.0.15** (herramienta global) | Crear migraciones y actualizar la base de datos |
| Git | 2.55 | Control de versiones |
| GitHub CLI (`gh`) | 2.105, sesión iniciada como MonicaRuiz06 | Issues y Project desde la terminal |

### ¿Por qué hay un `global.json`?

Visual Studio 2026 instaló también **.NET 11**, y sin este archivo los proyectos nuevos se crearían en .NET 11. El `global.json` de la raíz fija el SDK **10.0.x** para este repositorio, así todo queda en .NET 10 como pide el taller.

Para comprobarlo, desde la carpeta del repositorio:

```powershell
dotnet --version
```

Debe responder `10.0.404` (o un 10.0.x posterior).

## Paquetes NuGet (ya descargados en el caché local)

| Proyecto | Paquete | Versión |
|---|---|---|
| Infrastructure | Microsoft.EntityFrameworkCore | 10.0.15 |
| Infrastructure | Microsoft.EntityFrameworkCore.SqlServer | 10.0.15 |
| Infrastructure | Microsoft.EntityFrameworkCore.Tools | 10.0.15 |
| Web | Microsoft.EntityFrameworkCore.Design | 10.0.15 |
| Web | AutoMapper | 16.3.0 (la misma versión del ejemplo del profesor) |
| Web | Swashbuckle.AspNetCore | 10.3.1 |
| Tests | NUnit | 4.6.1 |
| Tests | NUnit3TestAdapter | 6.3.1 |
| Tests | NUnit.Analyzers | 4.15.0 |
| Tests | Microsoft.NET.Test.Sdk | 18.12.0 |
| Tests | coverlet.collector | 10.1.1 |
| Tests | Microsoft.EntityFrameworkCore.InMemory | 10.0.15 |

Se verificó con `dotnet list package --vulnerable` que ninguno tiene vulnerabilidades conocidas.

> Hay versiones más nuevas (EF Core 11, AutoMapper 17, NUnit 5), pero son para .NET 11 o cambian de versión mayor. Para el taller conviene quedarse con las de esta tabla.

## Cadena de conexión (LocalDB)

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=BaityMu;Trusted_Connection=True;TrustServerCertificate=True"
}
```

## Pendiente la primera vez que se ejecute la API

- **Certificado HTTPS de desarrollo.** Visual Studio pregunta si quieres confiar en el certificado de ASP.NET Core la primera vez que ejecutas la API. Responde **Sí**. También se puede hacer a mano con `dotnet dev-certs https --trust`.

## Para empezar: Épica 0 (issue #1)

1. La solución está en `BaityMu\BaityMu.slnx`, dentro del repositorio, con los proyectos en `BaityMu\src\` y las pruebas en `BaityMu\tests\`.
2. Seguir las tareas de la [Épica 0](https://github.com/MonicaRuiz06/BaityMu/issues/1) y moverla a **En progreso** en el [tablero](https://github.com/users/MonicaRuiz06/projects/1).
3. Primera migración, desde la carpeta de la solución (`C:\Users\lisak\BaityMu\BaityMu`):

```powershell
dotnet ef migrations add Inicial --project src\BaityMu.Infrastructure --startup-project src\BaityMu.Web
```

```powershell
dotnet ef database update --project src\BaityMu.Infrastructure --startup-project src\BaityMu.Web
```
