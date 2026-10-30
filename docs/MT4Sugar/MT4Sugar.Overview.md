# MT4Sugar — High-Level API (with examples)

> **One page, every helper — with a usage example for each.** `MT4Sugar` is a set of **extension methods on [`MT4Service`](../MT4Service/MT4Service.Overview.md)**: snapshots, normalization, risk sizing, one-line market orders and bulk close. For the terse machine-generated signature list see the **[generated index](../API_Reference/MT4Sugar.md)**.

---

## 🧭 Where it sits

```
MT4Account (low-level)  →  raw gRPC
     ↓
MT4Service (mid-level)  →  typed facade + getters
     ↓
MT4Sugar  (high-level)  →  convenience extension methods on MT4Service   ← YOU ARE HERE
```

Because they are extension methods, you call them **on a connected service instance** (`svc` below is an `MT4Service`):

```csharp
var svc  = new MT4Service(account);                 // account is a connected MT4Account
var snap = await svc.GetAccountSnapshotAsync();
```

> MT4Sugar is intentionally **bounded to MT4**: no Market Depth/DOM, no `OrderCheck`/margin helpers, and it works with **orders** (not netted positions). Signatures below omit the implicit `this MT4Service svc` receiver. Every method takes optional `DateTime? deadline = null, CancellationToken ct = default` at the end (shown as `…`).

---

## DTOs

Compact value objects returned by the snapshot helpers.

### `AccountSnapshot`

| Field | Type | Description |
| ----- | ---- | ----------- |
| `Login` | `long` | Account login. |
| `Balance` | `double` | Balance. |
| `Equity` | `double` | Equity (balance + floating P/L). |
| `Margin` | `double` | Used margin. |
| `FreeMargin` | `double` | Free margin. |
| `MarginLevel` | `double` | Margin level, percent. |
| `Currency` | `string` | Deposit currency. |

### `SymbolSnapshot`

| Field | Type | Description |
| ----- | ---- | ----------- |
| `Symbol` | `string` | Symbol name. |
| `Bid` / `Ask` | `double` | Top of book. |
| `Digits` | `int` | Price digits. |
| `Point` | `double` | Point size (`10^-digits`). |
| `SpreadPoints` | `double` | Spread in points. |
| `VolumeMin` / `VolumeMax` / `VolumeStep` | `double` | Volume limits (lots). |

---

## 1. Account

→ builds on [MT4Service · Account](../MT4Service/Account_Methods.md)

