# MT4Sugar · `TotalLotsAsync`

> Total open volume in lots (optionally one symbol).

→ extension method on **[`MT4Service`](../../MT4Service/Orders_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Total open volume in lots (optionally one symbol) — a quick exposure gauge for position limits.

## Signature

```csharp
Task<double> TotalLotsAsync(string? symbol = null, DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `symbol` | `string?` | `null` | Restrict to one symbol, or `null` for the whole account. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`double` — summed open volume in lots.

## Example

```csharp
double lots = await svc.TotalLotsAsync("EURUSD");
```

## Scenario

Exposure cap: stop opening once total volume on a symbol is reached.

```csharp
if (await svc.TotalLotsAsync("EURUSD") >= 1.0) { Console.WriteLine("EURUSD exposure full"); return; }
await svc.BuyMarketAsync("EURUSD", 0.10);
```

## Notes

* Sums all open orders' volume; combine with [`GetOrderCountAsync`](GetOrderCount.md) for a fuller picture.

## See also

* [`GetOrderCountAsync`](GetOrderCount.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Positions & Orders](../../MT4Service/Orders_Methods.md)
