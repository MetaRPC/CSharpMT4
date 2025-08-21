# Getting Basic Symbol Info

> **Request:** retrieve lightweight market info for a specific symbol
> Extracts a subset of detailed symbol parameters — mainly price precision, floating-spread flag, and bid.

---

### Code Example

```csharp
// --- Quick use (service wrapper) ---
await _service.ShowSymbolInfo("EURUSD");

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
var info = await _mt4.SymbolParamsManyAsync(
    symbolName: "EURUSD",
    deadline: null,
    cancellationToken: cts.Token);

foreach (var param in info.SymbolInfos)
{
    Console.WriteLine($"{param.SymbolName} — Digits: {param.Digits}, SpreadFloat: {param.SpreadFloat}, Bid: {param.Bid}");
}
```

---

### Method Signatures

```csharp
// Service wrapper
Task ShowSymbolInfo(string symbol);
```

```csharp
// Low-level account call
Task<SymbolParamsManyData> SymbolParamsManyAsync(
    string? symbolName = null,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default);
```

---

## 🔽 Input

| Parameter               | Type                                        | Description                                             |
| ----------------------- | ------------------------------------------- | ------------------------------------------------------- |
| `symbol` / `symbolName` | `string` (required for single-symbol usage) | Trading symbol to retrieve info for (e.g., `"EURUSD"`). |
| `deadline`              | `DateTime?` (optional)                      | Optional UTC deadline for request timeout.              |
| `cancellationToken`     | `CancellationToken` (optional)              | Token to cancel the operation.                          |

> Passing `null` for `symbolName` requests **all** symbols; for this example we supply a single symbol.

---

## ⬆️ Output

Returns a `SymbolParamsManyData` object with:

| Field         | Type                      | Description                             |
| ------------- | ------------------------- | --------------------------------------- |
| `SymbolInfos` | `IList<SymbolParamsInfo>` | Parameters for the requested symbol(s). |

Selected fields from **`SymbolParamsInfo`** used in this example:

| Field         | Type     | Description                                       |
| ------------- | -------- | ------------------------------------------------- |
| `SymbolName`  | `string` | Symbol name.                                      |
| `Digits`      | `int`    | Number of decimal places in price.                |
| `SpreadFloat` | `bool`   | `true` if broker uses floating spread for symbol. |
| `Bid`         | `double` | Current bid price snapshot.                       |

> Need the actual spread value? Derive it from quotes (`spread = Ask - Bid`) via `QuoteAsync`/`QuoteManyAsync`.

---

## 🎯 Purpose

Quickly obtain the most relevant **pricing attributes** of a symbol — useful for compact UIs, quote panels, or quick diagnostics, without pulling every parameter.

---

## 🧩 Notes & Tips

* **Exact naming.** Use symbol names exactly as returned by `SymbolsAsync()` (brokers may add suffixes like `EURUSD.r`).
* **One vs many.** `SymbolParamsManyAsync(null, ...)` returns all symbols — heavier on large instrument lists; prefer single-symbol calls for UI interactions.
* **Floating spread flag.** `SpreadFloat` is a boolean flag (not the value). To show spread in pips/points, compute from a live quote.

---

## ⚠️ Pitfalls

* **Symbol not found.** A non-existing symbol string yields empty `SymbolInfos`. Validate before using.
* **Caching.** Parameters change infrequently, but `Bid` is a snapshot — refresh if you display it as a price.

---

## 🧪 Testing Suggestions

* **Happy path.** For a major (e.g., `EURUSD`), `Digits` is 5, `SymbolName` matches input, `SymbolInfos` non-empty.
* **Edge cases.** Exotic symbols may have different `Digits`; `SymbolInfos` may be empty if the symbol is disabled.
* **Failure path.** Simulate disconnect — expect a clear exception from the guard (no crash).
