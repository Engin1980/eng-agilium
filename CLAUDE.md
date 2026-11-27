# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

Agilium is a Kanban/agile project-management tool (full spec in `.docs/development/description.md`, in Czech). Users belong
to projects; each project has Features → User-Stories → Tasks/Bugs ("Items"), organized into Sprints for
a Kanban view. Item status (TODO/ACTIVE/DONE) rolls up from tasks to user-stories to features (computed
per displayed sprint, not stored).

Documentation files (in Czech) — check them before assuming a feature exists:
- `plan.md` — plan of the remaining work (steps 0–8) incl. the client's decisions; the single source of truth for what to do next.
- `.docs/development/description.md` — full spec, architecture, known gaps.
- `.docs/development/layout-templates.md` — authoritative spec of the template layout (supersedes older notes elsewhere).
- `.docs/development/roles.md` — table of project roles and their permissions.
- `.docs/development/task-stories.md` — history of delivered work as user-facing stories in delivery order (tick boxes after each step).

### Templates

Item field layout (what's editable per item type) is driven by templates, one per item type (Feature,
User-Story, Task, Bug). Entities: `Template` → `TemplateTable` → `TemplateColumn` (`Width`) →
`TemplateSection` → `TemplateItem` (attribute), each level with `OrderIndex`. Tables are stacked; widths of
one table's columns are expected to sum to 12 (bootstrap-style, not enforced; min width 1); a section's
title may be empty; an attribute spans the full column width. `TemplateItemType` is a closed set of 7:
`SingleLineText`, `MultiLineText`, `Integer`, `Decimal`, `Boolean`, `Comments` (a list of Markdown comments, see below),
`LabelOnly`. Values live in `ItemFieldValue`, except `Comments`: those live in `ItemComment` (one thread per `Comments` element and item; endpoints `Features/Comments`, `projects/items/{id}/comments`, `projects/items/comments/{id}`; read = `ViewProject`, write = `EditItems`, edit only by the author, delete by the author or `ManageProject`; the FE renders Markdown with `react-markdown`, never raw HTML). Global templates (`Template.ProjectId IS NULL`) are seeded at
startup and are not editable; each project gets a copy at creation and only the project copies are edited.

## Solution layout

