# Streaming Opened Order Profits

> **Request:** subscribe to real-time stream of floating profit/loss per open order
> Starts a streaming channel to monitor floating profit for all active trades.

**API Information:**

* **SDK wrapper:** `MT4Account.OnOpenedOrdersProfitAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.SubscriptionService`
* **Proto definition:** `OnOpenedOrdersProfit` (in the SubscriptionService service)

### RPC

* **Service:** `mt4_term_api.SubscriptionService`
* **Method:** `OnOpenedOrdersProfit(OnOpenedOrdersProfitRequest) → stream OnOpenedOrdersProfitReply`
* **Low‑level client (generated):** `SubscriptionService.SubscriptionServiceClient.OnOpenedOrdersProfit(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async IAsyncEnumerable<OnOpenedOrdersProfitData> OnOpenedOrdersProfitAsync(
            int intervalMs = 1000,
            [EnumeratorCancellation] CancellationToken ct = default);
    }
}
```

**Request message:**

`OnOpenedOrdersProfitRequest { pullIntervalMilliseconds: int }`

**Reply message:**

`OnOpenedOrdersProfitReply { data: OnOpenedOrdersProfitData } (server stream)`

---

### Code Example

```csharp
// --- Service wrapper (streaming; same shape as low-level) ---
// await foreach (var upd in _service.OnOpenedOrdersProfitAsync(1000, ct)) { ... }

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.
// Tip: keep the interval modest to reduce load; cancel when you’re done.

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await foreach (var upd in _mt4.OnOpenedOrdersProfitAsync(intervalMs: 1000, cts.Token))
{
    foreach (var o in upd.OpenedOrdersWithProfitUpdated)
        Console.WriteLine($"[P/L] #{o.Ticket} {o.Symbol} Lots={o.Lots} Profit={o.OrderProfit} Open={o.OpenPrice}");
    Console.WriteLine($"Equity={upd.AccountInfo?.Equity}");

    // For demo purposes we exit on first update;
    // in production remove this break to keep streaming.
    break;
}
```

---

## 🔽 Input

* **`intervalMs`** (`int`) — required. Polling interval in **milliseconds** between consecutive updates.
* **`cancellationToken`** (`CancellationToken`, optional) — to cancel the streaming manually.

---

## ⬆️ Output

Returns a stream (`IAsyncEnumerable<OnOpenedOrdersProfitData>`). Each item is a snapshot:

| Field                           | Type                                           | Description                           |
| ------------------------------- | ---------------------------------------------- | ------------------------------------- |
| `Type`                          | `MT4_SUB_ENUM_EVENT_GROUP_TYPE`                | Event group — see enum below.         |
| `OpenedOrdersWithProfitUpdated` | `RepeatedField<OnOpenedOrdersProfitOrderInfo>` | Per-order profit rows (below).        |
| `AccountInfo`                   | `OnEventAccountInfo`                            | Account snapshot (balance/equity/…).  |
| `TerminalInstanceGuidId`        | `string`                                        | Terminal instance id.                 |

### `OnOpenedOrdersProfitOrderInfo` (each row)

| Field         | Type        | Description               |
| ------------- | ----------- | ------------------------- |
| `Ticket`      | `int`       | Order ticket.             |
| `Symbol`      | `string`    | Symbol.                   |
| `Lots`        | `double`    | Volume (lots).            |
| `OpenPrice`   | `double`    | Open price.               |
| `OrderProfit` | `double`    | Current floating profit.  |
| `Swap`        | `double`    | Swap.                     |
| `Commission`  | `double`    | Commission.               |
| `OpenTime`    | `Timestamp` | Open time.                |
| `MagicNumber` | `int`       | Magic number.             |
| `Comment`     | `string`    | Order comment.            |

---

## 🧱 Related enums (from proto)

### `MT4_SUB_ENUM_EVENT_GROUP_TYPE` — field `Type`

* `OrderProfit = 0` — periodic profit update (this stream)
* `OrderUpdate = 1` — order create / modify / close

---

## 🎯 Purpose

This method allows **live monitoring of floating P/L per open order**, enabling use cases such as:

* Real-time UI dashboards showing per-trade profit
* Risk monitoring systems
* Alerting systems for high drawdown or profit conditions

It's optimized for continuous updates, with polling controlled by `intervalMs`.

---

## 🧩 Notes & Tips

* **Continuous stream:** The method yields until you cancel via `CancellationToken` or exit your loop.
* **Reconnects on hiccups:** Client logic restarts the stream on transient gRPC errors; brief gaps are normal during recovery.
* **Interval trade-off:** Smaller `intervalMs` means more frequent updates and higher load; common values are 500–2000 ms.
* **Empty portfolio:** With no open orders you may see few/no updates; this is expected—keep the stream or cancel.
* **Currency:** `Profit` values are reported in the **account currency**.

---

## ⚠️ Pitfalls

* **Forgetting cancellation:** Without a token or break condition, the loop runs indefinitely.
* **Too-aggressive intervals:** Very small `intervalMs` can stress the terminal and your app.
* **Assuming tick timing:** Updates follow your interval, not every market tick.

---

## 🧪 Testing Suggestions

* **Happy path:** Open a small market order; verify that `Profit` fluctuates and ticket/symbol match.
* **Zero orders:** Ensure your code handles the quiet stream without errors.
* **Cancellation:** Cancel after a few seconds and confirm a clean shutdown (no unhandled exceptions).
