using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mt4_term_api;

namespace MetaRPC.CSharpMT4
{
    /// <summary>
    /// Typed, data-returning wrapper over <see cref="MT4Account"/> (mirrors the MT5Service design).
    /// No console output here - examples do the printing. MT4 has no granular AccountInfo*/SymbolInfo*
    /// RPCs, so the convenience getters derive from the bulk snapshots (AccountSummary / SymbolParamsMany / Quote).
    /// Methods are grouped into #regions by the canonical API areas for easy tracking and documentation.
    /// </summary>
    public class MT4Service
    {
        private readonly MT4Account _acc;

        /// <summary>Creates a service wrapping an already-constructed <see cref="MT4Account"/>.</summary>
        /// <param name="account">The low-level account to wrap.</param>
        public MT4Service(MT4Account account) => _acc = account;

        /// <summary>The underlying low-level account (for advanced use).</summary>
        public MT4Account Account => _acc;

        #region Connection
        /// <summary>Connects by broker server name.</summary>
        /// <param name="serverName">Broker server name (e.g. "MetaQuotes-Demo").</param>
        /// <param name="baseChartSymbol">Base chart symbol to open.</param>
        /// <param name="waitForTerminalIsAlive">Wait until the terminal reports alive.</param>
        /// <param name="timeoutSeconds">Connect timeout, seconds.</param>
        /// <param name="deadline">Optional RPC deadline.</param>
        /// <param name="ct">Cancellation token.</param>
        public Task ConnectByServerNameAsync(string serverName, string baseChartSymbol = "EURUSD", bool waitForTerminalIsAlive = true, int timeoutSeconds = 30, DateTime? deadline = null, CancellationToken ct = default)
            => _acc.ConnectByServerNameAsync(serverName, baseChartSymbol, waitForTerminalIsAlive, timeoutSeconds, deadline, ct);

        /// <summary>Connects by host:port.</summary>
        /// <param name="host">Gateway host.</param>
        /// <param name="port">Gateway port (default 443).</param>
        /// <param name="baseChartSymbol">Base chart symbol to open.</param>
        /// <param name="waitForTerminalIsAlive">Wait until the terminal reports alive.</param>
        /// <param name="timeoutSeconds">Connect timeout, seconds.</param>
        /// <param name="deadline">Optional RPC deadline.</param>
        /// <param name="ct">Cancellation token.</param>
        public Task ConnectByHostPortAsync(string host, int port = 443, string baseChartSymbol = "EURUSD", bool waitForTerminalIsAlive = true, int timeoutSeconds = 30, DateTime? deadline = null, CancellationToken ct = default)
            => _acc.ConnectByHostPortAsync(host, port, baseChartSymbol, waitForTerminalIsAlive, timeoutSeconds, deadline, ct);

        /// <summary>Disconnects the session (optionally deletes the terminal instance).</summary>
        /// <param name="delete">When true, deletes the server-side terminal instance.</param>
        public Task DisconnectAsync(bool delete = false) => _acc.DisconnectAsync(delete);

        /// <summary>Returns the session id (GUID).</summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The terminal session GUID.</returns>
        public Task<Guid> GetIdAsync(CancellationToken ct = default) => _acc.GetIdAsync(ct);
        #endregion

        #region Account_Information
        /// <summary>Full account snapshot (balance, equity, margin, leverage, currency, etc.) in one round trip.</summary>
        public Task<AccountSummaryData> AccountSummaryAsync(DateTime? deadline = null, CancellationToken ct = default)
            => _acc.AccountSummaryAsync(deadline, ct);

