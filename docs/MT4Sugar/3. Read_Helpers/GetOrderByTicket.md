# MT4Sugar · `GetOrderByTicketAsync`

> Find an open order by ticket, or `null`.

→ extension method on **[`MT4Service`](../../MT4Service/Orders_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Looks up a single open order by its ticket, or `null` if it isn't open (closed, or wrong ticket). Use it to confirm an order still exists before acting on it.

## Signature

```csharp
Task<OpenedOrderInfo?> GetOrderByTicketAsync(long ticket, DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `ticket` | `long` | — | Order ticket. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`OpenedOrderInfo?` — the order, or `null`. Each item exposes `Ticket`, `OrderType`, `Lots`, `OpenPrice`, `Profit`.

## Example

```csharp
var o = await svc.GetOrderByTicketAsync(462208455);
if (o is not null) Console.WriteLine($"{o.Symbol} P/L={o.Profit}");
```

## Scenario

Confirm an order is still open before acting on it.

```csharp
var o = await svc.GetOrderByTicketAsync(ticket);
if (o is null) { Console.WriteLine("already closed"); return; }
await svc.ModifySlTpAsync(ticket, tp: o.OpenPrice + 300 * point);
```

## Notes

* **`null` is normal** when the order has already closed — handle it before modify/close.

## See also

* [`GetOrdersBySymbolAsync`](GetOrdersBySymbol.md) · [`ModifySlTpAsync`](../4.%20Trading_Market/ModifySlTp.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Positions & Orders](../../MT4Service/Orders_Methods.md)
