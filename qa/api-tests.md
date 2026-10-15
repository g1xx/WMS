# API Tests – WMS

Base URL: `<API_BASE_URL>` (local default from `launchSettings.json`: `http://localhost:5124`; behind nginx the same paths are served on the frontend origin).
All endpoints are under `/api/...`. Auth = `Authorization: Bearer <JWT>` obtained from `POST /api/Auth/login`.

General rules found in code
- Every endpoint requires an authenticated user unless marked `[AllowAnonymous]` (fallback policy in `Program.cs`). Only `POST /api/Auth/login` and `GET /api/Demo/help` are anonymous.
- Missing / invalid / expired token → **401**. Valid token with a role not listed → **403** (empty body).
- Business errors are returned as a **plain string body**: 400 (validation / bad request), 404 (not found), 409 (conflict). Unhandled exceptions → 500 "An unexpected error occurred. Please try again or contact support."; concurrency conflicts → 409 "This record was just changed by someone else. Please refresh and try again."
- JSON uses camelCase; enums are serialized as strings (e.g. `"status": "Picking"`).
- Role groups: **AnyStaff** = Worker, Brigadier, Admin. **BrigadierOrAdmin** = Brigadier, Admin. **Integration** = upstream feed only.
- Postman IDs (`PM-xx`) refer to requests in `postman/WMS.postman_collection.json`.

## 1. Auth – `/api/Auth`

| ID | Method & URL | Auth | Body | Expected status | Key response checks | PM |
|---|---|---|---|---|---|---|
| API-01 | POST `/api/Auth/login` | none | `{ "username": "<ADMIN_USER>", "password": "<ADMIN_PASSWORD>" }` | 200 | `token` is a non-empty JWT (3 dot-separated parts); valid 8 h | PM-01 |
| API-02 | POST `/api/Auth/login` | none | valid username, wrong password | 401 | body "Invalid username or password." | PM-02 |
| API-03 | POST `/api/Auth/login` | none | unknown username | 401 | same body as API-02 (no user enumeration) | PM-03 |
| API-04 | POST `/api/Auth/register` | Admin | `{ "username", "email", "password" }` | 200 | `message` = "Worker successfully created", `userBarcode` = GUID | PM-04 |
| API-05 | POST `/api/Auth/register` | Worker | same | 403 | – | PM-30 |
| API-06 | POST `/api/Auth/register` | Admin | weak password `abc` | 400 | array of Identity errors (`code`, `description`) | – |
| API-07 | POST `/api/Auth/register-brigadier` | Admin | `{ "username", "email", "password" }` | 200 | `message` = "Brigadier successfully created", `userBarcode` | PM-05 |
| API-08 | POST `/api/Auth/supervisor-override` | Worker | `{ "badgeBarcode": "<BRIGADIER_BADGE_ID>" }` | 200 | `token` present; token role = Brigadier; `exp` = issue time + 2 min | PM-34 |
| API-09 | POST `/api/Auth/supervisor-override` | Worker | `{ "badgeBarcode": "<WORKER_BADGE_ID>" }` | 403 | body "Invalid badge or missing permissions." | – |
| API-10 | POST `/api/Auth/supervisor-override` | Worker | `{ "badgeBarcode": "not-a-guid" }` | 401 | body "Invalid badge or missing permissions." | PM-35 |
| API-11 | POST `/api/Auth/supervisor-override` | Integration | any badge | 403 | – | – |

## 2. Security smoke (401 / 403)

| ID | Method & URL | Auth | Expected | PM |
|---|---|---|---|---|
| API-12 | GET `/api/Orders` | none | 401 | PM-27 |
| API-13 | GET `/api/Orders` | tampered JWT | 401 | PM-28 |
| API-14 | GET `/api/PickTask` | Integration | 403 | PM-31 |
| API-15 | GET `/api/Stocks` | Integration | 403 | PM-32 |
| API-16 | POST `/api/PutawayTask` | Admin | 403 (Integration only) | PM-33 |
| API-17 | GET `/api/Products/for-ordering` | Admin | 403 (Integration only) | PM-29 |
| API-18 | POST `/api/PickTask/{id}/report-missing` | Worker | 403 (BrigadierOrAdmin) | PM-36 |
| API-19 | POST `/hubs/picktasks/negotiate?negotiateVersion=1` | none | 401 | – |
| API-20 | POST `/hubs/picktasks/negotiate?negotiateVersion=1&access_token=<JWT>` | token in query string | 200, `connectionToken` present | – |

