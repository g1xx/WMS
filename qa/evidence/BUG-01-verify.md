# BUG-01 evidence
Run: 2026-10-15 18:05 UTC | build: local source 241304b (docker image wmsqa-backend:local) | API http://127.0.0.1:5125 | run id mhx3e

> POST /api/Inventory/products
> body: {"name": "QA product b1", "sku": "QA-MHX3E-B1", "price": 10, "weightKg": 1, "lengthCm": 10, "widthCm": 10, "heightCm": 10, "baseUnit": 0, "itemPerPackage": 1, "locationBarcode": "qa-mhx3e-b1-shelf", "sector": "qmhx3eb1", "warehouseCode": "m", "floor": 9, "initialQuantity": 3}
< 200
< {
  "id": "01a140bd-b397-7291-bfec-6e36813f479c",
  "name": "QA product b1",
  "sku": "QA-MHX3E-B1",
  "sizeCategory": "S",
  "stocks": [
    {
      "productId": "01a140bd-b397-7291-bfec-6e36813f479c",
      "locationBarcode": "qa-mhx3e-b1-shelf",
      "quantity": 3
    }
  ]
}

> POST /api/Containers
> body: {"barcode": "QA-MHX3E-B1-C", "type": "Tote", "status": "Available"}
< 201
< {
  "id": "01a140bd-b45f-7777-b0ac-43cfacce78a8",
  "barcode": "QA-MHX3E-B1-C",
  "type": "Tote",
  "status": "Available",
  "maxWeightCapacityKg": 0,
  "locationId": null,
  "location": null,
  "stocks": [],
  "assignedSector": null
}


## Precondition: product stocked only at its shelf (physical 3); order for 3 units

