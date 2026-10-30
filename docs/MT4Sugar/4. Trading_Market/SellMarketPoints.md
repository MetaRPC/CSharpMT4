# MT4Sugar · `SellMarketPointsAsync`

> Market SELL with SL/TP expressed in **points** from Bid.

→ extension method on **[`MT4Service`](../../MT4Service/Trading_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Market **SELL** with SL/TP as **point distances** from the fill. Mirror of [`BuyMarketPointsAsync`](BuyMarketPoints.md).

## Signature

```csharp
Task<OrderSendData> SellMarketPointsAsync(string symbol, double volume, double? slPoints = null, double? tpPoints = null, int slippage = 5, int magic = 0, string comment = "MT4Sugar", ...)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `symbol` | `string` | — | Instrument, e.g. `"EURUSD"`. |
| `volume` | `double` | — | Order volume in lots (auto-normalized to the symbol's step). |
| `slPoints` | `double?` | `null` | Stop-loss distance in points from the fill, or `null`. |
| `tpPoints` | `double?` | `null` | Take-profit distance in points from the fill, or `null`. |
| `slippage` | `int` | `5` | Max allowed price slippage, in points. |
| `magic` | `int` | `0` | Magic-number tag for the order. |
| `comment` | `string` | `"MT4Sugar"` | Order comment. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`OrderSendData` — the broker reply. **`Ticket > 0` means success**; it also carries `Volume` and `Price`.

## Example

```csharp
var r = await svc.SellMarketPointsAsync("EURUSD", 0.10, slPoints: 300, tpPoints: 600);
```

## Scenario

SELL with SL/TP as point distances from the fill.

```csharp
var r = await svc.SellMarketPointsAsync("EURUSD", 0.10, slPoints: 150, tpPoints: 300);
Console.WriteLine($"opened #{r.Ticket} @ {r.Price}");
```

## Notes

* ⚠️ Places a **real order** — guard it.

## See also

* [`BuyMarketPointsAsync`](BuyMarketPoints.md) · [`SellMarketAsync`](SellMarket.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Trading](../../MT4Service/Trading_Methods.md)