        /// <summary>Account balance (deposit currency).</summary>
        public async Task<double> GetBalanceAsync(DateTime? deadline = null, CancellationToken ct = default)
            => (await AccountSummaryAsync(deadline, ct)).AccountBalance;
        /// <summary>Account equity (balance + floating P/L).</summary>
        public async Task<double> GetEquityAsync(DateTime? deadline = null, CancellationToken ct = default)
            => (await AccountSummaryAsync(deadline, ct)).AccountEquity;
        /// <summary>Margin currently used.</summary>
        public async Task<double> GetMarginAsync(DateTime? deadline = null, CancellationToken ct = default)
            => (await AccountSummaryAsync(deadline, ct)).AccountMargin;
        /// <summary>Free margin available for new trades.</summary>
        public async Task<double> GetFreeMarginAsync(DateTime? deadline = null, CancellationToken ct = default)
            => (await AccountSummaryAsync(deadline, ct)).AccountMarginFree;
        /// <summary>Margin level, percent.</summary>
        public async Task<double> GetMarginLevelAsync(DateTime? deadline = null, CancellationToken ct = default)
            => (await AccountSummaryAsync(deadline, ct)).AccountMarginLevel;
        /// <summary>Account credit.</summary>
        public async Task<double> GetCreditAsync(DateTime? deadline = null, CancellationToken ct = default)
            => (await AccountSummaryAsync(deadline, ct)).AccountCredit;
        /// <summary>Deposit currency (e.g. "USD").</summary>
        public async Task<string> GetCurrencyAsync(DateTime? deadline = null, CancellationToken ct = default)
            => (await AccountSummaryAsync(deadline, ct)).AccountCurrency;
        /// <summary>Account holder name.</summary>
        public async Task<string> GetAccountNameAsync(DateTime? deadline = null, CancellationToken ct = default)
            => (await AccountSummaryAsync(deadline, ct)).AccountUserName;
        /// <summary>Broker company name.</summary>
        public async Task<string> GetCompanyAsync(DateTime? deadline = null, CancellationToken ct = default)
            => (await AccountSummaryAsync(deadline, ct)).AccountCompanyName;

        /// <summary>Trade statistics (counts, profit, drawdown, per-market breakdown, ratios).</summary>
        public Task<StatsData> TradeStatsAsync(string? from = null, DateTime? deadline = null, CancellationToken ct = default)
            => _acc.TradeStatsAsync(from, deadline, ct);
        /// <summary>Per-order unrealized P/L extrema.</summary>
        public Task<TradeUnrealizedPLData> TradeUnrealizedPLAsync(string? from = null, DateTime? deadline = null, CancellationToken ct = default)
            => _acc.TradeUnrealizedPLAsync(from, deadline, ct);
        /// <summary>Equity history over time.</summary>
        public Task<EquityHistoryData> EquityHistoryAsync(string? from = null, bool excludeSameBars = false, DateTime? deadline = null, CancellationToken ct = default)
            => _acc.EquityHistoryAsync(from, excludeSameBars, deadline, ct);
        /// <summary>Trade-stats equity history (stats + charts).</summary>
        public Task<StatsWithChartsData> TradeStatsEquityHistoryAsync(string? from = null, bool excludeSameBars = false, DateTime? deadline = null, CancellationToken ct = default)
            => _acc.TradeStatsEquityHistoryAsync(from, excludeSameBars, deadline, ct);
        #endregion

