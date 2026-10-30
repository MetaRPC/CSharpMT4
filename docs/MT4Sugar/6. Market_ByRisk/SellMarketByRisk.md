# MT4Sugar · `SellMarketByRiskAsync`

> Risk-sized market SELL; SL placed `stopPoints` above Bid.

→ extension method on **[`MT4Service`](../../MT4Service/Trading_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Risk-sized market **SELL**; the SL is placed `stopPoints` above Bid automatically. Mirror of [`BuyMarketByRiskAsync`](BuyMarketByRisk.md).

## Signature

```csharp
Task<OrderSendData> SellMarketByRiskAsync(string symbol, double stopPoints, double riskMoney, int slippage = 5, int magic = 0, string comment = "MT4Sugar", ...)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `symbol` | `string` | — | Instrument, e.g. `"EURUSD"`. |
| `stopPoints` | `double` | — | Stop distance in points (drives both the sizing and the SL). |
| `riskMoney` | `double` | — | Money to lose if the stop is hit (deposit currency). |
| `slippage` | `int` | `5` | Max allowed price slippage, in points. |
| `magic` | `int` | `0` | Magic-number tag for the order. |
| `comment` | `string` | `"MT4Sugar"` | Order comment. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`OrderSendData` — the broker reply. **`Ticket > 0` means success**; it also carries `Volume` and `Price`.

## Example

```csharp
var r = await svc.SellMarketByRiskAsync("EURUSD", stopPoints: 200, riskMoney: 50);
```

## Scenario

Risk-sized SELL; SL placed stopPoints above Bid automatically.

```csharp
var r = await svc.SellMarketByRiskAsync("EURUSD", stopPoints: 200, riskMoney: 50);
Console.WriteLine($"opened #{r.Ticket} vol={r.Volume} @ {r.Price}");
```

## Notes

* ⚠️ Places a **real order** — guard it.

## See also

* [`BuyMarketByRiskAsync`](BuyMarketByRisk.md) · [`CalcVolumeForRiskAsync`](CalcVolumeForRisk.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Trading](../../MT4Service/Trading_Methods.md)
