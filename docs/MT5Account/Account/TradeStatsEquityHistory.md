# Getting Stats + Equity Chart + Trades

> **Request:** combined statistics, equity chart points and the trade list (`StatsWithChartsData`) from MT4
> One call that returns the `StatsData`, the equity curve points, and the closed/open trades behind the numbers.

**API Information:**

* **SDK wrapper:** `MT4Account.TradeStatsEquityHistoryAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.TradeStats`
* **Proto definition:** `TradeStatsEquityHistory` (trade-stats proto)

### RPC

* **Service:** `mt4_term_api.TradeStats`
* **Method:** `TradeStatsEquityHistory(TradeStatsEquityHistoryRequest) → TradeStatsEquityHistoryReply`
* **Low‑level client (generated):** `TradeStats.TradeStatsClient.TradeStatsEquityHistoryAsync(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async Task<StatsWithChartsData> TradeStatsEquityHistoryAsync(
            string? from = null,
            bool excludeSameBars = false,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default);
    }
}
```

**Request message:**

`TradeStatsEquityHistoryRequest { from?: string, excludeSameBars: bool }`

**Reply message:**

`TradeStatsEquityHistoryReply { data: StatsWithChartsData }`

---

### Code Example

```csharp
// --- Service wrapper (typed, data-returning) ---
var sc = await _service.TradeStatsEquityHistoryAsync();
Console.WriteLine($"{sc.Charts.Count} chart point(s), {sc.Trades.Count} trade(s), {sc.OpenTrades.Count} open");

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var data = await _mt4.TradeStatsEquityHistoryAsync(
    from: null,
    excludeSameBars: false,
    deadline: null,
    cancellationToken: cts.Token);

Console.WriteLine($"Total profit: {data.Stats.Summary.TotalProfit}, points: {data.Charts.Count}");
foreach (var t in data.Trades)
    Console.WriteLine($"#{t.Ticket} {t.Symbol} {(t.IsBuy ? "BUY" : "SELL")} {t.Lots} P/L={t.Profit}");
```

---

## 🔽 Input

| Parameter           | Type                | Description                                                 |
| ------------------- | ------------------- | ----------------------------------------------------------- |
| `from`              | `string?`           | Optional ISO start date; null = full history                |
| `excludeSameBars`   | `bool`              | Collapse consecutive identical equity points                |
| `deadline`          | `DateTime?`         | Optional absolute **UTC** deadline for the call             |
| `cancellationToken` | `CancellationToken` | Cooperative cancel for the call/retry loop                  |

---

## ⬆️ Output — `StatsWithChartsData`

| Field        | Type                                 | Description                                        |
| ------------ | ------------------------------------ | -------------------------------------------------- |
| `Stats`      | `StatsData`                          | Same statistics as [TradeStats](./TradeStats.md). |
| `Charts`     | `RepeatedField<EquityPointData>`     | Equity curve points (see [EquityHistory](./EquityHistory.md)). |
| `Trades`     | `RepeatedField<TradeHistoryItemData>`| Closed trades behind the stats.                    |
| `OpenTrades` | `RepeatedField<TradeHistoryItemData>`| Currently open trades.                             |

### `TradeHistoryItemData` (each trade)

| Field         | Type        | Description                       |
| ------------- | ----------- | --------------------------------- |
| `Ticket`      | `long`      | Trade ticket.                     |
| `Symbol`      | `string`    | Symbol.                           |
| `IsBuy`       | `bool`      | Direction (true = buy).           |
| `Lots`        | `double`    | Volume in lots.                   |
| `OpenPrice`   | `double`    | Open price.                       |
| `ClosePrice`  | `double`    | Close price (0 while open).       |
| `OpenTime`    | `Timestamp` | Open time (local).                |
| `CloseTime`   | `Timestamp` | Close time (local; unset if open).|
| `Profit`      | `double`    | Realized profit.                  |
| `Commission`  | `double`    | Commission.                       |

---

## 🎯 Purpose

Everything a performance report needs in a single round trip:

* Equity curve **and** the trades that produced it — no second call.
* Combine with `Stats.Summary` for headline numbers plus detail.
* Feed analytics / export without stitching separate responses.

---

## 🧩 Notes & Tips

* **Heavy payload.** This is the largest account read (stats + chart + trades). Summarize counts before printing, and use `from` / `excludeSameBars` to trim.
* **Reuses other shapes.** `Charts` are `EquityPointData` and `Stats` is the same `StatsData` as `TradeStatsAsync` — document once, reference here.

---

## ⚠️ Pitfalls

* **Console flooding.** Never dump the whole object; iterate `Trades` or print counts.
* **Open vs closed.** `Trades` are closed; currently open ones are in `OpenTrades` (ClosePrice/CloseTime unset).

---

## 🧪 Testing Suggestions

* **Happy path.** `Charts` non-empty; `Stats.Summary.TotalProfit` matches `TradeStatsAsync`; `OpenTrades.Count` matches `OpenedOrders`.
* **Edge cases.** `excludeSameBars: true` shrinks `Charts` without dropping trades.
