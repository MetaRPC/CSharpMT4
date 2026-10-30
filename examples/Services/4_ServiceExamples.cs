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

            // ---- Account_Information ----
            // The money/permissions side of the account. AccountSummary is the main call: one round trip
            // returns balance, equity, margin and currency. The Get* helpers are thin shortcuts that pull a
            // single field each — handy when you only need one value and don't want the whole summary object.
            Sec("1. ACCOUNT_INFORMATION");

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

            // ---- Symbol_Information ----
            // Everything about an instrument: its contract spec, current price and historical candles.
            // This is the data you feed into risk sizing and order placement.
            Sec("2. SYMBOL_INFORMATION");

            Row("ServerTimezone", await svc.ServerTimezoneAsync(ct: ct));

            // The instrument catalog (what this account is allowed to trade).
            var symsData = await svc.SymbolsAsync(ct: ct);
            Row("Symbols", $"{symsData.SymbolNameInfos.Count} instruments");

            // Live prices: one symbol, then several in a single round trip.
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

            // ---- Positions_And_Orders ----
            // The current open book plus recent history. OpenedOrders gives full rows; the tickets call is
            // a lightweight "just the IDs" variant for when you only need to know what's open.
            Sec("3. POSITIONS_AND_ORDERS");

            var opened = await svc.OpenedOrdersAsync(ct: ct);
            Row("OpenedOrders", $"{opened.OrderInfos.Count} order(s)");
            var tickets = await svc.OpenedOrdersTicketsAsync(ct: ct);
            Row("OpenedOrderTickets", $"{tickets.Tickets.Count}: [{string.Join(", ", tickets.Tickets)}]");
            var hist = await svc.OrdersHistoryAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, ct: ct);
            Row("OrdersHistory(7d)", $"{hist.OrdersInfo.Count} order(s)");

            // ---- Trading (described, not executed) ----
            // Read-only demo: we don't send orders here, just show the shape of the trading calls.
            // The orchestrators/sugar scenario demonstrate the real send/modify/close flow (guarded).
            Sec("4. TRADING (described - not executed)");
            Console.WriteLine("  OrderSendAsync / OrderModifyAsync / OrderCloseDeleteAsync / OrderCloseByAsync");
            Console.WriteLine("  Build an OrderSendRequest { Symbol, OperationType=OcOpBuy/OcOpSell, Volume, Price, Stoploss, Takeprofit } and call svc.OrderSendAsync(req).");

            // ---- Subscriptions ----
            // Live streams. Each one is sampled for 3 seconds and we just count events — a long-running
            // app would instead keep consuming the stream and react to each message.
            Sec("5. SUBSCRIPTIONS (3s samples)");
            await SampleAsync(c => svc.OnSymbolTickAsync(new[] { symbol }, c), "OnSymbolTick", 3, ct);
            await SampleAsync(c => svc.OnTradeAsync(c), "OnTrade", 3, ct);
            await SampleAsync(c => svc.OnOpenedOrdersProfitAsync(1000, c), "OnOpenedOrdersProfit", 3, ct);
            await SampleAsync(c => svc.OnOpenedOrdersTicketsAsync(1000, c), "OnOpenedOrdersTickets", 3, ct);
            await SampleAsync(c => svc.OnConnectStateAsync(c), "OnConnectState", 3, ct);

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