        #region Symbol_Information
        /// <summary>All available instrument names.</summary>
        public Task<SymbolsData> SymbolsAsync(DateTime? deadline = null, CancellationToken ct = default)
            => _acc.SymbolsAsync(deadline, ct);
        /// <summary>Symbol parameters (digits, lot limits, etc.); pass a symbol to filter, null for all.</summary>
        public Task<SymbolParamsManyData> SymbolParamsManyAsync(string? symbol = null, DateTime? deadline = null, CancellationToken ct = default)
            => _acc.SymbolParamsManyAsync(symbol, deadline, ct);
        /// <summary>Top-of-book quote for a symbol.</summary>
        public Task<QuoteData> QuoteAsync(string symbol, DateTime? deadline = null, CancellationToken ct = default)
            => _acc.QuoteAsync(symbol, deadline, ct);
        /// <summary>Batch quotes for several symbols.</summary>
        public Task<QuoteManyData> QuoteManyAsync(string[] symbols, DateTime? deadline = null, CancellationToken ct = default)
            => _acc.QuoteManyAsync(symbols, deadline, ct);
        /// <summary>Historical quotes (the classic QuoteHistory RPC).</summary>
        public Task<QuoteHistoryData> QuoteHistoryAsync(string symbol, ENUM_QUOTE_HISTORY_TIMEFRAME timeframe, DateTime from, DateTime to, DateTime? deadline = null, CancellationToken ct = default)
            => _acc.QuoteHistoryAsync(symbol, timeframe, from, to, deadline, ct);
        /// <summary>OHLC bars (the Bars RPC); from/to are UTC.</summary>
        public Task<QuoteHistoryBarsData> BarsAsync(string symbol, QUOTE_HISTORY_TIMEFRAME timeframe, DateTime fromUtc, DateTime toUtc, DateTime? deadline = null, CancellationToken ct = default)
            => _acc.BarsAsync(symbol, timeframe, fromUtc, toUtc, deadline, ct);
        /// <summary>Tick value and contract size per symbol.</summary>
        public Task<TickValueWithSizeData> TickValueWithSizeAsync(string[] symbols, DateTime? deadline = null, CancellationToken ct = default)
            => _acc.TickValueWithSizeAsync(symbols, deadline, ct);
        /// <summary>Adds/removes a symbol in Market Watch; returns its current quote.</summary>
        public Task<SymbolSelectData> SymbolSelectAsync(string symbol, bool select = true, DateTime? deadline = null, CancellationToken ct = default)
            => _acc.SymbolSelectAsync(symbol, select, deadline, ct);
        /// <summary>Server timezone / UTC offset info.</summary>
        public Task<ServerTimezoneData> ServerTimezoneAsync(DateTime? deadline = null, CancellationToken ct = default)
            => _acc.ServerTimezoneAsync(deadline, ct);

        private async Task<SymbolParamsManyInfo?> FirstParamAsync(string symbol, DateTime? deadline, CancellationToken ct)
        {
            var r = await SymbolParamsManyAsync(symbol, deadline, ct);
            return r.SymbolInfos.FirstOrDefault();
        }
        /// <summary>Price digits for a symbol.</summary>
        public async Task<int> GetDigitsAsync(string symbol, DateTime? deadline = null, CancellationToken ct = default)
            => (int)((await FirstParamAsync(symbol, deadline, ct))?.Digits ?? 5);
        /// <summary>Point size (10^-digits) for a symbol.</summary>
        public async Task<double> GetPointAsync(string symbol, DateTime? deadline = null, CancellationToken ct = default)
            => Math.Pow(10, -await GetDigitsAsync(symbol, deadline, ct));
        /// <summary>Current spread in points (computed from the quote).</summary>
        public async Task<double> GetSpreadAsync(string symbol, DateTime? deadline = null, CancellationToken ct = default)
        {
            var q = await QuoteAsync(symbol, deadline, ct);
            var pt = await GetPointAsync(symbol, deadline, ct);
            return pt > 0 ? (q.Ask - q.Bid) / pt : 0;
        }
        /// <summary>Minimum tradable volume (lots).</summary>
        public async Task<double> GetVolumeMinAsync(string symbol, DateTime? deadline = null, CancellationToken ct = default)
            => (await FirstParamAsync(symbol, deadline, ct))?.VolumeMin ?? 0;
        /// <summary>Maximum tradable volume (lots).</summary>
        public async Task<double> GetVolumeMaxAsync(string symbol, DateTime? deadline = null, CancellationToken ct = default)
            => (await FirstParamAsync(symbol, deadline, ct))?.VolumeMax ?? 0;
        /// <summary>Volume step (lots).</summary>
        public async Task<double> GetVolumeStepAsync(string symbol, DateTime? deadline = null, CancellationToken ct = default)
            => (await FirstParamAsync(symbol, deadline, ct))?.VolumeStep ?? 0;
        /// <summary>Current bid price.</summary>
        public async Task<double> GetBidAsync(string symbol, DateTime? deadline = null, CancellationToken ct = default)
            => (await QuoteAsync(symbol, deadline, ct)).Bid;
        /// <summary>Current ask price.</summary>
        public async Task<double> GetAskAsync(string symbol, DateTime? deadline = null, CancellationToken ct = default)
            => (await QuoteAsync(symbol, deadline, ct)).Ask;
        #endregion

