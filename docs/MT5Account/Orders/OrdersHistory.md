# Getting Order History

> **Request:** retrieve historical orders for a specified time range
> Fetch all closed orders from the trading account history within a defined window.

**API Information:**

* **SDK wrapper:** `MT4Account.OrdersHistoryAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.AccountHelper`
* **Proto definition:** `OrdersHistory` (in the AccountHelper service)

### RPC

* **Service:** `mt4_term_api.AccountHelper`
* **Method:** `OrdersHistory(OrdersHistoryRequest) → OrdersHistoryReply`
* **Low‑level client (generated):** `AccountHelper.AccountHelperClient.OrdersHistory(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async Task<OrdersHistoryData> OrdersHistoryAsync(
            EnumOrderHistorySortType sortType = EnumOrderHistorySortType.HistorySortByCloseTimeDesc,
            DateTime from = default,
            DateTime to = default,
            int? page = null,
            int? itemsPerPage = null,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default);
    }
}
```

**Request message:**

`OrdersHistoryRequest { sortType, from, to, page?, itemsPerPage? }`

**Reply message:**

`OrdersHistoryReply { data: OrdersHistoryData }`

---

### Code Example

```csharp
// --- Service wrapper (typed, data-returning) ---
var hist = await _service.OrdersHistoryAsync(
    DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
Console.WriteLine($"{hist.OrdersInfo.Count} closed order(s)");

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

var from = DateTime.UtcNow.AddDays(-7);
var to   = DateTime.UtcNow;

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

var history = await _mt4.OrdersHistoryAsync(
    sortType: EnumOrderHistorySortType.HistorySortByCloseTimeDesc,
    from: from,            // DateTime (UTC)
    to: to,                // DateTime (UTC)
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
| `OrdersInfo` | `RepeatedField<HistoryOrderInfo>` | List of historical (closed) orders. |

Each **`HistoryOrderInfo`** includes (selected fields):

| Field         | Type              | Description                          |
| ------------- | ----------------- | ------------------------------------ |
| `Ticket`      | `int`             | Unique ID of the order.              |
| `Symbol`      | `string`          | Trading symbol (e.g., `EURUSD`).     |
| `Lots`        | `double`          | Volume of the order in lots.         |
| `OpenPrice`   | `double`          | Entry price.                         |
| `ClosePrice`  | `double`          | Exit price.                          |
| `Profit`      | `double`          | Realized profit/loss.                |
| `Swap`        | `double`          | Accrued swap.                        |
| `Commision`   | `double`          | Commission.                          |
| `StopLoss`    | `double`          | Stop-loss price (0 if none).         |
| `TakeProfit`  | `double`          | Take-profit price (0 if none).       |
| `MagicNumber` | `int`             | Magic number (if set).               |
| `Comment`     | `string`          | Attached order comment.              |
| `OrderType`   | `OpenedOrderType` | Order type — see enum below.         |
| `OpenTime`    | `Timestamp`       | Time the order was opened.           |
| `CloseTime`   | `Timestamp`       | Time the order was closed.           |

---

## 🧱 Related enums (from proto)

### `EnumOrderHistorySortType` — parameter `sortType`

* `HistorySortByOpenTimeAsc = 0`
* `HistorySortByOpenTimeDesc = 1`
* `HistorySortByCloseTimeAsc = 2`
* `HistorySortByCloseTimeDesc = 3` (default)
* `HistorySortByOrderTicketIdAsc = 4`
* `HistorySortByOrderTicketIdDesc = 5`

### `OpenedOrderType` — field `OrderType`

* `OoOpBuy = 0` · `OoOpSell = 1` — market buy/sell
* `OoOpBuylimit = 2` · `OoOpSelllimit = 3` — pending limit
* `OoOpBuystop = 4` · `OoOpSellstop = 5` — pending stop
* `OoOpBalance = 6` · `OoOpCredit = 7` — balance/credit operations

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
