using System;
using System.Threading;
using System.Threading.Tasks;

namespace MetaRPC.CSharpMT4.Helpers;

/// <summary>
/// Robust async retry helper with backoff + jitter.
/// Never throws TaskCanceledException from internal delays; respects external CancellationToken.
/// Returns true on the first successful attempt; false on exhaustion or cancellation.
/// 
/// ============================================================================
/// SIGNATURE
/// ----------------------------------------------------------------------------
/// bool RunAsync(
///     Func<int, Task> action,                   // your async work; gets 1-based attempt index
///     int retries,                              // total attempts (>=1)
///     Func<int, TimeSpan>? backoff = null,      // backoff(attempt) -> delay; default 3s * attempt
///     Action<Exception,int>? onError = null,    // callback on each failure
///     CancellationToken ct = default)
///
/// RETURNS:
///   true  => action succeeded at some attempt
///   false => cancelled or all attempts failed
///
/// BACKOFF:
///   - Uses provided backoff(attempt) or default (3s * attempt),
///   - Adds 250..1250 ms jitter,
///   - Sleeps in small slices (≤250ms) to avoid TaskCanceledException on ct cancellation.
/// 
/// EXAMPLE:
///   var ok = await Retry.RunAsync(
///       async attempt => { await account.ConnectByServerNameAsync(...); },
///       retries: 5,
///       backoff: a => TimeSpan.FromSeconds(2 * a),
///       onError: (ex, a) => log.LogWarning(ex, "connect attempt {a} failed", a),
///       ct: appToken);
///   if (!ok) return; // bail out
/// ============================================================================
public static class Retry
{
    private static readonly Random _rng = new();

    // action gets attempt index (1..retries)
    public static async Task<bool> RunAsync(
        Func<int, Task> action,
        int retries,
        Func<int, TimeSpan>? backoff = null,
        Action<Exception,int>? onError = null,
        CancellationToken ct = default)
    {
        for (int attempt = 1; attempt <= retries; attempt++)
        {
            if (ct.IsCancellationRequested)
                return false;

            try
            {
                await action(attempt);
                return true;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex, attempt);
                if (attempt == retries) break;

                // compute backoff with jitter
                var delay = backoff?.Invoke(attempt) ?? TimeSpan.FromSeconds(3 * attempt);
                delay += TimeSpan.FromMilliseconds(_rng.Next(250, 1250));

                // If cancellation requested during backoff, exit without throwing.
                var end = DateTime.UtcNow + delay;
                while (DateTime.UtcNow < end)
                {
                    if (ct.IsCancellationRequested)
                        return false;

                    var slice = end - DateTime.UtcNow;
                    if (slice <= TimeSpan.Zero) break;

                    // sleep in small slices without linking ct to avoid TaskCanceledException
                    await Task.Delay(slice > TimeSpan.FromMilliseconds(250) ? TimeSpan.FromMilliseconds(250) : slice);
                }
            }
        }
        return false;
    }
}
