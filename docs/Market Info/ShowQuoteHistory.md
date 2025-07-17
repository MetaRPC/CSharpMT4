# Getting Historical Quote Data

> **Request:** retrieve historical OHLC (candlestick) data for a given symbol
> Returns a list of time-based bars with open, high, low, close prices over a defined time range and timeframe.

---

### Code Example

```csharp
// Using service wrapper
await _service.ShowQuoteHistory("EURUSD");

// Or directly from MT4Account
var from = DateTime.UtcNow.AddDays(-5);
var to = DateTime.UtcNow;
var timeframe = ENUM_QUOTE_HISTORY_TIMEFRAME.QhPeriodH1;

var history = await _mt4.QuoteHistoryAsync("EURUSD", timeframe, from, to);

foreach (var candle in history.HistoricalQuotes)
{
    Console.WriteLine($"[{candle.Time}] O: {candle.Open} H: {candle.High} L: {candle.Low} C: {candle.Close}");
}
```

---

### Method Signature

```csharp
Task<QuoteHistoryData> QuoteHistoryAsync(
    string symbol,
    ENUM_QUOTE_HISTORY_TIMEFRAME timeframe,
    DateTime from,
    DateTime to,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default
)
```

---

## 🔽 Input

* **`symbol`** (`string`) — required. Trading symbol to request history for (e.g., `"EURUSD"`).
* **`timeframe`** (`ENUM_QUOTE_HISTORY_TIMEFRAME`) — required. Bar timeframe:

### ENUM: `ENUM_QUOTE_HISTORY_TIMEFRAME`

| Value         | Description    |
| ------------- | -------------- |
| `QhPeriodM1`  | 1-minute bars  |
| `QhPeriodM5`  | 5-minute bars  |
| `QhPeriodM15` | 15-minute bars |
| `QhPeriodM30` | 30-minute bars |
| `QhPeriodH1`  | 1-hour bars    |
| `QhPeriodH4`  | 4-hour bars    |
| `QhPeriodD1`  | Daily bars     |
| `QhPeriodW1`  | Weekly bars    |
| `QhPeriodMN1` | Monthly bars   |

* **`from`** (`DateTime`) — required. Start of the historical range (UTC).
* **`to`** (`DateTime`) — required. End of the historical range (UTC).
* **`deadline`** (`DateTime?`, optional) — optional timeout.
* **`cancellationToken`** (`CancellationToken`, optional) — cancel the request.

---

## ⬆️ Output

Returns a `QuoteHistoryData` object containing:

| Field              | Type                       | Description                    |
| ------------------ | -------------------------- | ------------------------------ |
| `HistoricalQuotes` | `List<HistoricalQuoteBar>` | List of historical quote bars. |

Each **HistoricalQuoteBar** includes:

| Field   | Type     | Description           |
| ------- | -------- | --------------------- |
| `Time`  | `string` | Time of the bar (UTC) |
| `Open`  | `double` | Opening price         |
| `High`  | `double` | Highest price         |
| `Low`   | `double` | Lowest price          |
| `Close` | `double` | Closing price         |

---

## 🎯 Purpose

Use this method to load candlestick-style **historical price data** for a symbol.

Typical use cases:

* Charting historical candles
* Backtesting strategies
* Detecting technical patterns

Supports minute, hourly, daily, and higher timeframes using the `ENUM_QUOTE_HISTORY_TIMEFRAME` enum.
