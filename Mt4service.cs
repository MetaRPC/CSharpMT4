using Microsoft.Extensions.Logging;
using mt4_term_api;
using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using MetaRPC.CSharpMT4;
using static MetaRPC.CSharpMT4.ConsoleUi;

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

        #region Account
        /// <summary>
        /// Prints account summary: Balance, Equity, Currency.
        /// Wraps <see cref="MT4Account.AccountSummaryAsync"/> and writes to console.
        /// </summary>
        /// <returns>Task that completes after printing.</returns>
        /// <exception cref="mt4_term_api.ApiExceptionMT4">If terminal/API is not ready.</exception>

        public async Task AccountSummary()
        {
            Box("AccountSummary()");
            _logger.LogInformation("=== Account Summary ===");
            var summary = await _mt4.AccountSummaryAsync();
            Console.WriteLine($"Balance: {summary.AccountBalance}, Equity: {summary.AccountEquity}, Currency: {summary.AccountCurrency}");
        }

        #endregion

    //----------------------------------------------------------------------------------------------------------------

        #region Quotes
        /// <summary>
        /// Prints the current quote for the given symbol (Bid/Ask + timestamp).
        /// Wraps <see cref="MT4Account.QuoteAsync(string)"/> with retry and logs via ILogger.
        /// </summary>
        /// <param name="symbol">Trading symbol, e.g. "EURUSD".</param>
        /// <returns>Task that completes after printing.</returns>
        /// <exception cref="mt4_term_api.ApiExceptionMT4">If terminal/API is not ready.</exception>

        public async Task Quote(string symbol)
        {
            Box($"Quote(\"{symbol}\")");
            _logger.LogInformation("=== Current Quote for {Symbol} ===", symbol);
            var quote = await Retry.RunAsync(() => _mt4.QuoteAsync(symbol), logger: _logger);
            Console.WriteLine($"Quote for {symbol}: Bid={quote.Bid}, Ask={quote.Ask}, Time={quote.DateTime.ToDateTime():yyyy-MM-dd HH:mm:ss}");
        }


        /// <summary>
        /// For each symbol, subscribes to ticks and prints the first live tick (Bid/Ask @ time).
        /// Wraps <see cref="MT4Account.OnSymbolTickAsync(string[], System.Threading.CancellationToken)"/>; per-symbol timeout applies.
        /// </summary>
        /// <param name="symbols">Symbols to probe (e.g., "EURUSD").</param>
        /// <param name="timeoutSecondsPerSymbol">Max seconds to wait per symbol before skipping.</param>
        /// <param name="ct">Optional cancellation for the whole routine.</param>
        /// <returns>Task that completes after first tick (or timeout) for each symbol.</returns>
        /// <exception cref="mt4_term_api.ApiExceptionMT4">If the stream can’t be opened or terminal is not ready.</exception>

        public async Task QuotesMany(string[] symbols, int timeoutSecondsPerSymbol = 5, CancellationToken ct = default)
        {
            Box($"QuotesMany([{string.Join(", ", symbols)}]) — first live tick per symbol");
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
                        cts.Cancel(); // stop after first tick
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("⏹️ No ticks for {Symbol} within {Sec}s — skipping.", symbol, timeoutSecondsPerSymbol);
                }
            }
        }


        /// <summary>
        /// Prints last 5 days of H1 candles (UTC) for the symbol: time + OHLC.
        /// Wraps <see cref="MT4Account.QuoteHistoryAsync(string, ENUM_QUOTE_HISTORY_TIMEFRAME, DateTime, DateTime)"/>
        /// with QhPeriodH1 and [now-5d .. now], then writes to console.
        /// </summary>
        /// <param name="symbol">Trading symbol, e.g., "EURUSD".</param>
        /// <returns>Task that completes after printing.</returns>
        /// <exception cref="mt4_term_api.ApiExceptionMT4">If terminal/API is not ready.</exception>

        public async Task QuoteHistory(string symbol)
        {
            Box($"QuoteHistory(\"{symbol}\") — last 5 days, H1");
            _logger.LogInformation("=== Historical Quotes ===");
            var from = DateTime.UtcNow.AddDays(-5);
            var to = DateTime.UtcNow;
            var timeframe = ENUM_QUOTE_HISTORY_TIMEFRAME.QhPeriodH1;

            var history = await Retry.RunAsync(() => _mt4.QuoteHistoryAsync(symbol, timeframe, from, to), logger: _logger);

            foreach (var candle in history.HistoricalQuotes)
            {
                Console.WriteLine($"[{candle.Time}] O: {candle.Open} H: {candle.High} L: {candle.Low} C: {candle.Close}");
            }
        }


        /// <summary>
        /// Streams ticks for the symbol and prints the first tick (Bid/Ask @ time),
        /// or stops after <paramref name="timeoutSeconds"/>.
        /// Wraps <see cref="MT4Account.OnSymbolTickAsync(string[], System.Threading.CancellationToken)"/>.
        /// </summary>
        /// <param name="symbol">Trading symbol (e.g., "EURUSD").</param>
        /// <param name="timeoutSeconds">Max seconds to wait for the first tick.</param>
        /// <param name="ct">Optional cancellation for the routine.</param>
        /// <returns>Task that completes after first tick or timeout.</returns>
        /// <exception cref="mt4_term_api.ApiExceptionMT4">If the stream cannot be opened.</exception>

        public async Task RealTimeQuotes(string symbol, int timeoutSeconds = 5, CancellationToken ct = default)
        {
            Box($"RealTimeQuotes(\"{symbol}\") — first tick or {timeoutSeconds}s");
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
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("⏹️ No ticks for {Sec}s — stopping.", timeoutSeconds);
            }
        }


        /// <summary>
        /// Subscribes to ticks for the given symbols and prints each tick (Bid/Ask @ time)
        /// for the specified duration.
        /// Wraps <see cref="MT4Account.OnSymbolTickAsync(string[], System.Threading.CancellationToken)"/>
        /// and cancels internally after <paramref name="durationSeconds"/>.
        /// </summary>
        /// <param name="symbols">Symbols to stream (e.g., "EURUSD", "GBPUSD").</param>
        /// <param name="durationSeconds">Streaming duration in seconds.</param>
        /// <returns>Task that completes when streaming ends.</returns>
        /// <exception cref="mt4_term_api.ApiExceptionMT4">If the stream cannot be opened.</exception>

        public async Task StreamQuotesForSymbolsAsync(string[] symbols, int durationSeconds = 10)
        {
            Box($"StreamQuotesForSymbolsAsync([{string.Join(", ", symbols)}], {durationSeconds}s)");
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

        #endregion

    //----------------------------------------------------------------------------------------------------------------

        #region Market Info

        /// <summary>
        /// Lists all available symbols with their index and prints to console.
        /// Wraps <see cref="MT4Account.SymbolsAsync"/> with retry and logs via ILogger.
        /// </summary>
        /// <returns>Task that completes after printing.</returns>
        /// <exception cref="mt4_term_api.ApiExceptionMT4">If terminal/API is not ready.</exception>

        public async Task AllSymbols()
        {
            Box("AllSymbols()");
            _logger.LogInformation("=== All Available Symbols ===");

            var symbols = await Retry.RunAsync(() => _mt4.SymbolsAsync(), logger: _logger);
            foreach (var entry in symbols.SymbolNameInfos)
            {
                Console.WriteLine($"Symbol: {entry.SymbolName}, Index: {entry.SymbolIndex}");
            }
        }


        /// <summary>
        /// Fetches tick value, tick size, and contract size for each symbol and prints them.
        /// Wraps <see cref="MT4Account.TickValueWithSizeAsync(string[])"/> with retry and logging.
        /// </summary>
        /// <param name="symbols">Symbols to query (e.g., "EURUSD").</param>
        /// <returns>Task that completes after printing.</returns>
        /// <exception cref="mt4_term_api.ApiExceptionMT4">If terminal/API is not ready.</exception>

        public async Task TickValues(string[] symbols)
        {
            Box($"TickValues([{string.Join(", ", symbols)}])");
            _logger.LogInformation("=== Tick Value, Size and Contract Size ===");
            var result = await Retry.RunAsync(() => _mt4.TickValueWithSizeAsync(symbols), logger: _logger);

            foreach (var info in result.Infos)
            {
                Console.WriteLine($"Symbol: {info.SymbolName}");
                Console.WriteLine($"  TickValue: {info.TradeTickValue}");
                Console.WriteLine($"  TickSize: {info.TradeTickSize}");
                Console.WriteLine($"  ContractSize: {info.TradeContractSize}");
            }
        }


        /// <summary>
        /// Prints detailed parameters for the symbol (digits, spread, volumes, currencies, trade modes).
        /// Wraps <see cref="MT4Account.SymbolParamsManyAsync(string)"/> and writes each field to console.
        /// </summary>
        /// <param name="symbol">Trading symbol (e.g., "EURUSD").</param>
        /// <returns>Task that completes after printing.</returns>
        /// <exception cref="mt4_term_api.ApiExceptionMT4">If terminal/API is not ready.</exception>

        public async Task SymbolParams(string symbol)
        {
            Box($"SymbolParams(\"{symbol}\")");
            _logger.LogInformation("=== Symbol Parameters ===");
            var result = await Retry.RunAsync(() => _mt4.SymbolParamsManyAsync(symbol), logger: _logger);

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


        /// <summary>
        /// Prints a compact symbol overview: name, digits, spread flag, and current Bid.
        /// Wraps <see cref="MT4Account.SymbolParamsManyAsync(string)"/> with retry and logging.
        /// </summary>
        /// <param name="symbol">Trading symbol (e.g., "EURUSD").</param>
        /// <returns>Task that completes after printing.</returns>
        /// <exception cref="mt4_term_api.ApiExceptionMT4">If terminal/API is not ready.</exception>

        public async Task SymbolInfo(string symbol)
        {
            Box($"SymbolInfo(\"{symbol}\")");
            _logger.LogInformation("=== Symbol Info ===");
            var info = await Retry.RunAsync(() => _mt4.SymbolParamsManyAsync(symbol), logger: _logger);

            foreach (var param in info.SymbolInfos)
            {
                Console.WriteLine($"{param.SymbolName} — Digits: {param.Digits}, Spread: {param.SpreadFloat}, Bid: {param.Bid}");
            }
        }

        #endregion

        //----------------------------------------------------------------------------------------------------------------

        #region Orders & History


        /// <summary>
        /// Prints all currently opened orders: Type, Ticket, Symbol, Lots, OpenPrice, Profit, OpenTime.
        /// Wraps <see cref="MT4Account.OpenedOrdersAsync"/> with retry and logs via ILogger.
        /// </summary>
        /// <returns>Task that completes after printing.</returns>
        /// <exception cref="mt4_term_api.ApiExceptionMT4">If terminal/API is not ready.</exception>

        public async Task OpenedOrders()
        {
            Box("OpenedOrders()");
            _logger.LogInformation("=== Opened Orders ===");
            var ordersData = await Retry.RunAsync(() => _mt4.OpenedOrdersAsync(), logger: _logger);

            foreach (var order in ordersData.OrderInfos)
            {
                Console.WriteLine($"[{order.OrderType}] Ticket: {order.Ticket}, Symbol: {order.Symbol}, " +
                                  $"Lots: {order.Lots}, OpenPrice: {order.OpenPrice}, Profit: {order.Profit}, " +
                                  $"OpenTime: {order.OpenTime}");
            }
        }


        /// <summary>
        /// Prints IDs of currently opened orders (tickets only).
        /// Wraps <see cref="MT4Account.OpenedOrdersTicketsAsync"/> and writes to console.
        /// </summary>
        /// <returns>Task that completes after printing.</returns>
        /// <exception cref="mt4_term_api.ApiExceptionMT4">If terminal/API is not ready.</exception>

        public async Task OpenedOrderTickets()
        {
            Box("OpenedOrderTickets()");
            _logger.LogInformation("=== Opened Order Tickets ===");
            var ticketsData = await Retry.RunAsync(() => _mt4.OpenedOrdersTicketsAsync(), logger: _logger);

            Console.WriteLine("Open Order Tickets:");
            foreach (var ticket in ticketsData.Tickets)
            {
                Console.WriteLine($" - Ticket: {ticket}");
            }
        }


        /// <summary>
        /// Prints order history for the last 7 days (sorted by CloseTime DESC):
        /// Type, Ticket, Symbol, Lots, Open/Close price, Profit, CloseTime.
        /// Wraps <see cref="MT4Account.OrdersHistoryAsync(EnumOrderHistorySortType, DateTime, DateTime)"/>
        /// with <see cref="EnumOrderHistorySortType.HistorySortByCloseTimeDesc"/> and [now-7d .. now].
        /// </summary>
        /// <returns>Task that completes after printing.</returns>
        /// <exception cref="mt4_term_api.ApiExceptionMT4">If terminal/API is not ready.</exception>

        public async Task OrdersHistory()
        {
            Box("OrdersHistory() — last 7 days");
            _logger.LogInformation("=== Order History ===");
            var from = DateTime.UtcNow.AddDays(-7);
            var to = DateTime.UtcNow;

            var history = await Retry.RunAsync(
                () => _mt4.OrdersHistoryAsync(
                    sortType: EnumOrderHistorySortType.HistorySortByCloseTimeDesc,
                    from: from,
                    to: to
                ),
                logger: _logger);

            foreach (var order in history.OrdersInfo)
            {
                Console.WriteLine($"[{order.OrderType}] Ticket: {order.Ticket}, Symbol: {order.Symbol}, " +
                                  $"Lots: {order.Lots}, Open: {order.OpenPrice}, Close: {order.ClosePrice}, " +
                                  $"Profit: {order.Profit}, CloseTime: {order.CloseTime}");
            }
        }

        #endregion

    //----------------------------------------------------------------------------------------------------------------

        #region Streams (Trades & Order Updates)

        /// <summary>
        /// Subscribes to trade updates and prints the first received update, then stops.
        /// Wraps <see cref="MT4Account.OnTradeAsync()"/>; honors <paramref name="ct"/> for cancellation.
        /// </summary>
        /// <param name="ct">Optional cancellation token for stopping the stream.</param>
        /// <returns>Task that completes after the first update or cancellation.</returns>
        /// <exception cref="mt4_term_api.ApiExceptionMT4">If the stream cannot be opened.</exception>

        public async Task StreamTradeUpdates(CancellationToken ct = default)
        {
            Box("StreamTradeUpdates()");
            _logger.LogInformation("=== Streaming: Trades ===");
            await foreach (var trade in _mt4.OnTradeAsync())
            {
                if (ct.IsCancellationRequested) break;
                Console.WriteLine("Trade update received.");
                break;
            }
        }


        /// <summary>
        /// Subscribes to profit updates for currently opened orders and prints the first update, then stops.
        /// Wraps <see cref="MT4Account.OnOpenedOrdersProfitAsync(int)"/> with argument 1000 (library-specific).
        /// Honors <paramref name="ct"/> for cancellation.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>Task that completes after the first update or cancellation.</returns>
        /// <exception cref="mt4_term_api.ApiExceptionMT4">If the stream cannot be opened.</exception>

        public async Task StreamOpenedOrderProfits(CancellationToken ct = default)
        {
            Box("StreamOpenedOrderProfits()");
            _logger.LogInformation("=== Streaming: Opened Order Profits ===");
            await foreach (var profit in _mt4.OnOpenedOrdersProfitAsync(1000))
            {
                if (ct.IsCancellationRequested) break;
                Console.WriteLine("Profit update received.");
                break;
            }
        }


        /// <summary>
        /// Subscribes to opened-order ticket updates and prints the first update, then stops.
        /// Wraps <see cref="MT4Account.OnOpenedOrdersTicketsAsync(int)"/> (buffer: 1000); honors cancellation.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>Task that completes after the first update or cancellation.</returns>
        /// <exception cref="mt4_term_api.ApiExceptionMT4">If the stream cannot be opened.</exception>

        public async Task StreamOpenedOrderTickets(CancellationToken ct = default)
        {
            Box("StreamOpenedOrderTickets()");
            _logger.LogInformation("=== Streaming: Opened Order Tickets ===");
            await foreach (var ticket in _mt4.OnOpenedOrdersTicketsAsync(1000))
            {
                if (ct.IsCancellationRequested) break;
                Console.WriteLine("Ticket update received.");
                break;
            }
        }

        #endregion

        //----------------------------------------------------------------------------------------------------------------

        #region Trading (Send / Modify / Close)


        /// <summary>
        /// Opens a market BUY order for the given symbol.
        /// Builds a valid OrderSendRequest (proper digits, int slippage) and sends it with retry.
        /// </summary>
        /// <param name="symbol">Trading symbol, e.g. "EURUSD".</param>
        /// <returns>Task that completes after printing ticket/price.</returns>
        /// <exception cref="mt4_term_api.ApiExceptionMT4">If terminal/API is not ready.</exception>

        public async Task OrderSendExample(string symbol)
        {
            Box($"OrderSendExample(\"{symbol}\")");
            _logger.LogInformation("=== Order Send Example ===");

            try
            {
                var request = await OrderUtils.BuildSendAsync(
                    _mt4,
                    symbol,
                    OrderSendOperationType.OcOpBuy,
                    volume: 0.1,
                    slippage: 5,
                    magicNumber: 123456,
                    comment: "Test order");

                var result = await Retry.RunAsync(() => _mt4.OrderSendAsync(request), logger: _logger);
                Console.WriteLine($"Opened. Ticket: {result.Ticket}, Price: {result.Price}");

                // (optional) set SL/TP immediately after opening
                /*
                var mod = await OrderUtils.BuildStopsModifyAsync(
                    _mt4, result.Ticket, symbol,
                    stopLoss: 0.0, takeProfit: 0.0);

                var modRes = await Retry.RunAsync(() => _mt4.OrderModifyAsync(mod), logger: _logger);
                Console.WriteLine(modRes.OrderWasModified ? "SL/TP set" : "No changes to SL/TP");
                */
            }
            catch (ApiExceptionMT4 ex)
            {
                Console.WriteLine($"Error when opening an order: {ex.ErrorCode}");
            }
        }


        /// <summary>
        /// Modifies an existing order: price, SL/TP, and/or expiration (all optional).
        /// Builds <see cref="OrderModifyRequest"/> via <c>OrderUtils.BuildModify</c> and sends with retry.
        /// </summary>
        /// <param name="ticket">Order ticket to modify.</param>
        /// <returns>Task that completes after printing the result.</returns>
        /// <exception cref="mt4_term_api.ApiExceptionMT4">If terminal/API is not ready.</exception>

        public async Task OrderModifyExample(
            int ticket,
            double? newPrice = null,
            double? newStopLoss = null,
            double? newTakeProfit = null,
            DateTime? newExpiration = null)
        {
            Box($"OrderModifyExample(ticket={ticket})");
            _logger.LogInformation("=== Order Modify Example ===");

            if (ticket > int.MaxValue || ticket < int.MinValue)
                throw new OverflowException("Ticket value is out of int range!");

            var request = OrderUtils.BuildModify(ticket, newPrice, newStopLoss, newTakeProfit, newExpiration);

            try
            {
                var result = await Retry.RunAsync(() => _mt4.OrderModifyAsync(request), logger: _logger);
                Console.WriteLine(result.OrderWasModified ? "Modified: OK" : "Modified: NO CHANGES");
            }
            catch (ApiExceptionMT4 ex)
            {
                Console.WriteLine($"Modify failed for ticket {ticket}: {ex.ErrorCode}");
            }
        }


        /// <summary>
        /// Closes (market) or deletes (pending) an order by ticket and prints the result.
        /// Wraps <see cref="MT4Account.OrderCloseDeleteAsync(OrderCloseDeleteRequest)"/> with retry.
        /// </summary>
        /// <param name="ticket">Order ticket to close/delete.</param>
        /// <returns>Task that completes after printing mode and comment.</returns>
        /// <exception cref="mt4_term_api.ApiExceptionMT4">If terminal/API is not ready.</exception>

        public async Task CloseOrderExample(long ticket)
        {
            Box($"CloseOrderExample(ticket={ticket})");
            _logger.LogInformation("=== Close/Delete Order ===");

            var request = OrderUtils.BuildCloseDelete(ticket);
            var result = await Retry.RunAsync(() => _mt4.OrderCloseDeleteAsync(request), logger: _logger);
            Console.WriteLine($"Closed/Deleted: {result.Mode}, Comment: {result.HistoryOrderComment}");
        }


        /// <summary>
        /// Closes a position “by opposite” using two tickets and prints profit/price/time.
        /// Wraps <see cref="MT4Account.OrderCloseByAsync(OrderCloseByRequest)"/> via retry and logging.
        /// </summary>
        /// <param name="ticket">Ticket of the order to be closed.</param>
        /// <param name="oppositeTicket">Ticket of the opposite order used to close.</param>
        /// <returns>Task that completes after printing the result.</returns>
        /// <exception cref="mt4_term_api.ApiExceptionMT4">If terminal/API is not ready.</exception>

        public async Task CloseByOrderExample(long ticket, long oppositeTicket)
        {
            Box($"CloseByOrderExample(ticket={ticket}, opposite={oppositeTicket})");
            _logger.LogInformation("=== Close By Order ===");

            var request = OrderUtils.BuildCloseBy(ticket, oppositeTicket);
            var result = await Retry.RunAsync(() => _mt4.OrderCloseByAsync(request), logger: _logger);
            Console.WriteLine($"Closed by opposite: Profit={result.Profit}, Price={result.ClosePrice}, Time={result.CloseTime}");
        }

        #endregion
    }
}