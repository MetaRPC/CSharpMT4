# MT4Sugar — API Reference (generated)

> Layer 3 — convenience helpers on top of MT4Service: risk-based sizing, price/volume normalization, batch actions.

> ⚙️ **Generated directly from the source code.** This page lists every public method with its real signature, grouped exactly as in the code — treat it as the ground-truth index.
>
> 📖 **For comfortable reading** — full descriptions, parameters and examples live in the documentation: see the **[MT4Sugar overview](../MT4Sugar/MT4Sugar.Overview.md)** and its per-method pages.

---

## 💰 Account Information

- **`Task<AccountSnapshot> GetAccountSnapshotAsync(this MT4Service svc, DateTime? deadline, CancellationToken ct)`** — Builds an AccountSnapshot from a single AccountSummary round trip.

## 📈 Symbol Information

- **`Task<SymbolSnapshot> GetSymbolSnapshotAsync(this MT4Service svc, string symbol, DateTime? deadline, CancellationToken ct)`** — Builds a SymbolSnapshot (quote + digits/point/spread/volume limits).
- **`Task EnsureSelectedAsync(this MT4Service svc, string symbol, DateTime? deadline, CancellationToken ct)`** — Ensures the symbol is present in Market Watch (SymbolSelect with select=true).
- **`Task<double> NormalizePriceAsync(this MT4Service svc, string symbol, double price, DateTime? deadline, CancellationToken ct)`** — Rounds a price to the symbol's digits.
- **`Task<double> NormalizeVolumeAsync(this MT4Service svc, string symbol, double volume, DateTime? deadline, CancellationToken ct)`** — Clamps and snaps a volume to the symbol's min/max/step.
- **`Task<double> PointsToPipsAsync(this MT4Service svc, string symbol, double points, DateTime? deadline, CancellationToken ct)`** — Converts points to pips (on 3/5-digit symbols 1 pip = 10 points).
- **`Task<double> PriceFromOffsetPointsAsync(this MT4Service svc, string symbol, bool fromAsk, double offsetPoints, DateTime? deadline, CancellationToken ct)`** — A price at offsetPoints from the current Ask (fromAsk=true) or Bid; offset may be negative.

## 📦 Positions And Orders (read)

- **`Task<OrdersHistoryData> OrdersHistoryLastAsync(this MT4Service svc, TimeSpan window, DateTime? deadline, CancellationToken ct)`** — Order history for the last window up to now.
- **`Task<int> GetOrderCountAsync(this MT4Service svc, string? symbol, DateTime? deadline, CancellationToken ct)`** — Number of open orders (optionally for one symbol).
- **`Task<double> GetTotalProfitLossAsync(this MT4Service svc, string? symbol, DateTime? deadline, CancellationToken ct)`** — Total floating profit of open orders (optionally for one symbol).
- **`Task<double> TotalLotsAsync(this MT4Service svc, string? symbol, DateTime? deadline, CancellationToken ct)`** — Total open volume in lots (optionally for one symbol).
- **`Task<List<OpenedOrderInfo>> GetOrdersBySymbolAsync(this MT4Service svc, string symbol, DateTime? deadline, CancellationToken ct)`** — Open orders for a symbol.
- **`Task<OpenedOrderInfo?> GetOrderByTicketAsync(this MT4Service svc, long ticket, DateTime? deadline, CancellationToken ct)`** — Finds an open order by ticket, or null.
- **`Task<List<OpenedOrderInfo>> GetProfitableOrdersAsync(this MT4Service svc, string? symbol, DateTime? deadline, CancellationToken ct)`** — Open orders currently in profit (optionally for one symbol).
- **`Task<List<OpenedOrderInfo>> GetLosingOrdersAsync(this MT4Service svc, string? symbol, DateTime? deadline, CancellationToken ct)`** — Open orders currently at a loss (optionally for one symbol).
- **`Task<HistoryOrderInfo?> GetLastClosedOrderAsync(this MT4Service svc, TimeSpan window, DateTime? deadline, CancellationToken ct)`** — The most recent closed order within window, or null.

