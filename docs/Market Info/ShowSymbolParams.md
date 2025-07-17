# Getting Symbol Parameters

> **Request:** retrieve extended trading parameters for a symbol
> Provides detailed attributes such as precision, volume limits, currencies, and trade modes.

---

### Code Example

```csharp
// Using service wrapper
await _service.ShowSymbolParams("EURUSD");

// Or directly from MT4Account
var result = await _mt4.SymbolParamsManyAsync("EURUSD");

foreach (var param in result.SymbolInfos)
{
    Console.WriteLine($"Symbol: {param.SymbolName}");
    Console.WriteLine($"  Digits: {param.Digits}");
    Console.WriteLine($"  SpreadFloat: {param.SpreadFloat}");
    Console.WriteLine($"  Bid: {param.Bid}");
    Console.WriteLine($"  VolumeMin: {param.VolumeMin}");
    Console.WriteLine($"  VolumeMax: {param.VolumeMax}");
    Console.WriteLine($"  VolumeStep: {param.VolumeStep}");
    Console.WriteLine($"  CurrencyBase: {param.CurrencyBase}");
    Console.WriteLine($"  CurrencyProfit: {param.CurrencyProfit}");
    Console.WriteLine($"  CurrencyMargin: {param.CurrencyMargin}");
    Console.WriteLine($"  TradeMode: {param.TradeMode}");
    Console.WriteLine($"  TradeExeMode: {param.TradeExeMode}");
    Console.WriteLine();
}
```

---

### Method Signature

```csharp
Task<SymbolParamsManyData> SymbolParamsManyAsync(
    string symbol,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default
)
```

---

## 🔽 Input

* **`symbol`** (`string`) — required. Symbol to request parameters for (e.g., `"EURUSD"`).
* **`deadline`** (`DateTime?`, optional) — optional timeout.
* **`cancellationToken`** (`CancellationToken`, optional) — optional cancellation.

---

## ⬆️ Output

Returns a `SymbolParamsManyData` object containing:

| Field         | Type                     | Description                                 |
| ------------- | ------------------------ | ------------------------------------------- |
| `SymbolInfos` | `List<SymbolParamsInfo>` | List of detailed parameter sets per symbol. |

Each `SymbolParamsInfo` includes:

| Field            | Type     | Description                                      |
| ---------------- | -------- | ------------------------------------------------ |
| `SymbolName`     | `string` | Name of the symbol                               |
| `Digits`         | `int`    | Number of decimal places                         |
| `SpreadFloat`    | `double` | Current floating spread in points                |
| `Bid`            | `double` | Current bid price                                |
| `VolumeMin`      | `double` | Minimum allowed lot volume                       |
| `VolumeMax`      | `double` | Maximum allowed lot volume                       |
| `VolumeStep`     | `double` | Minimum lot increment                            |
| `CurrencyBase`   | `string` | Base currency of the symbol                      |
| `CurrencyProfit` | `string` | Profit currency for trades in this symbol        |
| `CurrencyMargin` | `string` | Margin currency used for this symbol             |
| `TradeMode`      | `int`    | Trade mode (e.g., disabled, long-only, etc.)     |
| `TradeExeMode`   | `int`    | Execution mode (e.g., market, instant execution) |

---

## 🎯 Purpose

Use this method to retrieve a **comprehensive profile** of a trading instrument, including trading rules, volume constraints, and precision.

Useful for:

* Validating orders before placement
* Displaying symbol-specific trading conditions
* Building instrument configuration panels
