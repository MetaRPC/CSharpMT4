# MT4Sugar · `GetLastClosedOrderAsync`

> The most recent closed order within `window`, or `null`.

→ extension method on **[`MT4Service`](../../MT4Service/Orders_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

The single most recent closed order within `window`, or `null`. Handy for a cooldown — don't re-enter right after the previous trade closed.

## Signature

```csharp
Task<HistoryOrderInfo?> GetLastClosedOrderAsync(TimeSpan window, DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `window` | `TimeSpan` | — | Look-back period ending now. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`HistoryOrderInfo?` — the order, or `null` if nothing closed in the window. Exposes `.Ticket`.

## Example

```csharp
var last = await svc.GetLastClosedOrderAsync(TimeSpan.FromDays(7));
Console.WriteLine(last?.Ticket);
```

## Scenario

Cooldown: don't re-enter right after the previous trade closed.

```csharp
var last = await svc.GetLastClosedOrderAsync(TimeSpan.FromMinutes(5));
if (last != null) { Console.WriteLine($"traded recently (#{last.Ticket}) — wait"); return; }
```

## Notes

* For the full list of recent closes, use [`OrdersHistoryLastAsync`](OrdersHistoryLast.md).

## See also

* [`OrdersHistoryLastAsync`](OrdersHistoryLast.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Positions & Orders](../../MT4Service/Orders_Methods.md)
