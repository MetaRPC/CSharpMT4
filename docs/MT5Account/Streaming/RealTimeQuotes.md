# Streaming Real-Time Quotes for a Symbol

> **Request:** subscribe to real‑time bid/ask price updates for a given symbol
> Opens a streaming channel to receive tick‑by‑tick quotes as the market changes.

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

`OnSymbolTickReply { data: OnSymbolTickData }` (server stream)

---

## Code Example

```csharp
// --- Service wrapper (streaming; same shape as the low-level call below) ---
// The service forwards to the account:
//   await foreach (var t in _service.OnSymbolTickAsync(new[]{"EURUSD"}, ct)) { ... }

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.
// Tip: always pass a CancellationToken; streams are infinite by design.
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await foreach (var tick in _mt4.OnSymbolTickAsync(new[] { "EURUSD" }, cts.Token))
{
    var q = tick.SymbolTick;
    if (q == null) continue;

    Console.WriteLine($"Tick: {q.Symbol} {q.Bid}/{q.Ask} @ {q.Time}");
    break; // demo: leave after the first tick
}
```

---

## 🔽 Input

**`OnSymbolTickAsync`** (service and low-level share the same signature)

| Parameter           | Type                | Required | Description                                                     |
| ------------------- | ------------------- | -------- | --------------------------------------------------------------- |
| `symbols`           | `string[]`          | ✅        | One or more symbols (even a single one like `new[]{"EURUSD"}`). |
| `cancellationToken` | `CancellationToken` | ❌        | Cancels the infinite stream.                                    |

---

## ⬆️ Output

Both variants deliver items of type **`SymbolTickData`**, which contains a **`QuoteData`** payload:

### Structure: `QuoteData`

| Field    | Type     | Description               |
| -------- | -------- | ------------------------- |
| `Symbol` | `string` | Symbol name               |
| `Bid`    | `double` | Current bid price         |
| `Ask`    | `double` | Current ask price         |
| `Time`   | `string` | UTC timestamp of the tick |

> In the wrapper, the first received tick is printed to console and the method exits.

---

## 🎯 Purpose

Use this to receive **live market data** for a symbol. Typical scenarios:

* Real‑time quote panels
* Alert engines that trigger on price movement
* Feeding pricing into automated strategies/UI overlays

---

## 🧩 Notes & Tips

* **Streams are infinite.** Always pass a `CancellationToken` (wrapper adds a timeout guard).
* **Server `Cancelled` != app cancellation.** Some servers respond with `StatusCode.Cancelled` when finalizing a subscription; client code treats this as **graceful completion** unless your token was cancelled with an error.
* **Reconnects.** Transient transport issues (`Unavailable`, `DeadlineExceeded`, `Internal`) are retried by the client’s internal stream wrapper with exponential backoff.
* **Many symbols?** Prefer a **single subscription** with multiple symbols over many per‑symbol subscriptions.

---

## ⚠️ Pitfalls

* **No first tick.** If the market is closed/illiquid or symbol is wrong, you may hit the timeout without any tick. Validate symbol names exactly as returned by `SymbolsAsync()`.
* **Over‑subscribing.** Creating lots of simultaneous per‑symbol streams can exhaust chart/session limits on the terminal. Batch symbols where possible.

---

## 🧪 Testing

* **Happy path:** You should see at least one tick during active market hours.
* **Timeout path:** Use a tiny `timeoutSeconds` to verify graceful stop without exceptions.
* **Cancel path:** Cancel the token and ensure the app exits the loop cleanly without leaking tasks.
