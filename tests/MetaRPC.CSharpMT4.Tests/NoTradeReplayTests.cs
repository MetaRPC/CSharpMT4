using System.Linq;
using System.Net;
using Grpc.Core;
using MetaRPC.CSharpMT4;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using mt4_term_api;
using Xunit;
using Error = Mt4TermApi.Error;

namespace MetaRPC.CSharpMT4.Tests;

/// <summary>
/// In-process gRPC server tests: a trade call is never re-sent after a transport failure (duplicate fill risk),
/// while reads and in-band TERMINAL_INSTANCE_NOT_FOUND keep their reconnect + retry.
/// </summary>
public sealed class NoTradeReplayTests : IAsyncLifetime
{
    private sealed class State
    {
        public int ConnectEx, OrderSend, OrderModify, OrderCloseDelete, OrderCloseBy, AccountSummary;
        public StatusCode? TradeFailure;   // trade calls throw this status (after "executing")
        public bool TradeNotFoundOnce;     // first OrderSend answers TERMINAL_INSTANCE_NOT_FOUND in-band
        public string? AccountErrorCode;   // AccountSummary answers this in-band error code
    }

    private sealed class FakeConnection(State s) : Connection.ConnectionBase
    {
        public override Task<ConnectExReply> ConnectEx(ConnectExRequest request, ServerCallContext context)
        {
            Interlocked.Increment(ref s.ConnectEx);
            return Task.FromResult(new ConnectExReply { Data = new ConnectData { TerminalInstanceGuid = Guid.NewGuid().ToString() } });
        }
    }

    private sealed class FakeTrading(State s) : TradingHelper.TradingHelperBase
    {
        private RpcException Fail() => new(new Status(s.TradeFailure ?? StatusCode.Unavailable, "simulated failure after execution"));

        public override Task<OrderSendReply> OrderSend(OrderSendRequest request, ServerCallContext context)
        {
            int n = Interlocked.Increment(ref s.OrderSend);
            if (s.TradeFailure != null) throw Fail();
            if (s.TradeNotFoundOnce && n == 1)
                return Task.FromResult(new OrderSendReply { Error = new Error { ErrorCode = "TERMINAL_INSTANCE_NOT_FOUND" } });
            return Task.FromResult(new OrderSendReply { Data = new OrderSendData { Ticket = 42 } });
        }

        public override Task<OrderModifyReply> OrderModify(OrderModifyRequest request, ServerCallContext context)
        {
            Interlocked.Increment(ref s.OrderModify);
            throw Fail();
        }

        public override Task<OrderCloseDeleteReply> OrderCloseDelete(OrderCloseDeleteRequest request, ServerCallContext context)
        {
            Interlocked.Increment(ref s.OrderCloseDelete);
            throw Fail();
        }

        public override Task<OrderCloseByReply> OrderCloseBy(OrderCloseByRequest request, ServerCallContext context)
        {
            Interlocked.Increment(ref s.OrderCloseBy);
            throw Fail();
        }
    }

    private sealed class FakeAccount(State s) : AccountHelper.AccountHelperBase
    {
        public override Task<AccountSummaryReply> AccountSummary(AccountSummaryRequest request, ServerCallContext context)
        {
            int n = Interlocked.Increment(ref s.AccountSummary);
            if (s.AccountErrorCode != null)
                return Task.FromResult(new AccountSummaryReply { Error = new Error { ErrorCode = s.AccountErrorCode, ErrorMessage = "refused" } });
            if (n == 1) throw new RpcException(new Status(StatusCode.Unavailable, "simulated outage"));
            return Task.FromResult(new AccountSummaryReply { Data = new AccountSummaryData { AccountBalance = 123 } });
        }
    }

