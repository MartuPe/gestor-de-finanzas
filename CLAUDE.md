# CLAUDE.md — contexto para continuar el proyecto

> Archivo para Claude: se carga solo al abrir una sesión en este repo.
> **Al cerrar una etapa, actualizá §2 (Estado) y §8 (Backlog).** El diseño vive en `docs/arquitectura/`; acá van el estado, el mapa del código y lo aprendido.

## 1. Qué es

App personal de finanzas que reemplaza el Excel `OrganizadorEconómico 2.xlsx` (está en la raíz, **no versionado**). Maneja ARS + USD, tarjetas con cuotas, fijos automáticos y el "Disponible para gastar". Backend **.NET 10 + SQL Server**; front **PWA** (todavía no empezado).

**Fuentes de verdad del diseño** (leelas antes de cambiar el modelo o la arquitectura):
- `docs/arquitectura/01-modelo-de-datos.md`: tablas (§4), **reglas R1–R12 (§5)**, migración del Excel (§7), errores del Excel (§8).
- `docs/arquitectura/02-backend.md`: capas (§2), endpoints planificados (§3.2), seguridad (§5), jobs (§6), persistencia (§7), errores→HTTP (§8), tests (§9), deploy (§10).

## 2. Estado (al 2026-10-20)

| Capa | Estado |
|---|---|
| Domain | ✅ Completo (R1–R12 menos las de consulta). |
| Infrastructure | ✅ EF Core 10, migraciones `Initial` e `Identity` (las dos aplicadas en la base local), repos, lectores SQL, cliente DolarApi, Identity. |
| Application | ✅ Movimientos, consultas, catálogo (libro, cuentas, categorías, etiquetas, fijos) y cotizaciones (ver §4). |
| Api | ✅ Endpoints de 02 §3.2 (los marcados ✓), login con contraseña y passkeys, registro cerrado, filtro global por libro. |
| Front | ⏳ No empezado. Hay que diseñarlo con la persona dueña del proyecto antes de codear. |

- **Commits en `main`, sincronizada con `origin`** (repo privado `github.com/MartuPe/gestor-de-finanzas`):
  - `10374b0` dominio
  - `fede281` infraestructura
  - `8614739` application
  - `13bc060` CLAUDE.md
  - `d11e652` api (catálogo, Identity, endpoints)
- **Tests: 124 en verde** (~30 s). Domain 73, Architecture 3, Infrastructure 36 (8 de persistencia y 28 de casos de uso), Api 12.
- **Próximo paso:** el front PWA (backlog §8.1), empezando por debatir el diseño. Antes de arrancar, confirmarlo con Martu.

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

**Api (`src/Finanzas.Api/`)**: los endpoints son finos: llaman al handler y devuelven `ToHttp()` / `ToCreatedHttp()`.
- `Program.cs`: `AddApplication` + `AddInfrastructure` + `AddAppAuthentication` + `AddAppHttp`. Bajo `/api`, solo `/auth/*` es público; el resto exige la política `Ledger` (sesión con claim `ledger_id`). OpenAPI (`/openapi/v1.json`) y Scalar (`/scalar`) fuera de producción.
- `Auth/`:
  - `CurrentUser`: `ICurrentUser` + `ILedgerScope` a partir de los claims.
  - `AuthSetup`: cookie `finanzas.session` (401/403 sin redirigir), `AddIdentityPersistence().AddSignInManager()`, rate limit `auth`.
  - `AuthEndpoints`: setup, login, logout, me, passkeys (alta, listado, baja, ingreso).
  - `AuthContracts`: cuerpos y respuestas de sesión.
- `Endpoints/`: `CatalogEndpoints`, `MovementEndpoints`, `ReportEndpoints`, y `EndpointRequests` (cuerpos de los pedidos con Id en la ruta).
- `Http/`:
  - `ResultHttpExtensions`: `ErrorType` → 400/404/409/422/503 como ProblemDetails con `code`; los campos de validación en camelCase. `Returns<T>()` / `ReturnsCreated<T>()` documentan el cuerpo en OpenAPI.
  - `QueryValues`: enums y períodos en ruta y query.
  - `PeriodJsonConverter` y `HttpSetup` (enums snake_case, números estrictos, OpenAPI con `Period` en línea como texto `yyyy-MM`).

**Identity (en Infrastructure, `Identity/`)**: `AppUser` (tabla `Users`), `AppClaims`, `AppClaimsPrincipalFactory` (agrega `ledger_id`), `OwnerSetup` (usuario + libro en una transacción), `AuthErrors`, `LocalizedIdentityErrorDescriber` (mensajes en castellano). `AddIdentityPersistence()` registra todo eso; el esquema 3 lo fija `AddInfrastructure()`. `FinanzasDbContext` hereda de `IdentityUserContext<AppUser, Guid>` e implementa `IDataProtectionKeyContext`.

