# 02 · Backend

> **Estado:** v1 · 2026-10-18
> **Depende de:** [01 · Modelo de datos](01-modelo-de-datos.md)

---

## 1. Decisiones

| Tema | Decisión | Por qué |
|---|---|---|
| Plataforma | **.NET 10 (LTS)** + EF Core 10 | Soporte hasta noviembre de 2028 |
| Front | **PWA** | Una sola base de código para celular y PC; se instala como app |
| Login | **Passkeys** (huella / Face ID) con ASP.NET Core Identity; mail + contraseña como respaldo | Lo más seguro y lo más cómodo en el celular; .NET 10 ya lo trae |
| Hosting | **Azure en capa gratuita**: Container Apps + Azure SQL serverless | Unos USD 0/mes para una persona |
| Arquitectura | **Clean Architecture en 4 proyectos**, con el dominio sin dependencias | Las reglas (cuotas, resúmenes, disponible) se testean como código puro |
| API | **Por intenciones**, no un CRUD genérico | El cliente no puede crear estados inválidos |
| Lectura / escritura | **CQRS liviano en la misma base** | Las escrituras pasan por el dominio; las lecturas van directo a DTOs |
| Librerías que se evitan | MediatR, AutoMapper | Agregan indirección innecesaria para este tamaño (y desde 2025 son comerciales) |

---

## 2. Estructura de la solución

```
gestor-de-finanzas/
├─ docs/arquitectura/
├─ src/
│  ├─ Finanzas.Domain/            Entidades, value objects, reglas R1–R12. Sin dependencias
│  ├─ Finanzas.Application/       Casos de uso e interfaces (puertos)
│  ├─ Finanzas.Infrastructure/    EF Core, SQL Server, cliente DolarApi, jobs
│  ├─ Finanzas.Api/               Minimal APIs, login, OpenAPI, sirve la PWA
│  └─ Finanzas.Importer/          CLI de uso único: Excel → base
├─ tests/
│  ├─ Finanzas.Domain.Tests/
│  ├─ Finanzas.Architecture.Tests/
│  ├─ Finanzas.Infrastructure.Tests/   Persistencia contra SQL Server real (Testcontainers)
│  └─ Finanzas.Api.IntegrationTests/
├─ compose.yaml                   SQL Server local en Docker
├─ .env.example                   Plantilla de la contraseña de sa (el .env real no se versiona)
├─ dotnet-tools.json              dotnet-ef fijado como herramienta local
├─ global.json                    SDK de .NET y runner de tests
├─ Directory.Build.props          net10.0, Nullable, TreatWarningsAsErrors
├─ Directory.Packages.props       Versiones centralizadas de paquetes
└─ Finanzas.slnx
```

> `Finanzas` es un nombre provisorio (ver §12).

**Referencias permitidas.** Un test de arquitectura las hace cumplir: falla si, por ejemplo, `Domain` llega a referenciar EF Core.

| Proyecto | Puede referenciar a |
|---|---|
| `Domain` | nada |
| `Application` | `Domain` |
| `Infrastructure` | `Application`, `Domain` |
| `Api` | `Application`, `Infrastructure` (esta última solo para registrar dependencias) |
| `Importer` | `Application`, `Infrastructure` |

### 2.1 `Domain`

```
Finanzas.Domain/
├─ Common/          Money, Period, Result<T>, Error, Entity, IAggregateRoot
├─ Ledgers/         Ledger, LedgerMember
├─ Accounts/        Account, CreditLine
├─ Statements/      Statement
├─ Transactions/    Transaction, Installment, TransactionKind, TransferFactory
├─ Recurring/       RecurringTemplate, Schedule
├─ Categories/      Category
├─ Tags/            Tag
└─ Services/        InstallmentScheduler, AvailableToSpendCalculator, RecurringGenerator
```

**Value objects**
- `Money(decimal Amount, string Currency)`: no permite operar entre monedas distintas, y el redondeo es explícito.
- `Period(int Year, int Month)`: `Next()`, `FromDate()`, `FirstDay`, `Contains(date)`. Se serializa como `"2026-10"`.

**Agregados**

| Agregado | Contiene | Protege |
|---|---|---|
| `Transaction` | sus `Installments` | Signo según `Kind`; categoría obligatoria cuando corresponde; Σ cuotas = monto; cuotas consecutivas en la misma moneda |
| `Statement` | — | Fechas coherentes; no se reasigna si ya tiene pagos (R5) |
| `Account` | su `CreditLine` | Una cuenta de crédito no es líquida |
| `RecurringTemplate` | — | Calcula sus ocurrencias para un rango de períodos |
| `Ledger`, `Category`, `Tag` | — | Unicidad de nombres; categorías de 2 niveles como máximo |

