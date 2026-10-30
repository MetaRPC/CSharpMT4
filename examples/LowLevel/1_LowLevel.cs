using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mt4_term_api;

namespace MetaRPC.CSharpMT4.Examples.LowLevel
{
    /// <summary>
    /// LOW-LEVEL (info) examples: direct <see cref="MT4Account"/> read calls - Account, Symbol and Orders info.
    /// Shows the full shape of each call (request where applicable) and reads concrete Reply fields, so you can
    /// see exactly what data the raw API returns. Trading lives in Trading.cs, streams in Streaming.cs.
    /// </summary>
    public static class LowLevelInfo
    {
        private static void Sec(string t) => Console.WriteLine($"\n-------------------- {t} --------------------");

        /// <summary>Runs the account/symbol/orders info demos against a connected account.</summary>
        public static async Task RunAsync(MT4Account acc, string symbol = "EURUSD", CancellationToken ct = default)
        {
            var syms = new[] { symbol, "GBPUSD", "USDJPY" };

            // ==================== ACCOUNT_INFORMATION ====================
            // Direct MT4Account reads — this is the raw API, no typed wrapper. We call each method and read
            // concrete fields off its Reply so you can see exactly what the server returns.
            Sec("ACCOUNT_INFORMATION");

            // AccountSummary - one round trip returns all money/permission fields (no separate AccountInfo* on MT4).
            var a = await acc.AccountSummaryAsync(cancellationToken: ct);
            Console.WriteLine($"Login={a.AccountLogin}  Name={a.AccountUserName}  Company={a.AccountCompanyName}");
            Console.WriteLine($"Balance={a.AccountBalance}  Equity={a.AccountEquity}  Credit={a.AccountCredit}");
            Console.WriteLine($"Margin={a.AccountMargin}  Free={a.AccountMarginFree}  Level={a.AccountMarginLevel}");
            Console.WriteLine($"Leverage={a.AccountLeverage}  Currency={a.AccountCurrency}  TradeMode={a.AccountTradeMode}");

            // TradeStats - counts/profit/drawdown/per-market breakdown.
            var st = (await acc.TradeStatsAsync(cancellationToken: ct)).Summary;
            Console.WriteLine($"TradeStats: open={st?.OpenTrades} openProfit={st?.OpenProfit} total={st?.TotalProfit}");

            // Equity curve points + per-order unrealized extrema. These replies are large, so we just
            // report size/counts rather than printing them.
            var eq = await acc.EquityHistoryAsync(cancellationToken: ct);
            Console.WriteLine($"EquityHistory: ok (payload {eq.CalculateSize()} bytes)");
            var upl = await acc.TradeUnrealizedPLAsync(cancellationToken: ct);
            Console.WriteLine($"TradeUnrealizedPL: {upl.Extrema.Count} ticket(s)");

            // ==================== SYMBOL_INFORMATION ====================
            Sec("SYMBOL_INFORMATION");
            var tz = await acc.ServerTimezoneAsync(cancellationToken: ct);
            Console.WriteLine($"ServerTimezone: {tz.Server} utcOffset={tz.UtcOffsetSeconds}s");

            // Symbols catalog.
            var sy = await acc.SymbolsAsync(cancellationToken: ct);
            Console.WriteLine($"Symbols: {sy.SymbolNameInfos.Count}; first: {string.Join(", ", sy.SymbolNameInfos.Take(5).Select(x => x.SymbolName))}");

            // Symbol parameters (filter by symbol). Read a few fields off the first item.
            var sp = (await acc.SymbolParamsManyAsync(symbol, cancellationToken: ct)).SymbolInfos.FirstOrDefault();
            if (sp != null)
                Console.WriteLine($"{sp.SymbolName}: Digits={sp.Digits} Vol[{sp.VolumeMin}..{sp.VolumeMax}/{sp.VolumeStep}] CcyBase={sp.CurrencyBase}");

            // Quotes.
            var q = await acc.QuoteAsync(symbol, cancellationToken: ct);
            Console.WriteLine($"Quote {q.Symbol}: Bid={q.Bid} Ask={q.Ask} High={q.High} Low={q.Low} Time={q.DateTime}");
            var qm = await acc.QuoteManyAsync(syms, cancellationToken: ct);
            Console.WriteLine($"QuoteMany: {string.Join("  ", qm.Quotes.Select(x => $"{x.Symbol} {x.Bid}/{x.Ask}"))}");

            // Market Watch.
            var sel = await acc.SymbolSelectAsync(symbol, true, cancellationToken: ct);
            Console.WriteLine($"SymbolSelect({symbol}): success={sel.Success}");

            // Tick value / contract size (risk sizing input).
            var tv = (await acc.TickValueWithSizeAsync(syms, cancellationToken: ct)).Infos.FirstOrDefault();
            if (tv != null)
                Console.WriteLine($"TickValue {tv.SymbolName}: value={tv.TradeTickValue} tickSize={tv.TradeTickSize} contract={tv.TradeContractSize}");

            // History: classic QuoteHistory + Bars (last 6h, H1).
            var to = DateTime.UtcNow; var from = to.AddHours(-6);
            var qh = await acc.QuoteHistoryAsync(symbol, ENUM_QUOTE_HISTORY_TIMEFRAME.QhPeriodH1, from, to, cancellationToken: ct);
            var h0 = qh.HistoricalQuotes.FirstOrDefault();
            Console.WriteLine($"QuoteHistory(H1): {qh.HistoricalQuotes.Count} quote(s); first O={h0?.Open} H={h0?.High} L={h0?.Low} C={h0?.Close} @ {h0?.Time}");
            var bars = await acc.BarsAsync(symbol, QUOTE_HISTORY_TIMEFRAME.QhbPeriodH1, from, to, cancellationToken: ct);
            var b0 = bars.Bars.FirstOrDefault();
            Console.WriteLine($"Bars(H1): {bars.Bars.Count} bars; first O={b0?.Open} H={b0?.High} L={b0?.Low} C={b0?.Close} V={b0?.TickVolume}");

            // ==================== POSITIONS_AND_ORDERS ====================
            Sec("POSITIONS_AND_ORDERS");
            var open = await acc.OpenedOrdersAsync(EnumOpenedOrderSortType.SortByOpenTimeAsc, cancellationToken: ct);
            Console.WriteLine($"OpenedOrders: {open.OrderInfos.Count}");
            foreach (var o in open.OrderInfos.Take(5))
                Console.WriteLine($"  #{o.Ticket} {o.OrderType} {o.Symbol} lots={o.Lots} open={o.OpenPrice} profit={o.Profit}");

            var tk = await acc.OpenedOrdersTicketsAsync(cancellationToken: ct);
            Console.WriteLine($"OpenedOrdersTickets: {tk.Tickets.Count}: [{string.Join(", ", tk.Tickets)}]");

            var hist = await acc.OrdersHistoryAsync(EnumOrderHistorySortType.HistorySortByCloseTimeDesc, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, null, null, cancellationToken: ct);
            Console.WriteLine($"OrdersHistory(7d): {hist.OrdersInfo.Count} closed order(s)");

            Console.WriteLine("\n=== LOW-LEVEL (info) DONE ===");
        }
    }
}
