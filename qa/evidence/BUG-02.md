# BUG-02 evidence
Run: 2026-10-15 17:55 UTC | build: local source 8d498d4 (docker image wmsqa-backend:local) | API http://127.0.0.1:5125 | run id 5fx01


## Step 1: Integration creates an order with an empty item list

> POST /api/Orders
> body: {"customerName": "QA empty", "destinationAddress": "QA street 1", "items": []}
< 201
< {
  "order": {
    "id": "01a140b4-c9dd-7133-8fcb-06cdb35aba8e",
    "orderNumber": "ORD-20261015-B65617",
    "customerName": "QA empty",
    "destinationAddress": "QA street 1",
    "status": "Picking",
    "createdAt": "2026-10-15T17:55:52.4130491Z",
    "items": []
  },
  "isAllocated": true,
  "allocationMessage": null
}


## Step 2-4: read, pack, allocate

> GET /api/Orders/01a140b4-c9dd-7133-8fcb-06cdb35aba8e
< 200
< {
  "id": "01a140b4-c9dd-7133-8fcb-06cdb35aba8e",
  "orderNumber": "ORD-20261015-B65617",
  "customerName": "QA empty",
  "destinationAddress": "QA street 1",
  "status": "Picking",
  "createdAt": "2026-10-15T17:55:52.413049Z",
  "items": []
}

> POST /api/Orders/01a140b4-c9dd-7133-8fcb-06cdb35aba8e/pack
< 400
< There are no pick tasks for this order.

> POST /api/Orders/01a140b4-c9dd-7133-8fcb-06cdb35aba8e/allocate
< 409
< Order ORD-20261015-B65617 cannot be allocated: current status is Picking, expected New.