Una transferencia **no** es un agregado guardado: `TransferFactory` crea las dos `Transaction` con el mismo `TransferGroupId`, y se guardan en una sola unidad de trabajo.

**Servicios de dominio.** Son funciones puras que reciben todo lo que necesitan:

| Servicio | Reglas | Entrada → salida |
|---|---|---|
| `InstallmentScheduler` | R3, R4, R5 | Compra + línea de crédito + resúmenes existentes → cuotas asignadas y resúmenes estimados nuevos |
| `AvailableToSpendCalculator` | R8, R8b | Saldos líquidos, movimientos del mes, resúmenes que vencen y cotización → disponible por moneda y unificado en ARS |
| `RecurringGenerator` | R7 | Plantillas + período + lo ya generado → movimientos `planned` faltantes |

### 2.2 `Application`

Está organizada por feature: cada caso de uso es una carpeta con su comando o consulta, su validador, su respuesta y su handler. Los marcados con ✓ ya están implementados y testeados.

```
Finanzas.Application/
├─ Abstractions/    ICurrentUser, IUnitOfWork, repositorios (Repositories.cs)
│                   y lectores de consulta (Readers.cs)
├─ Common/          Errores, validación, idempotencia, búsquedas acotadas al libro
├─ ExchangeRates/   ExchangeRateService (R9 + actualización a demanda), contratos de fuente y almacén
├─ Expenses/RegisterExpense/          ✓
├─ Incomes/RegisterIncome/            ✓
├─ Transfers/RegisterTransfer/        ✓
├─ Statements/PayStatement/           ✓    UpdateStatementDates/ (R5)
├─ Transactions/ConfirmPlanned/       ✓    EditInstallments/
├─ Recurring/GenerateRecurring/       ✓
├─ Accounts/ReconcileAccount/
├─ Months/GetMonthSummary/            ✓
├─ CreditLines/GetDebtSchedule/       ✓
└─ Reports/GetSpending/               ✓    GetCashflow/
```

- **Handlers sin MediatR:** cada handler es una clase con `HandleAsync` que devuelve `Result<T>`, y la Api lo llama directo.
- **Escritura:** repositorios por agregado + `IUnitOfWork`. `Application` no conoce EF Core. `IUnitOfWork.SaveChangesAsync` devuelve los conflictos (concurrencia, duplicados) como `Result`, no como excepciones.
- **Lectura:** *lectores* (`IMonthReader`, `IStatementReader`, `ISpendingReader`) que implementa `Infrastructure`. Hacen las sumas en SQL y devuelven datos planos; el handler los combina con las reglas del dominio. Por ejemplo, el disponible lo calcula siempre `AvailableToSpendCalculator`.
- **Libro actual:** todo se acota al libro de `ICurrentUser`. Lo que es de otro libro se responde como "no encontrado".
- **Tiempo:** se usa `TimeProvider`, más la `TimeZone` del libro. "Hoy" siempre se calcula en hora de Argentina.

### 2.3 Ejemplo de flujo: compra con tarjeta en 3 cuotas

1. **Api:** `POST /api/expenses` con `{ id, accountId, date, amount, currency, categoryId, tagIds, installments: 3 }`. El `id` lo genera el front (ver §4.2).
2. **Validador:** revisa la forma de los datos (monto > 0, fecha válida, 1 ≤ cuotas ≤ 60).
3. **Handler:** carga la cuenta con su `CreditLine` y los resúmenes desde el mes de la compra.
4. **Dominio:** `Transaction.CreateExpense(...)` crea la compra, y después `InstallmentScheduler` reparte las cuotas y crea los resúmenes estimados que falten.
5. **Unidad de trabajo:** un único `SaveChanges`, dentro de una transacción de SQL Server.
6. **Respuesta:** `201` con la compra y en qué resumen cayó cada cuota.

---

## 3. API

### 3.1 Convenciones

- Todo bajo el prefijo `/api`. JSON en `camelCase`.
- Fechas en formato `yyyy-MM-dd`; períodos en formato `yyyy-MM`.
- Los montos viajan como número JSON y **los totales siempre los calcula el back**: el front no suma plata.
- Los errores salen como *ProblemDetails* (§8).
- Documentación con OpenAPI nativo de .NET, navegable en `/scalar` en desarrollo. Del documento OpenAPI se genera el cliente TypeScript de la PWA.

