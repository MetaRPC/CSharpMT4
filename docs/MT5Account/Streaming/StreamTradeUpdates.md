# Streaming Trade Updates

> **Request:** subscribe to real-time trade update stream
> Starts a server-side stream to receive trade activity as it happens.

**API Information:**

* **SDK wrapper:** `MT4Account.OnTradeAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.SubscriptionService`
* **Proto definition:** `OnTrade` (in the SubscriptionService service)

### RPC

* **Service:** `mt4_term_api.SubscriptionService`
* **Method:** `OnTrade(OnTradeRequest) → stream OnTradeReply`
* **Low‑level client (generated):** `SubscriptionService.SubscriptionServiceClient.OnTrade(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async IAsyncEnumerable<OnTradeData> OnTradeAsync(
            [EnumeratorCancellation] CancellationToken ct = default);
    }
}
```

**Request message:**

`OnTradeRequest {}`

**Reply message:**

`OnTradeReply { data: OnTradeData } (server stream)`

---

### Code Example

```csharp
// --- Service wrapper (streaming; same shape as low-level) ---
// await foreach (var e in _service.OnTradeAsync(ct)) { ... }

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // cancel window for demo

await foreach (var evt in _mt4.OnTradeAsync(cts.Token))
{
    Console.WriteLine($"Trade event: {evt.Type} (equity={evt.AccountInfo?.Equity})");
    break; // demo: exit after first event
}
```

---

## 🔽 Input

No required parameters, except:

* **`cancellationToken`** (`CancellationToken`, optional) — used to cancel the stream manually.

---

## ⬆️ Output

Returns a stream (`IAsyncEnumerable<OnTradeData>`) where each item represents a trade event:

| Field                    | Type                            | Description                                   |
| ------------------------ | ------------------------------- | --------------------------------------------- |
| `Type`                   | `MT4_SUB_ENUM_EVENT_GROUP_TYPE` | Event group — see enum below.                 |
| `EventData`              | `OnTadeEventData`               | The trade event payload (orders affected).    |
| `AccountInfo`            | `OnEventAccountInfo`            | Account snapshot at the event (equity, etc.). |
| `TerminalInstanceGuidId` | `string`                        | Terminal instance that produced the event.    |

---

## 🧱 Related enums (from proto)

### `MT4_SUB_ENUM_EVENT_GROUP_TYPE` — field `Type`

* `OrderProfit = 0` — periodic profit/price update for open orders
* `OrderUpdate = 1` — an order was created / modified / closed

---

## 🎯 Purpose

Subscribe to **real-time trade activity** — executed orders, closes, and other trade events sent by the server. Suitable for:

* Live dashboards / client UIs
* Post-trade hooks (logging, risk checks)
* Auditing trade flow in automated systems

---

## 🧩 Notes & Tips

* The stream is **continuous** — manage lifecycle with cancellation tokens.
* Client performs **auto-reconnect** on transient gRPC errors; if you reprocess events after reconnect, deduplicate by `Ticket` + timestamps.
