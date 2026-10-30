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
            // Price stream: a new message on every tick of the subscribed symbol(s).
            Sec("OnSymbolTick");
            await Sample(c => acc.OnSymbolTickAsync(new[] { symbol }, c), ct, (i, d) =>
            {
                var t = d.SymbolTick;
                Console.WriteLine($"  tick #{i}: {t.Symbol} Bid={t.Bid} Ask={t.Ask} @ {t.Time}");
            });

            // Trade events: pushed whenever an order is created, modified or closed on the account.
            Sec("OnTrade (order create/modify/close events)");
            await Sample(c => acc.OnTradeAsync(c), ct, (i, d) => Console.WriteLine($"  trade #{i}: {d}"));

            // Live P/L: the server recomputes open-order profit every 1000 ms and pushes a snapshot.
            Sec("OnOpenedOrdersProfit (polling)");
            await Sample(c => acc.OnOpenedOrdersProfitAsync(1000, c), ct, (i, d) =>
                Console.WriteLine($"  profit #{i}: {d.OpenedOrdersWithProfitUpdated.Count} order(s), equity={d.AccountInfo?.Equity}"));

            // Open-ticket set, also polled every 1000 ms — lighter than the profit stream (just the IDs).
            Sec("OnOpenedOrdersTickets (polling)");
            await Sample(c => acc.OnOpenedOrdersTicketsAsync(1000, c), ct, (i, d) =>
                Console.WriteLine($"  tickets #{i}: {d.PositionTickets.Count} ticket(s)"));

            // Connection state: tells you when the terminal link drops or comes back.
            Sec("OnConnectState");
            await Sample(c => acc.OnConnectStateAsync(c), ct, (i, d) => Console.WriteLine($"  state #{i}: {d}"));

            Console.WriteLine("\n=== LOW-LEVEL (streaming) DONE ===");
        }

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
    }
}
