# Getting Quotes for Multiple Symbols with Ticks

> **Request:** fetch quotes for multiple symbols and stream real-time price ticks
> Combines a one-time quote snapshot (`QuoteManyAsync`) with a live tick stream (`OnSymbolTickAsync`) for each symbol.

**API Information:**

* **SDK wrapper:** `MT4Account.QuoteManyAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.MarketInfo`
* **Proto definition:** `QuoteMany` (in the MarketInfo service)

### RPC

* **Service:** `mt4_term_api.MarketInfo`
* **Method:** `QuoteMany(QuoteManyRequest) → QuoteManyReply`
* **Low‑level client (generated):** `MarketInfo.MarketInfoClient.QuoteMany(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async Task<QuoteManyData> QuoteManyAsync(
            string[] symbols,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default);
    }
}
```

**Request message:**

`QuoteManyRequest { symbols: string[] }`

**Reply message:**

`QuoteManyReply { data: QuoteManyData }`

---

### Code Example

```csharp
// --- Service wrapper (typed, data-returning) ---
// One snapshot for all symbols; you format the result.
var many = await _service.QuoteManyAsync(new[] { "EURUSD", "GBPUSD", "USDJPY" });
foreach (var x in many.Quotes)
    Console.WriteLine($"{x.Symbol}: {x.Bid}/{x.Ask}");

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

var symbols = new[] { "EURUSD", "GBPUSD" };

// 1) Optional: initial snapshot for all symbols
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // tighten/loosen as you need
var snapshot = await _mt4.QuoteManyAsync(
    symbols: symbols,
    deadline: null,
    cancellationToken: cts.Token);

// 2) Live first tick per symbol (soft timeout per symbol)
foreach (var symbol in symbols)
{
    using var perSymbolCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
    perSymbolCts.CancelAfter(TimeSpan.FromSeconds(5));

    try
    {
        await foreach (var tick in _mt4.OnSymbolTickAsync(new[] { symbol }, perSymbolCts.Token))
        {
            var q = tick.SymbolTick;
            if (q == null) continue;

            var time = q.Time?.ToDateTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "n/a";
            Console.WriteLine($"Tick: {q.Symbol} {q.Bid}/{q.Ask} @ {time}");
            perSymbolCts.Cancel(); // stop after the first tick
            break;
        }
    }
    catch (OperationCanceledException)
    {
        // expected if no tick within timeout
        Console.WriteLine($"⏹️ No ticks for {symbol} within 5s — skipping.");
    }
}
```

---

## 🔽 Input

### For `QuoteManyAsync`

* **`symbols`** (`IEnumerable<string>`) — required. Array/list of trading symbols (e.g., `{"EURUSD","USDJPY"}`).
* **`deadline`** (`DateTime?`, optional) — optional UTC deadline for timeout.
* **`cancellationToken`** (`CancellationToken`, optional) — token to cancel the request.

### For `OnSymbolTickAsync`

* **`symbols`** (`IEnumerable<string>`) — required. Symbols to subscribe for real-time ticks.
* **`cancellationToken`** (`CancellationToken`, optional) — used to stop the stream.

---

## ⬆️ Output

### From `QuoteManyAsync`

Returns **`QuoteManyData`** (container of snapshot quotes for requested symbols). Individual quote entries are of type `QuoteData` and expose:

| Field      | Type                                       | Description                             |
| ---------- | ------------------------------------------ | --------------------------------------- |
| `Bid`      | `double`                                   | Current bid price                       |
| `Ask`      | `double`                                   | Current ask price                       |
| `DateTime` | `Google.Protobuf.WellKnownTypes.Timestamp` | Server timestamp for the snapshot (UTC) |

### From `OnSymbolTickAsync`

Streams **`OnSymbolTickData`** where `SymbolTick` contains:

| Field    | Type                                       | Description                        |
| -------- | ------------------------------------------ | ---------------------------------- |
| `Symbol` | `string`                                   | Symbol name                        |
| `Bid`    | `double`                                   | Current bid price                  |
| `Ask`    | `double`                                   | Current ask price                  |
| `Time`   | `Google.Protobuf.WellKnownTypes.Timestamp` | Server timestamp of the tick (UTC) |

---

## 🎯 Purpose

Get both a quick **snapshot** (initial prices for multiple symbols) and **live** updates via ticks. Typical uses:

* Trading dashboards and watchlists
* Real-time price monitors and alerts
* Preloading prices before starting live streams

---

## 🧩 Notes & Tips

* **Endless stream.** `OnSymbolTickAsync` is an open stream. Always manage cancellation (as in the example) to stop cleanly.
* **Per-symbol timeout.** In the wrapper we use a small per-symbol timeout to avoid hanging when a symbol is inactive.
* **Exact names.** Use the exact symbol strings from `SymbolsAsync()` — some brokers add suffixes (e.g., `EURUSD.r`).