### [`GetAccountSnapshotAsync(…)`](1.%20Snapshots/GetAccountSnapshot.md)
Pulls the whole money side of the account in **one** `AccountSummary` round trip and packs it into an [`AccountSnapshot`](#accountsnapshot). Use it when you want balance/equity/margin together without several getter calls.

```csharp
var acc = await svc.GetAccountSnapshotAsync();
Console.WriteLine($"{acc.Equity} {acc.Currency} (free margin {acc.FreeMargin})");
if (acc.MarginLevel < 200) Console.WriteLine("margin getting tight");
```

---

## 2. Symbol

→ builds on [MT4Service · Symbol](../MT4Service/Symbol_Methods.md)

### [`GetSymbolSnapshotAsync(string symbol, …)`](1.%20Snapshots/GetSymbolSnapshot.md)
Quote plus the contract facts you need to trade the symbol — digits, point, spread, volume limits — in one [`SymbolSnapshot`](#symbolsnapshot). The starting point for most sizing/pricing logic.

```csharp
var s = await svc.GetSymbolSnapshotAsync("EURUSD");
Console.WriteLine($"bid={s.Bid} ask={s.Ask} spread={s.SpreadPoints}pt step={s.VolumeStep}");
```

### [`EnsureSelectedAsync(string symbol, …)`](2.%20Normalization_Utils/EnsureSelected.md)
Makes sure the symbol is in Market Watch (`SymbolSelect` with `select: true`) so quotes/history are available. Call it once before working with a symbol you're not sure is selected.

```csharp
await svc.EnsureSelectedAsync("GBPUSD");   // now safe to quote / stream it
```

### [`NormalizePriceAsync(string symbol, double price, …)`](2.%20Normalization_Utils/NormalizePrice.md)
Rounds a price to the symbol's `Digits`. Brokers reject prices off the tick grid, so normalize any computed SL/TP/entry before sending.

```csharp
double sl = await svc.NormalizePriceAsync("EURUSD", rawSl);   // e.g. 1.094827 → 1.09483
```

### [`NormalizeVolumeAsync(string symbol, double volume, …)`](2.%20Normalization_Utils/NormalizeVolume.md)
Clamps a volume into `[VolumeMin … VolumeMax]` and snaps it onto `VolumeStep`. Prevents "invalid volume" rejections from hand-computed lot sizes.

```csharp
double vol = await svc.NormalizeVolumeAsync("EURUSD", 0.023);  // → 0.02 on a 0.01 step
```

### [`PointsToPipsAsync(string symbol, double points, …)`](2.%20Normalization_Utils/PointsToPips.md)
Converts points to pips (on 3/5-digit symbols 1 pip = 10 points). Handy for display, since humans think in pips but the API works in points.

```csharp
double pips = await svc.PointsToPipsAsync("EURUSD", 200);   // → 20
```

### [`PriceFromOffsetPointsAsync(string symbol, bool fromAsk, double offsetPoints, …)`](2.%20Normalization_Utils/PriceFromOffsetPoints.md)
Returns a **normalized** absolute price a given number of points away from the current Ask (`fromAsk: true`) or Bid. Negative offset goes below. The building block for SL/TP and pending-order levels.

```csharp
// take-profit 300 points above Ask; stop 150 points below Ask
double tp = await svc.PriceFromOffsetPointsAsync("EURUSD", fromAsk: true, offsetPoints:  300);
double sl = await svc.PriceFromOffsetPointsAsync("EURUSD", fromAsk: true, offsetPoints: -150);
```

---

## 3. Positions & Orders (read)

→ builds on [MT4Service · Positions & Orders](../MT4Service/Orders_Methods.md)

### [`OrdersHistoryLastAsync(TimeSpan window, …)`](3.%20Read_Helpers/OrdersHistoryLast.md)
Closed-order history for the last `window` up to now — a shortcut over the dated `OrdersHistory` call.

```csharp
var hist = await svc.OrdersHistoryLastAsync(TimeSpan.FromDays(7));
Console.WriteLine($"{hist.OrdersInfo.Count} orders closed this week");
```

### [`GetOrderCountAsync(string? symbol = null, …)`](3.%20Read_Helpers/GetOrderCount.md)
How many orders are open — overall, or for one symbol when you pass it.

```csharp
int all = await svc.GetOrderCountAsync();
int eur = await svc.GetOrderCountAsync("EURUSD");
```

### [`GetTotalProfitLossAsync(string? symbol = null, …)`](3.%20Read_Helpers/GetTotalProfitLoss.md)
Total **floating** P/L of open orders (optionally one symbol). The usual input to a safety cap.

```csharp
if (await svc.GetTotalProfitLossAsync() < -100) Console.WriteLine("stand down");
```

### [`TotalLotsAsync(string? symbol = null, …)`](3.%20Read_Helpers/TotalLots.md)
Total open volume in lots (optionally one symbol) — a quick exposure gauge.

```csharp
double lots = await svc.TotalLotsAsync("EURUSD");
```

### [`GetOrdersBySymbolAsync(string symbol, …)`](3.%20Read_Helpers/GetOrdersBySymbol.md)
All open orders on one symbol, as a list you can iterate.

```csharp
foreach (var o in await svc.GetOrdersBySymbolAsync("EURUSD"))
    Console.WriteLine($"#{o.Ticket} {o.OrderType} {o.Lots}@{o.OpenPrice} P/L={o.Profit}");
```

### [`GetOrderByTicketAsync(long ticket, …)`](3.%20Read_Helpers/GetOrderByTicket.md)
Looks up one open order by ticket, or `null` if it isn't open.

```csharp
var o = await svc.GetOrderByTicketAsync(12345678);
if (o is null) Console.WriteLine("not open (closed or wrong ticket)");
```

### [`GetProfitableOrdersAsync(string? symbol = null, …)`](3.%20Read_Helpers/GetProfitableOrders.md) / [`GetLosingOrdersAsync(string? symbol = null, …)`](3.%20Read_Helpers/GetLosingOrders.md)
The open book split into winners and losers — e.g. to close only the losing side.

```csharp
var losers  = await svc.GetLosingOrdersAsync("EURUSD");
var winners = await svc.GetProfitableOrdersAsync("EURUSD");
Console.WriteLine($"{winners.Count} up / {losers.Count} down");
```

### [`GetLastClosedOrderAsync(TimeSpan window, …)`](3.%20Read_Helpers/GetLastClosedOrder.md)
The single most recent closed order within `window`, or `null`.

```csharp
var last = await svc.GetLastClosedOrderAsync(TimeSpan.FromDays(1));
Console.WriteLine(last is null ? "nothing closed today" : $"last #{last.Ticket}");
```

---

## 4. Trading

→ builds on [MT4Service · Trading](../MT4Service/Trading_Methods.md)

> ⚠️ All trading helpers place **real orders**. Guard them behind a flag in examples/strategies.

### [`BuyMarketAsync(string symbol, double volume, double? sl = null, double? tp = null, int slippage = 5, int magic = 0, string comment = "MT4Sugar", …)`](4.%20Trading_Market/BuyMarket.md) / [`SellMarketAsync(…)`](4.%20Trading_Market/SellMarket.md)
Market order with **absolute** SL/TP prices; volume is auto-normalized, price is the current Ask (buy) / Bid (sell).

```csharp
var r = await svc.BuyMarketAsync("EURUSD", 0.10, sl: 1.0950, tp: 1.1050);
Console.WriteLine($"opened #{r.Ticket} @ {r.Price}");
```

### [`BuyMarketPointsAsync(string symbol, double volume, double? slPoints = null, double? tpPoints = null, …)`](4.%20Trading_Market/BuyMarketPoints.md) / [`SellMarketPointsAsync(…)`](4.%20Trading_Market/SellMarketPoints.md)
Same, but SL/TP are given in **points** from the fill price — no need to compute absolute levels yourself.

```csharp
// buy 0.10 lot, stop 150 points away, target 300 points away
await svc.BuyMarketPointsAsync("EURUSD", 0.10, slPoints: 150, tpPoints: 300);
```

### [`ModifySlTpAsync(int ticket, double? sl = null, double? tp = null, …)`](4.%20Trading_Market/ModifySlTp.md)
Changes an order's SL/TP (absolute prices). Pass only what you want to change — `null` leaves a field as-is. This is how trailing/break-even logic moves stops.

```csharp
await svc.ModifySlTpAsync(12345678, tp: 1.1075);        // set/raise TP, leave SL
await svc.ModifySlTpAsync(12345678, sl: newTrailStop);  // ratchet the stop
```

### [`CloseByTicketAsync(int ticket, …)`](4.%20Trading_Market/CloseByTicket.md)
Closes a market order, or deletes a pending order, by ticket.

```csharp
await svc.CloseByTicketAsync(12345678);
```

### [`CloseAllAsync(string? symbol = null, …)`](5.%20Bulk_Convenience/CloseAll.md)
Closes/deletes **every** open order (optionally just one symbol); returns how many it acted on. The "flatten everything" button.

```csharp
int n = await svc.CloseAllAsync();              // whole account
int e = await svc.CloseAllAsync("EURUSD");      // just EURUSD
```

### [`CloseAllMarketAsync(string? symbol = null, …)`](5.%20Bulk_Convenience/CloseAllMarket.md) / [`CancelAllPendingAsync(string? symbol = null, …)`](5.%20Bulk_Convenience/CancelAllPending.md)
The two halves of `CloseAll`: close only **market** positions, or delete only **pending** orders. Returns the count.

```csharp
await svc.CloseAllMarketAsync("EURUSD");    // flatten positions, keep pendings
await svc.CancelAllPendingAsync("EURUSD");  // clear pendings, keep positions
```

### [`CalcVolumeForRiskAsync(string symbol, double stopPoints, double riskMoney, …)`](6.%20Market_ByRisk/CalcVolumeForRisk.md)
Sizes a position so a `stopPoints` move against you costs exactly `riskMoney`. Reads tick value internally and returns a broker-valid (normalized) volume. The heart of risk-based entries.

```csharp
// how many lots make a 200-point stop cost $50?
double vol = await svc.CalcVolumeForRiskAsync("EURUSD", stopPoints: 200, riskMoney: 50);
```

### [`BuyMarketByRiskAsync(string symbol, double stopPoints, double riskMoney, int slippage = 5, int magic = 0, string comment = "MT4Sugar", …)`](6.%20Market_ByRisk/BuyMarketByRisk.md) / [`SellMarketByRiskAsync(…)`](6.%20Market_ByRisk/SellMarketByRisk.md)
One call that sizes by risk **and** opens the order with its protective stop already attached (`stopPoints` below Ask for buy / above Bid for sell). The safest way to enter — you're never left without an SL.

```csharp
// risk $50 over a 200-point stop; volume is computed and the SL is placed for you
var r = await svc.BuyMarketByRiskAsync("EURUSD", stopPoints: 200, riskMoney: 50);
Console.WriteLine($"opened #{r.Ticket} vol={r.Volume} @ {r.Price}");
```

---

## 5. Subscriptions (streaming)

→ builds on [MT4Service · Subscriptions](../MT4Service/Streaming_Methods.md)

### [`ReadTicks(string symbol, CancellationToken ct = default)`](7.%20Streams_Helpers/ReadTicks.md)
Live tick stream for one symbol as `IAsyncEnumerable` — consume with `await foreach`, stop via the token. (See [gRPC Streaming](../All_Guides/GRPC_STREAM_MANAGEMENT.md).)

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try { await foreach (var t in svc.ReadTicks("EURUSD", cts.Token))
          Console.WriteLine($"{t.SymbolTick.Bid}/{t.SymbolTick.Ask}"); }
catch (OperationCanceledException) { }
```

### [`ReadTrades(CancellationToken ct = default)`](7.%20Streams_Helpers/ReadTrades.md)
Live trade-event stream (order create/modify/close), same consumption pattern.

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try { await foreach (var ev in svc.ReadTrades(cts.Token)) Console.WriteLine(ev); }
catch (OperationCanceledException) { }
```

---

> **Tip:** a sugar method is just a readable shortcut over `MT4Service`. For the underlying Request/Reply shapes and fields, follow the group link to [MT4Service](../MT4Service/MT4Service.Overview.md), then its `spec` into the [MT4Account](../MT5Account/MT4Account.Master.Overview.md) low-level docs. For the terse signature list, see the [generated index](../API_Reference/MT4Sugar.md).
