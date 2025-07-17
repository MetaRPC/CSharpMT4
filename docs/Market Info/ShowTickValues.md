# Getting Tick Value, Size, and Contract Size

> **Request:** retrieve tick value, tick size, and contract size for multiple symbols
> Useful for calculating profit/loss and position sizing.

---

### Code Example

```csharp
// Using service wrapper
await _service.ShowTickValues(new[] { "EURUSD", "XAUUSD" });

// Or directly from MT4Account
var result = await _mt4.TickValueWithSizeAsync(new[] { "EURUSD", "XAUUSD" });

foreach (var info in result.Infos)
{
    Console.WriteLine($"Symbol: {info.SymbolName}");
    Console.WriteLine($"  TickValue: {info.TradeTickValue}");
    Console.WriteLine($"  TickSize: {info.TradeTickSize}");
    Console.WriteLine($"  ContractSize: {info.TradeContractSize}");
}
```

---

### Method Signature

```csharp
Task<TickValueWithSizeData> TickValueWithSizeAsync(
    string[] symbols,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default
)
```

---

## 🔽 Input

* **`symbols`** (`string[]`) — required. Array of trading symbols to query (e.g., `"EURUSD"`, `"XAUUSD"`).
* **`deadline`** (`DateTime?`, optional) — timeout control.
* **`cancellationToken`** (`CancellationToken`, optional) — to cancel the request.

---

## ⬆️ Output

Returns a `TickValueWithSizeData` object containing:

| Field   | Type                                | Description                               |
| ------- | ----------------------------------- | ----------------------------------------- |
| `Infos` | `List<TickValueWithSizeSymbolInfo>` | Tick-related information for each symbol. |

Each `TickValueWithSizeSymbolInfo` includes:

| Field               | Type     | Description                                      |
| ------------------- | -------- | ------------------------------------------------ |
| `SymbolName`        | `string` | Symbol name (e.g., "EURUSD")                     |
| `TradeTickValue`    | `double` | Value of one tick movement in account currency   |
| `TradeTickSize`     | `double` | Minimum price change for the symbol              |
| `TradeContractSize` | `double` | Number of units per lot (usually 100,000 for FX) |

---

## 🎯 Purpose

This method provides core trading parameters used in calculations such as:

* Profit/loss estimation
* Pip value conversions
* Position sizing formulas

It is essential for both **manual trade interfaces** and **automated strategy logic**.
