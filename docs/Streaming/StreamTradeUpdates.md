# Streaming Trade Updates

> **Request:** subscribe to real-time trade update stream
> Starts a server-side stream to receive trade activity as it happens.

---

### Code Example

```csharp
// Using service wrapper
await _service.StreamTradeUpdates();

// Or directly from MT4Account
await foreach (var trade in _mt4.OnTradeAsync())
{
    Console.WriteLine("Trade update received.");
    break; // for test/demo purposes
}
```

---

### Method Signature

```csharp
IAsyncEnumerable<OnTradeData> OnTradeAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default
)
```

---

## 🔽 Input

No parameters required, except:

* **`cancellationToken`** (`CancellationToken`, optional) — used to cancel the stream manually.

---

## ⬆️ Output

Returns a stream (`IAsyncEnumerable<OnTradeData>`) where each item represents a trade event:

| Field       | Type        | Description                         |
| ----------- | ----------- | ----------------------------------- |
| `TradeInfo` | `TradeInfo` | Structure containing trade details. |

> ⚠️ Structure of `TradeInfo` may vary depending on implementation — consult your protobuf/gRPC definition for exact fields.

---

## 🎯 Purpose

Use this method to subscribe to **real-time trade activity** — executed orders, closed positions, and trade events sent by the server. Suitable for:

* Updating dashboards or client UIs in real-time
* Triggering post-trade logic (e.g. logging, risk checks)
* Auditing trade flow in automated systems

---

### ❓ Notes

This stream is **endless by nature** — unless filtered or cancelled manually. The wrapper method in `_service` uses a `break;` for demo purposes, but production code should handle stream lifecycle properly with cancellation tokens or filtering logic.