**Tests (`tests/`)**: nombres en español con guiones bajos, casos sacados del Excel.
- `Finanzas.Domain.Tests`: `Fixtures.cs` con cuentas tipo Excel.
- `Finanzas.Architecture.Tests`: NetArchTest, controla las referencias entre capas.
- `Finanzas.Infrastructure.Tests`:
  - `SqlServerFixture`: Testcontainers + `[assembly: AssemblyFixture]`.
  - `TestLedger`: libro con todas las cuentas y categorías.
  - `UseCases/App.cs`: arma la app como la Api, con `FakeTimeProvider` en el **18/10/2026 15:00 AR** y `FakeRateSource`.
  - `TestUsers`: los dueños de los libros tienen que existir en `Users` (FK de `LedgerMembers`).
- `Finanzas.Api.IntegrationTests`: `TestApi` levanta la Api con `WebApplicationFactory` en el entorno `Testing` (sin user-secrets), sobre **una base nueva por test** (el registro está cerrado; `SqlServerFixture.NewDatabaseAsync` la migra antes de arrancar), cliente HTTPS con cookies, reloj fijo y sin DolarApi. Las respuestas se leen como `JsonElement`: prueban el JSON tal cual viaja. Los helpers fallan mostrando el cuerpo del error.

## 5. Convenciones (respetarlas)

- **Idioma:** identificadores en inglés; comentarios, XML docs, mensajes de error y nombres de tests en español rioplatense. Los errores llevan `code` estable (`area.motivo`).
- **Un tipo por archivo.** Excepciones: los contratos agrupados en `Abstractions/Repositories.cs` y `Readers.cs`, los cuerpos HTTP en `Api/Auth/AuthContracts.cs` y `Api/Endpoints/EndpointRequests.cs`, y algunos records de respuesta chicos junto a su respuesta principal.
- **Casos de uso nuevos:** carpeta con Command/Query + Validator + Response + Handler; registrar el handler en `AddApplication()`, el endpoint en el `*Endpoints.cs` del área (con `.Returns<T>()` o `.ReturnsCreated<T>()`) y un test en `UseCases/` o en la Api.
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
- **Docker Desktop no arranca desde las herramientas de Claude** (`Start-Process` y `docker desktop start` no alcanzan después de reiniciar la PC). Pedirle a Martu que lo abra. Sin Docker no corren los tests de Infrastructure ni de la Api.
- **`dotnet ef` levanta la Api en Development**, que valida todo el DI al construir: si falta registrar un servicio, no genera la migración.
- **Identity:**
  - Elige el esquema (el 3 trae passkeys) leyendo `IdentityOptions` del contenedor de la app: sobrescribir `SchemaVersion` **no sirve**, porque `OnModelCreating` no lo usa. `AddInfrastructure` registra la opción, y si el contexto se crea sin DI (tests), `FinanzasDbContext.OnConfiguring` usa un contenedor mínimo con ella. Síntoma si falta: "IdentityPasskeyData requires a primary key".
  - Por defecto exige mayúscula, minúscula y dígito: acá solo largo (02 §5).
  - La API de passkeys se copió de la plantilla oficial (`dotnet new blazor --auth Individual`).
- **El filtro `Ledger`** se arma con `Expression.Constant(this)` sobre `CurrentLedgerId`: EF lo reevalúa por contexto. Sin `ILedgerScope` (tests, migraciones, login) no restringe.
- **Tests de la Api:**
  - Cliente con base `https://localhost` (la cookie es `Secure`) y `RateLimiting:AuthPermitLimit` alto.
  - La base tiene que existir **antes** de levantar la Api: al arrancar, Data Protection lee sus claves y `EnableRetryOnFailure` trata "Cannot open database" como transitorio. Si no existe, el arranque tarda ~100 s y la suite parece colgada.
- **OpenAPI (.NET 10):** un endpoint que devuelve `IResult` no documenta su cuerpo (por eso `Returns<T>()`). El transformador de esquemas ve las propiedades pero no el componente al que apuntan, así que `Period` va en línea (`CreateSchemaReferenceId` → null) y se cubre también `Period?`.
- **Argentina tuvo horario de verano hasta marzo de 2009** (UTC−2): las fechas al azar de los tests arrancan en 2010.
- **Levantar la Api a mano:** `ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Finanzas.Api` (con `--no-launch-profile`, `--environment` no alcanza y no carga los user-secrets).

## 8. Backlog (en orden)

1. **Front PWA:** el próximo paso grande. Primero debatir el diseño (Martu no quiere un "diseño típico de IA"). La PWA se sirve desde la misma Api (02 §4.1).
2. **Lo que falta de la Api** (02 §3.2, los marcados *Pendiente*):
   - Conciliar saldo (`Transaction.CreateReconciliation`) y fechas reales de un resumen (R5: `Statement.ChangeDates` + `InstallmentScheduler.Reschedule`).
   - Editar cuotas (`EditInstallmentAmounts`), editar y borrar movimientos, registrar rendimientos y devoluciones.
   - `GET /reports/cashflow`.
   - `rowVersion` en los `PUT` (02 §4.3) y archivar o borrar categorías y etiquetas.
3. **Jobs:** el diario (cotizaciones + fijos) y el backfill de ArgentinaDatos desde 2025-09-01.
4. **Importador del Excel** (01 §7 y §8) y el test de oro contra las celdas `K` de cada mes.
5. **CI/CD** con GitHub Actions y deploy a Azure (02 §10). Definir el dominio **antes** de registrar passkeys reales.

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
