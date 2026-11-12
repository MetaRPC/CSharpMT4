/*══════════════════════════════════════════════════════════════════════════════
 FILE: Examples/UserCode/11_ProgramUserCode.cs - USER CODE SANDBOX
 PURPOSE:
   A ready-to-edit sandbox for your own MT4 logic. The connection and a typed
   MT4Service (svc) are already wired up — you just fill in the marked block.

 🎯 WHO SHOULD USE THIS:
   • Anyone prototyping custom MT4 logic without connection boilerplate
   • Trying out MT4Service / MT4Sugar calls against a live connection
   • Building a quick throwaway script on top of the examples

 📚 WHAT THIS DEMO COVERS:
   • What's ready: a connected MT4Service (svc) + the whole MT4Sugar layer as
     extension methods (snapshots, risk sizing, market orders, SL/TP, bulk
     close, tick/trade streams), plus the base symbol from appsettings.
   • Where to write: the "YOUR CODE STARTS HERE" block in RunAsync().
   • Read-only starters run live; order-sending calls are left commented out.
   • RunStandaloneAsync() — opens its own connection, runs, then disconnects.

 ⚠️  Read-only calls are safe. Anything that sends orders is commented out on
     purpose (the demo account may be shared) — uncomment deliberately.

 📖 RELATED EXAMPLES:
   • MT4Service.cs — every MT4 proto method, grouped by canonical API area
   • MT4Sugar.cs   — high-level convenience helpers (what the block below uses)
   • 1_LowLevel.cs (1-3), 4_ServiceExamples.cs (4), 5_SugarScenario.cs (5),
     Orchestrators (6-9), 10_AdaptiveModePreset.cs (10)

 USAGE:
   Run `dotnet run` and choose [11] from the menu
   (or: dotnet run -- --example=11)
══════════════════════════════════════════════════════════════════════════════*/

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MetaRPC.CSharpMT4.Helpers;
using mt4_term_api;

namespace MetaRPC.CSharpMT4.Examples.UserCode
{
    /// <summary>
    /// A ready-to-edit sandbox for custom trading logic. The connection and <see cref="MT4Service"/>
    /// are already wired up — you just fill in <see cref="RunAsync"/>.
    /// </summary>
    public static class ProgramUserCode
    {
        /// <summary>
        /// Your sandbox. Runs against an already-connected <paramref name="svc"/> (the menu passes
        /// the shared connection). Write your logic in the marked block below.
        /// </summary>
        /// <param name="svc">A connected MT4Service.</param>
        /// <param name="symbol">Base symbol from configuration (e.g. "EURUSD").</param>
        /// <param name="ct">Cancellation token (Ctrl+C aware).</param>
        public static async Task RunAsync(MT4Service svc, string symbol = "EURUSD", CancellationToken ct = default)
        {
            Console.WriteLine("================= USER CODE SANDBOX =================");
            Console.WriteLine($"Connected. MT4Service is ready. Base symbol: {symbol}\n");

            #region YOUR CODE
            // ══════════════════════════════════════════════════════════════
            // YOUR CODE STARTS HERE ↓
            // ══════════════════════════════════════════════════════════════

            // --- Read-only starters (safe) ---------------------------------------
            var account = await svc.GetAccountSnapshotAsync(ct: ct);
            Console.WriteLine($"Account: {account}");

            var sym = await svc.GetSymbolSnapshotAsync(symbol, ct: ct);
            Console.WriteLine($"Symbol:  {sym}");

            var openOrders = await svc.GetOrderCountAsync(ct: ct);
            var floating = await svc.GetTotalProfitLossAsync(ct: ct);
            Console.WriteLine($"Open orders: {openOrders}, floating P/L: {floating}");

            // --- Sizing math (no orders) -----------------------------------------
            // var volume = await svc.CalcVolumeForRiskAsync(symbol, stopPoints: 200, riskMoney: 50, ct: ct);
            // var slPrice = await svc.PriceFromOffsetPointsAsync(symbol, fromAsk: true, offsetPoints: -200, ct: ct);
            // Console.WriteLine($"Risk-sized volume: {volume}, SL price: {slPrice}");

            // --- Trading (UNCOMMENT DELIBERATELY — places real orders) -----------
            // var buy = await svc.BuyMarketByRiskAsync(symbol, stopPoints: 200, riskMoney: 50, ct: ct);
            // Console.WriteLine($"Opened #{buy.Ticket} @ {buy.Price}");
            // await svc.ModifySlTpAsync(buy.Ticket, tp: sym.Ask + 400 * sym.Point, ct: ct);
            // await svc.CloseByTicketAsync(buy.Ticket, ct: ct);
            // await svc.CloseAllMarketAsync(symbol, ct: ct);

            // --- Live stream sample (3 seconds) ----------------------------------
            // using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            // cts.CancelAfter(TimeSpan.FromSeconds(3));
            // try { await foreach (var t in svc.ReadTicks(symbol, cts.Token)) Console.WriteLine($"tick {t.SymbolTick?.Bid}/{t.SymbolTick?.Ask}"); }
            // catch (OperationCanceledException) { }

            // ══════════════════════════════════════════════════════════════
            // YOUR CODE ENDS HERE ↑
            // ══════════════════════════════════════════════════════════════
            #endregion

            Console.WriteLine("\n================= SANDBOX DONE =================");
        }

        #region STANDALONE ENTRY POINT
        // ══════════════════════════════════════════════════════════════════
        // RunStandaloneAsync() - run the sandbox OUTSIDE the menu
        //    Opens its own connection via ConnectionHelper, runs RunAsync(),
        //    then disconnects. Handy for a quick one-off script.
        // ══════════════════════════════════════════════════════════════════
        /// <summary>
        /// Standalone entry point: opens its own connection via <see cref="ConnectionHelper"/>, runs
        /// <see cref="RunAsync"/>, then disconnects. Use this to run the sandbox outside the menu.
        /// </summary>
        /// <param name="symbol">Base symbol (defaults to EURUSD).</param>
        /// <param name="ct">Cancellation token.</param>
        public static async Task RunStandaloneAsync(string symbol = "EURUSD", CancellationToken ct = default)
        {
            using var loggerFactory = LoggerFactory.Create(b => b.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; }));
            var logger = loggerFactory.CreateLogger<MT4Service>();

            var account = await ConnectionHelper.CreateAndConnectAccountAsync(logger, ct);
            try
            {
                var svc = new MT4Service(account);
                await RunAsync(svc, symbol, ct);
            }
            finally
            {
                await ConnectionHelper.DisconnectAsync(account);
            }
        }
        #endregion
    }
}
