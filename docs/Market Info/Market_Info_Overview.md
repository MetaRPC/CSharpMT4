# Market Info — Overview

This section contains **market data and instrument metadata** methods for MT4. Use it to discover symbols, read quotes, fetch historical candles, and get contract/tick parameters.

---

## 📂 Files in this folder

* [ShowAllSymbols.md](ShowAllSymbols.md)
  Full catalogue of all instruments available in the terminal (names + indices).

* [ShowQuote.md](ShowQuote.md)
  Latest **bid/ask** snapshot for a single symbol.

* [ShowQuotesMany.md](ShowQuotesMany.md)
  Snapshot quotes for **multiple** symbols at once; handy before subscribing to ticks.

* [ShowRealTimeQuotes.md](ShowRealTimeQuotes.md)
  Subscribe to **live ticks** for a symbol.

* [ShowQuoteHistory.md](ShowQuoteHistory.md)
  Historical **OHLC** data (candles) for a symbol over a timeframe.

* [ShowSymbolInfo.md](ShowSymbolInfo.md)
  Lightweight info (digits/float-spread flag/bid) for a symbol.

* [ShowSymbolParams.md](ShowSymbolParams.md)
  Extended parameters: digits, volume rules, currencies, trade & execution modes.

* [ShowTickValues.md](ShowTickValues.md)
  **TickValue / TickSize / ContractSize** for one or more symbols (P/L & sizing math).

---

## ⚡ Typical Workflows

### 1) Build a watchlist and show live prices

```csharp
// Discover & pick symbols
await _service.ShowAllSymbols();

// Get initial snapshot for the shortlist
await _service.ShowQuotesMany(new[] { "EURUSD", "GBPUSD" });

// (optional) Subscribe to ticks (see Streaming section or ShowRealTimeQuotes)
await _service.ShowRealTimeQuotes("EURUSD", timeoutSeconds: 5);
```

### 2) Validate trading inputs and display instrument info

```csharp
// Fetch parameters for validation and formatting
await _service.ShowSymbolParams("EURUSD");

// Compute monetary values for risk & P/L
await _service.ShowTickValues(new[] { "EURUSD" });
```

### 3) Chart a symbol

```csharp
// Pull historical candles and render/print
await _service.ShowQuoteHistory("EURUSD");
```

---

## ✅ Best Practices

1. **Key by name, not index.** `SymbolIndex` can change across sessions; persist `SymbolName`.
2. **Format by Digits.** Use `Digits` from *ShowSymbolParams* for UI; keep raw doubles for math.
3. **Batch when possible.** Prefer *ShowQuotesMany* and *ShowTickValues* to reduce round‑trips.
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
