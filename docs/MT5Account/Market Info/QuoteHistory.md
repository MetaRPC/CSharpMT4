# Getting Historical Quote Data

> **Request:** retrieve historical OHLC (candlestick) data for a given symbol
> Returns a list of time-based bars with open, high, low, close prices over a defined time range and timeframe.

**API Information:**

* **SDK wrapper:** `MT4Account.QuoteHistoryAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.MarketInfo`
* **Proto definition:** `QuoteHistory` (in the MarketInfo service)

### RPC

* **Service:** `mt4_term_api.MarketInfo`
* **Method:** `QuoteHistory(QuoteHistoryRequest) → QuoteHistoryReply`
* **Low‑level client (generated):** `MarketInfo.MarketInfoClient.QuoteHistory(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async Task<QuoteHistoryData> QuoteHistoryAsync(
            string symbol,
            ENUM_QUOTE_HISTORY_TIMEFRAME timeframe,
            DateTime from,
            DateTime to,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default);
    }
}
```

**Request message:**

`QuoteHistoryRequest { symbol: string, timeframe: ENUM_QUOTE_HISTORY_TIMEFRAME, from, to }`

**Reply message:**

`QuoteHistoryReply { data: QuoteHistoryData }`

---

### Code Example

```csharp
// --- Service wrapper (typed, data-returning) ---
var hist = await _service.QuoteHistoryAsync(
    "EURUSD", ENUM_QUOTE_HISTORY_TIMEFRAME.QhPeriodH1,
    DateTime.UtcNow.AddHours(-6), DateTime.UtcNow);
Console.WriteLine($"{hist.HistoricalQuotes.Count} bar(s)");

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

var from = DateTime.UtcNow.AddDays(-5);
var to = DateTime.UtcNow;
var timeframe = ENUM_QUOTE_HISTORY_TIMEFRAME.QhPeriodH1;

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // widen if terminal is slow
var history = await _mt4.QuoteHistoryAsync(
    symbol: "EURUSD",
    timeframe: timeframe,
    from: from,
    to: to,
    deadline: null,
    cancellationToken: cts.Token);

foreach (var candle in history.HistoricalQuotes)
{
    Console.WriteLine($"[{candle.Time}] O: {candle.Open} H: {candle.High} L: {candle.Low} C: {candle.Close}");
}
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
* **`deadline`** (`DateTime?`, optional) — optional UTC deadline for timeout.
* **`cancellationToken`** (`CancellationToken`, optional) — token to cancel the request.

---

## ⬆️ Output

Returns a `QuoteHistoryData` object containing:

| Field              | Type                        | Description                      |
| ------------------ | --------------------------- | -------------------------------- |
| `HistoricalQuotes` | `RepeatedField<HistoryQuote>` | Sequence of historical OHLC bars |

Each **HistoryQuote** includes:

| Field   | Type        | Description           |
| ------- | ----------- | --------------------- |
| `Time`  | `Timestamp` | Time of the bar (UTC) |
| `Open`  | `double` | Opening price         |
| `High`  | `double` | Highest price         |
| `Low`   | `double` | Lowest price          |
| `Close` | `double` | Closing price         |

> *Note:* In some proto versions `Time` may be a `Timestamp`; convert to local `DateTime` via `.ToDateTime()` for display.

---

## 🎯 Purpose

Load candlestick-style historical price data for charting, backtesting, and technical analysis.

Typical uses:

* Charting historical candles
* Backtesting strategies
* Detecting technical patterns

---

## 🧩 Notes & Tips

* **UTC range.** Both `from` and `to` are interpreted as UTC on the wire. Ensure you convert local times appropriately.
* **Validation.** The client validates `from <= to`; invalid ranges throw immediately.
* **Broker calendars.** Expect gaps (weekends/holidays) and variable session times; this is normal for FX/CFD data.
* **Bar alignment.** Choose `from` aligned to timeframe boundaries for cleaner charts (e.g., top of the hour for `H1`).

---

## ⚠️ Pitfalls

* **Too-wide windows.** Some servers limit the number of bars per request; split very long ranges into chunks.
* **Symbol suffixes.** Request history for the exact symbol name returned by `SymbolsAsync()` (e.g., `EURUSD.r`).

---

## 🧪 Testing Suggestions

* **Happy path.** Non-empty bar list for liquid majors; OHLC values are consistent (`Low <= Open/Close <= High`).
* **Edge cases.** Empty result for exotic symbols or out-of-hours windows; verify graceful handling.
