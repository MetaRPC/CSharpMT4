# Streaming — Overview

This section groups **real‑time streams** from MT4: price ticks, trade events, and live snapshots of open orders (P/L or ticket lists). Use these when you need continuously updating data rather than one‑off RPC calls.

---

## 📂 Methods in this folder

* [RealTimeQuotes.md](RealTimeQuotes.md)
  Subscribe to **tick‑by‑tick** quotes for one or more symbols.

* [StreamOpenedOrderProfits.md](StreamOpenedOrderProfits.md)
  Live stream of **floating P/L** for each open order (polling interval controlled by you).

* [StreamOpenedOrderTickets.md](StreamOpenedOrderTickets.md)
  Lightweight stream that returns only the **ticket IDs** of currently open orders.

* [StreamTradeUpdates.md](StreamTradeUpdates.md)
  Server‑side stream of **trade activity** (executions/closures/etc.).

* [StreamConnectState.md](StreamConnectState.md)
  Terminal **connection-state** heartbeat (connected/alive/api-alive).

---

## ⚡ Typical Workflows

### 1) Show the first live tick for a symbol

```csharp
// Service (same shape as low-level):
//   await foreach (var tick in _service.OnSymbolTickAsync(new[]{"EURUSD"}, ct)) { ...; break; }

// Low-level: break on first item or cancel via token
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await foreach (var tick in _mt4.OnSymbolTickAsync(new[] { "EURUSD" }, cts.Token))
{
    var q = tick.SymbolTick;
    Console.WriteLine($"Tick: {q.Symbol} {q.Bid}/{q.Ask} @ {q.Time}");
    break; // stop after first
}
```

### 2) Monitor floating P/L per open order

```csharp
// Low-level: 1s interval, cancel on condition
using var cts = new CancellationTokenSource();
_ = Task.Run(async () => { await Task.Delay(TimeSpan.FromSeconds(30)); cts.Cancel(); });

await foreach (var upd in _mt4.OnOpenedOrdersProfitAsync(1000, cts.Token))
{
    foreach (var o in upd.OpenedOrdersWithProfitUpdated)
        Console.WriteLine($"{o.Symbol} #{o.Ticket} P/L={o.OrderProfit}");
}
```

### 3) Track when tickets change

```csharp
await foreach (var update in _mt4.OnOpenedOrdersTicketsAsync(1000, ct))
{
    Console.WriteLine($"Positions: {string.Join(", ", update.PositionTickets)} | Pending: {string.Join(", ", update.PendingOrderTickets)}");
}
```

### 4) Snapshot + streaming combo

```csharp
// Get a snapshot first...
var syms = new[] { "EURUSD", "GBPUSD" };
var snap = await _mt4.QuoteManyAsync(syms);
// ...then stream ticks to keep UI “live”
await foreach (var t in _mt4.OnSymbolTickAsync(syms, ct))
{
    // update widgets/pricing panels incrementally
}
```

---

## ✅ Best Practices

1. **Always pass a CancellationToken.** Streams are open‑ended; you control their lifetime.
2. **Expect `Cancelled` on shutdown.** Both client‑initiated and server‑initiated cancellations are normal termination paths.
3. **Choose sensible intervals.** For `OpenedOrderProfits`/`OpenedOrderTickets`, 500–2000 ms is typical. Faster polling increases load.
4. **Backoff & reconnect.** The client retries on transient gRPC statuses (`Unavailable`/`DeadlineExceeded`/`Internal`) with exponential backoff; persistent failures bubble up to you.
5. **Validate inputs.**

   * `OnSymbolTickAsync` requires at least one **existing** symbol (e.g., `"EURUSD"`).
   * Use the same casing and broker suffixes (`EURUSD.m`, `XAUUSD-RAW`) the server exposes.
6. **Keep prices raw for math;** round only for display. Timestamps are **UTC**.

---

## 🎯 Purpose

Streaming APIs power real‑time UIs, alerting, and automation:

* **Quotes:** live panels, best‑bid/ask triggers, latency‑sensitive logic.
* **Opened orders:** on‑the‑fly risk views (P/L), change detection via ticket lists.
* **Trades:** immediate reaction to fills/closures for logging, analytics, or hedging.

Use this page as a map and dive into each file for signatures, fields, and examples.
