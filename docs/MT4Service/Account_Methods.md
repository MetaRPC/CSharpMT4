# MT4Service — Account Methods

> Account snapshot, statistics and equity history, plus **value getters** that return a single number/string without re-deriving it from a bulk reply.

→ builds on low-level **[Account — Overview](../MT5Account/Account/Account_Overview.md)**

---

## 📋 Data methods

| Method | Returns | Low-level spec |
| ------ | ------- | -------------- |
| `AccountSummaryAsync(deadline, ct)` | `AccountSummaryData` | [AccountSummary](../MT5Account/Account/AccountSummary.md) |
| `TradeStatsAsync(from, deadline, ct)` | `StatsData` | [TradeStats](../MT5Account/Account/TradeStats.md) |
| `TradeUnrealizedPLAsync(from, deadline, ct)` | `TradeUnrealizedPLData` | [TradeUnrealizedPL](../MT5Account/Account/TradeUnrealizedPL.md) |
| `EquityHistoryAsync(from, excludeSameBars, deadline, ct)` | `EquityHistoryData` | [EquityHistory](../MT5Account/Account/EquityHistory.md) |
| `TradeStatsEquityHistoryAsync(from, excludeSameBars, deadline, ct)` | `StatsWithChartsData` | [TradeStatsEquityHistory](../MT5Account/Account/TradeStatsEquityHistory.md) |

## 📋 Value getters

All derive from a single `AccountSummary` round-trip — a readable shortcut, no low-level one-call equivalent.

| Method | Returns | Value |
| ------ | ------- | ----- |
| `GetBalanceAsync(…)` | `double` | Balance |
| `GetEquityAsync(…)` | `double` | Equity (balance + floating P/L) |
| `GetMarginAsync(…)` | `double` | Used margin |
| `GetFreeMarginAsync(…)` | `double` | Free margin |
| `GetMarginLevelAsync(…)` | `double` | Margin level, percent |
| `GetCreditAsync(…)` | `double` | Credit |
| `GetCurrencyAsync(…)` | `string` | Deposit currency |
| `GetAccountNameAsync(…)` | `string` | Account holder name |
| `GetCompanyAsync(…)` | `string` | Broker / company name |

---

## 💡 Usage examples

### 1) One-line status

```csharp
double balance  = await service.GetBalanceAsync();
double equity   = await service.GetEquityAsync();
string currency = await service.GetCurrencyAsync();
Console.WriteLine($"Balance: {balance:F2} {currency}, Equity: {equity:F2}");
```

### 2) Full snapshot when you need many fields

```csharp
// Prefer one AccountSummary over many getters if you need several values
var s = await service.AccountSummaryAsync();
Console.WriteLine($"#{s.AccountLogin} {s.AccountCurrency} Bal={s.AccountBalance} Eq={s.AccountEquity} " +
                  $"Margin={s.AccountMargin} Free={s.AccountMarginFree} Lev=1:{s.AccountLeverage}");
```

### 3) Performance read + equity curve

```csharp
var stats = (await service.TradeStatsAsync()).Summary;
Console.WriteLine($"open={stats?.OpenTrades} openProfit={stats?.OpenProfit} total={stats?.TotalProfit}");

var eq = await service.EquityHistoryAsync();            // can be large — summarize
Console.WriteLine($"{eq.Points.Count} equity point(s)");
```

---

## 🧩 Notes & Tips

* **Getter vs snapshot.** For one value, a getter is clearest; for several, call `AccountSummaryAsync` once.
* **Big reads.** `EquityHistory` / `TradeStatsEquityHistory` can be large — use `from` / `excludeSameBars` and print counts, not the whole object.
* **Snapshot semantics.** Equity / `OpenProfit` are live — re-query right before risk decisions.
* **Currency discipline.** Money fields are in the account deposit currency.
