using Microsoft.Extensions.Logging;
using mt4_term_api;
using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using MetaRPC.CSharpMT4;


namespace MetaRPC.CSharpMT4
{
    public class MT4Service
    {
        private readonly MT4Account _mt4;
        private readonly ILogger<MT4Service> _logger;

        public MT4Service(MT4Account mt4, ILogger<MT4Service> logger)
        {
            _mt4 = mt4;
            _logger = logger;
        }


        // Streams real-time tick quotes for given symbols during a specified duration.
        // 
        // Parameters:
        //   symbols         - array of symbols (e.g. "EURUSD", "GBPUSD") to subscribe to
        //   durationSeconds - how long to keep streaming ticks (default = 10 sec)
        //
        // Behavior:
        //   - Subscribes to MT4 tick stream via OnSymbolTickAsync.
        //   - Prints each incoming tick with Bid, Ask, and Time to console.
        //   - Automatically stops after the specified duration.
        //   - Catches OperationCanceledException when the cancellation token expires.
        public async Task StreamQuotesForSymbolsAsync(string[] symbols, int durationSeconds = 10)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(durationSeconds));
            var token = cts.Token;

            _logger.LogInformation("=== Streaming Ticks for {0} for {1} seconds ===", string.Join(", ", symbols), durationSeconds);

            try
            {
                await foreach (var tick in _mt4.OnSymbolTickAsync(symbols, token))
                {
                    var q = tick.SymbolTick;
                    if (q == null) continue;

                    var time = q.Time?.ToDateTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "n/a";
                    Console.WriteLine($"[Tick] {q.Symbol}: Bid={q.Bid}, Ask={q.Ask}, Time={time}");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("⏹️ Streaming stopped after {0} seconds", durationSeconds);
            }
        }



        // -----=== 📂 Account Info ===-----


        // Uses the MT4 API (CSharpMT4) to receive and output the balance, equity, and currency of the account.
        //
        // Receives data via AccountSummaryAsync (see MetaRPC/CSharpMT4 repository),
        // logs the start of the operation and outputs the result to the console.
        public async Task ShowAccountSummary()
        {
            _logger.LogInformation("=== Account Summary ===");
            var summary = await _mt4.AccountSummaryAsync();
            Console.WriteLine($"Balance: {summary.AccountBalance}, Equity: {summary.AccountEquity}, Currency: {summary.AccountCurrency}");
        }

        // -----=== 📂 Order Operations ===-----

        // Displays all currently opened orders in the account.
        //
        // Behavior:
        //   - Requests opened orders from MT4 via OpenedOrdersAsync (see MetaRPC/CSharpMT4).
        //   - Logs header "Opened Orders".
        //   - Iterates through returned OrderInfos and prints:
        //       OrderType, Ticket, Symbol, Lots, OpenPrice, Profit, OpenTime.
        public async Task ShowOpenedOrders()
        {
            _logger.LogInformation("=== Opened Orders ===");
            var ordersData = await _mt4.OpenedOrdersAsync();

            foreach (var order in ordersData.OrderInfos)
            {
                Console.WriteLine($"[{order.OrderType}] Ticket: {order.Ticket}, Symbol: {order.Symbol}, " +
                                  $"Lots: {order.Lots}, OpenPrice: {order.OpenPrice}, Profit: {order.Profit}, " +
                                  $"OpenTime: {order.OpenTime}");
            }
        }

        // Displays tickets of all currently opened orders.
        //
        // Behavior:
        //   - Requests opened order tickets from MT4 via OpenedOrdersTicketsAsync (see MetaRPC/CSharpMT4).
        //   - Logs header "Opened Order Tickets".
        //   - Iterates through returned ticket list and prints each ticket ID.
        public async Task ShowOpenedOrderTickets()
        {
            _logger.LogInformation("=== Opened Order Tickets ===");
            var ticketsData = await _mt4.OpenedOrdersTicketsAsync();

            Console.WriteLine("Open Order Tickets:");
            foreach (var ticket in ticketsData.Tickets)
            {
                Console.WriteLine($" - Ticket: {ticket}");
            }

        }

