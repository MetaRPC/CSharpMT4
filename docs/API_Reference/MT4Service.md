# MT4Service — API Reference (generated)

> Layer 2 — the typed facade. Clean signatures that return ready data structures; what most code uses.

> ⚙️ **Generated directly from the source code.** This page lists every public method with its real signature, grouped exactly as in the code — treat it as the ground-truth index.
>
> 📖 **For comfortable reading** — full descriptions, parameters and examples live in the documentation: see the **[MT4Service overview](../MT4Service/MT4Service.Overview.md)** and its per-method pages.

---

## 🔌 Connection

- **`Task ConnectByServerNameAsync(string serverName, string baseChartSymbol, bool waitForTerminalIsAlive, int timeoutSeconds, DateTime? deadline, CancellationToken ct)`** — Connects by broker server name.
- **`Task ConnectByHostPortAsync(string host, int port, string baseChartSymbol, bool waitForTerminalIsAlive, int timeoutSeconds, DateTime? deadline, CancellationToken ct)`** — Connects by host:port.
- **`Task DisconnectAsync(bool delete)`** — Disconnects the session (optionally deletes the terminal instance).
- **`Task<Guid> GetIdAsync(CancellationToken ct)`** — Returns the session id (GUID).

## 💰 Account Information

- **`Task<AccountSummaryData> AccountSummaryAsync(DateTime? deadline, CancellationToken ct)`** — Full account snapshot (balance, equity, margin, leverage, currency, etc.) in one round trip.
- **`Task<double> GetBalanceAsync(DateTime? deadline, CancellationToken ct)`** — Account balance (deposit currency).
- **`Task<double> GetEquityAsync(DateTime? deadline, CancellationToken ct)`** — Account equity (balance + floating P/L).
- **`Task<double> GetMarginAsync(DateTime? deadline, CancellationToken ct)`** — Margin currently used.
- **`Task<double> GetFreeMarginAsync(DateTime? deadline, CancellationToken ct)`** — Free margin available for new trades.
- **`Task<double> GetMarginLevelAsync(DateTime? deadline, CancellationToken ct)`** — Margin level, percent.
- **`Task<double> GetCreditAsync(DateTime? deadline, CancellationToken ct)`** — Account credit.
- **`Task<string> GetCurrencyAsync(DateTime? deadline, CancellationToken ct)`** — Deposit currency (e.g. "USD").
- **`Task<string> GetAccountNameAsync(DateTime? deadline, CancellationToken ct)`** — Account holder name.
- **`Task<string> GetCompanyAsync(DateTime? deadline, CancellationToken ct)`** — Broker company name.
- **`Task<StatsData> TradeStatsAsync(string? from, DateTime? deadline, CancellationToken ct)`** — Trade statistics (counts, profit, drawdown, per-market breakdown, ratios).
- **`Task<TradeUnrealizedPLData> TradeUnrealizedPLAsync(string? from, DateTime? deadline, CancellationToken ct)`** — Per-order unrealized P/L extrema.
- **`Task<EquityHistoryData> EquityHistoryAsync(string? from, bool excludeSameBars, DateTime? deadline, CancellationToken ct)`** — Equity history over time.
- **`Task<StatsWithChartsData> TradeStatsEquityHistoryAsync(string? from, bool excludeSameBars, DateTime? deadline, CancellationToken ct)`** — Trade-stats equity history (stats + charts).

## 📈 Symbol Information

