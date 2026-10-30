# MT4Sugar · `SellMarketAsync`

> Market SELL (price = Bid). SL/TP are **absolute** prices.

→ extension method on **[`MT4Service`](../../MT4Service/Trading_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Opens a market **SELL** at the current Bid. Volume is auto-normalized; SL/TP are **absolute** prices (SL above, TP below). Mirror of [`BuyMarketAsync`](BuyMarket.md).

## Signature

```csharp
Task<OrderSendData> SellMarketAsync(string symbol, double volume, double? sl = null, double? tp = null, int slippage = 5, int magic = 0, string comment = "MT4Sugar", ...)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `symbol` | `string` | — | Instrument, e.g. `"EURUSD"`. |
| `volume` | `double` | — | Order volume in lots (auto-normalized to the symbol's step). |
| `sl` | `double?` | `null` | Absolute stop-loss price, or `null` for none. |
| `tp` | `double?` | `null` | Absolute take-profit price, or `null` for none. |
| `slippage` | `int` | `5` | Max allowed price slippage, in points. |
| `magic` | `int` | `0` | Magic-number tag for the order. |
| `comment` | `string` | `"MT4Sugar"` | Order comment. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`OrderSendData` — the broker reply. **`Ticket > 0` means success**; it also carries `Volume` and `Price`.

## Example

```csharp
var r = await svc.SellMarketAsync("EURUSD", 0.10);
```

## Scenario

Mirror of BuyMarket: SELL fills at Bid; SL above, TP below.

```csharp
var s = await svc.GetSymbolSnapshotAsync("EURUSD");
var r = await svc.SellMarketAsync("EURUSD", 0.10, sl: s.Ask + 150 * s.Point, tp: s.Bid - 300 * s.Point);
Console.WriteLine($"opened #{r.Ticket} @ {r.Price}");
```

## Notes

* ⚠️ Places a **real order** — guard it.
* For point-based SL/TP use [`SellMarketPointsAsync`](SellMarketPoints.md); to size by risk use [`SellMarketByRiskAsync`](../6.%20Market_ByRisk/SellMarketByRisk.md).

## See also

* [`BuyMarketAsync`](BuyMarket.md) · [`SellMarketPointsAsync`](SellMarketPoints.md) · [`SellMarketByRiskAsync`](../6.%20Market_ByRisk/SellMarketByRisk.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Trading](../../MT4Service/Trading_Methods.md)
