using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mt4_term_api;

namespace MetaRPC.CSharpMT4.Examples.Orchestrators
{
    /// <summary>
    /// ORCHESTRATOR: a **breakout straddle** — a buy-stop <see cref="DistancePoints"/> above Ask and a
    /// sell-stop the same distance below Bid, each with SL/TP. Whichever side the market breaks fills;
    /// the orchestrator then cancels the opposite pending. On timeout it clears both.
    /// <para>Guarded: with <see cref="Live"/> = false (default) it describes the plan and sends nothing.</para>
    /// </summary>
    public sealed class BreakoutOrchestrator
    {
        private readonly MT4Service _svc;

        /// <summary>Trading symbol.</summary>
        public string Symbol { get; set; } = "EURUSD";
        /// <summary>How far from the market to place each stop, in points.</summary>
        public double DistancePoints { get; set; } = 200;
        /// <summary>Stop-loss distance from the entry, in points.</summary>
        public double StopLossPoints { get; set; } = 200;
        /// <summary>Take-profit distance from the entry, in points.</summary>
        public double TakeProfitPoints { get; set; } = 400;
        /// <summary>Volume per side (auto-normalized).</summary>
        public double Volume { get; set; } = 0.01;
        /// <summary>Seconds to wait for a breakout before flattening.</summary>
        public int MaxWaitSeconds { get; set; } = 60;
        /// <summary>Seconds between trigger checks.</summary>
        public int PollSeconds { get; set; } = 3;
        /// <summary>Magic number tag.</summary>
        public int Magic { get; set; } = 4002;
        /// <summary>When false (default) the plan is described but no orders are sent.</summary>
        public bool Live { get; set; } = false;

        /// <summary>Creates the orchestrator over a connected service.</summary>
        public BreakoutOrchestrator(MT4Service svc) => _svc = svc;

        /// <summary>Places the straddle (or describes it), waits for a breakout, cancels the loser / flattens.</summary>
        public async Task ExecuteAsync(CancellationToken ct = default)
        {
            Console.WriteLine("\n+== BREAKOUT ORCHESTRATOR =====================================+");

            // 1) Snapshot, normalize volume, and compute the two trigger prices. We don't know which
            //    way the market will break, so we arm BOTH sides: a BUY_STOP DistancePoints above Ask
            //    and a SELL_STOP the same distance below Bid. (pt = price value of one point, used to
            //    turn the SL/TP point distances into absolute prices below.)
            var snap = await _svc.GetSymbolSnapshotAsync(Symbol, ct: ct);
            var vol = await _svc.NormalizeVolumeAsync(Symbol, Volume, ct: ct);
            var pt = snap.Point;
            double buyStop = await _svc.PriceFromOffsetPointsAsync(Symbol, fromAsk: true, offsetPoints: DistancePoints, ct: ct);
            double sellStop = await _svc.PriceFromOffsetPointsAsync(Symbol, fromAsk: false, offsetPoints: -DistancePoints, ct: ct);
            Console.WriteLine($"  {Symbol} bid={snap.Bid} ask={snap.Ask}  BUY_STOP @ {buyStop} / SELL_STOP @ {sellStop}  vol={vol}  SL={StopLossPoints}pt TP={TakeProfitPoints}pt");

            // 2) Safety guard — described mode sends nothing.
            if (!Live)
            {
                Console.WriteLine("  [described] would: place both stops (OcOpBuystop / OcOpSellstop) with SL/TP,");
                Console.WriteLine("  then poll for a fill, cancel the opposite pending, and flatten on timeout. Set Live=true to run.");
                return;
            }

            // 3) Place the straddle. Each stop carries its own SL/TP (SL behind the entry, TP ahead of it),
            //    so once it fills the position is already fully protected — no second call needed.
            var buyTicket  = await SendStopAsync(OrderSendOperationType.OcOpBuystop,  buyStop,  buyStop - StopLossPoints * pt, buyStop + TakeProfitPoints * pt, vol, ct);
            var sellTicket = await SendStopAsync(OrderSendOperationType.OcOpSellstop, sellStop, sellStop + StopLossPoints * pt, sellStop - TakeProfitPoints * pt, vol, ct);
            Console.WriteLine($"  placed buy-stop #{buyTicket}, sell-stop #{sellTicket}");

            // 4) Wait for the break. We poll the open orders and look for a *market* order on our symbol
            //    (OoOpBuy/OoOpSell) — a stop that has triggered becomes a live position of that type.
            var deadline = DateTime.UtcNow.AddSeconds(MaxWaitSeconds);
            while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
            {
                var opened = await _svc.OpenedOrdersAsync(ct: ct);
                var market = opened.OrderInfos.FirstOrDefault(o => o.Symbol == Symbol &&
                    (o.OrderType == OpenedOrderType.OoOpBuy || o.OrderType == OpenedOrderType.OoOpSell));
                if (market != null)
                {
                    // One side broke and filled. Cancel the opposite stop so we aren't left holding both
                    // sides, then let the winning position run to the SL/TP it already carries.
                    Console.WriteLine($"  breakout filled: #{market.Ticket} {market.OrderType} @ {market.OpenPrice}");
                    var cancelled = await _svc.CancelAllPendingAsync(Symbol, ct: ct);   // drop the opposite stop
                    Console.WriteLine($"  cancelled {cancelled} opposite pending; position runs to its SL/TP.");
                    return;
                }
                try { await Task.Delay(TimeSpan.FromSeconds(PollSeconds), ct); } catch (OperationCanceledException) { break; }
            }

            // 5) Nothing broke in time → tear the straddle down so no stray stops stay armed.
            var swept = await _svc.CancelAllPendingAsync(Symbol, ct: ct);
            Console.WriteLine($"  no breakout within {MaxWaitSeconds}s — cancelled {swept} pending order(s)");
        }

        /// <summary>
        /// Builds and sends one pending stop order with SL/TP attached. All three prices are rounded to
        /// the symbol's tick (<see cref="MT4Service.NormalizePriceAsync"/>) first, because the broker
        /// rejects prices that don't sit on a valid tick. Returns the broker ticket (&gt; 0 on success).
        /// </summary>
        private async Task<int> SendStopAsync(OrderSendOperationType op, double price, double sl, double tp, double vol, CancellationToken ct)
        {
            var req = new OrderSendRequest
            {
                Symbol = Symbol,
                OperationType = op,          // OcOpBuystop or OcOpSellstop — the trigger side
                Volume = vol,
                Price = await _svc.NormalizePriceAsync(Symbol, price, ct: ct),        // trigger price
                Stoploss = await _svc.NormalizePriceAsync(Symbol, sl, ct: ct),        // travels into the position on fill
                Takeprofit = await _svc.NormalizePriceAsync(Symbol, tp, ct: ct),      // travels into the position on fill
                Slippage = 5,
                MagicNumber = Magic,
                Comment = "Breakout",
            };
            var r = await _svc.OrderSendAsync(req, ct: ct);
            return r.Ticket;
        }
    }
}
