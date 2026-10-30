# Getting Opened Orders

> **Request:** retrieve currently opened orders from MT4
> Fetch all active (non‑closed) trade positions on the account.

**API Information:**

* **SDK wrapper:** `MT4Account.OpenedOrdersAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.AccountHelper`
* **Proto definition:** `OpenedOrders` (in the AccountHelper service)

### RPC

* **Service:** `mt4_term_api.AccountHelper`
* **Method:** `OpenedOrders(OpenedOrdersRequest) → OpenedOrdersReply`
* **Low‑level client (generated):** `AccountHelper.AccountHelperClient.OpenedOrders(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async Task<OpenedOrdersData> OpenedOrdersAsync(
            EnumOpenedOrderSortType sortType = EnumOpenedOrderSortType.SortByOpenTimeAsc,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default);
    }
}
```

**Request message:**

`OpenedOrdersRequest { sortType: EnumOpenedOrderSortType }`

**Reply message:**

`OpenedOrdersReply { data: OpenedOrdersData }`

---

### Code Example

```csharp
// --- Service wrapper (typed, data-returning) ---
var opened = await _service.OpenedOrdersAsync();
Console.WriteLine($"{opened.OrderInfos.Count} open order(s)");

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

var result = await _mt4.OpenedOrdersAsync(
    sortType: EnumOpenedOrderSortType.SortByOpenTimeAsc, // default
    deadline: null,
    cancellationToken: default);

foreach (var order in result.OrderInfos)
{
    Console.WriteLine($"[{order.OrderType}] Ticket: {order.Ticket}, Symbol: {order.Symbol}, " +
                      $"Lots: {order.Lots}, OpenPrice: {order.OpenPrice}, Profit: {order.Profit}, " +
                      $"OpenTime: {order.OpenTime}");
}
```

---

## 🔽 Input

No required parameters.

Optional:

* **`sortType`** (`EnumOpenedOrderSortType`) — sorting mode for returned orders.
  *Default:* `SortByOpenTimeAsc`. (Refer to your proto for the full enum list.)
* **`deadline`** (`DateTime?`) — optional UTC deadline.
* **`cancellationToken`** (`CancellationToken`) — optional cancellation token.

---

## ⬆️ Output

Returns **`OpenedOrdersData`** with a collection of order information:

| Field        | Type              | Description                          |
| ------------ | ----------------- | ------------------------------------ |
| `OrderInfos` | `RepeatedField<OpenedOrderInfo>` | List of all currently opened orders. |

Each **`OpenedOrderInfo`** includes (selected fields):

| Field        | Type               | Description                                |
| ------------ | ------------------ | ------------------------------------------ |
| `Ticket`     | `int`              | Unique ticket ID for the order.            |
| `Symbol`     | `string`           | Trading symbol (e.g., `"EURUSD"`).         |
| `Lots`       | `double`           | Order volume in lots.                      |
| `OpenPrice`  | `double`           | Price at which the order was opened.       |
| `StopLoss`   | `double`           | Stop-loss price (0 if none).               |
| `TakeProfit` | `double`           | Take-profit price (0 if none).             |
| `Profit`     | `double`           | Current floating profit/loss.              |
| `Swap`       | `double`           | Accrued swap.                              |
| `Commision`  | `double`           | Commission.                                |
| `MagicNumber`| `int`              | Expert/strategy magic number.              |
| `OpenTime`   | `Timestamp`        | Time the order was opened (server).        |
| `OpenTimeUtc`| `Timestamp`        | Time the order was opened (UTC).           |
| `OrderType`  | `OpenedOrderType`  | Type of the order — see enum below.        |

---

## 🧱 Related enums (from proto)

### `EnumOpenedOrderSortType` — parameter `sortType`

* `SortByOpenTimeAsc = 0` — by open time, ascending (default)
* `SortByOpenTimeDesc = 1` — by open time, descending
* `SortByOrderTicketIdAsc = 2` — by ticket id, ascending
* `SortByOrderTicketIdDesc = 3` — by ticket id, descending

### `OpenedOrderType` — field `OrderType`

* `OoOpBuy = 0` — market buy
* `OoOpSell = 1` — market sell
* `OoOpBuylimit = 2` — pending buy limit
* `OoOpSelllimit = 3` — pending sell limit
* `OoOpBuystop = 4` — pending buy stop
* `OoOpSellstop = 5` — pending sell stop
* `OoOpBalance = 6` — balance operation (deposit/withdrawal)
* `OoOpCredit = 7` — credit operation

---

## 🎯 Purpose

Use this method to retrieve and display the list of all **currently open** orders. Helpful for:

* Monitoring active positions
* Building dashboards with live order info
* Analyzing exposure, floating P/L, and symbol distribution

---

## 🧩 Notes & Tips

* **IDs are `int`.** If you hold tickets as `long`, validate the value fits into `Int32` before calling close/modify APIs.
* **UTC times.** `OpenTime` is UTC; convert only for UI.
* **Freshness before actions.** Re-query right before sending `Close/Modify/CloseBy` to reduce race-condition errors.

---

## ⚠️ Pitfalls

* **Race conditions.** Orders can be closed by other processes between list and action; handle `ApiExceptionMT4` on follow-up calls.
* **Empty list ≠ error.** No open orders returns an empty `OrderInfos` list.

---

## 🧪 Testing Suggestions

* **Happy path:** Open a small order and verify it appears with expected `Symbol`, `Lots`, and non-zero `Ticket`.
* **Sorting check:** Call with a different `sortType` (if available in your proto) and confirm order is reordered.
* **Failure path:** Immediately close an order after listing and try to act on its ticket — expect a handled error on the next call.