        // Displays account order history for the last 7 days.
        //
        // Behavior:
        //   - Defines time range: from (UTC now - 7 days) to (UTC now).
        //   - Requests historical orders via OrdersHistoryAsync 
        //       with sorting by CloseTime descending (see MetaRPC/CSharpMT4).
        //   - Iterates through OrdersInfo and prints ticket & symbol (short form).
        //   - Then prints detailed info per order: 
        //       OrderType, Ticket, Symbol, Lots, OpenPrice, ClosePrice, Profit, CloseTime.
        public async Task ShowOrdersHistory()
        {
            _logger.LogInformation("=== Order History ===");
            var from = DateTime.UtcNow.AddDays(-7);
            var to = DateTime.UtcNow;

            var history = await _mt4.OrdersHistoryAsync(
                sortType: EnumOrderHistorySortType.HistorySortByCloseTimeDesc,
                from: from,
                to: to
            );

            foreach (var order in history.OrdersInfo)
            {
                Console.WriteLine($"Ticket: {order.Ticket}, Symbol: {order.Symbol}");
            }

            foreach (var order in history.OrdersInfo)
            {
                Console.WriteLine($"[{order.OrderType}] Ticket: {order.Ticket}, Symbol: {order.Symbol}, " +
                                  $"Lots: {order.Lots}, Open: {order.OpenPrice}, Close: {order.ClosePrice}, " +
                                  $"Profit: {order.Profit}, CloseTime: {order.CloseTime}");
            }
        }


        // Closes or deletes an order by its ticket.
        //
        // Behavior:
        //   - Accepts order ticket (long) as input.
        //   - Validates that ticket value fits into int range 
        //       (since MT4 API expects int, otherwise throws OverflowException).
        //   - Builds OrderCloseDeleteRequest and sends it via OrderCloseDeleteAsync 
        //       (see MetaRPC/CSharpMT4).
        //   - Prints result mode (Closed/Deleted) and server comment to console.
        public async Task CloseOrderExample(long ticket)
        {
            _logger.LogInformation("=== Close/Delete Order ===");

            var request = new OrderCloseDeleteRequest();
            if (ticket > int.MaxValue || ticket < int.MinValue)
            {
                throw new OverflowException("Ticket value is out of int range!");
            }

            request.OrderTicket = (int)ticket;

            var result = await _mt4.OrderCloseDeleteAsync(request);

            Console.WriteLine($"Closed/Deleted: {result.Mode}, Comment: {result.HistoryOrderComment}");
        }

        // Closes an order using another opposite order (Close By).
        //
        // Behavior:
        //   - Accepts two tickets (order to close and opposite order).
        //   - Validates that both fit into int range (MT4 API limitation).
        //   - Sends OrderCloseByRequest via OrderCloseByAsync (see MetaRPC/CSharpMT4).
        //   - Prints resulting profit, close price, and time.
        //   - Close By allows offsetting opposite positions without opening extra trades.

        public async Task CloseByOrderExample(long ticket, long oppositeTicket)
        {
            _logger.LogInformation("=== Close By Order ===");

            if (ticket > int.MaxValue || ticket < int.MinValue ||
    oppositeTicket > int.MaxValue || oppositeTicket < int.MinValue)
            {
                throw new OverflowException("One of the tickets is out of int range!");
            }

            var request = new OrderCloseByRequest
            {
                TicketToClose = (int)ticket,
                OppositeTicketClosingBy = (int)oppositeTicket
            };

            var result = await _mt4.OrderCloseByAsync(request);

            Console.WriteLine($"Closed by opposite: Profit={result.Profit}, Price={result.ClosePrice}, Time={result.CloseTime}");

        }


