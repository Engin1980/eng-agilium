# Agilium API — Bruno collection

Manual/regression testing collection for the `Agilium.Be` API, used while implementing and verifying
`.docs/development/task-stories.md`. Open this folder in [Bruno](https://www.usebruno.com/), select the **Local**
environment, and run requests top to bottom within each folder (they're numbered and chain into each
other via collection variables set in `script:post-response` blocks - e.g. `Auth/2. Login` sets
`accessToken`, `Projects/1. Create` sets `projectId`).

If your `Agilium.Db/.env` overrides the default backend port, update `baseUrl` in the **Local**
environment (`environments/Local.bru`) to match (default assumes the root `docker-compose.yml` default
`BE_PORT` of `5049`).

- `Auth/` — create a login-capable user, log in (sets the refresh-token cookie + `accessToken`),
  refresh, logout.
- `Projects/` — create/list a project, toggle its Active/Inactive state.
- `Items/` — the Story 2 feature/user-story/task/bug tree: create with and without an explicit parent
  (the latter exercises lazy generic-container creation), list the tree, rename, reparent, assign,
  read/write template field values (Story 3), and delete (with cascade, incl. field values) - several
  requests' `docs` blocks call out the validation errors worth trying on purpose (renaming/deleting a
  generic item, invalid parent/child type combinations, a field value that doesn't belong to the
  item's template, etc).
- `Templates/` — a project's own template (`GET`/`PUT /api/v1/projects/{id}/templates/{itemType}`) as a
  tree tables → columns (width) → sections → attributes; `PUT` replaces the whole tree. Global default
  templates are seeded at startup and have no endpoints. `docs` blocks call out the validation worth
  trying on purpose (column width ≥ 1, table needs a column, unique attribute keys; width sum 12 is
  not enforced).

Not covered yet by the collection: sprints/kanban (`Features\Sprints`) and project workflow
(`Features\Workflow`) endpoints, project member assignment.
