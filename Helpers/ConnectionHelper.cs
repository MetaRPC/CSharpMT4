using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MetaRPC.CSharpMT4;      // ReadyWaiter, MT4Service
using mt4_term_api;           // MT4Account

namespace MetaRPC.CSharpMT4.Helpers;

/// <summary>
/// One-stop connection bootstrap for the examples (mirrors the MT5 ConnectionHelper).
/// Loads options from <see cref="EnvConfig"/>, resolves the API key, auto-provisions a demo
/// account when credentials are missing, connects via the canonical two-phase
/// <see cref="Mt4ConnectHelper"/> and waits for terminal readiness.
/// <para>Keeps <c>Program.cs</c> and the UserCode sandbox down to a single call each.</para>
/// </summary>
public static class ConnectionHelper
{
    /// <summary>Optional API key supplied on the command line (<c>--api-key</c> or first positional arg); overrides config/env.</summary>
    public static string? ApiKeyOverride { get; set; }

    /// <summary>
    /// Loads <see cref="Mt4Options"/> from appsettings/env, resolves the API key
    /// (override → <c>MRPC_API_KEY</c> env → config → "TRIAL") and auto-provisions a demo
    /// account when <see cref="Mt4Options.User"/> is 0 or the password is blank.
    /// </summary>
    public static async Task<Mt4Options> LoadOptionsAsync(ILogger? log = null, CancellationToken ct = default)
    {
        var (opt, _) = EnvConfig.Load();
        if (string.IsNullOrWhiteSpace(opt.Symbol)) opt.Symbol = "EURUSD";

        // API key precedence: CLI override → environment → config value → TRIAL.
        if (!string.IsNullOrWhiteSpace(ApiKeyOverride))
            opt.ApiKey = ApiKeyOverride!;
        else
        {
            var envKey = Environment.GetEnvironmentVariable("MRPC_API_KEY");
            if (!string.IsNullOrWhiteSpace(envKey)) opt.ApiKey = envKey!;
            else if (string.IsNullOrWhiteSpace(opt.ApiKey)) opt.ApiKey = "TRIAL";
        }

        // Auto-provision a demo account if credentials are missing. On failure we warn and fall
        // back to config credentials instead of aborting with a fake "#0" success.
        if (opt.User == 0 || string.IsNullOrWhiteSpace(opt.Password))
        {
            try
            {
                Console.WriteLine("Auto-provisioning live demo account...");
                var (demoUser, demoPassword, demoServer) =
                    await OpenDemoAccountAsync(opt.Grpc!, string.IsNullOrWhiteSpace(opt.ServerName) ? "MetaQuotes-Demo" : opt.ServerName, opt.ApiKey);
                opt.User = demoUser;
                opt.Password = demoPassword;
                if (!string.IsNullOrEmpty(demoServer)) opt.ServerName = demoServer;
                Console.WriteLine($"✓ Live Demo Account Provisioned: #{opt.User} (Server: {opt.ServerName})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Auto-provision failed: {ex.Message} Falling back to config credentials.");
            }
        }

        return opt;
    }

    /// <summary>Creates an <see cref="MT4Account"/> for the given options, connects it (two-phase) and waits until ready.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the connection cannot be established.</exception>
    public static async Task<MT4Account> CreateAndConnectAccountAsync(Mt4Options opt, ILogger log, CancellationToken ct = default)
    {
        var account = new MT4Account(opt.User, opt.Password, opt.Grpc!, apiKey: opt.ApiKey);

        var ok = await Mt4ConnectHelper.ConnectAsync(account, opt, log, ct);
        if (!ok)
        {
            await SafeDisconnectAsync(account);
            throw new InvalidOperationException("Failed to connect to the MT4 terminal (both ServerName and Host/Port paths).");
        }

        await ReadyWaiter.WaitTerminalReadyAsync(
            account, log,
            timeout: TimeSpan.FromSeconds(Math.Min(opt.TimeoutSeconds, 180)),
            poll: TimeSpan.FromSeconds(2),
            ct: ct);

        return account;
    }

    /// <summary>Convenience overload for the sandbox: loads+validates options then connects, in one call.</summary>
    public static async Task<MT4Account> CreateAndConnectAccountAsync(ILogger log, CancellationToken ct = default)
    {
        var opt = await LoadOptionsAsync(log, ct);
        var err = opt.ValidateOrError();
        if (err.Length > 0) throw new InvalidOperationException(err);
        return await CreateAndConnectAccountAsync(opt, log, ct);
    }

    /// <summary>Gracefully disconnects an account (no-op on null; swallows shutdown errors).</summary>
    public static Task DisconnectAsync(MT4Account? account) => SafeDisconnectAsync(account);

    private static async Task SafeDisconnectAsync(MT4Account? account)
    {
        if (account is null) return;
        try { await account.DisconnectAsync(); } catch { /* best-effort */ }
    }

    /// <summary>
    /// Opens a live demo account through the broker's REST endpoint, derived from the configured gRPC
    /// endpoint so provisioning follows the same environment (prod vs staging) the SDK is pointed at.
    /// </summary>
    public static async Task<(ulong login, string password, string server)> OpenDemoAccountAsync(string grpcEndpoint, string server = "MetaQuotes-Demo", string apiKey = "TRIAL")
    {
        string restBase = "https://mt4.mrpc.pro";
        if (!string.IsNullOrWhiteSpace(grpcEndpoint) && Uri.TryCreate(grpcEndpoint, UriKind.Absolute, out var gu))
            restBase = $"{gu.Scheme}://{gu.Host}";

        using var http = new System.Net.Http.HttpClient();
        http.DefaultRequestHeaders.Add("APIKey", apiKey);
        var url = $"{restBase}/DemoAccount/Open?server={Uri.EscapeDataString(server)}";
        var jsonStr = await http.GetStringAsync(url);
        using var doc = System.Text.Json.JsonDocument.Parse(jsonStr);
        var root = doc.RootElement;

        // The endpoint answers HTTP 200 even on refusal: resultCode != 0 with the reason in debugLog.
        if (root.TryGetProperty("resultCode", out var rcElem) && rcElem.TryGetInt32(out var rc) && rc != 0)
        {
            var reason = root.TryGetProperty("debugLog", out var dbgElem) ? dbgElem.GetString() : null;
            throw new Exception($"demo provisioning refused (resultCode={rc}): {(string.IsNullOrWhiteSpace(reason) ? "no detail" : reason)}");
        }

        ulong login = 0;
        if (root.TryGetProperty("login", out var loginElem))
        {
            if (loginElem.ValueKind == System.Text.Json.JsonValueKind.Number)
                login = loginElem.GetUInt64();
            else if (loginElem.ValueKind == System.Text.Json.JsonValueKind.String && ulong.TryParse(loginElem.GetString(), out var parsedLogin))
                login = parsedLogin;
        }
        if (login == 0)
            throw new Exception("demo provisioning returned no login.");

        var password = root.TryGetProperty("password", out var pwdElem) ? (pwdElem.GetString() ?? "") : "";
        var srv = root.TryGetProperty("server", out var srvElem) && !string.IsNullOrEmpty(srvElem.GetString())
            ? srvElem.GetString()!
            : server;
        return (login, password, srv);
    }
}
