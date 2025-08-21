# Streaming Real-Time Quotes for a Symbol

> **Request:** subscribe to real-time bid/ask price updates for a given symbol
> Opens a streaming channel to receive tick-by-tick quotes as the market changes.

---

### Code Example

```csharp
// Using service wrapper
await _service.ShowRealTimeQuotes("EURUSD");

// Or directly from MT4Account
await foreach (var tick in _mt4.OnSymbolTickAsync(new[] { "EURUSD" }))
{
    Console.WriteLine($"Tick: {tick.SymbolTick.Symbol} {tick.SymbolTick.Bid}/{tick.SymbolTick.Ask} @ {tick.SymbolTick.Time}");
    break; // demo
}
```

---

### Method Signature

```csharp
IAsyncEnumerable<SymbolTickData> OnSymbolTickAsync(
    string[] symbols,
    [EnumeratorCancellation] CancellationToken cancellationToken = default
)
```

---

## 🔽 Input

* **`symbols`** (`string[]`) — required. Array of symbols to subscribe to (even if only one symbol like `["EURUSD"]`).
* **`cancellationToken`** (`CancellationToken`, optional) — stream control.

---

## ⬆️ Output

Returns a stream (`IAsyncEnumerable<SymbolTickData>`) where each item contains:

### Structure: `SymbolTickData`

| Field        | Type        | Description                 |
| ------------ | ----------- | --------------------------- |
| `SymbolTick` | `QuoteData` | Real-time price data object |

Structure of `QuoteData`:

| Field    | Type     | Description            |
| -------- | -------- | ---------------------- |
| `Symbol` | `string` | Symbol name            |
| `Bid`    | `double` | Current bid price      |
| `Ask`    | `double` | Current ask price      |
| `Time`   | `string` | UTC timestamp of quote |

---

## 🎯 Purpose

Use this method to receive **live market data** for any symbol — suitable for:

* Streaming UI quote panels
* Triggering alerts based on price movement
* Feeding pricing logic in automated systems

It's optimized for real-time trading environments and pricing overlays.

---

### ❗ Note

This stream is **continuous** — production code should manage cancellation via tokens or conditional exits to prevent runaway processes.