> GET /api/Info/location/qa-mhx3e-b1-shelf
< 200
< {
  "barcode": "qa-mhx3e-b1-shelf",
  "type": "Shelf",
  "sector": "qmhx3eb1",
  "zoneCode": "mqmhx3eb19",
  "items": [
    {
      "productSku": "QA-MHX3E-B1",
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
> body: {"customerName": "QA BUG-01", "destinationAddress": "QA street 1", "items": [{"productId": "01a140bd-b397-7291-bfec-6e36813f479c", "requiredQuantity": 3}]}
< 201
< {
  "order": {
    "id": "01a140bd-b586-74bc-abce-931af77f0ccc",
    "orderNumber": "ORD-20261015-B8D982",
    "customerName": "QA BUG-01",
    "destinationAddress": "QA street 1",
    "status": "Picking",
    "createdAt": "2026-10-15T18:05:37.0285896Z",
    "items": [
      {
        "id": "01a140bd-b59d-76d9-9834-467565eca143",
        "orderId": "01a140bd-b586-74bc-abce-931af77f0ccc",
        "order": null,
        "productId": "01a140bd-b397-7291-bfec-6e36813f479c",
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

> GET /api/PickTask/next?sector=mqmhx3eb19
< 200
< {
  "id": "01a140bd-b61c-7158-82d5-12031cef76e5",
  "sector": "mqmhx3eb19",
  "status": "New",
  "containerBarcode": null,
  "assignedWorkerId": "01a140bd-b1bc-7555-a4dc-e4352f8cc4f3",
  "items": [
    {
      "id": "01a140bd-b63c-78f5-b8ac-6af3be58b7c3",
      "locationBarcode": "qa-mhx3e-b1-shelf",
      "productName": "QA product b1",
      "productSku": "QA-MHX3E-B1",
      "requiredQuantity": 3,
      "pickedQuantity": 0,
      "missingQuantity": 0,
      "availableStock": 0
    }
  ]
}

> POST /api/PickTask/01a140bd-b61c-7158-82d5-12031cef76e5/start
> body: {"containerBarcode": "QA-MHX3E-B1-C"}
< 200
< Picking successfully started. Container linked, task locked to you.


## Step 1: Admin cycle-count correction -3 with confirmReservationImpact=true

> POST /api/Inventory/adjust-stock
> body: {"productId": "01a140bd-b397-7291-bfec-6e36813f479c", "locationBarcode": "qa-mhx3e-b1-shelf", "quantityDelta": -3, "reason": "cycle count: shelf empty", "confirmReservationImpact": true}
< 200
< {
  "productId": "01a140bd-b397-7291-bfec-6e36813f479c",
  "locationBarcode": "qa-mhx3e-b1-shelf",
  "quantityDelta": -3,
  "newPhysicalQuantity": 0,
  "reason": "cycle count: shelf empty",
  "reservedQuantityReduced": 3
}

> GET /api/Info/location/qa-mhx3e-b1-shelf
< 200
< {
  "barcode": "qa-mhx3e-b1-shelf",
  "type": "Shelf",
  "sector": "qmhx3eb1",
  "zoneCode": "mqmhx3eb19",
  "items": [],
  "distinctSkuCount": 0,
  "maxDistinctSkus": 3
}


## Step 2-3: Worker obtains supervisor override with Brigadier badge, reports 3 missing

> POST /api/Auth/supervisor-override
> body: {"badgeBarcode": "01a140bd-b2cf-726b-9404-8c9b6d1fd514"}
< 200
< {
  "token": "<JWT redacted>"
}

> POST /api/PickTask/01a140bd-b61c-7158-82d5-12031cef76e5/report-missing
> body: {"locationBarcode": "qa-mhx3e-b1-shelf", "productSku": "QA-MHX3E-B1", "missingQuantity": 3}
< 200
< {
  "message": "3 unit(s) marked missing for this item. 3 unit(s) could not be sourced from active picking zones and were marked pending replenishment."
}


## Follow-up: task, pick, dispatch and order after the shortage report

> GET /api/PickTask/active
< 200
< {
  "id": "01a140bd-b61c-7158-82d5-12031cef76e5",
  "sector": "mqmhx3eb19",
  "status": "InProgress",
  "containerBarcode": "QA-MHX3E-B1-C",
  "assignedWorkerId": "01a140bd-b1bc-7555-a4dc-e4352f8cc4f3",
  "items": [
    {
      "id": "01a140bd-b63c-78f5-b8ac-6af3be58b7c3",
      "locationBarcode": "qa-mhx3e-b1-shelf",
      "productName": "QA product b1",
      "productSku": "QA-MHX3E-B1",
      "requiredQuantity": 3,
      "pickedQuantity": 0,
      "missingQuantity": 3,
      "availableStock": 0
    }
  ]
}

> POST /api/PickTask/01a140bd-b61c-7158-82d5-12031cef76e5/pick
> body: {"locationBarcode": "qa-mhx3e-b1-shelf", "productSku": "QA-MHX3E-B1", "quantity": 1}
< 400
< Over-pick! You only need to pick 0 more units of this item.

> POST /api/PickTask/01a140bd-b61c-7158-82d5-12031cef76e5/dispatch
> body: {"containerBarcode": "QA-MHX3E-B1-C", "conveyorBarcode": "HZA301"}
< 200
< {
  "message": "Container successfully verified and sent to the conveyor.",
  "nextTaskId": null
}

> GET /api/Info/container/QA-MHX3E-B1-C
> note: sent at 18:12 UTC, after the run. The container was not touched after the HZA301 dispatch (the only later call, cancel, returned 400).
< 200
< {
  "barcode": "QA-MHX3E-B1-C",
  "type": "Tote",
  "status": "Available",
  "locationBarcode": null,
  "assignedSector": null,
  "linkedTasks": [],
  "contentSections": [
    {
      "kind": "Empty",
      "lines": [],
      "sourceTaskId": null,
      "sector": null,
      "isHistorical": false
    }
  ]
}

> GET /api/Orders/01a140bd-b586-74bc-abce-931af77f0ccc
< 200
< {
  "id": "01a140bd-b586-74bc-abce-931af77f0ccc",
  "orderNumber": "ORD-20261015-B8D982",
  "customerName": "QA BUG-01",
  "destinationAddress": "QA street 1",
  "status": "ShortShipped",
  "createdAt": "2026-10-15T18:05:37.028589Z",
  "items": [
    {
      "id": "01a140bd-b59d-76d9-9834-467565eca143",
      "orderId": "01a140bd-b586-74bc-abce-931af77f0ccc",
      "order": null,
      "productId": "01a140bd-b397-7291-bfec-6e36813f479c",
      "product": null,
      "requiredQuantity": 3,
      "pickedQuantity": 0,
      "shortedQuantity": 3,
      "isPendingReplenishment": true
    }
  ]
}


## Follow-up: cancel, then ask for the next task in the zone

> POST /api/PickTask/01a140bd-b61c-7158-82d5-12031cef76e5/cancel
< 400
< Only a task that is in progress can be cancelled.

> GET /api/PickTask/next?sector=mqmhx3eb19
< 204
< null


## API log (docker logs wmsqa_backend)

```
(no errors logged)
```