## 🛒 Trading

- **`Task<OrderSendData> BuyMarketAsync(this MT4Service svc, string symbol, double volume, double? sl, double? tp, int slippage, int magic, string comment, DateTime? deadline, CancellationToken ct)`** — Sends a market BUY (volume auto-normalized, price = current Ask). SL/TP are absolute prices.
- **`Task<OrderSendData> SellMarketAsync(this MT4Service svc, string symbol, double volume, double? sl, double? tp, int slippage, int magic, string comment, DateTime? deadline, CancellationToken ct)`** — Sends a market SELL (volume auto-normalized, price = current Bid). SL/TP are absolute prices.
- **`Task<OrderSendData> BuyMarketPointsAsync(this MT4Service svc, string symbol, double volume, double? slPoints, double? tpPoints, int slippage, int magic, string comment, DateTime? deadline, CancellationToken ct)`** — Market BUY with SL/TP expressed in points from the current Ask.
- **`Task<OrderSendData> SellMarketPointsAsync(this MT4Service svc, string symbol, double volume, double? slPoints, double? tpPoints, int slippage, int magic, string comment, DateTime? deadline, CancellationToken ct)`** — Market SELL with SL/TP expressed in points from the current Bid.
- **`Task<OrderModifyData> ModifySlTpAsync(this MT4Service svc, int ticket, double? sl, double? tp, DateTime? deadline, CancellationToken ct)`** — Modifies an order's SL/TP (absolute prices; null leaves the field unchanged).
- **`Task<OrderCloseDeleteData> CloseByTicketAsync(this MT4Service svc, int ticket, DateTime? deadline, CancellationToken ct)`** — Closes a market order / deletes a pending order by ticket.
- **`Task<int> CloseAllAsync(this MT4Service svc, string? symbol, DateTime? deadline, CancellationToken ct)`** — Closes/deletes ALL open orders (optionally one symbol). Returns how many were closed.
- **`Task<int> CloseAllMarketAsync(this MT4Service svc, string? symbol, DateTime? deadline, CancellationToken ct)`** — Closes only market (buy/sell) orders. Returns how many were closed.
- **`Task<int> CancelAllPendingAsync(this MT4Service svc, string? symbol, DateTime? deadline, CancellationToken ct)`** — Deletes only pending (limit/stop) orders. Returns how many were deleted.
- **`Task<double> CalcVolumeForRiskAsync(this MT4Service svc, string symbol, double stopPoints, double riskMoney, DateTime? deadline, CancellationToken ct)`** — Volume for a fixed money risk over a stop distance (points), best-effort from TickValueWithSize. valuePerPoint ~= TradeTickValue (per lot); volume = riskMoney / (stopPoints * valuePerPoint), then normalized.
- **`Task<OrderSendData> BuyMarketByRiskAsync(this MT4Service svc, string symbol, double stopPoints, double riskMoney, int slippage, int magic, string comment, DateTime? deadline, CancellationToken ct)`** — Market BUY sized to risk riskMoney over stopPoints; SL placed that far below Ask.
- **`Task<OrderSendData> SellMarketByRiskAsync(this MT4Service svc, string symbol, double stopPoints, double riskMoney, int slippage, int magic, string comment, DateTime? deadline, CancellationToken ct)`** — Market SELL sized to risk riskMoney over stopPoints; SL placed that far above Bid.

## 📡 Subscriptions

- **`IAsyncEnumerable<OnSymbolTickData> ReadTicks(this MT4Service svc, string symbol, CancellationToken ct)`** — Live tick stream for a single symbol.
- **`IAsyncEnumerable<OnTradeData> ReadTrades(this MT4Service svc, CancellationToken ct)`** — Live trade-event stream (order create/modify/close).

---

↩ Back to the **[MT4Sugar overview](../MT4Sugar/MT4Sugar.Overview.md)**.
