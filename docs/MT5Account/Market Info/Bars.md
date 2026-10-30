# Getting OHLC Bars

> **Request:** historical OHLC bars (`QuoteHistoryBarsData`) for a symbol/timeframe over a UTC range.
> Similar to QuoteHistory, but returns the broker's **bar** series (with tick/real volume and spread).

**API Information:**

* **SDK wrapper:** `MT4Account.BarsAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.QuoteHistory`
* **Proto definition:** `Bars` (in the QuoteHistory service)

### RPC

* **Service:** `mt4_term_api.QuoteHistory`
* **Method:** `Bars(QuoteHistoryBarsRequest) → QuoteHistoryBarsReply`
* **Low‑level client (generated):** `QuoteHistory.QuoteHistoryClient.Bars(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async Task<QuoteHistoryBarsData> BarsAsync(
            string symbol,
            QUOTE_HISTORY_TIMEFRAME timeframe,
            DateTime fromUtc,
            DateTime toUtc,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default);
    }
}
```

**Request message:**

`QuoteHistoryBarsRequest { symbol, timeframe: QUOTE_HISTORY_TIMEFRAME, fromTimeUtc, toTimeUtc }`

**Reply message:**

`QuoteHistoryBarsReply { data: QuoteHistoryBarsData }`

---

### Code Example

```csharp
// --- Service wrapper (typed, data-returning) ---
var bars = await _service.BarsAsync(
    "EURUSD", QUOTE_HISTORY_TIMEFRAME.QhbPeriodH1,
    DateTime.UtcNow.AddHours(-6), DateTime.UtcNow);
var b0 = bars.Bars.FirstOrDefault();
Console.WriteLine($"{bars.Bars.Count} bars; first O={b0?.Open} H={b0?.High} L={b0?.Low} C={b0?.Close} V={b0?.TickVolume}");

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var data = await _mt4.BarsAsync(
    symbol: "EURUSD",
    timeframe: QUOTE_HISTORY_TIMEFRAME.QhbPeriodH1,
    fromUtc: DateTime.UtcNow.AddHours(-6),
    toUtc: DateTime.UtcNow,
    deadline: null,
    cancellationToken: cts.Token);

foreach (var b in data.Bars)
    Console.WriteLine($"[{b.Time}] O:{b.Open} H:{b.High} L:{b.Low} C:{b.Close} V:{b.TickVolume}");
```

---

## 🔽 Input

| Parameter           | Type                       | Description                                        |
| ------------------- | -------------------------- | -------------------------------------------------- |
| `symbol`            | `string`                   | Trading symbol (e.g. `"EURUSD"`).                  |
| `timeframe`         | `QUOTE_HISTORY_TIMEFRAME`  | Bar period — see enum below.                       |
| `fromUtc`           | `DateTime`                 | Range start (**UTC**).                             |
| `toUtc`             | `DateTime`                 | Range end (**UTC**).                               |
| `deadline`          | `DateTime?`                | Optional absolute **UTC** deadline for the call.   |
| `cancellationToken` | `CancellationToken`        | Cooperative cancel for the call/retry loop.        |

---

## ⬆️ Output — `QuoteHistoryBarsData`

| Field            | Type                              | Description                       |
| ---------------- | --------------------------------- | --------------------------------- |
| `Symbol`         | `string`                          | Echoed symbol.                    |
| `Timeframe`      | `QUOTE_HISTORY_TIMEFRAME`         | Echoed timeframe.                 |
| `Bars`           | `RepeatedField<QuoteHistoryBar>`  | The bar series.                   |
| `NextPageToken`  | `string`                          | Paging token for large ranges.    |

### `QuoteHistoryBar` (each bar)

| Field        | Type     | Description                     |
| ------------ | -------- | ------------------------------- |
| `Time`       | `long`   | Bar open time (server, epoch s).|
| `TimeUtc`    | `long`   | Bar open time (UTC, epoch s).   |
| `Open`       | `double` | Open price.                     |
| `High`       | `double` | High price.                     |
| `Low`        | `double` | Low price.                      |
| `Close`      | `double` | Close price.                    |
| `TickVolume` | `long`   | Tick volume.                    |
| `Spread`     | `int`    | Spread (points) at bar.         |
| `RealVolume` | `long`   | Real volume (if provided).      |

---

## 🧱 Related enums (from proto)

### `QUOTE_HISTORY_TIMEFRAME` — parameter `timeframe`

Values are the bar length **in minutes**:

* `QhbPeriodM1 = 1` — 1-minute bars
* `QhbPeriodM5 = 5` — 5-minute bars
* `QhbPeriodM15 = 15` — 15-minute bars
* `QhbPeriodM30 = 30` — 30-minute bars
* `QhbPeriodH1 = 60` — 1-hour bars
* `QhbPeriodH4 = 240` — 4-hour bars
* `QhbPeriodD1 = 1440` — daily bars
* `QhbPeriodW1 = 10080` — weekly bars
* `QhbPeriodMn1 = 43200` — monthly bars
* `QhbPeriodUnspecified = 0` — unspecified (do not use)

> Note: `BarsAsync` uses `QUOTE_HISTORY_TIMEFRAME` (the `Qhb*` members). `QuoteHistoryAsync` uses the different `ENUM_QUOTE_HISTORY_TIMEFRAME` (`Qh*` members) — don't mix them.

---

## 🎯 Purpose

Load candlestick data for charting, indicators and backtests — with volume/spread per bar.

---

## 🧩 Notes & Tips

* **UTC in, UTC out.** Pass UTC `from`/`to`; `TimeUtc` is the canonical timestamp for storage.
* **Paging.** Wide ranges may return `NextPageToken`; page through instead of one huge call.
* **Enum family.** `Bars` = `QUOTE_HISTORY_TIMEFRAME`; `QuoteHistory` = `ENUM_QUOTE_HISTORY_TIMEFRAME`.

---

## ⚠️ Pitfalls

* **Wrong enum.** Passing a `Qh*` value where a `Qhb*` is expected won't compile — keep the two timeframe enums straight.
* **Gaps.** Expect weekend/holiday gaps and variable session lengths.

---

## 🧪 Testing Suggestions

* **Happy path.** Majors return a non-empty, time-ordered series; `Low <= Open/Close <= High`.
* **Edge cases.** Narrow out-of-hours windows may return few or no bars — handle gracefully.