### 3.2 Endpoints v1

| Área | Endpoint | Caso de uso |
|---|---|---|
| Sesión | `/api/auth/*` (Identity) | Registro y login con passkey, contraseña de respaldo, logout, `GET /api/auth/me` |
| Libro | `GET` · `PUT /api/ledger/settings` | Moneda base, casa de referencia, zona horaria |
| Mes | `GET /api/months/{yyyy-MM}` | **Pantalla principal**: disponible, compromisos y totales |
| Cuentas | `GET /api/accounts` | Cuentas con sus saldos por moneda |
| | `POST` · `PUT /api/accounts[/{id}]` · `POST /api/accounts/{id}/archive` | Alta, edición y archivado |
| | `POST /api/accounts/{id}/reconcile` | Recibe `{ realBalance }` y genera el `adjustment` por la diferencia (R12) |
| Crédito | `GET /api/credit-lines/debt?from=2026-10&months=6` | Acumulado mes a mes por tarjeta y de todas juntas |
| | `GET /api/credit-lines/{id}/statements?from&to` | Resúmenes de una línea |
| Resúmenes | `GET /api/statements/{id}` | Detalle con sus cuotas y totales por moneda |
| | `PUT /api/statements/{id}/dates` | Fechas reales; reasigna cuotas (R5) |
| | `POST /api/statements/{id}/payments` | Pago, en ARS o en USD (R6) |
| Movimientos | `POST /api/expenses` | Gasto; en una cuenta de crédito genera cuotas |
| | `POST /api/incomes` · `POST /api/yields` | Ingreso · rendimiento |
| | `POST /api/transfers` | Transferencia entre cuentas, incluida la compra de dólares |
| | `GET /api/transactions?from&to&accountId&categoryId&tagId&status&q` | Listado con filtros |
| | `GET` · `PUT` · `DELETE /api/transactions/{id}` | Detalle, edición y borrado lógico |
| | `POST /api/transactions/{id}/confirm` | Confirmar un previsto, con su monto y fecha reales |
| | `PUT /api/transactions/{id}/installments` | Editar los montos de las cuotas (§4.11 del modelo) |
| Fijos | CRUD `/api/recurring-templates` | Plantillas recurrentes |
| Catálogos | CRUD `/api/categories` · `/api/tags` | |
| Reportes | `GET /api/reports/spending?period=…&by=category\|tag&mode=due\|purchase` | Gasto por categoría o etiqueta (R10) |
| | `GET /api/reports/cashflow?from&to` | Evolución de saldos y ahorro |
| Cotizaciones | `GET /api/exchange-rates/latest` · `GET /api/exchange-rates?source&from&to` | |

### 3.3 Ejemplo: `GET /api/months/2026-10`

```json
{
  "period": "2026-10",
  "available": { "ARS": 460000.00, "USD": -40.00, "unifiedArs": 397800.00 },
  "liquidAtStart": { "ARS": 1800000.00, "USD": 0.00 },
  "income": { "ARS": 0.00 },
  "confirmedExpenses": { "ARS": 406000.00 },
  "plannedPending": [
    { "id": "…", "description": "Ahorro mensual", "amount": 800000.00, "currency": "ARS", "date": "2026-10-05" }
  ],
  "statementsDue": [
    { "creditLine": "Mastercard", "dueDate": "2026-10-06", "totals": { "ARS": 75000.00 }, "paid": false },
    { "creditLine": "Patagonia", "dueDate": "2026-10-10", "totals": { "ARS": 50000.00, "USD": 40.00 }, "paid": false },
    { "creditLine": "Mercado Crédito", "dueDate": "2026-10-10", "totals": { "ARS": 9000.00 }, "paid": false }
  ],
  "rate": { "source": "blue", "sell": 1555.00, "date": "2026-10-18" }
}
```

> Los números salen de la hoja Octubre2026 del Excel. Como no hay dólares líquidos, los USD 40 de Claude quedan en `USD: -40`, y por R8b se restan del unificado al blue del día: 40 × 1.555 = 62.200. El Excel da 400.000 porque convierte con 1.500 fijo; con el blue da 397.800.

---

## 4. Pensado para el celular

### 4.1 Mismo origen para la PWA y la API

El contenedor de la API también sirve los archivos estáticos de la PWA: la API en `/api` y la PWA en `/`. Como están en el mismo origen, la sesión va en una cookie `HttpOnly` + `Secure` + `SameSite=Strict`:
- No hace falta CORS.
- Ningún token queda accesible desde JavaScript.

