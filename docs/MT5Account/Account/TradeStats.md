# Getting Trade Statistics

> **Request:** aggregated trade statistics (`StatsData`) from MT4
> Open/closed trade counts, profit by period, drawdowns and per-market breakdown — in one call.

**API Information:**

* **SDK wrapper:** `MT4Account.TradeStatsAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.TradeStats`
* **Proto definition:** `TradeStats` (trade-stats proto)

### RPC

* **Service:** `mt4_term_api.TradeStats`
* **Method:** `TradeStats(TradeStatsRequest) → TradeStatsReply`
* **Low‑level client (generated):** `TradeStats.TradeStatsClient.TradeStatsAsync(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async Task<StatsData> TradeStatsAsync(
            string? from = null,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default);
    }
}
```

**Request message:**

`TradeStatsRequest { from?: string }`

**Reply message:**

`TradeStatsReply { data: StatsData }`

---

### Code Example

```csharp
// --- Service wrapper (typed, data-returning) ---
var stats = await _service.TradeStatsAsync();
var sum = stats.Summary;
_logger.LogInformation("open={Open} openProfit={OpenPL} total={Total}",
    sum?.OpenTrades, sum?.OpenProfit, sum?.TotalProfit);

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var s = await _mt4.TradeStatsAsync(
    from: null,          // optional ISO start date, e.g. "2026-01-01"
    deadline: null,
    cancellationToken: cts.Token);

Console.WriteLine($"Open: {s.Summary.OpenTrades}, Total profit: {s.Summary.TotalProfit}, Pips: {s.Pips}, Lots: {s.Lots}");
```

---

## 🔽 Input

| Parameter           | Type                | Description                                                       |
| ------------------- | ------------------- | ----------------------------------------------------------------- |
| `from`              | `string?`           | Optional ISO start date (e.g. `"2026-01-01"`); null = all history |
| `deadline`          | `DateTime?`         | Optional absolute **UTC** deadline for the call                   |
| `cancellationToken` | `CancellationToken` | Cooperative cancel for the call/retry loop                        |

---

## ⬆️ Output — `StatsData`

| Field                        | Type                          | Description                                 |
| ---------------------------- | ----------------------------- | ------------------------------------------- |
| `Summary`                    | `TradeSummaryData`            | Headline counts & profit (see below).       |
| `MaxBalanceDrawdownRaw`      | `double`                      | Largest balance drawdown, absolute.         |
| `MaxBalanceDrawdownRelative` | `double`                      | Largest balance drawdown, relative (%).     |
| `MaxEquityDrawdownRaw`       | `double`                      | Largest equity drawdown, absolute.          |
| `MaxEquityDrawdownRelative`  | `double`                      | Largest equity drawdown, relative (%).      |
| `Markets`                    | `RepeatedField<MarketTradeCountData>` | Per-symbol trade breakdown.         |
| `Profitability`              | `ProfitabilityData`           | Win/loss ratios and related metrics.        |
| `Pips`                       | `double`                      | Net pips.                                   |
| `Lots`                       | `double`                      | Total traded volume (lots).                 |
| `Comissions`                 | `double`                      | Total commissions.                          |

### `TradeSummaryData` (the `Summary` field)

| Field         | Type     | Description                          |
| ------------- | -------- | ------------------------------------ |
| `OpenTrades`  | `int`    | Currently open trades.               |
| `OpenProfit`  | `double` | Floating profit of open trades.      |
| `DayProfit`   | `double` | Realized profit today.               |
| `WeekProfit`  | `double` | Realized profit this week.           |
| `MonthProfit` | `double` | Realized profit this month.          |
| `TotalProfit` | `double` | Realized profit over the period.     |

---

## 🎯 Purpose

A compact performance snapshot for dashboards and risk checks:

* Current exposure (open trades + floating profit).
* Realized profit by day/week/month/total.
* Drawdown and per-market breakdown for reporting.

---

## 🧩 Notes & Tips

* **Summary first.** For a quick line, read `Summary` (open/open-profit/total); the rest is for deeper reports.
* **`from` filter.** Pass an ISO date to scope statistics to a period; omit for full history.
* **Snapshot semantics.** `OpenProfit` is floating — re-query right before risk decisions.

---

## ⚠️ Pitfalls

* **Empty history.** A fresh account returns zeros, not an error — guard division when computing ratios.
* **Currency.** Money fields are in the account deposit currency; don't mix with instrument quote currency.

---

## 🧪 Testing Suggestions

* **Happy path.** On an account with activity, `TotalProfit` and `Lots` are populated; `Summary.OpenTrades` matches `OpenedOrders`.
* **Edge cases.** With `from` set to today, period profits narrow to today's realized P/L.
