# MT4Service — Trading Methods

> Place, modify and close orders. These build an explicit request object; for one-liners (market buy/sell, SL/TP in points, risk sizing, bulk close) use **[MT4Sugar](../MT4Sugar/MT4Sugar.Overview.md)**.

→ builds on low-level **[Orders — Overview](../MT5Account/Orders/Orders_Overview.md)**

> ⚠️ These send **real orders** — guard them in examples/strategies.

---

## 📋 Methods

| Method | Returns | Low-level spec |
| ------ | ------- | -------------- |
| `OrderSendAsync(OrderSendRequest request, …)` | `OrderSendData` | [OrderSend](../MT5Account/Orders/OrderSend.md) |
| `OrderModifyAsync(OrderModifyRequest request, …)` | `OrderModifyData` | [OrderModify](../MT5Account/Orders/ModifyOrder.md) |
| `OrderCloseDeleteAsync(OrderCloseDeleteRequest request, …)` | `OrderCloseDeleteData` | [CloseOrder](../MT5Account/Orders/CloseOrder.md) |
| `OrderCloseByAsync(OrderCloseByRequest request, …)` | `OrderCloseByData` | [CloseByOrder](../MT5Account/Orders/CloseByOrder.md) |

* `OrderSendRequest.OperationType` is `OrderSendOperationType` — `OcOpBuy`/`OcOpSell` (market) or `OcOpBuylimit`/`OcOpBuystop`/`OcOpSelllimit`/`OcOpSellstop` (pending).
* `OrderCloseDeleteData.Mode` is `OrderCloseDeleteMode` (`OcdMarketOrder` closed / `OcdPendingOrder` deleted).

---

## 💡 Usage examples

### 1) Market BUY

```csharp
var sent = await service.OrderSendAsync(new OrderSendRequest {
    Symbol = "EURUSD", OperationType = OrderSendOperationType.OcOpBuy,
    Volume = 0.10, Slippage = 5, Comment = "demo"
});
Console.WriteLine($"opened #{sent.Ticket} @ {sent.Price}");
```

### 2) Pending BUY LIMIT

```csharp
await service.OrderSendAsync(new OrderSendRequest {
    Symbol = "EURUSD", OperationType = OrderSendOperationType.OcOpBuylimit,
    Volume = 0.10, Price = 1.10500, Slippage = 5
});
```

### 3) Modify SL/TP, then close

```csharp
await service.OrderModifyAsync(new OrderModifyRequest {
    OrderTicket = sent.Ticket, NewStopLoss = 1.09500, NewTakeProfit = 1.10500 });

var res = await service.OrderCloseDeleteAsync(new OrderCloseDeleteRequest { OrderTicket = sent.Ticket });
Console.WriteLine($"{res.Mode}: {res.HistoryOrderComment}");
```

### 4) Close one order by an opposite one (hedged accounts)

```csharp
await service.OrderCloseByAsync(new OrderCloseByRequest {
    TicketToClose = 123456, OppositeTicketClosingBy = 654321 });
```

---

## 🧩 Notes & Tips

* **Normalize first.** Round price to `GetDigitsAsync`, snap volume to `GetVolumeStepAsync`, respect `GetVolumeMinAsync`.
* **Stops distance.** Brokers enforce a minimum SL/TP distance; too-tight stops are rejected.
* **Prefer Sugar for common cases.** `BuyMarketAsync`, `BuyMarketPointsAsync`, `ModifySlTpAsync`, `CloseAllMarketAsync`, `BuyMarketByRiskAsync` build these requests for you.
