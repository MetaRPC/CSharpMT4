/*══════════════════════════════════════════════════════════════════════════════
 FILE: Examples/LowLevel/1_LowLevel.cs - LOW-LEVEL MT4 API INFORMATION DEMO
 PURPOSE:
   Comprehensive demonstration of MT4 information retrieval via the raw MT4Account
   client. A REFERENCE GUIDE for account, symbol and order/position queries
   WITHOUT trading (see 2_Trading.cs for trading, 3_Streaming.cs for streams).

 🎯 WHO SHOULD USE THIS:
   • Developers learning the MT4 gRPC API read methods
   • Users building monitoring / dashboard applications
   • Debugging data retrieval and raw protobuf reply shapes
   • Learning read-only operations on MT4Account

 📚 WHAT THIS DEMO COVERS (3 Sections):

   1. ACCOUNT INFORMATION
      • AccountSummaryAsync()      - all money/permission fields in one round trip
      • TradeStatsAsync()          - counts / profit / drawdown / per-market stats
      • EquityHistoryAsync()       - equity-curve points (size reported only)
      • TradeUnrealizedPLAsync()   - per-order unrealized P/L extrema

   2. SYMBOL INFORMATION
      • ServerTimezoneAsync()      - trade-server timezone / UTC offset
      • SymbolsAsync()             - full symbol catalog
      • SymbolParamsManyAsync()    - contract specs (digits, volume, currencies)
      • QuoteAsync() / QuoteManyAsync() - current Bid/Ask
      • SymbolSelectAsync()        - add a symbol to Market Watch
      • TickValueWithSizeAsync()   - tick value / size (risk-sizing input)
      • QuoteHistoryAsync() / BarsAsync() - historical quotes and bars

   3. POSITIONS & ORDERS
      • OpenedOrdersAsync()        - all open orders / positions
      • OpenedOrdersTicketsAsync() - ticket numbers only (lightweight)
      • OrdersHistoryAsync()       - closed orders over a time range

 🔄 COMPARISON: Low-Level vs High-Level API:

   Low-Level (MT4Account) - THIS FILE:
   ✓ Direct gRPC calls with raw protobuf replies
   ✓ Maximum control; see exactly what the API returns
   ✗ More verbose; handle protobuf structures manually

   High-Level (MT4Service / MT4Sugar):
   ✓ Typed wrappers and one-liner helpers, less boilerplate
   ✗ Less control over the exact API calls

 💡 WHEN TO USE LOW-LEVEL API:
   • You need methods not wrapped by MT4Service
   • Building your own wrappers, or debugging the high-level ones
   • You need exact control over the protobuf request/response

 📖 RELATED EXAMPLES:
   • 2_Trading.cs   - OrderSend / OrderModify / OrderClose
   • 3_Streaming.cs - real-time ticks / trades / P&L
   • 4_ServiceExamples.cs, 5_SugarScenario.cs - mid/high-level API

 USAGE:
   Run `dotnet run` and choose [1] from the menu
   (or: dotnet run -- --example=1)
══════════════════════════════════════════════════════════════════════════════*/

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

            #region ACCOUNT INFORMATION
            // ══════════════════════════════════════════════════════════════
            // 1. ACCOUNT INFORMATION
            //    Direct MT4Account reads — the raw API, no typed wrapper. Each
            //    method is called and concrete Reply fields are read so you can
            //    see exactly what the server returns.
            // ══════════════════════════════════════════════════════════════
            Sec("ACCOUNT_INFORMATION");

            // ─────────────────────────────────────────────────────────────
            // AccountSummaryAsync() - one round trip returns ALL money and
            //    permission fields (MT4 has no separate AccountInfo* calls).
            // ─────────────────────────────────────────────────────────────
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

            #endregion

            #region SYMBOL INFORMATION
            // ══════════════════════════════════════════════════════════════
            // 2. SYMBOL INFORMATION
            //    Server timezone, the symbol catalog, contract specs, quotes,
            //    Market Watch selection, tick value and price history.
            // ══════════════════════════════════════════════════════════════
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

            #endregion

            #region POSITIONS & ORDERS
            // ══════════════════════════════════════════════════════════════
            // 3. POSITIONS & ORDERS
            //    Currently open orders/positions, their ticket numbers, and
            //    the recent closed-order history.
            // ══════════════════════════════════════════════════════════════
            Sec("POSITIONS_AND_ORDERS");
            var open = await acc.OpenedOrdersAsync(EnumOpenedOrderSortType.SortByOpenTimeAsc, cancellationToken: ct);
            Console.WriteLine($"OpenedOrders: {open.OrderInfos.Count}");
            foreach (var o in open.OrderInfos.Take(5))
                Console.WriteLine($"  #{o.Ticket} {o.OrderType} {o.Symbol} lots={o.Lots} open={o.OpenPrice} profit={o.Profit}");

            var tk = await acc.OpenedOrdersTicketsAsync(cancellationToken: ct);
            Console.WriteLine($"OpenedOrdersTickets: {tk.Tickets.Count}: [{string.Join(", ", tk.Tickets)}]");

            var hist = await acc.OrdersHistoryAsync(EnumOrderHistorySortType.HistorySortByCloseTimeDesc, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, null, null, cancellationToken: ct);
            Console.WriteLine($"OrdersHistory(7d): {hist.OrdersInfo.Count} closed order(s)");
            #endregion

            Console.WriteLine("\n=== LOW-LEVEL (info) DONE ===");
        }
    }
}
