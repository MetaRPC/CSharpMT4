# Streaming Opened Order Profits

> **Request:** subscribe to real-time stream of floating profit/loss per open order
> Starts a streaming channel to monitor floating profit for all active trades.

---

### Code Example

```csharp
// --- Quick use (service wrapper) ---
// Demo: logs once and exits inside the method.
await _service.StreamOpenedOrderProfits();

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.
// Tip: keep the interval modest to reduce load; cancel when you’re done.

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await foreach (var info in _mt4.OnOpenedOrdersProfitAsync(intervalMs: 1000, cts.Token))
{
    Console.WriteLine(
        $"[P/L] Ticket={info.Ticket} {info.Symbol} Lots={info.Lots} Profit={info.Profit} " +
        $"Open={info.OpenPrice} Now={info.CurrentPrice} Type={info.OrderType}"
    );

    // For demo purposes we exit on first update;
    // in production remove this break to keep streaming.
    break;
}
```

---

### Method Signature

```csharp
IAsyncEnumerable<OnOpenedOrdersProfitOrderInfo> OnOpenedOrdersProfitAsync(
    int intervalMs,
    [EnumeratorCancellation] CancellationToken cancellationToken = default
)
```

---

## 🔽 Input

* **`intervalMs`** (`int`) — required. Polling interval in **milliseconds** between consecutive updates.
* **`cancellationToken`** (`CancellationToken`, optional) — to cancel the streaming manually.

---

## ⬆️ Output

Returns a stream (`IAsyncEnumerable<OnOpenedOrdersProfitOrderInfo>`) with the following structure:

### Structure: `OnOpenedOrdersProfitOrderInfo`

| Field          | Type                 | Description                                      |
| -------------- | -------------------- | ------------------------------------------------ |
| `Ticket`       | `int`                | Order ticket ID                                  |
| `Symbol`       | `string`             | Trading symbol (e.g., "EURUSD")                  |
| `Lots`         | `double`             | Trade volume in lots                             |
| `Profit`       | `double`             | Current floating profit/loss in account currency |
| `OpenPrice`    | `double`             | Price at which the position was opened           |
| `CurrentPrice` | `double`             | Current market price of the symbol               |
| `OpenTime`     | `string`             | Order open time in UTC string format             |
| `OrderType`    | `ENUM_ORDER_TYPE_TF` | Type of trade: Buy, Sell, etc.                   |
| `Magic`        | `int`                | Magic number for identifying order source        |
| `Comment`      | `string`             | Custom comment attached to the order             |

---

### ENUM: `ENUM_ORDER_TYPE_TF`

| Value                  | Description        |
| ---------------------- | ------------------ |
| `OrderTypeTfBuy`       | Buy order          |
| `OrderTypeTfSell`      | Sell order         |
| `OrderTypeTfBuyLimit`  | Pending Buy Limit  |
| `OrderTypeTfSellLimit` | Pending Sell Limit |
| `OrderTypeTfBuyStop`   | Pending Buy Stop   |
| `OrderTypeTfSellStop`  | Pending Sell Stop  |

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
