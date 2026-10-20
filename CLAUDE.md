# CLAUDE.md — contexto para continuar el proyecto

> Archivo para Claude: se carga solo al abrir una sesión en este repo.
> **Al cerrar una etapa, actualizá §2 (Estado) y §8 (Backlog).** El diseño vive en `docs/arquitectura/`; acá van el estado, el mapa del código y lo aprendido.

## 1. Qué es

App personal de finanzas que reemplaza el Excel `OrganizadorEconómico 2.xlsx` (está en la raíz, **no versionado**). Maneja ARS + USD, tarjetas con cuotas, fijos automáticos y el "Disponible para gastar". Backend **.NET 10 + SQL Server**; front **PWA** (todavía no empezado).

**Fuentes de verdad del diseño** (leelas antes de cambiar el modelo o la arquitectura):
- `docs/arquitectura/01-modelo-de-datos.md`: tablas (§4), **reglas R1–R12 (§5)**, migración del Excel (§7), errores del Excel (§8).
- `docs/arquitectura/02-backend.md`: capas (§2), endpoints planificados (§3.2), seguridad (§5), jobs (§6), persistencia (§7), errores→HTTP (§8), tests (§9), deploy (§10).

## 2. Estado (al 2026-10-19)

| Capa | Estado |
|---|---|
| Domain | ✅ Completo (R1–R12 menos las de consulta). |
| Infrastructure | ✅ EF Core 10, migración `Initial`, repos, lectores SQL, cliente DolarApi. |
| Application | ✅ 6 comandos + 3 consultas + `ExchangeRateService` (ver §4). Faltan los de catálogo (§8). |
| Api | ⏳ Solo `/health`, más el registro de `AddApplication()` y `AddInfrastructure()`. |
| Front | ⏳ No empezado. Hay que diseñarlo con la persona dueña del proyecto antes de codear. |

- **Commits en `main`, sincronizada con `origin`** (repo privado `github.com/MartuPe/gestor-de-finanzas`):
  - `10374b0` dominio
  - `fede281` infraestructura
  - `8614739` application
- **Tests: 103 en verde.** Domain 73, Architecture 3, Infrastructure 27 (7 de persistencia y 20 de casos de uso).
- **Próximo paso acordado:** la Api (endpoints, login con passkeys, casos de uso de catálogo). Antes de arrancar, confirmarlo con Martu.

## 3. Comandos

```bash
dotnet build Finanzas.slnx
dotnet test --solution Finanzas.slnx          # MTP: el runner se activa en global.json; necesita Docker corriendo
docker compose up -d                          # SQL Server local (contenedor finanzas-sqlserver, localhost:1433)
dotnet tool restore                           # dotnet-ef 10.0.13 (dotnet-tools.json)
dotnet ef migrations add <Nombre> --project src/Finanzas.Infrastructure --startup-project src/Finanzas.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/Finanzas.Infrastructure --startup-project src/Finanzas.Api
dotnet ef migrations has-pending-model-changes --project src/Finanzas.Infrastructure --startup-project src/Finanzas.Api
```

- **Secretos locales:**
  - La contraseña de `sa` está en `.env` (ignorado). **Nunca mostrarla en el chat.**
  - La cadena de conexión está en los user-secrets de `Finanzas.Api` (`UserSecretsId 3c103ebc-…`), con la clave `ConnectionStrings:Finanzas`.

## 4. Mapa del código

