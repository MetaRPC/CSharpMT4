# Orders — Overview

This section groups together methods for **managing orders**: creating, closing (directly or by opposite), and retrieving their details (live and historical). Use it for the full trade lifecycle.

---

## 📂 Methods in this folder

* [OpenedOrders.md](OpenedOrders.md)
  Get full details of all currently opened orders (ticket, symbol, lots, P/L, etc.).

* [OpenedOrderTickets.md](OpenedOrderTickets.md)
  Lightweight list of **ticket IDs** for opened orders.

* [OrdersHistory.md](OrdersHistory.md)
  Retrieve **closed trades** within a specified time range (supports sorting).

* [OrderSend.md](OrderSend.md)
  Example of sending a market/pending order with volume, slippage, comment, magic.

* [ModifyOrder.md](ModifyOrder.md)
  Change an existing order's SL/TP, price or expiration by ticket.

* [CloseOrder.md](CloseOrder.md)
  Close (or delete) an order by its ticket and print server result/comment.

* [CloseByOrder.md](CloseByOrder.md)
  Close one order **by** another opposite-direction order (hedged close-by).

---

## ⚡ Example Workflow (service wrapper)

```csharp
// 1) Place an order (market or pending) -> returns OrderSendData with the ticket
var sent = await _service.OrderSendAsync(new OrderSendRequest {
    Symbol = "EURUSD", OperationType = OrderSendOperationType.OcOpBuy, Volume = 0.10, Slippage = 5 });

// 2) Monitor active positions
var opened  = await _service.OpenedOrdersAsync();
var tickets = await _service.OpenedOrdersTicketsAsync();

// 3) Option A: Close directly by ticket (sugar shortcut)
await _service.CloseByTicketAsync(sent.Ticket);
//    ...or raw: await _service.OrderCloseDeleteAsync(new OrderCloseDeleteRequest { OrderTicket = sent.Ticket });

// 4) Option B: Close by opposite order (hedged accounts only)
await _service.OrderCloseByAsync(new OrderCloseByRequest {
    TicketToClose = 123456, OppositeTicketClosingBy = 654321 });

// 5) Review historical performance for the past week
var history = await _service.OrdersHistoryAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
```

---

## ✅ Best Practices

1. **Fetch before you act.** Call `OpenedOrders` (or `OpenedOrdersAsync`) before attempting modify/close to ensure the ticket is valid and still open.
2. **CloseBy preconditions.** Both tickets must be open, have **equal volume**, and be in **opposite directions**. Some brokers/accounts may disallow CloseBy.
3. **Deadlines & cancellation.** Pass `deadline`/`CancellationToken` on long operations; the library applies sensible defaults but you can tighten or relax them.
4. **Error handling.** Catch `ApiExceptionMT4` and log broker/terminal error codes (e.g., invalid ticket, trade disabled, market closed, stops too close).
5. **Paging history.** If you pull long ranges from `OrdersHistoryAsync`, use paging (`page/itemsPerPage`) to avoid huge payloads.
6. **Respect symbol rules.** Use Market‑Info docs (Digits, VolumeMin/Max/Step, TradeMode) to validate orders before send/close.

---

## 🎯 Purpose

Provide a compact toolkit for the **order lifecycle**:

* Open → Monitor → Close/Delete → Audit.
* Simplify UI/CLI workflows and automation.
* Offer predictable timeouts, retries, and clear error surfaces.

---

👉 Use this overview as a **map** and follow links to each `.md` file for complete method details, parameters, enums, and pitfalls.
