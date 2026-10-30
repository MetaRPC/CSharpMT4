# MT4Sugar · `GetLosingOrdersAsync`

> Open orders currently at a loss (optionally one symbol).

→ extension method on **[`MT4Service`](../../MT4Service/Orders_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

The open orders currently **at a loss** (optionally one symbol) — e.g. to cut losers while letting winners run.

## Signature

```csharp
Task<List<OpenedOrderInfo>> GetLosingOrdersAsync(string? symbol = null, DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `symbol` | `string?` | `null` | Restrict to one symbol, or `null` for the whole account. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`List<OpenedOrderInfo>` — orders with `Profit < 0`. Each item exposes `Ticket`, `OrderType`, `Lots`, `OpenPrice`, `Profit`.

## Example

```csharp
var losers = await svc.GetLosingOrdersAsync();
```

## Scenario

Cut losers: close only the orders currently at a loss, keep the winners running.

```csharp
foreach (var o in await svc.GetLosingOrdersAsync("EURUSD"))
    await svc.CloseByTicketAsync((int)o.Ticket);
```

## Notes

* Mirror of [`GetProfitableOrdersAsync`](GetProfitableOrders.md).
* Close selected tickets with [`CloseByTicketAsync`](../4.%20Trading_Market/CloseByTicket.md).

## See also

* [`GetProfitableOrdersAsync`](GetProfitableOrders.md) · [`GetTotalProfitLossAsync`](GetTotalProfitLoss.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Positions & Orders](../../MT4Service/Orders_Methods.md)
