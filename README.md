# SAPFIAI - Plantilla Tecnica Clean Architecture

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download)

## Descripcion

SAPFIAI es una solucion empresarial basada en Clean Architecture para APIs ASP.NET Core sobre .NET 10. La separacion por capas desacopla dominio, aplicacion, infraestructura y presentacion para facilitar mantenimiento, pruebas y evolucion.

## Stack Actual

- .NET 10 (TargetFramework `net10.0` centralizado en `Directory.Build.props`)
- ASP.NET Core 10 + Entity Framework Core 10
- CQRS con MediatR
- Validacion con FluentValidation
- Mapeo con AutoMapper
- Central Package Management con `Directory.Packages.props`
- Pruebas con NUnit, FluentAssertions, Moq, Respawn y Testcontainers

## Requisitos

- .NET SDK 10
- SQL Server (local o remoto)
- Visual Studio 2022+ o VS Code
- Git

## Ejecucion Local

1. Restaurar paquetes:

```bash
dotnet restore
```

2. Compilar la solucion:

```bash
dotnet build SAPFIAI.sln -c Release
```

3. Ejecutar la API:

```bash
dotnet run --project src/Web/Web.csproj
```

## Estructura Real del Proyecto

```text
SAPFIAI/
|- .github/
|- docs/
|- docsPublish/
|- packages/
|  \- ui/
|- publish/
|- scripts/
|- src/
|  |- Application/
|  |  |- Common/
|  |  \- Users/
|  |- Domain/
|  |  |- Common/
|  |  |- Constants/
|  |  |- Entities/
|  |  |- Enums/
|  |  |- Exceptions/
|  |  \- ValueObjects/
|  |- Infrastructure/
|  |  |- Authorization/
|  |  |- BackgroundJobs/
|  |  |- Data/
|  |  |- Identity/
|  |  \- Services/
|  \- Web/
|     |- Endpoints/
|     |- Infrastructure/
|     |- Middleware/
|     |- Pages/
|     |- Properties/
|     |- Services/
|     \- wwwroot/
|- tests/
|  |- Application.FunctionalTests/
|  |- Application.UnitTests/
|  |- Domain.UnitTests/
|  \- Infrastructure.IntegrationTests/
|- Directory.Build.props
|- Directory.Packages.props
|- global.json
|- NuGet.Config
\- SAPFIAI.sln
```

## Proyectos Principales

- `src/Domain`: entidades, value objects, enums, reglas de dominio.
- `src/Application`: casos de uso, contratos, handlers y logica de aplicacion.
- `src/Infrastructure`: persistencia, servicios externos, identidad y autorizacion.
- `src/Web`: host de la API, endpoints, middleware, configuracion y assets web.

## Pruebas

Ejecutar todas las pruebas:

```bash
dotnet test SAPFIAI.sln -c Release
```

Tipos de pruebas presentes en la solucion:

- Unitarias de dominio y aplicacion.
- Funcionales de aplicacion.
- Integracion de infraestructura.

## CI (GitHub Actions)

El workflow de CI activo esta en `.github/workflows/ci.yml` y ejecuta:

- Restore
- Build en `Release`
- Test con recoleccion de cobertura (`XPlat Code Coverage`)
- Publicacion de artefactos de resultados de prueba

## Notas de Seguridad y Build

- La solucion compila con `TreatWarningsAsErrors=true`.
- `NU1903` esta configurado temporalmente para no romper build (`WarningsNotAsErrors`) y seguir visible como warning.



