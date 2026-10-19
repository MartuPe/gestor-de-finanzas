# Gestor de finanzas

App personal de finanzas (ARS + USD, tarjetas, cuotas y fijos) que reemplaza a una planilla de Excel. Backend en .NET y SQL Server; front PWA.

El diseño está en [`docs/arquitectura`](docs/arquitectura):
- [01 · Modelo de datos](docs/arquitectura/01-modelo-de-datos.md)
- [02 · Backend](docs/arquitectura/02-backend.md)

## Requisitos

- **.NET SDK 10.0.402 o posterior** (fijado en [`global.json`](global.json)).
- Un IDE compatible con .NET 10:
  - **Visual Studio 2026** (versión 18) o posterior. Visual Studio 2022 **no** sirve: el SDK de .NET 10 exige MSBuild 18, y con VS 2022 todos los proyectos fallan con *"The SDK 'Microsoft.NET.Sdk' specified could not be found"*.
  - O VS Code con C# Dev Kit, o JetBrains Rider.

## Comandos

```bash
dotnet build Finanzas.slnx
dotnet test --solution Finanzas.slnx
```

Los tests usan xUnit v3 sobre Microsoft Testing Platform, que se activa en `global.json`.

## Datos

El Excel original (`*.xlsx`) está en `.gitignore` a propósito: tiene datos financieros personales y no se versiona.
