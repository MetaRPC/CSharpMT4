using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MetaRPC.CSharpMT4.Helpers;

/// <summary>
/// Connects to MT4 via gRPC using a two-phase strategy:
///   PHASE 1 (KICK):   Start/attach terminal instance without waiting for readiness.
///   PHASE 2 (WAIT):   Re-connect and wait until terminal becomes "alive".
///
/// This helper centralizes retries, backoffs and common transient conditions
/// (dead instance cooldown, readiness probe failures).
///
/// ============================================================================
/// USAGE
/// ----------------------------------------------------------------------------
/// var (opt, cfg) = EnvConfig.Load();
/// await using var account = new MT4Account(opt.User, opt.Password, opt.Grpc!);
/// var ok = await Mt4ConnectHelper.ConnectAsync(account, opt, logger, ct);
/// if (!ok) return 2; // bail out
/// ============================================================================
///
/// ============================================================================
/// INPUTS / OUTPUTS
/// ----------------------------------------------------------------------------
/// | Param     | Type         | Description
/// |-----------|--------------|-----------------------------------------------|
/// | account   | MT4Account   | Connected via gRPC client (user/password/grpc)|
/// | opt       | Mt4Options   | ServerName/Symbol/Timeouts/Host/Port flags    |
/// | log       | ILogger      | Logging sink                                  |
/// | ct        | CancellationToken | App cancellation token                  |
/// | returns   | bool         | true = connected & ready; false = failed      |
/// ============================================================================
///
/// ============================================================================
/// BEHAVIOR SUMMARY
/// ----------------------------------------------------------------------------
/// | Phase | Call                                     | waitAlive | Timeout pattern             | Retries | Backoff         | Notes
/// |------:|------------------------------------------|----------:|-----------------------------|--------:|-----------------|-------------------------------|
/// |  1    | ConnectByServerNameAsync()               |   false   | 15s,30s,45s,60s (per attempt)|  ≥2     | 2s * attempt    | Quick "kick" to spin up pool  |
/// |  1b   | ConnectByHostPortAsync()  (optional)     |   false   | same as above               |  ≥2     | 2s * attempt    | Only if ForceServerNameOnly=false + Host/Port set
/// |  —    | settle delay                             |    —      | 5s                          |   —     | —               | Let terminal finish bootstrap |
/// |  2    | ConnectByServerNameAsync()               |   true    | 60s → 120s → 180s           |  ≥3     | 3s * attempt    | Now we expect readiness OK    |
///
/// Handles:
///   • "TERMINAL_MANAGER_STOPPING_DEAD_TERMINAL" → 15s cooldown, then retry
///   • "TERMINAL_INSTANCE_READINESS_PROBE_FAILED" → 10s cooldown (WAIT phase), then retry
/// ============================================================================
///
/// Dependencies:
///   - Retry.RunAsync(...)        : retry helper (Helpers/Retry.cs)
///   - RpcDiagnostics.Dump(...)   : structured gRPC error logging (Helpers/RpcDiagnostics.cs)
///   - NetworkDiag.CheckTcpAsync(): optional Host:Port reachability (Helpers/NetworkDiag.cs)
/// </summary>
public static class Mt4ConnectHelper
{
    public static async Task<bool> ConnectAsync(MT4Account account, Mt4Options opt, ILogger log, CancellationToken ct)
    {
        // Phase 0: basic sanity
        if (string.IsNullOrWhiteSpace(opt.ServerName))
        {
            log.LogError("ServerName is required.");
            return false;
        }

        // === PHASE 1: KICK (do not wait for alive) ===
        // Goal: ask pool to spin up terminal instance quickly, avoid failing on readiness probe inside ConnectEx.
        var kickOk = await Retry.RunAsync(
            action: async attempt =>
            {
                int tmo = Math.Min(Math.Max(opt.TimeoutSeconds / 3, 15) * attempt, 60); // 15s, 30s, 45s, 60s...
                log.LogInformation("KICK: ConnectByServerName(waitAlive=false) server='{Server}', timeout={Timeout}s (attempt {Attempt})",
                    opt.ServerName, tmo, attempt);

                try
                {
                    await account.ConnectByServerNameAsync(
                        serverName: opt.ServerName,
                        baseChartSymbol: opt.Symbol!,
                        waitForTerminalIsAlive: false, // <— key difference
                        timeoutSeconds: tmo
                    );
                }
                catch (Exception ex)
                {
                    var msg = ex.ToString();
                    if (msg.Contains("TERMINAL_MANAGER_STOPPING_DEAD_TERMINAL", StringComparison.OrdinalIgnoreCase))
                    {
                        log.LogWarning("Pool is stopping a dead terminal. Cooldown 15s before next KICK…");
                        await Task.Delay(TimeSpan.FromSeconds(15), ct);
                    }
                    throw;
                }
            },
            retries: Math.Max(2, opt.ConnectRetries),                               // give a couple of KICKs
            backoff: attempt => TimeSpan.FromSeconds(2 * attempt),                  // small backoff
            onError: (ex, attempt) => RpcDiagnostics.Dump(ex, log, $"KICK attempt {attempt}"),
            ct: ct
        );

        if (!kickOk)
        {
            log.LogError("KICK phase failed — cannot start terminal instance.");
            if (opt.ForceServerNameOnly)
                return false;

            // Optional Host:Port KICK if allowed
            if (!string.IsNullOrWhiteSpace(opt.Host) && opt.Port is > 0)
            {
                var tcpOk = await NetworkDiag.CheckTcpAsync(opt.Host!, opt.Port!.Value, TimeSpan.FromSeconds(3), ct);
                if (!tcpOk)
                {
                    log.LogWarning("TCP check failed for {Host}:{Port}. Skipping HostPort KICK.", opt.Host, opt.Port);
                }
                else
                {
                    kickOk = await Retry.RunAsync(
                        action: async attempt =>
                        {
                            int tmo = Math.Min(Math.Max(opt.TimeoutSeconds / 3, 15) * attempt, 60);
                            log.LogInformation("KICK: ConnectByHostPort(waitAlive=false) {Host}:{Port}, timeout={Timeout}s (attempt {Attempt})",
                                opt.Host, opt.Port, tmo, attempt);

                            try
                            {
                                await account.ConnectByHostPortAsync(
                                    host: opt.Host!,
                                    port: opt.Port!.Value,
                                    baseChartSymbol: opt.Symbol!,
                                    waitForTerminalIsAlive: false, // <— key difference
                                    timeoutSeconds: tmo
                                );
                            }
                            catch (Exception ex)
                            {
                                var msg = ex.ToString();
                                if (msg.Contains("TERMINAL_MANAGER_STOPPING_DEAD_TERMINAL", StringComparison.OrdinalIgnoreCase))
                                {
                                    log.LogWarning("Pool is stopping a dead terminal (HostPort). Cooldown 15s before next KICK…");
                                    await Task.Delay(TimeSpan.FromSeconds(15), ct);
                                }
                                throw;
                            }
                        },
                        retries: Math.Max(2, opt.ConnectRetries),
                        backoff: attempt => TimeSpan.FromSeconds(2 * attempt),
                        onError: (ex, attempt) => RpcDiagnostics.Dump(ex, log, $"KICK HostPort attempt {attempt}"),
                        ct: ct
                    );
                }
            }

            if (!kickOk) return false;
        }

        // Small settle delay after KICK so the instance can bootstrap MQL/feeds.
        await Task.Delay(TimeSpan.FromSeconds(5), ct);

        // === PHASE 2: WAIT (now ask to wait for alive) ===
        // Here we request readiness and expect it to succeed once the instance is fully up.
        var connected = await Retry.RunAsync(
            action: async attempt =>
            {
                int tmo = Math.Min(opt.TimeoutSeconds * attempt, 180); // 60 → 120 → 180...
                log.LogInformation("WAIT: ConnectByServerName(waitAlive=true) server='{Server}', timeout={Timeout}s (attempt {Attempt})",
                    opt.ServerName, tmo, attempt);

                try
                {
                    await account.ConnectByServerNameAsync(
                        serverName: opt.ServerName,
                        baseChartSymbol: opt.Symbol!,
                        waitForTerminalIsAlive: true,
                        timeoutSeconds: tmo
                    );
                }
                catch (Exception ex)
                {
                    var msg = ex.ToString();
                    if (msg.Contains("TERMINAL_MANAGER_STOPPING_DEAD_TERMINAL", StringComparison.OrdinalIgnoreCase))
                    {
                        log.LogWarning("Pool is stopping a dead terminal during WAIT. Cooldown 15s…");
                        await Task.Delay(TimeSpan.FromSeconds(15), ct);
                    }
                    else if (msg.Contains("TERMINAL_INSTANCE_READINESS_PROBE_FAILED", StringComparison.OrdinalIgnoreCase))
                    {
                        // Give terminal more time to finish bootstrapping between attempts
                        log.LogWarning("Readiness probe failed. Additional cooldown 10s before next WAIT attempt…");
                        await Task.Delay(TimeSpan.FromSeconds(10), ct);
                    }
                    throw;
                }
            },
            retries: Math.Max(3, opt.ConnectRetries),
            backoff: attempt => TimeSpan.FromSeconds(3 * attempt), // 3s, 6s, 9s...
            onError: (ex, attempt) => RpcDiagnostics.Dump(ex, log, $"WAIT attempt {attempt}"),
            ct: ct
        );

        return connected;
    }
}
