# MT4Sugar · `GetOrdersBySymbolAsync`

> All open orders for a symbol.

→ extension method on **[`MT4Service`](../../MT4Service/Orders_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

All open orders on one symbol, as a list you can iterate — e.g. to inspect before flattening it.

## Signature

```csharp
Task<List<OpenedOrderInfo>> GetOrdersBySymbolAsync(string symbol, DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `symbol` | `string` | — | Instrument, e.g. `"EURUSD"`. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`List<OpenedOrderInfo>` — open orders on the symbol. Each item exposes `Ticket`, `OrderType`, `Lots`, `OpenPrice`, `Profit`.

## Example

```csharp
foreach (var o in await svc.GetOrdersBySymbolAsync("EURUSD"))
    Console.WriteLine($"#{o.Ticket} {o.OrderType} {o.Lots}");
```

## Scenario

List the open book on one symbol — e.g. before deciding to flatten it.

```csharp
foreach (var o in await svc.GetOrdersBySymbolAsync("EURUSD"))
    Console.WriteLine($"#{o.Ticket} {o.OrderType} {o.Lots}@{o.OpenPrice} P/L={o.Profit}");
```

## Notes

* Returns an empty list when nothing is open on the symbol.
* Filtered variants: [`GetProfitableOrdersAsync`](GetProfitableOrders.md) / [`GetLosingOrdersAsync`](GetLosingOrders.md).

## See also

* [`GetOrderByTicketAsync`](GetOrderByTicket.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Positions & Orders](../../MT4Service/Orders_Methods.md)