### 4.2 Idempotencia

El front genera el `id` (GUID) de cada movimiento antes de enviarlo:
- Si el mismo `id` vuelve a llegar con los **mismos** datos, el servidor responde `200` con lo ya guardado, sin duplicar.
- Si llega con datos **distintos**, responde `409`.

Así, un reintento por mala señal nunca duplica un gasto. Además, deja la base preparada para cargar sin conexión en el futuro.

### 4.3 Concurrencia optimista

Cada `PUT` lleva el `rowVersion` que el cliente leyó. Si otro dispositivo modificó el registro en el medio, la respuesta es `409` con la versión actual, y la PWA ofrece recargar.

---

## 5. Seguridad y login

- **Usuarios:** `AppUser : IdentityUser<Guid>` (con `DisplayName`) se mapea a la tabla `Users` del modelo. Identity suma sus propias tablas (passkeys, logins externos, tokens).
- **Passkeys como método principal**, más mail + contraseña como respaldo y para recuperar la cuenta.
- **El dominio importa.** Una passkey queda atada al dominio donde se registró: si después cambia el dominio, hay que volver a registrarla. Por eso conviene **definir el dominio definitivo antes de registrar la primera** (§12).
- **Claves de Data Protection persistidas en la base** (`PersistKeysToDbContext`). Sin eso, cada vez que el contenedor arranca desde cero se invalidan las cookies, y la sesión se cerraría cada vez que la app se despierta.
- **Registro cerrado:** solo se crea el primer usuario (owner) del libro. A futuro se suman miembros por invitación.
- **Autorización:** todas las consultas filtran por el `LedgerId` del usuario actual, mediante un filtro global de EF Core. Los roles (`owner` / `editor` / `viewer`) se aplican con *policies*.
- **Rate limiting** en los endpoints de login, con el limitador nativo de ASP.NET Core.
- **Secretos:** `dotnet user-secrets` en desarrollo y *secrets* de Container Apps en producción.

---

## 6. Tareas en segundo plano

En la capa gratuita, la app **escala a cero**: si nadie la usa, no hay proceso corriendo, así que nada puede depender de un timer interno. Cada tarea tiene un disparo *a demanda* y, como respaldo, uno programado:

| Tarea | A demanda | Programado | Detalle |
|---|---|---|---|
| Cotizaciones | Al pedir la cotización de hoy, si la guardada tiene más de 30 min. Si DolarApi falla, se usa la última conocida y su fecha va en la respuesta | Container Apps Job diario, 18:30 (hora AR) | Upsert en `ExchangeRates` desde DolarApi, con la fecha local de Argentina |
| Histórico de cotizaciones | — | Una vez, desde el importador | ArgentinaDatos desde 2025-09-01 |
| Fijos previstos | `EnsureGeneratedThrough(period)` al abrir un mes | El mismo job diario | Idempotente gracias al `UNIQUE` (R7) |
| Resúmenes estimados | Al asignar cuotas (R4) | — | |
| Resumen abierto → cerrado | Se calcula al leer, comparando la fecha de hoy con `ClosingDate` | — | No hace falta un job |

---

## 7. Persistencia (EF Core)

- **Contexto:** un `FinanzasDbContext`, con una `IEntityTypeConfiguration<T>` por entidad (`Infrastructure/Persistence/Configurations`).
- **Convenciones globales** (`ConfigureConventions`):
  - enums guardados como string en snake_case;
  - `Period` guardado como `date` (día 1 del mes);
  - `DateTimeOffset` guardado en `datetime2(3)` y en UTC;
  - precisión `decimal(18,2)`, y `(18,6)` para las cotizaciones.
- **Auditoría:** `CreatedAt`, `UpdatedAt`, `DeletedAt` y `RowVersion` son *shadow properties*: el dominio no las conoce. Las completa un único `AuditInterceptor`, que además convierte los `DELETE` en borrado lógico.
- **Agregados completos:** las cuotas y las etiquetas de un movimiento se cargan siempre con él (`AutoInclude`).
- **Filtros globales con nombre** (EF Core 10):
  - `SoftDelete` ya está aplicado a todas las tablas auditadas.
  - `Ledger` se agrega junto con el login. Al estar separados, se puede apagar uno solo (por ejemplo, un "deshacer borrado" ignora `SoftDelete`).