**Domain (`src/Finanzas.Domain/`):** sin dependencias. Los factories devuelven `Result<T>` y las excepciones quedan solo para errores de programación.
- `Common/`: `Money` (≤2 decimales, no mezcla monedas), `MoneyBag` (montos por moneda), `Period` (yyyy-MM; `Day(31)` se ajusta al largo del mes), `Result`, `Error` (record **no sealed**, a propósito), `ErrorType` (Validation/NotFound/Conflict/BusinessRule/Unavailable), `SequentialGuid`, `CurrencyCodes`, `ExchangeRateSources`.
- `Accounts/`: `Account` (`Create` para cuentas de dinero, `CreateCredit` para crédito) + `CreditLine` (owned; `EstimateDates(period)`).
- `Statements/Statement`: `Cutoff`, `ChangeDates` (R5), `Settle` (R6), `Outstanding`. "Cerrado" no se guarda: se deriva de la fecha.
- `Transactions/`: `Transaction` (factories por tipo; `Confirm`, `EditInstallmentAmounts`, `SetTags` que aplica solo diferencias) + `Installment`, `InstallmentPlan`, `TransferFactory` (transferencias, pagos de resumen y su confirmación).
- `Recurring/RecurringTemplate`: `OccursIn`, `OccurrenceDate`.
- `Ledgers/Ledger`: `TodayAt`, `PeriodAt` (zona horaria de Argentina).
- `Services/`:
  - `InstallmentScheduler`: `Split`, `Plan` (R3/R4), `Reschedule` (R5).
  - `StatementBook` (internal).
  - `RecurringGenerator` (R7) + `GeneratedKey`.
  - `AvailableToSpendCalculator` (R8/R8b).

**Application (`src/Finanzas.Application/`):** handlers con `HandleAsync`, **sin MediatR**.
- `Abstractions/`: `ICurrentUser` (UserId, LedgerId), `IUnitOfWork` (devuelve `Result`), `Repositories.cs`, `Readers.cs` (`IMonthReader`, `IStatementReader`, `ISpendingReader`).
- `Common/`:
  - `ApplicationErrors` y `ValidationError` (con detalle por campo).
  - `ValidationExtensions`: `IsAmount`, `IsCurrencyCode`, `IsPeriod`, …
  - `Lookups.GetOwnedAsync`: lo que es de otro libro responde NotFound.
  - `Idempotency.CheckAsync`, `Registration<T>(Value, Created)`, `MoneyBagExtensions` (`ToAmounts`, `ToBase`).
- Casos de uso (una carpeta por caso, con Command/Query + Validator + Response + Handler):
  - `Expenses/RegisterExpense`, `Incomes/RegisterIncome`, `Transfers/RegisterTransfer`.
  - `Statements/PayStatement`, `Transactions/ConfirmPlanned`, `Recurring/GenerateRecurring`.
  - `Months/GetMonthSummary`, `Reports/GetSpending`, `CreditLines/GetDebtSchedule`.
- `ExchangeRates/ExchangeRateService`: R9 + actualización si la cotización guardada tiene más de 30 min. Si la fuente falla, usa la última conocida.
- `DependencyInjection.AddApplication()`: registra los handlers explícitamente, los validadores y la cultura `es` de FluentValidation.

**Infrastructure (`src/Finanzas.Infrastructure/`)**
- `Persistence/FinanzasDbContext` y `Configurations/`: una por tabla, con los CHECK/UNIQUE/índices de 01 §4.
- `Persistence/Conventions/`:
  - `EnumNames` + `SnakeCaseEnumConverter`: enums en snake_case.
  - `PeriodConverter` (como `date`) y `UtcDateTimeOffsetConverter`.
  - `AuditColumns`: shadow props + filtro con nombre `SoftDelete`.
  - `Sql` (helpers para CHECK).
- `Persistence/Interceptors/AuditInterceptor`: CreatedAt/UpdatedAt y borrado lógico. Conserva las filas sin auditoría (etiquetas, miembros) de lo que se borra lógicamente.
- `Persistence/Repositories/`: repos y `UnitOfWork` (concurrencia → `ConcurrencyConflict`; error SQL 2601/2627 → `Duplicate`).
- `Persistence/Readers/`: `MonthReader`, `StatementReader`, `SpendingReader` (R10).
- `Persistence/Records/`: `CurrencyRecord`, `ExchangeRateRecord` (datos globales, no son de dominio).
- `ExchangeRates/`:
  - `DolarApiClient` (`GET v1/dolares`; descarta casas desconocidas o datos incompletos).
  - `ExchangeRateStore`: usa su propio scope de DbContext, para no guardar de paso cambios del pedido.
- `DependencyInjection.AddInfrastructure(config)`.

