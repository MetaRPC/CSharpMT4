using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mt4_term_api;

namespace MetaRPC.CSharpMT4.Examples.Orchestrators
{
    /// <summary>
    /// ORCHESTRATOR: a complete, parametrized scalping strategy built on <see cref="MT4Sugar"/>.
    /// Opens one risk-sized market order with a protective SL, holds it for <see cref="MaxHoldSeconds"/>,
    /// and force-closes whatever is still open. The class holds only the strategy logic; parameters are
    /// set by the caller (see <see cref="ScalpOrchestrator(MT4Service)"/> and the public properties).
    /// <para>Guarded: with <see cref="Live"/> = false (default) it prints the plan and sends nothing — safe on a shared account.</para>
    /// </summary>
    public sealed class ScalpOrchestrator
    {
        private readonly MT4Service _svc;

        /// <summary>Trading symbol.</summary>
        public string Symbol { get; set; } = "EURUSD";
        /// <summary>Direction: true = BUY, false = SELL.</summary>
        public bool IsBuy { get; set; } = true;
        /// <summary>Money (deposit currency) risked over the stop distance.</summary>
        public double RiskMoney { get; set; } = 20.0;
        /// <summary>Stop distance in points (also drives risk-based sizing).</summary>
        public double StopPoints { get; set; } = 150;
        /// <summary>Take-profit distance in points (attached after entry).</summary>
        public double TakeProfitPoints { get; set; } = 300;
        /// <summary>Maximum hold time before the position is force-closed.</summary>
        public int MaxHoldSeconds { get; set; } = 30;
        /// <summary>When false (default) the strategy is described but no orders are sent.</summary>
        public bool Live { get; set; } = false;

        /// <summary>Creates the orchestrator over a connected service.</summary>
        public ScalpOrchestrator(MT4Service svc) => _svc = svc;

        /// <summary>Runs the full open → hold → flatten cycle. Returns the ticket opened, or 0 when nothing was sent.</summary>
        public async Task<int> ExecuteAsync(CancellationToken ct = default)
        {
            Console.WriteLine("\n+== SCALP ORCHESTRATOR =========================================+");

            // 1) Read the market and size the position by risk.
            //    Instead of hard-coding a lot size, we ask: "how many lots make a move of
            //    StopPoints against us cost exactly RiskMoney?" That keeps the money at risk
            //    constant no matter the symbol's tick value or the current price.
            var snap = await _svc.GetSymbolSnapshotAsync(Symbol, ct: ct);
            var vol = await _svc.CalcVolumeForRiskAsync(Symbol, StopPoints, RiskMoney, ct: ct);
            Console.WriteLine($"  {Symbol} {(IsBuy ? "BUY" : "SELL")}  risk=${RiskMoney} over {StopPoints}pt -> {vol} lot(s)");
            Console.WriteLine($"  SL={StopPoints}pt  TP={TakeProfitPoints}pt  maxHold={MaxHoldSeconds}s  spread={snap.SpreadPoints:F1}pt");

            // 2) Safety guard. In "described" mode we print the plan and send nothing — this is
            //    what lets the example run against a shared/demo account without touching it.
            if (!Live)
            {
                Console.WriteLine("  [described] would: open-by-risk -> attach TP -> hold -> force-close. Set Live=true to run.");
                return 0;
            }

            // 3) Enter the market. The by-risk helper opens the order AND attaches the protective
            //    stop-loss in one call, so we are never exposed without an SL. Ticket <= 0 means
            //    the broker rejected it (bad price, closed market, not enough money, ...).
            var open = IsBuy
                ? await _svc.BuyMarketByRiskAsync(Symbol, StopPoints, RiskMoney, comment: "Scalp", ct: ct)
                : await _svc.SellMarketByRiskAsync(Symbol, StopPoints, RiskMoney, comment: "Scalp", ct: ct);
            if (open.Ticket <= 0) { Console.WriteLine("  order rejected"); return 0; }
            Console.WriteLine($"  opened #{open.Ticket} vol={open.Volume} @ {open.Price}");

            // 4) Attach the take-profit. We translate "TakeProfitPoints away, in the profitable
            //    direction" into an absolute price (sign flips for SELL) and modify the live order.
            var tp = await _svc.PriceFromOffsetPointsAsync(Symbol, IsBuy, IsBuy ? TakeProfitPoints : -TakeProfitPoints, ct: ct);
            await _svc.ModifySlTpAsync(open.Ticket, tp: tp, ct: ct);
            Console.WriteLine($"  TP attached @ {tp}");

            // 5) Hold. Scalping is short-lived: we wait for the SL/TP to resolve the trade, but
            //    cap the wait at MaxHoldSeconds so a flat market never leaves the position open.
            await HoldUntilClosedOrTimeoutAsync(open.Ticket, ct);

            // 6) Clean up. If the hold timed out while still open, flatten it now so we never
            //    walk away with an unmanaged position.
            var swept = await _svc.CloseAllMarketAsync(Symbol, ct: ct);
            Console.WriteLine($"  flatten: closed {swept} remaining market order(s)");
            return open.Ticket;
        }

        /// <summary>
        /// Polls the open-orders list until our ticket disappears (SL or TP fired) or the hold
        /// window expires. Returning here simply means "stop waiting"; the caller flattens whatever
        /// is left. We detect closure by absence of the ticket rather than by watching the price.
        /// </summary>
        private async Task HoldUntilClosedOrTimeoutAsync(int ticket, CancellationToken ct)
        {
            var deadline = DateTime.UtcNow.AddSeconds(MaxHoldSeconds);
            while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
            {
                // If the broker closed the order (hit SL/TP), it is gone from the open list — we're done.
                var tickets = await _svc.OpenedOrdersTicketsAsync(ct: ct);
                if (!tickets.Tickets.Contains(ticket))
                {
                    Console.WriteLine($"  #{ticket} hit SL/TP and closed on its own");
                    return;
                }
                // Otherwise wait a couple of seconds and check again. Cancellation ends the hold cleanly.
                try { await Task.Delay(TimeSpan.FromSeconds(2), ct); } catch (OperationCanceledException) { return; }
            }
            Console.WriteLine($"  max hold reached — forcing close of #{ticket}");
        }
    }
}
