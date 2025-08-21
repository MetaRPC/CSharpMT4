using Grpc.Core;
using Grpc.Net.Client;
using mt4_term_api;
using System;
using System.Linq;
using System.Net.Http;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static mt4_term_api.Connection;
using static mt4_term_api.MarketInfo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;



namespace MetaRPC.CSharpMT4
{
    public class MT4Account
    {
        /// <summary>
        /// Gets the MT4 user account number.
        /// </summary>
        public ulong User { get; }

        /// <summary>
        /// Gets the password for the user account.
        /// </summary>
        public string Password { get; }

        /// <summary>
        /// Gets the MT4 server host.
        /// </summary>
        public string? Host { get; private set; }

        /// <summary>
        /// Gets the the MT4 server port.
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Gets the the MT4 server port.
        /// </summary>
        public string? ServerName { get; private set; }
       
       /// <summary>Base chart symbol used on connect (null until connected or after Dispose()).</summary>
        public string? BaseChartSymbol { get; private set; }

        /// <summary>Terminal readiness timeout (seconds).</summary>
        public int ConnectTimeoutSeconds { get; private set; }

        /// <summary>
        /// Gets the gRPC server address used to connect.
        /// </summary>
        public readonly string GrpcServer;

        /// <summary>
        /// Gets the gRPC channel used for communication.
        /// </summary>
        public readonly GrpcChannel GrpcChannel;
        /// <summary>
        /// Gets the gRPC client for connection operations.
        /// </summary>
        public readonly Connection.ConnectionClient ConnectionClient;

        /// <summary>
        /// Gets the gRPC client for subscription services.
        /// </summary>
        public readonly SubscriptionService.SubscriptionServiceClient SubscriptionClient;

        /// <summary>
        /// Gets the gRPC client for account helper operations.
        /// </summary>
        public readonly AccountHelper.AccountHelperClient AccountClient;

        /// <summary>
        /// Gets the gRPC client for trading helper operations.
        /// </summary>
        public readonly TradingHelper.TradingHelperClient TradeClient;

        /// <summary>
        /// Gets the gRPC client for market information queries.
        /// </summary>
        public readonly MarketInfo.MarketInfoClient MarketInfoClient;

        /// <summary>
        /// Gets the unique identifier for the account instance.
        /// </summary>
        public Guid Id { get; private set; } = default;
        
        // How we were connected last time
        private enum ConnectionMode { None, HostPort, ServerName }
        private ConnectionMode _lastConnectionMode = ConnectionMode.None;

        // Keep the last "WaitForTerminalIsAlive" flag to reuse on reconnect
        private bool _lastWaitForTerminalIsAlive = true;

        // put this near other fields in MT4Account
        private const string HeaderIdKey = "id";

        public bool IsConnected => !_disposed && Id != default;

        
        // Max restarts for streaming calls before we give up
        private const int DefaultMaxStreamRestarts = 8;

        private bool _disposed;

        private readonly SemaphoreSlim _reconnectGate = new(1, 1);
     
        // Default per-RPC timeout if caller doesn't provide a deadline
        public TimeSpan DefaultRpcTimeout { get; set; } = TimeSpan.FromSeconds(8);
        
     // Treat some server "errors" as normal stream finalization
        private static bool IsStreamFinalizationError(Mt4TermApi.Error? e)
    => e != null && (
           e.ErrorCode == "ON_SUBSCRIPTION_EA_DEINITIALIZATION_START_WATCHING_MULTI_CHARTS_COUNT_ZERO"
           
       );

        // Default per-RPC timeouts
        public TimeSpan TradeRpcTimeout { get; set; } = TimeSpan.FromSeconds(5); // trading calls


// Resolve effective deadline: use caller's value or our default
        private DateTime? ResolveDeadline(DateTime? deadline)
    => deadline ?? DateTime.UtcNow.Add(DefaultRpcTimeout);

// 2) with a customfallback (for shopping RPC)
        private DateTime? ResolveDeadline(DateTime? deadline, TimeSpan fallback)
    => deadline ?? DateTime.UtcNow.Add(fallback);
        

        // Reset connection-related state so any RPCs will fail fast via EnsureConnected()
        
        
        private void ResetState()

        {
            Id = default;
            Host = null;
            ServerName = null;
            BaseChartSymbol = null;
            ConnectTimeoutSeconds = 0;
            _lastConnectionMode = ConnectionMode.None;
            _lastWaitForTerminalIsAlive = true;
        }


private void EnsureConnected()
        {
            // Guard: do not perform RPC calls without a valid terminal instance Id
            if (!IsConnected)
                throw new ConnectExceptionMT4("Not connected: missing terminal instance Id. Call Connect* first.");
        }

// Replace old GetHeaders() with this version:
private Metadata GetHeaders()
{
    // Ensure we never send an empty Id header
    EnsureConnected();
    return new Metadata { { HeaderIdKey, Id.ToString() } };
}

// Retry/backoff settings for unary RPC calls
private static readonly TimeSpan BaseBackoff = TimeSpan.FromMilliseconds(250);
private static readonly TimeSpan MaxBackoff  = TimeSpan.FromSeconds(5);
private const int DefaultMaxAttempts = 8;

// Decide whether an API error is reconnectable (terminal lost, etc.)
private static bool IsReconnectableError(Mt4TermApi.Error? e)
    => e != null && (e.ErrorCode == "TERMINAL_INSTANCE_NOT_FOUND"
                     || e.ErrorCode == "TERMINAL_REGISTRY_TERMINAL_NOT_FOUND");

// Exponential backoff with jitter
// after (thread-safe on .NET 6+)
private static TimeSpan NextBackoff(int attempt)
{
    var target = Math.Min(
        BaseBackoff.TotalMilliseconds * Math.Pow(2, attempt),
        MaxBackoff.TotalMilliseconds
    );
    var jitter = Random.Shared.Next(-150, 150); // ±150ms
    var ms = Math.Max(100, target + jitter);
    return TimeSpan.FromMilliseconds(ms);
}


/// <summary>
/// Gracefully disconnects and disposes underlying resources. Safe to call multiple times.
/// </summary>
public void Disconnect()
{
    Dispose();
}

/// <summary>
/// Dispose pattern (sync). GrpcChannel supports IDisposable; disposing it will close sockets.
/// </summary>
public void Dispose()
{
    if (_disposed) return;
    _disposed = true;

    try
    {
        // If you have a server-side Disconnect RPC, you can call it here
        // wrapped in try/catch and without throwing on failure.
        // Example (pseudo):
        // var headers = Id != default ? new Metadata { { HeaderIdKey, Id.ToString() } } : null;
        // try { ConnectionClient.Disconnect(new DisconnectRequest(), headers); } catch {}

        // Dispose the channel to release HTTP/2 connections and sockets
        GrpcChannel?.Dispose();
    }
    catch
    {
        // swallow dispose-time exceptions; nothing we can reasonably do here in console apps
    }
    finally
    {
        // Make future calls fail fast with a clear message
        ResetState();
    }
}
public ValueTask DisposeAsync()
{
    Dispose(); // GrpcChannel doesn't need true async dispose
    return ValueTask.CompletedTask;
}


        /// <summary>
        /// Initializes a new instance of the <see cref="MT4Account"/> class using credentials.
        /// </summary>
        /// <param name="user">The MT4 user account number.</param>
        /// <param name="password">The password for the user account.</param>
        /// <param name="grpcServer">The address of the gRPC server (optional).</param>
        /// <param name="id">An optional unique identifier for the account instance.</param>
        
