using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using mt4_term_api;
using MetaRPC.CSharpMT4;

namespace MetaRPC.CSharpMT4
{
    /// <summary>
    /// Console helpers (single place for pretty headers and small utilities).
    /// </summary>
    public static class ConsoleUi
    {
        public static void Box(string title, int width = 76)
        {
            var content = $" {title} ";
            var pad = Math.Max(0, width - 2 - content.Length);
            Console.WriteLine("┌" + new string('─', width - 2) + "┐");
            Console.WriteLine("│" + content + new string(' ', pad) + "│");
            Console.WriteLine("└" + new string('─', width - 2) + "┘");
        }

        public static void KV(string key, object? value) =>
            Console.WriteLine($"{key}: {value}");
    }

    /// <summary>
    /// Strongly-typed MT4 settings + configuration loader (ENV overrides JSON).
    /// </summary>
    public sealed class Mt4Settings
    {
        public string Login { get; init; } = "";
        public string Password { get; init; } = "";
        public string Server { get; init; } = "";
        public string Grpc { get; init; } = "https://mt4.mrpc.pro:443";
        public string Symbol { get; init; } = "EURUSD";

        public bool TryGetLogin(out ulong login) => ulong.TryParse(Login, out login);

        public string? Validate()
        {
            if (string.IsNullOrWhiteSpace(Login))    return "Login is empty";
            if (string.IsNullOrWhiteSpace(Password)) return "Password is empty";
            if (string.IsNullOrWhiteSpace(Server))   return "Server is empty";
            return null;
        }
    }

    public static class ConfigHelper
    {
        public static IConfiguration BuildConfiguration(string? basePath = null) =>
            new ConfigurationBuilder()
                .SetBasePath(basePath ?? Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

        /// <summary>
        /// ENV has priority over JSON (section MT4:*).
        /// </summary>
public static Mt4Settings GetMt4Settings(IConfiguration cfg)
{
    string? env(string k) => Environment.GetEnvironmentVariable(k);

    var mt4 = cfg.GetSection("MT4");          
    var opt = cfg.GetSection("MT4Options");  

    return new Mt4Settings
    {
        Login    = env("MT4_LOGIN")    ?? mt4["Login"]     ?? opt["User"]       ?? "",
        Password = env("MT4_PASSWORD") ?? mt4["Password"]  ?? opt["Password"]   ?? "",
        Server   = env("MT4_SERVER")   ?? mt4["Server"]    ?? opt["ServerName"] ?? "",
        Grpc     = env("GRPC_SERVER")  ?? mt4["Grpc"]      ?? cfg["Grpc"]       ?? "https://mt4.mrpc.pro:443",
        Symbol   = env("SYMBOL")       ?? mt4["Symbol"]    ?? cfg["Symbol"]     ?? "EURUSD",
    };
}

    }

    /// <summary>
    /// Waits until terminal becomes ready by polling AccountSummary.
    /// </summary>
    public static class ReadyWaiter
    {
        public static async Task WaitTerminalReadyAsync(
            MT4Account account,
            ILogger? logger,
            TimeSpan timeout,
            TimeSpan? poll = null,
            CancellationToken ct = default)
        {
            var period = poll ?? TimeSpan.FromSeconds(2);
            var deadline = DateTime.UtcNow + timeout;

            while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
            {
                try
                {
                    var s = await account.AccountSummaryAsync();
                    logger?.LogInformation("Terminal READY. Balance={Balance} {Currency}", s.AccountBalance, s.AccountCurrency);
                    return;
                }
                catch (ApiExceptionMT4)
                {
                    await Task.Delay(period, ct);
                }
            }

            throw new TimeoutException($"Terminal did not become ready in {timeout.TotalSeconds:F0}s.");
        }
    }

    /// <summary>
    /// Lightweight retry with exponential backoff and jitter.
    /// </summary>
    public static class Retry
    {
        private static readonly Random _rnd = new Random();

