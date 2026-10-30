# User Code Sandbox

> A ready-to-edit playground for your own MT4 trading logic — the connection and `MT4Service` are already wired up, you just fill in the body.

---

## 🎯 What it is

The sandbox is the file **`examples/UserCode/11_ProgramUserCode.cs`** (class `ProgramUserCode`). It hands you a **connected `MT4Service`** and a base symbol so you can experiment without writing any setup/connection code.

It is wired into the examples menu as **`11. USER CODE sandbox`**.

---

## 🚀 How to run

```bash
# non-interactive (runs only the sandbox)
dotnet run -- --example=11
# or: dotnet run -- --example=sandbox

# interactive menu → choose 11
dotnet run
```

Connection settings come from `appsettings.json` / environment — see **[Getting Started → Configuration](GETTING_STARTED.md#configuration-provide-your-connection-data)**.

---

## ✍️ Where to write your code

Open `examples/UserCode/11_ProgramUserCode.cs` and edit the body of `RunAsync` between the markers:

```csharp
public static async Task RunAsync(MT4Service svc, string symbol = "EURUSD", CancellationToken ct = default)
{
    // ── YOUR CODE STARTS HERE ↓ ───────────────────────────────

    // read-only starters (safe):
    var account = await svc.GetAccountSnapshotAsync(ct: ct);
    Console.WriteLine($"Account: {account}");

    var sym = await svc.GetSymbolSnapshotAsync(symbol, ct: ct);
    Console.WriteLine($"Symbol:  {sym}");

    // ── YOUR CODE ENDS HERE ↑ ─────────────────────────────────
}
```

Everything on `svc` is available — the typed [MT4Service](../MT4Service/MT4Service.Overview.md) methods **and** the [MT4Sugar](../MT4Sugar/MT4Sugar.Overview.md) extension helpers.

---

## 🧪 Examples to drop in

```csharp
// Sizing math (no orders)
var volume  = await svc.CalcVolumeForRiskAsync(symbol, stopPoints: 200, riskMoney: 50, ct: ct);
var slPrice = await svc.PriceFromOffsetPointsAsync(symbol, fromAsk: true, offsetPoints: -200, ct: ct);
Console.WriteLine($"risk-sized volume: {volume}, SL: {slPrice}");

// Live tick sample (3 seconds)
using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
cts.CancelAfter(TimeSpan.FromSeconds(3));
try { await foreach (var t in svc.ReadTicks(symbol, cts.Token)) Console.WriteLine($"{t.SymbolTick?.Bid}/{t.SymbolTick?.Ask}"); }
catch (OperationCanceledException) { }

// Trading — UNCOMMENT DELIBERATELY (places real orders)
// var buy = await svc.BuyMarketByRiskAsync(symbol, stopPoints: 200, riskMoney: 50, ct: ct);
// await svc.ModifySlTpAsync(buy.Ticket, tp: sym.Ask + 400 * sym.Point, ct: ct);
// await svc.CloseByTicketAsync(buy.Ticket, ct: ct);
```

---

## 🔌 Running it standalone (own connection)

Inside the menu the sandbox reuses the already-connected service. To run it **outside** the menu with its own connection, call the standalone entry point — it connects via `ConnectionHelper`, runs your `RunAsync`, then disconnects:

```csharp
await ProgramUserCode.RunStandaloneAsync("EURUSD");
```

---

## 🧩 Notes & Tips

* **Trading is real.** Anything that sends/modifies/closes orders acts on the configured account — keep those lines commented until you mean them.
* **Streams need a token.** Always bound `ReadTicks` / `ReadTrades` with a `CancellationToken` or timeout.
* **Start read-only.** Snapshots, counts and history are safe to run repeatedly while you learn the API.
* **Layer of choice.** Use Sugar for one-liners; drop to `MT4Service` for typed data; drop to `MT4Account` only when you need the raw request/reply.
