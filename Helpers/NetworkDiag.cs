using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MetaRPC.CSharpMT4.Helpers;

/// <summary>
/// Tiny TCP reachability probe.
/// Opens a raw TCP socket to (host, port) and returns true if the connection
/// is established within the given timeout. This does NOT validate TLS/HTTP/HTTP2,
/// credentials, or application protocol — only the TCP handshake.
///
/// ============================================================================
/// SIGNATURE / CONTRACT
/// ----------------------------------------------------------------------------
/// | Method                                 | Purpose
/// |----------------------------------------|----------------------------------|
/// | CheckTcpAsync(host, port, timeout, ct) | Return true if TCP connects in time
///
/// Parameters
/// | Name     | Type        | Required | Meaning
/// |----------|-------------|----------|-----------------------------------------|
/// | host     | string      | yes      | DNS name or IP to test                   |
/// | port     | int         | yes      | TCP port                                 |
/// | timeout  | TimeSpan    | yes      | Max time to wait for connect             |
/// | ct       | CancellationToken | no | External cancellation                    |
///
/// Returns
/// | Type | Meaning
/// |------|--------------------------------------------------------------|
/// | bool | true = TCP connected (socket.Connected == true); false otherwise
///
/// Notes / Caveats
/// • Success means only: TCP handshake completed. TLS/HTTP2/gRPC may still fail.
/// • DNS failure / firewall drop / closed port → false.
/// • On timeout we cancel a wait task; disposing TcpClient aborts pending connect.
/// ============================================================================
///
/// Usage example
///   var ok = await NetworkDiag.CheckTcpAsync("example.com", 443, TimeSpan.FromSeconds(2));
///   if (!ok) { /* skip Host:Port path, try ServerName instead */ }
/// </summary>
public static class NetworkDiag
{
    public static async Task<bool> CheckTcpAsync(string host, int port, TimeSpan timeout, CancellationToken ct = default)
    {
        try
        {
            using var client = new TcpClient();

            // Start connect and a timeout wait tied to ct.
            var connectTask = client.ConnectAsync(host, port);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);

            var completed = await Task.WhenAny(connectTask, Task.Delay(Timeout.Infinite, cts.Token));

            // If connectTask won, ensure socket is connected.
            return completed == connectTask && client.Connected;
        }
        catch
        {
            // DNS errors, socket exceptions, cancellation → treat as unreachable
            return false;
        }
    }
}

