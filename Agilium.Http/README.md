# Agilium API — Bruno collection

Manual/regression testing collection for the `Agilium.Be` API, used while implementing and verifying
`task-stories.md`. Open this folder in [Bruno](https://www.usebruno.com/), select the **Local**
environment, and run requests top to bottom within each folder (they're numbered and chain into each
other via collection variables set in `script:post-response` blocks - e.g. `Auth/2. Login` sets
`accessToken`, `Projects/1. Create` sets `projectId`).

If your `Agilium.Db/.env` overrides the default backend port, update `baseUrl` in the **Local**
environment (`environments/Local.bru`) to match (default assumes the `docker-compose.yml` default of
`5049`).

- `Auth/` — create a login-capable user, log in (sets the refresh-token cookie + `accessToken`),
  refresh, logout.
- `Projects/` — create/list a project, toggle its Active/Inactive state.
- `Items/` — the Story 2 feature/user-story/task/bug tree: create with and without an explicit parent
  (the latter exercises lazy generic-container creation), list the tree, rename, reparent, assign,
  read/write template field values (Story 3), and delete (with cascade, incl. field values) - several
  requests' `docs` blocks call out the validation errors worth trying on purpose (renaming/deleting a
  generic item, invalid parent/child type combinations, a field value that doesn't belong to the
  item's template, etc).
- `Templates/` — Story 3's CSS-grid template CRUD, both the global default template
  (`GET`/`PUT /api/v1/templates/{itemType}`) and a project's own copy
  (`GET`/`PUT /api/v1/projects/{id}/templates/{itemType}`) - editing the global template doesn't
  retroactively change any project's copy. `docs` blocks call out the grid-bounds and duplicate-key
  validation worth trying on purpose.
