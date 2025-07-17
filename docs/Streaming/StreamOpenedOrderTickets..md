# Streaming Opened Order Tickets

> **Request:** subscribe to stream of currently open order ticket numbers
> Returns only the **ticket IDs** of all open orders as they change in real time.

---

### Code Example

```csharp
// Using service wrapper
await _service.StreamOpenedOrderTickets();

// Or directly from MT4Account
await foreach (var update in _mt4.StreamOpenedOrderTicketsAsync(1000))
{
    Console.WriteLine($"Open tickets: {string.Join(", ", update.Tickets)}");
    break; // for demo purposes
}
```

---

### Method Signature

```csharp
IAsyncEnumerable<OpenedOrderTicketsData> StreamOpenedOrderTicketsAsync(
    int intervalMs,
    [EnumeratorCancellation] CancellationToken cancellationToken = default
)
```

---

## 🔽 Input

* **`intervalMs`** (`int`) — required. Interval in milliseconds between each update.
* **`cancellationToken`** (`CancellationToken`, optional) — token to cancel the stream.

---

## ⬆️ Output

Returns a stream (`IAsyncEnumerable<OpenedOrderTicketsData>`) where each item contains:

### Structure: `OpenedOrderTicketsData`

| Field     | Type        | Description                                 |
| --------- | ----------- | ------------------------------------------- |
| `Tickets` | `List<int>` | List of currently open order ticket numbers |

Each ticket corresponds to an active trade in the terminal.

---

## 🎯 Purpose

Use this method to **track open order IDs in real time**, useful for:

* Updating UI lists of active tickets
* Detecting when new trades are opened or old ones closed
* Triggering updates to related order details based on ticket change events

This is a **lightweight alternative** to streaming full order data, optimized for performance and minimal network load.
