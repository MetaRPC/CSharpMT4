# Streaming Opened Order Profits

> **Request:** subscribe to real-time stream of floating profit/loss per open order
> Starts a streaming channel to monitor floating profit for all active trades.

---

### Code Example

```csharp
// Using service wrapper
await _service.StreamOpenedOrderProfits();

// Or directly from MT4Account
await foreach (var profit in _mt4.OnOpenedOrdersProfitAsync(1000))
{
    Console.WriteLine("Profit update received.");
    break; // for demo purposes
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

It's optimized for minimal payloads and continuous updates, with polling interval controlled by `intervalMs`.
