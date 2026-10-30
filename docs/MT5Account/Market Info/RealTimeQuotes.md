# Streaming Real-Time Quotes for a Symbol

> **Request:** subscribe to real-time bid/ask price updates for a given symbol
> Opens a streaming channel to receive tick-by-tick quotes as the market changes.

**API Information:**

* **SDK wrapper:** `MT4Account.OnSymbolTickAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.SubscriptionService`
* **Proto definition:** `OnSymbolTick` (in the SubscriptionService service)

### RPC

* **Service:** `mt4_term_api.SubscriptionService`
* **Method:** `OnSymbolTick(OnSymbolTickRequest) → stream OnSymbolTickReply`
* **Low‑level client (generated):** `SubscriptionService.SubscriptionServiceClient.OnSymbolTick(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async IAsyncEnumerable<OnSymbolTickData> OnSymbolTickAsync(
            IEnumerable<string> symbols,
            [EnumeratorCancellation] CancellationToken cancellationToken = default);
    }
}
```

**Request message:**

`OnSymbolTickRequest { symbols: string[] }`

**Reply message:**

`OnSymbolTickReply { data: OnSymbolTickData } (server stream)`

---

### Code Example

```csharp
// --- Service wrapper (streaming; same shape as low-level) ---
using (var scts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
await foreach (var tick in _service.OnSymbolTickAsync(new[] { "EURUSD" }, scts.Token))
{
    var t = tick.SymbolTick; if (t == null) continue;
    Console.WriteLine($"{t.Symbol} {t.Bid}/{t.Ask}");
    break; // demo: stop after first tick
}

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await foreach (var tick in _mt4.OnSymbolTickAsync(new[] { "EURUSD" }, cts.Token))
{
    var q = tick.SymbolTick; if (q == null) continue;
    var time = q.Time?.ToDateTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "n/a";
    Console.WriteLine($"Tick: {q.Symbol} {q.Bid}/{q.Ask} @ {time}");
    cts.Cancel(); // demo: stop after first tick
    break;
}
```

---

## 🔽 Input

> The service method and the low-level call share the same signature — the service simply forwards to the account. Bound the stream yourself with a `CancellationToken` / timeout (the demo stops after the first tick).

### For `OnSymbolTickAsync`

| Parameter           | Type                           | Description                                 |
| ------------------- | ------------------------------ | ------------------------------------------- |
| `symbols`           | `IEnumerable<string>`          | Required. One or more symbols to subscribe. |
| `cancellationToken` | `CancellationToken` (optional) | Stream control (stop/cancel).               |

---

## ⬆️ Output

`OnSymbolTickAsync` returns a stream: `IAsyncEnumerable<OnSymbolTickData>`.

**OnSymbolTickData** contains:

| Field        | Type        | Description                     |
| ------------ | ----------- | ------------------------------- |
| `SymbolTick` | `QuoteData` | Real-time data object for tick. |

**QuoteData** fields:

| Field    | Type                                       | Description                         |
| -------- | ------------------------------------------ | ----------------------------------- |
| `Symbol` | `string`                                   | Symbol name.                        |
| `Bid`    | `double`                                   | Current bid price.                  |
| `Ask`    | `double`                                   | Current ask price.                  |
| `Time`   | `Google.Protobuf.WellKnownTypes.Timestamp` | Server timestamp of the tick (UTC). |

---

## 🎯 Purpose

Receive **live market data** for a symbol in real time — suitable for:

* Streaming UI quote panels
* Price-driven alerts
* Feeding pricing logic in automated systems

---

## 🧩 Notes & Tips

* **Endless stream.** This is a continuous stream; always manage cancellation (see example).
* **Normal completion signals.** A `RpcException` with `Status=Cancelled` (when you cancel via token) is expected; the client treats it as normal completion. Certain server finalization codes (e.g., `ON_SUBSCRIPTION_EA_DEINITIALIZATION_START_WATCHING_MULTI_CHARTS_COUNT_ZERO`) are also handled as clean stream end.
* **Inactive symbols.** If a symbol is inactive, you may not receive ticks within the timeout; design UI to handle this gracefully.
* **Exact naming.** Use symbol names exactly as returned by `SymbolsAsync()` (brokers may use suffixes like `EURUSD.r`).

---

## ⚠️ Pitfalls

* **Forgetting cancellation.** Without a token or timeout, streams keep running indefinitely.
* **Mixed subscriptions.** Subscribing a large set of symbols without per-symbol timeouts may block your flow if some are inactive.

---

## 🧪 Testing Suggestions

* **Happy path.** First tick arrives within a few seconds during active market hours.
* **Edge cases.** In after-hours or on illiquid symbols, ensure your timeout/cancellation path logs and exits cleanly.
* **Failure path.** Simulate a reconnect (temporary network issue): the client should restart the stream with backoff or end gracefully if you cancel.
