# Getting Opened Orders

> **Request:** retrieve currently opened orders from MT4
> Fetch all active (non-closed) trade positions on the account.

---

### Code Example

```csharp
// Using service wrapper
await _service.ShowOpenedOrders();

// Or directly from MT4Account
var result = await _mt4.OpenedOrdersAsync();
foreach (var order in result.OrderInfos)
{
    Console.WriteLine($"[{order.OrderType}] Ticket: {order.Ticket}, Symbol: {order.Symbol}, " +
                      $"Lots: {order.Lots}, OpenPrice: {order.OpenPrice}, Profit: {order.Profit}, " +
                      $"OpenTime: {order.OpenTime}");
}
```

---

### ✨ Method Signature

```csharp
Task<OpenedOrdersData> OpenedOrdersAsync(
    DateTime? deadline = null,
    CancellationToken cancellationToken = default
)
```

---

## 🔽 Input

No required input parameters.

Optional:

* **`deadline`** (`DateTime?`) — optional UTC deadline.
* **`cancellationToken`** (`CancellationToken`) — optional cancellation token.

---

## ⬆️ Output

Returns an **`OpenedOrdersData`** object with a collection of order information:

| Field        | Type              | Description                           |
| ------------ | ----------------- | ------------------------------------- |
| `OrderInfos` | `List<OrderInfo>` | List of opened orders on the account. |

Each `OrderInfo` includes:

| Field       | Type     | Description                                |
| ----------- | -------- | ------------------------------------------ |
| `Ticket`    | `int`    | Unique ticket ID for the order.            |
| `Symbol`    | `string` | Trading symbol (e.g., "EURUSD").           |
| `Lots`      | `double` | Volume of the order in lots.               |
| `OpenPrice` | `double` | Price at which the order was opened.       |
| `Profit`    | `double` | Current floating profit/loss of the order. |
| `OpenTime`  | `string` | Timestamp when the order was opened.       |
| `OrderType` | `string` | Type of the order (e.g., Buy, Sell, etc).  |

---

## 🎯 Purpose

Use this method to retrieve and display a list of all currently open orders. Useful for:

* Monitoring active positions
* Building UI dashboards with real-time order info
* Analyzing exposure, profit/loss, and position distribution

It provides core data for managing open trade state within any MT4 integration.
