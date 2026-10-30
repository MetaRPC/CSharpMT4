# Closing an Order

> **Request:** close or delete an open order by its ticket
> Sends a request to the server to close or delete the specified order.

**API Information:**

* **SDK wrapper:** `MT4Account.OrderCloseDeleteAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.TradingHelper`
* **Proto definition:** `OrderCloseDelete` (in the TradingHelper service)

### RPC

* **Service:** `mt4_term_api.TradingHelper`
* **Method:** `OrderCloseDelete(OrderCloseDeleteRequest) → OrderCloseDeleteReply`
* **Low‑level client (generated):** `TradingHelper.TradingHelperClient.OrderCloseDelete(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async Task<OrderCloseDeleteData> OrderCloseDeleteAsync(
            OrderCloseDeleteRequest request,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default);
    }
}
```

**Request message:**

`OrderCloseDeleteRequest { OrderTicket: int }`

**Reply message:**

`OrderCloseDeleteReply { data: OrderCloseDeleteData }`

---

### Code Example

```csharp
// --- Service wrapper (typed, data-returning) ---
// Build the request (see below) and call: await _service.OrderCloseDeleteAsync(request);
// Or via sugar (by ticket): await _service.CloseByTicketAsync(123456);

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

var request = new OrderCloseDeleteRequest
{
    OrderTicket = 123456 // must be a valid ticket ID
};

var result = await _mt4.OrderCloseDeleteAsync(
    request: request,
    deadline: null,
    cancellationToken: default);

Console.WriteLine($"Closed/Deleted: {result.Mode}, Comment: {result.HistoryOrderComment}");
```

---

## 🔽 Input

* **`request`** (`OrderCloseDeleteRequest`) — fields:

  | Field         | Type  | Description                                               |
  | ------------- | ----- | --------------------------------------------------------- |
  | `OrderTicket` | `int` | **Required.** Ticket number of the order to close/delete. |

Optional:

* **`deadline`** (`DateTime?`) — optional per-call deadline.
* **`cancellationToken`** (`CancellationToken`) — to cancel the request.

> The ticket must correspond to a **currently open** order; otherwise the server returns errors like *Invalid ticket* or *Ticket not found*.

---

## ⬆️ Output

Returns **`OrderCloseDeleteData`** with:

| Field                 | Type                   | Description                                      |
| --------------------- | ---------------------- | ------------------------------------------------ |
| `Mode`                | `OrderCloseDeleteMode` | Whether a market order was closed or a pending deleted — see enum below. |
| `HistoryOrderComment` | `string`               | Server comment explaining the result.            |

### `OrderCloseDeleteMode` — field `Mode`

* `OcdMarketOrder = 0` — a market order was **closed**
* `OcdPendingOrder = 1` — a pending order was **deleted**

> The actual action (close **vs** delete) is determined by the server based on the order state/type.

---

## 🎯 Purpose

Close or delete an order by its ticket — convenient for:

* Manual trade intervention from UI/debug tools
* Post-trade cleanup
* Simulating close workflows in sandbox/testing

---

## 🧩 Notes & Tips

* **Ticket range.** The wire type is `int`. If you store tickets as `long`, ensure the value fits into `Int32` before sending (the wrapper does this check).
* **State matters.** Market positions are typically **closed**; pending orders are typically **deleted** — the server sets `Mode` accordingly.

---

## ⚠️ Pitfalls

* **Invalid/closed ticket.** Calling the method for a non-open ticket yields an API error — handle `ApiExceptionMT4`.
* **Race conditions.** Another process might close the ticket between fetching and sending your request; retry logic should be at the caller level if needed.

---

## 🧪 Testing Suggestions

* **Happy path.** Use a fresh open ticket from `OpenedOrdersAsync()`; verify `Mode` and that the ticket disappears from the open-orders list.
* **Failure path.** Try an obviously invalid ticket and assert that an `ApiExceptionMT4` is thrown and logged without crashing.