        #region Positions_And_Orders
        /// <summary>Open orders and pending orders (full details).</summary>
        public Task<OpenedOrdersData> OpenedOrdersAsync(EnumOpenedOrderSortType sortType = EnumOpenedOrderSortType.SortByOpenTimeAsc, DateTime? deadline = null, CancellationToken ct = default)
            => _acc.OpenedOrdersAsync(sortType, deadline, ct);
        /// <summary>Lightweight list of open-order tickets.</summary>
        public Task<OpenedOrdersTicketsData> OpenedOrdersTicketsAsync(DateTime? deadline = null, CancellationToken ct = default)
            => _acc.OpenedOrdersTicketsAsync(deadline, ct);
        /// <summary>Closed/cancelled order history within [from..to].</summary>
        public Task<OrdersHistoryData> OrdersHistoryAsync(DateTime from, DateTime to, EnumOrderHistorySortType sortType = EnumOrderHistorySortType.HistorySortByCloseTimeDesc, int? page = null, int? itemsPerPage = null, DateTime? deadline = null, CancellationToken ct = default)
            => _acc.OrdersHistoryAsync(sortType, from, to, page, itemsPerPage, deadline, ct);
        #endregion

        #region Trading
        /// <summary>Sends a market or pending order.</summary>
        public Task<OrderSendData> OrderSendAsync(OrderSendRequest request, DateTime? deadline = null, CancellationToken ct = default)
            => _acc.OrderSendAsync(request, deadline, ct);
        /// <summary>Modifies an order (SL/TP/price).</summary>
        public Task<OrderModifyData> OrderModifyAsync(OrderModifyRequest request, DateTime? deadline = null, CancellationToken ct = default)
            => _acc.OrderModifyAsync(request, deadline, ct);
        /// <summary>Closes a market order / deletes a pending order.</summary>
        public Task<OrderCloseDeleteData> OrderCloseDeleteAsync(OrderCloseDeleteRequest request, DateTime? deadline = null, CancellationToken ct = default)
            => _acc.OrderCloseDeleteAsync(request, deadline, ct);
        /// <summary>Closes a position by an opposite one (hedge close-by).</summary>
        public Task<OrderCloseByData> OrderCloseByAsync(OrderCloseByRequest request, DateTime? deadline = null, CancellationToken ct = default)
            => _acc.OrderCloseByAsync(request, deadline, ct);
        #endregion

        #region Subscriptions
        /// <summary>Live tick stream for the given symbols.</summary>
        public IAsyncEnumerable<OnSymbolTickData> OnSymbolTickAsync(IEnumerable<string> symbols, CancellationToken ct = default)
            => _acc.OnSymbolTickAsync(symbols, ct);
        /// <summary>Live trade-event stream.</summary>
        public IAsyncEnumerable<OnTradeData> OnTradeAsync(CancellationToken ct = default)
            => _acc.OnTradeAsync(ct);
        /// <summary>Polling stream of open-order profit snapshots.</summary>
        public IAsyncEnumerable<OnOpenedOrdersProfitData> OnOpenedOrdersProfitAsync(int intervalMs = 1000, CancellationToken ct = default)
            => _acc.OnOpenedOrdersProfitAsync(intervalMs, ct);
        /// <summary>Polling stream of open-order ticket snapshots.</summary>
        public IAsyncEnumerable<OnOpenedOrdersTicketsData> OnOpenedOrdersTicketsAsync(int intervalMs = 1000, CancellationToken ct = default)
            => _acc.OnOpenedOrdersTicketsAsync(intervalMs, ct);
        /// <summary>Stream of terminal connection-state events.</summary>
        public IAsyncEnumerable<ConnectStateData> OnConnectStateAsync(CancellationToken ct = default)
            => _acc.OnConnectStateAsync(ct);
        #endregion
    }
}
