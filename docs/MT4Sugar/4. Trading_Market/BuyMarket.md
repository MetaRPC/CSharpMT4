# MT4Sugar · `BuyMarketAsync`

> Market BUY (volume auto-normalized, price = Ask). SL/TP are **absolute** prices.

→ extension method on **[`MT4Service`](../../MT4Service/Trading_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Opens a market **BUY** at the current Ask. Volume is auto-normalized; SL/TP are **absolute** prices (pass `null` to omit). Returns the broker reply.

## Signature

```csharp
Task<OrderSendData> BuyMarketAsync(string symbol, double volume, double? sl = null, double? tp = null, int slippage = 5, int magic = 0, string comment = "MT4Sugar", ...)
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
var r = await svc.BuyMarketAsync("EURUSD", 0.10, sl: 1.0950, tp: 1.1050);
```

## Scenario

Snapshot → size → buy with absolute SL/TP → confirm.

```csharp
var s  = await svc.GetSymbolSnapshotAsync("EURUSD");
var r  = await svc.BuyMarketAsync("EURUSD", 0.10, sl: s.Bid - 150 * s.Point, tp: s.Ask + 300 * s.Point);
Console.WriteLine(r.Ticket > 0 ? $"opened #{r.Ticket} @ {r.Price}" : "rejected");
```

## Notes

* ⚠️ Places a **real order** — guard it behind a flag in examples/strategies.
* For SL/TP as point distances use [`BuyMarketPointsAsync`](BuyMarketPoints.md); to size by risk use [`BuyMarketByRiskAsync`](../6.%20Market_ByRisk/BuyMarketByRisk.md).

## See also

* [`SellMarketAsync`](SellMarket.md) · [`BuyMarketPointsAsync`](BuyMarketPoints.md) · [`BuyMarketByRiskAsync`](../6.%20Market_ByRisk/BuyMarketByRisk.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Trading](../../MT4Service/Trading_Methods.md)