## 3. Products & Inventory – `/api/Products`, `/api/Inventory`, `/api/Stocks`

| ID | Method & URL | Auth | Body | Expected status | Key response checks | PM |
|---|---|---|---|---|---|---|
| API-21 | POST `/api/Inventory/products` | AnyStaff | `{ "name", "sku", "price", "weightKg", "lengthCm", "widthCm", "heightCm", "baseUnit": 0, "itemPerPackage": 1, "locationBarcode", "sector": "p", "warehouseCode": "m", "floor": 9, "initialQuantity": 10 }` | 200 | `id` (GUID), `sku`, `name`, `sizeCategory` | PM-06 |
| API-22 | POST `/api/Inventory/products` | AnyStaff | same SKU again | 409 | "A product with SKU '<SKU>' already exists." | PM-07 |
| API-23 | POST `/api/Inventory/products` | AnyStaff | `name` = "" | 400 | "Name and SKU are required." | PM-08 |
| API-24 | GET `/api/Products` | AnyStaff | – | 200 | array; each item has `id`, `name`, `sku`, `sizeCategory`, `stocks[]` (`locationBarcode`, `quantity` = physical − reserved) | PM-09 |
| API-25 | GET `/api/Products/for-ordering` | Integration | – | 200 | array of `{ id, sku, name, availableQuantity }`; no location data | PM-10 |
| API-26 | POST `/api/Stocks` | AnyStaff | `{ "productId", "locationBarcode", "quantity": 0 }` | 400 | "Quantity must be greater than zero." | PM-11 |
| API-27 | POST `/api/Stocks` | AnyStaff | unknown `locationBarcode` | 400 | "Location with barcode '<X>' was not found in the system." | PM-12 |
| API-28 | POST `/api/Inventory/adjust-stock` | AnyStaff | `{ "productId", "locationBarcode", "quantityDelta": 0, "reason": "x" }` | 400 | "Quantity delta must not be zero." | PM-13 |
| API-29 | POST `/api/Inventory/adjust-stock` | AnyStaff | `quantityDelta`: 5, `reason`: "QA cycle count" | 200 | `newPhysicalQuantity`, `quantityDelta` = 5, `reservedQuantityReduced` = 0 | PM-14 |
| API-30 | POST `/api/Inventory/adjust-stock` | AnyStaff | delta below reserved, `confirmReservationImpact`: false | 409 | message starts "This adjustment would take physical quantity to" | – |

## 4. Orders – `/api/Orders`

| ID | Method & URL | Auth | Body | Expected status | Key response checks | PM |
|---|---|---|---|---|---|---|
| API-31 | POST `/api/Orders` | any authenticated role | `{ "customerName", "destinationAddress", "items": [ { "productId", "requiredQuantity": 2 } ] }` | 201 | `order.id`, `order.orderNumber` (`ORD-yyyyMMdd-XXXXXX`), `isAllocated` = true, `order.status` = "Picking" | PM-15 |
| API-32 | POST `/api/Orders` | any authenticated role | `requiredQuantity`: 0 | 400 | "Required quantity must be greater than zero for every item." | PM-16 |
| API-33 | POST `/api/Orders` | any authenticated role | `requiredQuantity` > available stock | 201 | `isAllocated` = false; `allocationMessage` starts "Shortage detected."; `order.status` = "AwaitingReplenishment" | PM-17 |
| API-34 | GET `/api/Orders/{id}` | AnyStaff | – | 200 | `id`, `status`, `items[]` (`requiredQuantity`, `pickedQuantity`, `shortedQuantity`) | PM-18 |
| API-35 | GET `/api/Orders/{random GUID}` | AnyStaff | – | 404 | "Order with ID <GUID> not found." | PM-19 |
| API-36 | POST `/api/Orders/{id}/allocate` | AnyStaff | – | 409 when status ≠ New | "Order <NUMBER> cannot be allocated: current status is <S>, expected New." | PM-26 |
| API-37 | POST `/api/Orders/{id}/pack` | AnyStaff | – | 400 when status ≠ Picking | "Packing not possible. Order is not in the Picking status." | – |

