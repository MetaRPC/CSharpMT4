# Getting Order History

> **Request:** retrieve historical orders for a specified time range
> Fetch all closed orders from the trading account history within a defined window.

---

### Code Example

```csharp
// --- Quick use (service wrapper) ---
await _service.ShowOrdersHistory();

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

var from = DateTime.UtcNow.AddDays(-7);
var to   = DateTime.UtcNow;

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

var history = await _mt4.OrdersHistoryAsync(
    sortType: EnumOrderHistorySortType.HistorySortByCloseTimeDesc,
    from: from,
    to: to,
    page: null,            // optional paging
    itemsPerPage: null,    // optional paging
    deadline: null,
    cancellationToken: cts.Token);

foreach (var order in history.OrdersInfo)
{
    Console.WriteLine($"[{order.OrderType}] Ticket: {order.Ticket}, Symbol: {order.Symbol}, " +
                      $"Lots: {order.Lots}, Open: {order.OpenPrice}, Close: {order.ClosePrice}, " +
                      $"Profit: {order.Profit}, CloseTime: {order.CloseTime}");
}
```

---

### Method Signatures

```csharp
// Service wrapper (example implementation)
Task ShowOrdersHistory();
```

```csharp
// Low-level account call
Task<OrdersHistoryData> OrdersHistoryAsync(
    EnumOrderHistorySortType sortType = EnumOrderHistorySortType.HistorySortByCloseTimeDesc,
    DateTime? from = null,
    DateTime? to = null,
    int? page = null,
    int? itemsPerPage = null,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default
);
```

---

## 🔽 Input

* **`sortType`** (`EnumOrderHistorySortType`) — sorting mode:

  * `HistorySortByOpenTimeAsc`
  * `HistorySortByOpenTimeDesc`
  * `HistorySortByCloseTimeAsc`
  * `HistorySortByCloseTimeDesc`

* **`from`** (`DateTime?`) — start time (UTC). Optional; if omitted, server default applies.

* **`to`** (`DateTime?`) — end time (UTC). Optional.

Optional paging:

* **`page`** (`int?`) — page number (>= 1).
* **`itemsPerPage`** (`int?`) — items per page (>= 1).

Common optional:

* **`deadline`** (`DateTime?`) — per‑call deadline.
* **`cancellationToken`** (`CancellationToken`) — cancellation control.

> The client validates `from <= to` (when both present) and non‑negative paging values.

---

## ⬆️ Output

Returns **`OrdersHistoryData`** with:

| Field        | Type              | Description                         |
| ------------ | ----------------- | ----------------------------------- |
| `OrdersInfo` | `List<OrderInfo>` | List of historical (closed) orders. |

Each **`OrderInfo`** typically includes:

| Field        | Type                 | Description                          |
| ------------ | -------------------- | ------------------------------------ |
| `Ticket`     | `int`                | Unique ID of the order.              |
| `Symbol`     | `string`             | Trading symbol (e.g., `EURUSD`).     |
| `Lots`       | `double`             | Volume of the order in lots.         |
| `OpenPrice`  | `double`             | Entry price.                         |
| `ClosePrice` | `double`             | Exit price.                          |
| `Profit`     | `double`             | Realized profit/loss.                |
| `OrderType`  | `ENUM_ORDER_TYPE_TF` | Type of the order (Buy, Sell, etc.). |
| `OpenTime`   | `string`             | Time when the order was opened.      |
| `CloseTime`  | `string`             | Time when the order was closed.      |
| `Sl`         | `double`             | Stop Loss price (if set).            |
| `Tp`         | `double`             | Take Profit price (if set).          |
| `Magic`      | `int`                | Magic number (if set).               |
| `Comment`    | `string`             | Attached order comment.              |
| `Expiration` | `string`             | Pending order expiration (if used).  |

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

Retrieve completed trades within a given time window for:

* Historical trade analysis
* Auditing and reporting
* Exporting logs for compliance/analytics

---

## 🧩 Notes & Tips

* **UTC throughout.** Client sends timestamps as UTC; convert only at presentation.
* **Paginate large ranges.** For long histories, use `page/itemsPerPage` to reduce payload and latency.
* **Sorting first, then paging.** The sort is applied before pagination by the server/proto contract.

---

## ⚠️ Pitfalls

* **Reversed range.** If `from > to`, the client throws a helpful error before calling the server.
* **Partial data windows.** Broker servers may limit history depth; request smaller chunks if very old data is needed.

---

## 🧪 Testing Suggestions

* **Happy path:** Request the last 7 days and verify non‑empty results on an active demo.
* **Edge cases:** Call with `from == to` or omit both to check server defaults.
* **Paging:** Request `itemsPerPage = 5` and iterate pages; ensure deterministic ordering with a fixed `sortType`.
