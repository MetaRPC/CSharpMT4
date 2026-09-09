using Xunit;
using MetaRPC.CSharpMT4;

namespace MetaRPC.CSharpMT4.Tests;

public class MT4ServiceTests
{
    [Fact]
    public void TestMT4Account_ClientConstruction()
    {
        var account = new MT4Account(12345678, "demo_pass", "https://mt4.mrpc.pro:443", Guid.NewGuid());
        Assert.NotNull(account);
        Assert.Equal(12345678UL, account.User);
        Assert.Equal("demo_pass", account.Password);
    }

    [Fact]
    public void TestMT4Account_DefaultConnectionState()
    {
        var account = new MT4Account(12345678, "demo_pass");
        Assert.NotNull(account);
        Assert.Null(account.Host);
        Assert.Null(account.ServerName);
    }

    [Fact]
    public void TestMT4Account_AutoGetId()
    {
        var account = new MT4Account(12345678, "demo_password", "https://mt4.mrpc.pro:443", "test_api_key");
        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.Equal("test_api_key", account.ApiKey);
    }

    [Fact]
    public void TestGetIdRequest_ProtoSerialization()
    {
        var req = new mt4_term_api.GetIdRequest
        {
            User = "12345678",
            Password = "demo_password"
        };
        Assert.Equal("12345678", req.User);
        Assert.Equal("demo_password", req.Password);

        var reply = new mt4_term_api.GetIdReply
        {
            Data = new mt4_term_api.GetIdData { Id = "68c935ee-a2b1-4f3e-bb36-3982845cfa85" }
        };
        Assert.NotNull(reply.Data);
        Assert.Equal("68c935ee-a2b1-4f3e-bb36-3982845cfa85", reply.Data.Id);
    }
}
