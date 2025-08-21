# 🛡️ Safe-by-Default Examples & Feature Toggles (C#)

> Run the examples **safely by default**. Turn on the powerful (but potentially dangerous) trading calls only when you explicitly decide to.

---

## 🪪 Requirements

* `.NET SDK 8.0+`
* `appsettings.json` with MT4 credentials and connection details (see `MT4Options` section)

  * `User`, `Password`
  * Either `ServerName` **or** `Host`+`Port`
  * `DefaultSymbol` (e.g., `"EURUSD"`)
* You do **not** have to call `MT4Account` directly — use the `MT4Service` methods in examples.

---

## ⚡ Quick Start (Safe Mode)

1. Fill `appsettings.json`.
2. Build & run:

   ```bash
   dotnet run --project MetaRPC.CSharpMT4.csproj
   ```
3. As shipped, only **read‑only** calls and safe demo streams run.
4. When you’re ready to try trading (open/modify/close), enable it via **Feature Toggles** below.

---

## ✨ Feature Toggles (Safe by Default)

Place these flags near the top of your `Program.cs` (they already exist in the template):

```csharp
// ===== Feature toggles (safe by default) =====
private static readonly bool EnableTradingExamples = false;  // ⚠️ Real trading operations
private const bool        EnableStreams         = true;      // ticks/profit/tickets
```

### 🔧 What each toggle does

* **`EnableTradingExamples`**

  * `false` (recommended default): trading methods are **not** executed.
  * `true`: allows examples that **open/modify/close** orders.
* **`EnableStreams`**

  * Enables read‑only streaming examples (quotes, opened‑orders profit/tickets, trade updates).

> Tip: You can keep these toggles in version control with **safe defaults**, and flip them locally when testing.

---

## 🧩 Example Layout in `Program.cs`

Keep `Main` readable and declarative. The toggles control the “dangerous” parts.

```csharp
// --- 📂 Account Info ---
await _service.ShowAccountSummary();

// --- 📂 Order Operations (read-only) ---
await _service.ShowOpenedOrders();
await _service.ShowOpenedOrderTickets();
await _service.ShowOrdersHistory();   // one-time snapshot

// --- ⚠️ Trading (DANGEROUS) ---
if (EnableTradingExamples)
{
    await _service.ShowOrderSendExample(symbol);

    // Real tickets are required for the following:
    // await _service.CloseOrderExample(12345678);
    // await _service.CloseByOrderExample(12345678, 12345679);
    // await _service.ShowOrderModifyExample(12345678); // if you add an implementation
}

// --- 📂 Market / Symbols ---
await _service.ShowQuote(symbol);
await _service.ShowQuotesMany(new[] { "EURUSD", "GBPUSD", "USDJPY" });
await _service.ShowQuoteHistory(symbol);             // one-time
await _service.ShowAllSymbols();
await _service.ShowTickValues(new[] { "EURUSD", "GBPUSD", "USDJPY" });
await _service.ShowSymbolParams("EURUSD");
await _service.ShowSymbolInfo(symbol);

// Quick live tick: first tick OR timeout; will not hang indefinitely.
await _service.ShowRealTimeQuotes(symbol, timeoutSeconds: 5, ct);

// --- 📂 Streaming / Subscriptions (read-only) ---
if (EnableStreams && !ct.IsCancellationRequested)
{
    // Live ticks for a fixed time
    await _service.StreamQuotesForSymbolsAsync(new[] { "EURUSD", "GBPUSD" }, durationSeconds: 10);

    // Demo streams: examples exit after the first message
    await _service.StreamTradeUpdates();
    await _service.StreamOpenedOrderProfits();
    await _service.StreamOpenedOrderTickets();
}
```

> Note: `CTRL+C` in the console triggers a graceful shutdown — the code wires a cancellation token to stop streams and exit cleanly.

---

## 🚫 Dangerous vs ✅ Safe

**Dangerous (disabled by default):**

* `ShowOrderSendExample` — opens a trade (market/pending)
* `ShowOrderModifyExample` — changes SL/TP (requires valid ticket)
* `CloseOrderExample` — closes a ticket
* `CloseByOrderExample` — closes against an opposite ticket
* (Any method that **modifies** account state)

**Safe (read‑only):**

* All `Show*` info calls (account, symbols, quotes, history)
* `Stream*` subscriptions: quotes, opened‑order profits, tickets, trade updates

---

## 🏗️ How to Use the Toggles (Step by Step)

1. Open `Program.cs` and locate the **Feature toggles** block:

   ```csharp
   private static readonly bool EnableTradingExamples = false;
   private const bool        EnableStreams         = true;
   ```
2. To run **only safe** examples → keep `EnableTradingExamples = false`.
3. When ready to try trading:

   * Set `EnableTradingExamples = true`.
   * Pass **real, valid tickets** into modify/close/close‑by helpers (copy from `ShowOpenedOrders`).
   * Prefer **DEMO** first.
4. Run normally (`dotnet run --project MetaRPC.CSharpMT4.csproj`).

---

## ✅ Safety Checklist (before enabling trading)

* You’re on a **DEMO** account for first runs.
* `ShowAccountSummary` looks sane — you understand `Equity` and `FreeMargin`.
* Symbol, volume, SL/TP comply with broker rules (check `ShowSymbolParams`).
* Tickets are **real** (from `ShowOpenedOrders` / `ShowOpenedOrderTickets`).

---

## ❓ FAQ

**Do I need to call `MT4Account` directly?**
No. Use `MT4Service` methods (`_service.Show...`, `_service.Stream...`). `MT4Account` is managed inside the service and in `Program.cs` during connect/disconnect.

**Where do connection settings live?**
In `appsettings.json` → `MT4Options` section (`User`, `Password`, `ServerName` or `Host`+`Port`, `DefaultSymbol`).

**Why do some streams stop quickly?**
Examples intentionally stop after a **first message** or a **short timeout** (e.g., `ShowRealTimeQuotes` uses a 5s timeout; `StreamQuotesForSymbolsAsync` runs for 10s). Tune or remove those limits in your own code.

**I see `RpcException: Cancelled`. Is that bad?**
Not necessarily — when we cancel via token or a short example timeout, a `Cancelled` status is expected and treated as a clean completion.

---

## 🧰 Troubleshooting

* *“Nothing happens in trading examples”* → `EnableTradingExamples` is likely still `false`.
* *“Modify/close fails”* → Invalid ticket or unsupported action (e.g., trying to delete a market order; `CloseBy` requires opposite positions of matching volume).
* *“Streams stop too soon”* → Increase the example timeouts or remove the first‑message `break;` in wrappers.
* *“Not connected / missing options”* → Ensure `ServerName` **or** `Host`+`Port` is set in `appsettings.json` and credentials are correct.

---

**That’s it — a clean `Main`, safe defaults, and powerful features behind a single switch. Flip the toggle when you’re ready, and proceed step by step.**
