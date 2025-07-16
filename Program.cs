using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using mt4_term_api;
using Newtonsoft.Json;

namespace MetaRPC.CSharpMT4
{
    public class MT4Options
    {
        public ulong User { get; set; }
        public string Password { get; set; }
        public string ServerName { get; set; }
        public string Host { get; set; }
        public int Port { get; set; } = 443;
        public string DefaultSymbol { get; set; } = "EURUSD";
    }

    internal class Program
    {
        private static ILogger<Program> _logger;
        private static IConfiguration _configuration;
        private static MT4Account _mt4;
        private static MT4Service _service;

        static async Task Main(string[] args)
        {
            await new Program().Run(args);
        }

        private async Task Run(string[] args)
        {
            Configure();

            var options = _configuration.GetSection("MT4Options").Get<MT4Options>();
            _mt4 = new MT4Account(options.User, options.Password);

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<Program>();
            _service = new MT4Service(_mt4, loggerFactory.CreateLogger<MT4Service>());

            try
            {
                _logger.LogInformation("Connecting to MT4...");
                await _mt4.ConnectByServerNameAsync(options.ServerName);

                // === BLOCK 1: Account Info ===
                await _service.ShowAccountSummary();

                // === BLOCK 2: Order Operations ===
                await _service.ShowOpenedOrders();
                 await _service.ShowOpenedOrderTickets();
                  await _service.ShowOrdersHistory();


                //These methods require specific order numbers.
                // If you call with a non—existent ticket, there will be an error from the server (Ticket not found, Invalid ticket, etc.).
                //(123456, 654321 — These are stubs, not real tickets..)

                // await _service.CloseOrderExample(123456);
                // await _service.CloseByOrderExample(123456, 654321);
                // await _service.ShowOrderSendExample(options.DefaultSymbol);

                // === BLOCK 3: Market Info ===
                await _service.ShowSymbolInfo(options.DefaultSymbol);
                 await _service.ShowQuote(options.DefaultSymbol);
                  await _service.ShowQuotesMany(new[] { "EURUSD", "GBPUSD", "USDJPY" });
                   await _service.ShowQuoteHistory(options.DefaultSymbol);
                    await _service.ShowAllSymbols();
                     await _service.ShowTickValues(new[] { "EURUSD", "GBPUSD" });
                      await _service.ShowSymbolParams("EURUSD");

                // === BLOCK 4: Streaming ===
                await _service.ShowRealTimeQuotes(options.DefaultSymbol);
                 await _service.StreamQuotesForSymbolsAsync(new[] { "EURUSD", "GBPUSD" }, durationSeconds: 10);
                  await _service.StreamTradeUpdates();
                   await _service.StreamOpenedOrderProfits();
                    await _service.StreamOpenedOrderTickets();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error occurred");
            }
        }

        private void Configure()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
        }
    }
}