        // Sends a new market Buy order (example).
        //
        // Behavior:
        //   - Builds OrderSendRequest with given symbol, fixed parameters (volume, slippage, magic, comment).
        //   - Calls OrderSendAsync to open order via MT4 API (see MetaRPC/CSharpMT4).
        //   - On success prints order ticket and open price.
        //   - Catches ApiExceptionMT4 to display error code if order fails.
        //   - Serves as a simple template for creating custom order requests.
        public async Task ShowOrderSendExample(string symbol)
        {
            _logger.LogInformation("=== Order Send Example ===");

            var request = new OrderSendRequest
            {
                Symbol = symbol,
                OperationType = OrderSendOperationType.OcOpBuy,
                Volume = 0.1,
                Price = 0,
                Slippage = 5,
                MagicNumber = 123456,
                Comment = "Test order"
            };

            try
            {
                var result = await _mt4.OrderSendAsync(request);
                Console.WriteLine($"The order was successfully opened. Ticket: {result.Ticket}, Price: {result.Price}");
            }
            catch (ApiExceptionMT4 ex)
            {
                Console.WriteLine($"Error when opening an order: {ex.ErrorCode}");
            }
        }

        // -----=== 📂 Streaming ===-----


        // Streams trade updates from MT4 in real time.
        //
        // Behavior:
        //   - Subscribes to OnTradeAsync stream (see MetaRPC/CSharpMT4).
        //   - Logs header "Streaming: Trades".
        //   - Prints message when a trade update is received, then exits loop.
        //   - Useful as a minimal example; in practice loop can process multiple updates.
        public async Task StreamTradeUpdates()
        {
            _logger.LogInformation("=== Streaming: Trades ===");
            await foreach (var trade in _mt4.OnTradeAsync())
            {
                Console.WriteLine("Trade update received.");
                break;
            }
        }


        // Streams profit updates for opened orders.
        //
        // Behavior:
        //   - Subscribes to OnOpenedOrdersProfitAsync with update interval (ms).
        //   - Logs header "Streaming: Opened Order Profits".
        //   - Prints message when a profit update is received, then exits loop.
        //   - Useful as a demo; normally you would keep streaming to track live PnL changes.
        public async Task StreamOpenedOrderProfits()
        {
            _logger.LogInformation("=== Streaming: Opened Order Profits ===");
            await foreach (var profit in _mt4.OnOpenedOrdersProfitAsync(1000))
            {
                Console.WriteLine("Profit update received.");
                break;
            }
        }


        // Streams updates of currently opened order tickets.
        //
        // Behavior:
        //   - Subscribes to OnOpenedOrdersTicketsAsync with update interval in ms (1000 = 1 sec).
        //   - Logs header "Streaming: Opened Order Tickets".
        //   - Prints message when a ticket update is received, then exits loop.
        //   - Intended as a minimal example; usually loop runs continuously to track changes in opened tickets.
        public async Task StreamOpenedOrderTickets()
        {
            _logger.LogInformation("=== Streaming: Opened Order Tickets ===");
            await foreach (var ticket in _mt4.OnOpenedOrdersTicketsAsync(1000))
            {
                Console.WriteLine("Ticket update received.");
                break;
            }
        }

        // -----=== 📂 Market Info ===-----


        // Displays the latest quote for a given symbol.
        //
        // Behavior:
        //   - Calls QuoteAsync(symbol) to request current market quote (Bid/Ask/Time).
        //   - Logs header with the symbol name.
        //   - Prints Bid, Ask, and server time to console.
        //   - Equivalent to requesting a single tick snapshot, not a stream.
        public async Task ShowQuote(string symbol)
        {
            _logger.LogInformation($"=== Current Quote for {symbol} ===");
            var quote = await _mt4.QuoteAsync(symbol);

            Console.WriteLine($"Quote for {symbol}: Bid={quote.Bid}, Ask={quote.Ask}, Time={quote.DateTime.ToDateTime():yyyy-MM-dd HH:mm:ss}");
        }