**Tests (`tests/`)**: nombres en español con guiones bajos, casos sacados del Excel.
- `Finanzas.Domain.Tests`: `Fixtures.cs` con cuentas tipo Excel.
- `Finanzas.Architecture.Tests`: NetArchTest, controla las referencias entre capas.
- `Finanzas.Infrastructure.Tests`:
  - `SqlServerFixture`: Testcontainers + `[assembly: AssemblyFixture]`.
  - `TestLedger`: libro con todas las cuentas y categorías.
  - `UseCases/App.cs`: arma la app como la Api, con `FakeTimeProvider` en el **18/10/2026 15:00 AR** y `FakeRateSource`.

## 5. Convenciones (respetarlas)

- **Idioma:** identificadores en inglés; comentarios, XML docs, mensajes de error y nombres de tests en español rioplatense. Los errores llevan `code` estable (`area.motivo`).
- **Un tipo por archivo.** Excepciones: los contratos agrupados en `Abstractions/Repositories.cs` y `Readers.cs`, y algunos records de respuesta chicos junto a su respuesta principal.
- **Dinero:**
  - Montos con signo en los movimientos.
  - Los comandos reciben montos en positivo; los validadores impiden que `Money` reciba algo inválido.
  - La conversión a moneda base ocurre solo al responder (`ToBase`, R8b).
- **Períodos en EF:** no comparar `Period` con `<` o `>` en LINQ (es un tipo convertido). Usar `DueDate` o fechas; `==` sí funciona.
- **Versiones de paquetes:** todas en `Directory.Packages.props` (CPM con transitive pinning). Build con `TreatWarningsAsErrors`.
- **Docs:** cada decisión nueva se refleja en `docs/arquitectura/*.md`.

## 6. Decisiones ya tomadas (no reabrir sin que lo pida Martu)

| Tema | Decisión |
|---|---|
| Tarjetas | El resumen impacta en el **mes de vencimiento**. Las tarjetas son cuentas `credit` no líquidas. Toda compra a crédito tiene ≥1 cuota. |
| Moneda | Monto nativo + historial de cotizaciones. Referencia: **blue, precio de venta**. Fuente: DolarApi; histórico: ArgentinaDatos. |
| Fijos | Se generan como `planned` y se confirman; idempotentes por `UNIQUE(template, period, account)`, que incluye los borrados. |
| Multiusuario | Tenant `Ledger` + `LedgerMembers`. El filtro global por libro y la FK a `Users` se agregan con Identity. |
| Gasto por categoría | Por defecto, cada cuota en el mes en que vence (`SpendingMode.Due`). |
| Etiquetas | Sí: una dimensión aparte de la categoría ("Lu", viajes, gym). |
| Front | PWA servida por la misma Api (mismo origen, cookie HttpOnly). |
| Login | Passkeys (Identity .NET 10) + mail y contraseña como respaldo. Registro cerrado. |
| Hosting | Azure Container Apps + Azure SQL serverless gratis; imagen en ghcr.io. Definir el **dominio antes** de registrar passkeys. |
| Dev DB | SQL Server 2025 en Docker. La licencia de SQL Server Developer (EULA) la aceptó Martu explícitamente. |

## 7. Lecciones y trampas ya resueltas

- **Visual Studio 2022 no carga .NET 10** (necesita MSBuild 18). Martu usa VS 2026.
- **xUnit v3 4.x corre sobre MTP.** No agregar `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio` ni coverlet.
- **Analizadores de xUnit:** pasar `TestContext.Current.CancellationToken` y usar `Assert.Single`/`Assert.Empty` en lugar de comparar `Count` con 1 o 0.
- **`System.Security.Cryptography.Pkcs` está fijado en 10.0.13.** Lo trae `Microsoft.Data.SqlClient` en una versión vulnerable, y la auditoría de NuGet corta el build.
- **`dotnet ef migrations remove` necesita la base levantada.** Si la migración no se aplicó en ningún lado, borrar los 3 archivos a mano.
- **`migrations script --no-build`** justo después de `add` no ve la migración nueva: compilar antes.
- **En consultas LINQ, `from` es palabra reservada:** usar una variable local.
- **`dotnet user-secrets init`** desordena el csproj: hay que reformatearlo.
- **EF Core:**
  - No hay FK hacia tipos owned, así que `Statements` apunta a `Accounts`.
  - Las cuotas y `_tags` (navegación solo por campo) tienen `AutoInclude`.
  - Borrar y volver a crear la misma clave en un mismo guardado choca: por eso `SetTags` aplica solo diferencias.
