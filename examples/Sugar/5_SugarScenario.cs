using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mt4_term_api;

namespace MetaRPC.CSharpMT4.Examples.Sugar
{
    /// <summary>
    /// SUGAR-LEVEL example: one end-to-end trading lifecycle built entirely from <see cref="MT4Sugar"/>
    /// convenience helpers. It runs as three meaningful actions, in order:
    ///   <para>① ASSESS  — read the account, the symbol and the open book (read-only).</para>
    ///   <para>② PLAN    — turn a risk budget into a broker-valid volume and SL/TP prices (pure math, no orders).</para>
    ///   <para>③ EXECUTE — place → modify → close that plan (guarded: only when <c>enableTrading</c> is true).</para>
    /// Each step feeds the next: ASSESS picks the symbol context, PLAN sizes the trade from it, EXECUTE acts on the plan.
    /// </summary>
    public static class SugarScenario
    {
        private static void Step(string title)
            => Console.WriteLine($"\n~~~~~~~~~~ {title} ~~~~~~~~~~");

        /// <summary>The sizing/levels produced by <see cref="PlanAsync"/> and consumed by <see cref="ExecuteAsync"/>.</summary>
        public sealed class TradePlan
        {
            /// <summary>Symbol the plan is for.</summary>
            public string Symbol = "";
            /// <summary>Stop distance in points used for both sizing and the SL price.</summary>
            public double StopPoints;
            /// <summary>Money (deposit currency) risked if the stop is hit.</summary>
            public double RiskMoney;
            /// <summary>Broker-valid volume (lots) sized to the risk budget.</summary>
            public double Volume;
            /// <summary>Absolute stop-loss price for a BUY (StopPoints below Ask).</summary>
            public double StopLoss;
            /// <summary>Absolute take-profit price for a BUY (2x StopPoints above Ask).</summary>
            public double TakeProfit;
        }

        /// <summary>Runs the three-step lifecycle end to end.</summary>
        /// <param name="svc">A connected MT4Service.</param>
        /// <param name="symbol">Symbol to work with (default EURUSD).</param>
        /// <param name="enableTrading">When false (default) step ③ only describes what it would do; no orders are sent.</param>
        /// <param name="ct">Cancellation token (also used to bound the live-tick sample).</param>
        public static async Task RunAsync(MT4Service svc, string symbol = "EURUSD", bool enableTrading = false, CancellationToken ct = default)
        {
            Console.WriteLine($"=== SUGAR SCENARIO: assess -> plan -> execute ({symbol}) ===");
            await AssessAsync(svc, symbol, ct);
            var plan = await PlanAsync(svc, symbol, stopPoints: 200, riskMoney: 50, ct);
            await ExecuteAsync(svc, plan, enableTrading, ct);
            Console.WriteLine("\n=== SUGAR SCENARIO DONE ===");
        }

        /// <summary>① ASSESS — one-glance view of account, symbol and the current open book, plus a short live-tick sample.</summary>
        public static async Task AssessAsync(MT4Service svc, string symbol, CancellationToken ct = default)
        {
            Step("① ASSESS (read-only)");

            // Snapshots: one compact object each for the account and the symbol. The sugar snapshot helpers
            // bundle several raw reads into a single ready-to-print value, so you don't juggle many calls.
            var acc = await svc.GetAccountSnapshotAsync(ct: ct);
            var sym = await svc.GetSymbolSnapshotAsync(symbol, ct: ct);
            Console.WriteLine($"Account: {acc}");
            Console.WriteLine($"Symbol:  {sym}");

            // Open book at a glance: how many orders, total lots, floating P/L, and the winners/losers split.
            // These sugar helpers do the filtering/summing for you over the raw open-orders list.
            var count = await svc.GetOrderCountAsync(ct: ct);
            var lots = await svc.TotalLotsAsync(ct: ct);
            var floating = await svc.GetTotalProfitLossAsync(ct: ct);
            var winners = await svc.GetProfitableOrdersAsync(ct: ct);
            var losers = await svc.GetLosingOrdersAsync(ct: ct);
            Console.WriteLine($"Open book: {count} order(s), {lots} lot(s), floating P/L={floating} ({winners.Count} up / {losers.Count} down)");

            // Drill down: orders on just this symbol, then look one up by its ticket.
            var onSymbol = await svc.GetOrdersBySymbolAsync(symbol, ct: ct);
            Console.WriteLine($"On {symbol}: {onSymbol.Count} order(s)");
            if (onSymbol.Count > 0)
            {
                var first = onSymbol[0];
                var byTicket = await svc.GetOrderByTicketAsync(first.Ticket, ct: ct);
                Console.WriteLine($"  lookup #{first.Ticket}: {(byTicket is null ? "not found" : $"{byTicket.OrderType} {byTicket.Lots}@{byTicket.OpenPrice} P/L={byTicket.Profit}")}");
            }

            // Recent closed history (last 7 days) + the single most recent closed order.
            var last = await svc.GetLastClosedOrderAsync(TimeSpan.FromDays(7), ct: ct);
            var hist = await svc.OrdersHistoryLastAsync(TimeSpan.FromDays(7), ct: ct);
            Console.WriteLine($"History(7d): {hist.OrdersInfo.Count} closed, last={(last is null ? "none" : last.Ticket.ToString())}");

            // Finally, a short live sample so ASSESS also shows the streaming side, not just one-shot reads.
            await SampleTicksAsync(svc, symbol, 3, ct);
            await SampleTradesAsync(svc, 3, ct);
        }

