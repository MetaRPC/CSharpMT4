# Market Info — Overview

This section contains **market data and instrument metadata** methods for MT4. Use it to discover symbols, read quotes, fetch historical candles, and get contract/tick parameters.

---

## 📂 Files in this folder

* [AllSymbols.md](AllSymbols.md)
  Full catalogue of all instruments available in the terminal (names + indices).

* [Quote.md](Quote.md)
  Latest **bid/ask** snapshot for a single symbol.

* [QuotesMany.md](QuotesMany.md)
  Snapshot quotes for **multiple** symbols at once; handy before subscribing to ticks.

* [RealTimeQuotes.md](RealTimeQuotes.md)
  Subscribe to **live ticks** for a symbol.

* [QuoteHistory.md](QuoteHistory.md)
  Historical **OHLC** data (candles) for a symbol over a timeframe.

* [SymbolInfo.md](SymbolInfo.md)
  Lightweight info (digits/float-spread flag/bid) for a symbol.

* [SymbolParams.md](SymbolParams.md)
  Extended parameters: digits, volume rules, currencies, trade & execution modes.

* [TickValues.md](TickValues.md)
  **TickValue / TickSize / ContractSize** for one or more symbols (P/L & sizing math).

* [Bars.md](Bars.md)
  **OHLC bars** (with volume/spread) for a symbol/timeframe over a UTC range.

* [SymbolSelect.md](SymbolSelect.md)
  Add/remove a symbol in **Market Watch** before quoting/trading it.

* [ServerTimezone.md](ServerTimezone.md)
  Broker **server timezone / UTC offset** for converting timestamps.

---

## ⚡ Typical Workflows

### 1) Build a watchlist and show live prices

```csharp
// Discover & pick symbols
var all = await _service.SymbolsAsync();

// Get initial snapshot for the shortlist
var many = await _service.QuoteManyAsync(new[] { "EURUSD", "GBPUSD" });

// (optional) Subscribe to ticks (see Streaming section or RealTimeQuotes)
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await foreach (var tick in _service.OnSymbolTickAsync(new[] { "EURUSD" }, cts.Token)) { /* ... */ break; }
```

### 2) Validate trading inputs and display instrument info

```csharp
// Fetch parameters for validation and formatting
var p = await _service.SymbolParamsManyAsync("EURUSD");

// Compute monetary values for risk & P/L
var tv = await _service.TickValueWithSizeAsync(new[] { "EURUSD" });
```

### 3) Chart a symbol

```csharp
// Pull historical candles and render/print
var hist = await _service.QuoteHistoryAsync(
    "EURUSD", ENUM_QUOTE_HISTORY_TIMEFRAME.QhPeriodH1,
    DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
```

---

## ✅ Best Practices

1. **Key by name, not index.** `SymbolIndex` can change across sessions; persist `SymbolName`.
2. **Format by Digits.** Use `Digits` from *SymbolParams* for UI; keep raw doubles for math.
3. **Batch when possible.** Prefer *QuotesMany* and *TickValues* to reduce round‑trips.
4. **Time is UTC.** Quotes and candles carry UTC timestamps—convert only at presentation.
5. **Broker suffixes are real.** Treat `EURUSD.m` / `XAUUSD-RAW` as distinct symbols.

---

## 🎯 Purpose

The **Market Info** block helps you:

* Discover and organize instruments.
* Read **current** and **historical** market data.
* Validate and format symbol‑specific values (digits, steps, currencies).
* Compute monetary effects (tick value/size, contract size) for risk & P/L.

---

👉 Use this page as a **map**. Jump into each `.md` file for full method details, parameters, and pitfalls.
