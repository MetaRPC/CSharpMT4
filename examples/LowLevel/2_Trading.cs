using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mt4_term_api;

namespace MetaRPC.CSharpMT4.Examples.LowLevel
{
    /// <summary>
    /// LOW-LEVEL trading examples: account operations - send / modify / close / close-by - via direct
    /// <see cref="MT4Account"/> calls with fully constructed request objects (Trading group).
    /// Guarded: places a real order only when enable=true; otherwise the full request flow is printed.
    /// </summary>
    public static class LowLevelTrading
    {
        private static void Sec(string t) => Console.WriteLine($"\n-------------------- {t} --------------------");

        /// <summary>Demonstrates the trading flow. Set enable=true to actually place/modify/close a minimal order.</summary>
        public static async Task RunAsync(MT4Account acc, string symbol = "EURUSD", bool enable = false, CancellationToken ct = default)
        {
            Sec("TRADING" + (enable ? " (LIVE - minimal lot)" : " (described - enable to run)"));

            // 1) Fresh quote and symbol params to size the order and place stops.
            var q = await acc.QuoteAsync(symbol, cancellationToken: ct);
            var sp = (await acc.SymbolParamsManyAsync(symbol, cancellationToken: ct)).SymbolInfos.FirstOrDefault();
            int digits = (int)(sp?.Digits ?? 5);
            double point = Math.Pow(10, -digits);
            double volume = sp?.VolumeMin ?? 0.01;
            double price = q.Ask > 0 ? q.Ask : q.Bid;
            double sl = Math.Round(price - 300 * point, digits);
            double tp = Math.Round(price + 600 * point, digits);

            // 2) Build the OrderSendRequest explicitly (this is the "full" low-level shape).
            var sendReq = new OrderSendRequest
            {
                Symbol        = symbol,
                OperationType = OrderSendOperationType.OcOpBuy,
                Volume        = volume,
                Slippage      = 5,            // points
                Comment       = "LowLevel demo",
                MagicNumber   = 777,
            };
            sendReq.Price      = price;
            sendReq.Stoploss   = sl;
            sendReq.Takeprofit = tp;

            Console.WriteLine($"OrderSendRequest: {symbol} BUY vol={volume} price={price} SL={sl} TP={tp} magic=777");

            // 2b) Pending order: the SAME OrderSendAsync, just a pending OperationType plus the
            // target Price the order waits for. MT4 pending types: OcOpBuylimit / OcOpBuystop /
            // OcOpSelllimit / OcOpSellstop. (A buy-limit sits below the Ask and fills on a dip.)
            double pendingPrice = Math.Round(price - 500 * point, digits);
            var pendingReq = new OrderSendRequest
            {
                Symbol        = symbol,
                OperationType = OrderSendOperationType.OcOpBuylimit,
                Volume        = volume,
                Price         = pendingPrice,
                Slippage      = 5,
                Comment       = "LowLevel pending demo",
                MagicNumber   = 778,
            };
            pendingReq.Stoploss   = Math.Round(pendingPrice - 300 * point, digits);
            pendingReq.Takeprofit = Math.Round(pendingPrice + 600 * point, digits);
            Console.WriteLine($"OrderSendRequest (pending): {symbol} BUY_LIMIT vol={volume} price={pendingPrice} SL={pendingReq.Stoploss} TP={pendingReq.Takeprofit} magic=778");

            if (!enable)
            {
                Console.WriteLine("OrderSend/OrderModify/OrderCloseDelete/OrderCloseBy are described only (enable=false).");
                Console.WriteLine("  acc.OrderSendAsync(sendReq)     -> ticket   (market, OperationType=OcOpBuy/OcOpSell)");
                Console.WriteLine("  acc.OrderSendAsync(pendingReq)  -> ticket   (pending, OperationType=OcOpBuylimit/Buystop/Selllimit/Sellstop)");
                Console.WriteLine("  acc.OrderModifyAsync(new OrderModifyRequest { OrderTicket=ticket, NewStopLoss=.., NewTakeProfit=.. })");
                Console.WriteLine("  acc.OrderCloseDeleteAsync(new OrderCloseDeleteRequest { OrderTicket=ticket })  // also deletes a pending order");
                Console.WriteLine("  acc.OrderCloseByAsync(new OrderCloseByRequest { TicketToClose=.., OppositeTicketClosingBy=.. })");
                Console.WriteLine("\n=== LOW-LEVEL (trading) DONE ===");
                return;
            }

            // 3) Send the market order.
            var sendRes = await acc.OrderSendAsync(sendReq, deadline: DateTime.UtcNow.AddSeconds(10), cancellationToken: ct);
            long ticket = sendRes.Ticket;
            Console.WriteLine($"OrderSend -> ticket={ticket}");
            if (ticket <= 0) { Console.WriteLine("No ticket; stop."); return; }

            // 4) Modify SL/TP a bit.
            var modReq = new OrderModifyRequest
            {
                OrderTicket   = (int)ticket,
                NewStopLoss   = Math.Round(price - 350 * point, digits),
                NewTakeProfit = Math.Round(price + 650 * point, digits),
            };
            var modRes = await acc.OrderModifyAsync(modReq, deadline: DateTime.UtcNow.AddSeconds(10), cancellationToken: ct);
            Console.WriteLine($"OrderModify -> {modRes}");

            // 5) Close / delete the order.
            var closeRes = await acc.OrderCloseDeleteAsync(new OrderCloseDeleteRequest { OrderTicket = (int)ticket }, deadline: DateTime.UtcNow.AddSeconds(10), cancellationToken: ct);
            Console.WriteLine($"OrderCloseDelete -> {closeRes}");

            // 6) Place the pending order, then delete it (same close/delete RPC removes a pending order).
            var pendRes = await acc.OrderSendAsync(pendingReq, deadline: DateTime.UtcNow.AddSeconds(10), cancellationToken: ct);
            long pendTicket = pendRes.Ticket;
            Console.WriteLine($"OrderSend (pending) -> ticket={pendTicket}");
            if (pendTicket > 0)
            {
                var pendDel = await acc.OrderCloseDeleteAsync(new OrderCloseDeleteRequest { OrderTicket = (int)pendTicket }, deadline: DateTime.UtcNow.AddSeconds(10), cancellationToken: ct);
                Console.WriteLine($"OrderCloseDelete (pending) -> {pendDel}");
            }

            Console.WriteLine("\n=== LOW-LEVEL (trading) DONE ===");
        }
    }
}
