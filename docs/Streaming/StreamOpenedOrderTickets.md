# Streaming Opened Order Tickets

> **Request:** subscribe to stream of currently open order ticket numbers
> Returns only the **ticket IDs** of all open orders as they change in real time.

---

### Code Example

```csharp
// --- Quick use (service wrapper) ---
await _service.StreamOpenedOrderTickets();

// --- Low-level (direct account call) ---
// Periodic snapshots of the current ticket set; cancel when done.
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await foreach (var update in _mt4.OnOpenedOrdersTicketsAsync(
    intervalMs: 1000,
    cancellationToken: cts.Token))
{
    Console.WriteLine($"Open tickets: {string.Join(", ", update.Tickets)}");
    break; // demo: take first update only
}
```

---

### Method Signature

```csharp
IAsyncEnumerable<OpenedOrderTicketsData> OnOpenedOrdersTicketsAsync(
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

---

## 🧩 Notes & Tips

* **Snapshot-style updates.** The stream returns the *current* ticket set at the chosen interval; compute diffs on the client if you need add/remove events.
* **Cancellation = clean finish.** When you cancel the token, the stream completes gracefully (no exception needed in user code).
* **Reconnects on transport hiccups.** Transient gRPC errors are retried internally with backoff up to a limit; short gaps are expected during restarts.
* **Pick a sensible interval.** 500–2000 ms usually balances freshness and load for most UIs.

---

## ⚠️ Pitfalls

* **Races are normal.** Orders can open/close between intervals; don’t assume you’ll see every intermediate state.
* **Large ticket sets.** Many open orders can cause heavy UI updates each tick; debounce or diff before rendering.

---

## 🧪 Testing Suggestions

* **Happy path:** With at least one open order, first snapshot contains that ticket.
* **Change detection:** Open/close an order while streaming and verify your diffing logic spots adds/removes.
* **Timeout path:** Use a short cancellation timeout to ensure your loop exits cleanly without unhandled exceptions.
