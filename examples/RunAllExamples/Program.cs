// File: examples/RunAllExamples/Program.cs
// Build: dotnet build
// Run:   dotnet run --project .\examples\RunAllExamples\RunAllExamples.csproj

#nullable enable

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;                         // RpcException / StatusCode
using MetaRPC.CSharpMT4;                // MT4Account
using MetaRPC.CSharpMT4.Examples;      // MT4Account_RunAllLowLevel

static class EnvUtil
{
    public static string Env(string name, string def = "") =>
        Environment.GetEnvironmentVariable(name) ?? def;

    public static ulong EnvUlong(params (string key, ulong def)[] options)
    {
        foreach (var (key, _) in options)
            if (ulong.TryParse(Env(key), out var v)) return v;
        return options.Length > 0 ? options[0].def : 0UL;
    }

    public static int EnvInt(string name, int def) =>
        int.TryParse(Env(name), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : def;
}

class Program
{
    public static async Task<int> Main()
    {
        Console.WriteLine("== MT4 Low-Level Full Run ==");

        // ---- ENV (simple & explicit) ----
        // Required:
        var user       = EnvUtil.EnvUlong(("MT4_USER", 0UL), ("MT4_LOGIN", 0UL));
        var password   = EnvUtil.Env("MT4_PASSWORD");
        var grpcServer = EnvUtil.Env("GRPC_SERVER");            // e.g. http://localhost:5000 or https://host:443
        var serverName = EnvUtil.Env("MT4_SERVER_NAME", EnvUtil.Env("MT4_SERVER"));

        // Optional:
        var baseSymbolRaw = EnvUtil.Env("BASE_CHART_SYMBOL", "EURUSD");
        var symbolsCsvRaw = EnvUtil.Env("SYMBOLS", "EURUSD,GBPUSD,USDJPY");

        var connectTimeout = EnvUtil.EnvInt("CONNECT_TIMEOUT_SECONDS", 180);
        var warmupPollMs   = EnvUtil.EnvInt("WARMUP_POLL_MS", 1200);
        var retryAttempts  = EnvUtil.EnvInt("CONNECT_RETRIES", 5);

        // Basic validation
        if (user == 0 || string.IsNullOrWhiteSpace(password))
        {
            Console.Error.WriteLine("ENV MT4_USER/MT4_LOGIN and MT4_PASSWORD are required.");
            return 2;
        }
        if (string.IsNullOrWhiteSpace(grpcServer))
        {
            Console.Error.WriteLine("ENV GRPC_SERVER is required (e.g., http://localhost:5000 or https://host:443).");
            return 2;
        }
        if (!grpcServer.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            grpcServer = "https://" + grpcServer;
        if (string.IsNullOrWhiteSpace(serverName))
        {
            Console.Error.WriteLine("ENV MT4_SERVER_NAME (or MT4_SERVER) is required (e.g., MetaQuotes-Demo).");
            return 2;
        }

        Console.WriteLine($"Using gRPC: {grpcServer}");
        Console.WriteLine($"ServerName: {serverName}");
        Console.WriteLine($"Timeout: {connectTimeout}s; Retries: {retryAttempts}");

        using var ctsAll = new CancellationTokenSource(TimeSpan.FromMinutes(6));
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; ctsAll.Cancel(); };

        await using var account = new MT4Account(user, password, grpcServer);

        // ---- PHASE 1: Connect (by ServerName) with simple retries, soft-failing on "terminal not ready" ----
        var connected = await ConnectByServerNameWithRetriesAsync(
            account, serverName, baseSymbolRaw, retryAttempts, connectTimeout, ctsAll.Token);

        if (!connected)
        {
            Console.Error.WriteLine("Connect failed after retries.");
            return 3;
        }

        // ---- PHASE 2: Warmup (probe AccountSummary until terminal becomes ready) ----
        var ready = await WaitTerminalReadyAsync(
            account,
            maxSeconds: connectTimeout,
            pollMs: warmupPollMs,
            ct: ctsAll.Token);

        if (!ready)
        {
            Console.Error.WriteLine("Terminal did not become ready in the warmup window.");
            return 4;
        }

        // ---- PHASE 3: Symbol remap (handle broker suffixes like EURUSD.m) ----
        var (baseSymbol, symbolsCsv) = await PickSymbolsAsync(account, baseSymbolRaw, symbolsCsvRaw, ctsAll.Token);
        Environment.SetEnvironmentVariable("BASE_CHART_SYMBOL", baseSymbol);
        Environment.SetEnvironmentVariable("SYMBOLS", symbolsCsv);

        // ---- PHASE 4: Run the low-level example suite ----
        var rc = await MT4Account_RunAllLowLevel.RunAllAsync(account, ctsAll.Token);
        Console.WriteLine($"RunAll finished with code {rc}.");
        return rc;
    }

    // --- helpers ---

