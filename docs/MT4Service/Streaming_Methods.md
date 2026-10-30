# MT4Service — Streaming Methods

> Real-time server streams. All return `IAsyncEnumerable<...>` — consume with `await foreach` and always pass a `CancellationToken`.

→ builds on low-level **[Streaming — Overview](../MT5Account/Streaming/Streaming_Overview.md)**

---

## 📋 Methods

| Method | Yields | Low-level spec |
| ------ | ------ | -------------- |
| `OnSymbolTickAsync(symbols, ct)` | `OnSymbolTickData` | [RealTimeQuotes](../MT5Account/Streaming/RealTimeQuotes.md) |
| `OnTradeAsync(ct)` | `OnTradeData` | [TradeUpdates](../MT5Account/Streaming/StreamTradeUpdates.md) |
| `OnOpenedOrdersProfitAsync(intervalMs = 1000, ct)` | `OnOpenedOrdersProfitData` | [OrderProfits](../MT5Account/Streaming/StreamOpenedOrderProfits.md) |
| `OnOpenedOrdersTicketsAsync(intervalMs = 1000, ct)` | `OnOpenedOrdersTicketsData` | [OrderTickets](../MT5Account/Streaming/StreamOpenedOrderTickets.md) |
| `OnConnectStateAsync(ct)` | `ConnectStateData` | [ConnectState](../MT5Account/Streaming/StreamConnectState.md) |

---

## 💡 Usage examples

### 1) Live ticks (stop after first)

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await foreach (var t in service.OnSymbolTickAsync(new[] { "EURUSD" }, cts.Token))
{
    Console.WriteLine($"{t.SymbolTick?.Bid}/{t.SymbolTick?.Ask}");
    break;
}
```

### 2) Floating-P/L monitor (polling interval)

```csharp
await foreach (var upd in service.OnOpenedOrdersProfitAsync(1000, ct))
{
    foreach (var o in upd.OpenedOrdersWithProfitUpdated)
        Console.WriteLine($"#{o.Ticket} {o.Symbol} P/L={o.OrderProfit}");
    Console.WriteLine($"Equity={upd.AccountInfo?.Equity}");
}
```

### 3) Connection heartbeat

```csharp
await foreach (var s in service.OnConnectStateAsync(ct))
    Console.WriteLine($"[{s.State}] connected={s.IsConnected} alive={s.IsAlive} api={s.ApiIsAlive}");
```

---

## 🧩 Notes & Tips

* **Always bound the stream.** Pass a `CancellationToken` / timeout; cancellation completes the stream gracefully (a `Cancelled` status is normal).
* **Snapshot semantics.** Profit/ticket streams emit the **current** set each interval — compute diffs client-side if you need add/remove events.
* **Reconnect.** Transient gRPC errors are retried with backoff by the client's stream wrapper.
* **Interval.** For the polling streams, 500–2000 ms is typical; faster increases load.
