# BUG-01 evidence
Run: 2026-10-15 17:55 UTC | build: local source 8d498d4 (docker image wmsqa-backend:local) | API http://127.0.0.1:5125 | run id 5fx01

> POST /api/Inventory/products
> body: {"name": "QA product b1", "sku": "QA-5FX01-B1", "price": 10, "weightKg": 1, "lengthCm": 10, "widthCm": 10, "heightCm": 10, "baseUnit": 0, "itemPerPackage": 1, "locationBarcode": "qa-5fx01-b1-shelf", "sector": "q5fx01b1", "warehouseCode": "m", "floor": 9, "initialQuantity": 3}
< 200
< {
  "id": "01a140b4-c3ca-7cb5-ad86-b87c39bc03a9",
  "name": "QA product b1",
  "sku": "QA-5FX01-B1",
  "sizeCategory": "S",
  "stocks": [
    {
      "productId": "01a140b4-c3ca-7cb5-ad86-b87c39bc03a9",
      "locationBarcode": "qa-5fx01-b1-shelf",
      "quantity": 3
    }
  ]
}

> POST /api/Containers
> body: {"barcode": "QA-5FX01-B1-C", "type": "Tote", "status": "Available"}
< 201
< {
  "id": "01a140b4-c4ad-7761-aa1c-03cc3c75ff4e",
  "barcode": "QA-5FX01-B1-C",
  "type": "Tote",
  "status": "Available",
  "maxWeightCapacityKg": 0,
  "locationId": null,
  "location": null,
  "stocks": [],
  "assignedSector": null
}


## Precondition: product stocked only at its shelf (physical 3); order for 3 units

> GET /api/Info/location/qa-5fx01-b1-shelf
< 200
< {
  "barcode": "qa-5fx01-b1-shelf",
  "type": "Shelf",
  "sector": "q5fx01b1",
  "zoneCode": "mq5fx01b19",
  "items": [
    {
      "productSku": "QA-5FX01-B1",
      "productName": "QA product b1",
      "physicalQuantity": 3,
      "reservedQuantity": 0,
      "availableQuantity": 3
    }
  ],
  "distinctSkuCount": 1,
  "maxDistinctSkus": 3
}

> POST /api/Orders
> body: {"customerName": "QA BUG-01", "destinationAddress": "QA street 1", "items": [{"productId": "01a140b4-c3ca-7cb5-ad86-b87c39bc03a9", "requiredQuantity": 3}]}
< 201
< {
  "order": {
    "id": "01a140b4-c586-78ea-82cd-9f4316dbeb15",
    "orderNumber": "ORD-20261015-B428EC",
    "customerName": "QA BUG-01",
    "destinationAddress": "QA street 1",
    "status": "Picking",
    "createdAt": "2026-10-15T17:55:51.2986786Z",
    "items": [
      {
        "id": "01a140b4-c5a1-71c8-bdeb-4eefd9d9b86c",
        "orderId": "01a140b4-c586-78ea-82cd-9f4316dbeb15",
        "order": null,
        "productId": "01a140b4-c3ca-7cb5-ad86-b87c39bc03a9",
        "product": null,
        "requiredQuantity": 3,
        "pickedQuantity": 0,
        "shortedQuantity": 0,
        "isPendingReplenishment": false
      }
    ]
  },
  "isAllocated": true,
  "allocationMessage": null
}


## Precondition: Worker claims and starts the pick task

> GET /api/PickTask/next?sector=mq5fx01b19
< 200
< {
  "id": "01a140b4-c661-75f5-b4cb-db552f9f31d1",
  "sector": "mq5fx01b19",
  "status": "New",
  "containerBarcode": null,
  "assignedWorkerId": "01a140b4-c225-79d6-8d69-95dcfb59db19",
  "items": [
    {
      "id": "01a140b4-c67e-7c6a-9592-5097f1a78df7",
      "locationBarcode": "qa-5fx01-b1-shelf",
      "productName": "QA product b1",
      "productSku": "QA-5FX01-B1",
      "requiredQuantity": 3,
      "pickedQuantity": 0,
      "missingQuantity": 0,
      "availableStock": 0
    }
  ]
}

> POST /api/PickTask/01a140b4-c661-75f5-b4cb-db552f9f31d1/start
> body: {"containerBarcode": "QA-5FX01-B1-C"}
< 200
< Picking successfully started. Container linked, task locked to you.


## Step 1: Admin cycle-count correction -3 with confirmReservationImpact=true

> POST /api/Inventory/adjust-stock
> body: {"productId": "01a140b4-c3ca-7cb5-ad86-b87c39bc03a9", "locationBarcode": "qa-5fx01-b1-shelf", "quantityDelta": -3, "reason": "cycle count: shelf empty", "confirmReservationImpact": true}
< 200
< {
  "productId": "01a140b4-c3ca-7cb5-ad86-b87c39bc03a9",
  "locationBarcode": "qa-5fx01-b1-shelf",
  "quantityDelta": -3,
  "newPhysicalQuantity": 0,
  "reason": "cycle count: shelf empty",
  "reservedQuantityReduced": 3
}

> GET /api/Info/location/qa-5fx01-b1-shelf
< 200
< {
  "barcode": "qa-5fx01-b1-shelf",
  "type": "Shelf",
  "sector": "q5fx01b1",
  "zoneCode": "mq5fx01b19",
  "items": [],
  "distinctSkuCount": 0,
  "maxDistinctSkus": 3
}