    private readonly State _s = new();
    private WebApplication _app = null!;
    private MT4Account _acc = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(k => k.Listen(IPAddress.Loopback, 0, o => o.Protocols = HttpProtocols.Http2));
        builder.Services.AddGrpc();
        builder.Services.AddSingleton(_s);
        _app = builder.Build();
        _app.MapGrpcService<FakeConnection>();
        _app.MapGrpcService<FakeTrading>();
        _app.MapGrpcService<FakeAccount>();
        await _app.StartAsync();
        string address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();

        _acc = new MT4Account(1, "pw", address, Guid.Empty, "TEST_KEY");
        await _acc.ConnectByServerNameAsync("Demo-Server", waitForTerminalIsAlive: false);
        _s.ConnectEx = 0;
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Theory]
    [InlineData(StatusCode.Unavailable)]
    [InlineData(StatusCode.DeadlineExceeded)]
    [InlineData(StatusCode.Unknown)]
    [InlineData(StatusCode.Internal)]
    public async Task OrderSend_TransportFailure_IsNotResent(StatusCode code)
    {
        _s.TradeFailure = code;
        var e = await Assert.ThrowsAsync<TradeOutcomeUnknownExceptionMT4>(() =>
            _acc.OrderSendAsync(new OrderSendRequest { Symbol = "EURUSD", Volume = 0.01 }));
        Assert.Equal(1, _s.OrderSend);
        Assert.Equal(0, _s.ConnectEx);
        Assert.Equal("TRADE_OUTCOME_UNKNOWN", e.ErrorCode);
        Assert.Equal("OrderSend", e.Operation);
        Assert.Equal(code, e.StatusCode);
        Assert.IsAssignableFrom<RpcException>(e);
    }

    [Fact]
    public async Task Modify_CloseDelete_CloseBy_Unavailable_AreNotResent()
    {
        _s.TradeFailure = StatusCode.Unavailable;
        await Assert.ThrowsAsync<TradeOutcomeUnknownExceptionMT4>(() => _acc.OrderModifyAsync(new OrderModifyRequest()));
        var e = await Assert.ThrowsAsync<TradeOutcomeUnknownExceptionMT4>(() =>
            _acc.OrderCloseDeleteAsync(new OrderCloseDeleteRequest { OrderTicket = 5 }));
        Assert.Equal("OrderCloseDelete", e.Operation);
        await Assert.ThrowsAsync<TradeOutcomeUnknownExceptionMT4>(() =>
            _acc.OrderCloseByAsync(new OrderCloseByRequest { TicketToClose = 5, OppositeTicketClosingBy = 6 }));
        Assert.Equal(1, _s.OrderModify);
        Assert.Equal(1, _s.OrderCloseDelete);
        Assert.Equal(1, _s.OrderCloseBy);
        Assert.Equal(0, _s.ConnectEx);
    }

    [Fact]
    public async Task OrderSend_TerminalInstanceNotFound_ReconnectsAndResends()
    {
        _s.TradeNotFoundOnce = true;
        var d = await _acc.OrderSendAsync(new OrderSendRequest { Symbol = "EURUSD", Volume = 0.01 });
        Assert.Equal(42, d.Ticket);
        Assert.Equal(2, _s.OrderSend);
        Assert.Equal(1, _s.ConnectEx);
    }

    [Fact]
    public async Task Read_Unavailable_IsRetried()
    {
        var d = await _acc.AccountSummaryAsync();
        Assert.Equal(123, d.AccountBalance);
        Assert.Equal(2, _s.AccountSummary);
        Assert.Equal(1, _s.ConnectEx);
    }

    [Fact]
    public async Task Read_ApiKeyInvalid_KeepsCode_NoReconnect()
    {
        _s.AccountErrorCode = "API_KEY_INVALID";
        var e = await Assert.ThrowsAsync<ApiExceptionMT4>(() => _acc.AccountSummaryAsync());
        Assert.Equal("API_KEY_INVALID", e.ErrorCode);
        Assert.Equal(1, _s.AccountSummary);
        Assert.Equal(0, _s.ConnectEx);
    }
}
