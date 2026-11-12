/*══════════════════════════════════════════════════════════════════════════════
 FILE: Examples/Services/4_ServiceExamples.cs - MID-LEVEL (MT4Service) DEMO SUITE
 PURPOSE:
   One-file tour of the typed, data-returning MT4Service wrapper. Walks every
   canonical group as a section: account info, symbol info, positions/orders,
   trading (described, not executed) and live subscriptions. Output is compact —
   large protobuf replies are summarized (counts + a few key fields).

 🎯 WHO SHOULD USE THIS:
   • Developers moving from the raw MT4Account to the typed MT4Service wrapper
   • Users who want cleaner, data-returning calls with less boilerplate
   • Building monitoring/reporting on top of the mid-level API
   • Learning the full surface of MT4Service in one place

 📚 WHAT THIS DEMO COVERS (5 Sections):

   1. ACCOUNT INFORMATION
      • AccountSummaryAsync()         - balance / equity / margin / currency in one call
      • GetAccountNameAsync() / GetCompanyAsync() / GetMarginLevelAsync() /
        GetCreditAsync() / GetIdAsync() - single-field getters
      • TradeStatsAsync()             - counts / profit / drawdown
      • TradeUnrealizedPLAsync()      - per-order unrealized P/L extrema
      • EquityHistoryAsync()          - equity-curve points
      • TradeStatsEquityHistoryAsync()- stats + equity + trades in one call

   2. SYMBOL INFORMATION
      • ServerTimezoneAsync()         - trade-server timezone
      • SymbolsAsync()                - instrument catalog
      • QuoteAsync() / QuoteManyAsync() - current Bid/Ask
      • GetDigits/Point/Spread/Bid/Ask Async() - per-symbol scalar getters
      • GetVolumeMin/Max/Step Async() - volume limits
      • SymbolSelectAsync()           - add to Market Watch
      • SymbolParamsManyAsync()       - full parameter record
      • TickValueWithSizeAsync()      - money-per-point for risk sizing
      • QuoteHistoryAsync() / BarsAsync() - historical quotes and OHLC bars

   3. POSITIONS & ORDERS
      • OpenedOrdersAsync()           - full open-order rows
      • OpenedOrdersTicketsAsync()    - ticket numbers only (lightweight)
      • OrdersHistoryAsync()          - closed orders over a time range

   4. TRADING (described, NOT executed)
      • OrderSendAsync / OrderModifyAsync / OrderCloseDeleteAsync /
        OrderCloseByAsync - shown as shapes only; no orders are sent here

   5. SUBSCRIPTIONS (3s samples)
      • OnSymbolTickAsync / OnTradeAsync / OnOpenedOrdersProfitAsync /
        OnOpenedOrdersTicketsAsync / OnConnectStateAsync - live streams, sampled

 🔄 COMPARISON: API levels:
   Low-Level  (MT4Account) - raw gRPC / protobuf, maximum control, verbose
   Mid-Level  (MT4Service) - THIS FILE: typed wrappers, data-returning, cleaner
   High-Level (MT4Sugar)   - one-liner helpers and smart defaults

 📖 RELATED EXAMPLES:
   • 1_LowLevel.cs    - the same data via the raw MT4Account
   • 5_SugarScenario.cs - high-level assess → plan → execute flow
   • 3_Streaming.cs   - full streaming examples

 USAGE:
   Run `dotnet run` and choose [4] from the menu
   (or: dotnet run -- --example=4)

 ℹ️  SAFE: read-only — trading is described, not executed; no orders are sent.
══════════════════════════════════════════════════════════════════════════════*/

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mt4_term_api;

namespace MetaRPC.CSharpMT4.Examples.Services
{
    /// <summary>
    /// SERVICE-LEVEL examples: everything through <see cref="MT4Service"/> (the typed, data-returning wrapper).
    /// One file, all canonical groups as sections. Read-only by default; trading is described, not executed.
    /// Output is deliberately compact — big protobuf replies are summarized (counts + a few key fields)
    /// instead of dumping the whole message, so the console stays readable.
    /// </summary>
    public static class ServiceExamples
    {
        private static void Sec(string title)
            => Console.WriteLine($"\n==================== {title} ====================");

        // Aligned "key = value" row for scalar readouts.
        private static void Row(string key, object? value)
            => Console.WriteLine($"  {key,-20} {value}");

