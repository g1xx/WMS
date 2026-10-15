# Bug Reports

Format: `bug-report-template.md`.

**Test environment for all reports.** Isolated Docker Compose stack (project `wmsqa`), built from local source at commit `8d498d4` (branch `dev`), image `wmsqa-backend:local`, `ASPNETCORE_ENVIRONMENT=Production`, PostgreSQL 16 (compose `db` service), fresh database with demo data seeded (`--seed-demo-data`). API `http://127.0.0.1:5125`. Run date: 2026-10-15, run id `5fx01`.

**How reproduced.** Through the REST API, using the exact requests and responses logged in `evidence/BUG-0x.md`. Steps written as terminal (UI) actions name the API call they trigger. These UI paths were not clicked through in a browser.

---

## BUG-01 – [Picking] "Item not found" returns HTTP 500 when the location's system stock is lower than the missing quantity

| Field | Value |
|---|---|
| Summary | [Picking] Reporting a missing item fails with HTTP 500 (check-constraint violation) when the location's physical stock in the system is already lower than the reported quantity, so the shortage can never be recorded |
| Environment | See header. Roles: Worker (picker), Brigadier (supervisor badge), Admin (stock adjustment) |
| Preconditions | 1. Product `QA-5FX01-B1` is stocked only at shelf `qa-5fx01-b1-shelf`: physical 3, reserved 0.<br>2. Order `ORD-20261015-B428EC` for 3 units is created and allocated, which reserves 3 at the shelf.<br>3. The Worker has claimed and started the pick task with container `QA-5FX01-B1-C` (status InProgress). Nothing has been picked yet. |
| Steps to reproduce | 1. Admin records a cycle count of an empty shelf: `POST /api/Inventory/adjust-stock` `{ "quantityDelta": -3, "reason": "cycle count: shelf empty", "confirmReservationImpact": true }`.<br>2. Worker: Escape → "❌ Item not found", quantity 3, scan the Brigadier badge. This calls `POST /api/Auth/supervisor-override` and then `POST /api/PickTask/{id}/report-missing` `{ "locationBarcode": "qa-5fx01-b1-shelf", "productSku": "QA-5FX01-B1", "missingQuantity": 3 }` with the elevated token. |
| Expected result | 200 with "3 unit(s) marked missing for this item. ...". The shortage is recorded on the task and order (written off, `shortedQuantity` 3). Physical stock stays at 0, not negative, and the task can be closed so the order resolves to ShortShipped. For comparison, "Defective / Damaged" caps the deducted quantity at the physical stock. |
| Actual result | Step 1: 200, `newPhysicalQuantity: 0`, `reservedQuantityReduced: 3`.<br>Step 2: override 200, then **report-missing 500** "An unexpected error occurred. Please try again or contact support." The API log shows `23514: new row for relation "Stocks" violates check constraint "CK_Stock_PhysicalQuantity_NonNegative"` and nothing is saved.<br>Afterwards: pick returns 400 "Only 0 unit(s) are physically on the shelf here.", and dispatch returns 400 "The container is empty — nothing has been picked into it. Cancel the task instead of closing the container." Cancel returns 200, but `GET /api/PickTask/next` hands the same task straight back (requiredQuantity 3, `availableStock: 0`). The order stays in **Picking** with `pickedQuantity` 0 and `shortedQuantity` 0, and cannot progress until someone adds stock back. |
| Severity | Major. The core shortage flow is blocked and the order is stuck. |
| Priority | High |
| Attachments | `evidence/BUG-01.md` (full request/response log and API log excerpt) |
| Linked test case | TC-23, TC-25 |
| Reproducibility | Always (1 of 1 run, deterministic) |
| Root cause (hint) | `PickTaskService.ReportMissingItemAsync` runs `stock.PhysicalQuantity -= dto.MissingQuantity` with no lower bound (`Backend/Warehouse.Application/Services/PickTaskService.cs:510`). The DB exception is not translated by `UnitOfWork.SaveChangesAsync`, so it becomes a 500. |
| Status | **Fixed in `6cd4fa2`, verified** (branch `fix/qa-bugs`, rebuilt `wmsqa` stack, run `mhx3e`, 2026-10-15). Same steps: report-missing returns 200 "3 unit(s) marked missing for this item. 3 unit(s) could not be sourced from active picking zones and were marked pending replenishment." Dispatch of the empty tote returns 200, and the order ends **ShortShipped** (`shortedQuantity` 3). No errors in the API log. Evidence: `evidence/BUG-01-verify.md`. Unit test: `ReportMissingItemAsync_SystemStockBelowMissingQuantity_CapsDeductionAtPhysicalStock`. |

