# Closing an Order

> **Request:** close or delete an open order by its ticket
> Sends a request to the server to close or delete the specified order.

---

### Code Example

```csharp
// Using service wrapper
await _service.CloseOrderExample(123456);

// Or directly from MT4Account
var request = new OrderCloseDeleteRequest
{
    OrderTicket = 123456 // must be a valid ticket ID
};

var result = await _mt4.OrderCloseDeleteAsync(request);
Console.WriteLine($"Closed/Deleted: {result.Mode}, Comment: {result.HistoryOrderComment}");
```

---

###  Method Signature

```csharp
Task<OrderCloseDeleteData> OrderCloseDeleteAsync(
    OrderCloseDeleteRequest request,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default
)
```

---

## 🔽 Input

* **`request`** (`OrderCloseDeleteRequest`) — request structure with fields:

  * **`OrderTicket`** (`int`) — required. The ticket number of the order to be closed or deleted.

Optional:

* **`deadline`** (`DateTime?`) — optional deadline.
* **`cancellationToken`** (`CancellationToken`) — for cancellation control.

Ticket must be a valid open order ID — otherwise the server will return an error like `Invalid ticket`, `Ticket not found`, etc.

---

## ⬆️ Output

Returns an **`OrderCloseDeleteData`** object with:

| Field                 | Type     | Description                                       |
| --------------------- | -------- | ------------------------------------------------- |
| `Mode`                | `string` | Operation mode result (e.g. "Closed", "Deleted"). |
| `HistoryOrderComment` | `string` | Server comment describing the result.             |

---

## 🎯 Purpose

This method allows closing or deleting an order manually by ticket — useful for:

* Manual trade intervention from UI/debug tool
* Post-trade cleanup
* Simulating close workflows in sandbox/testing

---

### ❓ Why it's commented out in code:

This method requires a **valid, active ticket number**. Since demo or test environments may not have such a ticket readily available, it’s commented out by default to:

* ❌ Avoid runtime exceptions from invalid ticket errors
* ✅ Encourage intentional use only when real ticket IDs are known

To test it, provide a known valid open order ticket — or use one received from `OpenedOrdersAsync()`.
