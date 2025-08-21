# Getting All Available Symbols

> **Request:** retrieve a list of all symbols (instruments) available in the terminal
> Returns all symbol names and their corresponding internal indices.

---

### Code Example

```csharp
// --- Quick use (service wrapper) ---
// Prints each symbol with its index inside the method.
await _service.ShowAllSymbols();

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)); // keep short; bump if your terminal is slow
var symbols = await _mt4.SymbolsAsync(
    deadline: null,
    cancellationToken: cts.Token);

foreach (var entry in symbols.SymbolNameInfos)
{
    Console.WriteLine($"Symbol: {entry.SymbolName}, Index: {entry.SymbolIndex}");
}
```

---

### Method Signature

```csharp
// Service wrapper
Task ShowAllSymbols();
```

```csharp
// Low-level account call
Task<SymbolsData> SymbolsAsync(
    DateTime? deadline = null,
    CancellationToken cancellationToken = default
);
```

---

## 🔽 Input

No required parameters.

Optional:

* **`deadline`** (`DateTime?`) — optional UTC deadline for request timeout.
* **`cancellationToken`** (`CancellationToken`) — token to cancel the operation.

---

## ⬆️ Output

Returns a `SymbolsData` object containing:

| Field             | Type                    | Description                                  |
| ----------------- | ----------------------- | -------------------------------------------- |
| `SymbolNameInfos` | `IList<SymbolNameInfo>` | All available symbols with their MT4 indices |

Each `SymbolNameInfo` includes:

| Field         | Type     | Description                         |
| ------------- | -------- | ----------------------------------- |
| `SymbolName`  | `string` | Trading symbol name (e.g. `EURUSD`) |
| `SymbolIndex` | `int`    | Internal symbol index               |

---

## 🎯 Purpose

Enumerate all instruments available to the logged-in account. Useful for:

* Populating UI dropdowns or symbol selectors
* Building watchlists
* Running batch requests over multiple symbols

---

## 🧩 Notes & Tips

* **Broker suffixes.** Some brokers append suffixes to symbol names (e.g., `EURUSD.r`, `XAUUSD.m`). Use exactly the names returned here when requesting quotes or placing orders.
* **Account scope.** The list may vary by account type/permissions. Cache the result per session to avoid repeated calls.