---

## BUG-02 – [Orders] An order with an empty item list is accepted, moves to "Picking" and can never be completed

| Field | Value |
|---|---|
| Summary | [Orders] `POST /api/Orders` with `"items": []` returns 201 with `isAllocated: true`, and the order stays in status Picking forever with no pick tasks |
| Environment | See header. Role: Integration (`erp-feed`) |
| Preconditions | Integration token. |
| Steps to reproduce | 1. `POST /api/Orders` `{ "customerName": "QA empty", "destinationAddress": "QA street 1", "items": [] }`<br>2. `GET /api/Orders/{id}`<br>3. `POST /api/Orders/{id}/pack`<br>4. `POST /api/Orders/{id}/allocate` |
| Expected result | Step 1 is rejected with 400 and a validation message (an order needs at least one line). Putaway already does this: "At least one expected item is required." |
| Actual result | 1. **201**, `order.status: "Picking"`, `items: []`, `isAllocated: true`, order `ORD-20261015-B65617`.<br>2. 200, status "Picking", `items: []`.<br>3. 400 "There are no pick tasks for this order."<br>4. 409 "Order ORD-20261015-B65617 cannot be allocated: current status is Picking, expected New."<br>No API call can move the order out of Picking. |
| Severity | Major. Creates orphan orders that stay "in progress" forever and distort order status reporting. |
| Priority | Medium |
| Attachments | `evidence/BUG-02.md` |
| Linked test case | TC-29 (proposed new row: `items: []`), Q-05 |
| Reproducibility | Always (1 of 1 run) |
| Root cause (hint) | `OrdersController.cs:53` only checks `RequiredQuantity <= 0`, which an empty list passes. `OrderAllocationService.cs:118` sets `Picking` even when no picks were planned. |
| Status | **Fixed in `1d0b017`, verified** (branch `fix/qa-bugs`, rebuilt `wmsqa` stack, run `mhx3e`, 2026-10-15). Step 1 now returns **400** "An order must contain at least one item." and no order is created. Evidence: `evidence/BUG-02-verify.md`. Unit test: `OrdersControllerTests.CreateOrder_EmptyItems_Returns400AndPersistsNothing`. |

---

## BUG-03 – [Orders] Unknown productId in an order line returns HTTP 500 instead of a validation error