       private readonly Microsoft.Extensions.Logging.ILogger<MT4Account>? _logger;


public MT4Account(ulong user, string password, string? grpcServer = null, Guid id = default,
                  ILogger<MT4Account>? logger = null)
{
    User = user;
    Password = password;
    GrpcServer = grpcServer ?? "https://mt4.mrpc.pro:443";

    // HTTP/2 keepalive to keep long streams healthy behind NAT/firewalls
    var handler = new SocketsHttpHandler
    {
        // Frequency of keepalive pings when the connection is inactive
        KeepAlivePingDelay = TimeSpan.FromSeconds(30),
        // How long have we been waiting for a ping response?
        KeepAlivePingTimeout = TimeSpan.FromSeconds(10),
        // Allow multiple HTTP/2 connections per host (useful for parallel streams)
        EnableMultipleHttp2Connections = true,
        // Slightly reduce the idle timeout so that the runtime does not keep "dead" sockets for too long.
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
    };

    GrpcChannel = GrpcChannel.ForAddress(
        GrpcServer,
        new GrpcChannelOptions { HttpHandler = handler }
    );

    ConnectionClient   = new Connection.ConnectionClient(GrpcChannel);
    SubscriptionClient = new SubscriptionService.SubscriptionServiceClient(GrpcChannel);
    AccountClient      = new AccountHelper.AccountHelperClient(GrpcChannel);
    TradeClient        = new TradingHelper.TradingHelperClient(GrpcChannel);
    MarketInfoClient   = new MarketInfo.MarketInfoClient(GrpcChannel);

    Id = id;
    _logger = logger ?? NullLogger<MT4Account>.Instance;
}
        

      // Reconnect using the last-known mode/params, but serialize attempts
private async Task ReconnectAsync(DateTime? deadline, CancellationToken ct)
{
    await _reconnectGate.WaitAsync(ct).ConfigureAwait(false);
    try
    {
        if (_lastConnectionMode == ConnectionMode.None)
            throw new ConnectExceptionMT4("Cannot reconnect: no previous connection parameters are available.");

        switch (_lastConnectionMode)
        {
            case ConnectionMode.ServerName:
                if (string.IsNullOrWhiteSpace(ServerName))
                    throw new ConnectExceptionMT4("Cannot reconnect via server name: ServerName is empty.");

                await ConnectByServerNameAsync(
                    serverName: ServerName!,
                    baseChartSymbol: BaseChartSymbol ?? "EURUSD",
                    waitForTerminalIsAlive: _lastWaitForTerminalIsAlive,
                    timeoutSeconds: ConnectTimeoutSeconds > 0 ? ConnectTimeoutSeconds : 30,
                    deadline: deadline,
                    cancellationToken: ct
                ).ConfigureAwait(false);
                break;

            case ConnectionMode.HostPort:
                if (string.IsNullOrWhiteSpace(Host) || Port <= 0)
                    throw new ConnectExceptionMT4("Cannot reconnect via host/port: Host or Port is invalid.");

                await ConnectByHostPortAsync(
                    host: Host!, port: Port,
                    baseChartSymbol: BaseChartSymbol ?? "EURUSD",
                    waitForTerminalIsAlive: _lastWaitForTerminalIsAlive,
                    timeoutSeconds: ConnectTimeoutSeconds > 0 ? ConnectTimeoutSeconds : 30,
                    deadline: deadline,
                    cancellationToken: ct
                ).ConfigureAwait(false);
                break;
        }
    }
    finally
    {
        _reconnectGate.Release();
    }
}

