# Getting a Quote for Symbol

> **Request:** fetch the latest quote for a given symbol
> Returns current bid/ask prices, spread, and time for a specified trading instrument.

---

### Code Example

```csharp
// Using service wrapper
await _service.ShowQuote("EURUSD");

// Or directly from MT4Account
var quote = await _mt4.QuoteAsync("EURUSD");
Console.WriteLine($"Bid: {quote.Bid}, Ask: {quote.Ask}, Spread: {quote.Spread}, Time: {quote.Time}");
```

---

### Method Signature

```csharp
Task<QuoteData> QuoteAsync(
    string symbol,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default
)
```

---

## 🔽 Input

* **`symbol`** (`string`) — required. Symbol to get the quote for (e.g., `"EURUSD"`).
* **`deadline`** (`DateTime?`, optional) — optional deadline for timeout.
* **`cancellationToken`** (`CancellationToken`, optional) — optional cancellation token.

---

## ⬆️ Output

Returns a **`QuoteData`** object with the following fields:

| Field    | Type     | Description                            |
| -------- | -------- | -------------------------------------- |
| `Bid`    | `double` | Current bid price                      |
| `Ask`    | `double` | Current ask price                      |
| `Spread` | `double` | Spread between ask and bid (in points) |
| `Time`   | `string` | UTC timestamp of the quote             |

---

## 🎯 Purpose

Use this method to retrieve **live market pricing** for a specific symbol, including bid/ask prices and calculated spread.

It's commonly used for:

* Building order entry forms or pricing dashboards
* Quoting prices in trading interfaces
* Spread monitoring and alerting systems

This method gives you the most up-to-date snapshot of market conditions for the given instrument.
