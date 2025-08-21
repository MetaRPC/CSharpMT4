# Getting Tick Value, Size, and Contract Size

> **Request:** retrieve tick value, tick size, and contract size for multiple symbols
> Useful for calculating profit/loss and position sizing.

---

### Code Example

```csharp
// --- Quick use (service wrapper) ---
await _service.ShowTickValues(new[] { "EURUSD", "XAUUSD" });

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var result = await _mt4.TickValueWithSizeAsync(
    symbolNames: new[] { "EURUSD", "XAUUSD" },
    deadline: null,
    cancellationToken: cts.Token);

foreach (var info in result.Infos)
{
    Console.WriteLine($"Symbol: {info.SymbolName}");
    Console.WriteLine($"  TickValue: {info.TradeTickValue}");
    Console.WriteLine($"  TickSize: {info.TradeTickSize}");
    Console.WriteLine($"  ContractSize: {info.TradeContractSize}");
}
```

---

### Method Signatures

```csharp
// Service wrapper
Task ShowTickValues(string[] symbols);
```

```csharp
// Low-level account call
Task<TickValueWithSizeData> TickValueWithSizeAsync(
    IEnumerable<string> symbolNames,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default
);
```

---

## 🔽 Input

| Parameter                 | Type                             | Description                                                 |
| ------------------------- | -------------------------------- | ----------------------------------------------------------- |
| `symbols` / `symbolNames` | `IEnumerable<string>` (required) | One or more trading symbols (e.g., `"EURUSD"`, `"XAUUSD"`). |
| `deadline`                | `DateTime?` (optional)           | Optional UTC deadline for request timeout.                  |
| `cancellationToken`       | `CancellationToken` (optional)   | Token to cancel the operation.                              |

---

## ⬆️ Output

Returns **`TickValueWithSizeData`** containing:

| Field   | Type                                 | Description                   |
| ------- | ------------------------------------ | ----------------------------- |
| `Infos` | `IList<TickValueWithSizeSymbolInfo>` | Tick-related info per symbol. |

**TickValueWithSizeSymbolInfo** fields:

| Field               | Type     | Description                                                         |
| ------------------- | -------- | ------------------------------------------------------------------- |
| `SymbolName`        | `string` | Trading symbol name.                                                |
| `TradeTickValue`    | `double` | Monetary value of **one tick** in the account currency.             |
| `TradeTickSize`     | `double` | Minimal price increment (tick size).                                |
| `TradeContractSize` | `double` | Units per 1.0 lot (broker-defined; e.g., 100000 for many FX pairs). |

---

## 🎯 Purpose

Provide core parameters for:

* Profit/loss estimation
* Pip value conversions
* Position sizing formulas

---

## 🧩 Notes & Tips

* **Pip vs tick.** A pip is not always equal to one tick. Derive pip value as:
  `pipValue = TradeTickValue * (pipSize / TradeTickSize)`
  where `pipSize` depends on the instrument (e.g., `0.0001` for 5-digit FX pairs, `0.01` for JPY pairs).
* **Account currency.** `TradeTickValue` is already in the **account currency**; no extra conversion needed unless you aggregate across accounts.
* **Contract size.** Metals/indices/CFDs may use different contract sizes than FX. Always read `TradeContractSize` from the API instead of hardcoding.
* **Caching.** These parameters are fairly stable; cache per session and refresh when switching accounts or symbol groups.

---

## ⚠️ Pitfalls

* **Wrong pip math.** Using `Ask-Bid` directly as “pips” without normalizing by `TradeTickSize` leads to wrong P/L.
* **Assuming FX-only sizes.** Not all instruments use 100000 units/lot; indices/commodities vary by broker.

---

## 🧪 Testing Suggestions

* **Happy path.** For majors, `TradeTickSize`≈`1e-5` and `TradeContractSize`≈`100000`.
* **Edge cases.** JPY pairs have `TradeTickSize`≈`0.001`; metals/indices return broker-specific contract sizes.
* **Failure path.** Invalid/disabled symbol should not crash; handle empty or missing entries gracefully.
