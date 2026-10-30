using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MetaRPC.CSharpMT4;
using mt4_term_api;
using MetaRPC.CSharpMT4.Examples.LowLevel;
using MetaRPC.CSharpMT4.Examples.Services;
using MetaRPC.CSharpMT4.Examples.Sugar;
using MetaRPC.CSharpMT4.Examples.Orchestrators;
using MetaRPC.CSharpMT4.Examples.Presets;
using MetaRPC.CSharpMT4.Examples.UserCode;
using MetaRPC.CSharpMT4.Examples.Tools;
using static MetaRPC.CSharpMT4.ConsoleUi;
using MetaRPC.CSharpMT4.Helpers;

static class Program
{
    static async Task<int> Main(string[] args)
    {
        // Ctrl+C → graceful cancellation
        var appToken = Shutdown.HookCtrlC();

        // Protobuf inspector is reflection-only — run it without connecting.
        if (args.Contains("--inspect")) { ProtobufInspector.Run(); return 0; }

        string? apiKeyCli = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--api-key" && i + 1 < args.Length)
            {
                apiKeyCli = args[i + 1];
                break;
            }
            else if (args[i].StartsWith("--api-key="))
            {
                apiKeyCli = args[i].Substring("--api-key=".Length);
                break;
            }
        }
        if (apiKeyCli == null && args.Length > 0 && !args[0].StartsWith("-"))
        {
            apiKeyCli = args[0];
        }

