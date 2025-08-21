# 🚦 Beginner Run Guide for CSharpMT4 (CLI, no GUI)

This guide shows how to use **CSharpMT4** from the terminal with **.NET 8**, without any GUI. Perfect for quick tests, CI runs, or headless servers.

---

## 🔧 Requirements

| Tool / File            | Purpose                                                        |
| ---------------------- | -------------------------------------------------------------- |
| **.NET SDK 8.0+**      | Build & run the console app                                    |
| **MetaTrader 4**       | Terminal with the MetaRPC MT4 gRPC bridge running              |
| **`appsettings.json`** | Login/connection options (user, password, server or host/port) |
| **PowerShell / Bash**  | All commands here are CLI-friendly                             |

> Optional: VS Code or Visual Studio for editing/debugging.

---

## 📁 Project Structure (key files)

```bash
CSharpMT4/
├── docs/                               # Markdown docs (Account / Market Info / Orders / Streaming)
├── appsettings.json                    # MT4Options config (credentials & defaults)
├── MetaRPC.CSharpMT4.csproj            # Project file
├── MetaRPC.CSharpMT4.sln               # Solution file
├── Mt4Account.cs                       # Low-level MT4 account/gRPC calls
├── Mt4service.cs                       # Friendly wrappers (Show*/Stream* helpers)
└── Program.cs                          # Entry point with demo toggles
```

---

## 🔐 Example `appsettings.json`

> Put this file **in the project root** (`CSharpMT4/appsettings.json`). Adjust values.

```json
{
  "MT4Options": {
    "User": 501401178,
    "Password": "***",
    "ServerName": "RoboForex-Demo",
    "DefaultSymbol": "EURUSD"
  }
}
```

*Alternative (host/port):*

```json
{
  "MT4Options": {
    "User": 501401178,
    "Password": "***",
    "Host": "mt4.mrpc.pro",
    "Port": 443,
    "DefaultSymbol": "EURUSD"
  }
}
```

> **Note:** Either `ServerName` **or** `Host` must be set.

---

## 🚀 Running the App

From the repository root:

```bash
# Restore, build, run
 dotnet build
 dotnet run --project MetaRPC.CSharpMT4.csproj
```

If everything is ok you’ll see logs like:

```
🔌 Connecting to MT4...
✅ Connected to MT4 server
```

Use **Ctrl+C** to stop gracefully (the app wires a cancellation token for clean shutdown).

---

## 🧪 Safe First Steps (read‑only)

These **do not modify** the account state. In `Program.cs`, keep only the lines you want active.

```csharp
await _service.ShowAccountSummary();              // Account snapshot
await _service.ShowAllSymbols();                  // Discover instruments
await _service.ShowQuote(symbol);                 // One‑shot quote for default symbol
await _service.ShowQuotesMany(new[]{"EURUSD", "GBPUSD", "USDJPY"});
await _service.ShowQuoteHistory(symbol);          // Last 5 days in example (H1)
await _service.ShowSymbolParams("EURUSD");       // Full instrument profile
await _service.ShowTickValues(new[]{"EURUSD", "GBPUSD"}); // Monetary metrics
```

> The demo helpers already apply timeouts/cancellation where sensible.

---

## 📊 Getting Data (account & market)

Further readers to inspect the environment:

```csharp
await _service.ShowOpenedOrders();                // All active (incl. pendings)
await _service.ShowOpenedOrderTickets();          // Only ticket IDs
```

---

## ⚠️ Trading Operations (danger zone)

These **modify state** (even on demo). Use real ticket IDs from previous outputs.

```csharp
await _service.ShowOrderSendExample(symbol);             // Place market/pending (inside helper)
await _service.CloseOrderExample(12345678);              // Close by ticket
await _service.CloseByOrderExample(12345678, 12345679);  // Close with opposite ticket
```

> Only enable when you’re ready. Prefer demo accounts until confident.

---

## 📡 Streaming

Real‑time subscriptions with graceful cancellation inside helpers:

```csharp
await _service.ShowRealTimeQuotes(symbol);               // First tick or timeout
await _service.StreamQuotesForSymbolsAsync(
    new[]{"EURUSD","GBPUSD"}, durationSeconds:10);     // Live ticks for N seconds

await _service.StreamOpenedOrderProfits();               // Floating P/L by order (interval inside)
await _service.StreamOpenedOrderTickets();               // Live list of open tickets
await _service.StreamTradeUpdates();                     // Trade activity events
```

> Under the hood: resilient streaming with reconnect/backoff; server-side `Cancelled` is treated as normal completion in demos.

---

## 🎬 Combo Scenarios

### A) Read‑only dashboard (safe)

```csharp
await _service.ShowAccountSummary();
await _service.ShowQuote(symbol);
await _service.ShowOpenedOrders();
await _service.ShowRealTimeQuotes(symbol);
await _service.StreamOpenedOrderProfits();
```

### B) Adjust risk → then close

```csharp
await _service.ShowOpenedOrders();                           // Inspect & pick ticket(s)
// (example helper for SL/TP can be added similarly to Close/Send)
await _service.CloseOrderExample(/*ticket=*/ 12345678);      // Close when ready
```

### C) Ticket stream → lazy details

```csharp
await _service.StreamOpenedOrderTickets();   // Low‑overhead ticket stream
await _service.ShowOpenedOrders();           // Fetch details only on change
```

### D) History snapshot (safe)

```csharp
await _service.ShowOrdersHistory();
```

---

## 🧠 Tips

* Start with **safe readers** first; add trading calls later.
* Don’t hardcode tickets — copy them from console output (`ShowOpenedOrders` or tickets stream).
* Respect precision: use `Digits` from `ShowSymbolParams` for UI formatting; keep raw doubles for math.
* If the terminal is slow or remote, increase timeouts (the helpers use short defaults).
* Logs are your friend: Console logging is already configured via `LoggerFactory` in `Program.cs`.

---

## 🛠 Pro Notes

* **Deadlines & cancellation**: all async calls accept `deadline` and `CancellationToken`. You can pass a hard deadline:

  ```csharp
  using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
  var summary = await _mt4.AccountSummaryAsync(deadline: null, cancellationToken: cts.Token);
  ```
* **Reconnects**: `ExecuteWithReconnect` retries on transient gRPC errors (e.g., `Unavailable`, `DeadlineExceeded`) and on terminal-instance restarts.
* **Streaming restarts**: long-lived streams auto‑restart with exponential backoff; if the server cancels the stream, demos treat it as a clean finish.
* **Connection**: `ConnectByServerNameAsync`/`ConnectByHostPortAsync` are called inside startup code. Ensure credentials + server/host are valid.

---

## 📎 Quick Example (Program.cs)

```csharp
// Toggle only what you need — keep dangerous calls commented until ready
await _service.ShowAccountSummary();
await _service.ShowQuote(symbol);
// await _service.ShowOrderSendExample(symbol);
// await _service.CloseOrderExample(12345678);
await _service.ShowRealTimeQuotes(symbol);
```

That’s it — you now have a **terminal‑driven MT4 toolbox** for reading market data, streaming quotes, and managing orders.
