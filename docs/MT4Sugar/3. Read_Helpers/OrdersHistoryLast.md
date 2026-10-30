# MT4Sugar · `OrdersHistoryLastAsync`

> Order history for the last `window` up to now.

→ extension method on **[`MT4Service`](../../MT4Service/Orders_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Closed-order history for the last `window` up to now — a shortcut over the dated `OrdersHistory` call when you just want "recent".

## Signature

```csharp
Task<OrdersHistoryData> OrdersHistoryLastAsync(TimeSpan window, DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `window` | `TimeSpan` | — | Look-back period ending now. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`OrdersHistoryData` — `.OrdersInfo` is the list of closed orders in the window.

## Example

```csharp
var h = await svc.OrdersHistoryLastAsync(TimeSpan.FromDays(7));
Console.WriteLine($"{h.OrdersInfo.Count} closed");
```

## Scenario

End-of-day check: how much closed in the last 24 hours.

```csharp
var h = await svc.OrdersHistoryLastAsync(TimeSpan.FromDays(1));
Console.WriteLine($"{h.OrdersInfo.Count} order(s) closed in the last day");
```

## Notes

* **Window ends now.** Pass e.g. `TimeSpan.FromDays(7)` for the last week.
* For just the single most recent close, use [`GetLastClosedOrderAsync`](GetLastClosedOrder.md).

## See also

* [`GetLastClosedOrderAsync`](GetLastClosedOrder.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Positions & Orders](../../MT4Service/Orders_Methods.md)
