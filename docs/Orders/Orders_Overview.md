# Orders — Overview

This section groups together methods for **managing orders**: creating, closing (directly or by opposite), and retrieving their details (live and historical). Use it for the full trade lifecycle.

---

## 📂 Methods in this folder

* [ShowOpenedOrders.md](ShowOpenedOrders.md)
  Get full details of all currently opened orders (ticket, symbol, lots, P/L, etc.).

* [ShowOpenedOrderTickets.md](ShowOpenedOrderTickets.md)
  Lightweight list of **ticket IDs** for opened orders.

* [ShowOrdersHistory.md](ShowOrdersHistory.md)
  Retrieve **closed trades** within a specified time range (supports sorting).

* [ShowOrderSendExample.md](ShowOrderSendExample.md)
  Example of sending a market/pending order with volume, slippage, comment, magic.

* [CloseOrderExample.md](CloseOrderExample.md)
  Close (or delete) an order by its ticket and print server result/comment.

* [CloseByOrderExample.md](CloseByOrderExample.md)
  Close one order **by** another opposite-direction order (hedged close-by).

---

## ⚡ Example Workflow (service wrapper)

```csharp
// 1) Place an order (market or pending) — returns ticket in logs
await _service.ShowOrderSendExample("EURUSD");

// 2) Monitor active positions
await _service.ShowOpenedOrders();
await _service.ShowOpenedOrderTickets();

// 3) Option A: Close directly by ticket
await _service.CloseOrderExample(123456);

// 4) Option B: Close by opposite order (hedged accounts only)
await _service.CloseByOrderExample(123456, 654321);

// 5) Review historical performance for the past week
await _service.ShowOrdersHistory();
```

---

## ✅ Best Practices

1. **Fetch before you act.** Call `ShowOpenedOrders` (or `OpenedOrdersAsync`) before attempting modify/close to ensure the ticket is valid and still open.
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