        // ---------------------------------------------------------------------
        // CONFIG + LOGGER
        // ---------------------------------------------------------------------
        ConnectionHelper.ApiKeyOverride = apiKeyCli;

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; })
                .SetMinimumLevel(LogLevel.Information);
        });
        var logger = loggerFactory.CreateLogger<MT4Service>();

        // Load options (appsettings/env), resolve the API key and auto-provision a demo account if needed.
        var mt4 = await ConnectionHelper.LoadOptionsAsync(logger, appToken);

        var err = mt4.ValidateOrError();
        if (err.Length > 0)                      { Console.Error.WriteLine(err); return 2; }
        if (string.IsNullOrWhiteSpace(mt4.Grpc)) { Console.Error.WriteLine("Invalid Grpc endpoint"); return 2; }

        Console.WriteLine($"CFG → user={mt4.User}, server={mt4.ServerName}, host={(mt4.Host ?? "(null)")}:{(mt4.Port?.ToString() ?? "(null)")}, grpc={mt4.Grpc}, symbol={mt4.Symbol}, apiKey={(mt4.ApiKey == "TRIAL" ? "TRIAL" : "***")}, timeout={mt4.TimeoutSeconds}s, retries={mt4.ConnectRetries}");

        MT4Account account;

        Box("CONNECT");
        try
        {
            account = await ConnectionHelper.CreateAndConnectAccountAsync(mt4, logger, appToken);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Connect failed: {ex.Message}");
            return 3;
        }

        try
        {
        // ---------------------------------------------------------------------
        // MAIN CALLS (PRIMARY DEMO SEQUENCE)
        // ---------------------------------------------------------------------
        // Keep this list compact and obvious — this is what reviewers will run.
        var svc = new MT4Service(account);

        var sym = mt4.Symbol ?? "EURUSD";

        // ---------------------------------------------------------------------
        // EXAMPLE MENU. Each entry calls a data-returning MT4Service method and
        // prints the result here (the Service no longer prints). Grouped like the docs.
        // Selection: --example <n|substring>, --all, or an interactive menu.
        // Non-interactive stdin (CI / piped) runs the whole suite so nothing hangs.
        // ---------------------------------------------------------------------
        var examples = new List<(string Name, Func<Task> Run)>
        {
            // ===== 3-tier suites (run a whole level) =====
            ("== 1. LOW-LEVEL: info ==",      () => LowLevelInfo.RunAsync(account, sym, appToken)),
            ("== 2. LOW-LEVEL: trading ==",   () => LowLevelTrading.RunAsync(account, sym, false, appToken)),
            ("== 3. LOW-LEVEL: streaming ==", () => LowLevelStreaming.RunAsync(account, sym, appToken)),
            ("== 4. SERVICE suite (all) ==",  () => ServiceExamples.RunAllAsync(svc, sym, appToken)),
            ("== 5. SUGAR scenario (assess->plan->execute) ==", () => SugarScenario.RunAsync(svc, sym, false, appToken)),
            ("== 6. ORCHESTRATOR: scalp (described) ==",         () => new ScalpOrchestrator(svc) { Symbol = sym, Live = false }.ExecuteAsync(appToken)),
            ("== 7. ORCHESTRATOR: trailing stop (described) ==", () => new TrailingStopOrchestrator(svc) { Symbol = sym, Live = false }.ExecuteAsync(appToken)),
            ("== 8. ORCHESTRATOR: grid (described) ==",          () => new GridOrchestrator(svc) { Symbol = sym, Live = false }.ExecuteAsync(appToken)),
            ("== 9. ORCHESTRATOR: breakout (described) ==",      () => new BreakoutOrchestrator(svc) { Symbol = sym, Live = false }.ExecuteAsync(appToken)),
            ("== 10. PRESET: adaptive mode (described) ==",      () => new AdaptiveModePreset(svc) { Symbol = sym, Live = false }.ExecuteAsync(appToken)),
            ("== 11. USER CODE sandbox ==",                      () => ProgramUserCode.RunAsync(svc, sym, appToken)),
            ("== 12. PROTOBUF inspector (interactive) ==",       () => { ProtobufInspector.Run(); return Task.CompletedTask; }),
        };

        async Task RunOneAsync(string name, Func<Task> run)
        {
            Box(name);
            try { await run(); }
            catch (Exception ex) { logger.LogWarning(ex, "Example '{Name}' failed", name); }
        }
        async Task RunAllAsync()
        {
            foreach (var (name, run) in examples) await RunOneAsync(name, run);
        }

        string? exSel = null;
        var runAll = false;
        foreach (var a in args)
        {
            if (a == "--all") runAll = true;
            else if (a.StartsWith("--example=")) exSel = a.Substring("--example=".Length);
        }

        if (!string.IsNullOrEmpty(exSel))
        {
            var hit = examples.FirstOrDefault(e => e.Name.IndexOf(exSel, StringComparison.OrdinalIgnoreCase) >= 0);
            if (int.TryParse(exSel, out var idx) && idx >= 1 && idx <= examples.Count) hit = examples[idx - 1];
            if (hit.Run == null) { Console.Error.WriteLine($"No example matches '{exSel}'."); return 2; }
            await RunOneAsync(hit.Name, hit.Run);
        }
        else if (runAll || Console.IsInputRedirected)
        {
            await RunAllAsync();
        }
        else
        {
            while (!appToken.IsCancellationRequested)
            {
                Console.WriteLine();
                Console.WriteLine("════════════════ MT4 EXAMPLES (Service) ════════════════");
                for (int i = 0; i < examples.Count; i++)
                    Console.WriteLine($"  {i + 1,2}. {examples[i].Name}");
                Console.WriteLine("   a. Run ALL");
                Console.WriteLine("   q. Quit");
                Console.Write("Select: ");
                var input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input) || input.Equals("q", StringComparison.OrdinalIgnoreCase)) break;
                if (input.Equals("a", StringComparison.OrdinalIgnoreCase)) { await RunAllAsync(); continue; }
                if (int.TryParse(input, out var n) && n >= 1 && n <= examples.Count)
                    await RunOneAsync(examples[n - 1].Name, examples[n - 1].Run);
                else
                    Console.WriteLine($"Unknown selection: '{input}'");
            }
        }

        return 0;
    }
    finally
    {
        await ConnectionHelper.DisconnectAsync(account);
    }
    }
}
