# MT4Account — API Reference (generated)

> Layer 1 — the low-level gRPC client. Direct terminal calls, session/reconnect, raw request/reply and streams.

> ⚙️ **Generated directly from the source code.** This page lists every public method with its real signature, grouped exactly as in the code — treat it as the ground-truth index.
>
> 📖 **For comfortable reading** — full descriptions, parameters and examples live in the documentation: see the **[MT4Account overview](../MT5Account/MT4Account.Master.Overview.md)** and its per-method pages.

> ℹ️ Methods marked **†** also have a **synchronous twin** (same name without `Async`, blocks the calling thread). See [Sync vs Async](../All_Guides/SYNC_VS_ASYNC.md).

---

## 🔌 Connection & Session

- **`Task DisconnectAsync(bool delete)`** † — Gracefully disconnects from MT4 terminal and disposes underlying resources.
- **`Task<Guid> GetIdAsync(CancellationToken cancellationToken)`** †
- **`Task ConnectByHostPortAsync(string host, int port, string baseChartSymbol, bool waitForTerminalIsAlive, int timeoutSeconds, DateTime? deadline, CancellationToken cancellationToken)`** † — Connects to the MT4 terminal using credentials provided in the constructor.
- **`Task ConnectByServerNameAsync(string serverName, string baseChartSymbol, bool waitForTerminalIsAlive, int timeoutSeconds, DateTime? deadline, CancellationToken cancellationToken)`** † — Connects to the MT4 terminal using credentials provided in the constructor.

## 💰 Account Information

- **`Task<AccountSummaryData> AccountSummaryAsync(DateTime? deadline, CancellationToken cancellationToken)`** † — Asynchronously retrieves summary information about the currently connected MT4 trading account.
- **`Task<StatsData> TradeStatsAsync(string? from, DateTime? deadline, CancellationToken cancellationToken)`** — Trade statistics. Group: Account_Information. 'from' is an optional MT4/ISO date string.
- **`Task<TradeUnrealizedPLData> TradeUnrealizedPLAsync(string? from, DateTime? deadline, CancellationToken cancellationToken)`** — Unrealized profit/loss of open trades. Group: Account_Information.
- **`Task<EquityHistoryData> EquityHistoryAsync(string? from, bool excludeSameBars, DateTime? deadline, CancellationToken cancellationToken)`** — Equity history over time. Group: Account_Information.
- **`Task<StatsWithChartsData> TradeStatsEquityHistoryAsync(string? from, bool excludeSameBars, DateTime? deadline, CancellationToken cancellationToken)`** — Trade-stats equity history. Group: Account_Information.

## 📈 Symbol Information

- **`Task<SymbolParamsManyData> SymbolParamsManyAsync(string? symbolName, DateTime? deadline, CancellationToken cancellationToken)`** † — Asynchronously retrieves trading symbol parameters for all or a specific symbol.
- **`Task<TickValueWithSizeData> TickValueWithSizeAsync(IEnumerable<string> symbolNames, DateTime? deadline, CancellationToken cancellationToken)`** † — Asynchronously calculates tick value, size, and contract size for specified symbols.
- **`Task<QuoteData> QuoteAsync(string symbol, DateTime? deadline, CancellationToken cancellationToken)`** † — Asynchronously retrieves the latest quote for a single symbol.
- **`Task<QuoteManyData> QuoteManyAsync(IEnumerable<string> symbols, DateTime? deadline, CancellationToken cancellationToken)`** † — Asynchronously retrieves quotes for multiple symbols.
- **`Task<SymbolsData> SymbolsAsync(DateTime? deadline, CancellationToken cancellationToken)`** † — Asynchronously retrieves all available symbol names and their indices.
- **`Task<QuoteHistoryData> QuoteHistoryAsync(string symbol, ENUM_QUOTE_HISTORY_TIMEFRAME timeframe, DateTime from, DateTime to, DateTime? deadline, CancellationToken cancellationToken)`** † — Asynchronously retrieves historical quote data for a symbol within a time range.
- **`Task<SymbolSelectData> SymbolSelectAsync(string symbol, bool select, DateTime? deadline, CancellationToken cancellationToken)`** — Adds a symbol to Market Watch (or removes it) and returns its current quote. Group: Symbol_Information.
- **`Task<ServerTimezoneData> ServerTimezoneAsync(DateTime? deadline, CancellationToken cancellationToken)`** — Server timezone / time info. Group: Symbol_Information (infra).
- **`Task<QuoteHistoryBarsData> BarsAsync(string symbol, QUOTE_HISTORY_TIMEFRAME timeframe, DateTime fromUtc, DateTime toUtc, DateTime? deadline, CancellationToken cancellationToken)`** — OHLC bars for [symbol, timeframe] in [fromUtc..toUtc]. Group: Symbol_Information.

## 📦 Positions And Orders

- **`Task<OpenedOrdersData> OpenedOrdersAsync(EnumOpenedOrderSortType sortType, DateTime? deadline, CancellationToken cancellationToken)`** † — Asynchronously retrieves a list of opened orders for the connected account.
- **`Task<OpenedOrdersTicketsData> OpenedOrdersTicketsAsync(DateTime? deadline, CancellationToken cancellationToken)`** † — Asynchronously retrieves the ticket IDs of opened orders.
- **`Task<OrdersHistoryData> OrdersHistoryAsync(EnumOrderHistorySortType sortType, DateTime? from, DateTime? to, int? page, int? itemsPerPage, DateTime? deadline, CancellationToken cancellationToken)`** † — Asynchronously retrieves paginated historical orders based on filter options.

## 🛒 Trading

- **`Task<OrderSendData> OrderSendAsync(OrderSendRequest request, DateTime? deadline, CancellationToken cancellationToken)`** † — Sends a new market or pending order.
- **`Task<OrderModifyData> OrderModifyAsync(OrderModifyRequest request, DateTime? deadline, CancellationToken cancellationToken)`** † — Modifies an existing order (price, SL, TP, expiration).
- **`Task<OrderCloseDeleteData> OrderCloseDeleteAsync(OrderCloseDeleteRequest request, DateTime? deadline, CancellationToken cancellationToken)`** † — Closes a market order or deletes a pending order.
- **`Task<OrderCloseByData> OrderCloseByAsync(OrderCloseByRequest request, DateTime? deadline, CancellationToken cancellationToken)`** † — Closes a market order by using another opposite market order.

## 📡 Subscriptions

- **`IAsyncEnumerable<OnSymbolTickData> OnSymbolTickAsync(IEnumerable<string> symbols, CancellationToken ct)`** — Subscribes to real-time tick data for specified symbols.
- **`IAsyncEnumerable<OnTradeData> OnTradeAsync(CancellationToken ct)`** — Subscribes to real-time updates for open trade operations (orders, positions, history).
- **`IAsyncEnumerable<OnOpenedOrdersProfitData> OnOpenedOrdersProfitAsync(int intervalMs, CancellationToken ct)`** — Subscribes to real-time profit updates for open orders.
- **`IAsyncEnumerable<OnOpenedOrdersTicketsData> OnOpenedOrdersTicketsAsync(int intervalMs, CancellationToken ct)`** — Subscribes to updates of position and pending order ticket IDs.
- **`IAsyncEnumerable<ConnectStateData> OnConnectStateAsync(CancellationToken ct)`** — Streams terminal connection-state events. Group: Subscriptions.

---

↩ Back to the **[MT4Account overview](../MT5Account/MT4Account.Master.Overview.md)**.
