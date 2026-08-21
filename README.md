# WMS

A warehouse management system for picking, packing, and dispatching orders — the workflow a warehouse worker and their supervisor use to get an order from "allocated" to "on the truck," including what happens when an item on the shelf turns out not to be there.

## Why this is interesting

The most interesting bug in this codebase was a distributed-state bug, not a CRUD bug. There were two ways for a worker to report a problem with a pick: a defective item, or a missing item. Both looked like the same kind of "supervisor override" — same UI pattern, same idea of confirming a shortfall — but only the defect path actually fed back into the order and dispatch layer. The missing-item path set the pick task to `Completed` directly and stopped there. It never recorded a shortfall on the order, and never ran the logic that releases the container for dispatch. The result: an order with a genuinely missing item (as opposed to a defective one) could get stuck in `Picking` forever, with no terminal state and nothing in the UI explaining why. I unified both paths behind one handler (`IUnfulfillableUnitHandler`) that searches for replacement stock in the active picking zones only (not bulk/reserve storage — you can't send a picker there mid-route) and, if no replacement exists, writes the shortfall onto `OrderItem.ShortedQuantity`. Dispatch logic now checks that value, so an order that's short a unit reaches a real terminal state (`ShortShipped`) instead of hanging indefinitely.

Second thing worth knowing about: the frontend's API base URL was hardcoded to `http://localhost:5124/api`. It worked fine through every local test and demo, then broke immediately on the first real deployment, because the browser was no longer running on the same machine as the API. Fixed by switching to a relative `/api` path and letting nginx proxy it to the backend container — same-origin from the browser's point of view, no config needed per environment.

The app is deployed on an Azure VM behind Docker Compose and an nginx reverse proxy: nginx is the only container with a port open to the outside world, and the database and API are only reachable over the internal Docker network (or via loopback, for local debugging on the VM itself).

## Tech stack

**Backend**
- .NET 10 / ASP.NET Core Web API
- EF Core 10 + Npgsql (PostgreSQL)
- ASP.NET Core Identity + JWT bearer auth

**Frontend**
- React 19 + TypeScript
- Vite 8
- React Router 7
- TanStack React Query
- axios

**Infrastructure**
- PostgreSQL 16
- Docker Compose (multi-stage builds for both services)
- nginx (reverse proxy + static file serving)
- Deployed on an Azure Ubuntu VM

## Live demo

http://wms.polandcentral.cloudapp.azure.com

Login: `admin` / `AdminDemo123!`

To see the missing-item / short-shipment flow described above without setting it up by hand, open **Orders** and look for `ORD-DEMO-SHORTSHIP` — it's already been picked, short-reported, and dispatched, so you can see the resulting `ShortShipped` status and the per-line shorted quantity directly. There's also `ORD-DEMO-LIVE`, left unallocated on purpose, if you want to click through the normal allocate → pick → dispatch path yourself.

This is running on a free-tier VM and may go offline; the repo is the durable copy if the link is down.

## Running locally

```bash
git clone https://github.com/g1xx/WMS.git
cd WMS
cp .env.example .env   # set JWT_SECRET at minimum — see comments in the file
docker compose up --build
```

Frontend at `http://localhost`. On first run the database is empty — to populate it with the same demo data as the live deployment (roles, an admin user, products, stock, and the `ORD-DEMO-SHORTSHIP` walkthrough):

```bash
docker compose exec backend dotnet Warehouse.Api.dll --seed-demo-data
```

It's idempotent — safe to run again.

## Architecture

A request from the browser hits nginx on port 80, which serves the React build directly and proxies anything under `/api` to the backend container over the internal Docker network; the backend talks to Postgres, also internal-only. nginx being the single public entry point means the frontend and API are same-origin in production (no CORS needed there — the backend's CORS policy exists for local dev, where the Vite dev server and the API run on different ports) and means the database and backend are never directly reachable from outside the host, regardless of what a misconfigured firewall rule might allow.
