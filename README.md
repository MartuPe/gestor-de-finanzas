# Gestor de finanzas

App personal de finanzas (ARS + USD, tarjetas, cuotas y fijos) que reemplaza a una planilla de Excel. Backend en .NET y SQL Server; front PWA.

El diseño está en [`docs/arquitectura`](docs/arquitectura):
- [01 · Modelo de datos](docs/arquitectura/01-modelo-de-datos.md)
- [02 · Backend](docs/arquitectura/02-backend.md)

## Requisitos

- **.NET SDK 10.0.402 o posterior** (fijado en [`global.json`](global.json)).
- **Docker Desktop**, para SQL Server local y para los tests de persistencia.
- Un IDE compatible con .NET 10:
  - **Visual Studio 2026** (versión 18) o posterior. Visual Studio 2022 **no** sirve: el SDK de .NET 10 exige MSBuild 18, y con VS 2022 todos los proyectos fallan con *"The SDK 'Microsoft.NET.Sdk' specified could not be found"*.
  - O VS Code con C# Dev Kit, o JetBrains Rider.

## Primera vez

1. Creá `.env` a partir de [`.env.example`](.env.example), con una contraseña propia para `sa`.
2. Guardá la cadena de conexión en los user-secrets de la Api, con la misma contraseña:

   ```bash
   dotnet user-secrets set "ConnectionStrings:Finanzas" "Server=localhost,1433;Database=Finanzas;User Id=sa;Password=<la de .env>;Encrypt=True;TrustServerCertificate=True" --project src/Finanzas.Api
   ```

3. Levantá SQL Server y creá la base:

   ```bash
   docker compose up -d
   dotnet tool restore
   dotnet ef database update --project src/Finanzas.Infrastructure --startup-project src/Finanzas.Api
   ```

Para conectarte con SSMS: servidor `localhost,1433`, usuario `sa` y la contraseña de `.env`.

## Comandos

```bash
dotnet build Finanzas.slnx
dotnet test --solution Finanzas.slnx
```

- Los tests usan xUnit v3 sobre Microsoft Testing Platform, que se activa en `global.json`.
- Los de `Finanzas.Infrastructure.Tests` levantan su propio SQL Server descartable con Testcontainers: necesitan Docker corriendo, pero no tocan tu base local.

Para crear una migración nueva después de cambiar el modelo:

```bash
dotnet ef migrations add <Nombre> --project src/Finanzas.Infrastructure --startup-project src/Finanzas.Api --output-dir Persistence/Migrations
```

## Datos

- El Excel original (`*.xlsx`) está en `.gitignore` a propósito: tiene datos financieros personales y no se versiona.
- Lo mismo pasa con `.env`, que tiene la contraseña local de SQL Server.