- **Cascadas:** solo las cuotas y las etiquetas de un movimiento (más los tipos owned). El resto de las FKs son `Restrict`, porque SQL Server no admite varios caminos de cascada hacia una misma tabla.
- **Restricciones del modelo:** los `CHECK` van con `HasCheckConstraint` y los índices filtrados con `HasFilter`. Así, toda la §4 del modelo queda versionada en las migraciones.
- **Migraciones:** code-first, en `Infrastructure/Persistence/Migrations`. La Api es el proyecto de inicio de `dotnet ef`. En producción se aplican con un *migration bundle* (`dotnet ef migrations bundle`) desde el pipeline, nunca al arrancar la app.
- **Resiliencia:** `EnableRetryOnFailure`. Azure SQL serverless puede tardar alrededor de un minuto en despertar después de una pausa, y el reintento cubre ese tiempo.

---

## 8. Errores y validación

- **Forma de los datos:** FluentValidation sobre cada comando.
- **Reglas de negocio:** el dominio devuelve `Result<T>` con un `Error(code, message)`; las excepciones quedan solo para lo inesperado.
- **Salida:** todo error se traduce a *ProblemDetails* (RFC 9457), con un `code` estable (por ejemplo `expense.account_not_credit`) que el front puede traducir.

| Situación | HTTP |
|---|---|
| Datos mal formados | `400` con el detalle por campo |
| No existe (o no es de tu libro) | `404` |
| Conflicto de versión o de idempotencia | `409` |
| Viola una regla de negocio | `422` |

---

## 9. Tests

| Nivel | Herramienta | Qué cubre |
|---|---|---|
| Dominio | xUnit | R1–R12 con casos reales del Excel: Perfume Lu 2/3, Perfume mamá 30.000/30.000/31.600, Patagonia PES + USD, Claude cubierto con dólares, aguinaldo cada 6 meses |
| Arquitectura | NetArchTest | Las referencias permitidas de §2 |
| Persistencia | EF Core + Testcontainers (SQL Server real en Docker) | Guardar y leer agregados completos, enums en snake_case, borrado lógico, `CHECK` y `UNIQUE` del modelo, concurrencia |
| Casos de uso | Application + Infrastructure armadas como en la Api, contra el mismo SQL Server, con reloj fijo y DolarApi simulada | La pantalla de octubre 2026 contra la celda `K10` del Excel, idempotencia, gasto por categoría y etiqueta (R10), deuda por tarjeta, fijos a demanda, cotizaciones |
| Integración | `WebApplicationFactory` + Testcontainers (SQL Server real en Docker) | Endpoints de punta a punta, `CHECK` / `UNIQUE`, idempotencia, concurrencia, filtros por libro |
| Test de oro | Importador + comparación | El disponible de cada mes contra la celda `K` del Excel, salvo en los meses afectados por la §8 del modelo |

---

## 10. Entornos y deploy

| Entorno | Componentes |
|---|---|
| **Local** | SQL Server 2025 en Docker (`compose.yaml`, contraseña en `.env`) · cadena de conexión en los user-secrets de la Api · API con `dotnet watch` · Scalar en `/scalar` |
| **CI** | GitHub Actions: build y tests (incluidos los de Testcontainers) → imagen del contenedor → `ghcr.io` → migration bundle → deploy |
| **Producción** | **Azure Container Apps** (plan de consumo): escala a cero, cubierto por la capa gratuita mensual y con dominio propio y certificado gratis · **Azure SQL Database** con la oferta gratuita (serverless, con pausa automática) |

Por qué se descartan algunas alternativas:
- **App Service Free (F1):** no admite dominio propio, y eso importa por las passkeys (§5).
- **Azure Container Registry:** es pago. La imagen se publica en GitHub Container Registry, que es gratis.

Monitoreo: logs estructurados, `/health` para los *health checks* y, opcionalmente, Application Insights dentro de su capa gratuita.

---

## 11. Futuro (fuera de v1)

- **Carga sin conexión** en la PWA, con una cola local y sincronización. La base ya está preparada: el front genera los Ids y la API es idempotente.
- **Notificaciones push**, por ejemplo: "en 3 días vence la Visa: $84.000".
- **Varios libros por usuario** desde la UI (el modelo ya lo soporta).
- **App nativa.** La API no cambia; solo se agregaría un login por token.

---

## 12. Pendientes

- [ ] **Nombre** de la app y de la solución (hoy se usa `Finanzas` como provisorio).
- [ ] **Dominio definitivo**, antes de registrar las passkeys: sirve el gratuito de Azure o uno propio.
- [ ] **Repositorio git + GitHub**, necesario para el CI/CD.
