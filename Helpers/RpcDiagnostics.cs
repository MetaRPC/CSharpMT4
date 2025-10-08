using System;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace MetaRPC.CSharpMT4.Helpers;

/// <summary>
/// Centralized gRPC error logging.
/// Prints StatusCode, Detail, Trailers and DebugException for RpcException;
/// otherwise logs a generic transport error. Keeps logs consistent across phases
/// (e.g., "KICK", "WAIT", "RunAll", etc.).
///
/// ============================================================================
/// API
/// ----------------------------------------------------------------------------
/// | Method                          | Purpose
/// |---------------------------------|-----------------------------------------|
/// | Dump(Exception ex, ILogger log, string phase)
///                                   | Log a readable diagnostic line (and trailers)
///
/// Parameters
/// | Name   | Type           | Required | Description
/// |--------|----------------|----------|-----------------------------------------|
/// | ex     | Exception      | yes      | Exception to inspect (may be RpcException)
/// | logger | ILogger        | yes      | Destination logger
/// | phase  | string         | yes      | Logical phase/context (e.g., "KICK")
///
/// Behavior
/// • If ex is RpcException:
///     - Log warning: "{Phase}: RPC error {Code} - {Detail}"
///     - For each trailer: "Trailer: {Key} = {Value}"
///     - If DebugException present: "{Phase}: DebugException from server"
/// • Else:
///     - Log warning: "{Phase}: transport error"
///
/// Notes
/// • Use at catch sites inside retries or connect phases to keep logs uniform.
/// • Trailers frequently carry server-side diagnostic hints (e.g., error IDs).
/// ============================================================================
///
/// Example:
/// try { await account.ConnectByServerNameAsync(...); }
/// catch (Exception ex)
/// {
///     RpcDiagnostics.Dump(ex, log, "KICK attempt 1");
///     // decide whether to retry or bail out...
/// }
/// </summary>
public static class RpcDiagnostics
{
    public static void Dump(Exception ex, ILogger logger, string phase)
    {
        if (ex is RpcException rex)
        {
            logger.LogWarning(rex, "{Phase}: RPC error {Code} - {Detail}", phase, rex.StatusCode, rex.Status.Detail);

            if (rex.Trailers != null && rex.Trailers.Count > 0)
            {
                foreach (var t in rex.Trailers)
                    logger.LogWarning("Trailer: {Key} = {Value}", t.Key, t.Value);
            }

            if (rex.Status.DebugException != null)
                logger.LogWarning(rex.Status.DebugException, "{Phase}: DebugException from server", phase);
        }
        else
        {
            logger.LogWarning(ex, "{Phase}: transport error", phase);
        }
    }
}