        /// <summary>
        /// Connects to the MT4 terminal using credentials provided in the constructor.
        /// </summary>
        /// <param name="host">The IP address or domain of the MT4 server.</param>
        /// <param name="port">The port on which the MT4 server listens (default is 443).</param>
        /// <param name="baseChartSymbol">The base chart symbol to use (e.g., "EURUSD").</param>
        /// <param name="waitForTerminalIsAlive">Whether to wait for terminal readiness before returning.</param>
        /// <param name="timeoutSeconds">How long to wait for terminal readiness before timing out.</param>
        /// <returns>A task representing the asynchronous connection operation.</returns>
        /// <exception cref="ApiExceptionMT4">Thrown if the server returns an error response.</exception>
        /// <exception cref="Grpc.Core.RpcException">Thrown if the gRPC connection fails.</exception>
        public async Task ConnectByHostPortAsync(
    string host,
    int port = 443,
    string baseChartSymbol = "EURUSD",
    bool waitForTerminalIsAlive = true,
    int timeoutSeconds = 30,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(host))
        throw new ArgumentException("Host must be provided.", nameof(host));
    if (port <= 0)
        throw new ArgumentOutOfRangeException(nameof(port), "Port must be > 0.");
    if (timeoutSeconds <= 0)
        throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), "timeoutSeconds must be > 0.");

    var connectRequest = new ConnectRequest
    {
        User = User,
        Password = Password,
        Host = host,
        Port = port,
        BaseChartSymbol = baseChartSymbol,
        WaitForTerminalIsAlive = waitForTerminalIsAlive,
        TerminalReadinessWaitingTimeoutSeconds = timeoutSeconds
    };

    Metadata? headers = Id != default ? new Metadata { { HeaderIdKey, Id.ToString() } } : null;

    RpcException? lastRpcEx = null;

    for (int attempt = 0; attempt < DefaultMaxAttempts && !cancellationToken.IsCancellationRequested; attempt++)
    {
        var callDeadline = deadline ?? DateTime.UtcNow.AddSeconds(Math.Max(timeoutSeconds, 5) + 20);

        try
        {
            _logger?.LogInformation(
                "Connect attempt {Attempt}/{Max} to {Grpc} host={Host}:{Port} (deadline={Deadline:o})",
                attempt + 1, DefaultMaxAttempts, GrpcServer, host, port, callDeadline);

            var res = await ConnectionClient
                .ConnectAsync(connectRequest, headers, callDeadline, cancellationToken)
                .ConfigureAwait(false);

            if (res.Error != null)
                throw new ApiExceptionMT4(res.Error);

            Host = host;
            Port = port;
            BaseChartSymbol = baseChartSymbol;
            ConnectTimeoutSeconds = timeoutSeconds;
            Id = Guid.Parse(res.Data.TerminalInstanceGuid);

            _lastConnectionMode = ConnectionMode.HostPort;
            _lastWaitForTerminalIsAlive = waitForTerminalIsAlive;

            _logger?.LogInformation("Connected. TerminalInstanceGuid={Id}", Id);
            return;
        }
        catch (RpcException ex) when (
            ex.StatusCode == StatusCode.Unavailable ||
            ex.StatusCode == StatusCode.DeadlineExceeded ||
            ex.StatusCode == StatusCode.Internal)
        {
            lastRpcEx = ex;
            _logger?.LogWarning("Connect transport error: {Status}. Retrying...", ex.StatusCode);
            await Task.Delay(NextBackoff(attempt), cancellationToken).ConfigureAwait(false);
        }
    }

    if (cancellationToken.IsCancellationRequested)
        throw new OperationCanceledException("ConnectByHostPortAsync canceled by caller.", cancellationToken);

    throw new TimeoutException(
        $"ConnectByHostPortAsync retry limit exceeded ({DefaultMaxAttempts} attempts). " +
        (lastRpcEx != null ? $"Last gRPC status={lastRpcEx.StatusCode}." : "No transport error captured."));
}


        /// <summary>
        /// Synchronously connects to the MT4 terminal.
        /// </summary>
        /// <param name="host">The MT4 server host.</param>
        /// <param name="port">The port the server listens on.</param>
        /// <param name="baseChartSymbol">Chart symbol to initialize the session.</param>
        /// <param name="waitForTerminalIsAlive">Wait for terminal readiness flag.</param>
        /// <param name="timeoutSeconds">Timeout duration in seconds.</param>
        public void ConnectByHostPort(
            string host,
            int port = 443,
            string baseChartSymbol = "EURUSD",
            bool waitForTerminalIsAlive = true,
            int timeoutSeconds = 30)
        {
            ConnectByHostPortAsync(host, port, baseChartSymbol, waitForTerminalIsAlive, timeoutSeconds).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Connects to the MT4 terminal using credentials provided in the constructor.
        /// </summary>
        /// <param name="serverName">MT4 server name from MT4 Terminal.</param>
        /// <param name="baseChartSymbol">The base chart symbol to use (e.g., "EURUSD").</param>
        /// <param name="waitForTerminalIsAlive">Whether to wait for terminal readiness before returning.</param>
        /// <param name="timeoutSeconds">How long to wait for terminal readiness before timing out.</param>
        /// <returns>A task representing the asynchronous connection operation.</returns>
        /// <exception cref="ApiExceptionMT4">Thrown if the server returns an error response.</exception>
        /// <exception cref="Grpc.Core.RpcException">Thrown if the gRPC connection fails.</exception>
       public async Task ConnectByServerNameAsync(
    string serverName,
    string baseChartSymbol = "EURUSD",
    bool waitForTerminalIsAlive = true,
    int timeoutSeconds = 30,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(serverName))
        throw new ArgumentException("Server name must be provided.", nameof(serverName));
    if (timeoutSeconds <= 0)
        throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), "timeoutSeconds must be > 0.");

    var connectRequest = new ConnectExRequest
    {
        User = User,
        Password = Password,
        MtClusterName = serverName,
        BaseChartSymbol = baseChartSymbol,
        TerminalReadinessWaitingTimeoutSeconds = timeoutSeconds,
        // If supported by proto:
        // WaitForTerminalIsAlive = waitForTerminalIsAlive
    };

    Metadata? headers = Id != default ? new Metadata { { HeaderIdKey, Id.ToString() } } : null;

    RpcException? lastRpcEx = null;

    for (int attempt = 0; attempt < DefaultMaxAttempts && !cancellationToken.IsCancellationRequested; attempt++)
    {
        var callDeadline = deadline ?? DateTime.UtcNow.AddSeconds(Math.Max(timeoutSeconds, 5) + 20);

        try
        {
            _logger?.LogInformation(
                "ConnectEx attempt {Attempt}/{Max} to {Grpc} serverName={Server} (deadline={Deadline:o})",
                attempt + 1, DefaultMaxAttempts, GrpcServer, serverName, callDeadline);

            var res = await ConnectionClient
                .ConnectExAsync(connectRequest, headers, callDeadline, cancellationToken)
                .ConfigureAwait(false);

            if (res.Error != null)
                throw new ApiExceptionMT4(res.Error);

            ServerName = serverName;
            BaseChartSymbol = baseChartSymbol;
            ConnectTimeoutSeconds = timeoutSeconds;
            Id = Guid.Parse(res.Data.TerminalInstanceGuid);

            // Save mode & flag for future reconnects
            _lastConnectionMode = ConnectionMode.ServerName;
            _lastWaitForTerminalIsAlive = waitForTerminalIsAlive;

            _logger?.LogInformation("Connected. TerminalInstanceGuid={Id}", Id);
            return;
        }
        catch (RpcException ex) when (
            ex.StatusCode == StatusCode.Unavailable ||
            ex.StatusCode == StatusCode.DeadlineExceeded ||
            ex.StatusCode == StatusCode.Internal)
        {
            lastRpcEx = ex;
            _logger?.LogWarning("ConnectEx transport error: {Status}. Retrying...", ex.StatusCode);
            await Task.Delay(NextBackoff(attempt), cancellationToken).ConfigureAwait(false);
        }
    }

    if (cancellationToken.IsCancellationRequested)
        throw new OperationCanceledException("ConnectByServerNameAsync canceled by caller.", cancellationToken);

    throw new TimeoutException(
        $"ConnectByServerNameAsync retry limit exceeded ({DefaultMaxAttempts} attempts). " +
        (lastRpcEx != null ? $"Last gRPC status={lastRpcEx.StatusCode}." : "No transport error captured."));
}


        /// <summary>
        /// Synchronously connects to the MT4 terminal.
        /// </summary>
        /// <param name="serverName">MT4 server name from MT4 Terminal.</param>
        /// <param name="baseChartSymbol">Chart symbol to initialize the session.</param>
        /// <param name="waitForTerminalIsAlive">Wait for terminal readiness flag.</param>
        /// <param name="timeoutSeconds">Timeout duration in seconds.</param>
        public void ConnectByServerName(
            string serverName,
            string baseChartSymbol = "EURUSD",
            bool waitForTerminalIsAlive = true,
            int timeoutSeconds = 30)
        {
            ConnectByServerNameAsync(serverName, baseChartSymbol, waitForTerminalIsAlive, timeoutSeconds).GetAwaiter().GetResult();
        }


        // Unary invoker with reconnect + bounded retries + backoff
private async Task<T> ExecuteWithReconnect<T>(
    Func<Metadata, T> grpcCall,
    Func<T, Mt4TermApi.Error?> errorSelector,
    DateTime? deadline,
    CancellationToken ct,
    int maxAttempts = DefaultMaxAttempts)
{
    EnsureConnected(); // do not even try without a valid Id

    RpcException? lastRpcEx = null;

    for (int attempt = 0; attempt < maxAttempts && !ct.IsCancellationRequested; attempt++)
    {
        try
        {
            // 1) Do the call
            var res = grpcCall(GetHeaders());

            // 2) Inspect server-side error in reply envelope
            var err = errorSelector(res);
            if (err != null)
            {
                if (IsReconnectableError(err))
                {
                    // Reconnect and retry with backoff
                    await ReconnectAsync(deadline, ct).ConfigureAwait(false);

_logger?.LogWarning("Unary retry {Attempt}/{Max}. Status={Status}. Backoff={Backoff}ms",
    attempt + 1,
    maxAttempts,
    lastRpcEx?.StatusCode.ToString() ?? "API_ERROR",
    (int)NextBackoff(attempt).TotalMilliseconds);


                    await Task.Delay(NextBackoff(attempt), ct).ConfigureAwait(false);
                    continue;
                }

                // Non-retryable API error: surface immediately
                throw new ApiExceptionMT4(err);
            }

if (lastRpcEx != null)
    _logger?.LogInformation("Unary succeeded after {Attempts} attempt(s).", attempt + 1);

                    // Success
                    return res;
        }
        catch (RpcException ex) when (
            ex.StatusCode == StatusCode.Unavailable ||
            ex.StatusCode == StatusCode.DeadlineExceeded ||
            ex.StatusCode == StatusCode.Internal // often transient in gRPC transports
        )
        {
            lastRpcEx = ex;

            // Attempt to reconnect and retry with backoff
            await ReconnectAsync(deadline, ct).ConfigureAwait(false);
            await Task.Delay(NextBackoff(attempt), ct).ConfigureAwait(false);
            continue;
        }
    }

    // If we fell out of the loop: either canceled or ran out of attempts
    if (ct.IsCancellationRequested)
        throw new OperationCanceledException("Operation canceled by user.", ct);

    throw new TimeoutException(
        $"RPC retry limit exceeded (attempts={maxAttempts}). " +
        (lastRpcEx != null ? $"Last gRPC status={lastRpcEx.StatusCode}." : "No transport error captured.")
    );
}

