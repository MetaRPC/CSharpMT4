# MT4Sugar · `BuyMarketByRiskAsync`

> Risk-sized market BUY; SL placed `stopPoints` below Ask.

→ extension method on **[`MT4Service`](../../MT4Service/Trading_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

One call that sizes by risk **and** opens a BUY with its protective stop already attached (`stopPoints` below Ask). The safest way to enter — you're never left without an SL.

## Signature

```csharp
Task<OrderSendData> BuyMarketByRiskAsync(string symbol, double stopPoints, double riskMoney, int slippage = 5, int magic = 0, string comment = "MT4Sugar", ...)
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
var r = await svc.BuyMarketByRiskAsync("EURUSD", stopPoints: 200, riskMoney: 50);
```

## Scenario

One call: size by risk AND open with the protective stop attached.

```csharp
var r = await svc.BuyMarketByRiskAsync("EURUSD", stopPoints: 200, riskMoney: 50);
Console.WriteLine($"opened #{r.Ticket} vol={r.Volume} @ {r.Price}");
```

## Notes

* ⚠️ Places a **real order** — guard it.
* Combines [`CalcVolumeForRiskAsync`](CalcVolumeForRisk.md) + a market entry with the SL set for you.

## See also

* [`SellMarketByRiskAsync`](SellMarketByRisk.md) · [`CalcVolumeForRiskAsync`](CalcVolumeForRisk.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Trading](../../MT4Service/Trading_Methods.md)