## 5. Pick tasks – `/api/PickTask` (AnyStaff unless stated)

| ID | Method & URL | Body | Expected status | Key response checks | PM |
|---|---|---|---|---|---|
| API-38 | GET `/api/PickTask/next` (no `sector`) | – | 400 | "A sector is required to request the next task." | PM-20 |
| API-39 | GET `/api/PickTask/next?sector=mp9` | – | 200 (task) or 204 (no task) | `id`, `sector` = "mp9", `status` = "New", `assignedWorkerId` = caller, `items[]` (`locationBarcode`, `productSku`, `requiredQuantity`) | PM-21 |
| API-40 | GET `/api/PickTask/active` | – | 200 (task) or 204 (none) | caller's New-claimed or InProgress task | – |
| API-41 | POST `/api/PickTask/{id}/start` | `{ "containerBarcode": "QA-NOPE" }` | 400 | "Container with barcode 'QA-NOPE' not found." | PM-22 |
| API-42 | POST `/api/PickTask/{id}/start` | `{ "containerBarcode": "<FREE_CONTAINER>" }` | 200 | "Picking successfully started. Container linked, task locked to you." | PM-23 |
| API-43 | POST `/api/PickTask/{id}/start` | container already InProgress | 409 | "Container is currently InProgress, not Available — it may have just been taken by someone else." | – |
| API-44 | POST `/api/PickTask/{id}/pick` | `{ "locationBarcode", "productSku", "quantity": 0 }` | 400 | "Quantity must be greater than zero." | PM-24 |
| API-45 | POST `/api/PickTask/{id}/pick` | quantity > outstanding | 400 | "Over-pick! You only need to pick N more units of this item." | PM-25a |
| API-46 | POST `/api/PickTask/{id}/pick` | valid | 200 | "Successfully picked: N units." | PM-25b |
| API-47 | POST `/api/PickTask/{id}/report-missing` (BrigadierOrAdmin) | `{ "locationBarcode", "productSku", "missingQuantity" }` > outstanding | 400 | "Over-report! Only N more unit(s) are outstanding on this item." | PM-25c |
| API-48 | POST `/api/PickTask/{id}/report-missing` (BrigadierOrAdmin) | valid | 200 | `message` starts "N unit(s) marked missing for this item." | PM-25d |
| API-49 | POST `/api/PickTask/{id}/dispatch` | `{ "containerBarcode": "<OTHER>", "conveyorBarcode": "HZA301" }` | 400 | "Wrong container barcode! Scan the container linked to this task." | PM-25e |
| API-50 | POST `/api/PickTask/{id}/dispatch` | `{ "containerBarcode": "<LINKED>", "conveyorBarcode": "QA-NOPE" }` | 404 | "Conveyor 'QA-NOPE' was not found." | PM-25f |
| API-51 | POST `/api/PickTask/{id}/dispatch` | valid | 200 | `message` = "Container successfully verified and sent to the conveyor.", `nextTaskId` | PM-25g |
| API-52 | POST `/api/PickTask/{id}/cancel` | – (InProgress, nothing picked) | 200 | `message` = "Pick task cancelled and returned to the queue." | – |
| API-53 | POST `/api/PickTask/{id}/release` | – | 200 | `message` = "Task returned to the queue." or "No claim to release." | – |
| API-54 | POST `/api/PickTask/{random GUID}/pick` | valid body | 404 | "Task not found." | – |

## 6. Putaway – `/api/PutawayTask`

