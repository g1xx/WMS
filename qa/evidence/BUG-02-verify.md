# BUG-02 evidence
Run: 2026-10-15 18:05 UTC | build: local source 241304b (docker image wmsqa-backend:local) | API http://127.0.0.1:5125 | run id mhx3e


## Step 1: Integration creates an order with an empty item list

> POST /api/Orders
> body: {"customerName": "QA empty", "destinationAddress": "QA street 1", "items": []}
< 400
< An order must contain at least one item.


## Steps 2-4

Not applicable: step 1 was rejected with 400, so no order exists to read, pack or allocate.
