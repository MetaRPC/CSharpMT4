# 🧰 Using CSharpMT4 via CLI (No GUI)

This guide shows how to run **MetaRPC MT4 (C#)** entirely from the terminal — no GUI required. Perfect for developers, ops, and anyone who prefers command‑line control.

---

## 🔧 Requirements

| Tool/Lib                | Purpose                                                  |
| ----------------------- | -------------------------------------------------------- |
| **.NET 8 SDK**          | Build & run the console app                              |
| **MetaTrader 4**        | Terminal with **MetaRPC MT4** plugin enabled             |
| **`appsettings.json`**  | MT4 login, server/host, and default symbol configuration |
| **Terminal/PowerShell** | Execute all commands from the shell                      |

> ❗ Exactly one of `ServerName` **or** `Host` must be set in `appsettings.json`.

---

## 📁 Project Structure

```bash
CSharpMT4/
├── docs/
│   ├── Account/
│   ├── Market Info/
│   ├── Orders/
│   ├── Streaming/
│   ├── index.md
│   └── cli_usage.md                  # ← you are here
│
├── appsettings.json                  # MT4Options: credentials & defaults
├── MetaRPC.CSharpMT4.csproj          # Project file
├── MetaRPC.CSharpMT4.sln             # Solution file
├── Mt4account.cs                     # MT4Account: connectivity & low‑level API
├── Mt4service.cs                     # MT4Service: friendly wrappers & demos
├── Program.cs                        # Entry point with toggles
│
├── .github/
├── bin/
├── obj/
└── .gitignore
```

---

## 🧩 Example `appsettings.json`

```json
{
  "MT4Options": {
    "User": 501401178,
    "Password": "***",
    "ServerName": "RoboForex-Demo",  // OR set "Host": "mt4.mrpc.pro"
    "Host": null,
    "Port": 443,
    "DefaultSymbol": "EURUSD"
  }
}
```

**Notes**

* Set **either** `ServerName` **or** `Host`; leave the other `null`/empty.
* `DefaultSymbol` is used as the base chart for the terminal session.

---

## 🚀 Running the App

From the repository root:

```bash
# Build
dotnet build

# Run
dotnet run --project MetaRPC.CSharpMT4.csproj
```

If connection succeeds you’ll see logs like:

```
🔌 Connecting to MT4...
✅ Connected to MT4 server
=== Account Summary ===
Balance: ..., Equity: ..., Currency: ...
```

> Press **Ctrl+C** to stop; the app handles graceful cancellation.

---

## 🧪 Available Functions (by category)

### 🧾 Account

* `ShowAccountSummary()` — prints balance, equity, and currency.

### 📈 Market Info

* `ShowQuote(symbol)` — latest bid/ask snapshot.
* `ShowQuotesMany(symbols, timeoutSecondsPerSymbol=5)` — first tick per symbol.
* `ShowQuoteHistory(symbol)` — last 5 days of H1 OHLC data.
* `ShowAllSymbols()` — list all instruments with indices.
* `ShowSymbolParams(symbol)` — full trading parameters (digits, volumes, modes, currencies).
* `ShowSymbolInfo(symbol)` — lightweight info (digits, spread flag, bid).
* `ShowTickValues(symbols[])` — TickValue, TickSize, ContractSize for sizing math.

### 📦 Orders

* `ShowOpenedOrders()` — dump currently opened orders.
* `ShowOpenedOrderTickets()` — only open order ticket IDs.
* `ShowOrdersHistory()` — closed orders (last 7 days, close‑time DESC).
* `ShowOrderSendExample(symbol)` — **opens a real order** (demo parameters). ⚠️
* `CloseOrderExample(ticket)` — close/delete by ticket. ⚠️
* `CloseByOrderExample(ticket, oppositeTicket)` — close with opposite order. ⚠️

### 🔄 Streaming

* `ShowRealTimeQuotes(symbol, timeoutSeconds=5)` — first arriving tick or timeout.
* `StreamQuotesForSymbolsAsync(symbols[], durationSeconds=10)` — live ticks for a fixed period.
* `StreamTradeUpdates()` — trade activity stream (demo breaks after first event).
* `StreamOpenedOrderProfits()` — floating P/L per open order (interval=1000 ms in demo).
* `StreamOpenedOrderTickets()` — current open tickets (interval=1000 ms in demo).

> Methods marked **⚠️** can place/modify/close trades — even on demo. Use carefully.

---

## 💻 Enabling/Disabling Examples

`Program.cs` contains simple toggles:

```csharp
// ⚠️ Real trading operations (leave false unless you know what you’re doing)
private static readonly bool EnableTradingExamples = false;

// Streaming demos (ticks/profits/tickets)
private const bool EnableStreams = true;
```

You can also comment/uncomment individual calls near the "Market / Symbols" and "Streaming" sections inside `Run(...)`.

---

## 🔍 Quick CLI Session

```csharp
await _service.ShowAccountSummary();
await _service.ShowQuote("EURUSD");
await _service.ShowAllSymbols();
await _service.ShowQuotesMany(new[] { "EURUSD", "GBPUSD", "USDJPY" });

// Live tick for 5 seconds max
await _service.ShowRealTimeQuotes("EURUSD", timeoutSeconds: 5);
```

Expected tick output format:

```
Tick: EURUSD 1.09876/1.09889 @ 2025-08-21 21:23:05
```

---

## 🧠 Tips

* **Timeouts & cancellation**: streaming demos intentionally end with `OperationCanceledException` when the token fires — that’s normal and handled by logs.
* **Base chart**: `ConnectByServerNameAsync(..., baseChartSymbol: "EURUSD")` ensures the terminal opens a chart that can emit ticks immediately.
* **Logging**: uses `Microsoft.Extensions.Logging.Console`. Adjust verbosity via filters if needed.
* **Culture/formatting**: keep raw doubles for math; format only for display.

---

## ❓ Troubleshooting

* `MSB1009: The project file does not exist` — run from repo root or pass the correct path to `.csproj`.
* `Charts ids not defined` after a short tick stream — the service closes the temporary chart when cancelling; it’s safe. Avoid reusing the same stream instance after cancellation; start a new call.
* `not connected` / connection retries — check credentials, `ServerName`/`Host`, and network reachability to the MT4 gateway.

---

This console app is a **minimal, fast, and scriptable** way to interact with MT4: discover instruments, read quotes, place/close orders, and subscribe to live streams — all from your terminal.
