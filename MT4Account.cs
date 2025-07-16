using Grpc.Core;
using Grpc.Net.Client;
using mt4_term_api;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static mt4_term_api.Connection;
using static mt4_term_api.MarketInfo;

namespace mt4_term_api
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
        public string Host { get; internal set; }

        /// <summary>
        /// Gets the the MT4 server port.
        /// </summary>
        public int Port { get; internal set; }

        /// <summary>
        /// Gets the the MT4 server port.
        /// </summary>
        public string ServerName { get; internal set; }
        /// <summary>
        /// 
        /// </summary>
        public string BaseChartSymbol { get; private set; }
        public int ConnectTimeoutSeconds { get; set; }

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

        private bool Connected => !(Host is null) || !(ServerName is null);

        /// <summary>
        /// Initializes a new instance of the <see cref="MT4Account"/> class using credentials.
        /// </summary>
        /// <param name="user">The MT4 user account number.</param>
        /// <param name="password">The password for the user account.</param>
        /// <param name="grpcServer">The address of the gRPC server (optional).</param>
        /// <param name="id">An optional unique identifier for the account instance.</param>
        public MT4Account(ulong user, string password, string? grpcServer = null, Guid id = default)
        {
            User = user;
            Password = password;
            GrpcServer = grpcServer ?? "https://mt4.mrpc.pro:443";
            GrpcChannel = GrpcChannel.ForAddress(GrpcServer);

            ConnectionClient = new Connection.ConnectionClient(GrpcChannel);
            SubscriptionClient = new SubscriptionService.SubscriptionServiceClient(GrpcChannel);
            AccountClient = new AccountHelper.AccountHelperClient(GrpcChannel);
            TradeClient = new TradingHelper.TradingHelperClient(GrpcChannel);
            MarketInfoClient = new MarketInfo.MarketInfoClient(GrpcChannel);

            Id = id;
        }

        async Task Reconnect(DateTime? deadline, CancellationToken cancellationToken)
        {
            if (ServerName == null)
                await ConnectByHostPortAsync(Host, Port, BaseChartSymbol, true, ConnectTimeoutSeconds, deadline, cancellationToken);
            else
                await ConnectByServerNameAsync(ServerName, BaseChartSymbol, true, ConnectTimeoutSeconds, deadline, cancellationToken);
        }

        // Connect methods

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

            Metadata? headers = null;
            if (Id != default)
            {
                headers = new Metadata { { "id", Id.ToString() } };
            }

            var res = await ConnectionClient.ConnectAsync(connectRequest, headers, deadline, cancellationToken);
            if (res.Error != null)
                throw new ApiExceptionMT4(res.Error);
            Host = host;
            Port = port;
            BaseChartSymbol = baseChartSymbol;
            ConnectTimeoutSeconds = timeoutSeconds;
            Id = Guid.Parse(res.Data.TerminalInstanceGuid);
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
            var connectRequest = new ConnectExRequest
            {
                User = User,
                Password = Password,
                MtClusterName = serverName,
                BaseChartSymbol = baseChartSymbol,
                TerminalReadinessWaitingTimeoutSeconds = timeoutSeconds
            };

            Metadata? headers = null;
            if (Id != default)
            {
                headers = new Metadata { { "id", Id.ToString() } };
            }

            var res = await ConnectionClient.ConnectExAsync(connectRequest, headers, deadline, cancellationToken);

            if (res.Error != null)
                throw new ApiExceptionMT4(res.Error);
            ServerName = serverName;
            BaseChartSymbol = baseChartSymbol;
            ConnectTimeoutSeconds = timeoutSeconds;
            Id = Guid.Parse(res.Data.TerminalInstanceGuid);
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

        //
        // Account helper methods --------------------------------------------------------------------------------------------------------
        //

        private Metadata GetHeaders()
        {
            return new Metadata { { "id", Id.ToString() } };
        }

        private async Task<T> ExecuteWithReconnect<T>(
            Func<Metadata, T> grpcCall,
            Func<T, Mt4TermApi.Error?> errorSelector,
            DateTime? deadline,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var headers = GetHeaders();
                T res;

                try
                {
                    res = grpcCall(headers);
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
                {
                    await Task.Delay(500, cancellationToken);
                    continue; // In future, you might call Reconnect() here
                }

                var error = errorSelector(res);

                if (error?.ErrorCode == "TERMINAL_INSTANCE_NOT_FOUND")
                {
                    await Task.Delay(500, cancellationToken);
                    continue;
                }

                if (error != null)
                    throw new ApiExceptionMT4(error);

                return res;
            }

            throw new OperationCanceledException("Operation canceled by user.");
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
        private async IAsyncEnumerable<TData> ExecuteStreamWithReconnect<TRequest, TReply, TData>(
        TRequest request,
        Func<TRequest, Metadata, CancellationToken, AsyncServerStreamingCall<TReply>> streamInvoker,
        Func<TReply, Mt4TermApi.Error?> getError,
        Func<TReply, TData> getData,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var reconnectRequired = false;

                AsyncServerStreamingCall<TReply>? stream = null;
                try
                {
                    stream = streamInvoker(request, GetHeaders(), cancellationToken);
                    var responseStream = stream.ResponseStream;

                    while (true)
                    {
                        TReply reply;

                        try
                        {
                            if (!await responseStream.MoveNext(cancellationToken))
                                break; // Stream ended naturally

                            reply = responseStream.Current;
                        }
                        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable) // || ex.StatusCode == StatusCode.Internal
                        {
                            reconnectRequired = true;
                            break; // Trigger reconnect
                        }

                        var error = getError(reply);
                        if (error?.ErrorCode == "TERMINAL_INSTANCE_NOT_FOUND")
                        {
                            reconnectRequired = true;
                            break; // Trigger reconnect
                        }
                        else if (error?.ErrorCode == "TERMINAL_REGISTRY_TERMINAL_NOT_FOUND")
                        {
                            reconnectRequired = true;
                            break; // Trigger reconnect
                        }

                        if (error != null)
                            throw new ApiExceptionMT4(error);

                        var data = getData(reply);
                        if (data != null)
                            yield return data; // Real-time yield outside try-catch
                    }
                }
                finally
                {
                    stream?.Dispose();
                }

                if (reconnectRequired)
                {
                    await Task.Delay(500, cancellationToken);
                    await Reconnect(null, cancellationToken);
                }
                else
                {
                    break; // Exit loop normally
                }
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
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (Id == default)
                throw new ConnectExceptionMT4("Please call Connect method firstly");

            var request = new OnSymbolTickRequest();
            request.SymbolNames.AddRange(symbols);

            await foreach (var data in ExecuteStreamWithReconnect<OnSymbolTickRequest, OnSymbolTickReply, OnSymbolTickData>(
                request,
                (req, headers, ct) => SubscriptionClient.OnSymbolTick(req, headers, cancellationToken: ct),
                reply => reply.ResponseCase == OnSymbolTickReply.ResponseOneofCase.Error ? reply.Error : null,
                reply => reply.ResponseCase == OnSymbolTickReply.ResponseOneofCase.Data ? reply.Data : null,
                cancellationToken))
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
        public async IAsyncEnumerable<OnTradeData> OnTradeAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (Id == default)
                throw new ConnectExceptionMT4("Please call Connect method firstly");

            var request = new OnTradeRequest();

            await foreach (var data in ExecuteStreamWithReconnect<OnTradeRequest, OnTradeReply, OnTradeData>(
                request,
                (req, headers, ct) => SubscriptionClient.OnTrade(req, headers, cancellationToken: ct),
                reply => reply.ResponseCase == OnTradeReply.ResponseOneofCase.Error ? reply.Error : null,
                reply => reply.ResponseCase == OnTradeReply.ResponseOneofCase.Data ? reply.Data : null,
                cancellationToken))
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
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (Id == default)
                throw new ConnectExceptionMT4("Please call Connect method firstly");

            var request = new OnOpenedOrdersProfitRequest
            {
                TimerPeriodMilliseconds = intervalMs
            };

            await foreach (var data in ExecuteStreamWithReconnect<OnOpenedOrdersProfitRequest, OnOpenedOrdersProfitReply, OnOpenedOrdersProfitData>(
                request,
                (req, headers, ct) => SubscriptionClient.OnOpenedOrdersProfit(req, headers, cancellationToken: ct),
                reply => reply.ResponseCase == OnOpenedOrdersProfitReply.ResponseOneofCase.Error ? reply.Error : null,
                reply => reply.ResponseCase == OnOpenedOrdersProfitReply.ResponseOneofCase.Data ? reply.Data : null,
                cancellationToken))
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
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (Id == default)
                throw new ConnectExceptionMT4("Please call Connect method firstly");

            var request = new OnOpenedOrdersTicketsRequest
            {
                PullIntervalMilliseconds = intervalMs
            };

            await foreach (var data in ExecuteStreamWithReconnect<OnOpenedOrdersTicketsRequest, OnOpenedOrdersTicketsReply, OnOpenedOrdersTicketsData>(
                request,
                (req, headers, ct) => SubscriptionClient.OnOpenedOrdersTickets(req, headers, cancellationToken: ct),
                reply => reply.ResponseCase == OnOpenedOrdersTicketsReply.ResponseOneofCase.Error ? reply.Error : null,
                reply => reply.ResponseCase == OnOpenedOrdersTicketsReply.ResponseOneofCase.Data ? reply.Data : null,
                cancellationToken))
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
            if (!Connected)
                throw new ConnectExceptionMT4("You must set ID to connect to MT4 Server.");

            var request = new AccountSummaryRequest();

            var res = await ExecuteWithReconnect(
                headers => AccountClient.AccountSummary(request, headers, deadline, cancellationToken),
                r => r.ResponseCase == AccountSummaryReply.ResponseOneofCase.Error ? r.Error : null,
                deadline,
                cancellationToken
            );

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
            var request = new OpenedOrdersRequest { SortType = sortType };

            var res = await ExecuteWithReconnect(
                headers => AccountClient.OpenedOrders(request, headers, deadline, cancellationToken),
                r => r.ResponseCase == OpenedOrdersReply.ResponseOneofCase.Error ? r.Error : null,
                deadline,
                cancellationToken
            );

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
            var request = new OpenedOrdersTicketsRequest();

            var res = await ExecuteWithReconnect(
                headers => AccountClient.OpenedOrdersTickets(request, headers, deadline, cancellationToken),
                r => r.ResponseCase == OpenedOrdersTicketsReply.ResponseOneofCase.Error ? r.Error : null,
                deadline,
                cancellationToken
            );

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
            var request = new OrdersHistoryRequest
            {
                InputSortMode = sortType
            };

            if (from.HasValue)
                request.InputFrom = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(from.Value.ToUniversalTime());
            if (to.HasValue)
                request.InputTo = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(to.Value.ToUniversalTime());
            if (page.HasValue)
                request.PageNumber = page.Value;
            if (itemsPerPage.HasValue)
                request.ItemsPerPage = itemsPerPage.Value;

            var res = await ExecuteWithReconnect(
                headers => AccountClient.OrdersHistory(request, headers, deadline, cancellationToken),
                r => r.ResponseCase == OrdersHistoryReply.ResponseOneofCase.Error ? r.Error : null,
                deadline,
                cancellationToken
            );

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
            var request = new SymbolParamsManyRequest();

            if (!string.IsNullOrWhiteSpace(symbolName))
                request.SymbolName = symbolName;

            var res = await ExecuteWithReconnect(
                headers => AccountClient.SymbolParamsMany(request, headers, deadline, cancellationToken),
                r => r.ResponseCase == SymbolParamsManyReply.ResponseOneofCase.Error ? r.Error : null,
                deadline,
                cancellationToken
            );

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
            var request = new TickValueWithSizeRequest();
            request.SymbolNames.AddRange(symbolNames);

            var res = await ExecuteWithReconnect(
                headers => AccountClient.TickValueWithSize(request, headers, deadline, cancellationToken),
                r => r.ResponseCase == TickValueWithSizeReply.ResponseOneofCase.Error ? r.Error : null,
                deadline,
                cancellationToken
            );

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
            var request = new QuoteRequest { Symbol = symbol };

            var res = await ExecuteWithReconnect(
                headers => MarketInfoClient.Quote(request, headers, deadline, cancellationToken),
                r => r.ResponseCase == QuoteReply.ResponseOneofCase.Error ? r.Error : null,
                deadline,
                cancellationToken
            );

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
            var request = new QuoteManyRequest();
            request.Symbols.AddRange(symbols);

            var res = await ExecuteWithReconnect(
                headers => MarketInfoClient.QuoteMany(request, headers, deadline, cancellationToken),
                r => r.ResponseCase == QuoteManyReply.ResponseOneofCase.Error ? r.Error : null,
                deadline,
                cancellationToken
            );

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
            var res = await ExecuteWithReconnect(
                headers => MarketInfoClient.Symbols(new SymbolsRequest(), headers, deadline, cancellationToken),
                r => r.ResponseCase == SymbolsReply.ResponseOneofCase.Error ? r.Error : null,
                deadline,
                cancellationToken
            );

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
            var request = new QuoteHistoryRequest
            {
                Symbol = symbol,
                Timeframe = timeframe,
                FromTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(from.ToUniversalTime()),
                ToTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(to.ToUniversalTime())
            };

            var res = await ExecuteWithReconnect(
                headers => MarketInfoClient.QuoteHistory(request, headers, deadline, cancellationToken),
                r => r.ResponseCase == QuoteHistoryReply.ResponseOneofCase.Error ? r.Error : null,
                deadline,
                cancellationToken
            );

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
            var res = await ExecuteWithReconnect(
                headers => TradeClient.OrderSend(request, headers, deadline, cancellationToken),
                r => r.ResponseCase == OrderSendReply.ResponseOneofCase.Error ? r.Error : null,
                deadline,
                cancellationToken
            );

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
            var res = await ExecuteWithReconnect(
                headers => TradeClient.OrderModify(request, headers, deadline, cancellationToken),
                r => r.ResponseCase == OrderModifyReply.ResponseOneofCase.Error ? r.Error : null,
                deadline,
                cancellationToken
            );

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
            var res = await ExecuteWithReconnect(
                headers => TradeClient.OrderCloseDelete(request, headers, deadline, cancellationToken),
                r => r.ResponseCase == OrderCloseDeleteReply.ResponseOneofCase.Error ? r.Error : null,
                deadline,
                cancellationToken
            );

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
            var res = await ExecuteWithReconnect(
                headers => TradeClient.OrderCloseBy(request, headers, deadline, cancellationToken),
                r => r.ResponseCase == OrderCloseByReply.ResponseOneofCase.Error ? r.Error : null,
                deadline,
                cancellationToken
            );

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