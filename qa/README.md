# QA – Warehouse Management System

Manual and API test artifacts for the WMS (ASP.NET Core Web API + PostgreSQL, React/TypeScript staff terminal, SignalR pick-task hub, JWT auth with roles `Worker`, `Brigadier`, `Admin`, `Integration`).
All expected results are taken from the application code (controllers, services, DTOs, frontend pages). Anything unclear is listed in `open-questions.md` instead of being assumed.

## Contents

| File | Purpose |
|---|---|
| `test-plan.md` | Scope, test levels/types, environment, test accounts (placeholders), entry/exit criteria, risks |
| `test-cases.md` | 32 test cases + 3 exploratory charters, with decision tables DT-1 (role × action), DT-2 (supervisor override), DT-3 (stock adjustment) |
| `api-tests.md` | API checks per endpoint group: method, URL, auth, body, expected status and key response fields, incl. 401/403 checks |
| `postman/WMS.postman_collection.json` | Postman v2.1 collection (58 requests) implementing the key checks from `api-tests.md` with `pm.test` assertions |
| `bug-report-template.md` | Jira-style bug template + one example (format only, not a real defect) |
| `bug-reports.md` | BUG-01 – BUG-04: defects reproduced via the API on a local Docker build |
| `evidence/` | Request/response logs and API log excerpts for each bug |
| `open-questions.md` | Ambiguities found in the code |

## Running the Postman collection

1. Start the backend and database, and seed demo data:
   `docker compose exec backend dotnet Warehouse.Api.dll --seed-demo-data`
2. Import `postman/WMS.postman_collection.json` into Postman (File → Import).
3. Open the collection → **Variables** and set the current values:
   - `baseUrl` – API root without `/api` (default `http://localhost:5124`; behind nginx use the frontend origin, e.g. `http://localhost`)
   - `adminUsername` / `adminPassword` – an Admin account
   - `integrationUsername` / `integrationPassword` – an Integration account
   - `testUserPassword` – password used for the Worker/Brigadier accounts the run creates (must satisfy the ASP.NET Identity default policy: ≥ 6 chars, upper, lower, digit, symbol)
   - `conveyorBarcode` – a `ConveyorDrop` location (seeded: `HZA301`)
4. Run the whole collection with the **Collection Runner**, folders in order (00 → 07). Requests depend on variables saved by earlier requests.

How it works
- `PM-01 Login (Admin)` generates a new `runId` and saves the Admin JWT to the collection variable `token`. The collection uses **Bearer `{{token}}`** by default; requests that test other roles override it with `{{workerToken}}` or `{{integrationToken}}` (saved by the login requests in folder 00).
- Every run creates its own Worker and Brigadier users, product, shelf, containers and its own pick zone (`mq<runId>9`), so runs do not interfere with each other or with the seeded demo data.
- Runs create data in the database; use a test database, not production.
- Not covered by Postman: SignalR realtime behaviour (TC-31, TC-32) – test manually in two browsers.

## Traceability (feature → test cases)

| Feature | Test cases | API checks | Postman |
|---|---|---|---|
| Login / logout / invalid login | TC-01, TC-02, TC-03, TC-04 | API-01–03, API-12, API-13 | PM-01–03, PM-27, PM-28 |
| Role-based access | TC-05 (DT-1), TC-06, TC-07 (DT-2) | API-04–11, API-14–18 | PM-04, PM-05, PM-29–36 |
| Goods receipt | TC-08, TC-09, TC-10 | API-26, API-27, API-55, API-56 | PM-11, PM-12, PM-37, PM-38 |
| Putaway | TC-11, TC-12, TC-13, TC-14 | API-57–61 | PM-39–42 |
| Relocation | TC-15, TC-16 | API-62–66 | PM-43–46 |
| Order picking flow | TC-17, TC-18, TC-19, TC-20, TC-21 | API-38–46, API-49–54 | PM-20–25g |
| Shortages / stock discrepancies | TC-22, TC-23, TC-24, TC-25 (DT-3) | API-30, API-33, API-47, API-48 | PM-17, PM-25c, PM-25d, PM-25h |
| Order status transitions | TC-17, TC-22, TC-23, TC-26 | API-31, API-33, API-36, API-37 | PM-15, PM-17, PM-25h, PM-26 |
| Pick-task status transitions | TC-27, TC-28 | API-39–53 | PM-21–25g |
| Input validation | TC-09, TC-10, TC-13, TC-16, TC-20, TC-29, TC-30 | API-23, API-28, API-32, API-44 | PM-08, PM-13, PM-16, PM-24 |
| Realtime (SignalR) | TC-31, TC-32 | API-19, API-20 | – (manual) |
| Exploratory | EX-01, EX-02, EX-03 | – | – |

## Status workflow

`Status` in `test-cases.md` starts as **Not run**. During execution set it to **Passed / Failed / Blocked** and write the observed behaviour into **Actual result**. For every Failed case, raise a bug using `bug-report-template.md` and reference the test case ID.
