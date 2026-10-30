# Getting Symbol Parameters

> **Request:** retrieve extended trading parameters for a symbol
> Provides detailed attributes such as precision, volume limits, currencies, and trade modes.

**API Information:**

* **SDK wrapper:** `MT4Account.SymbolParamsManyAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.AccountHelper`
* **Proto definition:** `SymbolParamsMany` (in the AccountHelper service)

### RPC

* **Service:** `mt4_term_api.AccountHelper`
* **Method:** `SymbolParamsMany(SymbolParamsManyRequest) → SymbolParamsManyReply`
* **Low‑level client (generated):** `AccountHelper.AccountHelperClient.SymbolParamsMany(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async Task<SymbolParamsManyData> SymbolParamsManyAsync(
            string? symbolName = null,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default);
    }
}
```

**Request message:**

`SymbolParamsManyRequest { symbolName?: string }`

**Reply message:**

`SymbolParamsManyReply { data: SymbolParamsManyData }`

---

### Code Example

```csharp
// --- Service wrapper (typed, data-returning) ---
var result = await _service.SymbolParamsManyAsync("EURUSD");
var p = result.SymbolInfos.FirstOrDefault();
Console.WriteLine($"{p?.SymbolName} Digits={p?.Digits} Vol[{p?.VolumeMin}..{p?.VolumeMax}/{p?.VolumeStep}] TradeMode={p?.TradeMode}");

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // widen if terminal is slow
var result = await _mt4.SymbolParamsManyAsync(
    symbolName: "EURUSD",
    deadline: null,
    cancellationToken: cts.Token);

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

## 🔽 Input

| Parameter               | Type                                        | Description                                          |
| ----------------------- | ------------------------------------------- | ---------------------------------------------------- |
| `symbol` / `symbolName` | `string` (required for single-symbol usage) | Symbol to request parameters for (e.g., `"EURUSD"`). |
| `deadline`              | `DateTime?` (optional)                      | Optional UTC deadline for request timeout.           |
| `cancellationToken`     | `CancellationToken` (optional)              | Token to cancel the operation.                       |

> Passing `null` for `symbolName` requests **all** symbols and returns a large list.

---

## ⬆️ Output

Returns `SymbolParamsManyData` containing detailed per-symbol settings:

| Field         | Type                      | Description                        |
| ------------- | ------------------------- | ---------------------------------- |
| `SymbolInfos` | `RepeatedField<SymbolParamsManyInfo>` | Detailed parameter set per symbol. |

**SymbolParamsManyInfo** (selected fields):

| Field            | Type     | Description                                  |
| ---------------- | -------- | -------------------------------------------- |
| `SymbolName`     | `string` | Symbol name                                  |
| `Digits`         | `int`    | Number of decimal places                     |
| `SpreadFloat`    | `bool`   | `true` if broker uses floating spread (flag) |
| `Bid`            | `double` | Current bid snapshot                         |
| `VolumeMin`      | `double` | Minimum lot volume                           |
| `VolumeMax`      | `double` | Maximum lot volume                           |
| `VolumeStep`     | `double` | Minimum lot increment                        |
| `CurrencyBase`   | `string` | Base currency                                |
| `CurrencyProfit` | `string` | Profit currency                              |
| `CurrencyMargin` | `string` | Margin currency                              |
| `TradeMode`      | `SP_ENUM_SYMBOL_TRADE_MODE`      | Trade mode for symbol — see enums below.     |
| `TradeExeMode`   | `SP_ENUM_SYMBOL_TRADE_EXECUTION` | Execution mode for symbol — see enums below. |

---

## 🧱 Related enums (from proto)

### `SP_ENUM_SYMBOL_TRADE_MODE` — field `TradeMode`

* `SymbolTradeModeDisabled = 0` — trading disabled for the symbol
* `SymbolTradeModeLongonly = 1` — only long (buy) positions
* `SymbolTradeModeShortonly = 2` — only short (sell) positions
* `SymbolTradeModeCloseonly = 3` — only closing of existing positions
* `SymbolTradeModeFull = 4` — unrestricted trading

### `SP_ENUM_SYMBOL_TRADE_EXECUTION` — field `TradeExeMode`

* `SymbolTradeExecutionRequest = 0` — request execution
* `SymbolTradeExecutionInstant = 1` — instant execution
* `SymbolTradeExecutionMarket = 2` — market execution
* `SymbolTradeExecutionExchange = 3` — exchange execution

---

## 🎯 Purpose

Retrieve a **comprehensive profile** of a trading instrument for validation and UI:

* Validate orders before placement (volumes, execution mode)
* Display symbol-specific trading conditions
* Power instrument configuration panels

---

## 🧩 Notes & Tips

* **One vs many.** For single-symbol UIs prefer passing the symbol; `null` loads all and can be heavy.
* **Spread value vs flag.** `SpreadFloat` is a boolean flag. To show numeric spread, compute `Ask - Bid` from `QuoteAsync`.
* **Stability.** Most parameters are stable; cache them per session. Refresh `Bid` via quotes when displaying live prices.

---

## ⚠️ Pitfalls

* **Symbol not enabled.** Disabled or unknown symbols may yield empty `SymbolInfos`.
* **Precision mismatch.** Use `Digits` from here when formatting prices/SL/TP to avoid broker rejections.

---

## 🧪 Testing Suggestions

* **Happy path.** For majors, `Digits`=5; volumes within broker limits; enums have meaningful values.
* **Edge cases.** Exotic metals/indices may have different step/contract rules.
* **Failure path.** Disconnect the terminal — expect a guarded exception, no crash.
