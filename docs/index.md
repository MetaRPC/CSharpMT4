# Getting Started with MetaTrader 4 in C\#

Welcome to the **MetaRPC MT4 C# Documentation** — your guide to integrating with **MetaTrader 4** using **C#** and **gRPC**.

This documentation will help you:

* 📘 Explore all available **account, market, and order methods**
* 💡 Learn from **async C# usage examples** with logging and cancellation
* 🔁 Work with **real‑time streaming** for quotes, orders, and trades
* ⚙️ Understand all **input/output types** such as `OrderInfo`, `QuoteData`, and enums like `ENUM_ORDER_TYPE_TF`

---

## 📚 Main Sections

### Account

* [Show Account Summary](Account/ShowAccountSummary.md)

---

### Market Info

* **Section overview:** [Market Info — Overview](Market%20Info/Market_Info_Overview.md)
* [Show Quote](Market%20Info/ShowQuote.md)
* [Show Quotes Many](Market%20Info/ShowQuotesMany.md)
* [Show Quote History](Market%20Info/ShowQuoteHistory.md)
* [Show Symbol Info](Market%20Info/ShowSymbolInfo.md)
* [Show Symbol Params](Market%20Info/ShowSymbolParams.md)
* [Show All Symbols](Market%20Info/ShowAllSymbols.md)
* [Show Tick Values](Market%20Info/ShowTickValues.md)

---

### Order Operations ⚠️

* **Section overview:** [Orders — Overview](Orders/Orders_Overview.md)
* [Show Opened Orders](Orders/ShowOpenedOrders.md)
* [Show Opened Order Tickets](Orders/ShowOpenedOrderTickets.md)
* [Show Orders History](Orders/ShowOrdersHistory.md)
* [Close Order Example](Orders/CloseOrderExample.md)
* [Close By Order Example](Orders/CloseByOrderExample.md)
* [Order Send Example](Orders/ShowOrderSendExample.md)

---

### Streaming

* **Section overview:** [Streaming — Overview](Streaming/Streaming_Overview.md)
* [Show Real‑Time Quotes](Streaming/ShowRealTimeQuotes.md)
* [Stream Opened Order Profits](Streaming/StreamOpenedOrderProfits.md)
* [Stream Opened Order Tickets](Streaming/StreamOpenedOrderTickets.md)
* [Stream Trade Updates](Streaming/StreamTradeUpdates.md)

---

## 🚀 Quick Start

1. Configure your **`appsettings.json`** with MT4 credentials and connection details.
2. Create **`MT4Account`** and **`MT4Service`**, then connect by server name or host/port.
3. Run demos from **`Program.cs`** (the `Show*` helpers) or call the low‑level methods directly.

```csharp
// appsettings.json
{
  "MT4Options": {
    "User": 1234567,
    "Password": "<<<use env var>>>",
    "ServerName": "RoboForex-Demo",
    "Host": null,
    "Port": 443,
    "DefaultSymbol": "EURUSD"
  }
}
```
```csharp
// Program.cs
var cfg = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var options = cfg.GetSection("MT4Options").Get<MT4Options>()!;

using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
var mt4 = new MT4Account(options.User, options.Password, logger: loggerFactory.CreateLogger<MT4Account>());
var svc = new MT4Service(mt4, loggerFactory.CreateLogger<MT4Service>());

await mt4.ConnectByServerNameAsync(options.ServerName!, baseChartSymbol: options.DefaultSymbol);

await svc.ShowAccountSummary();
await svc.ShowQuote(options.DefaultSymbol);
```

## 🛠 Requirements

* .NET 8 SDK
* gRPC client runtime and Protobuf bindings (included via project references)
* `Microsoft.Extensions.Logging.*` for console logging

---

## 🧭 Navigation

* The section links above point **directly** to method pages in this repo.
* Each **Overview** page gives workflow tips and best practices.
* Method pages list exact **input/output fields** and enums.
