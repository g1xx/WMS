# Test Plan – Warehouse Management System (WMS)

| Item | Value |
|---|---|
| Version under test | `dev` branch (build/commit: `<COMMIT_SHA>`) |
| Author | `<NAME>` |
| Date | `<DATE>` |

## 1. Scope

**In scope**
- Authentication: login, logout, invalid credentials, missing/invalid/expired JWT.
- Role-based access (roles: `Worker`, `Brigadier`, `Admin`, `Integration`) on API endpoints and terminal screens, incl. supervisor badge override.
- Goods receipt: inbound receiving notice (`POST /api/PutawayTask`, Integration role) and direct stock receipt (`POST /api/Stocks`).
- Putaway (terminal "Start Putaway") and stock relocation (terminal "Relokacja").
- Order creation and allocation, picking flow (claim → start → pick → dispatch), cancel/release.
- Shortages and discrepancies: allocation shortage, "Item not found", "Defective / Damaged", manual stock adjustment.
- Order and pick-task status transitions.
- Input validation (quantities, required fields, unknown barcodes/SKUs).
- Realtime pick-task updates over SignalR (`/hubs/picktasks`, event `PickTasksChanged`).
- Basic UI checks of the staff terminal (`Frontend/warehouse-client`).

**Out of scope**
- `Frontend/TestOrderGenerator` ("Inbound Order Feed") UI – only its API calls are covered.
- Performance, load and security penetration testing.
- Bulk seeding endpoints (`seed-mass-locations`, `seed-mass-containers`) beyond a smoke check.
- Database migrations, deployment/CI pipeline, HTTPS/nginx configuration.
- Cross-browser and mobile-device compatibility (single desktop browser only).

## 2. Test levels and types

| Level / type | Approach |
|---|---|
| System – functional (UI) | Manual execution of `test-cases.md` in the staff terminal |
| System – API | Manual + Postman collection (`postman/WMS.postman_collection.json`), checks in `api-tests.md` |
| Role-based access | Decision table (role × action), 401/403 checks per endpoint group |
| Negative | Invalid input, wrong barcodes, wrong state, wrong role |
| Basic UI | Navigation, alerts/messages, disabled buttons, Escape key menus |
| Exploratory | Time-boxed charters (EX-xx in `test-cases.md`) |

Test design techniques: equivalence partitioning (EP), boundary value analysis (BVA), state transition (ST), decision table (DT), exploratory (EXP).

## 3. Environment

| Item | Value |
|---|---|
| Frontend URL | `<FRONTEND_URL>` (e.g. `http://localhost` via Docker/nginx, or `http://localhost:5173` Vite dev) |
| API base URL | `<API_BASE_URL>` (e.g. `http://localhost:5124` local, or same origin `/api` behind nginx) |
| SignalR hub | `<API_BASE_URL>/hubs/picktasks` |
| Database | PostgreSQL, demo data seeded with `--seed-demo-data` |
| Browser | `<BROWSER + VERSION>` |
| Tools | Postman (v10+), browser DevTools (Network, Console) |

**Test accounts (placeholders – never store real passwords in this repo)**

| Role | Username | Password | Badge barcode (user id) | How created |
|---|---|---|---|---|
| Admin | `<ADMIN_USER>` | `<ADMIN_PASSWORD>` | `<ADMIN_BADGE_ID>` | Demo seeder |
| Integration | `<INTEGRATION_USER>` | `<INTEGRATION_PASSWORD>` | n/a | Demo seeder |
| Brigadier | `<BRIGADIER_USER>` | `<BRIGADIER_PASSWORD>` | `<BRIGADIER_BADGE_ID>` | `POST /api/Auth/register-brigadier` (Admin) |
| Worker | `<WORKER_USER>` | `<WORKER_PASSWORD>` | `<WORKER_BADGE_ID>` | `POST /api/Auth/register` (Admin) |
| Worker 2 | `<WORKER2_USER>` | `<WORKER2_PASSWORD>` | – | `POST /api/Auth/register` (Admin) |

## 4. Entry criteria
- Build deployed to the test environment; API and frontend reachable; DB migrated and demo data seeded.
- All test accounts above exist and can log in.
- Test cases and Postman collection reviewed.

## 5. Exit criteria
- 100% of High-priority test cases executed; ≥ 90% of all test cases executed.
- No open Critical/Blocker defects; Major defects have an agreed workaround or fix plan.
- All defects logged using `bug-report-template.md`; open questions in `open-questions.md` answered or accepted.

## 6. Risks

| Risk | Impact | Mitigation |
|---|---|---|
| Shared demo data is consumed by testing (containers, stock, tasks) | Tests become non-repeatable | Use dedicated test sector (e.g. `mp9`) and unique SKUs/barcodes per run |
| Concurrency (two workers, same container/task) is hard to reproduce manually | Race-condition defects missed | Two browsers/profiles side by side; exploratory charter EX-01 |
| Claim timeout is 15 min (`PickTaskSettings:ClaimTimeoutMinutes`) | Long waits during testing | Ask for a lower value in the test environment config |
| Supervisor override token lives only 2 minutes | Flaky manual API tests | Request override token immediately before the elevated call |
| No UI role gating found in the terminal | Access issues only visible via API | Cover role checks at API level (decision table) |
| Several behaviours are unclear in code (see `open-questions.md`) | Wrong expected results | Mark affected cases as blocked until answered |
