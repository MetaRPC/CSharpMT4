# Getting Equity History

> **Request:** time series of equity points (`EquityHistoryData`) from MT4
> Balance/equity samples over time with running drawdowns and realized/unrealized P/L — the raw material for an equity curve.

**API Information:**

* **SDK wrapper:** `MT4Account.EquityHistoryAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.TradeStats`
* **Proto definition:** `EquityHistory` (trade-stats proto)

### RPC

* **Service:** `mt4_term_api.TradeStats`
* **Method:** `EquityHistory(EquityHistoryRequest) → EquityHistoryReply`
* **Low‑level client (generated):** `TradeStats.TradeStatsClient.EquityHistoryAsync(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async Task<EquityHistoryData> EquityHistoryAsync(
            string? from = null,
            bool excludeSameBars = false,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default);
    }
}
```

**Request message:**

`EquityHistoryRequest { from?: string, excludeSameBars: bool }`

**Reply message:**

`EquityHistoryReply { data: EquityHistoryData }`

---

### Code Example

```csharp
// --- Service wrapper (typed, data-returning) ---
var eq = await _service.EquityHistoryAsync();
Console.WriteLine($"{eq.Points.Count} equity point(s)");

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var data = await _mt4.EquityHistoryAsync(
    from: null,               // optional ISO start date
    excludeSameBars: false,   // true = collapse consecutive identical points
    deadline: null,
    cancellationToken: cts.Token);

var last = data.Points.LastOrDefault();
Console.WriteLine($"Latest: balance={last?.Balance} equity={last?.Equity} @ {last?.TimeUtc}");
```

---

## 🔽 Input

| Parameter           | Type                | Description                                                      |
| ------------------- | ------------------- | ---------------------------------------------------------------- |
| `from`              | `string?`           | Optional ISO start date; null = full history                     |
| `excludeSameBars`   | `bool`              | Collapse consecutive identical points to shrink the payload      |
| `deadline`          | `DateTime?`         | Optional absolute **UTC** deadline for the call                  |
| `cancellationToken` | `CancellationToken` | Cooperative cancel for the call/retry loop                       |

---

## ⬆️ Output — `EquityHistoryData`

| Field    | Type                             | Description                  |
| -------- | -------------------------------- | ---------------------------- |
| `Points` | `RepeatedField<EquityPointData>` | Ordered equity samples.      |

### `EquityPointData` (each point)

| Field                     | Type        | Description                              |
| ------------------------- | ----------- | ---------------------------------------- |
| `Time`                    | `Timestamp` | Sample time (server local).              |
| `TimeUtc`                 | `Timestamp` | Sample time (UTC).                       |
| `Balance`                 | `double`    | Balance at this point.                   |
| `Equity`                  | `double`    | Equity at this point.                    |
| `BalanceDrawdownRaw`      | `double`    | Balance drawdown, absolute.              |
| `BalanceDrawdownRelative` | `double`    | Balance drawdown, relative (%).          |
| `EquityDrawdownRaw`       | `double`    | Equity drawdown, absolute.               |
| `EquityDrawdownRelative`  | `double`    | Equity drawdown, relative (%).           |
| `RealizedPl`              | `double`    | Realized P/L up to this point.           |
| `UnrealizedPl`            | `double`    | Floating P/L at this point.              |
| `CashFlow`                | `double`    | Deposits/withdrawals effect.             |

---

## 🎯 Purpose

Draw and analyze the account equity curve:

* Equity/balance charts for dashboards and reports.
* Drawdown analysis straight from the series.
* Separating realized vs floating contribution over time.

---

## 🧩 Notes & Tips

* **Payload size.** Long histories can be large; set `excludeSameBars: true` or a `from` date to trim.
* **UTC.** Use `TimeUtc` for storage/joins; `Time` is for local display.
* **Ordering.** Points are time-ordered; `Points.Last()` is the most recent sample.

---

## ⚠️ Pitfalls

* **Big responses.** Printing the whole object floods the console — summarize (count + last point) as above.
* **Sparse data.** New accounts yield few points; chart code should handle short series.

---

## 🧪 Testing Suggestions

* **Happy path.** `Points` is non-empty and time-ordered; `Equity` ≈ `Balance` on a flat account.
* **Edge cases.** `excludeSameBars: true` reduces the count without changing the shape of the curve.
