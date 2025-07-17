# Getting All Available Symbols

> **Request:** retrieve a list of all symbols (instruments) available in the terminal
> Returns all symbol names and their corresponding internal indices.

---

### Code Example

```csharp
// Using service wrapper
await _service.ShowAllSymbols();

// Or directly from MT4Account
var symbols = await _mt4.SymbolsAsync();

foreach (var entry in symbols.SymbolNameInfos)
{
    Console.WriteLine($"Symbol: {entry.SymbolName}, Index: {entry.SymbolIndex}");
}
```

---

### Method Signature

```csharp
Task<SymbolNamesData> SymbolsAsync(
    DateTime? deadline = null,
    CancellationToken cancellationToken = default
)
```

---

## 🔽 Input

No required parameters.

Optional:

* **`deadline`** (`DateTime?`) — optional timeout.
* **`cancellationToken`** (`CancellationToken`) — to cancel the request.

---

## ⬆️ Output

Returns a `SymbolNamesData` object containing:

| Field             | Type                        | Description                           |
| ----------------- | --------------------------- | ------------------------------------- |
| `SymbolNameInfos` | `List<SymbolNameIndexPair>` | List of all symbols and their indices |

Each `SymbolNameIndexPair` includes:

| Field         | Type     | Description                  |
| ------------- | -------- | ---------------------------- |
| `SymbolName`  | `string` | Name of the trading symbol   |
| `SymbolIndex` | `int`    | Internal index of the symbol |

---

## 🎯 Purpose

Use this method to enumerate all available instruments (e.g. `"EURUSD"`, `"XAUUSD"`, `"USDJPY"`, etc.) within the MT4 terminal.

Useful for:

* Populating UI dropdowns or symbol selectors
* Building watchlists
* Performing batch requests over multiple symbols
