// ══════════════════════════════════════════════════════════════════════════════
// USER SANDBOX — write your own MT4 trading logic here.
//
// WHAT'S READY FOR YOU:
//   ✓ A connected MT4Service (svc) — the typed, data-returning wrapper.
//   ✓ The whole MT4Sugar layer as extension methods on svc (snapshots, risk sizing,
//     market orders, SL/TP, bulk close, tick/trade streams).
//   ✓ The base symbol from appsettings (passed in as `symbol`).
//
// HOW TO RUN:
//   • From the menu:            choose "9. USER CODE sandbox"
//   • Non-interactive:          dotnet run -- --example=9   (or --example=sandbox)
//   • Standalone (own connect):  call ProgramUserCode.RunStandaloneAsync()
//
// WHERE TO WRITE:
//   Put your code in the "YOUR CODE STARTS HERE" block of RunAsync below.
//   Read-only calls are safe; anything that sends orders is left commented out
//   because the demo account may be shared — uncomment deliberately.
//
// REFERENCE:
//   • MT4Service.cs — every MT4 proto method, grouped by canonical API area.
//   • MT4Sugar.cs   — high-level convenience helpers (what the examples below use).
//   • examples/     — LowLevel (1-3), Service (4), Sugar scenario (5),
//                     Orchestrators (6-7), Preset (8).
// ══════════════════════════════════════════════════════════════════════════════

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

            // ═════════════════════════════════════════════════════════════════
            // YOUR CODE STARTS HERE ↓
            // ═════════════════════════════════════════════════════════════════

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

            // ═════════════════════════════════════════════════════════════════
            // YOUR CODE ENDS HERE ↑
            // ═════════════════════════════════════════════════════════════════

            Console.WriteLine("\n================= SANDBOX DONE =================");
        }

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
    }
}
