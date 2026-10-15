# BUG-04 evidence
Run: 2026-10-15 18:05 UTC | build: local source 241304b (docker image wmsqa-backend:local) | API http://127.0.0.1:5125 | run id mhx3e

> POST /api/Inventory/products
> body: {"name": "QA product b4", "sku": "QA-MHX3E-B4", "price": 10, "weightKg": 1, "lengthCm": 10, "widthCm": 10, "heightCm": 10, "baseUnit": 0, "itemPerPackage": 1, "locationBarcode": "qa-mhx3e-b4-shelf", "sector": "qmhx3eb4", "warehouseCode": "m", "floor": 9, "initialQuantity": 5}
< 200
< {
  "id": "01a140bd-ba06-79f3-9dfb-8b5eb1ef08da",
  "name": "QA product b4",
  "sku": "QA-MHX3E-B4",
  "sizeCategory": "S",
  "stocks": [
    {
      "productId": "01a140bd-ba06-79f3-9dfb-8b5eb1ef08da",
      "locationBarcode": "qa-mhx3e-b4-shelf",
      "quantity": 5
    }
  ]
}

> POST /api/Containers
> body: {"barcode": "QA-MHX3E-B4-C", "type": "Tote", "status": "Available"}
< 201
< {
  "id": "01a140bd-ba10-7754-b0ea-ad454fecebd0",
  "barcode": "QA-MHX3E-B4-C",
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
> body: {"customerName": "QA BUG-04", "destinationAddress": "QA street 1", "items": [{"productId": "01a140bd-ba06-79f3-9dfb-8b5eb1ef08da", "requiredQuantity": 1}]}
< 201
< {
  "order": {
    "id": "01a140bd-ba19-7ec3-a61d-23c94e44d7b0",
    "orderNumber": "ORD-20261015-680469",
    "customerName": "QA BUG-04",
    "destinationAddress": "QA street 1",
    "status": "Picking",
    "createdAt": "2026-10-15T18:05:38.2019018Z",
    "items": [
      {
        "id": "01a140bd-ba1a-7684-8929-56cbfa643f11",
        "orderId": "01a140bd-ba19-7ec3-a61d-23c94e44d7b0",
        "order": null,
        "productId": "01a140bd-ba06-79f3-9dfb-8b5eb1ef08da",
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

> GET /api/PickTask/next?sector=mqmhx3eb49
< 200
< {
  "id": "01a140bd-ba1f-78c5-b9ab-8223c44a5f57",
  "sector": "mqmhx3eb49",
  "status": "New",
  "containerBarcode": null,
  "assignedWorkerId": "01a140bd-b1bc-7555-a4dc-e4352f8cc4f3",
  "items": [
    {
      "id": "01a140bd-ba1f-762b-80e1-33512bc84ec5",
      "locationBarcode": "qa-mhx3e-b4-shelf",
      "productName": "QA product b4",
      "productSku": "QA-MHX3E-B4",
      "requiredQuantity": 1,
      "pickedQuantity": 0,
      "missingQuantity": 0,
      "availableStock": 4
    }
  ]
}

> POST /api/PickTask/01a140bd-ba1f-78c5-b9ab-8223c44a5f57/start
> body: {"containerBarcode": "QA-MHX3E-B4-C"}
< 200
< Picking successfully started. Container linked, task locked to you.

> POST /api/PickTask/01a140bd-ba1f-78c5-b9ab-8223c44a5f57/pick
> body: {"locationBarcode": "qa-mhx3e-b4-shelf", "productSku": "QA-MHX3E-B4", "quantity": 1}
< 200
< Successfully picked: 1 units.


## Step 3: dispatch with a SHELF barcode in the conveyor field

> GET /api/Info/location/qa-mhx3e-b4-shelf
< 200
< {
  "barcode": "qa-mhx3e-b4-shelf",
  "type": "Shelf",
  "sector": "qmhx3eb4",
  "zoneCode": "mqmhx3eb49",
  "items": [
    {
      "productSku": "QA-MHX3E-B4",
      "productName": "QA product b4",
      "physicalQuantity": 4,
      "reservedQuantity": 0,
      "availableQuantity": 4
    }
  ],
  "distinctSkuCount": 1,
  "maxDistinctSkus": 3
}

> POST /api/PickTask/01a140bd-ba1f-78c5-b9ab-8223c44a5f57/dispatch
> body: {"containerBarcode": "QA-MHX3E-B4-C", "conveyorBarcode": "qa-mhx3e-b4-shelf"}
< 400
< Location 'qa-mhx3e-b4-shelf' is not a conveyor (it is a Shelf). Scan a conveyor barcode.


## Step 3b: dispatch the same container to a real conveyor (HZA301)

> POST /api/PickTask/01a140bd-ba1f-78c5-b9ab-8223c44a5f57/dispatch
> body: {"containerBarcode": "QA-MHX3E-B4-C", "conveyorBarcode": "HZA301"}
< 200
< {
  "message": "Container successfully verified and sent to the conveyor.",
  "nextTaskId": null
}


## Step 4: container and order after dispatch

> GET /api/Info/container/QA-MHX3E-B4-C
< 200
< {
  "barcode": "QA-MHX3E-B4-C",
  "type": "Tote",
  "status": "Ready",
  "locationBarcode": "HZA301",
  "assignedSector": null,
  "linkedTasks": [],
  "contentSections": [
    {
      "kind": "AsDispatched",
      "lines": [
        {
          "productSku": "QA-MHX3E-B4",
          "productName": "QA product b4",
          "quantity": 1
        }
      ],
      "sourceTaskId": "01a140bd-ba1f-78c5-b9ab-8223c44a5f57",
      "sector": "mqmhx3eb49",
      "isHistorical": true
    }
  ]
}

> GET /api/Orders/01a140bd-ba19-7ec3-a61d-23c94e44d7b0
< 200
< {
  "id": "01a140bd-ba19-7ec3-a61d-23c94e44d7b0",
  "orderNumber": "ORD-20261015-680469",
  "customerName": "QA BUG-04",
  "destinationAddress": "QA street 1",
  "status": "Packed",
  "createdAt": "2026-10-15T18:05:38.201901Z",
  "items": [
    {
      "id": "01a140bd-ba1a-7684-8929-56cbfa643f11",
      "orderId": "01a140bd-ba19-7ec3-a61d-23c94e44d7b0",
      "order": null,
      "productId": "01a140bd-ba06-79f3-9dfb-8b5eb1ef08da",
      "product": null,
      "requiredQuantity": 1,
      "pickedQuantity": 1,
      "shortedQuantity": 0,
      "isPendingReplenishment": false
    }
  ]
}
