# Streaming Trade Updates

> **Request:** subscribe to real-time trade update stream
> Starts a server-side stream to receive trade activity as it happens.

---

### Code Example

```csharp
// --- Quick use (service wrapper) ---
// Prints a marker when a trade event arrives (demo stops after 1st event).
await _service.StreamTradeUpdates();

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // cancel window for demo

await foreach (var trade in _mt4.OnTradeAsync(cts.Token))
{
    // TODO: map fields you need from trade.TradeInfo (see Output section)
    Console.WriteLine("Trade update received.");
    break; // demo: exit after first event
}
```

---

### Method Signature

```csharp
IAsyncEnumerable<OnTradeData> OnTradeAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default
)
```

---

## 🔽 Input

No required parameters, except:

* **`cancellationToken`** (`CancellationToken`, optional) — used to cancel the stream manually.

---

## ⬆️ Output

Returns a stream (`IAsyncEnumerable<OnTradeData>`) where each item represents a trade event:

| Field       | Type        | Description                         |
| ----------- | ----------- | ----------------------------------- |
| `TradeInfo` | `TradeInfo` | Structure containing trade details. |

Structure of **`TradeInfo`**:

| Field       | Type                 | Description                              |
| ----------- | -------------------- | ---------------------------------------- |
| `Ticket`    | `int`                | Unique ID of the trade order             |
| `Symbol`    | `string`             | Trading symbol (e.g., "EURUSD")          |
| `Lots`      | `double`             | Trade volume in lots                     |
| `OpenPrice` | `double`             | Price at which the trade was opened      |
| `Profit`    | `double`             | Current profit or loss of the trade      |
| `OpenTime`  | `string`             | UTC timestamp when the trade was opened  |
| `OrderType` | `ENUM_ORDER_TYPE_TF` | Type of trade (e.g., Buy, Sell)          |
| `Comment`   | `string`             | Custom comment associated with the trade |
| `Magic`     | `int`                | Magic number used to tag the trade       |

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

Subscribe to **real-time trade activity** — executed orders, closes, and other trade events sent by the server. Suitable for:

* Live dashboards / client UIs
* Post-trade hooks (logging, risk checks)
* Auditing trade flow in automated systems

---

## 🧩 Notes & Tips

* The stream is **continuous** — manage lifecycle with cancellation tokens.
* Client performs **auto-reconnect** on transient gRPC errors; if you reprocess events after reconnect, deduplicate by `Ticket` + timestamps.