## Step 2-3: Worker obtains supervisor override with Brigadier badge, reports 3 missing

> POST /api/Auth/supervisor-override
> body: {"badgeBarcode": "01a140b4-c312-71bc-8d57-4c27eb4aaf27"}
< 200
< {
  "token": "<JWT redacted>"
}

> POST /api/PickTask/01a140b4-c661-75f5-b4cb-db552f9f31d1/report-missing
> body: {"locationBarcode": "qa-5fx01-b1-shelf", "productSku": "QA-5FX01-B1", "missingQuantity": 3}
< 500
< An unexpected error occurred. Please try again or contact support.


## Follow-up: state after the failed call (nothing saved)

> GET /api/PickTask/active
< 200
< {
  "id": "01a140b4-c661-75f5-b4cb-db552f9f31d1",
  "sector": "mq5fx01b19",
  "status": "InProgress",
  "containerBarcode": "QA-5FX01-B1-C",
  "assignedWorkerId": "01a140b4-c225-79d6-8d69-95dcfb59db19",
  "items": [
    {
      "id": "01a140b4-c67e-7c6a-9592-5097f1a78df7",
      "locationBarcode": "qa-5fx01-b1-shelf",
      "productName": "QA product b1",
      "productSku": "QA-5FX01-B1",
      "requiredQuantity": 3,
      "pickedQuantity": 0,
      "missingQuantity": 0,
      "availableStock": 0
    }
  ]
}

> POST /api/PickTask/01a140b4-c661-75f5-b4cb-db552f9f31d1/pick
> body: {"locationBarcode": "qa-5fx01-b1-shelf", "productSku": "QA-5FX01-B1", "quantity": 1}
< 400
< Only 0 unit(s) are physically on the shelf here.

> POST /api/PickTask/01a140b4-c661-75f5-b4cb-db552f9f31d1/dispatch
> body: {"containerBarcode": "QA-5FX01-B1-C", "conveyorBarcode": "HZA301"}
< 400
< The container is empty — nothing has been picked into it. Cancel the task instead of closing the container.

> GET /api/Orders/01a140b4-c586-78ea-82cd-9f4316dbeb15
< 200
< {
  "id": "01a140b4-c586-78ea-82cd-9f4316dbeb15",
  "orderNumber": "ORD-20261015-B428EC",
  "customerName": "QA BUG-01",
  "destinationAddress": "QA street 1",
  "status": "Picking",
  "createdAt": "2026-10-15T17:55:51.298678Z",
  "items": [
    {
      "id": "01a140b4-c5a1-71c8-bdeb-4eefd9d9b86c",
      "orderId": "01a140b4-c586-78ea-82cd-9f4316dbeb15",
      "order": null,
      "productId": "01a140b4-c3ca-7cb5-ad86-b87c39bc03a9",
      "product": null,
      "requiredQuantity": 3,
      "pickedQuantity": 0,
      "shortedQuantity": 0,
      "isPendingReplenishment": false
    }
  ]
}


## Follow-up: cancel is possible (nothing picked) but returns the same unpickable task to the queue

> POST /api/PickTask/01a140b4-c661-75f5-b4cb-db552f9f31d1/cancel
< 200
< {
  "message": "Pick task cancelled and returned to the queue."
}

> GET /api/PickTask/next?sector=mq5fx01b19
< 200
< {
  "id": "01a140b4-c661-75f5-b4cb-db552f9f31d1",
  "sector": "mq5fx01b19",
  "status": "New",
  "containerBarcode": null,
  "assignedWorkerId": "01a140b3-f9a4-73fa-afe9-f51d44fbaa02",
  "items": [
    {
      "id": "01a140b4-c67e-7c6a-9592-5097f1a78df7",
      "locationBarcode": "qa-5fx01-b1-shelf",
      "productName": "QA product b1",
      "productSku": "QA-5FX01-B1",
      "requiredQuantity": 3,
      "pickedQuantity": 0,
      "missingQuantity": 0,
      "availableStock": 0
    }
  ]
}


## API log (docker logs wmsqa_backend)

```
fail: Microsoft.EntityFrameworkCore.Update[10000]
       ---> Npgsql.PostgresException (0x80004005): 23514: new row for relation "Stocks" violates check constraint "CK_Stock_PhysicalQuantity_NonNegative"
          SqlState: 23514
          MessageText: new row for relation "Stocks" violates check constraint "CK_Stock_PhysicalQuantity_NonNegative"
       ---> Npgsql.PostgresException (0x80004005): 23514: new row for relation "Stocks" violates check constraint "CK_Stock_PhysicalQuantity_NonNegative"
          SqlState: 23514
          MessageText: new row for relation "Stocks" violates check constraint "CK_Stock_PhysicalQuantity_NonNegative"
fail: Warehouse.Api.Middleware.GlobalExceptionHandler[0]
      Unhandled exception on POST /api/PickTask/01a140b4-c661-75f5-b4cb-db552f9f31d1/report-missing
       ---> Npgsql.PostgresException (0x80004005): 23514: new row for relation "Stocks" violates check constraint "CK_Stock_PhysicalQuantity_NonNegative"
          SqlState: 23514
          MessageText: new row for relation "Stocks" violates check constraint "CK_Stock_PhysicalQuantity_NonNegative"
```