// Async unary invoker with reconnect, bounded retries, and backoff
private async Task<T> ExecuteWithReconnectAsync<T>(
    Func<Metadata, Task<T>> grpcCall,
    Func<T, Mt4TermApi.Error?> errorSelector,
    DateTime? deadline,
    CancellationToken ct,
    int maxAttempts = DefaultMaxAttempts)
{
    EnsureConnected();

    RpcException? lastRpcEx = null;

    for (int attempt = 0; attempt < maxAttempts && !ct.IsCancellationRequested; attempt++)
    {
        try
        {
            // 1) Await the actual gRPC unary response
            var res = await grpcCall(GetHeaders()).ConfigureAwait(false);

            // 2) Check envelope error from server
            var err = errorSelector(res);
            if (err != null)
            {
                if (IsReconnectableError(err))
                {
                    await ReconnectAsync(deadline, ct).ConfigureAwait(false);
                    await Task.Delay(NextBackoff(attempt), ct).ConfigureAwait(false);
                    continue;
                }

                throw new ApiExceptionMT4(err);
            }

            if (attempt > 0)
                _logger?.LogInformation("Unary succeeded after {Attempts} attempt(s).", attempt + 1);

            return res; // success
        }
        catch (RpcException ex) when (
            ex.StatusCode == StatusCode.Unavailable ||
            ex.StatusCode == StatusCode.DeadlineExceeded ||
            ex.StatusCode == StatusCode.Internal)
        {
            lastRpcEx = ex;
            await ReconnectAsync(deadline, ct).ConfigureAwait(false);
            await Task.Delay(NextBackoff(attempt), ct).ConfigureAwait(false);
        }
    }

    if (ct.IsCancellationRequested)
        throw new OperationCanceledException("Operation canceled by user.", ct);

    throw new TimeoutException(
        $"RPC retry limit exceeded (attempts={maxAttempts}). " +
        (lastRpcEx != null ? $"Last gRPC status={lastRpcEx.StatusCode}." : "No transport error captured.")
    );
}


        /// <summary>
        /// Executes a gRPC server-streaming call with automatic reconnection logic on recoverable errors.
        /// </summary>
        /// <typeparam name="TRequest">The request type sent to the stream method.</typeparam>
        /// <typeparam name="TReply">The reply type received from the stream.</typeparam>
        /// <typeparam name="TData">The extracted data type yielded to the consumer.</typeparam>
        /// <param name="request">The request object to initiate the stream with.</param>
        /// <param name="streamInvoker">
        /// A delegate that opens the stream. It receives the request, metadata headers, and cancellation token, 
        /// and returns an <see cref="Grpc.Core.AsyncServerStreamingCall{TReply}"/>.
        /// </param>
        /// <param name="getErrorCode">
        /// A delegate that extracts the error code (if any) from a <typeparamref name="TReply"/> instance.
        /// Return <c>"TERMINAL_INSTANCE_NOT_FOUND"</c> to trigger reconnection logic, or any non-null code to throw <see cref="ApiExceptionMT4"/>.
        /// </param>
        /// <param name="getData">
        /// A delegate that extracts the data object from a <typeparamref name="TReply"/> instance.
        /// Return <c>null</c> to skip the current message.
        /// </param>
        /// <param name="headers">The gRPC metadata headers to include in the stream request.</param>
        /// <param name="cancellationToken">Optional cancellation token to stop streaming and reconnection attempts.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> of extracted <typeparamref name="TData"/> items streamed from the server.
        /// </returns>
        /// <exception cref="ConnectExceptionMT4">Thrown if reconnection logic fails due to missing account context.</exception>
        /// <exception cref="ApiExceptionMT4">Thrown when the stream response contains a known API error.</exception>
        /// <exception cref="Grpc.Core.RpcException">Thrown if a non-recoverable gRPC error occurs.</exception>
        // Streaming invoker with reconnect, bounded restarts, and backoff
        // Treat some server "errors" as normal stream finalization