        public static async Task<T> RunAsync<T>(
            Func<Task<T>> action,
            int attempts = 3,
            int firstDelayMs = 300,
            double backoff = 2.0,
            ILogger? logger = null,
            CancellationToken ct = default)
        {
            if (attempts <= 0) throw new ArgumentOutOfRangeException(nameof(attempts));

            var delay = firstDelayMs;
            Exception? last = null;

            for (int i = 1; i <= attempts; i++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    return await action();
                }
                catch (Exception ex) when (IsTransient(ex))
                {
                    last = ex;
                    if (i == attempts) break;

                    var jitter = _rnd.Next(25, 125) / 100.0; // 0.25x..1.25x
                    var sleep = TimeSpan.FromMilliseconds(delay * jitter);
                    logger?.LogWarning(ex, "Retry {Attempt}/{Attempts} after {Delay}ms", i, attempts, (int)sleep.TotalMilliseconds);
                    await Task.Delay(sleep, ct);
                    delay = (int)(delay * backoff);
                }
            }

            throw last ?? new Exception("Retry failed without exception?");
        }

        public static Task RunAsync(
            Func<Task> action,
            int attempts = 3,
            int firstDelayMs = 300,
            double backoff = 2.0,
            ILogger? logger = null,
            CancellationToken ct = default)
            => RunAsync(async () => { await action(); return true; }, attempts, firstDelayMs, backoff, logger, ct);

        private static bool IsTransient(Exception ex) =>
            ex is ApiExceptionMT4 ||
            ex is TimeoutException ||
            ex is TaskCanceledException ||
            ex is OperationCanceledException;
    }

    /// <summary>
    /// Cache symbol parameters (e.g., digits) to help format/round prices.
    /// </summary>
    public static class SymbolCache
    {
        private static readonly ConcurrentDictionary<string, int> _digits = new(StringComparer.OrdinalIgnoreCase);

        public static async Task<int> GetDigitsAsync(MT4Account mt4, string symbol)
        {
            if (_digits.TryGetValue(symbol, out var d)) return d;
            var info = await mt4.SymbolParamsManyAsync(symbol);
            foreach (var s in info.SymbolInfos)
            {
                _digits[s.SymbolName] = s.Digits;
                if (string.Equals(s.SymbolName, symbol, StringComparison.OrdinalIgnoreCase))
                    d = s.Digits;
            }
            return d == 0 ? 5 : d; // sane default
        }
    }

    /// <summary>
    /// Utilities to build valid trading requests with proper rounding.
    /// </summary>
    public static class OrderUtils
    {
        public static double RoundToDigits(double value, int digits)
        {
            var scale = Math.Pow(10, digits);
            return Math.Round(value * scale) / scale;
        }


        /// <summary>
        /// Builds a valid <see cref="OrderSendRequest"/> for the symbol: rounds price
        /// to the symbol’s digits (via <c>SymbolCache</c>), sets int slippage, magic and comment.
        /// For market orders pass/keep <c>price = 0</c> (server-side fill).
        /// </summary>
        /// <remarks>Use before <c>MT4Account.OrderSendAsync</c>; SL/TP are set later via OrderModify.</remarks>
        /// <param name="symbol">Trading symbol.</param>
        /// <param name="op">Operation type (BUY/SELL/... ).</param>
        /// <returns>Ready-to-send <see cref="OrderSendRequest"/>.</returns>

        public static async Task<OrderSendRequest> BuildSendAsync(
    MT4Account mt4,
    string symbol,
    OrderSendOperationType op,
    double volume,
    double price = 0,
    int slippage = 5,
    int magicNumber = 0,
    string? comment = null)
        {
            var digits = await SymbolCache.GetDigitsAsync(mt4, symbol);
            double r(double v) => RoundToDigits(v, digits);

            var req = new OrderSendRequest
            {
                Symbol = symbol,
                OperationType = op,
                Volume = volume,
                Price = price > 0 ? r(price) : 0, 
                Slippage = slippage,
                MagicNumber = magicNumber,
                Comment = comment ?? string.Empty
            };

            return req;
        }


        /// <summary>
        /// Builds an <see cref="OrderModifyRequest"/> to set SL/TP/expiration for a ticket.
        /// Rounds price levels to the symbol’s digits via <c>SymbolCache</c>.
        /// Intended to be called right after OrderSend, since SL/TP aren’t part of OrderSendRequest.
        /// </summary>
        /// <param name="ticket">Order ticket to update.</param>
        /// <param name="symbol">Symbol used to determine price precision.</param>
        /// <returns>Ready-to-send <see cref="OrderModifyRequest"/> for <c>OrderModifyAsync</c>.</returns>

