# MT4Sugar · `GetOrderCountAsync`

> Number of open orders (optionally for one symbol).

→ extension method on **[`MT4Service`](../../MT4Service/Orders_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

How many orders are currently open — account-wide, or for one symbol when you pass it. Useful to cap concurrency.

## Signature

```csharp
Task<int> GetOrderCountAsync(string? symbol = null, DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `symbol` | `string?` | `null` | Restrict to one symbol, or `null` for the whole account. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`int` — the number of open orders.

## Example

```csharp
int n = await svc.GetOrderCountAsync();           // all
int e = await svc.GetOrderCountAsync("EURUSD");   // one symbol
```

## Scenario

Cap concurrency: don't open more than N orders at once.

```csharp
if (await svc.GetOrderCountAsync("EURUSD") >= 3)
{
    Console.WriteLine("already 3 EURUSD orders — skip");
    return;
}
await svc.BuyMarketByRiskAsync("EURUSD", stopPoints: 200, riskMoney: 50);
```

## Notes

* Counts open orders (market + pending). For lots, use [`TotalLotsAsync`](TotalLots.md).

## See also

* [`TotalLotsAsync`](TotalLots.md) · [`GetOrdersBySymbolAsync`](GetOrdersBySymbol.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Positions & Orders](../../MT4Service/Orders_Methods.md)