- **`Task<SymbolsData> SymbolsAsync(DateTime? deadline, CancellationToken ct)`** — All available instrument names.
- **`Task<SymbolParamsManyData> SymbolParamsManyAsync(string? symbol, DateTime? deadline, CancellationToken ct)`** — Symbol parameters (digits, lot limits, etc.); pass a symbol to filter, null for all.
- **`Task<QuoteData> QuoteAsync(string symbol, DateTime? deadline, CancellationToken ct)`** — Top-of-book quote for a symbol.
- **`Task<QuoteManyData> QuoteManyAsync(string[] symbols, DateTime? deadline, CancellationToken ct)`** — Batch quotes for several symbols.
- **`Task<QuoteHistoryData> QuoteHistoryAsync(string symbol, ENUM_QUOTE_HISTORY_TIMEFRAME timeframe, DateTime from, DateTime to, DateTime? deadline, CancellationToken ct)`** — Historical quotes (the classic QuoteHistory RPC).
- **`Task<QuoteHistoryBarsData> BarsAsync(string symbol, QUOTE_HISTORY_TIMEFRAME timeframe, DateTime fromUtc, DateTime toUtc, DateTime? deadline, CancellationToken ct)`** — OHLC bars (the Bars RPC); from/to are UTC.
- **`Task<TickValueWithSizeData> TickValueWithSizeAsync(string[] symbols, DateTime? deadline, CancellationToken ct)`** — Tick value and contract size per symbol.
- **`Task<SymbolSelectData> SymbolSelectAsync(string symbol, bool select, DateTime? deadline, CancellationToken ct)`** — Adds/removes a symbol in Market Watch; returns its current quote.
- **`Task<ServerTimezoneData> ServerTimezoneAsync(DateTime? deadline, CancellationToken ct)`** — Server timezone / UTC offset info.
- **`Task<int> GetDigitsAsync(string symbol, DateTime? deadline, CancellationToken ct)`** — Price digits for a symbol.
- **`Task<double> GetPointAsync(string symbol, DateTime? deadline, CancellationToken ct)`** — Point size (10^-digits) for a symbol.
- **`Task<double> GetSpreadAsync(string symbol, DateTime? deadline, CancellationToken ct)`** — Current spread in points (computed from the quote).
- **`Task<double> GetVolumeMinAsync(string symbol, DateTime? deadline, CancellationToken ct)`** — Minimum tradable volume (lots).
- **`Task<double> GetVolumeMaxAsync(string symbol, DateTime? deadline, CancellationToken ct)`** — Maximum tradable volume (lots).
- **`Task<double> GetVolumeStepAsync(string symbol, DateTime? deadline, CancellationToken ct)`** — Volume step (lots).
- **`Task<double> GetBidAsync(string symbol, DateTime? deadline, CancellationToken ct)`** — Current bid price.
- **`Task<double> GetAskAsync(string symbol, DateTime? deadline, CancellationToken ct)`** — Current ask price.

## 📦 Positions And Orders

- **`Task<OpenedOrdersData> OpenedOrdersAsync(EnumOpenedOrderSortType sortType, DateTime? deadline, CancellationToken ct)`** — Open orders and pending orders (full details).
- **`Task<OpenedOrdersTicketsData> OpenedOrdersTicketsAsync(DateTime? deadline, CancellationToken ct)`** — Lightweight list of open-order tickets.
- **`Task<OrdersHistoryData> OrdersHistoryAsync(DateTime from, DateTime to, EnumOrderHistorySortType sortType, int? page, int? itemsPerPage, DateTime? deadline, CancellationToken ct)`** — Closed/cancelled order history within [from..to].

## 🛒 Trading

- **`Task<OrderSendData> OrderSendAsync(OrderSendRequest request, DateTime? deadline, CancellationToken ct)`** — Sends a market or pending order.
- **`Task<OrderModifyData> OrderModifyAsync(OrderModifyRequest request, DateTime? deadline, CancellationToken ct)`** — Modifies an order (SL/TP/price).
- **`Task<OrderCloseDeleteData> OrderCloseDeleteAsync(OrderCloseDeleteRequest request, DateTime? deadline, CancellationToken ct)`** — Closes a market order / deletes a pending order.
- **`Task<OrderCloseByData> OrderCloseByAsync(OrderCloseByRequest request, DateTime? deadline, CancellationToken ct)`** — Closes a position by an opposite one (hedge close-by).

## 📡 Subscriptions

- **`IAsyncEnumerable<OnSymbolTickData> OnSymbolTickAsync(IEnumerable<string> symbols, CancellationToken ct)`** — Live tick stream for the given symbols.
- **`IAsyncEnumerable<OnTradeData> OnTradeAsync(CancellationToken ct)`** — Live trade-event stream.
- **`IAsyncEnumerable<OnOpenedOrdersProfitData> OnOpenedOrdersProfitAsync(int intervalMs, CancellationToken ct)`** — Polling stream of open-order profit snapshots.
- **`IAsyncEnumerable<OnOpenedOrdersTicketsData> OnOpenedOrdersTicketsAsync(int intervalMs, CancellationToken ct)`** — Polling stream of open-order ticket snapshots.
- **`IAsyncEnumerable<ConnectStateData> OnConnectStateAsync(CancellationToken ct)`** — Stream of terminal connection-state events.

---

↩ Back to the **[MT4Service overview](../MT4Service/MT4Service.Overview.md)**.
