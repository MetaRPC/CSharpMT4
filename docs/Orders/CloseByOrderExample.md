# Closing an Order By Opposite

> **Request:** close one open order using another opposite-position order
> Sends a request to match and close two opposite-direction orders by ticket.

---

### Code Example

```csharp
// Using service wrapper
await _service.CloseByOrderExample(123456, 654321);

// Or directly from MT4Account
var request = new OrderCloseByRequest
{
    TicketToClose = 123456,
    OppositeTicketClosingBy = 654321
};

var result = await _mt4.OrderCloseByAsync(request);
Console.WriteLine($"Closed by opposite: Profit={result.Profit}, Price={result.ClosePrice}, Time={result.CloseTime}");
```

---

### Method Signature

```csharp
Task<OrderCloseByData> OrderCloseByAsync(
    OrderCloseByRequest request,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default
)
```

---

## 🔽 Input

* **`request`** (`OrderCloseByRequest`) — request structure with:

  * **`TicketToClose`** (`int`) — the primary ticket ID you want to close.
  * **`OppositeTicketClosingBy`** (`int`) — the opposite-direction order used to close the first.

Optional:

* **`deadline`** (`DateTime?`) — optional deadline.
* **`cancellationToken`** (`CancellationToken`) — for cancellation control.

⚠️ Both tickets must be valid and open at the same time, and must be in **opposite trade directions** (Buy vs Sell).

---

## ⬆️ Output

Returns an **`OrderCloseByData`** object with:

| Field        | Type     | Description                             |
| ------------ | -------- | --------------------------------------- |
| `Profit`     | `double` | Profit/loss from the close-by operation |
| `ClosePrice` | `double` | Price at which orders were closed       |
| `CloseTime`  | `string` | Timestamp of the operation              |

---

## 🎯 Purpose

Used for closing matched hedge positions — one Buy and one Sell of equal volume — using the `Close By` mechanism. Useful when minimizing commission/slippage on opposite trades.

---

### ❓ Why it's commented out in code:

This method requires **two valid and open orders** with opposite directions. Since most test/demo environments don’t have such conditions by default, the method is commented out to:

* ❌ Avoid runtime errors like `Invalid ticket`, `Ticket not found`, `Tickets must be opposite`, etc.
* ✅ Ensure it's only used intentionally when such order pairs are known to exist

To test, retrieve live open orders and select a valid Buy/Sell pair from `OpenedOrdersAsync()`.
