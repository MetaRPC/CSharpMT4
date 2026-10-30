# Getting Opened Order Tickets

> **Request:** fetch only the tickets (IDs) of currently opened orders
> Retrieve a lightweight ticket list without full order details.

**API Information:**

* **SDK wrapper:** `MT4Account.OpenedOrdersTicketsAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.AccountHelper`
* **Proto definition:** `OpenedOrdersTickets` (in the AccountHelper service)

### RPC

* **Service:** `mt4_term_api.AccountHelper`
* **Method:** `OpenedOrdersTickets(OpenedOrdersTicketsRequest) → OpenedOrdersTicketsReply`
* **Low‑level client (generated):** `AccountHelper.AccountHelperClient.OpenedOrdersTickets(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async Task<OpenedOrdersTicketsData> OpenedOrdersTicketsAsync(
            DateTime? deadline = null,
            CancellationToken cancellationToken = default);
    }
}
```

**Request message:**

`OpenedOrdersTicketsRequest {}`

**Reply message:**

`OpenedOrdersTicketsReply { data: OpenedOrdersTicketsData }`

---

### Code Example

```csharp
// --- Service wrapper (typed, data-returning) ---
var tickets = await _service.OpenedOrdersTicketsAsync();
Console.WriteLine($"{tickets.Tickets.Count}: [{string.Join(", ", tickets.Tickets)}]");

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

var result = await _mt4.OpenedOrdersTicketsAsync(
    deadline: null,
    cancellationToken: default);

foreach (var ticket in result.Tickets)
{
    Console.WriteLine($"Open Order Ticket: {ticket}");
}
```

---

## 🔽 Input

No required parameters.

Optional:

* **`deadline`** (`DateTime?`) — optional UTC deadline for the call.
* **`cancellationToken`** (`CancellationToken`) — optional cancellation token.

---

## ⬆️ Output

Returns **`OpenedOrdersTicketsData`** with:

| Field     | Type        | Description                                   |
| --------- | ----------- | --------------------------------------------- |
| `Tickets` | `RepeatedField<int>` | List of ticket IDs for currently open orders. |

Each ticket is an `int` that uniquely identifies an open trade in MT4.

---

## 🎯 Purpose

Use this when you only need IDs, e.g. for quick matching/sync, or to feed targeted operations like **Close/Delete** or **Close By** without fetching full order rows. Minimal bandwidth versus `OpenedOrdersAsync()`.

---

## 🧩 Notes & Tips

* **Int32 boundary.** MT4 tickets are `int`. If your app stores them as `long`, ensure values fit into `Int32` before sending to close/modify APIs.
* **Empty ≠ error.** An empty list simply means there are no open orders right now.
* **Freshness.** In volatile markets, fetch tickets right before acting; another process may open/close orders in between.

---

## ⚠️ Pitfalls

* **Race conditions.** A ticket can disappear between listing and action; handle `ApiExceptionMT4` from subsequent calls (e.g., *Ticket not found*).

---

## 🧪 Testing Suggestions

* **Happy path.** Open a small market order; verify its ticket appears in the list.
* **No orders.** Close all trades and confirm the method returns an empty `Tickets` list without errors.