- **Las cotizaciones son globales en la base de tests:** cada test usa fechas propias. Solo `MonthSummaryTests` usa el 18/10/2026.
- **Los días de cierre y vencimiento de los fixtures son supuestos de test,** no datos reales: Visa y Mastercard 28/5, Patagonia 28/10, Mercado Crédito vence el 10.
- **`gh` no está instalado:** git usa HTTPS con Git Credential Manager.
- **Hay otras instancias de SQL Server locales** (MARTU, SQLEXPRESS) que **no** se usan para este proyecto.

## 8. Backlog (en orden)

1. **Api:**
   - Minimal APIs según 02 §3.2.
   - Mapeo `ErrorType` → HTTP: 400 con `Fields`, 404, 409, 422 y 503 para Unavailable.
   - OpenAPI + Scalar.
   - `ICurrentUser` a partir de la sesión.
   - `GET /months/{p}` llama a `GenerateRecurring` y después a `GetMonthSummary`.
2. **Identity + passkeys:**
   - `AppUser : IdentityUser<Guid>` → tabla `Users`, más la FK `LedgerMembers.UserId`.
   - Filtro global `Ledger`.
   - Claves de Data Protection en la base, rate limiting y registro cerrado.
   - Revisar el test de arquitectura si Infrastructure pasa a usar `Microsoft.AspNetCore.Identity.EntityFrameworkCore`.
3. **Catálogo:** crear y editar libro, cuentas (incluido crédito), categorías, etiquetas, plantillas de fijos y saldo inicial (`opening`).
4. **Reglas con el dominio listo pero sin caso de uso:**
   - `UpdateStatementDates` (R5: `Statement.ChangeDates` + `InstallmentScheduler.Reschedule`).
   - `EditInstallments`.
   - `ReconcileAccount` (`Transaction.CreateReconciliation`).
   - Registrar devoluciones y rendimientos.
   - `GetCashflow`.
5. **Jobs:** el diario (cotizaciones + fijos) y el backfill de ArgentinaDatos desde 2025-09-01.
6. **Importador del Excel** (01 §7 y §8) y el test de oro contra las celdas `K` de cada mes.
7. **Front PWA:** primero debatir el diseño (Martu no quiere un "diseño típico de IA").
8. **CI/CD** con GitHub Actions y deploy a Azure (02 §10).

**Datos pendientes de Martu:**
- Nombre de la app (hoy se usa `Finanzas`).
- Dominio definitivo.
- Dónde están los dólares.
- Día de cobro del sueldo.
- Días reales de cierre y vencimiento de cada tarjeta.

## 9. Cómo trabajar con Martu

- **Escribir en español rioplatense.** Referirse a Martu por su nombre, sin asumir género.
- **Diseño:** proponer algo concreto (diagrama, tabla o código) con una recomendación, y dejar pocas decisiones numeradas. Usar ejemplos reales del Excel ("Perfume Lu 2/3", "celda K10"): convencen más que la teoría.
- **Pedir OK antes de cada commit y antes de cada push**, aunque se haya aprobado uno antes.
  - Los mensajes de commit van en español, terminan con `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>` y van directo a `main`.
  - Antes de publicar algo, verificar que no se cuelen secretos.
- **No versionar nunca** el Excel ni `.env`. **No mostrar** la contraseña de `sa`.
- **Pedir permiso antes de** aceptar licencias, instalar software o crear recursos externos.
- **Al terminar cada etapa:** build y tests en verde, actualizar docs y `CLAUDE.md` (§2 y §8), y proponer el commit.
