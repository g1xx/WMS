# BUG-03 evidence
Run: 2026-10-15 18:05 UTC | build: local source 241304b (docker image wmsqa-backend:local) | API http://127.0.0.1:5125 | run id mhx3e


## Step 1: Integration creates an order for a productId that does not exist

> POST /api/Orders
> body: {"customerName": "QA", "destinationAddress": "QA street 1", "items": [{"productId": "11111111-1111-1111-1111-111111111111", "requiredQuantity": 1}]}
< 400
< Unknown product id(s): 11111111-1111-1111-1111-111111111111.


## Step 2: no order was created for that customer name in this run (orders list filtered)

Orders containing productId 11111111-...: 0


## API log (docker logs wmsqa_backend)

```
(no errors logged)
```