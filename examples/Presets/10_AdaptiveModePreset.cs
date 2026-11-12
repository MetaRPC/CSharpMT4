/*══════════════════════════════════════════════════════════════════════════════
 FILE: Examples/Presets/10_AdaptiveModePreset.cs - ADAPTIVE MODE PRESET
 PURPOSE:
   A preset that reads live market conditions and dispatches to the orchestrator
   that best fits them, behind a floating-loss safety cap. It combines the two
   MT4 orchestrators into one adaptive system.

 🎯 WHO SHOULD USE THIS:
   • Anyone wanting one entry point that picks a strategy by market state
   • Learning how a preset composes orchestrators + a risk guard
   • A template for condition-driven strategy selection

 📚 WHAT THIS DEMO COVERS (3 Steps):

   1. SAFETY CAP
      • GetTotalProfitLossAsync() - abort the whole cycle if floating loss is
        already worse than MaxFloatingLoss (veto before any analysis)

   2. ANALYZE
      • GetSymbolSnapshotAsync() - spread as a cheap volatility proxy
        (tight spread → CALM, wide spread → VOLATILE)

   3. DISPATCH
      • CALM     → ScalpOrchestrator         (quick in/out, fixed TP)
      • VOLATILE → TrailingStopOrchestrator   (ride the move, trimmed risk)

   Configurable: Symbol, CalmSpreadPoints, RiskMoney, MaxFloatingLoss, Live.

 ℹ️  SAFE by default: Live=false only DESCRIBES the chosen orchestrator's plan.
     Live flows straight through, so the preset is exactly as guarded as the
     orchestrator it runs.

 📖 RELATED EXAMPLES:
   • 6_ScalpOrchestrator.cs / 7_TrailingStopOrchestrator.cs - the two strategies
   • 8_GridOrchestrator.cs, 9_BreakoutOrchestrator.cs - other orchestrators

 USAGE:
   Run `dotnet run` and choose [10] from the menu
   (or: dotnet run -- --example=10)
══════════════════════════════════════════════════════════════════════════════*/

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

            #region STEP 1 — SAFETY CAP
            // ══════════════════════════════════════════════════════════════
            // 1. SAFETY CAP (runs before any analysis, so it can veto the cycle)
            //    Read the account's total floating P/L; if we are already down
            //    more than MaxFloatingLoss, stand down rather than pile new risk
            //    onto a losing book.
            // ══════════════════════════════════════════════════════════════
            var floating = await _svc.GetTotalProfitLossAsync(ct: ct);
            Console.WriteLine($"  floating P/L = {floating} (cap {MaxFloatingLoss})");
            if (floating < MaxFloatingLoss)
            {
                Console.WriteLine("  safety cap breached — standing down, no strategy launched.");
                return;
            }

            #endregion

            #region STEP 2 — ANALYZE
            // ══════════════════════════════════════════════════════════════
            // 2. ANALYZE — pick a strategy from a cheap volatility read.
            //    Spread widens when the market gets fast/illiquid, so a tight
            //    spread ~ calm, a wide spread ~ volatile. (A stand-in; a
            //    production preset would measure historical bar ranges.)
            // ══════════════════════════════════════════════════════════════
            var snap = await _svc.GetSymbolSnapshotAsync(Symbol, ct: ct);
            bool calm = snap.SpreadPoints <= CalmSpreadPoints;
            Console.WriteLine($"  {Symbol} spread={snap.SpreadPoints:F1}pt -> {(calm ? "CALM" : "VOLATILE")}");

            #endregion

            #region STEP 3 — DISPATCH
            // ══════════════════════════════════════════════════════════════
            // 3. DISPATCH — the preset owns the DECISION; each orchestrator owns
            //    the EXECUTION. Build the chosen orchestrator with conditions-
            //    tuned parameters and hand off. Live flows straight through.
            // ══════════════════════════════════════════════════════════════
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
            #endregion
        }
    }
}
