# ⚡ Sync vs Async Methods (C#)

> The CSharpMT4 SDK gives you **both** synchronous and asynchronous calls — but **not on every layer**. Which you get depends on where you call from, and the two are not interchangeable everywhere. This guide shows what exists where, and when to use which.

---

## 🗺️ What exists where

| Layer | Async | Sync | Notes |
| ----- | :---: | :--: | ----- |
| **`MT4Account`** (low-level) | ✅ `…Async` | ✅ (core calls) | Sync twins for the main request/reply calls: `AccountSummary`, `Quote`, `QuoteMany`, `Symbols`, `SymbolParamsMany`, `TickValueWithSize`, `QuoteHistory`, `OpenedOrders`, `OpenedOrdersTickets`, `OrdersHistory`, `OrderSend`, `OrderModify`, `OrderCloseDelete`, `OrderCloseBy`, `ConnectByServerName`, `ConnectByHostPort`, `Disconnect`. |
| **`MT4Service`** (typed facade) | ✅ only | ❌ | Async-only. |
| **`MT4Sugar`** (helpers) | ✅ only | ❌ | Async-only. |
| **Streaming** (any layer) | ✅ only | ❌ | `IAsyncEnumerable<T>` — there is no synchronous form. |

> ⚠️ **Not every async method has a sync twin.** Even on `MT4Account`, calls like `TradeStatsAsync`, `EquityHistoryAsync`, `TradeUnrealizedPLAsync` and all streams are **async-only**. The sync list above is the complete set.

---

## 🔍 How the sync methods actually work

Each synchronous method is a thin **blocking wrapper** over its async twin:

```csharp
// Inside the SDK — the sync method just blocks on the async one:
public AccountSummaryData AccountSummary(DateTime? deadline = null, CancellationToken ct = default)
    => AccountSummaryAsync(deadline, ct).GetAwaiter().GetResult();
```

So the **result is identical** — the only difference is that the sync version **parks the calling thread** until the network round trip finishes, while the async version frees it. That single fact drives every recommendation below.

---

## 🧭 When to use which

### ✅ Async — the default for anything real

```csharp
var account = new MT4Account(user, password, grpcServer, apiKey);
await account.ConnectByServerNameAsync(serverName, "EURUSD");   // 3rd arg is a bool, not a timeout
var summary = await account.AccountSummaryAsync();
Console.WriteLine($"Balance: {summary.AccountBalance}, Equity: {summary.AccountEquity}");
```

Use async for:

- **Bots, services, GUIs** — anything that must stay responsive or handle concurrency.
- **Streaming** — ticks/trades/P-L are async-only anyway (`await foreach`).
- **Multi-symbol / parallel work** — fire several calls at once (see below).
- **Everything through `MT4Service` / `MT4Sugar`** — they offer nothing else.

### 🟡 Sync — fine for simple, linear, one-thread code

```csharp
var account = new MT4Account(user, password, grpcServer, apiKey);
account.ConnectByServerName(serverName, "EURUSD");
var summary = account.AccountSummary();     // blocks until the reply arrives
Console.WriteLine($"Balance: {summary.AccountBalance}");
```

Reasonable when:

- A short **CLI one-off** or setup script that does one thing after another.
- A **REPL / notebook / learning** snippet where async plumbing is just noise.
- You're on `MT4Account` and don't touch streams.

The sync calls are a **convenience**, not a performance choice — they are the async calls with a block on top.

---

## 🚫 The one rule when mixing

**Don't call the sync (blocking) wrappers from inside async code or a UI/sync-context thread.** Blocking on a Task that still needs the current context to complete is the classic **deadlock**, and even when it doesn't deadlock it wastes a thread for the whole round trip.

```csharp
// ❌ WRONG — inside an async method, don't block with the sync wrapper (or .Result/.Wait())
async Task HandleAsync()
{
    var s = account.AccountSummary();            // blocks this async flow
}

// ✅ RIGHT — in async code, await the async twin
async Task HandleAsync()
{
    var s = await account.AccountSummaryAsync();
}
```

Rule of thumb: **in async code, stay async; reach for the sync wrappers only in genuinely synchronous, top-level code.**

---

## 🔀 Concurrency (async only)

Async lets you run independent calls **in parallel** instead of back-to-back — something the sync wrappers can't do on one thread:

```csharp
// All three quotes in flight together, then awaited as one:
var quotes = await Task.WhenAll(
    svc.QuoteAsync("EURUSD"),
    svc.QuoteAsync("GBPUSD"),
    svc.QuoteAsync("USDJPY"));
```

> ⚠️ Fan-out is for **independent** reads. Don't fire overlapping **trading** operations on the same position concurrently — order them with `await`.

---

## 📡 Streams are async-only

Live subscriptions are `IAsyncEnumerable<T>`: consume with `await foreach`, stop with a `CancellationToken`. There is no synchronous equivalent.

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
try
{
    await foreach (var d in svc.OnSymbolTickAsync(new[] { "EURUSD" }, cts.Token))
        Console.WriteLine(d.SymbolTick.Bid);
}
catch (OperationCanceledException) { /* expected on stop */ }
```

→ Full lifecycle, reconnect and cancellation: **[gRPC Streaming](GRPC_STREAM_MANAGEMENT.md)**.

---

## 🧯 Common pitfalls

| Pitfall | Why it hurts | Fix |
| ------- | ------------ | --- |
| Sync wrapper (or `.Result`/`.Wait()`) inside async code | Deadlocks, blocked threads | Call the `…Async` twin and `await` it |
| Expecting a sync version on `MT4Service`/`MT4Sugar` | They're async-only — it won't compile | Use the async method with `await` |
| Looking for a sync stream | Streams are `IAsyncEnumerable` only | `await foreach` + `CancellationToken` |
| `async void` methods | Exceptions can't be awaited/caught | Use `async Task` (except real event handlers) |
| Not awaiting a Task | Result used before it's ready; exceptions swallowed | Always `await` |

---

## 💡 Summary

- **`MT4Account`** has sync **and** async for its core calls; **`MT4Service` / `MT4Sugar` / streaming** are **async-only**.
- A sync method is literally `…Async().GetAwaiter().GetResult()` — same result, but it **blocks the thread**.
- **Prefer async** for bots, services, GUIs, streams and parallelism; use **sync** only for simple linear scripts on `MT4Account`.
- **Never** block with a sync wrapper (or `.Result`/`.Wait()`) **inside** async code — stay async there.

---

## See Also

- **[Getting Started](GETTING_STARTED.md)** — install, configure, run.
- **[gRPC Streaming](GRPC_STREAM_MANAGEMENT.md)** — the async-only streaming model.
- **[Return Codes](RETURN_CODES_REFERENCE.md)** — handling errors from a call, sync or async.
