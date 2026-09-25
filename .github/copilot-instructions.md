# Copilot instructions — eng-agilium

Agilium is a Kanban/agile project-management tool (see `description.md`, in Czech). Users belong to
projects; each project has Features → User-Stories → Tasks/Bugs ("Items"), organized into Sprints for
a Kanban view. Item status (TODO/ACTIVE/DONE) rolls up from tasks to user-stories to features.

## Solution layout

- `Agilium.Be` — ASP.NET Core minimal-API backend (.NET 8+, C#), EF Core + SQL Server.
- `Agilium.Fe` — React 19 + TypeScript + Vite frontend, TanStack Router/Query, Tailwind v4, Radix UI.
- `Agilium.Db` — just a `docker-compose.yml` that runs SQL Server for local dev (`docker compose up`
  from `Agilium.Db`, using `.env` copied from `.env.example` for `DB_PASSWORD`).

There are no automated test projects in the solution currently.

## Backend (Agilium.Be)

**Build / run:**
- `dotnet build` from `Agilium.Be` (or `dotnet build Agilium.Be.csproj`).
- `dotnet run --project Agilium.Be` — runs migrations automatically on startup (`db.Database.Migrate()`
  in `Program.cs`) and requires the DB from `Agilium.Db` plus a valid `.env` (loaded via `DotNetEnv`).
- Config is read from `appsettings.json` + `appsettings.Development.json` into a strongly-typed
  `AppSettings` (see `Program.cs`'s `ConfigureConfigurations`).

**Architecture — Handler/Endpoint pattern (Features folder):**
Every REST endpoint lives under `Features/<PluralNoun>/<Verb>.cs` (e.g. `Features/Items/Create.cs`) and
follows a fixed shape, in this order:
1. `Command` record — JSON body (use `EmptyCommand` if none).
2. `Parameters` record — query/route params (use `EmptyParameters`, or `IdParameters` for a single `Id`).
3. `Result` record — response body (use `EmptyResult` if none), mapped directly from EF entities via
   `Select(e => new XRecord(e.Id, ...))` where possible instead of loading whole entities.
4. `Handler : GenericHandler<TCommand, TParameters, TResult>` — implements `HandleAsync`, injects
   `AppDbContext` via primary constructor, throws `EntityNotFoundException(typeof(Entity), id)` when a
   looked-up entity is missing, uses `.AsNoTracking()` for read-only queries.
5. `Endpoint` — inherits `GenericOkEndpoint<...>` (200, includes `EmptyResult` → 204), or
   `GenericCreatedEndpoint<...>` (201 + `IdResult`), or `GenericEndpoint<...>` for custom behavior via
   overriding `ProcessRequestAsync`. Declares `Method`, `BaseRoute` (enum in `GenericEndpoint.cs`),
   `EndpointRoute`, `RequiredRoles` (leave empty unless told otherwise), and an `[EndpointSummary(...)]`.
   HTTP verb convention: Read=GET, Create=POST, Update=PATCH, Delete=DELETE.

Handlers/Endpoints are auto-discovered and DI-registered by scanning the assembly for
`IHandler`/`IEndpoint` implementations (`Program.cs` → `ConfigureHandlersAndEndpoints` /
`MapEndpoints`) — no manual registration needed for new features.

**Validation:** only use custom attributes from `Exceptions/Validation` (e.g. `XNonEmpty`,
`XEnumValidation`, `XRegex`) on `Command`/`Parameters` properties — never standard .NET
`System.ComponentModel.DataAnnotations` attributes. If a needed validator doesn't exist, add a comment
noting it rather than falling back to built-ins.

**Errors:** throw the typed exceptions in `Exceptions/` (`EntityNotFoundException`, `BadRequestException`,
`ValidationException`, etc.); `GlobalExceptionHandler` translates them to problem-details responses.

**Auth:** JWT bearer with claim `sub` = user id, `role` = role assignments (`P...` prefixed strings parsed
via `IRoleAssignment.FromRoleString`); `LoggedUser` is populated per-request in `GenericEndpoint` and
available in handlers via `this.LoggedUser`.

**EF Core query style:** when chaining multiple query methods, put a trailing `//` after the first line:
```csharp
var exam = await dbContext.Exams //
  .AsNoTracking()
  .Include(e => e.Stations)
  .FirstOrDefaultAsync(e => e.Id == parameters.Id, cancellationToken)
  ?? throw new EntityNotFoundException(typeof(Exam), parameters.Id);
```

**Migrations:** EF Core migrations live in `Agilium.Be/Migrations`; add with
`dotnet ef migrations add <Name> --project Agilium.Be`.

## Frontend (Agilium.Fe)

- `npm run dev` — Vite dev server. `npm run build` — `tsc -b && vite build`. `npm run lint` — ESLint.
  `npm run preview` — preview production build.
- Routing is file-based via TanStack Router (`src/routes/**`, e.g. `routes/projects/$id/index.tsx`);
  `routeTree.gen.ts` is generated — don't hand-edit it.
- `src/components/global` holds shared building blocks (`dialogs`, `forms`, generic UI); keep
  feature-specific components colocated near their route where practical.
- Server state uses TanStack Query; styling uses Tailwind v4 + Radix UI primitives.

## Custom agent/prompt files already in this repo

- `.github/agents/.NET Developer.agent.md`, `.NET Reviewer.agent.md`, `React Developer.agent.md` —
  role-specific agent personas (modern idiomatic C#/.NET or React conventions, SOLID, DI, etc.).
- `.github/prompts/createEndpoint.prompt.md` — the authoritative, detailed spec for scaffolding a new
  backend endpoint; follow it exactly when asked to add an endpoint (it matches the pattern above).
- `.github/prompts/reviewBeforeCommit.prompt.md` — a Czech-language pre-commit review checklist
  (staged-diff sanity, formatting, secrets, debug leftovers); propose fixes but don't apply them
  without confirmation.
