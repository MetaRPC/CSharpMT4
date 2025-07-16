# Getting Opened Order Tickets

> **Request:** fetch only the tickets (IDs) of currently opened orders
> Retrieve lightweight ticket list without full order details.

---

### Code Example

```csharp
// Using service wrapper
await _service.ShowOpenedOrderTickets();

// Or directly from MT4Account
var result = await _mt4.OpenedOrdersTicketsAsync();
foreach (var ticket in result.Tickets)
{
    Console.WriteLine($"Open Order Ticket: {ticket}");
}
```

---

###  Method Signature

```csharp
Task<OpenedOrderTicketsData> OpenedOrdersTicketsAsync(
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

Returns an **`OpenedOrderTicketsData`** object with:

| Field     | Type        | Description                           |
| --------- | ----------- | ------------------------------------- |
| `Tickets` | `List<int>` | List of ticket IDs for opened orders. |

Each ticket is an integer representing the unique identifier of an open trade.

---

## 🎯 Purpose

Use this method when you only need the ticket IDs of opened orders — e.g., for fast matching, syncing, or preparing targeted order operations without fetching full order info.

It's a low-latency, minimal-overhead alternative to `OpenedOrdersAsync()`.
