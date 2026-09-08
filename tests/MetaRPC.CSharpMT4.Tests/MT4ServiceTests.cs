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
}