        /// <summary>Runs the whole service-level demo suite against a connected service.</summary>
        /// <param name="svc">A connected MT4Service.</param>
        /// <param name="symbol">Base symbol to use (default EURUSD).</param>
        /// <param name="ct">Cancellation token (for stream sampling).</param>
        public static async Task RunAllAsync(MT4Service svc, string symbol = "EURUSD", CancellationToken ct = default)
        {
            var syms = new[] { symbol, "GBPUSD", "USDJPY" };

            #region ACCOUNT INFORMATION
            // ══════════════════════════════════════════════════════════════
            // 1. ACCOUNT INFORMATION
            //    The money/permissions side of the account. AccountSummary is
            //    the main call; the Get* helpers are thin single-field shortcuts.
            // ══════════════════════════════════════════════════════════════
            Sec("1. ACCOUNT_INFORMATION");

            // ─────────────────────────────────────────────────────────────
            // AccountSummaryAsync() - one round trip returns balance, equity,
            //    margin and currency (the whole money/permission record).
            // ─────────────────────────────────────────────────────────────
            var acc = await svc.AccountSummaryAsync(ct: ct);
            Row("Login", acc.AccountLogin);
            Row("Balance / Equity", $"{acc.AccountBalance} / {acc.AccountEquity} {acc.AccountCurrency}");
            Row("Margin / Free", $"{acc.AccountMargin} / {acc.AccountMarginFree}");

            // Single-field getters — same data as the summary above, one value at a time.
            Row("Getters", $"Name={await svc.GetAccountNameAsync(ct: ct)} Company={await svc.GetCompanyAsync(ct: ct)}");
            Row("Getters", $"Level={await svc.GetMarginLevelAsync(ct: ct)} Credit={await svc.GetCreditAsync(ct: ct)} Id={await svc.GetIdAsync(ct: ct)}");

            // Historical/statistical reads. These return large protobuf payloads, so we summarize each
            // (counts + size) rather than dumping it — the point is to show the call works and what it yields.
            var stats = (await svc.TradeStatsAsync(ct: ct)).Summary;
            Row("TradeStats", $"open={stats?.OpenTrades} openProfit={stats?.OpenProfit} total={stats?.TotalProfit}");

            var upl = await svc.TradeUnrealizedPLAsync(ct: ct);
            Row("TradeUnrealizedPL", $"{upl.Extrema.Count} extrema row(s)");

            var eq = await svc.EquityHistoryAsync(ct: ct);
            Row("EquityHistory", $"{eq.Points.Count} point(s), {eq.CalculateSize()} bytes");

            var sc = await svc.TradeStatsEquityHistoryAsync(ct: ct);
            Row("TradeStatsEquityHistory", $"{sc.Charts.Count} chart point(s), {sc.Trades.Count} trade(s)");

            #endregion

            #region SYMBOL INFORMATION
            // ══════════════════════════════════════════════════════════════
            // 2. SYMBOL INFORMATION
            //    Everything about an instrument: contract spec, current price
            //    and historical candles — the inputs to risk sizing and orders.
            // ══════════════════════════════════════════════════════════════
            Sec("2. SYMBOL_INFORMATION");

            Row("ServerTimezone", await svc.ServerTimezoneAsync(ct: ct));

            // The instrument catalog (what this account is allowed to trade).
            var symsData = await svc.SymbolsAsync(ct: ct);
            Row("Symbols", $"{symsData.SymbolNameInfos.Count} instruments");

            // ─────────────────────────────────────────────────────────────
            // QuoteAsync() / QuoteManyAsync() - live Bid/Ask for one symbol,
            //    then several instruments in a single round trip.
            // ─────────────────────────────────────────────────────────────
            var q = await svc.QuoteAsync(symbol, ct: ct);
            Row($"Quote {q.Symbol}", $"Bid={q.Bid} Ask={q.Ask}");
            var qm = await svc.QuoteManyAsync(syms, ct: ct);
            Row("QuoteMany", string.Join("  ", qm.Quotes.Select(x => $"{x.Symbol} {x.Bid}/{x.Ask}")));

            // Per-symbol scalar getters — digits/point drive price rounding; spread/bid/ask are the live market.
            Row("Getters", $"digits={await svc.GetDigitsAsync(symbol, ct: ct)} point={await svc.GetPointAsync(symbol, ct: ct)} spread={await svc.GetSpreadAsync(symbol, ct: ct):F1}pt bid={await svc.GetBidAsync(symbol, ct: ct)} ask={await svc.GetAskAsync(symbol, ct: ct)}");

            // Volume limits — any order volume must sit within [min..max] and on the step grid.
            Row("Vol limits", $"[{await svc.GetVolumeMinAsync(symbol, ct: ct)}..{await svc.GetVolumeMaxAsync(symbol, ct: ct)}/{await svc.GetVolumeStepAsync(symbol, ct: ct)}]");

            // Ensure the symbol is in Market Watch, then read its full parameter record.
            Row("SymbolSelect", $"success={(await svc.SymbolSelectAsync(symbol, true, ct: ct)).Success}");
            var sp = (await svc.SymbolParamsManyAsync(symbol, ct: ct)).SymbolInfos.FirstOrDefault();
            Row("SymbolParamsMany", $"{symbol}: digits={sp?.Digits} volMin={sp?.VolumeMin} volStep={sp?.VolumeStep}");

            // Tick value / contract size — the money-per-point needed to convert a risk budget into lots.
            var tv = await svc.TickValueWithSizeAsync(syms, ct: ct);
            Row("TickValueWithSize", string.Join("  ", tv.Infos.Select(x => $"{x.SymbolName}={x.TradeTickValue}")));

            // Historical candles: QuoteHistory (quote-based) and Bars (OHLC), both last 6h on H1.
            var qh = await svc.QuoteHistoryAsync(symbol, ENUM_QUOTE_HISTORY_TIMEFRAME.QhPeriodH1, DateTime.UtcNow.AddHours(-6), DateTime.UtcNow, ct: ct);
            Row("QuoteHistory(H1,6h)", $"{qh.HistoricalQuotes.Count} quote(s)");
            var bars = await svc.BarsAsync(symbol, QUOTE_HISTORY_TIMEFRAME.QhbPeriodH1, DateTime.UtcNow.AddHours(-6), DateTime.UtcNow, ct: ct);
            var b0 = bars.Bars.FirstOrDefault();
            Row("Bars(H1,6h)", $"{bars.Bars.Count} bar(s); first O={b0?.Open} H={b0?.High} L={b0?.Low} C={b0?.Close} V={b0?.TickVolume}");

            #endregion

            #region POSITIONS & ORDERS
            // ══════════════════════════════════════════════════════════════
            // 3. POSITIONS & ORDERS
            //    The current open book plus recent history. OpenedOrders gives
            //    full rows; the tickets call is a lightweight "just the IDs".
            // ══════════════════════════════════════════════════════════════
            Sec("3. POSITIONS_AND_ORDERS");

            var opened = await svc.OpenedOrdersAsync(ct: ct);
            Row("OpenedOrders", $"{opened.OrderInfos.Count} order(s)");
            var tickets = await svc.OpenedOrdersTicketsAsync(ct: ct);
            Row("OpenedOrderTickets", $"{tickets.Tickets.Count}: [{string.Join(", ", tickets.Tickets)}]");
            var hist = await svc.OrdersHistoryAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, ct: ct);
            Row("OrdersHistory(7d)", $"{hist.OrdersInfo.Count} order(s)");