        /// <summary>② PLAN — convert a risk budget into a broker-valid volume and absolute SL/TP prices. No orders are sent.</summary>
        public static async Task<TradePlan> PlanAsync(MT4Service svc, string symbol, double stopPoints, double riskMoney, CancellationToken ct = default)
        {
            Step("② PLAN (pure math, no orders)");

            // Make sure the symbol is selected before we read its prices/params.
            await svc.EnsureSelectedAsync(symbol, ct: ct);

            // Turn the risk budget into a volume: how many lots make a stopPoints move cost riskMoney.
            var volume = await svc.CalcVolumeForRiskAsync(symbol, stopPoints, riskMoney, ct: ct);

            // Derive the absolute SL/TP prices from offsets around the Ask (BUY setup):
            //   SL = stopPoints below Ask, TP = 2x stopPoints above Ask (a 1:2 risk:reward).
            var sl = await svc.PriceFromOffsetPointsAsync(symbol, fromAsk: true, offsetPoints: -stopPoints, ct: ct);
            var tp = await svc.PriceFromOffsetPointsAsync(symbol, fromAsk: true, offsetPoints: 2 * stopPoints, ct: ct);
            var pips = await svc.PointsToPipsAsync(symbol, stopPoints, ct: ct);   // points->pips, just for display

            // Normalize to broker-valid values: volume onto the lot step, prices onto the tick grid.
            // Skipping this is the usual cause of "invalid volume / invalid price" rejections.
            var normVol = await svc.NormalizeVolumeAsync(symbol, volume, ct: ct);
            var normSl = await svc.NormalizePriceAsync(symbol, sl, ct: ct);

            var plan = new TradePlan
            {
                Symbol = symbol,
                StopPoints = stopPoints,
                RiskMoney = riskMoney,
                Volume = normVol,
                StopLoss = normSl,
                TakeProfit = await svc.NormalizePriceAsync(symbol, tp, ct: ct),
            };

            Console.WriteLine($"Risk {riskMoney} over {stopPoints}pt ({pips} pips) -> volume {plan.Volume} lot(s)");
            Console.WriteLine($"Levels: SL={plan.StopLoss}  TP={plan.TakeProfit}");
            return plan;
        }

        /// <summary>③ EXECUTE — place the plan, tighten its stops, then flatten. Guarded by <paramref name="enableTrading"/>.</summary>
        public static async Task ExecuteAsync(MT4Service svc, TradePlan plan, bool enableTrading, CancellationToken ct = default)
        {
            Step($"③ EXECUTE ({(enableTrading ? "LIVE" : "described - enable to run")})");

            if (!enableTrading)
            {
                Console.WriteLine("Would run, in order:");
                Console.WriteLine($"  1) BuyMarketByRiskAsync({plan.Symbol}, {plan.StopPoints}pt, {plan.RiskMoney}) — risk-sized entry with protective SL");
                Console.WriteLine($"  2) ModifySlTpAsync(ticket, tp={plan.TakeProfit}) — attach the take-profit from the plan");
                Console.WriteLine( "  3) CloseByTicketAsync(ticket) — then CloseAllMarketAsync / CancelAllPendingAsync to flatten");
                Console.WriteLine( "  alt entries: BuyMarketAsync / SellMarketAsync (absolute SL/TP), BuyMarketPointsAsync / SellMarketPointsAsync (SL/TP in points)");
                Console.WriteLine( "  alt close:   CloseAllAsync (every order, market + pending)");
                return;
            }

            // 1) risk-sized market entry (SL placed StopPoints below Ask by the helper)
            var open = await svc.BuyMarketByRiskAsync(plan.Symbol, plan.StopPoints, plan.RiskMoney, ct: ct);
            Console.WriteLine($"Opened #{open.Ticket} vol={open.Volume} @ {open.Price}");

            // 2) attach the planned take-profit
            var mod = await svc.ModifySlTpAsync(open.Ticket, tp: plan.TakeProfit, ct: ct);
            Console.WriteLine($"Modified #{open.Ticket}: TP -> {plan.TakeProfit} ({mod})");

            // 3) flatten: this ticket first, then sweep anything left on the symbol
            var closed = await svc.CloseByTicketAsync(open.Ticket, ct: ct);
            Console.WriteLine($"Closed #{open.Ticket} ({closed})");
            var market = await svc.CloseAllMarketAsync(plan.Symbol, ct: ct);
            var pending = await svc.CancelAllPendingAsync(plan.Symbol, ct: ct);
            Console.WriteLine($"Swept {plan.Symbol}: {market} market + {pending} pending");
        }

        private static async Task SampleTicksAsync(MT4Service svc, string symbol, int seconds, CancellationToken outer)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(outer);
            cts.CancelAfter(TimeSpan.FromSeconds(seconds));
            int n = 0;
            try
            {
                await foreach (var t in svc.ReadTicks(symbol, cts.Token))
                {
                    if (n == 0) Console.WriteLine($"Live tick: Bid={t.SymbolTick?.Bid} Ask={t.SymbolTick?.Ask}");
                    if (++n >= 3) break;
                }
            }
            catch (OperationCanceledException) { }
            Console.WriteLine($"ReadTicks: {n} event(s) in {seconds}s");
        }

        private static async Task SampleTradesAsync(MT4Service svc, int seconds, CancellationToken outer)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(outer);
            cts.CancelAfter(TimeSpan.FromSeconds(seconds));
            int n = 0;
            try { await foreach (var _ in svc.ReadTrades(cts.Token)) { if (++n >= 3) break; } }
            catch (OperationCanceledException) { }
            Console.WriteLine($"ReadTrades: {n} event(s) in {seconds}s");
        }
    }
}
