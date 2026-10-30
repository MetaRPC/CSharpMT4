using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mt4_term_api;

namespace MetaRPC.CSharpMT4.Examples.Orchestrators
{
    /// <summary>
    /// ORCHESTRATOR: a risk-sized market entry protected by a trailing stop, built on <see cref="MT4Sugar"/>.
    /// Opens one order, then every poll interval ratchets the stop-loss to lock in gains once price has moved
    /// at least <see cref="TrailStartPoints"/> in favor, keeping it <see cref="TrailDistancePoints"/> behind price.
    /// Exits when the market takes out the trailing stop or <see cref="MaxHoldSeconds"/> elapses.
    /// <para>Guarded: with <see cref="Live"/> = false (default) it describes the plan and sends nothing.</para>
    /// </summary>
    public sealed class TrailingStopOrchestrator
    {
        private readonly MT4Service _svc;

        /// <summary>Trading symbol.</summary>
        public string Symbol { get; set; } = "EURUSD";
        /// <summary>Direction: true = BUY (trails stop up), false = SELL (trails stop down).</summary>
        public bool IsBuy { get; set; } = true;
        /// <summary>Money (deposit currency) risked over the initial stop distance.</summary>
        public double RiskMoney { get; set; } = 20.0;
        /// <summary>Initial stop distance in points (also drives risk-based sizing).</summary>
        public double StopPoints { get; set; } = 200;
        /// <summary>Profit (points) required before the stop starts trailing.</summary>
        public double TrailStartPoints { get; set; } = 150;
        /// <summary>Distance (points) the trailing stop keeps behind the current price.</summary>
        public double TrailDistancePoints { get; set; } = 100;
        /// <summary>Maximum hold time before the position is force-closed.</summary>
        public int MaxHoldSeconds { get; set; } = 60;
        /// <summary>Seconds between trailing checks.</summary>
        public int PollSeconds { get; set; } = 3;
        /// <summary>When false (default) the strategy is described but no orders are sent.</summary>
        public bool Live { get; set; } = false;

        /// <summary>Creates the orchestrator over a connected service.</summary>
        public TrailingStopOrchestrator(MT4Service svc) => _svc = svc;

        /// <summary>Runs the full open → trail → exit cycle. Returns the ticket opened, or 0 when nothing was sent.</summary>
        public async Task<int> ExecuteAsync(CancellationToken ct = default)
        {
            Console.WriteLine("\n+== TRAILING-STOP ORCHESTRATOR =================================+");

            // 1) Size by risk (same idea as Scalp): lots are derived so the initial stop costs RiskMoney.
            var vol = await _svc.CalcVolumeForRiskAsync(Symbol, StopPoints, RiskMoney, ct: ct);
            Console.WriteLine($"  {Symbol} {(IsBuy ? "BUY" : "SELL")}  risk=${RiskMoney} over {StopPoints}pt -> {vol} lot(s)");
            Console.WriteLine($"  trail: start after {TrailStartPoints}pt, keep {TrailDistancePoints}pt behind; maxHold={MaxHoldSeconds}s");

            // 2) Safety guard — described mode sends nothing.
            if (!Live)
            {
                Console.WriteLine("  [described] would: open-by-risk -> poll quote -> ratchet SL via ModifySlTp -> exit. Set Live=true to run.");
                return 0;
            }

            // 3) Open the position with its initial protective stop already attached.
            var open = IsBuy
                ? await _svc.BuyMarketByRiskAsync(Symbol, StopPoints, RiskMoney, comment: "Trail", ct: ct)
                : await _svc.SellMarketByRiskAsync(Symbol, StopPoints, RiskMoney, comment: "Trail", ct: ct);
            if (open.Ticket <= 0) { Console.WriteLine("  order rejected"); return 0; }
            var entry = open.Price;
            var point = await _svc.GetPointAsync(Symbol, ct: ct);   // price value of 1 point, for points<->price math
            Console.WriteLine($"  opened #{open.Ticket} vol={open.Volume} @ {entry}");

            // bestStop = the tightest stop we have set so far. It starts at the initial SL and only ever
            // moves toward price (up for BUY, down for SELL) — it never loosens. Tracking it locally lets
            // us decide whether a new candidate stop is actually an improvement before calling the server.
            double bestStop = IsBuy ? entry - StopPoints * point : entry + StopPoints * point;
            var deadline = DateTime.UtcNow.AddSeconds(MaxHoldSeconds);

            // 4) Trailing loop: watch price and ratchet the stop until we exit or time out.
            while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
            {
                // If the ticket is gone, the market already took out our stop — the trade is over.
                var tickets = await _svc.OpenedOrdersTicketsAsync(ct: ct);
                if (!tickets.Tickets.Contains(open.Ticket)) { Console.WriteLine($"  #{open.Ticket} stopped out / closed"); return open.Ticket; }

                // Measure how far price has moved in our favor, in points. For BUY we track the Bid
                // (the price we would exit at); for SELL we track the Ask.
                var q = await _svc.QuoteAsync(Symbol, ct: ct);
                double price = IsBuy ? q.Bid : q.Ask;
                double profitPoints = IsBuy ? (price - entry) / point : (entry - price) / point;

                // Only start trailing once we are TrailStartPoints in profit — below that we leave the
                // original stop alone so normal noise doesn't stop us out prematurely.
                if (profitPoints >= TrailStartPoints)
                {
                    // Candidate stop = TrailDistancePoints behind the current price.
                    double candidate = IsBuy ? price - TrailDistancePoints * point : price + TrailDistancePoints * point;
                    // Move the stop only if the candidate is tighter than what we already have (locks gains, never gives them back).
                    bool improves = IsBuy ? candidate > bestStop : candidate < bestStop;
                    if (improves)
                    {
                        bestStop = await _svc.NormalizePriceAsync(Symbol, candidate, ct: ct);   // round to the symbol's tick
                        await _svc.ModifySlTpAsync(open.Ticket, sl: bestStop, ct: ct);
                        Console.WriteLine($"  +{profitPoints:F0}pt — trail SL -> {bestStop}");
                    }
                }

                // Wait before the next check. Faster polling reacts sooner but adds load; cancellation ends the loop.
                try { await Task.Delay(TimeSpan.FromSeconds(PollSeconds), ct); } catch (OperationCanceledException) { break; }
            }

            // 5) Timed out while still open → flatten so nothing is left running.
            var swept = await _svc.CloseAllMarketAsync(Symbol, ct: ct);
            Console.WriteLine($"  max hold reached — flatten: closed {swept} remaining market order(s)");
            return open.Ticket;
        }
    }
}