private async IAsyncEnumerable<TData> ExecuteStreamWithReconnect<TRequest, TReply, TData>(
    TRequest request,
    Func<TRequest, Metadata, CancellationToken, AsyncServerStreamingCall<TReply>> streamInvoker,
    Func<TReply, Mt4TermApi.Error?> getError,
    Func<TReply, TData?> getData,   // <— допускаем null внутри
    [EnumeratorCancellation] CancellationToken ct = default,
    int maxRestarts = DefaultMaxStreamRestarts)
    where TData : class            
{
    EnsureConnected();

    int restart = 0;

    while (!ct.IsCancellationRequested)
    {
        bool needReconnect = false;
        AsyncServerStreamingCall<TReply>? call = null;

        try
        {
            call = streamInvoker(request, GetHeaders(), ct);
            var response = call.ResponseStream;

            while (true)
            {
                bool moved;
                try
                {
                    moved = await response.MoveNext(ct).ConfigureAwait(false);
                }
                
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && ct.IsCancellationRequested)
                {
                    _logger?.LogInformation("Stream completed. Reason=Cancelled(by token)");
                    yield break;
                }
                // Temporary transport problems — let's try to reconnect
                catch (RpcException ex) when (
                    ex.StatusCode == StatusCode.Unavailable ||
                    ex.StatusCode == StatusCode.DeadlineExceeded ||
                    ex.StatusCode == StatusCode.Internal)
                {
                    needReconnect = true;
                    break;
                }
                // The server sent Cancelled without our cancellation — we consider it a normal termination.
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
                {
                    _logger?.LogInformation("Stream cancelled (Status=Cancelled). Treating as completion.");
                    yield break;
                }

                if (!moved)
                {
                    _logger?.LogInformation("Stream completed. Reason=EOF");
                    yield break;
                }

                var reply = response.Current;
                var err = getError(reply);

                if (err != null)
                {
                    if (IsReconnectableError(err))
                    {
                        needReconnect = true;
                        break;
                    }

                    if (IsStreamFinalizationError(err))
                    {
                        _logger?.LogInformation("Stream finalized by server: {Code}", err.ErrorCode);
                        yield break;
                    }

                    throw new ApiExceptionMT4(err);
                }

                var data = getData(reply); // TData? внутри
                if (data != null)          // наружу — только не-null
                    yield return data;
            }
        }
        finally
        {
            call?.Dispose();
        }

        if (needReconnect)
        {
            if (restart >= maxRestarts)
                throw new TimeoutException($"Stream restart limit exceeded (restarts={maxRestarts}).");

            var backoff = NextBackoff(restart);
            _logger?.LogWarning("Stream restart {Restart}/{Max}. Backoff={Backoff}ms",
                restart + 1, maxRestarts, (int)backoff.TotalMilliseconds);

            await ReconnectAsync(null, ct).ConfigureAwait(false);
            await Task.Delay(backoff, ct).ConfigureAwait(false);
            restart++;
            continue;
        }

        break;
    }
}



        /// <summary>
        /// Subscribes to real-time tick data for specified symbols.
        /// </summary>
        /// <param name="symbols">The symbol names to subscribe to.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Async stream of tick data responses.</returns>
        /// <exception cref="ConnectExceptionMT4">Thrown if the account is not connected.</exception>
        /// <exception cref="Grpc.Core.RpcException">If the stream fails.</exception>
        /// <exception cref="ApiExceptionMT4">Thrown if an error is received from the stream.</exception>
       public async IAsyncEnumerable<OnSymbolTickData> OnSymbolTickAsync(
    IEnumerable<string> symbols,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    EnsureConnected();

    if (symbols is null) throw new ArgumentNullException(nameof(symbols));
    var list = symbols as IList<string> ?? symbols.ToList();
    if (list.Count == 0) throw new ArgumentException("At least one symbol must be provided.", nameof(symbols));

    var req = new OnSymbolTickRequest();
    req.SymbolNames.AddRange(list);

    await foreach (var data in ExecuteStreamWithReconnect<OnSymbolTickRequest, OnSymbolTickReply, OnSymbolTickData>(
        req,
        (r, h, token) => SubscriptionClient.OnSymbolTick(r, h, cancellationToken: token),
        reply => reply.ResponseCase == OnSymbolTickReply.ResponseOneofCase.Error ? reply.Error : null,
        reply => reply.ResponseCase == OnSymbolTickReply.ResponseOneofCase.Data  ? reply.Data  : null,
        ct))
    {
        yield return data;
    }
}


        /// <summary>
        /// Subscribes to real-time updates for open trade operations (orders, positions, history).
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Async stream of trade update data.</returns>
        /// <exception cref="ConnectExceptionMT4">Thrown if the account is not connected.</exception>
        /// <exception cref="Grpc.Core.RpcException">If the stream fails.</exception>
        /// <exception cref="ApiExceptionMT4">Thrown if an error is received from the stream.</exception>
       public async IAsyncEnumerable<OnTradeData> OnTradeAsync(
    [EnumeratorCancellation] CancellationToken ct = default)
{
    EnsureConnected();

    var req = new OnTradeRequest();

    await foreach (var data in ExecuteStreamWithReconnect<OnTradeRequest, OnTradeReply, OnTradeData>(
        req,
        (r, h, token) => SubscriptionClient.OnTrade(r, h, cancellationToken: token),
        reply => reply.ResponseCase == OnTradeReply.ResponseOneofCase.Error ? reply.Error : null,
        reply => reply.ResponseCase == OnTradeReply.ResponseOneofCase.Data  ? reply.Data  : null,
        ct))
    {
        yield return data;
    }
}

        /// <summary>
        /// Subscribes to real-time profit updates for open orders.
        /// </summary>
        /// <param name="intervalMs">Polling interval in milliseconds.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Async stream of profit update snapshots for open orders.</returns>
        /// <exception cref="ConnectExceptionMT4">Thrown if the account is not connected.</exception>
        /// <exception cref="Grpc.Core.RpcException">If the stream fails.</exception>
        /// <exception cref="ApiExceptionMT4">Thrown if the stream returns a known API error.</exception>
        public async IAsyncEnumerable<OnOpenedOrdersProfitData> OnOpenedOrdersProfitAsync(
    int intervalMs = 1000,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    EnsureConnected();
    if (intervalMs <= 0) throw new ArgumentOutOfRangeException(nameof(intervalMs), "intervalMs must be > 0.");

    var req = new OnOpenedOrdersProfitRequest { TimerPeriodMilliseconds = intervalMs };

    await foreach (var data in ExecuteStreamWithReconnect<OnOpenedOrdersProfitRequest, OnOpenedOrdersProfitReply, OnOpenedOrdersProfitData>(
        req,
        (r, h, token) => SubscriptionClient.OnOpenedOrdersProfit(r, h, cancellationToken: token),
        reply => reply.ResponseCase == OnOpenedOrdersProfitReply.ResponseOneofCase.Error ? reply.Error : null,
        reply => reply.ResponseCase == OnOpenedOrdersProfitReply.ResponseOneofCase.Data  ? reply.Data  : null,
        ct))
    {
        yield return data;
    }
}


        /// <summary>
        /// Subscribes to updates of position and pending order ticket IDs.
        /// </summary>
        /// <param name="intervalMs">Polling interval in milliseconds.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Async stream of ticket ID snapshots.</returns>
        /// <exception cref="ConnectExceptionMT4">Thrown if the account is not connected.</exception>
        /// <exception cref="Grpc.Core.RpcException">Thrown if the stream fails.</exception>
        /// <exception cref="ApiExceptionMT4">Thrown if the stream returns a known API error.</exception>
        public async IAsyncEnumerable<OnOpenedOrdersTicketsData> OnOpenedOrdersTicketsAsync(
    int intervalMs = 1000,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    EnsureConnected();
    if (intervalMs <= 0) throw new ArgumentOutOfRangeException(nameof(intervalMs), "intervalMs must be > 0.");

    var req = new OnOpenedOrdersTicketsRequest { PullIntervalMilliseconds = intervalMs };

    await foreach (var data in ExecuteStreamWithReconnect<OnOpenedOrdersTicketsRequest, OnOpenedOrdersTicketsReply, OnOpenedOrdersTicketsData>(
        req,
        (r, h, token) => SubscriptionClient.OnOpenedOrdersTickets(r, h, cancellationToken: token),
        reply => reply.ResponseCase == OnOpenedOrdersTicketsReply.ResponseOneofCase.Error ? reply.Error : null,
        reply => reply.ResponseCase == OnOpenedOrdersTicketsReply.ResponseOneofCase.Data  ? reply.Data  : null,
        ct))
    {
        yield return data;
    }
}


        /// <summary>
        /// Asynchronously retrieves summary information about the currently connected MT4 trading account.
        /// </summary>
        /// <param name="deadline">
        /// Optional gRPC deadline for the request. If not specified, the default timeout will apply.
        /// </param>
        /// <param name="cancellationToken">
        /// Optional <see cref="CancellationToken"/> to cancel the request.
        /// </param>
        /// <returns>
        /// A task that resolves to an <see cref="AccountSummaryData"/> object containing details such as balance, equity, leverage,
        /// currency, server time, and other account attributes.
        /// </returns>
        /// <exception cref="ConnectExceptionMT4">
        /// Thrown if the account is not connected (i.e., connection ID is not initialized).
        /// </exception>
        /// <exception cref="ApiExceptionMT4">
        /// Thrown if the gRPC server returns an error response.
        /// </exception>
        public async Task<AccountSummaryData> AccountSummaryAsync(
    DateTime? deadline = null,
    CancellationToken cancellationToken = default)
{
    EnsureConnected();

    var request = new AccountSummaryRequest();
    var effectiveDeadline = ResolveDeadline(deadline);

    var res = await ExecuteWithReconnectAsync(
        headers => AccountClient
            .AccountSummaryAsync(request, headers, effectiveDeadline, cancellationToken)
            .ResponseAsync, // <- await the actual reply
        r => r.ResponseCase == AccountSummaryReply.ResponseOneofCase.Error ? r.Error : null,
        effectiveDeadline,
        cancellationToken
    ).ConfigureAwait(false);

    return res.Data;
}


        /// <summary>
        /// Synchronously retrieves summary information about the currently connected MT4 trading account.
        /// </summary>
        /// <param name="deadline">
        /// Optional gRPC deadline for the request. If not specified, the default timeout will apply.
        /// </param>
        /// <param name="cancellationToken">
        /// Optional <see cref="CancellationToken"/> to cancel the request.
        /// </param>
        /// <returns>
        /// An <see cref="AccountSummaryData"/> object containing information such as account balance, equity, trade mode,
        /// server time, credit, leverage, and currency.
        /// </returns>
        /// <exception cref="ConnectExceptionMT4">
        /// Thrown if the account is not connected (i.e., connection ID is not initialized).
        /// </exception>
        /// <exception cref="ApiExceptionMT4">
        /// Thrown if the gRPC server returns an error response.
        /// </exception>
        public AccountSummaryData AccountSummary(
            DateTime? deadline = null,
            CancellationToken cancellationToken = default)
        {
            return AccountSummaryAsync(deadline, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously retrieves a list of opened orders for the connected account.
        /// </summary>
        /// <param name="sortType">The sorting method for returned orders (default is by open time ascending).</param>
        /// <param name="deadline">Optional deadline for the gRPC call.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task returning opened orders data.</returns>
        public async Task<OpenedOrdersData> OpenedOrdersAsync(
    EnumOpenedOrderSortType sortType = EnumOpenedOrderSortType.SortByOpenTimeAsc,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default)
{
    EnsureConnected();

    var request = new OpenedOrdersRequest { SortType = sortType };
    var effectiveDeadline = ResolveDeadline(deadline);

    var res = await ExecuteWithReconnectAsync(
        headers => AccountClient
            .OpenedOrdersAsync(request, headers, effectiveDeadline, cancellationToken)
            .ResponseAsync,
        r => r.ResponseCase == OpenedOrdersReply.ResponseOneofCase.Error ? r.Error : null,
        effectiveDeadline,
        cancellationToken
    ).ConfigureAwait(false);

    return res.Data;
}


        /// <summary>
        /// Synchronously retrieves a list of opened orders.
        /// </summary>
        /// <param name="sortType">Sorting method.</param>
        /// <param name="deadline">Optional deadline.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Opened orders data.</returns>
        public OpenedOrdersData OpenedOrders(
            EnumOpenedOrderSortType sortType = EnumOpenedOrderSortType.SortByOpenTimeAsc,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default)
        {
            return OpenedOrdersAsync(sortType, deadline, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously retrieves the ticket IDs of opened orders.
        /// </summary>
        /// <param name="deadline">Optional deadline.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Ticket ID list for opened orders.</returns>
        public async Task<OpenedOrdersTicketsData> OpenedOrdersTicketsAsync(
    DateTime? deadline = null,
    CancellationToken cancellationToken = default)
{
    EnsureConnected();

    var request = new OpenedOrdersTicketsRequest();
    var effectiveDeadline = ResolveDeadline(deadline);

    var res = await ExecuteWithReconnectAsync(
        headers => AccountClient
            .OpenedOrdersTicketsAsync(request, headers, effectiveDeadline, cancellationToken)
            .ResponseAsync,
        r => r.ResponseCase == OpenedOrdersTicketsReply.ResponseOneofCase.Error ? r.Error : null,
        effectiveDeadline,
        cancellationToken
    ).ConfigureAwait(false);

    return res.Data;
}


        /// <summary>
        /// Synchronously retrieves the ticket IDs of opened orders.
        /// </summary>
        /// <param name="deadline">Optional deadline.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Ticket ID list for opened orders.</returns>
        public OpenedOrdersTicketsData OpenedOrdersTickets(
            DateTime? deadline = null,
            CancellationToken cancellationToken = default)
        {
            return OpenedOrdersTicketsAsync(deadline, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously retrieves paginated historical orders based on filter options.
        /// </summary>
        /// <param name="sortType">Sorting method for history.</param>
        /// <param name="from">Start date (UTC).</param>
        /// <param name="to">End date (UTC).</param>
        /// <param name="page">Page number (optional).</param>
        /// <param name="itemsPerPage">Items per page (optional).</param>
        /// <param name="deadline">Optional deadline.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Historical order data.</returns>
public async Task<OrdersHistoryData> OrdersHistoryAsync(
    EnumOrderHistorySortType sortType = EnumOrderHistorySortType.HistorySortByCloseTimeDesc,
    DateTime? from = null,
    DateTime? to = null,
    int? page = null,
    int? itemsPerPage = null,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default)
{
    EnsureConnected();

    // Validate input ranges
    if (page.HasValue && page.Value < 1)
        throw new ArgumentOutOfRangeException(nameof(page), "Page must be >= 1.");
    if (itemsPerPage.HasValue && itemsPerPage.Value < 1)
        throw new ArgumentOutOfRangeException(nameof(itemsPerPage), "ItemsPerPage must be >= 1.");
    if (from.HasValue && to.HasValue && from.Value > to.Value)
        throw new ArgumentException("'from' must be <= 'to'.");

    var request = new OrdersHistoryRequest
    {
        InputSortMode = sortType
    };

    // Normalize to UTC if provided
    if (from.HasValue)
        request.InputFrom = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(from.Value.ToUniversalTime());
    if (to.HasValue)
        request.InputTo = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(to.Value.ToUniversalTime());

    // Paging (only when provided)
    if (page is >= 1)
        request.PageNumber = page.Value;
    if (itemsPerPage is >= 1)
        request.ItemsPerPage = itemsPerPage.Value;

    var effectiveDeadline = ResolveDeadline(deadline);

    var res = await ExecuteWithReconnectAsync(
        headers => AccountClient
            .OrdersHistoryAsync(request, headers, effectiveDeadline, cancellationToken)
            .ResponseAsync,
        r => r.ResponseCase == OrdersHistoryReply.ResponseOneofCase.Error ? r.Error : null,
        effectiveDeadline,
        cancellationToken
    ).ConfigureAwait(false);

    return res.Data;
}



        /// <summary>
        /// Synchronously retrieves paginated historical orders.
        /// </summary>
        /// <param name="sortType">Sorting method.</param>
        /// <param name="from">Start date.</param>
        /// <param name="to">End date.</param>
        /// <param name="page">Page number.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="deadline">Optional deadline.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Historical order data.</returns>
        public OrdersHistoryData OrdersHistory(
            EnumOrderHistorySortType sortType = EnumOrderHistorySortType.HistorySortByCloseTimeDesc,
            DateTime? from = null,
            DateTime? to = null,
            int? page = null,
            int? itemsPerPage = null,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default)
        {
            return OrdersHistoryAsync(sortType, from, to, page, itemsPerPage, deadline, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously retrieves trading symbol parameters for all or a specific symbol.
        /// </summary>
        /// <param name="symbolName">Symbol name to filter by (optional).</param>
        /// <param name="deadline">Optional gRPC deadline.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Symbol parameter details.</returns>
        public async Task<SymbolParamsManyData> SymbolParamsManyAsync(
    string? symbolName = null,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default)
{
    EnsureConnected();

    var request = new SymbolParamsManyRequest();
    if (!string.IsNullOrWhiteSpace(symbolName))
        request.SymbolName = symbolName;

    var effectiveDeadline = ResolveDeadline(deadline);

    var res = await ExecuteWithReconnectAsync(
        headers => AccountClient
            .SymbolParamsManyAsync(request, headers, effectiveDeadline, cancellationToken)
            .ResponseAsync,
        r => r.ResponseCase == SymbolParamsManyReply.ResponseOneofCase.Error ? r.Error : null,
        effectiveDeadline,
        cancellationToken
    ).ConfigureAwait(false);

    return res.Data;
}


        /// <summary>
        /// Synchronously retrieves trading symbol parameters.
        /// </summary>
        /// <param name="symbolName">Symbol name (optional).</param>
        /// <param name="deadline">Deadline (optional).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Symbol parameter details.</returns>
        public SymbolParamsManyData SymbolParamsMany(
            string? symbolName = null,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default)
        {
            return SymbolParamsManyAsync(symbolName, deadline, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously calculates tick value, size, and contract size for specified symbols.
        /// </summary>
        /// <param name="symbolNames">List of symbol names.</param>
        /// <param name="deadline">Optional deadline.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Tick value data for each symbol.</returns>
        public async Task<TickValueWithSizeData> TickValueWithSizeAsync(
    IEnumerable<string> symbolNames,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default)
{
    EnsureConnected();

    if (symbolNames == null)
        throw new ArgumentNullException(nameof(symbolNames), "Symbol list cannot be null.");
    if (!symbolNames.Any())
        throw new ArgumentException("Symbol list cannot be empty.", nameof(symbolNames));

    var request = new TickValueWithSizeRequest();
    request.SymbolNames.AddRange(symbolNames);

    var effectiveDeadline = ResolveDeadline(deadline);

    var res = await ExecuteWithReconnect(
        headers => AccountClient.TickValueWithSize(request, headers, effectiveDeadline, cancellationToken),
        r => r.ResponseCase == TickValueWithSizeReply.ResponseOneofCase.Error ? r.Error : null,
        effectiveDeadline,
        cancellationToken
    ).ConfigureAwait(false);

    return res.Data;
}


        /// <summary>
        /// Synchronously retrieves tick value and size details.
        /// </summary>
        /// <param name="symbolNames">Symbol names to request.</param>
        /// <param name="deadline">Optional deadline.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Tick value result set.</returns>
        public TickValueWithSizeData TickValueWithSize(
            IEnumerable<string> symbolNames,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default)
        {
            return TickValueWithSizeAsync(symbolNames, deadline, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously retrieves the latest quote for a single symbol.
        /// </summary>
        /// <param name="symbol">The symbol name (e.g., "EURUSD").</param>
        /// <param name="deadline">Optional deadline for the request.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Latest <see cref="QuoteData"/> for the given symbol.</returns>
        /// <exception cref="ApiExceptionMT4">If the gRPC response contains an error.</exception>
        public async Task<QuoteData> QuoteAsync(
    string symbol,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default)
{
    EnsureConnected();

    if (string.IsNullOrWhiteSpace(symbol))
        throw new ArgumentException("Symbol must be provided.", nameof(symbol));

    var request = new QuoteRequest { Symbol = symbol };
    var effectiveDeadline = ResolveDeadline(deadline);

    var res = await ExecuteWithReconnectAsync(
        headers => MarketInfoClient
            .QuoteAsync(request, headers, effectiveDeadline, cancellationToken)
            .ResponseAsync,
        r => r.ResponseCase == QuoteReply.ResponseOneofCase.Error ? r.Error : null,
        effectiveDeadline,
        cancellationToken
    ).ConfigureAwait(false);

    return res.Data;
}



        /// <summary>
        /// Synchronously retrieves the latest quote for a single symbol.
        /// </summary>
        /// <param name="symbol">The symbol name (e.g., "EURUSD").</param>
        /// <param name="deadline">Optional deadline for the request.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Latest <see cref="QuoteData"/> for the given symbol.</returns>
        public QuoteData Quote(
            string symbol,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default)
        {
            return QuoteAsync(symbol, deadline, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously retrieves quotes for multiple symbols.
        /// </summary>
        /// <param name="symbols">A list of symbol names.</param>
        /// <param name="deadline">Optional deadline for the request.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A list of <see cref="QuoteReply"/> containing data or errors for each symbol.</returns>
        public async Task<QuoteManyData> QuoteManyAsync(
    IEnumerable<string> symbols,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default)
{
    EnsureConnected();

    if (symbols == null)
        throw new ArgumentNullException(nameof(symbols), "Symbol list cannot be null.");
    if (!symbols.Any())
        throw new ArgumentException("Symbol list cannot be empty.", nameof(symbols));

    var request = new QuoteManyRequest();
    request.Symbols.AddRange(symbols);

    var effectiveDeadline = ResolveDeadline(deadline);

    var res = await ExecuteWithReconnectAsync(
        headers => MarketInfoClient
            .QuoteManyAsync(request, headers, effectiveDeadline, cancellationToken)
            .ResponseAsync,
        r => r.ResponseCase == QuoteManyReply.ResponseOneofCase.Error ? r.Error : null,
        effectiveDeadline,
        cancellationToken
    ).ConfigureAwait(false);

    return res.Data;
}



        /// <summary>
        /// Synchronously retrieves quotes for multiple symbols.
        /// </summary>
        /// <param name="symbols">A list of symbol names.</param>
        /// <param name="deadline">Optional deadline for the request.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A list of <see cref="QuoteReply"/> results.</returns>
        public QuoteManyData QuoteMany(
            IEnumerable<string> symbols,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default)
        {
            return QuoteManyAsync(symbols, deadline, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously retrieves all available symbol names and their indices.
        /// </summary>
        /// <param name="deadline">Optional deadline for the request.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A <see cref="SymbolsData"/> object containing all symbols.</returns>
        public async Task<SymbolsData> SymbolsAsync(
    DateTime? deadline = null,
    CancellationToken cancellationToken = default)
{
    EnsureConnected();

    var effectiveDeadline = ResolveDeadline(deadline);

    var res = await ExecuteWithReconnectAsync(
        headers => MarketInfoClient
            .SymbolsAsync(new SymbolsRequest(), headers, effectiveDeadline, cancellationToken)
            .ResponseAsync,
        r => r.ResponseCase == SymbolsReply.ResponseOneofCase.Error ? r.Error : null,
        effectiveDeadline,
        cancellationToken
    ).ConfigureAwait(false);

    return res.Data;
}



        /// <summary>
        /// Synchronously retrieves all available symbol names and indices.
        /// </summary>
        /// <param name="deadline">Optional deadline for the request.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A <see cref="SymbolsData"/> object.</returns>
        public SymbolsData Symbols(
            DateTime? deadline = null,
            CancellationToken cancellationToken = default)
        {
            return SymbolsAsync(deadline, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously retrieves historical quote data for a symbol within a time range.
        /// </summary>
        /// <param name="symbol">Symbol to query (e.g., "EURUSD").</param>
        /// <param name="timeframe">Desired timeframe (e.g., H1, D1).</param>
        /// <param name="from">Start time (UTC).</param>
        /// <param name="to">End time (UTC).</param>
        /// <param name="deadline">Optional gRPC deadline.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns><see cref="QuoteHistoryData"/> including candlestick OHLC quotes.</returns>
       public async Task<QuoteHistoryData> QuoteHistoryAsync(
    string symbol,
    ENUM_QUOTE_HISTORY_TIMEFRAME timeframe,
    DateTime from,
    DateTime to,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default)
{
    EnsureConnected();

    if (string.IsNullOrWhiteSpace(symbol))
        throw new ArgumentException("Symbol must be provided.", nameof(symbol));
    if (from > to)
        throw new ArgumentException("'from' date must be less than or equal to 'to' date.");

    var request = new QuoteHistoryRequest
    {
        Symbol = symbol,
        Timeframe = timeframe,
        FromTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(from.ToUniversalTime()),
        ToTime   = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(to.ToUniversalTime())
    };

    var effectiveDeadline = ResolveDeadline(deadline);

    var res = await ExecuteWithReconnectAsync(
        headers => MarketInfoClient
            .QuoteHistoryAsync(request, headers, effectiveDeadline, cancellationToken)
            .ResponseAsync,
        r => r.ResponseCase == QuoteHistoryReply.ResponseOneofCase.Error ? r.Error : null,
        effectiveDeadline,
        cancellationToken
    ).ConfigureAwait(false);

    return res.Data;
}



        /// <summary>
        /// Synchronously retrieves historical quote data for a symbol within a time range.
        /// </summary>
        /// <param name="symbol">Symbol name.</param>
        /// <param name="timeframe">Chart timeframe.</param>
        /// <param name="from">Start time (UTC).</param>
        /// <param name="to">End time (UTC).</param>
        /// <param name="deadline">Optional gRPC deadline.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns><see cref="QuoteHistoryData"/> with OHLC and volume data.</returns>
        public QuoteHistoryData QuoteHistory(
            string symbol,
            ENUM_QUOTE_HISTORY_TIMEFRAME timeframe,
            DateTime from,
            DateTime to,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default)
        {
            return QuoteHistoryAsync(symbol, timeframe, from, to, deadline, cancellationToken).GetAwaiter().GetResult();
        }


        /// <summary>
        /// Sends a new market or pending order.
        /// </summary>
        /// <param name="request">The <see cref="OrderSendRequest"/> containing order parameters.</param>
        /// <param name="deadline">Optional deadline for the request.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Details of the opened order via <see cref="OrderSendData"/>.</returns>
        /// <exception cref="ApiExceptionMT4">Thrown if the response contains an error.</exception>
       public async Task<OrderSendData> OrderSendAsync(
    OrderSendRequest request,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default)
{
    EnsureConnected();

    // Basic input validation
    if (request is null)
        throw new ArgumentNullException(nameof(request), "OrderSendRequest cannot be null.");
    if (string.IsNullOrWhiteSpace(request.Symbol))
        throw new ArgumentException("Symbol must be provided.", nameof(request.Symbol));
    if (request.Volume <= 0)
        throw new ArgumentOutOfRangeException(nameof(request.Volume), "Volume must be > 0.");
    if (request.Slippage < 0)
        throw new ArgumentOutOfRangeException(nameof(request.Slippage), "Slippage must be >= 0.");
    if (!Enum.IsDefined(typeof(OrderSendOperationType), request.OperationType))
        throw new ArgumentOutOfRangeException(nameof(request.OperationType), "Invalid OperationType value.");

    var effectiveDeadline = ResolveDeadline(deadline);

    var res = await ExecuteWithReconnectAsync(
        headers => TradeClient
            .OrderSendAsync(request, headers, effectiveDeadline, cancellationToken)
            .ResponseAsync,
        r => r.ResponseCase == OrderSendReply.ResponseOneofCase.Error ? r.Error : null,
        effectiveDeadline,
        cancellationToken
    ).ConfigureAwait(false);

    return res.Data;
}



        /// <summary>
        /// Synchronously sends a market or pending order.
        /// </summary>
        /// <param name="request">The order request parameters.</param>
        /// <param name="deadline">Optional deadline.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns><see cref="OrderSendData"/> with execution details.</returns>
        public OrderSendData OrderSend(
            OrderSendRequest request,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default)
        {
            return OrderSendAsync(request, deadline, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Modifies an existing order (price, SL, TP, expiration).
        /// </summary>
        /// <param name="request">The <see cref="OrderModifyRequest"/> specifying fields to change.</param>
        /// <param name="deadline">Optional gRPC deadline.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns><see cref="OrderModifyData"/> indicating if modification was successful.</returns>
        /// <exception cref="ApiExceptionMT4">If the gRPC response contains an error.</exception>
public async Task<OrderModifyData> OrderModifyAsync(
    OrderModifyRequest request,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default)
{
    EnsureConnected();

    if (request is null)
        throw new ArgumentNullException(nameof(request), "OrderModifyRequest cannot be null.");
    // Optionally validate required fields here (Ticket/SL/TP/etc.)

    var effectiveDeadline = ResolveDeadline(deadline);

    var res = await ExecuteWithReconnectAsync(
        headers => TradeClient
            .OrderModifyAsync(request, headers, effectiveDeadline, cancellationToken)
            .ResponseAsync,
        r => r.ResponseCase == OrderModifyReply.ResponseOneofCase.Error ? r.Error : null,
        effectiveDeadline,
        cancellationToken
    ).ConfigureAwait(false);

    return res.Data;
}


        /// <summary>
        /// Synchronously modifies an existing order.
        /// </summary>
        /// <param name="request">Modification request.</param>
        /// <param name="deadline">Optional deadline.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Result of the modification.</returns>
        public OrderModifyData OrderModify(
            OrderModifyRequest request,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default)
        {
            return OrderModifyAsync(request, deadline, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Closes a market order or deletes a pending order.
        /// </summary>
        /// <param name="request">The <see cref="OrderCloseDeleteRequest"/> containing close/delete parameters.</param>
        /// <param name="deadline">Optional request deadline.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns><see cref="OrderCloseDeleteData"/> with result mode and optional comment.</returns>
        public async Task<OrderCloseDeleteData> OrderCloseDeleteAsync(
    OrderCloseDeleteRequest request,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default)
{
    EnsureConnected();

    if (request is null)
        throw new ArgumentNullException(nameof(request), "OrderCloseDeleteRequest cannot be null.");
    if (request.OrderTicket <= 0)
        throw new ArgumentOutOfRangeException(nameof(request.OrderTicket), "OrderTicket must be > 0.");

    var effectiveDeadline = ResolveDeadline(deadline);

    var res = await ExecuteWithReconnectAsync(
        headers => TradeClient
            .OrderCloseDeleteAsync(request, headers, effectiveDeadline, cancellationToken)
            .ResponseAsync,
        r => r.ResponseCase == OrderCloseDeleteReply.ResponseOneofCase.Error ? r.Error : null,
        effectiveDeadline,
        cancellationToken
    ).ConfigureAwait(false);

    return res.Data;
}



        /// <summary>
        /// Synchronously closes a market order or deletes a pending order.
        /// </summary>
        /// <param name="request">Close/delete request.</param>
        /// <param name="deadline">Optional deadline.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns><see cref="OrderCloseDeleteData"/> indicating result.</returns>
        public OrderCloseDeleteData OrderCloseDelete(
            OrderCloseDeleteRequest request,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default)
        {
            return OrderCloseDeleteAsync(request, deadline, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Closes a market order by using another opposite market order.
        /// </summary>
        /// <param name="request">The <see cref="OrderCloseByRequest"/> specifying the orders to close by.</param>
        /// <param name="deadline">Optional request deadline.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Close result including price, profit, and time.</returns>
      public async Task<OrderCloseByData> OrderCloseByAsync(
    OrderCloseByRequest request,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default)
{
    EnsureConnected();

    if (request is null)
        throw new ArgumentNullException(nameof(request), "OrderCloseByRequest cannot be null.");
    if (request.TicketToClose <= 0)
        throw new ArgumentOutOfRangeException(nameof(request.TicketToClose), "TicketToClose must be > 0.");
    if (request.OppositeTicketClosingBy <= 0)
        throw new ArgumentOutOfRangeException(nameof(request.OppositeTicketClosingBy), "OppositeTicketClosingBy must be > 0.");

    var effectiveDeadline = ResolveDeadline(deadline);

    var res = await ExecuteWithReconnectAsync(
        headers => TradeClient
            .OrderCloseByAsync(request, headers, effectiveDeadline, cancellationToken)
            .ResponseAsync,
        r => r.ResponseCase == OrderCloseByReply.ResponseOneofCase.Error ? r.Error : null,
        effectiveDeadline,
        cancellationToken
    ).ConfigureAwait(false);

    return res.Data;
}



        /// <summary>
        /// Synchronously closes a market order using an opposite market order.
        /// </summary>
        /// <param name="request">Order close-by request.</param>
        /// <param name="deadline">Optional deadline.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns><see cref="OrderCloseByData"/> with result details.</returns>
        public OrderCloseByData OrderCloseBy(
            OrderCloseByRequest request,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default)
        {
            return OrderCloseByAsync(request, deadline, cancellationToken).GetAwaiter().GetResult();
        }

    }
}
