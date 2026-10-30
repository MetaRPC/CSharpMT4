# MT4Sugar · `PriceFromOffsetPointsAsync`

> A normalized price at `offsetPoints` from Ask (`fromAsk:true`) or Bid; offset may be negative.

→ extension method on **[`MT4Service`](../../MT4Service/Symbol_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Returns a **normalized** absolute price a given number of points away from the current Ask (`fromAsk: true`) or Bid. Negative offset goes below the market. The building block for SL/TP levels and pending-order prices.

## Signature

```csharp
Task<double> PriceFromOffsetPointsAsync(string symbol, bool fromAsk, double offsetPoints, DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `symbol` | `string` | — | Instrument, e.g. `"EURUSD"`. |
| `fromAsk` | `bool` | — | `true` = measure from Ask, `false` = from Bid. |
| `offsetPoints` | `double` | — | Signed distance in points; negative goes below. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`double` — an absolute, already-normalized price.

## Example

```csharp
double sl = await svc.PriceFromOffsetPointsAsync("EURUSD", fromAsk: true, offsetPoints: -200);
```

## Scenario

Build absolute SL/TP around the market, then open the order with them.

```csharp
double sl = await svc.PriceFromOffsetPointsAsync("EURUSD", fromAsk: true, offsetPoints: -150);
double tp = await svc.PriceFromOffsetPointsAsync("EURUSD", fromAsk: true, offsetPoints:  300);
await svc.BuyMarketAsync("EURUSD", 0.10, sl: sl, tp: tp);
```

## Notes

* **Already normalized** — no extra `NormalizePrice` needed.
* Buy stops/targets usually measure from Ask; sell stops/targets from Bid.

## See also

* [`NormalizePriceAsync`](NormalizePrice.md) · [`PointsToPipsAsync`](PointsToPips.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Symbol](../../MT4Service/Symbol_Methods.md)
