# Selecting a Symbol (Market Watch)

> **Request:** add a symbol to Market Watch (or remove it) via `SymbolSelect`, returning whether it succeeded.
> You must select a symbol before some quote/trade operations will see it.

**API Information:**

* **SDK wrapper:** `MT4Account.SymbolSelectAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.MarketInfo`
* **Proto definition:** `SymbolSelect` (in the MarketInfo service)

### RPC

* **Service:** `mt4_term_api.MarketInfo`
* **Method:** `SymbolSelect(SymbolSelectRequest) → SymbolSelectReply`
* **Low‑level client (generated):** `MarketInfo.MarketInfoClient.SymbolSelect(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async Task<SymbolSelectData> SymbolSelectAsync(
            string symbol,
            bool select = true,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default);
    }
}
```

**Request message:**

`SymbolSelectRequest { symbol: string, select: bool }`

**Reply message:**

`SymbolSelectReply { data: SymbolSelectData }`

---

### Code Example

```csharp
// --- Service wrapper (typed, data-returning) ---
var res = await _service.SymbolSelectAsync("EURUSD", select: true);
Console.WriteLine($"SymbolSelect(EURUSD): success={res.Success}");

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
var data = await _mt4.SymbolSelectAsync(
    symbol: "EURUSD",
    select: true,          // false = remove from Market Watch
    deadline: null,
    cancellationToken: cts.Token);

Console.WriteLine($"Selected: {data.Success}");
```

---

## 🔽 Input

| Parameter           | Type                | Description                                             |
| ------------------- | ------------------- | ------------------------------------------------------ |
| `symbol`            | `string`            | Symbol to select/deselect (e.g. `"EURUSD"`).           |
| `select`            | `bool`              | `true` = add to Market Watch; `false` = remove.        |
| `deadline`          | `DateTime?`         | Optional absolute **UTC** deadline for the call.       |
| `cancellationToken` | `CancellationToken` | Cooperative cancel for the call/retry loop.            |

---

## ⬆️ Output — `SymbolSelectData`

| Field     | Type   | Description                                   |
| --------- | ------ | --------------------------------------------- |
| `Success` | `bool` | `true` if the select/deselect was applied.    |

---

## 🎯 Purpose

Ensure a symbol is visible in Market Watch before quoting/trading it:

* Prepare a symbol before `QuoteAsync` / streaming / order placement.
* Trim Market Watch by deselecting symbols you no longer need.

---

## 🧩 Notes & Tips

* **Idempotent.** Selecting an already-selected symbol returns `Success = true` without side effects.
* **Exact naming.** Use the exact symbol from `SymbolsAsync()` (brokers may add suffixes like `EURUSD.r`).
* **Sugar.** `MT4Sugar.EnsureSelectedAsync(symbol)` wraps this call with `select: true`.

---

## ⚠️ Pitfalls

* **Unknown symbol.** Selecting a non-existent symbol returns `Success = false` — validate against `SymbolsAsync()` first.
* **Deselect in use.** Removing a symbol you are still streaming/trading can disrupt those operations.

---

## 🧪 Testing Suggestions

* **Happy path.** `SymbolSelectAsync("EURUSD", true)` → `Success = true`; a following `QuoteAsync("EURUSD")` returns prices.
* **Edge cases.** A bogus symbol → `Success = false`, no exception.