| ID | Method & URL | Auth | Body | Expected status | Key response checks | PM |
|---|---|---|---|---|---|---|
| API-55 | POST `/api/PutawayTask` | Integration | `{ "containerBarcode", "sector": "mp9", "items": [ { "productSku", "expectedQuantity": 3 } ] }` | 200 | `id`, `status` = "New", `sector`, `items[0].expectedQuantity` = 3 | PM-37 |
| API-56 | POST `/api/PutawayTask` | Integration | `items`: [] | 400 | "At least one expected item is required." | PM-38 |
| API-57 | POST `/api/PutawayTask/validate-container` | AnyStaff | `{ "containerBarcode", "sector": "mp1" }` (task is in mp9) | 200 | `isValid` = false, `containerSector` = "mp9", `message` = "This container is from sector mp9." | PM-39 |
| API-58 | POST `/api/PutawayTask/start` | AnyStaff | `{ "containerBarcode", "sector": "mp9" }` | 200 | `status` = "InProgress", `items[].suggestedLocations` | PM-40 |
| API-59 | POST `/api/PutawayTask/{id}/confirm-item` | AnyStaff | quantity > expected | 400 | "Over-scan! Only N more unit(s) are expected here." | PM-41 |
| API-60 | POST `/api/PutawayTask/{id}/confirm-item` | AnyStaff | `{ "locationBarcode", "productSku", "quantity": 3 }` | 200 | `status` = "Completed", `items[0].putAwayQuantity` = 3 | PM-42 |
| API-61 | POST `/api/PutawayTask/{id}/report-missing` | BrigadierOrAdmin | `{ "productSku", "missingQuantity": 0 }` | 400 | "Missing quantity must be greater than zero." | – |

## 7. Relocation – `/api/Relocation` (AnyStaff)

| ID | Method & URL | Body | Expected status | Key response checks | PM |
|---|---|---|---|---|---|
| API-62 | GET `/api/Relocation/state` | – | 200 | `transitBarcode`, `carriedItems[]`, `canExit` | PM-43 |
| API-63 | POST `/api/Relocation/take` | `{ "sourceLocationBarcode", "productSku", "quantity": 1 }` | 200 | `carriedItems` contains SKU; `canExit` = false | PM-44 |
| API-64 | POST `/api/Relocation/putaway` | `{ "targetLocationBarcode", "productSku", "quantity": 1 }` | 200 | `carriedItems` empty; `canExit` = true | PM-45 |
| API-65 | POST `/api/Relocation/take` | unknown `sourceLocationBarcode` | 404 | "Location '<X>' was not found." | PM-46 |
| API-66 | POST `/api/Relocation/putaway` | SKU not carried | 409 | "You aren't carrying any <SKU>." | – |

## 8. Info lookup – `/api/Info` (AnyStaff)

| ID | Method & URL | Expected status | Key response checks | PM |
|---|---|---|---|---|
| API-67 | GET `/api/Info/product/{sku}` | 200 | `sku`, `name`, `sizeCategory`, `locations[]` (`physicalQuantity`, `reservedQuantity`, `availableQuantity`), `carriedByWorkersQuantity` | PM-47 |
| API-68 | GET `/api/Info/product/QA-NOPE` | 404 | "Product 'QA-NOPE' was not found." | PM-48 |
| API-69 | GET `/api/Info/container/{barcode}` | 200 | `status`, `locationBarcode`, `linkedTasks[]`, `contentSections[]` | – |
| API-70 | GET `/api/Info/location/{barcode}` | 200 | `type`, `zoneCode`, `items[]`, `distinctSkuCount`, `maxDistinctSkus` | – |

## 9. Containers & Locations (AnyStaff)

| ID | Method & URL | Body | Expected status | Key response checks | PM |
|---|---|---|---|---|---|
| API-71 | POST `/api/Containers` | `{ "barcode": "<NEW>", "type": "Tote", "status": "Available" }` | 201 | `id`, `barcode`, `status` = "Available" | PM-06b |
| API-72 | GET `/api/Containers/free` | – | 200 | every item has `status` = "Available" | – |
| API-73 | GET `/api/Containers/by-barcode/QA-NOPE` | – | 404 | – | – |
| API-74 | POST `/api/Locations/bulk` | `[]` | 400 | "The list of locations cannot be empty." | – |
| API-75 | POST `/api/Containers/seed-mass-containers?count=0` | – | 400 | "Count must be greater than zero." | – |
