using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using mt4_term_api;
using MetaRPC.CSharpMT4;

namespace MetaRPC.CSharpMT4
{
    public class MT4Options
    {
        public ulong User { get; set; }
        public string Password { get; set; } = string.Empty;
        public string? ServerName { get; set; }
        public string? Host { get; set; }
        public int Port { get; set; } = 443;

        public string DefaultSymbol { get; set; } = "EURUSD";
    }

    internal class Program
    {
        private static ILogger<Program> _logger = default!;
        private static IConfiguration _configuration = default!;
        private static MT4Account _mt4 = default!;
        private static MT4Service _service = default!;
        private static CancellationTokenSource _appCts = default!;

        // ================================
        // ===------ 🔧 Toggles -------===
        // ================================
        private static readonly bool EnableTradingExamples = false;  // ⚠️ Real trading operations
        private const bool EnableStreams = true;                      // ticks/profit/tickets

        static async Task Main(string[] args)
        {
            await new Program().Run(args);
        }

        private async Task Run(string[] args)
        {

            Configure();

            // Ctrl+C — a neat exit
            _appCts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                _logger?.LogWarning("CTRL+C pressed — stopping...");
                _appCts.Cancel();
            };
            var ct = _appCts.Token;

            // 1) Read and validate options
            var options = _configuration.GetSection("MT4Options").Get<MT4Options>()
                ?? throw new ArgumentException("Missing 'MT4Options' section in configuration.");

            if (string.IsNullOrWhiteSpace(options.Password))
                throw new ArgumentException("MT4Options.Password must be set.");
            if (string.IsNullOrWhiteSpace(options.ServerName) && string.IsNullOrWhiteSpace(options.Host))
                throw new ArgumentException("Either MT4Options.ServerName or MT4Options.Host must be set.");

            var symbol = string.IsNullOrWhiteSpace(options.DefaultSymbol) ? "EURUSD" : options.DefaultSymbol;

            // 2) Create account/service and connect
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});
_logger = loggerFactory.CreateLogger<Program>();

_mt4 = new MT4Account(
    user: options.User,
    password: options.Password,
    logger: loggerFactory.CreateLogger<MT4Account>() // <- logger inside MT4Account
);

_service = new MT4Service(
    _mt4,
    loggerFactory.CreateLogger<MT4Service>()        // <- logger for the service
);

try
{
    _logger.LogInformation("🔌 Connecting to MT4...");

    if (!string.IsNullOrWhiteSpace(options.ServerName))
    {
        await _mt4.ConnectByServerNameAsync(
            serverName: options.ServerName!,
            baseChartSymbol: symbol,
            waitForTerminalIsAlive: true,
            timeoutSeconds: 30,
            cancellationToken: ct
        ).ConfigureAwait(false);
    }
    else
    {
        await _mt4.ConnectByHostPortAsync(
            host: options.Host!,
            port: options.Port,
            baseChartSymbol: symbol,
            waitForTerminalIsAlive: true,
            timeoutSeconds: 30,
            cancellationToken: ct
        ).ConfigureAwait(false);
    }

    _logger.LogInformation("✅ Connected to MT4 server");


                // ============================================
                // ---🚀 Step-by-step execution of methods ---
                //=============================================

                // --- 📂 Account Info ---
                await _service.ShowAccountSummary();

                // --- 📂 Order Operations (read-only) ---
                await _service.ShowOpenedOrders();
                await _service.ShowOpenedOrderTickets();
                await _service.ShowOrdersHistory();   // (once)

                // --- ⚠️ Trading (DANGEROUS) ---
                if (EnableTradingExamples)
                {
                    await _service.ShowOrderSendExample(symbol);

                    // Real tickets are required for the following:
                    // await _service.CloseOrderExample(12345678);
                    // await _service.CloseByOrderExample(12345678, 12345679);
                    // await _service.ShowOrderModifyExample(12345678); // if you add an implementation
                }

                // --- 📂 Market / Symbols ---
                await _service.ShowQuote(symbol);

                // Live first tick per symbol (each has its own small timeout).
                await _service.ShowQuotesMany(new[] { "EURUSD", "GBPUSD", "USDJPY" });

                await _service.ShowQuoteHistory(symbol);             // (once)
                await _service.ShowAllSymbols();
                await _service.ShowTickValues(new[] { "EURUSD", "GBPUSD", "USDJPY" });
                await _service.ShowSymbolParams("EURUSD");
                await _service.ShowSymbolInfo(symbol);

                // Quick live tick: subscribes to `symbol` and prints the first incoming tick,
                // then exits on first tick OR on timeout/cancellation (won't hang indefinitely).
                await _service.ShowRealTimeQuotes(symbol, timeoutSeconds: 5, ct);


                // --- 📂 Streaming / Subscriptions ---
                if (EnableStreams && !ct.IsCancellationRequested)
                {            
                    // Live ticks for a fixed time        
                    await _service.StreamQuotesForSymbolsAsync(new[] { "EURUSD", "GBPUSD" }, durationSeconds: 10);

                    // Demo streams: examples exit after the first message
                    await _service.StreamTradeUpdates();
                    await _service.StreamOpenedOrderProfits();
                    await _service.StreamOpenedOrderTickets();
                }
            }
            catch (ApiExceptionMT4 ex)
            {
                _logger.LogError(ex, "MT4 API error: {Code}", ex.ErrorCode);
                throw;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("⏹️ Canceled by user.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error occurred");
                throw;
            }
            finally
            {
                try
                {
                    _mt4?.Disconnect();
                }
                catch { /* ignoring shutdown errors */ }
            }
        }

        private void Configure()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