    static async Task<bool> ConnectByServerNameWithRetriesAsync(
        MT4Account account,
        string serverName,
        string baseSymbol,
        int attempts,
        int timeoutSeconds,
        CancellationToken ct)
    {
        Exception? last = null;

        for (int i = 1; i <= attempts && !ct.IsCancellationRequested; i++)
        {
            try
            {
                Console.WriteLine($"[{i}/{attempts}] Connecting by ServerName: {serverName} (timeout={timeoutSeconds}s) …");
                await account.ConnectByServerNameAsync(
                    serverName: serverName,
                    baseChartSymbol: baseSymbol,
                    waitForTerminalIsAlive: false,   // soft kick
                    timeoutSeconds: timeoutSeconds,
                    cancellationToken: ct);
                Console.WriteLine("Connected (kick ok).");
                return true;
            }
            catch (Exception ex)
            {
                // Treat readiness-probe errors as soft-fail → warmup will handle it.
                if (IsTerminalProbeError(ex))
                {
                    Console.WriteLine($"Connect soft-fail (terminal not ready yet): {ex.Message}");
                    return true;
                }

                last = ex;
                var backoff = Math.Min(8000, 500 * (int)Math.Pow(2, i - 1));
                Console.WriteLine($"Connect failed: {ex.GetType().Name}: {ex.Message}. Retry in {backoff}ms…");
                try { await Task.Delay(backoff, ct); } catch { /* ignore */ }
            }
        }

        if (last != null) Console.WriteLine("Last connect error: " + last.Message);
        return false;
    }

    static async Task<bool> WaitTerminalReadyAsync(
        MT4Account acc,
        int maxSeconds,
        int pollMs,
        CancellationToken ct)
    {
        var until = DateTime.UtcNow.AddSeconds(maxSeconds);
        Exception? last = null;

        while (DateTime.UtcNow < until && !ct.IsCancellationRequested)
        {
            try
            {
                await acc.AccountSummaryAsync(deadline: null, cancellationToken: ct);
                Console.WriteLine("Warmup OK: terminal responded.");
                return true;
            }
            catch (RpcException rx) when (rx.StatusCode == StatusCode.Unavailable)
            {
                last = rx;
                await Task.Delay(pollMs, ct);
            }
            catch (Exception ex)
            {
                last = ex;
                await Task.Delay(pollMs, ct);
            }
        }

        if (last != null) Console.WriteLine("[warmup] last error: " + last.Message);
        return false;
    }

    static bool IsTerminalProbeError(Exception ex)
    {
        var s = ex.ToString();
        return s.IndexOf("TERMINAL_INSTANCE_READINESS_PROBE_FAILED", StringComparison.OrdinalIgnoreCase) >= 0
            || s.IndexOf("Terminal API didn't response properly", StringComparison.OrdinalIgnoreCase) >= 0
            || s.IndexOf("didn't response properly", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static async Task<(string baseSymbol, string symbolsCsv)> PickSymbolsAsync(
        MT4Account acc, string baseSymbolRaw, string symbolsCsvRaw, CancellationToken ct)
    {
        try
        {
            var symsMsg = await acc.SymbolsAsync(deadline: null, cancellationToken: ct);

            var all = symsMsg?.GetType().GetProperties()
                         .Select(p => p.GetValue(symsMsg))
                         .OfType<System.Collections.IEnumerable>()
                         .SelectMany(x => x.Cast<object>())
                         .Select(o =>
                         {
                             var prop = o.GetType().GetProperty("SymbolName")
                                     ?? o.GetType().GetProperty("Name")
                                     ?? o.GetType().GetProperty("Symbol");
                             return prop?.GetValue(o)?.ToString() ?? "";
                         })
                         .Where(s => !string.IsNullOrWhiteSpace(s))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .ToArray() ?? Array.Empty<string>();

            string pickOne(string raw)
            {
                if (all.Length == 0) return raw;
                var exact = all.FirstOrDefault(s => s.Equals(raw, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(exact)) return exact;
                var pref = all.FirstOrDefault(s => s.StartsWith(raw, StringComparison.OrdinalIgnoreCase));
                return string.IsNullOrEmpty(pref) ? raw : pref;
            }

            var basePicked  = pickOne(baseSymbolRaw);
            var symbolsPicked = string.Join(",",
                symbolsCsvRaw.Split(',')
                             .Select(s => s.Trim())
                             .Where(s => s.Length > 0)
                             .Select(pickOne)
                             .Distinct(StringComparer.OrdinalIgnoreCase));

            if (!basePicked.Equals(baseSymbolRaw, StringComparison.OrdinalIgnoreCase))
                Console.WriteLine($"[symbol remap] {baseSymbolRaw} → {basePicked}");
            if (!symbolsPicked.Equals(symbolsCsvRaw, StringComparison.OrdinalIgnoreCase))
                Console.WriteLine($"[symbol remap] {symbolsCsvRaw} → {symbolsPicked}");

            return (basePicked, symbolsPicked);
        }
        catch
        {
            // Fallback to raw values if anything goes wrong
            return (baseSymbolRaw, symbolsCsvRaw);
        }
    }
}

//  set envs (example)
// $env:MT4_USER="168418518"
// $env:MT4_PASSWORD="rlmj5or"      # read-only
// $env:GRPC_SERVER="http://localhost:5000"
// $env:MT4_SERVER_NAME="MetaQuotes-Demo"

//  run
// dotnet run -c Release --project .\examples\RunAllExamples\RunAllExamples.csproj
