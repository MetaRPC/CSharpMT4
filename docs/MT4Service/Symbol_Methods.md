# MT4Service — Symbol Methods

> Quotes, symbol inventory, history/bars, tick values and Market Watch, plus **value getters** for the common single properties.

→ builds on low-level **[Market Info — Overview](../MT5Account/Market%20Info/Market_Info_Overview.md)**

---

## 📋 Data methods

| Method | Returns | Low-level spec |
| ------ | ------- | -------------- |
| `SymbolsAsync(deadline, ct)` | `SymbolsData` | [AllSymbols](../MT5Account/Market%20Info/AllSymbols.md) |
| `SymbolParamsManyAsync(symbol, deadline, ct)` | `SymbolParamsManyData` | [SymbolParams](../MT5Account/Market%20Info/SymbolParams.md) |
| `QuoteAsync(symbol, deadline, ct)` | `QuoteData` | [Quote](../MT5Account/Market%20Info/Quote.md) |
| `QuoteManyAsync(symbols, deadline, ct)` | `QuoteManyData` | [QuotesMany](../MT5Account/Market%20Info/QuotesMany.md) |
| `QuoteHistoryAsync(symbol, timeframe, from, to, …)` | `QuoteHistoryData` | [QuoteHistory](../MT5Account/Market%20Info/QuoteHistory.md) |
| `BarsAsync(symbol, timeframe, fromUtc, toUtc, …)` | `QuoteHistoryBarsData` | [Bars](../MT5Account/Market%20Info/Bars.md) |
| `TickValueWithSizeAsync(symbols, deadline, ct)` | `TickValueWithSizeData` | [TickValues](../MT5Account/Market%20Info/TickValues.md) |
| `SymbolSelectAsync(symbol, select = true, …)` | `SymbolSelectData` | [SymbolSelect](../MT5Account/Market%20Info/SymbolSelect.md) |
| `ServerTimezoneAsync(deadline, ct)` | `ServerTimezoneData` | [ServerTimezone](../MT5Account/Market%20Info/ServerTimezone.md) |

> `QuoteHistoryAsync` takes `ENUM_QUOTE_HISTORY_TIMEFRAME` (`Qh*`); `BarsAsync` takes `QUOTE_HISTORY_TIMEFRAME` (`Qhb*`) — don't mix the two enums.

## 📋 Value getters

| Method | Returns | Value |
| ------ | ------- | ----- |
| `GetDigitsAsync(symbol, …)` | `int` | Price digits (from `SymbolParams`) |
| `GetPointAsync(symbol, …)` | `double` | Point size `10^-digits` |
| `GetSpreadAsync(symbol, …)` | `double` | Spread in points, **computed** `(Ask-Bid)/point` |
| `GetBidAsync(symbol, …)` | `double` | Current bid |
| `GetAskAsync(symbol, …)` | `double` | Current ask |
| `GetVolumeMinAsync(symbol, …)` | `double` | Minimum volume (lots) |
| `GetVolumeMaxAsync(symbol, …)` | `double` | Maximum volume (lots) |
| `GetVolumeStepAsync(symbol, …)` | `double` | Volume step (lots) |

---

## 💡 Usage examples

### 1) Price + spread in one glance

```csharp
var q = await service.QuoteAsync("EURUSD");
Console.WriteLine($"{q.Symbol}: {q.Bid}/{q.Ask}  spread={await service.GetSpreadAsync("EURUSD"):F1}pt");
```

### 2) Validate before trading

```csharp
int    digits = await service.GetDigitsAsync("EURUSD");
double step   = await service.GetVolumeStepAsync("EURUSD");
double min    = await service.GetVolumeMinAsync("EURUSD");
// round price to `digits`, snap volume to `step`, clamp to `min` before OrderSend
```

### 3) Candles for a chart

```csharp
var bars = await service.BarsAsync(
    "EURUSD", QUOTE_HISTORY_TIMEFRAME.QhbPeriodH1,
    DateTime.UtcNow.AddHours(-6), DateTime.UtcNow);
Console.WriteLine($"{bars.Bars.Count} bar(s)");
```

---

## 🧩 Notes & Tips

* **Exact symbol names.** Use names from `SymbolsAsync()` — brokers add suffixes (`EURUSD.r`).
* **Spread is derived.** `GetSpreadAsync` computes from a live quote; `SpreadFloat` in symbol params is a flag, not a value.
* **Batch where possible.** Prefer `QuoteManyAsync` / `TickValueWithSizeAsync` over many single calls.
