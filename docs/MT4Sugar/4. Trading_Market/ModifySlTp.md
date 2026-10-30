# MT4Sugar · `ModifySlTpAsync`

> Modify an order's SL/TP (absolute prices; `null` leaves a field unchanged).

→ extension method on **[`MT4Service`](../../MT4Service/Trading_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Changes an order's SL/TP (absolute prices). Pass only what you want to change — `null` leaves a field untouched. This is how break-even and trailing logic move stops.

## Signature

```csharp
Task<OrderModifyData> ModifySlTpAsync(int ticket, double? sl = null, double? tp = null, DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `ticket` | `int` | — | Order ticket. |
| `sl` | `double?` | `null` | Absolute stop-loss price, or `null` for none. |
| `tp` | `double?` | `null` | Absolute take-profit price, or `null` for none. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`OrderModifyData` — the broker reply for the modify.

## Example

```csharp
await svc.ModifySlTpAsync(ticket, sl: 1.0950, tp: 1.1050);
```

## Scenario

Move the stop to break-even once the trade is in profit.

```csharp
var o = await svc.GetOrderByTicketAsync(ticket);
if (o != null && o.Profit > 0)
    await svc.ModifySlTpAsync(ticket, sl: o.OpenPrice);   // lock in: SL = entry
```

## Notes

* **Partial updates.** `ModifySlTpAsync(t, sl: x)` moves only the stop.
* Normalize computed prices first with [`NormalizePriceAsync`](../2.%20Normalization_Utils/NormalizePrice.md).

## See also

* [`CloseByTicketAsync`](CloseByTicket.md) · [`GetOrderByTicketAsync`](../3.%20Read_Helpers/GetOrderByTicket.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Trading](../../MT4Service/Trading_Methods.md)