| Field | Value |
|---|---|
| Summary | [Orders] `POST /api/Orders` with a non-existent `productId` returns 500 "An unexpected error occurred" instead of 400/404 |
| Environment | See header. Role: Integration (`erp-feed`) |
| Preconditions | Integration token. The GUID `11111111-1111-1111-1111-111111111111` is not the ID of any product. |
| Steps to reproduce | 1. `POST /api/Orders` `{ "customerName": "QA", "destinationAddress": "QA street 1", "items": [ { "productId": "11111111-1111-1111-1111-111111111111", "requiredQuantity": 1 } ] }`<br>2. `GET /api/Orders` and search for that productId. |
| Expected result | 400 or 404 with a message naming the unknown product (similar to putaway: "Product with SKU '<SKU>' was not found."). |
| Actual result | 1. **500** "An unexpected error occurred. Please try again or contact support." The API log shows `23503: insert or update on table "OrderItems" violates foreign key constraint "FK_OrderItems_Products_ProductId"` followed by `Unhandled exception on POST /api/Orders`.<br>2. No order with that productId exists, so nothing was persisted. The caller gets no usable error text. |
| Severity | Major. Unhandled server error on the integration endpoint, and the caller cannot tell what is wrong. |
| Priority | Medium |
| Attachments | `evidence/BUG-03.md` (request/response and API log excerpt) |
| Linked test case | TC-29 (proposed new row: unknown productId), Q-05 |
| Reproducibility | Always (1 of 1 run) |
| Status | **Fixed in `4505031`, verified** (branch `fix/qa-bugs`, rebuilt `wmsqa` stack, run `mhx3e`, 2026-10-15). Step 1 now returns **400** "Unknown product id(s): 11111111-1111-1111-1111-111111111111." No order is persisted and nothing is logged as an error. Evidence: `evidence/BUG-03-verify.md`. Unit test: `OrdersControllerTests.CreateOrder_UnknownProductIds_Returns400NamingEveryUnknownId`. |

---

## BUG-04 – [Picking] Dispatch accepts a shelf barcode as the conveyor, so the loaded container is recorded on a shelf

| Field | Value |
|---|---|
| Summary | [Picking] Dispatch accepts any location barcode (here a Shelf) in the conveyor field, completes the task, and records the loaded container at that shelf |
| Environment | See header. Role: Worker |
| Preconditions | Product `QA-5FX01-B4` at shelf `qa-5fx01-b4-shelf` (type **Shelf**). Order `ORD-20261015-6D0C40` for 1 unit. The Worker has started the pick task with container `QA-5FX01-B4-C` and picked 1 unit. |
| Steps to reproduce | 1. Dispatch screen: "1. CONTAINER barcode" = `QA-5FX01-B4-C`, "2. CONVEYOR barcode" = `qa-5fx01-b4-shelf`, submit. This calls `POST /api/PickTask/{id}/dispatch` `{ "containerBarcode": "QA-5FX01-B4-C", "conveyorBarcode": "qa-5fx01-b4-shelf" }`.<br>2. "Informacja o..." → container `QA-5FX01-B4-C` (`GET /api/Info/container/QA-5FX01-B4-C`).<br>3. `GET /api/Orders/{id}` |
| Expected result | Step 1 is rejected with an error that the scanned location is not a conveyor. Only `ConveyorDrop` locations (e.g. `HZA301`) are accepted, and the task stays InProgress. |
| Actual result | 1. **200** "Container successfully verified and sent to the conveyor.", `nextTaskId: null`.<br>2. Container `status: "Ready"`, **`locationBarcode: "qa-5fx01-b4-shelf"`**, and its content shows 1 unit "AsDispatched".<br>3. Order status "Packed".<br>A scanning mistake is accepted silently, and the system records a loaded tote as sitting on a storage shelf. |
| Severity | Major. Wrong physical location recorded for a loaded container. |
| Priority | Medium |
| Attachments | `evidence/BUG-04.md` |
| Linked test case | TC-21 (proposed new row: conveyor = shelf barcode), Q-08 |
| Reproducibility | Always (1 of 1 run) |
| Root cause (hint) | The conveyor is looked up by barcode only, with no `LocationType` check (`PickTaskService.cs:275`). The container then takes that location (`PickTaskService.cs:304`). |
| Status | **Fixed in `241304b`, verified** (branch `fix/qa-bugs`, rebuilt `wmsqa` stack, run `mhx3e`, 2026-10-15). Dispatch with the shelf barcode returns **400** "Location 'qa-mhx3e-b4-shelf' is not a conveyor (it is a Shelf). Scan a conveyor barcode." and the task stays InProgress. A follow-up dispatch to `HZA301` returns 200, and the container is recorded at `HZA301`. Evidence: `evidence/BUG-04-verify.md`. Unit test: `DispatchContainerAsync_StationIsNotAConveyor_IsRejectedWithoutMovingAnything`. |
