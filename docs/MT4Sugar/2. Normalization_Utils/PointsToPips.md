# MT4Sugar · `PointsToPipsAsync`

> Convert points to pips (on 3/5-digit symbols 1 pip = 10 points).

→ extension method on **[`MT4Service`](../../MT4Service/Symbol_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Converts a distance in **points** to **pips**. On 3/5-digit symbols 1 pip = 10 points; on 2/4-digit symbols they're equal. Handy for display, since the API works in points but humans think in pips.

## Signature

```csharp
Task<double> PointsToPipsAsync(string symbol, double points, DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `symbol` | `string` | — | Instrument, e.g. `"EURUSD"`. |
| `points` | `double` | — | A distance in points to convert. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`double` — the same distance expressed in pips.

## Example

```csharp
double pips = await svc.PointsToPipsAsync("EURUSD", 150); // -> 15
```

## Scenario

Work in points internally, show the trader pips.

```csharp
double stopPoints = 200;
double pips = await svc.PointsToPipsAsync("EURUSD", stopPoints);
Console.WriteLine($"stop = {stopPoints} points ({pips} pips)");
```

## Notes

* **Display only.** All order/price math stays in points.
* `Point` itself comes from [`GetSymbolSnapshotAsync`](../1.%20Snapshots/GetSymbolSnapshot.md).

## See also

* [`PriceFromOffsetPointsAsync`](PriceFromOffsetPoints.md) — turns points into an absolute price.
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Symbol](../../MT4Service/Symbol_Methods.md)