- `Agilium.Be` — ASP.NET Core minimal-API backend (.NET 8, C#), EF Core + SQL Server.
- `Agilium.Fe` — React 19 + TypeScript + Vite frontend, TanStack Router/Query, Tailwind v4, Radix UI.
- `Agilium.Db` — a `docker-compose.yml` that runs only SQL Server for local dev (`docker compose up`
  from `Agilium.Db`, using `.env` copied from `.env.example` for `DB_PASSWORD`).
- Root `docker-compose.yml` (+ `Dockerfile` in `Agilium.Be` and `Agilium.Fe`, root `.env.example` with
  `DB_PASSWORD`, `DB_PORT`, `BE_PORT`, `FE_PORT`) — runs the whole stack (`db`, `be`, `fe`) in containers.
  The `be` service uses `ASPNETCORE_ENVIRONMENT=Development`; a `mailpit` service catches e-mails (web UI on
  port 8025); SMTP settings come from `.env` (`SMTP_*`), `SEED_DEMO_DATA=true` creates `demo@example.com` /
  `Demo-1234!` with a demo project (`Services\DevSeed.cs`, Development only, idempotent).
- `docker-compose.prod.yml` + `.env.prod.example` + `Agilium.Fe\Dockerfile.prod` — generic production stack
  (`ASPNETCORE_ENVIRONMENT=Production`, every value from `.env`, DB port not published, FE built and served by
  nginx). No reverse proxy/TLS/backups yet (deployment target unknown); the refresh cookie is `Secure`, so put
  HTTPS in front. Outside Development/Testing the BE refuses to start with insecure defaults
  (`Services\StartupConfigurationValidator.cs`: JWT key, Turnstile, SMTP, connection string).
- `scripts\check-be-image.ps1` — builds the BE image and verifies no `appsettings.Development.json`/`.env` leaks into it.
- `Agilium.Http` — Bruno collection for manual/regression testing of the BE API (see its `README.md`).

- `Agilium.Be.Tests` — xUnit tests (unit + integration). Integration tests host the whole API in memory
  (`WebApplicationFactory<Program>`) against a real SQL Server started by Testcontainers (Docker required) and
  reset the data with Respawn between tests. Helpers: `Infrastructure\TestApi` (register/login/create project,
  item, sprint), `ApiTestBase` (clean DB per test). `Program` configuration is overridden with `UseSetting`
  because it is read while the host is being built.
- `scripts\test-all.ps1` / `scripts\test-all.sh` — run every check (BE tests, FE install/lint/build/tests).
- `.github\workflows\ci.yml` — the same checks on GitHub, manually triggered only (`workflow_dispatch`).

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

### Tests
- `dotnet test Agilium.Be.Tests` — backend tests (first run pulls the SQL Server image).
- `npm test` in `Agilium.Fe` — Vitest + Testing Library + MSW (`src\test`: setup, MSW `server`, `renderWithProviders`).
- `scripts\test-all.ps1` — everything; must be green before a step is considered done.

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
   `EndpointRoute`, `Policy` (an `EndpointPolicy`: `Anonymous`, `LoggedIn`, `Project(ProjectPermission.X)` or `ProjectOwner`; there is no default — decide for every endpoint), and an `[EndpointSummary(...)]`.
   HTTP verb convention: Read=GET, Create=POST, Update=PATCH, Delete=DELETE. `PUT` is allowed only for
   replacing a whole collection/definition in one call (a project's template, workflow, an item's field
   values); partial updates always use PATCH.
   Keep exactly one folder level: `Features/<PluralNoun>/<Verb>.cs`, no nested subfolders (e.g. future
   role endpoints go to `Features/Roles/`, not `Features/Projects/Roles/`). The URL comes from `BaseRoute`
   + `EndpointRoute`, independent of the folder.

Handlers/Endpoints are auto-discovered and DI-registered by scanning the assembly for
`IHandler`/`IEndpoint` implementations (`Program.cs` → `ConfigureHandlersAndEndpoints` /
`MapEndpoints`) — no manual registration needed for new features.

Model classes are in `Model`; entities mapped to the database are in `Model\Db` (e.g. `Project`, `Item`,
`Sprint`, `SprintItem`, `Template`/`TemplateTable`/`TemplateColumn`/`TemplateSection`/`TemplateItem`, `Role`, `Membership`,
`WorkflowState`).

**Validation:** only use custom attributes from `Exceptions/Validation` (e.g. `XNonEmpty`,
`XEnumValidation`, `XRegex`) on `Command`/`Parameters` properties — never standard .NET
`System.ComponentModel.DataAnnotations` attributes. If a needed validator doesn't exist, add a comment
noting it rather than falling back to built-ins.

**Errors:** throw the typed exceptions in `Exceptions/` (`EntityNotFoundException`, `BadRequestException`,
`ValidationException`, etc.); `GlobalExceptionHandler` translates them to problem-details responses
(`ErrorKey` + `Params`): NotFound 404, AlreadyExists 409, Authentication 401, Authorization 403, Validation /
BadRequest / account locked or deactivated 400. The specific exception types are matched before the base
`BadRequestException` they derive from.

**Auth:** JWT bearer with claim `sub` = user id, `role` = role assignments (`P...`-prefixed strings parsed
via `IRoleAssignment.FromRoleString`); `LoggedUser` is populated per-request in `GenericEndpoint` and
available in handlers via `this.LoggedUser` (identity only, no role claims in the token). The endpoint enforces login (401)
and an active account; the handler enforces the project part with `Access.EnsureCanAsync(projectId, ProjectPermission.X, ct)` /
`Access.EnsureOwnerAsync(projectId, ct)` (`IProjectAccess`, permissions read from the DB through the server-side
`IAccessCache`; non-member → 404, member without the right → 403). Take the project id from the loaded entity, not from the
request, and call it before any change. Code that changes memberships/roles/accounts must call
`IAccessCache.InvalidateProject/InvalidateUser` after commit. `EndpointMatrixTests` walks all endpoints by reflection.

**EF Core query style:** when chaining multiple query methods, put a trailing `//` after the first line:
```csharp
var exam = await dbContext.Exams //
  .AsNoTracking()
  .Include(e => e.Stations)
  .FirstOrDefaultAsync(e => e.Id == parameters.Id, cancellationToken)
  ?? throw new EntityNotFoundException(typeof(Exam), parameters.Id);
```

### Frontend

Three-layer segmentation:

- **routes** (`src\routes`) — file-based routing via TanStack Router (`routeTree.gen.ts` is
  generated — don't hand-edit it); pages, layout, and data wiring via TanStack Query. Covers login,
  registration, projects, project detail (item tree), item detail, sprints + kanban, and project settings
  (templates, workflow).
- **components** (`src\components`) — `global` holds reusable generic elements (dialogs, forms,
  loaders); `specific` holds domain-bound components (item tree, item detail form, template layout and
  template editor, sprint board, workflow editor, attribute inputs).
- **services** (`src\services`) — `http-client.ts` (fetch wrapper, access token kept in memory, silent
  refresh via httpOnly cookie) plus per-domain `*-api.ts` / `*-queries.ts` (TanStack Query hooks) for auth,
  projects, items, item fields, sprints, templates, workflow. Routes/components talk to the BE only
  through this layer. Auth state is in `src\contexts\auth-context.tsx`.

Members, roles and invitation keys: `settings/members`, `settings/roles`, `/join` (done, plan Step 3). Account management (plan Step 5, done): registration sends a verification e-mail (`IEmailSender`, MailKit; Mailpit locally; `FakeEmailSender` in tests) and an unverified account cannot log in (`EMAIL_NOT_VERIFIED`); `auth/verify-email`, `auth/verify-email/resend`, `auth/password-reset/request|confirm`, `auth/password` (change); FE routes `/verify-email`, `/forgot-password`, `/reset-password`, `/account`. Registration, resend and reset answer identically whether or not the e-mail exists.

Styling uses Tailwind v4 + Radix UI primitives.

## Custom prompt files in this repo

- `.github/prompts/createEndpoint.prompt.md` — the authoritative, detailed spec for scaffolding a new
  backend endpoint; follow it exactly when asked to add an endpoint (it matches the pattern above).
- `.github/prompts/reviewBeforeCommit.prompt.md` — a Czech-language pre-commit review checklist
  (staged-diff sanity, formatting, secrets, debug leftovers); propose fixes but don't apply them
  without confirmation.