            #endregion

            #region TRADING (described, not executed)
            // ══════════════════════════════════════════════════════════════
            // 4. TRADING (described - NOT executed)
            //    Read-only: no orders are sent here, only the shape of the
            //    trading calls is shown. The orchestrators / sugar scenario
            //    demonstrate the real send/modify/close flow (guarded).
            // ══════════════════════════════════════════════════════════════
            Sec("4. TRADING (described - not executed)");
            Console.WriteLine("  OrderSendAsync / OrderModifyAsync / OrderCloseDeleteAsync / OrderCloseByAsync");
            Console.WriteLine("  Build an OrderSendRequest { Symbol, OperationType=OcOpBuy/OcOpSell, Volume, Price, Stoploss, Takeprofit } and call svc.OrderSendAsync(req).");

            #endregion

            #region SUBSCRIPTIONS
            // ══════════════════════════════════════════════════════════════
            // 5. SUBSCRIPTIONS (3s samples)
            //    Live streams. Each is sampled for 3 seconds and events are
            //    counted; a long-running app would keep consuming the stream.
            // ══════════════════════════════════════════════════════════════
            Sec("5. SUBSCRIPTIONS (3s samples)");
            await SampleAsync(c => svc.OnSymbolTickAsync(new[] { symbol }, c), "OnSymbolTick", 3, ct);
            await SampleAsync(c => svc.OnTradeAsync(c), "OnTrade", 3, ct);
            await SampleAsync(c => svc.OnOpenedOrdersProfitAsync(1000, c), "OnOpenedOrdersProfit", 3, ct);
            await SampleAsync(c => svc.OnOpenedOrdersTicketsAsync(1000, c), "OnOpenedOrdersTickets", 3, ct);
            await SampleAsync(c => svc.OnConnectStateAsync(c), "OnConnectState", 3, ct);
            #endregion

            Console.WriteLine("\n=== SERVICE DEMO DONE ===");
        }

        // Counts events over a short window without dumping the (large) payload of each message.
        private static async Task SampleAsync<T>(Func<CancellationToken, System.Collections.Generic.IAsyncEnumerable<T>> make, string name, int seconds, CancellationToken outer)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(outer);
            cts.CancelAfter(TimeSpan.FromSeconds(seconds));
            int n = 0;
            try { await foreach (var _ in make(cts.Token)) { if (++n >= 5) break; } }
            catch (OperationCanceledException) { }
            Row(name, $"{n} event(s) in {seconds}s");
        }
    }
}