        // Displays quotes for multiple symbols.
        //
        // Behavior:
        //   - Requests initial quotes for all provided symbols via QuoteManyAsync.
        //   - For each symbol, subscribes to OnSymbolTickAsync and takes the first tick.
        //   - Prints Bid, Ask, and server time for each symbol, then breaks the loop.
        //   - Acts as a snapshot for several instruments; in practice the loop can be left open for continuous streaming.
        // Streams the first live tick for each symbol (per-symbol soft timeout).
public async Task ShowQuotesMany(string[] symbols, int timeoutSecondsPerSymbol = 5, CancellationToken ct = default)
{
    _logger.LogInformation("=== Live first tick for: {Symbols} ===", string.Join(", ", symbols));

    foreach (var symbol in symbols)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSecondsPerSymbol));

        try
        {
            await foreach (var tick in _mt4.OnSymbolTickAsync(new[] { symbol }, cts.Token))
            {
                var q = tick.SymbolTick;
                if (q == null) continue;

                var time = q.Time?.ToDateTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "n/a";
                Console.WriteLine($"Tick: {q.Symbol} {q.Bid}/{q.Ask} @ {time}");
                cts.Cancel();
                break; // first tick only
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("⏹️ No ticks for {Symbol} within {Sec}s — skipping.", symbol, timeoutSecondsPerSymbol);
        }
    }
}




        // Displays historical quotes (candles) for a given symbol.
        //
        // Behavior:
        //   - Defines time range: last 5 days (UTC) and timeframe = H1 (1 hour).
        //   - Calls QuoteHistoryAsync to request historical data (see MetaRPC/CSharpMT4).
        //   - Iterates through HistoricalQuotes and prints OHLC values with time.
        //   - Equivalent to fetching bar history (like iBars/iCandles in MQL).
        public async Task ShowQuoteHistory(string symbol)
        {
            _logger.LogInformation("=== Historical Quotes ===");
            var from = DateTime.UtcNow.AddDays(-5);
            var to = DateTime.UtcNow;
            var timeframe = ENUM_QUOTE_HISTORY_TIMEFRAME.QhPeriodH1;

            var history = await _mt4.QuoteHistoryAsync(symbol, timeframe, from, to);

            foreach (var candle in history.HistoricalQuotes)
            {
                Console.WriteLine($"[{candle.Time}] O: {candle.Open} H: {candle.High} L: {candle.Low} C: {candle.Close}");
            }
        }



        // Displays all available trading symbols from the MT4 server.
        //
        // Behavior:
        //   - Calls SymbolsAsync to request the full list of instruments (see MetaRPC/CSharpMT4).
        //   - Logs header "All Available Symbols".
        //   - Iterates through SymbolNameInfos and prints symbol name with its index.
        //   - Useful for discovering instruments before requesting quotes or sending orders.
        public async Task ShowAllSymbols()
        {
            _logger.LogInformation("=== All Available Symbols ===");

            var symbols = await _mt4.SymbolsAsync();

            foreach (var entry in symbols.SymbolNameInfos)
            {
                Console.WriteLine($"Symbol: {entry.SymbolName}, Index: {entry.SymbolIndex}");
            }

        }



        // Displays tick value, tick size, and contract size for given symbols.
        //
        // Behavior:
        //   - Calls TickValueWithSizeAsync to request trading parameters (see MetaRPC/CSharpMT4).
        //   - Logs header "Tick Value, Size and Contract Size".
        //   - Iterates through Infos and prints SymbolName, TickValue, TickSize, and ContractSize.
        //   - Useful for risk management and position sizing calculations.
        public async Task ShowTickValues(string[] symbols)
        {
            _logger.LogInformation("=== Tick Value, Size and Contract Size ===");
            var result = await _mt4.TickValueWithSizeAsync(symbols);

            foreach (var info in result.Infos)
            {
                Console.WriteLine($"Symbol: {info.SymbolName}");
                Console.WriteLine($"  TickValue: {info.TradeTickValue}");
                Console.WriteLine($"  TickSize: {info.TradeTickSize}");
                Console.WriteLine($"  ContractSize: {info.TradeContractSize}");
            }
        }




        // Displays detailed trading parameters for a given symbol.
        //
        // Behavior:
        //   - Calls SymbolParamsManyAsync to request symbol information (see MetaRPC/CSharpMT4).
        //   - Logs header "Symbol Parameters".
        //   - Iterates through SymbolInfos and prints key properties:
        //       Digits, SpreadFloat, Bid, VolumeMin/Max/Step, base/profit/margin currencies,
        //       TradeMode, and TradeExeMode.
        //   - Useful for validating instrument settings before sending orders or quotes.
        public async Task ShowSymbolParams(string symbol)
        {
            _logger.LogInformation("=== Symbol Parameters ===");
            var result = await _mt4.SymbolParamsManyAsync(symbol);

            foreach (var param in result.SymbolInfos)
            {
                Console.WriteLine($"Symbol: {param.SymbolName}");
                Console.WriteLine($"  Digits: {param.Digits}");
                Console.WriteLine($"  SpreadFloat: {param.SpreadFloat}");
                Console.WriteLine($"  Bid: {param.Bid}");
                Console.WriteLine($"  VolumeMin: {param.VolumeMin}");
                Console.WriteLine($"  VolumeMax: {param.VolumeMax}");
                Console.WriteLine($"  VolumeStep: {param.VolumeStep}");
                Console.WriteLine($"  CurrencyBase: {param.CurrencyBase}");
                Console.WriteLine($"  CurrencyProfit: {param.CurrencyProfit}");
                Console.WriteLine($"  CurrencyMargin: {param.CurrencyMargin}");
                Console.WriteLine($"  TradeMode: {param.TradeMode}");
                Console.WriteLine($"  TradeExeMode: {param.TradeExeMode}");
                Console.WriteLine();
            }
        }


        // Displays basic information for a given symbol.
        //
        // Behavior:
        //   - Calls SymbolParamsManyAsync to fetch symbol parameters (see MetaRPC/CSharpMT4).
        //   - Logs header with the symbol name.
        //   - Iterates through SymbolInfos and prints: SymbolName, Digits, Spread, and Bid.
        //   - Useful for a quick overview without showing all advanced parameters.
        public async Task ShowSymbolInfo(string symbol)
        {
            _logger.LogInformation($"=== Symbol Info: {symbol} ===");
            var info = await _mt4.SymbolParamsManyAsync(symbol);

            foreach (var param in info.SymbolInfos)
            {
                Console.WriteLine($"{param.SymbolName} — Digits: {param.Digits}, Spread: {param.SpreadFloat}, Bid: {param.Bid}");
            }
        }



        // Streams real-time quotes for a given symbol.
        //
        // Behavior:
        //   - Subscribes to OnSymbolTickAsync for the specified symbol (see MetaRPC/CSharpMT4).
        //   - Logs header "Streaming Quotes" with the symbol name.
        //   - Prints the first received tick (Symbol, Bid/Ask, Time) to console, then exits loop.
        //   - Works as a minimal demo; in real use the loop is kept open to stream continuous ticks.
        // Streams the first real-time tick for a symbol, but exits gracefully on timeout.
        public async Task ShowRealTimeQuotes(string symbol, int timeoutSeconds = 5, CancellationToken ct = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            _logger.LogInformation("=== Streaming Quotes: {Symbol} (first tick or {Sec}s) ===", symbol, timeoutSeconds);

            try
            {
                await foreach (var tick in _mt4.OnSymbolTickAsync(new[] { symbol }, cts.Token))
                {
                    var q = tick.SymbolTick;
                    if (q == null) continue;

                    var time = q.Time?.ToDateTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "n/a";
                    Console.WriteLine($"Tick: {q.Symbol} {q.Bid}/{q.Ask} @ {time}");
                    cts.Cancel();
                    break; // first tick only
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("⏹️ No ticks for {Sec}s — stopping.", timeoutSeconds);
            }
        }

    }
}
