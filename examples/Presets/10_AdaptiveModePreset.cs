using System;
using System.Threading;
using System.Threading.Tasks;
using MetaRPC.CSharpMT4.Examples.Orchestrators;
using mt4_term_api;

namespace MetaRPC.CSharpMT4.Examples.Presets
{
    /// <summary>
    /// PRESET: an adaptive wrapper that reads live market conditions and dispatches to the orchestrator that
    /// best fits them, with a floating-loss safety cap. It combines the two MT4 orchestrators into one system:
    /// <para>• tight spread (calm market)  → <see cref="ScalpOrchestrator"/> — quick in/out with a fixed TP.</para>
    /// <para>• wide spread (volatile/trend) → <see cref="TrailingStopOrchestrator"/> — ride the move behind a trailing stop.</para>
    /// Spread is used as a simple, dependency-free volatility proxy; a production preset would use historical bars.
    /// <para>Guarded: <see cref="Live"/> flows down to the chosen orchestrator; false (default) only describes.</para>
    /// </summary>
    public sealed class AdaptiveModePreset
    {
        private readonly MT4Service _svc;

        /// <summary>Trading symbol.</summary>
        public string Symbol { get; set; } = "EURUSD";
        /// <summary>Spread (points) at or below which the market counts as calm → scalp.</summary>
        public double CalmSpreadPoints { get; set; } = 20;
        /// <summary>Base money risk per trade (deposit currency).</summary>
        public double RiskMoney { get; set; } = 20.0;
        /// <summary>Abort before trading if current floating loss is worse than this (negative number).</summary>
        public double MaxFloatingLoss { get; set; } = -100.0;
        /// <summary>When false (default) the chosen orchestrator only describes its plan.</summary>
        public bool Live { get; set; } = false;

        /// <summary>Creates the preset over a connected service.</summary>
        public AdaptiveModePreset(MT4Service svc) => _svc = svc;

        /// <summary>Analyzes the market, enforces the safety cap, then runs the orchestrator chosen for current conditions.</summary>
        public async Task ExecuteAsync(CancellationToken ct = default)
        {
            Console.WriteLine("\n+== ADAPTIVE MODE PRESET =======================================+");

            // STEP 1 — Safety cap (runs before any analysis, so it can veto the whole cycle).
            // Read the account's total floating P/L; if we are already down more than MaxFloatingLoss,
            // stand down rather than pile new risk onto a losing book.
            var floating = await _svc.GetTotalProfitLossAsync(ct: ct);
            Console.WriteLine($"  floating P/L = {floating} (cap {MaxFloatingLoss})");
            if (floating < MaxFloatingLoss)
            {
                Console.WriteLine("  safety cap breached — standing down, no strategy launched.");
                return;
            }

            // STEP 2 — Analyze. We need a cheap read on volatility to pick a strategy. The spread widens
            // when the market gets fast/illiquid, so a tight spread ~ calm, a wide spread ~ volatile.
            // (It's a stand-in; a production preset would measure historical bar ranges instead.)
            var snap = await _svc.GetSymbolSnapshotAsync(Symbol, ct: ct);
            bool calm = snap.SpreadPoints <= CalmSpreadPoints;
            Console.WriteLine($"  {Symbol} spread={snap.SpreadPoints:F1}pt -> {(calm ? "CALM" : "VOLATILE")}");

            // STEP 3 — Dispatch. The preset owns the *decision*; each orchestrator owns the *execution*.
            // We just build the chosen orchestrator with conditions-tuned parameters and hand off.
            // Live flows straight through, so the preset is as safe/guarded as the orchestrator it runs.
            if (calm)
            {
                // Calm → Scalp: get in and out fast with a fixed TP; no trailing needed in a quiet market.
                Console.WriteLine("  mode: SCALP (tight spread)");
                await new ScalpOrchestrator(_svc)
                {
                    Symbol = Symbol,
                    RiskMoney = RiskMoney,
                    StopPoints = 150,
                    TakeProfitPoints = 300,
                    MaxHoldSeconds = 30,
                    Live = Live,
                }.ExecuteAsync(ct);
            }
            else
            {
                // Volatile → Trailing: give the move room (wider stop) and ride it behind a trailing stop,
                // but cut the risk — a wide-spread market is where sizing hurts most.
                Console.WriteLine("  mode: TRAILING (wide spread — reduced risk)");
                await new TrailingStopOrchestrator(_svc)
                {
                    Symbol = Symbol,
                    RiskMoney = RiskMoney * 0.7,   // trim risk when it is choppy
                    StopPoints = 250,
                    TrailStartPoints = 150,
                    TrailDistancePoints = 100,
                    MaxHoldSeconds = 60,
                    Live = Live,
                }.ExecuteAsync(ct);
            }
        }
    }
}
