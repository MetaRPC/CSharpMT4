# MT4Sugar · `CloseByTicketAsync`

> Close a market order / delete a pending order by ticket.

→ extension method on **[`MT4Service`](../../MT4Service/Trading_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Closes a market order, or deletes a pending order, by ticket.

## Signature

```csharp
Task<OrderCloseDeleteData> CloseByTicketAsync(int ticket, DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `ticket` | `int` | — | Order ticket. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`OrderCloseDeleteData` — the broker reply for the close/delete.

## Example

```csharp
await svc.CloseByTicketAsync(ticket);
```

## Scenario

Close the first open order on a symbol by its ticket.

```csharp
var orders = await svc.GetOrdersBySymbolAsync("EURUSD");
if (orders.Count > 0) await svc.CloseByTicketAsync((int)orders[0].Ticket);
```

## Notes

* Works for both market positions and pending orders.
* To close many at once use the bulk helpers ([`CloseAllAsync`](../5.%20Bulk_Convenience/CloseAll.md) etc.).

## See also

* [`CloseAllAsync`](../5.%20Bulk_Convenience/CloseAll.md) · [`GetOrderByTicketAsync`](../3.%20Read_Helpers/GetOrderByTicket.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Trading](../../MT4Service/Trading_Methods.md)