        public static async Task<OrderModifyRequest> BuildStopsModifyAsync(
            MT4Account mt4,
            int ticket,
            string symbol,
            double? stopLoss = null,
            double? takeProfit = null,
            DateTime? expiration = null)
        {
            var digits = await SymbolCache.GetDigitsAsync(mt4, symbol);
            double r(double v) => RoundToDigits(v, digits);

            var req = new OrderModifyRequest { OrderTicket = ticket };
            if (stopLoss.HasValue) req.NewStopLoss = r(stopLoss.Value);
            if (takeProfit.HasValue) req.NewTakeProfit = r(takeProfit.Value);
            if (expiration.HasValue) req.NewExpiration =
                Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                    DateTime.SpecifyKind(expiration.Value, DateTimeKind.Utc));

            return req;
        }


        /// <summary>
        /// Builds an <see cref="OrderCloseDeleteRequest"/> for a given ticket,
        /// used to close market orders or delete pending ones.
        /// Validates that the ticket fits into Int32 (MT4 API requirement).
        /// </summary>
        /// <param name="ticket">Order ticket (will be cast to int if valid).</param>
        /// <returns>Ready-to-send request for <c>MT4Account.OrderCloseDeleteAsync</c>.</returns>
        /// <exception cref="OverflowException">If the ticket is outside Int32 range.</exception>

        public static OrderCloseDeleteRequest BuildCloseDelete(long ticket)
        {
            if (ticket > int.MaxValue || ticket < int.MinValue)
                throw new OverflowException("Ticket value is out of int range!");
            return new OrderCloseDeleteRequest { OrderTicket = (int)ticket };
        }


        /// <summary>
        /// Builds an <see cref="OrderCloseByRequest"/> to close a position by an opposite one.
        /// Validates that both tickets fit into Int32 (MT4 API requirement).
        /// </summary>
        /// <param name="ticketToClose">Ticket of the position to be closed.</param>
        /// <param name="oppositeTicket">Ticket of the opposite position used to close.</param>
        /// <returns>Ready-to-send request for <c>MT4Account.OrderCloseByAsync</c>.</returns>
        /// <exception cref="OverflowException">If any ticket is outside Int32 range.</exception>

        public static OrderCloseByRequest BuildCloseBy(long ticketToClose, long oppositeTicket)
        {
            if (ticketToClose > int.MaxValue || ticketToClose < int.MinValue ||
                oppositeTicket > int.MaxValue || oppositeTicket < int.MinValue)
                throw new OverflowException("One of the tickets is out of int range!");
            return new OrderCloseByRequest
            {
                TicketToClose = (int)ticketToClose,
                OppositeTicketClosingBy = (int)oppositeTicket
            };
        }


        /// <summary>
        /// Builds a generic <see cref="OrderModifyRequest"/> with optional new price, SL/TP and expiration.
        /// Converts <paramref name="newExpiration"/> to UTC protobuf Timestamp. No digit rounding here.
        /// </summary>
        /// <param name="ticket">Order ticket to modify (int).</param>
        /// <returns>Ready-to-send request for <c>MT4Account.OrderModifyAsync</c>.</returns>
        /// <remarks>For SL/TP with proper price precision use <c>BuildStopsModifyAsync</c>.</remarks>

        public static OrderModifyRequest BuildModify(
            int ticket,
            double? newPrice = null,
            double? newStopLoss = null,
            double? newTakeProfit = null,
            DateTime? newExpiration = null)
        {
            var req = new OrderModifyRequest { OrderTicket = ticket };
            if (newPrice.HasValue) req.NewPrice = newPrice.Value;
            if (newStopLoss.HasValue) req.NewStopLoss = newStopLoss.Value;
            if (newTakeProfit.HasValue) req.NewTakeProfit = newTakeProfit.Value;
            if (newExpiration.HasValue) req.NewExpiration = Timestamp.FromDateTime(DateTime.SpecifyKind(newExpiration.Value, DateTimeKind.Utc));
            return req;
        }
    }


    /// <summary>
    /// Hooks Console.CancelKeyPress (Ctrl+C) and returns a CancellationToken
    /// that is canceled on the first Ctrl+C press (prevents immediate exit).
    /// Use this token to gracefully stop streams/long-running async operations.
    /// </summary>
    /// <returns>A CancellationToken canceled when the user presses Ctrl+C.</returns>
    /// <remarks>Sets <c>e.Cancel = true</c> to avoid abrupt termination; call once per process.</remarks>

    public static class Shutdown
    {
        public static CancellationToken HookCtrlC()
        {
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };
            return cts.Token;
        }
    }
}