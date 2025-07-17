# Getting Basic Symbol Info

> **Request:** retrieve lightweight market info for a specific symbol
> Extracts a subset of detailed symbol parameters — mainly price precision, spread, and bid.

---

### Code Example

```csharp
// Using service wrapper
await _service.ShowSymbolInfo("EURUSD");

// Or directly from MT4Account
var info = await _mt4.SymbolParamsManyAsync("EURUSD");

foreach (var param in info.SymbolInfos)
{
    Console.WriteLine($"{param.SymbolName} — Digits: {param.Digits}, Spread: {param.SpreadFloat}, Bid: {param.Bid}");
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

* **`symbol`** (`string`) — required. Symbol to retrieve info for (e.g., `"EURUSD"`).
* **`deadline`** (`DateTime?`, optional) — timeout.
* **`cancellationToken`** (`CancellationToken`, optional) — for cancellation control.

---

## ⬆️ Output

Returns a `SymbolParamsManyData` object with:

| Field         | Type                     | Description                            |
| ------------- | ------------------------ | -------------------------------------- |
| `SymbolInfos` | `List<SymbolParamsInfo>` | Parameters for the requested symbol(s) |

Filtered fields from `SymbolParamsInfo` used in this example:

| Field         | Type     | Description                       |
| ------------- | -------- | --------------------------------- |
| `SymbolName`  | `string` | Symbol name                       |
| `Digits`      | `int`    | Number of decimal places          |
| `SpreadFloat` | `double` | Current floating spread in points |
| `Bid`         | `double` | Current bid price                 |

---

## 🎯 Purpose

This method variation is intended to retrieve **just the most relevant pricing attributes** of a symbol — useful for quick overviews, quote panels, or compact UIs.

It is built on top of the more general `SymbolParamsManyAsync` method, but narrows the output to **precision and market pricing** only.

---

### ❗Note

This is a simplified usage pattern of the `SymbolParamsManyAsync` method. For full parameter access (e.g., volume limits, execution modes), refer to the [Get Symbol Parameters](#get-symbol-parameters) documentation.
