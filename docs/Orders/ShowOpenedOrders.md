# Getting Opened Orders

> **Request:** retrieve currently opened orders from MT4
> Fetch all active (non‑closed) trade positions on the account.

---

### Code Example

```csharp
// --- Quick use (service wrapper) ---
await _service.ShowOpenedOrders();

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

### Method Signatures

```csharp
// Service wrapper (example implementation)
Task ShowOpenedOrders();
```

```csharp
// Low-level account call
Task<OpenedOrdersData> OpenedOrdersAsync(
    EnumOpenedOrderSortType sortType = EnumOpenedOrderSortType.SortByOpenTimeAsc,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default
);
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
| `OrderInfos` | `List<OrderInfo>` | List of all currently opened orders. |

Each **`OrderInfo`** includes:

| Field       | Type                 | Description                                |
| ----------- | -------------------- | ------------------------------------------ |
| `Ticket`    | `int`                | Unique ticket ID for the order.            |
| `Symbol`    | `string`             | Trading symbol (e.g., `"EURUSD"`).         |
| `Lots`      | `double`             | Order volume in lots.                      |
| `OpenPrice` | `double`             | Price at which the order was opened.       |
| `Profit`    | `double`             | Current floating profit/loss.              |
| `OpenTime`  | `string`             | Timestamp when the order was opened (UTC). |
| `OrderType` | `ENUM_ORDER_TYPE_TF` | Type of the order (Buy, Sell, etc.).       |

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
