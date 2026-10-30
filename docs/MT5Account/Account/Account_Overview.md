# Account — Overview

This section groups methods for reading **account state and performance**: a full snapshot, trade statistics, per-order unrealized extrema, and equity history. Use it for dashboards, risk checks and reporting.

---

## 📂 Methods in this folder

* [AccountSummary.md](AccountSummary.md)
  Complete account snapshot (balance, equity, margin, leverage, currency, trade/margin mode, server time).

* [TradeStats.md](TradeStats.md)
  Aggregated statistics — open/closed counts, profit by period, drawdowns, per-market breakdown.

* [TradeUnrealizedPL.md](TradeUnrealizedPL.md)
  Per-open-order unrealized P/L **extrema** (best/worst floating P/L with timestamps).

* [EquityHistory.md](EquityHistory.md)
  Time series of equity/balance points with running drawdowns — the equity curve.

* [TradeStatsEquityHistory.md](TradeStatsEquityHistory.md)
  Combined stats **+** equity chart points **+** the trade list, in one call.

---

## ⚡ Example Workflow (service wrapper — data-returning)

```csharp
// 1) One-glance account state
var s = await _service.AccountSummaryAsync();
Console.WriteLine($"{s.AccountLogin} {s.AccountCurrency} Bal={s.AccountBalance} Eq={s.AccountEquity}");

// 2) Headline performance
var stats = (await _service.TradeStatsAsync()).Summary;
Console.WriteLine($"open={stats?.OpenTrades} total={stats?.TotalProfit}");

// 3) Equity curve (summarize — payload can be large)
var eq = await _service.EquityHistoryAsync();
Console.WriteLine($"{eq.Points.Count} equity point(s)");

// 4) Everything for a report in one round trip
var sc = await _service.TradeStatsEquityHistoryAsync();
Console.WriteLine($"{sc.Charts.Count} points, {sc.Trades.Count} trades");
```

---

## ✅ Best Practices

1. **Snapshot for liveness.** `AccountSummaryAsync` is the cheapest "is the connection alive?" check — login/currency/equity present → good.
2. **Summarize big reads.** `EquityHistory` and `TradeStatsEquityHistory` can be large; print counts/last point, and use `from` / `excludeSameBars` to trim.
3. **Deadlines & cancellation.** Pass `deadline`/`CancellationToken` for snappy UX; the library applies sensible defaults with automatic reconnection.
4. **Currency discipline.** All money fields are in the account deposit currency — don't mix with instrument quote currency without converting.
5. **Snapshot semantics.** `Equity`, `OpenProfit` and unrealized extrema are live — re-query right before risk decisions.

---

## 🎯 Purpose

A compact toolkit for **account state and performance**:

* Snapshot → Statistics → Equity curve → Full report.
* Feed dashboards, risk guards and exports.
* Predictable timeouts, retries and clear error surfaces.

---

👉 Use this overview as a **map** and follow links to each `.md` file for complete method details, parameters, enums (where used) and pitfalls.
