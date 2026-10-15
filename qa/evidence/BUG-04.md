# BUG-04 evidence
Run: 2026-10-15 17:55 UTC | build: local source 8d498d4 (docker image wmsqa-backend:local) | API http://127.0.0.1:5125 | run id 5fx01

> POST /api/Inventory/products
> body: {"name": "QA product b4", "sku": "QA-5FX01-B4", "price": 10, "weightKg": 1, "lengthCm": 10, "widthCm": 10, "heightCm": 10, "baseUnit": 0, "itemPerPackage": 1, "locationBarcode": "qa-5fx01-b4-shelf", "sector": "q5fx01b4", "warehouseCode": "m", "floor": 9, "initialQuantity": 5}
< 200
< {
  "id": "01a140b4-ca89-768f-be0a-f21e1e58714f",
  "name": "QA product b4",
  "sku": "QA-5FX01-B4",
  "sizeCategory": "S",
  "stocks": [
    {
      "productId": "01a140b4-ca89-768f-be0a-f21e1e58714f",
      "locationBarcode": "qa-5fx01-b4-shelf",
      "quantity": 5
    }
  ]
}

> POST /api/Containers
> body: {"barcode": "QA-5FX01-B4-C", "type": "Tote", "status": "Available"}
< 201
< {
  "id": "01a140b4-ca92-7e83-9e86-d303c319c19a",
  "barcode": "QA-5FX01-B4-C",
  "type": "Tote",
  "status": "Available",
  "maxWeightCapacityKg": 0,
  "locationId": null,
  "location": null,
  "stocks": [],
  "assignedSector": null
}


## Precondition: order for 1 unit; Worker starts the task and picks 1

> POST /api/Orders
> body: {"customerName": "QA BUG-04", "destinationAddress": "QA street 1", "items": [{"productId": "01a140b4-ca89-768f-be0a-f21e1e58714f", "requiredQuantity": 1}]}
< 201
< {
  "order": {
    "id": "01a140b4-ca99-7f81-8d87-fb4b6ae86084",
    "orderNumber": "ORD-20261015-6D0C40",
    "customerName": "QA BUG-04",
    "destinationAddress": "QA street 1",
    "status": "Picking",
    "createdAt": "2026-10-15T17:55:52.601314Z",
    "items": [
      {
        "id": "01a140b4-ca9a-7d02-bba0-c0444b4178ae",
        "orderId": "01a140b4-ca99-7f81-8d87-fb4b6ae86084",
        "order": null,
        "productId": "01a140b4-ca89-768f-be0a-f21e1e58714f",
        "product": null,
        "requiredQuantity": 1,
        "pickedQuantity": 0,
        "shortedQuantity": 0,
        "isPendingReplenishment": false
      }
    ]
  },
  "isAllocated": true,
  "allocationMessage": null
}

> GET /api/PickTask/next?sector=mq5fx01b49
< 200
< {
  "id": "01a140b4-caa1-79de-a7bb-4ea7e4b41fdd",
  "sector": "mq5fx01b49",
  "status": "New",
  "containerBarcode": null,
  "assignedWorkerId": "01a140b4-c225-79d6-8d69-95dcfb59db19",
  "items": [
    {
      "id": "01a140b4-caa2-738a-80b2-66851c2cdb41",
      "locationBarcode": "qa-5fx01-b4-shelf",
      "productName": "QA product b4",
      "productSku": "QA-5FX01-B4",
      "requiredQuantity": 1,
      "pickedQuantity": 0,
      "missingQuantity": 0,
      "availableStock": 4
    }
  ]
}

> POST /api/PickTask/01a140b4-caa1-79de-a7bb-4ea7e4b41fdd/start
> body: {"containerBarcode": "QA-5FX01-B4-C"}
< 200
< Picking successfully started. Container linked, task locked to you.

> POST /api/PickTask/01a140b4-caa1-79de-a7bb-4ea7e4b41fdd/pick
> body: {"locationBarcode": "qa-5fx01-b4-shelf", "productSku": "QA-5FX01-B4", "quantity": 1}
< 200
< Successfully picked: 1 units.


## Step 3: dispatch with a SHELF barcode in the conveyor field

> GET /api/Info/location/qa-5fx01-b4-shelf
< 200
< {
  "barcode": "qa-5fx01-b4-shelf",
  "type": "Shelf",
  "sector": "q5fx01b4",
  "zoneCode": "mq5fx01b49",
  "items": [
    {
      "productSku": "QA-5FX01-B4",
      "productName": "QA product b4",
      "physicalQuantity": 4,
      "reservedQuantity": 0,
      "availableQuantity": 4
    }
  ],
  "distinctSkuCount": 1,
  "maxDistinctSkus": 3
}

> POST /api/PickTask/01a140b4-caa1-79de-a7bb-4ea7e4b41fdd/dispatch
> body: {"containerBarcode": "QA-5FX01-B4-C", "conveyorBarcode": "qa-5fx01-b4-shelf"}
< 200
< {
  "message": "Container successfully verified and sent to the conveyor.",
  "nextTaskId": null
}


## Step 4: container and order after dispatch

> GET /api/Info/container/QA-5FX01-B4-C
< 200
< {
  "barcode": "QA-5FX01-B4-C",
  "type": "Tote",
  "status": "Ready",
  "locationBarcode": "qa-5fx01-b4-shelf",
  "assignedSector": null,
  "linkedTasks": [],
  "contentSections": [
    {
      "kind": "AsDispatched",
      "lines": [
        {
          "productSku": "QA-5FX01-B4",
          "productName": "QA product b4",
          "quantity": 1
        }
      ],
      "sourceTaskId": "01a140b4-caa1-79de-a7bb-4ea7e4b41fdd",
      "sector": "mq5fx01b49",
      "isHistorical": true
    }
  ]
}

> GET /api/Orders/01a140b4-ca99-7f81-8d87-fb4b6ae86084
< 200
< {
  "id": "01a140b4-ca99-7f81-8d87-fb4b6ae86084",
  "orderNumber": "ORD-20261015-6D0C40",
  "customerName": "QA BUG-04",
  "destinationAddress": "QA street 1",
  "status": "Packed",
  "createdAt": "2026-10-15T17:55:52.601314Z",
  "items": [
    {
      "id": "01a140b4-ca9a-7d02-bba0-c0444b4178ae",
      "orderId": "01a140b4-ca99-7f81-8d87-fb4b6ae86084",
      "order": null,
      "productId": "01a140b4-ca89-768f-be0a-f21e1e58714f",
      "product": null,
      "requiredQuantity": 1,
      "pickedQuantity": 1,
      "shortedQuantity": 0,
      "isPendingReplenishment": false
    }
  ]
}
