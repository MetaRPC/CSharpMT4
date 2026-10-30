# 📡 gRPC Streaming & Subscription Management

> **Complete guide** to real-time subscriptions in the **C# MT4 SDK** — how to open a stream, how to stop it cleanly, and how the built-in reconnect loop keeps it alive.

This document covers:

- ✅ **How to subscribe** to real-time market/account data
- ✅ **How to stop** a stream without leaking connections
- ✅ **Patterns** from a 10-second sample to a long-running background service
- ✅ **Architecture** — the reconnect loop and its safety guarantees
- ✅ **Troubleshooting** and best practices

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Available Streaming Methods](#available-streaming-methods)
3. [How Stopping Works in C#](#how-stopping-works-in-c)
4. [Patterns (simple → advanced)](#patterns-simple-advanced)
5. [Common Mistakes](#common-mistakes)
6. [Streaming with Enums](#streaming-with-enums)
7. [Architecture & Safety](#architecture-safety)
8. [Troubleshooting](#troubleshooting)

---

## Quick Start

Every streaming method returns an **`IAsyncEnumerable<T>`**. You consume it with **`await foreach`**, and you stop it with a **`CancellationToken`**. That is the whole model.

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

// acc is a connected MT4Account (see Getting Started for how to connect).
// Stream EURUSD ticks for 10 seconds, then stop automatically.
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

try
{
    await foreach (var data in acc.OnSymbolTickAsync(new[] { "EURUSD" }, cts.Token))
    {
        var t = data.SymbolTick;
        Console.WriteLine($"{t.Symbol}  Bid={t.Bid}  Ask={t.Ask}  @ {t.Time}");
    }
}
catch (OperationCanceledException)
{
    // Expected when the token fires — the stream was asked to stop.
}

Console.WriteLine("Stream finished.");
```

**What happens here**

- `CancellationTokenSource(TimeSpan)` cancels itself after 10 s (same as calling `CancelAfter`).
- When the token fires, the stream ends and `await foreach` throws `OperationCanceledException`, which we swallow.
- `using` disposes the `CancellationTokenSource`; leaving the `foreach` disposes the underlying gRPC call. **No channel to close per stream** — the connection belongs to the account.

> 🟢 **When to use:** quick tests, short monitoring windows, examples.

---

## Available Streaming Methods

All of these return `IAsyncEnumerable<T>` and are driven by a `CancellationToken`.

### On `MT4Account` (low-level)

| Method | Yields | What it pushes |
| ------ | ------ | -------------- |
| `OnSymbolTickAsync(IEnumerable<string> symbols, ct)` | `OnSymbolTickData` | A message on every tick of the subscribed symbol(s) — `.SymbolTick` has `Symbol`, `Bid`, `Ask`, `Time`. |
| `OnTradeAsync(ct)` | `OnTradeData` | Order create / modify / close events on the account. |
| `OnOpenedOrdersProfitAsync(int intervalMs, ct)` | `OnOpenedOrdersProfitData` | Live P/L snapshot every `intervalMs` — `.OpenedOrdersWithProfitUpdated`, `.AccountInfo?.Equity`. |
| `OnOpenedOrdersTicketsAsync(int intervalMs, ct)` | `OnOpenedOrdersTicketsData` | The set of open tickets every `intervalMs` — `.PositionTickets` (lighter than the profit stream). |
| `OnConnectStateAsync(ct)` | `ConnectStateData` | Terminal link state — fires when the connection drops or returns. |

### On `MT4Service` (typed facade)

`MT4Service` re-exposes the **same five methods** with the same signatures (`intervalMs` defaults to `1000`), so you can stream straight from the service you already use for everything else.

It also adds two **sugar** readers for the two most common streams:

| Method | Yields | Notes |
| ------ | ------ | ----- |
| `ReadTicks(string symbol, ct)` | `OnSymbolTickData` | Single-symbol tick reader — thin wrapper over `OnSymbolTickAsync`. |
| `ReadTrades(ct)` | `OnTradeData` | Trade-event reader — thin wrapper over `OnTradeAsync`. |

**Live examples in the repo:**

- [`examples/LowLevel/3_Streaming.cs`](https://github.com/MetaRPC/CSharpMT4/blob/main/examples/LowLevel/3_Streaming.cs) — all five low-level streams, each sampled ~3 s.
- [`examples/Services/4_ServiceExamples.cs`](https://github.com/MetaRPC/CSharpMT4/blob/main/examples/Services/4_ServiceExamples.cs) — the *Subscriptions* section streams the same five from `MT4Service`.
- [`examples/Sugar/5_SugarScenario.cs`](https://github.com/MetaRPC/CSharpMT4/blob/main/examples/Sugar/5_SugarScenario.cs) — `ReadTicks` / `ReadTrades` inside the ASSESS step.

---

## How Stopping Works in C#

There are **two** ways a stream ends, and they are not the same:

### 1. `break` out of the `await foreach` — clean, but only between items

When you `break` (or `return`, or throw) out of an `await foreach`, the compiler disposes the async enumerator. That runs the stream's internal `finally`, which disposes the gRPC call — so there is **no orphaned background loop** (unlike some other runtimes, C# iterators stop when iteration stops).

```csharp
int n = 0;
await foreach (var data in acc.OnSymbolTickAsync(new[] { "EURUSD" }, ct))
{
    if (++n >= 10) break;   // clean: the enumerator is disposed, the call is released
    Console.WriteLine(data.SymbolTick.Bid);
}
```

⚠️ **The catch:** `break` can only run *after* an item is delivered. If the market goes quiet, the loop is parked inside `MoveNext` waiting for the next message and your `break` never gets a turn. For a stream that may idle, use a token.

### 2. Cancel the `CancellationToken` — prompt, works even when idle

Cancelling the token unblocks the pending `MoveNext` immediately, so the stream stops **now**, whether or not another message ever arrives. This is why every example drives streams with a token.

```csharp
using var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(30));   // or cts.Cancel() from elsewhere

try
{
    await foreach (var data in acc.OnSymbolTickAsync(new[] { "EURUSD" }, cts.Token))
        Console.WriteLine(data.SymbolTick.Bid);
}
catch (OperationCanceledException) { /* expected on cancel */ }
```

> 🔑 **Rule of thumb:** use a **token** to bound *when* a stream stops; use `break` to stop on a *data condition*. They compose — see the patterns below.

### The reusable sampling helper

The examples factor this into one small helper you can copy. It opens a stream, prints up to `maxItems`, and auto-stops after `seconds` — using a **linked** token so an outer cancellation still propagates:

```csharp
static async Task Sample<T>(
    Func<CancellationToken, IAsyncEnumerable<T>> make,
    CancellationToken outer,
    Action<int, T> print,
    int seconds = 3,
    int maxItems = 3)
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(outer);
    cts.CancelAfter(TimeSpan.FromSeconds(seconds));

    int n = 0;
    try
    {
        await foreach (var x in make(cts.Token))
        {
            print(++n, x);
            if (n >= maxItems) break;
        }
    }
    catch (OperationCanceledException) { }   // timeout/cancel is expected
    Console.WriteLine($"  ({n} event(s))");
}

// Usage:
await Sample(c => acc.OnSymbolTickAsync(new[] { "EURUSD" }, c), ct,
             (i, d) => Console.WriteLine($"tick #{i}: {d.SymbolTick.Bid}"));
```

---

## Patterns (simple → advanced)

### Pattern 1 — Timeout window

Stop after a fixed time. Simplest possible bound.

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
try
{
    await foreach (var d in svc.OnSymbolTickAsync(new[] { "EURUSD" }, cts.Token))
        Console.WriteLine(d.SymbolTick.Bid);
}
catch (OperationCanceledException) { }
```

### Pattern 2 — Event-limited

Stop after N messages (token still there as a safety net in case the market is quiet).

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
int count = 0;
try
{
    await foreach (var d in svc.OnSymbolTickAsync(new[] { "EURUSD" }, cts.Token))
    {
        Console.WriteLine($"[{++count}] {d.SymbolTick.Symbol} {d.SymbolTick.Bid}");
        if (count >= 100) break;
    }
}
catch (OperationCanceledException) { }
```

### Pattern 3 — Condition-based stop

Stop when the data meets a condition.

```csharp
using var cts = new CancellationTokenSource();
try
{
    await foreach (var d in svc.OnSymbolTickAsync(new[] { "EURUSD" }, cts.Token))
    {
        var bid = d.SymbolTick.Bid;
        Console.WriteLine($"Price: {bid}");
        if (bid > 1.10000) { Console.WriteLine("Target reached!"); break; }
    }
}
catch (OperationCanceledException) { }
```

### Pattern 4 — Background service with manual control

Run a stream on its own `Task` and stop it on demand from elsewhere. The `CancellationTokenSource` is the off-switch.

```csharp
public sealed class PriceMonitor
{
    private readonly MT4Service _svc;
    private readonly CancellationTokenSource _cts = new();
    private Task? _task;

    public PriceMonitor(MT4Service svc) => _svc = svc;

    public void Start(string[] symbols) => _task = Task.Run(() => LoopAsync(symbols));

    private async Task LoopAsync(string[] symbols)
    {
        try
        {
            await foreach (var d in _svc.OnSymbolTickAsync(symbols, _cts.Token))
                Console.WriteLine($"{d.SymbolTick.Symbol}: {d.SymbolTick.Bid} / {d.SymbolTick.Ask}");
        }
        catch (OperationCanceledException) { }   // normal shutdown
    }

    public async Task StopAsync()
    {
        _cts.Cancel();                 // signal the stream to stop
        if (_task != null) await _task; // wait for the loop to unwind
        _cts.Dispose();
    }
}

// Usage:
var monitor = new PriceMonitor(svc);
monitor.Start(new[] { "EURUSD", "GBPUSD" });
await Task.Delay(TimeSpan.FromSeconds(30));
await monitor.StopAsync();
```

### Pattern 5 — Multiple concurrent streams

Run several streams at once and stop **all** of them with a **single** token. Give each its own `try/catch` so one failing stream doesn't tear down the others, and await them together.

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // stops all after 30 s

async Task Ticks()
{
    try { await foreach (var d in svc.OnSymbolTickAsync(new[] { "EURUSD" }, cts.Token))
              Console.WriteLine($"[TICK] {d.SymbolTick.Bid}"); }
    catch (OperationCanceledException) { }
}

async Task Trades()
{
    try { await foreach (var d in svc.OnTradeAsync(cts.Token))
              Console.WriteLine($"[TRADE] {d}"); }
    catch (OperationCanceledException) { }
}

async Task Profits()
{
    try { await foreach (var d in svc.OnOpenedOrdersProfitAsync(2000, cts.Token))
              Console.WriteLine($"[PROFIT] {d.OpenedOrdersWithProfitUpdated.Count} order(s), equity={d.AccountInfo?.Equity}"); }
    catch (OperationCanceledException) { }
}

await Task.WhenAll(Ticks(), Trades(), Profits());
Console.WriteLine("All streams stopped.");
```

---

## Common Mistakes

### ❌ Mistake 1 — Streaming without a token

```csharp
// If the market goes quiet, there is no way to stop this loop promptly.
await foreach (var d in acc.OnSymbolTickAsync(new[] { "EURUSD" }))
{
    if (count >= 10) break;   // only runs when the NEXT tick arrives
}
```

✅ **Fix:** pass a token so you can stop even when no item is pending.

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
await foreach (var d in acc.OnSymbolTickAsync(new[] { "EURUSD" }, cts.Token)) { /* ... */ }
```

### ❌ Mistake 2 — Letting `OperationCanceledException` escape

Cancelling a stream throws `OperationCanceledException` (or `TaskCanceledException`). That is **normal**; don't let it crash your app.

```csharp
try { await foreach (var d in svc.OnTradeAsync(cts.Token)) { /* ... */ } }
catch (OperationCanceledException) { /* expected on stop */ }
```

### ❌ Mistake 3 — One `CancellationTokenSource` reused after cancel

A `CancellationTokenSource` is **single-use**: once cancelled, every token from it is already cancelled. To start a new stream, create a **new** source.

```csharp
// ❌ cts was cancelled earlier → this stream ends immediately
await foreach (var d in svc.OnSymbolTickAsync(new[] { "EURUSD" }, cts.Token)) { }

// ✅ fresh source per run
using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
await foreach (var d in svc.OnSymbolTickAsync(new[] { "EURUSD" }, cts2.Token)) { }
```

### ❌ Mistake 4 — Heavy work inside the loop body

The loop body runs on the consuming task; slow work there backs up the stream. Hand off heavy processing (queue it, or `Task.Run` the CPU-bound part) and keep the loop tight.

---

## Streaming with Enums

Some stream payloads carry MT4 enum fields you can filter on. The two that matter for subscriptions:

| Enum | Where it appears |
| ---- | ---------------- |
| `MT4_SUB_ENUM_EVENT_GROUP_TYPE` | `OnTradeData.Type` / `OnOpenedOrdersProfitData.Type` — which kind of event this message represents. |
| `SUB_ORDER_OPERATION_TYPE` | `OnOpenedOrdersProfitOrderInfo.Type` — the operation type of each order in a profit snapshot. |

Example — react only to a specific trade-event group:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
try
{
    await foreach (var ev in svc.OnTradeAsync(cts.Token))
    {
        // ev.Type is an MT4_SUB_ENUM_EVENT_GROUP_TYPE value — switch on it to route events.
        Console.WriteLine($"event group: {ev.Type}");
    }
}
catch (OperationCanceledException) { }
```

> See [ENUMs Reference → Streaming](ENUMS_USAGE_REFERENCE.md) for the full list of streaming enums and their members.

---

## Architecture & Safety

### One mechanism behind every stream

All five low-level streams funnel through a single private helper on `MT4Account`:

```
OnSymbolTickAsync / OnTradeAsync / OnOpenedOrdersProfitAsync / ...
        │
        ▼
ExecuteStreamWithReconnect<TRequest, TReply, TData>   (the reconnect loop)
        │
        ▼
gRPC server-streaming call  ──►  Network  ──►  MT4 terminal
```

Because every stream shares this helper, **the lifecycle rules are identical** for all of them: the same cancellation behavior, the same reconnect behavior, the same cleanup.

### What the reconnect loop guarantees

`ExecuteStreamWithReconnect` runs roughly:

```csharp
while (!ct.IsCancellationRequested)          // token is the only way out
{
    AsyncServerStreamingCall<TReply>? call = null;
    try
    {
        call = streamInvoker(request, headers, ct);
        while (await call.ResponseStream.MoveNext(ct))
            yield return data;               // hand each message to your foreach
    }
    catch (RpcException ex) when (transient)  // Unavailable / DeadlineExceeded / Internal
    {
        needReconnect = true;                 // drop out, reconnect, open a NEW stream
    }
    finally
    {
        call?.Dispose();                      // ✅ the current call is ALWAYS released
    }

    if (needReconnect) { await Reconnect(); } // transparent to your loop
    else break;
}
```

✅ **What it does for you**

- **Auto-reconnect** on transient transport errors (`Unavailable`, `DeadlineExceeded`, `Internal`) and on reconnectable API errors — your `await foreach` keeps delivering across a blip, up to a restart cap.
- **Always disposes** the current gRPC call in `finally`, so a dropped or finished stream never leaks.
- **Clean completion** — cancellation-by-token, server-sent `Cancelled`, and end-of-stream (EOF) all end the enumeration gracefully (`yield break`).

⚠️ **What you must do**

- **Provide and control a `CancellationToken`.** The loop exits only when the token is cancelled (or the server finalizes the stream). Without cancellation a healthy stream runs indefinitely — which is correct for a long-running service, but means *you* own the off-switch.
- **Expect `OperationCanceledException`** when you cancel, and catch it.
- A **non-transient** API error surfaces as `ApiExceptionMT4` out of the `await foreach` — handle it like any other call's error (see [Return Codes](RETURN_CODES_REFERENCE.md)).

### Connection vs. stream

A stream does **not** own the connection. You connect once (e.g. via `ConnectionHelper`/`MT4Account`), open and close as many streams as you like over that connection, and disconnect once at the end. There is no per-stream `channel.close()`.

---

## Troubleshooting

| Symptom | Likely cause | Fix |
| ------- | ------------ | --- |
| Stream never stops on `break` | Market is quiet; loop is parked in `MoveNext` | Drive it with a `CancellationToken` (`CancelAfter` / `Cancel`). |
| `OperationCanceledException` crashes the app | Not caught around the `await foreach` | Wrap the loop in `try { } catch (OperationCanceledException) { }`. |
| New stream ends immediately | Reusing an already-cancelled `CancellationTokenSource` | Create a **new** `CancellationTokenSource` per run. |
| Stream keeps dropping and reopening | Transient transport errors being auto-retried | Usually fine (that's the reconnect loop). If persistent, check network / server endpoint in config. |
| `ApiExceptionMT4` out of the loop | Server sent a non-transient, non-final error | Inspect the error code; see [Return Codes](RETURN_CODES_REFERENCE.md). |
| Lagging / backed-up stream | Heavy work in the loop body | Keep the body tight; offload processing. |

---

## See Also

- **[MT4Account — API Reference](../MT5Account/MT4Account.Master.Overview.md)** — low-level streaming methods.
- **[MT4Service — API Reference](../MT4Service/MT4Service.Overview.md)** — the typed streaming facade.
- **[MT4Sugar — API Reference](../MT4Sugar/MT4Sugar.Overview.md)** — `ReadTicks` / `ReadTrades`.
- **[ENUMs Reference](ENUMS_USAGE_REFERENCE.md)** — streaming enums.
- **[Return Codes](RETURN_CODES_REFERENCE.md)** — handling `ApiExceptionMT4` from a stream.

---

**Remember:** streams are `IAsyncEnumerable<T>` you read with `await foreach`; a `CancellationToken` is your off-switch; the SDK reconnects and disposes for you. Own the token, catch the cancel, and your streaming code stays leak-free. 🚀
