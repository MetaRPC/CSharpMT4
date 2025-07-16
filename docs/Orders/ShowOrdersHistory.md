# Getting Order History

> **Request:** retrieve historical orders for a specified time range
> Fetch all closed orders from the trading account history within a defined window.

---

### Code Example

```csharp
// Using service wrapper
await _service.ShowOrdersHistory();

// Or directly from MT4Account
var from = DateTime.UtcNow.AddDays(-7);
var to = DateTime.UtcNow;

var history = await _mt4.OrdersHistoryAsync(
    sortType: EnumOrderHistorySortType.HistorySortByCloseTimeDesc,
    from: from,
    to: to
);

foreach (var order in history.OrdersInfo)
{
    Console.WriteLine($"[{order.OrderType}] Ticket: {order.Ticket}, Symbol: {order.Symbol}, " +
                      $"Lots: {order.Lots}, Open: {order.OpenPrice}, Close: {order.ClosePrice}, " +
                      $"Profit: {order.Profit}, CloseTime: {order.CloseTime}");
}
```

---

### Method Signature

```csharp
Task<OrdersHistoryData> OrdersHistoryAsync(
    EnumOrderHistorySortType sortType,
    DateTime from,
    DateTime to,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default
)
```

---

## 🔽 Input

* **`sortType`** (`EnumOrderHistorySortType`) — defines sorting logic. Possible values:

  * `HistorySortByOpenTimeAsc`
  * `HistorySortByOpenTimeDesc`
  * `HistorySortByCloseTimeAsc`
  * `HistorySortByCloseTimeDesc`

* **`from`** (`DateTime`) — start time of the history window.

* **`to`** (`DateTime`) — end time of the history window.

Optional:

* **`deadline`** (`DateTime?`) — optional timeout.
* **`cancellationToken`** (`CancellationToken`) — cancellation control.

---

## ⬆️ Output

Returns an **`OrdersHistoryData`** object containing:

| Field        | Type              | Description                         |
| ------------ | ----------------- | ----------------------------------- |
| `OrdersInfo` | `List<OrderInfo>` | List of historical (closed) orders. |

Each `OrderInfo` includes:

| Field        | Type     | Description                           |
| ------------ | -------- | ------------------------------------- |
| `Ticket`     | `int`    | Unique ID of the order.               |
| `Symbol`     | `string` | Trading symbol (e.g., EURUSD).        |
| `Lots`       | `double` | Volume of the order in lots.          |
| `OpenPrice`  | `double` | Entry price of the order.             |
| `ClosePrice` | `double` | Exit price of the order.              |
| `Profit`     | `double` | Final realized profit/loss.           |
| `OrderType`  | `string` | Type of order (Buy, Sell, etc).       |
| `OpenTime`   | `string` | Time when the order was opened.       |
| `CloseTime`  | `string` | Time when the order was closed.       |
| `Sl`         | `double` | Stop Loss price (if set).             |
| `Tp`         | `double` | Take Profit price (if set).           |
| `Magic`      | `int`    | Magic number for programmatic orders. |
| `Comment`    | `string` | Custom comment attached to the order. |
| `Expiration` | `string` | Expiration time for pending orders.   |

---

## 🎯 Purpose

This method retrieves completed trades within a given time window.
It's useful for:

* Historical trade analysis
* Auditing or reporting
* Exporting trade logs for compliance or analytics

It's the standard way to fetch past closed positions from MT4.
