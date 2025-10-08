using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using MetaRPC.CSharpMT4;
using static MetaRPC.CSharpMT4.ConsoleUi;
using Microsoft.Extensions.Configuration;
using MetaRPC.CSharpMT4.Helpers;
using Grpc.Core;

static class Program
{
    static async Task<int> Main()
    {
        // Ctrl+C → graceful cancellation
        var appToken = Shutdown.HookCtrlC();

        // ---------------------------------------------------------------------
        // CONFIG
        // ---------------------------------------------------------------------
        var cfg = new ConfigurationBuilder()
            .AddEnvironmentVariables() // lower priority
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .Build();

        var mt4 = cfg.GetSection("MT4Options").Get<Mt4Options>() ?? new Mt4Options();
        mt4.Grpc ??= cfg["Grpc"] ?? "https://mt4.mrpc.pro:443";  // flat fallback
        if (string.IsNullOrWhiteSpace(mt4.Symbol)) mt4.Symbol = "EURUSD";
        if (mt4.TimeoutSeconds <= 0) mt4.TimeoutSeconds = 60;
        if (mt4.ConnectRetries <= 0) mt4.ConnectRetries = 3;

        // Basic validation
        if (mt4.User == 0)                             { Console.Error.WriteLine("Invalid MT4Options.User"); return 2; }
        if (string.IsNullOrWhiteSpace(mt4.Password))   { Console.Error.WriteLine("Invalid MT4Options.Password"); return 2; }
        if (string.IsNullOrWhiteSpace(mt4.ServerName)) { Console.Error.WriteLine("Invalid MT4Options.ServerName"); return 2; }
        if (string.IsNullOrWhiteSpace(mt4.Grpc))       { Console.Error.WriteLine("Invalid Grpc endpoint"); return 2; }

        Console.WriteLine($"CFG → user={mt4.User}, server={mt4.ServerName}, host={(mt4.Host ?? "(null)")}:{(mt4.Port?.ToString() ?? "(null)")}, grpc={mt4.Grpc}, symbol={mt4.Symbol}, timeout={mt4.TimeoutSeconds}s, retries={mt4.ConnectRetries}");

        // ---------------------------------------------------------------------
        // LOGGER + ACCOUNT
        // ---------------------------------------------------------------------
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; })
                .SetMinimumLevel(LogLevel.Information);
        });
        var logger = loggerFactory.CreateLogger<MT4Service>();

        await using var account = new MT4Account(mt4.User, mt4.Password, mt4.Grpc!);

        Box("CONNECT");

        // ---------------------------------------------------------------------
        // CONNECT (Host:Port → ServerName)
        // ---------------------------------------------------------------------
        var connected = false;

        // A) Optional Host:Port first (only if allowed and provided)
        if (!mt4.ForceServerNameOnly && !string.IsNullOrWhiteSpace(mt4.Host) && mt4.Port is > 0)
        {
            connected = await ConnectHostPortPhasedAsync(account, mt4, logger, appToken);
        }

        // B) ServerName (primary path)
        if (!connected)
        {
            connected = await ConnectServerNamePhasedAsync(account, mt4, logger, appToken);
        }

        if (!connected)
        {
            Console.Error.WriteLine("Failed to connect via both Host/Port and ServerName.");
            return 3;
        }

        // Final quick readiness (usually instant if WAIT succeeded)
        await ReadyWaiter.WaitTerminalReadyAsync(
            account, logger,
            timeout: TimeSpan.FromSeconds(Math.Min(mt4.TimeoutSeconds, 180)),
            poll: TimeSpan.FromSeconds(2),
            ct: appToken
        );

        // ---------------------------------------------------------------------
        // MAIN CALLS (PRIMARY DEMO SEQUENCE)
        // ---------------------------------------------------------------------
        // Keep this list compact and obvious — this is what reviewers will run.
        var svc = new MT4Service(account, logger);

        await svc.AccountSummary();                   // account info snapshot
        await svc.Quote(mt4.Symbol!);                 // single quote (base symbol)

        // Try multi-symbol first tick batch; fallback to per-symbol on brokers that deny charts
        try
        {
            await svc.QuotesMany(new[] { "EURUSD", "GBPUSD", "USDJPY" }, timeoutSecondsPerSymbol: 5);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "QuotesMany failed. Falling back to per-symbol RealTimeQuotes…");
            foreach (var s in new[] { "EURUSD", "GBPUSD", "USDJPY" })
            {
                try { await svc.RealTimeQuotes(s, timeoutSeconds: 5); }
                catch (Exception ex2) { logger.LogWarning(ex2, "Per-symbol RealTimeQuotes fallback failed for {Symbol}", s); }
            }
        }

        await svc.QuoteHistory(mt4.Symbol!);          // recent history for base symbol
        await svc.RealTimeQuotes(mt4.Symbol!, timeoutSeconds: 5);  // live stream (base)
        await svc.AllSymbols();                       // list all symbols
        await svc.SymbolParams(mt4.Symbol!);          // symbol parameters (digits, lot step, etc.)
        await svc.SymbolInfo(mt4.Symbol!);            // extended symbol info
        await svc.TickValues(new[] { "EURUSD", "GBPUSD", "USDJPY" }); // last tick values
        await svc.OpenedOrders();                     // open orders list
        await svc.OpenedOrderTickets();               // open orders ticket IDs
        await svc.OrdersHistory();                    // closed orders history

        // Some hosts deny multi-stream via chart open → ensure per-symbol path still works
        foreach (var s in new[] { "EURUSD", "GBPUSD" })
        {
            try { await svc.RealTimeQuotes(s, timeoutSeconds: 5); }
            catch (Exception ex) { logger.LogWarning(ex, "Per-symbol stream fallback failed for {Symbol}", s); }
        }

        // Non-invasive streams (safe to keep running)
        await svc.StreamTradeUpdates(appToken);
        await svc.StreamOpenedOrderProfits(appToken);
        await svc.StreamOpenedOrderTickets(appToken);

        // ---------------------------------------------------------------------
        // TRADING DEMOS (OFF BY DEFAULT)
        // ---------------------------------------------------------------------
        var doTradingDemos =
            cfg.GetValue<bool>("RunTradingDemos", false) ||
            string.Equals(Environment.GetEnvironmentVariable("RUN_TRADING_DEMOS"), "1", StringComparison.OrdinalIgnoreCase);

        if (doTradingDemos)
        {
            // Use master password if you enable this
            await svc.OrderSendExample(mt4.Symbol!);

            int ticketToModify = 0; // set real ticket to test modification/close
            if (ticketToModify > 0)
            {
                await svc.OrderModifyExample(ticketToModify, newStopLoss: 0.0, newTakeProfit: 0.0);
                await svc.CloseOrderExample(ticketToModify);
            }
        }

        return 0;
    }

    // =====================================================================
    // Connection phases: KICK (no wait) → short settle → WAIT (require alive)
    // Retries/backoff + rich RPC diagnostics.
    // =====================================================================
    #region HELPERS

    private static async Task<bool> ConnectHostPortPhasedAsync(MT4Account account, Mt4Options opt, ILogger log, CancellationToken ct)
    {
        // PHASE A: KICK (waitAlive=false)
        var kickOk = await RunWithRetriesAsync(
            retries: Math.Max(2, opt.ConnectRetries),
            backoffSecondsPerAttempt: 2,
            ct: ct,
            step: async attempt =>
            {
                int tmo = Math.Min(Math.Max(opt.TimeoutSeconds / 3, 15) * attempt, 60);
                Console.WriteLine($"Connecting by HostPort: '{opt.Host}:{opt.Port}' (waitForTerminalIsAlive=false, timeout={tmo}s) …");
                try
                {
                    await account.ConnectByHostPortAsync(opt.Host!, opt.Port!.Value, opt.Symbol!, waitForTerminalIsAlive: false, timeoutSeconds: tmo);
                    Console.WriteLine("Connected by HostPort (KICK).");
                }
                catch (Exception ex)
                {
                    DumpRpc("ConnectByHostPort(KICK)", ex, log);
                    if (IsDeadTerminalStop(ex))
                    {
                        log.LogWarning("Pool is stopping a dead terminal (HostPort). Cooldown 15s…");
                        await SmallDelayAsync(TimeSpan.FromSeconds(15), ct);
                    }
                    throw;
                }
            });

        if (!kickOk) return false;

        await SmallDelayAsync(TimeSpan.FromSeconds(5), ct); // allow bootstrap

        // PHASE B: WAIT (waitAlive=true)
        var waitOk = await RunWithRetriesAsync(
            retries: Math.Max(3, opt.ConnectRetries),
            backoffSecondsPerAttempt: 3,
            ct: ct,
            step: async attempt =>
            {
                int tmo = Math.Min(opt.TimeoutSeconds * attempt, 180);
                Console.WriteLine($"Connecting by HostPort: '{opt.Host}:{opt.Port}' (waitForTerminalIsAlive=true, timeout={tmo}s) …");
                try
                {
                    await account.ConnectByHostPortAsync(opt.Host!, opt.Port!.Value, opt.Symbol!, waitForTerminalIsAlive: true, timeoutSeconds: tmo);
                    Console.WriteLine("Connected by HostPort.");
                }
                catch (Exception ex)
                {
                    DumpRpc("ConnectByHostPort(WAIT)", ex, log);
                    if (IsDeadTerminalStop(ex))
                    {
                        log.LogWarning("Dead terminal detected (HostPort WAIT). Cooldown 15s…");
                        await SmallDelayAsync(TimeSpan.FromSeconds(15), ct);
                    }
                    else if (IsReadinessProbeFailed(ex))
                    {
                        log.LogWarning("Readiness probe failed (HostPort). Additional cooldown 10s…");
                        await SmallDelayAsync(TimeSpan.FromSeconds(10), ct);
                    }
                    throw;
                }
            });

        return waitOk;
    }

    private static async Task<bool> ConnectServerNamePhasedAsync(MT4Account account, Mt4Options opt, ILogger log, CancellationToken ct)
    {
        // PHASE A: KICK
        var kickOk = await RunWithRetriesAsync(
            retries: Math.Max(2, opt.ConnectRetries),
            backoffSecondsPerAttempt: 2,
            ct: ct,
            step: async attempt =>
            {
                int tmo = Math.Min(Math.Max(opt.TimeoutSeconds / 3, 15) * attempt, 60);
                Console.WriteLine($"Connecting by ServerName: '{opt.ServerName}' (waitForTerminalIsAlive=false, timeout={tmo}s) …");
                try
                {
                    await account.ConnectByServerNameAsync(opt.ServerName, opt.Symbol!, waitForTerminalIsAlive: false, timeoutSeconds: tmo);
                    Console.WriteLine("Connected by ServerName (KICK).");
                }
                catch (Exception ex)
                {
                    DumpRpc("ConnectByServerName(KICK)", ex, log);
                    if (IsDeadTerminalStop(ex))
                    {
                        log.LogWarning("Pool is stopping a dead terminal (ServerName). Cooldown 15s…");
                        await SmallDelayAsync(TimeSpan.FromSeconds(15), ct);
                    }
                    throw;
                }
            });

        if (!kickOk) return false;

        await SmallDelayAsync(TimeSpan.FromSeconds(5), ct);

        // PHASE B: WAIT
        var waitOk = await RunWithRetriesAsync(
            retries: Math.Max(3, opt.ConnectRetries),
            backoffSecondsPerAttempt: 3,
            ct: ct,
            step: async attempt =>
            {
                int tmo = Math.Min(opt.TimeoutSeconds * attempt, 180);
                Console.WriteLine($"Connecting by ServerName: '{opt.ServerName}' (waitForTerminalIsAlive=true, timeout={tmo}s) …");
                try
                {
                    await account.ConnectByServerNameAsync(opt.ServerName, opt.Symbol!, waitForTerminalIsAlive: true, timeoutSeconds: tmo);
                    Console.WriteLine("Connected by ServerName.");
                }
                catch (Exception ex)
                {
                    DumpRpc("ConnectByServerName(WAIT)", ex, log);
                    if (IsDeadTerminalStop(ex))
                    {
                        log.LogWarning("Dead terminal detected (ServerName WAIT). Cooldown 15s…");
                        await SmallDelayAsync(TimeSpan.FromSeconds(15), ct);
                    }
                    else if (IsReadinessProbeFailed(ex))
                    {
                        log.LogWarning("Readiness probe failed (ServerName). Additional cooldown 10s…");
                        await SmallDelayAsync(TimeSpan.FromSeconds(10), ct);
                    }
                    throw;
                }
            });

        return waitOk;
    }

    // --- mini infra (retry, delays, classification, diagnostics) ---

    private static async Task<bool> RunWithRetriesAsync(int retries, int backoffSecondsPerAttempt, CancellationToken ct, Func<int, Task> step)
    {
        if (retries < 1) retries = 1;
        var rnd = new Random();
        for (int attempt = 1; attempt <= retries; attempt++)
        {
            if (ct.IsCancellationRequested) return false;
            try
            {
                await step(attempt);
                return true;
            }
            catch
            {
                if (attempt >= retries) break;
                var delay = TimeSpan.FromSeconds(backoffSecondsPerAttempt * attempt)
                          + TimeSpan.FromMilliseconds(rnd.Next(250, 1250));
                await SmallDelayAsync(delay, ct);
            }
        }
        return false;
    }

    private static async Task SmallDelayAsync(TimeSpan delay, CancellationToken ct)
    {
        var end = DateTime.UtcNow + delay;
        while (DateTime.UtcNow < end)
        {
            if (ct.IsCancellationRequested) return;
            var slice = end - DateTime.UtcNow;
            if (slice <= TimeSpan.Zero) break;
            await Task.Delay(slice > TimeSpan.FromMilliseconds(250) ? TimeSpan.FromMilliseconds(250) : slice);
        }
    }

    private static bool IsDeadTerminalStop(Exception ex)
        => ex.ToString().IndexOf("TERMINAL_MANAGER_STOPPING_DEAD_TERMINAL", StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool IsReadinessProbeFailed(Exception ex)
        => ex.ToString().IndexOf("TERMINAL_INSTANCE_READINESS_PROBE_FAILED", StringComparison.OrdinalIgnoreCase) >= 0;

    private static void DumpRpc(string phase, Exception ex, ILogger logger)
    {
        logger.LogWarning(ex, "Transport error at phase {Phase}", phase);
        try
        {
            if (ex is RpcException rex)
            {
                logger.LogWarning("{Phase}: RPC {Code} - {Detail}", phase, rex.StatusCode, rex.Status.Detail);
                if (rex.Trailers != null && rex.Trailers.Count > 0)
                    foreach (var t in rex.Trailers)
                        logger.LogWarning("Trailer: {Key} = {Value}", t.Key, t.Value);
                if (rex.Status.DebugException != null)
                    logger.LogWarning(rex.Status.DebugException, "DebugException from server at phase {Phase}", phase);
            }
        }
        catch { /* best-effort */ }
    }

    #endregion
}
