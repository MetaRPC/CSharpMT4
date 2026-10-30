# MT4Service — Mid-Level API Overview

> **Not just a wrapper.** `MT4Service` is the typed, data-returning facade over the low-level [`MT4Account`](../MT5Account/MT4Account.Master.Overview.md): it forwards the data calls 1:1 and adds **convenience getters** that have no single low-level equivalent.

---

## 🎯 Where it sits

```
MT4Account (low-level)  →  raw gRPC: build a Request, read Reply.Data
     ↓
MT4Service (mid-level)  →  forwards data methods 1:1 + convenience getters   ← YOU ARE HERE
     ↓
MT4Sugar  (high-level)  →  snapshots, risk sizing, bulk ops (extension methods on MT4Service)
```

**What the mid-level adds:**

- ✅ A single, stable, typed entry point the whole app (and the Sugar layer) builds on.
- ✅ **Convenience getters** — `GetBalanceAsync`, `GetDigitsAsync`, `GetSpreadAsync` (computed) — one call instead of re-deriving from a bulk reply.
- ✅ Same objects as low-level for the 1:1 wrappers, so you can drop down to `MT4Account` any time.

```csharp
// Low-level: fetch the quote, then compute spread yourself
var q  = await account.QuoteAsync("EURUSD");
var pt = Math.Pow(10, -(int)(await account.SymbolParamsManyAsync("EURUSD")).SymbolInfos[0].Digits);
var spreadPts = (q.Ask - q.Bid) / pt;

// Mid-level: one call
var spreadPts2 = await service.GetSpreadAsync("EURUSD");
```

---

## 📂 Method groups

| Group | What's inside |
| ----- | ------------- |
| **[Account Methods](Account_Methods.md)** | Snapshot, statistics, equity history + value getters (balance/equity/margin/…). |
| **[Symbol Methods](Symbol_Methods.md)** | Quotes, symbols, history/bars, tick values, Market Watch + value getters (digits/point/spread/…). |
| **[Orders Methods](Orders_Methods.md)** | Open orders, tickets, order history. |
| **[Trading Methods](Trading_Methods.md)** | Send / modify / close / close-by. |
| **[Streaming Methods](Streaming_Methods.md)** | Live ticks, trade events, profit & ticket snapshots, connection state. |

---

## 🔌 Connection

Setup lives on the service too (it forwards to `MT4Account`):

```csharp
await service.ConnectByServerNameAsync("EGlobal-Demo", baseChartSymbol: "EURUSD");
// ...or: await service.ConnectByHostPortAsync("mt4.mrpc.pro", 443, "EURUSD");
var id = await service.GetIdAsync();      // terminal session GUID (usually not needed)
await service.DisconnectAsync();
```

| Method | Returns | Notes |
| ------ | ------- | ----- |
| `ConnectByServerNameAsync(serverName, baseChartSymbol, waitForTerminalIsAlive, timeoutSeconds, …)` | `Task` | Primary connect path. |
| `ConnectByHostPortAsync(host, port, baseChartSymbol, …)` | `Task` | Optional host:port fallback. |
| `GetIdAsync(ct)` | `Task<Guid>` | Terminal session id (auto-attached after connect). |
| `DisconnectAsync(delete = false)` | `Task` | Disconnect (optionally delete the instance). |

---

## 🔗 Usage pattern

* **Request/Reply:** `var data = await service.MethodAsync(args);` → returns the typed `*Data` object.
* **Streaming:** `await foreach (var x in service.OnMethodAsync(args, ct)) { … }`.
* Timestamps are **UTC**; failures throw `ApiExceptionMT4` (transient gRPC errors are retried with reconnection).

> For the full Request/Reply shapes, fields and enums behind any method, follow the **spec** links in each group into the low-level [MT4Account](../MT5Account/MT4Account.Master.Overview.md) docs.
