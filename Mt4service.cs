using Microsoft.Extensions.Logging;
using mt4_term_api;
using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;


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



        // === Account Info ===
        public async Task ShowAccountSummary()
        {
            _logger.LogInformation("=== Account Summary ===");
            var summary = await _mt4.AccountSummaryAsync();
            Console.WriteLine($"Balance: {summary.AccountBalance}, Equity: {summary.AccountEquity}, Currency: {summary.AccountCurrency}");
        }

        // === Order Operations ===
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

        // === Streaming ===
        public async Task StreamTradeUpdates()
        {
            _logger.LogInformation("=== Streaming: Trades ===");
            await foreach (var trade in _mt4.OnTradeAsync())
            {
                Console.WriteLine("Trade update received.");
                break;
            }
        }

        public async Task StreamOpenedOrderProfits()
        {
            _logger.LogInformation("=== Streaming: Opened Order Profits ===");
            await foreach (var profit in _mt4.OnOpenedOrdersProfitAsync(1000))
            {
                Console.WriteLine("Profit update received.");
                break;
            }
        }

        public async Task StreamOpenedOrderTickets()
        {
            _logger.LogInformation("=== Streaming: Opened Order Tickets ===");
            await foreach (var ticket in _mt4.OnOpenedOrdersTicketsAsync(1000))
            {
                Console.WriteLine("Ticket update received.");
                break;
            }
        }

        // === Market Info ===
        public async Task ShowQuote(string symbol)
        {
            _logger.LogInformation($"=== Current Quote for {symbol} ===");
            var quote = await _mt4.QuoteAsync(symbol);

            Console.WriteLine($"Quote for {symbol}: Bid={quote.Bid}, Ask={quote.Ask}, Time={quote.DateTime.ToDateTime():yyyy-MM-dd HH:mm:ss}");
        }

        public async Task ShowQuotesMany(string[] symbols)
        {
            _logger.LogInformation("=== Quotes for Multiple Symbols ===");
            var quotes = await _mt4.QuoteManyAsync(symbols);

            foreach (var symbol in symbols)
            {
                await foreach (var tick in _mt4.OnSymbolTickAsync(new[] { symbol }))
                {
                    var q = tick.SymbolTick;
                    var time = q.Time?.ToDateTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "n/a";
                    Console.WriteLine($"Quote for {q.Symbol}: Bid={q.Bid}, Ask={q.Ask}, Time={time}");
                    break;
                }
            }


        }

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

        public async Task ShowAllSymbols()
        {
            _logger.LogInformation("=== All Available Symbols ===");

            var symbols = await _mt4.SymbolsAsync();

            foreach (var entry in symbols.SymbolNameInfos)
            {
                Console.WriteLine($"Symbol: {entry.SymbolName}, Index: {entry.SymbolIndex}");
            }

        }
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

        public async Task ShowSymbolInfo(string symbol)
        {
            _logger.LogInformation($"=== Symbol Info: {symbol} ===");
            var info = await _mt4.SymbolParamsManyAsync(symbol);

            foreach (var param in info.SymbolInfos)
            {
                Console.WriteLine($"{param.SymbolName} — Digits: {param.Digits}, Spread: {param.SpreadFloat}, Bid: {param.Bid}");
            }
        }

        public async Task ShowRealTimeQuotes(string symbol)
        {
            _logger.LogInformation($"=== Streaming Quotes: {symbol} ===");
            await foreach (var tick in _mt4.OnSymbolTickAsync(new[] { symbol }))
            {
                Console.WriteLine($"Tick: {tick.SymbolTick.Symbol} {tick.SymbolTick.Bid}/{tick.SymbolTick.Ask} @ {tick.SymbolTick.Time}");
                break;
            }
        }
    }

}
