/*══════════════════════════════════════════════════════════════════════════════
 FILE: Examples/LowLevel/3_Streaming.cs - LOW-LEVEL MT4 STREAMING DEMO
 PURPOSE:
   Demonstrates every MT4Account subscription stream. Each stream is opened
   directly, sampled for a few seconds / items, and its payload fields are read
   (see 1_LowLevel.cs for read-only info, 2_Trading.cs for trading).

 🎯 WHO SHOULD USE THIS:
   • Developers building live dashboards / tick or P&L monitors
   • Reacting to trade and connection events in real time
   • Learning how MT4Account async-generator streams are consumed

 📚 WHAT THIS DEMO COVERS (3 Sections + helper):

   1. MARKET DATA STREAM
      • OnSymbolTickAsync()         - live Bid/Ask tick per subscribed symbol

   2. TRADE & ORDER STREAMS
      • OnTradeAsync()              - order create / modify / close events
      • OnOpenedOrdersProfitAsync() - polled open-order P/L + account snapshot
      • OnOpenedOrdersTicketsAsync()- polled open-ticket set (lightweight)

   3. CONNECTION STREAM
      • OnConnectStateAsync()       - terminal link up / down changes

   HELPER
      • Sample() - bounds an `await foreach` to N items / N seconds via a
        linked CancellationTokenSource + CancelAfter (so a demo never blocks)

 💡 NOTES:
   • Each stream is sampled ~3s / 3 items; the expected OperationCanceledException
     on timeout is swallowed. Real code would keep consuming instead.

 📖 RELATED EXAMPLES:
   • 1_LowLevel.cs - read-only account/symbol/orders info
   • 2_Trading.cs  - OrderSend / OrderModify / OrderClose
   • 4_ServiceExamples.cs - mid-level streaming wrappers

 ℹ️  SAFE: read-only — only subscribes to streams, never sends an order.

 USAGE:
   Run `dotnet run` and choose [3] from the menu
   (or: dotnet run -- --example=3)
══════════════════════════════════════════════════════════════════════════════*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using mt4_term_api;

namespace MetaRPC.CSharpMT4.Examples.LowLevel
{
    /// <summary>
    /// LOW-LEVEL streaming examples: all <see cref="MT4Account"/> subscription streams (Subscriptions group).
    /// Each stream is opened directly, sampled for a few seconds / items, and its payload fields are read.
    /// </summary>
    public static class LowLevelStreaming
    {
        private static void Sec(string t) => Console.WriteLine($"\n-------------------- {t} --------------------");

        /// <summary>Runs all streaming demos against a connected account (each sampled ~3s / 3 items).</summary>
        public static async Task RunAsync(MT4Account acc, string symbol = "EURUSD", CancellationToken ct = default)
        {
            #region MARKET DATA STREAM
            // ══════════════════════════════════════════════════════════════
            // 1. MARKET DATA STREAM
            // ══════════════════════════════════════════════════════════════

            // ─────────────────────────────────────────────────────────────
            // OnSymbolTickAsync() - a new message on every tick of the symbol(s)
            // ─────────────────────────────────────────────────────────────
            Sec("OnSymbolTick");
            await Sample(c => acc.OnSymbolTickAsync(new[] { symbol }, c), ct, (i, d) =>
            {
                var t = d.SymbolTick;
                Console.WriteLine($"  tick #{i}: {t.Symbol} Bid={t.Bid} Ask={t.Ask} @ {t.Time}");
            });

            #endregion

            #region TRADE & ORDER STREAMS
            // ══════════════════════════════════════════════════════════════
            // 2. TRADE & ORDER STREAMS
            // ══════════════════════════════════════════════════════════════

            // ─────────────────────────────────────────────────────────────
            // OnTradeAsync() - pushed when an order is created/modified/closed
            // ─────────────────────────────────────────────────────────────
            Sec("OnTrade (order create/modify/close events)");
            await Sample(c => acc.OnTradeAsync(c), ct, (i, d) => Console.WriteLine($"  trade #{i}: {d}"));

            // ─────────────────────────────────────────────────────────────
            // OnOpenedOrdersProfitAsync() - server recomputes open-order P/L
            //    every 1000 ms and pushes a snapshot (+ account info).
            // ─────────────────────────────────────────────────────────────
            Sec("OnOpenedOrdersProfit (polling)");
            await Sample(c => acc.OnOpenedOrdersProfitAsync(1000, c), ct, (i, d) =>
                Console.WriteLine($"  profit #{i}: {d.OpenedOrdersWithProfitUpdated.Count} order(s), equity={d.AccountInfo?.Equity}"));

            // ─────────────────────────────────────────────────────────────
            // OnOpenedOrdersTicketsAsync() - polled open-ticket set, lighter
            //    than the profit stream (just the IDs).
            // ─────────────────────────────────────────────────────────────
            Sec("OnOpenedOrdersTickets (polling)");
            await Sample(c => acc.OnOpenedOrdersTicketsAsync(1000, c), ct, (i, d) =>
                Console.WriteLine($"  tickets #{i}: {d.PositionTickets.Count} ticket(s)"));

            #endregion

            #region CONNECTION STREAM
            // ══════════════════════════════════════════════════════════════
            // 3. CONNECTION STREAM
            // ══════════════════════════════════════════════════════════════

            // ─────────────────────────────────────────────────────────────
            // OnConnectStateAsync() - terminal link drops / comes back
            // ─────────────────────────────────────────────────────────────
            Sec("OnConnectState");
            await Sample(c => acc.OnConnectStateAsync(c), ct, (i, d) => Console.WriteLine($"  state #{i}: {d}"));

            Console.WriteLine("\n=== LOW-LEVEL (streaming) DONE ===");
            #endregion
        }

        #region STREAM SAMPLER (helper)
        // ─────────────────────────────────────────────────────────────────
        // Sample() - bound an await-foreach stream to N items / N seconds
        //    via a linked CancellationTokenSource + CancelAfter.
        // ─────────────────────────────────────────────────────────────────
        /// <summary>
        /// Opens a stream, prints up to <paramref name="maxItems"/> messages, and auto-stops after
        /// <paramref name="seconds"/>. The key detail: a linked CancellationTokenSource with CancelAfter
        /// bounds the <c>await foreach</c> so a demo stream never blocks forever; the expected
        /// OperationCanceledException on timeout is swallowed. Real code would keep consuming instead.
        /// </summary>
        private static async Task Sample<T>(Func<CancellationToken, IAsyncEnumerable<T>> make, CancellationToken outer, Action<int, T> print, int seconds = 3, int maxItems = 3)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(outer);
            cts.CancelAfter(TimeSpan.FromSeconds(seconds));
            int n = 0;
            try { await foreach (var x in make(cts.Token)) { print(++n, x); if (n >= maxItems) break; } }
            catch (OperationCanceledException) { }
            Console.WriteLine($"  ({n} event(s))");
        }
        #endregion
    }
}
