# BUG-03 evidence
Run: 2026-10-15 17:55 UTC | build: local source 8d498d4 (docker image wmsqa-backend:local) | API http://127.0.0.1:5125 | run id 5fx01


## Step 1: Integration creates an order for a productId that does not exist

> POST /api/Orders
> body: {"customerName": "QA", "destinationAddress": "QA street 1", "items": [{"productId": "11111111-1111-1111-1111-111111111111", "requiredQuantity": 1}]}
< 500
< An unexpected error occurred. Please try again or contact support.


## Step 2: no order was created for that customer name in this run (orders list filtered)

Orders containing productId 11111111-...: 0


## API log (docker logs wmsqa_backend)

```
fail: Microsoft.EntityFrameworkCore.Update[10000]
       ---> Npgsql.PostgresException (0x80004005): 23503: insert or update on table "OrderItems" violates foreign key constraint "FK_OrderItems_Products_ProductId"
          SqlState: 23503
          MessageText: insert or update on table "OrderItems" violates foreign key constraint "FK_OrderItems_Products_ProductId"
       ---> Npgsql.PostgresException (0x80004005): 23503: insert or update on table "OrderItems" violates foreign key constraint "FK_OrderItems_Products_ProductId"
          SqlState: 23503
          MessageText: insert or update on table "OrderItems" violates foreign key constraint "FK_OrderItems_Products_ProductId"
fail: Warehouse.Api.Middleware.GlobalExceptionHandler[0]
      Unhandled exception on POST /api/Orders
       ---> Npgsql.PostgresException (0x80004005): 23503: insert or update on table "OrderItems" violates foreign key constraint "FK_OrderItems_Products_ProductId"
          SqlState: 23503
          MessageText: insert or update on table "OrderItems" violates foreign key constraint "FK_OrderItems_Products_ProductId"
```