# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

Agilium is a Kanban/agile project-management tool (full spec in `description.md`, in Czech). Users belong
to projects; each project has Features → User-Stories → Tasks/Bugs ("Items"), organized into Sprints for
a Kanban view. Item status (TODO/ACTIVE/DONE) rolls up from tasks to user-stories to features. Item field
layout (what's editable per item type) is driven by per-project `Template`/`TemplateColumn`/`TemplateItem`
definitions. See `tasks.md` for the current implementation backlog (what's done vs. still missing) — check
it before assuming a feature exists.

## Solution layout

- `Agilium.Be` — ASP.NET Core minimal-API backend (.NET 8+, C#), EF Core + SQL Server.
- `Agilium.Fe` — React 19 + TypeScript + Vite frontend, TanStack Router/Query, Tailwind v4, Radix UI.
- `Agilium.Db` — just a `docker-compose.yml` that runs SQL Server for local dev (`docker compose up`
  from `Agilium.Db`, using `.env` copied from `.env.example` for `DB_PASSWORD`).

There are no automated test projects in the solution currently.

## Commands

### Backend (Agilium.Be)
- `dotnet build` from `Agilium.Be` (or `dotnet build Agilium.Be.csproj`).
- `dotnet run --project Agilium.Be` — runs EF Core migrations automatically on startup
  (`db.Database.Migrate()` in `Program.cs`); requires the DB from `Agilium.Db` plus a valid `.env`
  (loaded via `DotNetEnv`).
- `dotnet ef migrations add <Name> --project Agilium.Be` — add a new EF Core migration
  (migrations live in `Agilium.Be/Migrations`).
- Config is read from `appsettings.json` + `appsettings.Development.json` into a strongly-typed
  `AppSettings` (see `Program.cs`'s `ConfigureConfigurations`).

### Frontend (Agilium.Fe)
- `npm run dev` — Vite dev server.
- `npm run build` — `tsc -b && vite build`.
- `npm run lint` — ESLint.
- `npm run preview` — preview production build.

## Architecture

### Backend — Handler/Endpoint pattern (Features folder)

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

Model classes are in `Model`; entities mapped to the database are in `Model\Db` (e.g. `Project`, `Item`,
`Sprint`, `SprintItem`, `Template`/`TemplateColumn`/`TemplateItem`, `Role`, `Membership`,
`WorkflowState`).

**Validation:** only use custom attributes from `Exceptions/Validation` (e.g. `XNonEmpty`,
`XEnumValidation`, `XRegex`) on `Command`/`Parameters` properties — never standard .NET
`System.ComponentModel.DataAnnotations` attributes. If a needed validator doesn't exist, add a comment
noting it rather than falling back to built-ins.

**Errors:** throw the typed exceptions in `Exceptions/` (`EntityNotFoundException`, `BadRequestException`,
`ValidationException`, etc.); `GlobalExceptionHandler` translates them to problem-details responses.

**Auth:** JWT bearer with claim `sub` = user id, `role` = role assignments (`P...`-prefixed strings parsed
via `IRoleAssignment.FromRoleString`); `LoggedUser` is populated per-request in `GenericEndpoint` and
available in handlers via `this.LoggedUser`. Role-based authorization (`RequiredRoles` /
`GenericEndpoint.ValidateRequiredRoles`) is currently unimplemented/commented out — see `tasks.md`.

**EF Core query style:** when chaining multiple query methods, put a trailing `//` after the first line:
```csharp
var exam = await dbContext.Exams //
  .AsNoTracking()
  .Include(e => e.Stations)
  .FirstOrDefaultAsync(e => e.Id == parameters.Id, cancellationToken)
  ?? throw new EntityNotFoundException(typeof(Exam), parameters.Id);
```

### Frontend

Target three-layer segmentation (see `description.md` for current gaps):

- **routes** (`src\routes`) — file-based routing via TanStack Router (`routeTree.gen.ts` is
  generated — don't hand-edit it); pages, layout, and data wiring via TanStack Query.
- **components** (`src\components`) — `global` holds reusable generic elements (dialogs, forms,
  loaders); `specific` is for domain-bound components (kanban card, feature/user-story/task detail,
  template editor) — currently empty, fill in as features land.
- **services** — HTTP client + TanStack Query hooks layer between routes/components and the BE API.
  Does not exist yet; routes currently use mock data directly (e.g. `routes\projects\index.tsx`).

Styling uses Tailwind v4 + Radix UI primitives.

## Custom agent/prompt files in this repo

- `.github/agents/.NET Developer.agent.md`, `.NET Reviewer.agent.md`, `React Developer.agent.md` —
  role-specific agent personas (modern idiomatic C#/.NET or React conventions, SOLID, DI, etc.).
- `.github/prompts/createEndpoint.prompt.md` — the authoritative, detailed spec for scaffolding a new
  backend endpoint; follow it exactly when asked to add an endpoint (it matches the pattern above).
- `.github/prompts/reviewBeforeCommit.prompt.md` — a Czech-language pre-commit review checklist
  (staged-diff sanity, formatting, secrets, debug leftovers); propose fixes but don't apply them
  without confirmation.
