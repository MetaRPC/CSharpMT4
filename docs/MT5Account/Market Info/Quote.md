# Getting a Quote for Symbol

> **Request:** fetch the latest quote for a given symbol
> Returns current bid/ask prices and server time for a specified instrument.

**API Information:**

* **SDK wrapper:** `MT4Account.QuoteAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.MarketInfo`
* **Proto definition:** `Quote` (in the MarketInfo service)

### RPC

* **Service:** `mt4_term_api.MarketInfo`
* **Method:** `Quote(QuoteRequest) → QuoteReply`
* **Low‑level client (generated):** `MarketInfo.MarketInfoClient.Quote(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async Task<QuoteData> QuoteAsync(
            string symbol,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default);
    }
}
```

**Request message:**

`QuoteRequest { symbol: string }`

**Reply message:**

`QuoteReply { data: QuoteData }`

---

### Code Example

```csharp
// --- Service wrapper (typed, data-returning) ---
var q = await _service.QuoteAsync("EURUSD");
Console.WriteLine($"{q.Symbol}: Bid={q.Bid}, Ask={q.Ask}");

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)); // keep short; bump if your terminal is slow
var quote = await _mt4.QuoteAsync(
    symbol: "EURUSD",
    deadline: null,
    cancellationToken: cts.Token);

Console.WriteLine($"Quote for EURUSD: Bid={quote.Bid}, Ask={quote.Ask}, Time={quote.DateTime.ToDateTime():yyyy-MM-dd HH:mm:ss}");
```

---

## 🔽 Input

* **`symbol`** (`string`) — required. Symbol to get the quote for (e.g., `"EURUSD"`).
* **`deadline`** (`DateTime?`, optional) — optional UTC deadline for timeout.
* **`cancellationToken`** (`CancellationToken`, optional) — optional cancellation token.

---

## ⬆️ Output

Returns a **`QuoteData`** object with the following fields (from proto):

| Field      | Type                                       | Description                           |
| ---------- | ------------------------------------------ | ------------------------------------- |
| `Bid`      | `double`                                   | Current bid price                     |
| `Ask`      | `double`                                   | Current ask price                     |
| `DateTime` | `Google.Protobuf.WellKnownTypes.Timestamp` | Server timestamp for this quote (UTC) |

> *Note:* The proto does **not** include spread. If you need it, compute on the client (e.g., `spread = Ask - Bid`).

---

## 🎯 Purpose

Retrieve a live snapshot of market pricing for a specific symbol. Typical uses:

* Building order entry or pricing dashboards
* Quoting prices in trading UIs
* Lightweight spread/latency checks (derive spread client-side)

---

## 🧩 Notes & Tips

* **Symbol spelling.** Use the exact symbol returned by `SymbolsAsync()` (brokers may add suffixes like `EURUSD.r`).
* **Timeouts.** If you don't pass a `deadline`, the library applies its default per-RPC timeout (8s). For UI snappiness, a short 3–5s cancellation token is fine.
* **Time zone.** `DateTime` is a server-side UTC timestamp. Convert via `.ToDateTime()` for display.
