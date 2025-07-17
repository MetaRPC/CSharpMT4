# Getting Quotes for Multiple Symbols with Ticks

> **Request:** fetch quotes for multiple symbols and stream real-time price ticks
> Combines a one-time quote snapshot (`QuoteManyAsync`) with a live tick stream (`OnSymbolTickAsync`) for each symbol.

---

### Code Example

```csharp
// Using service wrapper
await _service.ShowQuotesMany(new[] { "EURUSD", "GBPUSD" });

// Or directly from MT4Account
var symbols = new[] { "EURUSD", "GBPUSD" };
var quotes = await _mt4.QuoteManyAsync(symbols);

foreach (var symbol in symbols)
{
    await foreach (var tick in _mt4.OnSymbolTickAsync(new[] { symbol }))
    {
        var q = tick.SymbolTick;
        var time = q.Time?.ToDateTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "n/a";
        Console.WriteLine($"Quote for {q.Symbol}: Bid={q.Bid}, Ask={q.Ask}, Time={time}");
        break; // for test/demo
    }
}
```

---

## 🔽 Input

### For `QuoteManyAsync`:

* **`symbols`** (`string[]`) — required. Array of trading symbols (e.g., `["EURUSD", "USDJPY"]`).

### For `OnSymbolTickAsync`:

* **`symbols`** (`string[]`) — required. List of symbols to subscribe for real-time tick updates.
* **`cancellationToken`** (`CancellationToken`, optional) — used to stop the stream.

---

### Method Signatures

```csharp
Task<List<QuoteData>> QuoteManyAsync(
    string[] symbols,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default
)

IAsyncEnumerable<SymbolTickData> OnSymbolTickAsync(
    string[] symbols,
    [EnumeratorCancellation] CancellationToken cancellationToken = default
)
```

---

## ⬆️ Output

### From `QuoteManyAsync`

Returns `List<QuoteData>` — see single `QuoteData` structure:

| Field    | Type     | Description       |
| -------- | -------- | ----------------- |
| `Bid`    | `double` | Bid price         |
| `Ask`    | `double` | Ask price         |
| `Spread` | `double` | Spread in points  |
| `Time`   | `string` | UTC time of quote |

---

### From `OnSymbolTickAsync`

Returns a stream (`IAsyncEnumerable<SymbolTickData>`) with:

| Field        | Type        | Description                    |
| ------------ | ----------- | ------------------------------ |
| `SymbolTick` | `QuoteData` | Real-time tick data for symbol |

---

## 🎯 Purpose

This method gives you both:

1. **Snapshot quote info** via `QuoteManyAsync` — useful for instant display or order validation
2. **Live tick stream** via `OnSymbolTickAsync` — for real-time pricing, UI updates, or alerts

Useful in trading dashboards, price monitors, and any interface where users watch multiple symbols simultaneously.

---

### ❓ Notes

* `OnSymbolTickAsync` returns an endless stream — make sure to manage cancellation.
* You can use `.Take(1)` or manual `break;` logic for testing/demo.
