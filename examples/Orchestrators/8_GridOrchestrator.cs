using System;
using System.Threading;
using System.Threading.Tasks;
using mt4_term_api;

namespace MetaRPC.CSharpMT4.Examples.Orchestrators
{
    /// <summary>
    /// ORCHESTRATOR: a symmetric pending-order **grid** around the current price — buy-limits below Ask
    /// and sell-limits above Bid, <see cref="LevelsPerSide"/> each at <see cref="SpacingPoints"/> spacing.
    /// Classic range/mean-reversion scaffold; here it just places the grid, then (demo) sweeps the pendings.
    /// <para>Guarded: with <see cref="Live"/> = false (default) it prints the grid and sends nothing.</para>
    /// </summary>
    public sealed class GridOrchestrator
    {
        private readonly MT4Service _svc;

        /// <summary>Trading symbol.</summary>
        public string Symbol { get; set; } = "EURUSD";
        /// <summary>Number of pending levels on each side.</summary>
        public int LevelsPerSide { get; set; } = 3;
        /// <summary>Spacing between levels, in points.</summary>
        public double SpacingPoints { get; set; } = 150;
        /// <summary>Volume per pending order (auto-normalized).</summary>
        public double Volume { get; set; } = 0.01;
        /// <summary>Magic number tag for the grid.</summary>
        public int Magic { get; set; } = 4001;
        /// <summary>When false (default) the grid is described but no orders are sent.</summary>
        public bool Live { get; set; } = false;

        /// <summary>Creates the orchestrator over a connected service.</summary>
        public GridOrchestrator(MT4Service svc) => _svc = svc;

        /// <summary>Places the grid (or describes it), then sweeps the pendings in demo mode.</summary>
        public async Task ExecuteAsync(CancellationToken ct = default)
        {
            Console.WriteLine("\n+== GRID ORCHESTRATOR =========================================+");

            // 1) Snapshot the market and normalize the per-order volume to the symbol's lot step
            //    (e.g. 0.013 -> 0.01) so every pending order we build is valid.
            var snap = await _svc.GetSymbolSnapshotAsync(Symbol, ct: ct);
            var vol = await _svc.NormalizeVolumeAsync(Symbol, Volume, ct: ct);
            Console.WriteLine($"  {Symbol} bid={snap.Bid} ask={snap.Ask}  {LevelsPerSide} level(s)/side @ {SpacingPoints}pt, vol={vol}");

            // 2) Described mode: compute and print every level's price, but send nothing.
            //    The grid is symmetric — BUY_LIMITs sit *below* Ask (negative offset) and SELL_LIMITs
            //    *above* Bid (positive offset), each level one more SpacingPoints further out.
            if (!Live)
            {
                for (int i = 1; i <= LevelsPerSide; i++)
                {
                    var buy = await _svc.PriceFromOffsetPointsAsync(Symbol, fromAsk: true,  offsetPoints: -i * SpacingPoints, ct: ct);
                    var sell = await _svc.PriceFromOffsetPointsAsync(Symbol, fromAsk: false, offsetPoints:  i * SpacingPoints, ct: ct);
                    Console.WriteLine($"  [described] L{i}: BUY_LIMIT @ {buy}   SELL_LIMIT @ {sell}");
                }
                Console.WriteLine("  Set Live=true to place the grid (OcOpBuylimit / OcOpSelllimit), then CancelAllPendingAsync to clear.");
                return;
            }

            // 3) Live: place the real grid. Same price math as above, one BUY_LIMIT and one SELL_LIMIT per level.
            int placed = 0;
            for (int i = 1; i <= LevelsPerSide; i++)
            {
                var buyPrice = await _svc.PriceFromOffsetPointsAsync(Symbol, fromAsk: true, offsetPoints: -i * SpacingPoints, ct: ct);
                placed += await SendPendingAsync(OrderSendOperationType.OcOpBuylimit, buyPrice, vol, ct);

                var sellPrice = await _svc.PriceFromOffsetPointsAsync(Symbol, fromAsk: false, offsetPoints: i * SpacingPoints, ct: ct);
                placed += await SendPendingAsync(OrderSendOperationType.OcOpSelllimit, sellPrice, vol, ct);
            }
            Console.WriteLine($"  placed {placed} pending order(s)");

            // 4) Demo cleanup: immediately cancel the grid we just set so the example leaves no
            //    working orders behind. A real grid would *keep* these and manage fills instead.
            var cancelled = await _svc.CancelAllPendingAsync(Symbol, ct: ct);
            Console.WriteLine($"  swept {cancelled} pending order(s)");
        }

        /// <summary>
        /// Builds and sends one pending order. Sugar has no pending-order helper, so we construct the
        /// raw <see cref="OrderSendRequest"/> ourselves and set its pending <c>OperationType</c>
        /// (OcOpBuylimit / OcOpSelllimit). Returns 1 if the broker accepted it (Ticket &gt; 0), else 0.
        /// </summary>
        private async Task<int> SendPendingAsync(OrderSendOperationType op, double price, double vol, CancellationToken ct)
        {
            var req = new OrderSendRequest
            {
                Symbol = Symbol,
                OperationType = op,          // the pending type decides the side + that it's a limit order
                Volume = vol,
                Price = price,               // the level the limit order waits at
                Slippage = 5,
                MagicNumber = Magic,         // tags the order as ours, so we can find/manage it later
                Comment = "Grid",
            };
            var r = await _svc.OrderSendAsync(req, ct: ct);
            if (r.Ticket > 0) { Console.WriteLine($"    #{r.Ticket} {op} @ {price}"); return 1; }
            Console.WriteLine($"    rejected {op} @ {price}");   // e.g. level too close to market
            return 0;
        }
    }
}
