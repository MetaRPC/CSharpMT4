# MT4Sugar · `GetProfitableOrdersAsync`

> Open orders currently in profit (optionally one symbol).

→ extension method on **[`MT4Service`](../../MT4Service/Orders_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

The open orders currently **in profit** (optionally one symbol) — e.g. to take partial profit by closing only the winners.

## Signature

```csharp
Task<List<OpenedOrderInfo>> GetProfitableOrdersAsync(string? symbol = null, DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `symbol` | `string?` | `null` | Restrict to one symbol, or `null` for the whole account. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`List<OpenedOrderInfo>` — orders with `Profit > 0`. Each item exposes `Ticket`, `OrderType`, `Lots`, `OpenPrice`, `Profit`.

## Example

```csharp
var winners = await svc.GetProfitableOrdersAsync();
```

## Scenario

Take partial profit: close only the orders currently in the green.

```csharp
foreach (var o in await svc.GetProfitableOrdersAsync("EURUSD"))
    await svc.CloseByTicketAsync((int)o.Ticket);
```

## Notes

* Mirror of [`GetLosingOrdersAsync`](GetLosingOrders.md).
* Close selected tickets with [`CloseByTicketAsync`](../4.%20Trading_Market/CloseByTicket.md).

## See also

* [`GetLosingOrdersAsync`](GetLosingOrders.md) · [`GetTotalProfitLossAsync`](GetTotalProfitLoss.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Positions & Orders](../../MT4Service/Orders_Methods.md)
