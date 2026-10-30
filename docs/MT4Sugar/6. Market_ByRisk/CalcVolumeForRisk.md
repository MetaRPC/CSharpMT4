# MT4Sugar · `CalcVolumeForRiskAsync`

> Volume for a fixed money risk over a stop distance (from `TickValueWithSize`), normalized.

→ extension method on **[`MT4Service`](../../MT4Service/Trading_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Sizes a position so that a `stopPoints` move against you costs exactly `riskMoney`. Reads tick value internally and returns a broker-valid (normalized) volume. The heart of risk-based entries.

## Signature

```csharp
Task<double> CalcVolumeForRiskAsync(string symbol, double stopPoints, double riskMoney, DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `symbol` | `string` | — | Instrument, e.g. `"EURUSD"`. |
| `stopPoints` | `double` | — | Stop distance in points (drives both the sizing and the SL). |
| `riskMoney` | `double` | — | Money to lose if the stop is hit (deposit currency). |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`double` — the risk-sized volume in lots (already normalized).

## Example

```csharp
double vol = await svc.CalcVolumeForRiskAsync("EURUSD", stopPoints: 200, riskMoney: 50);
```

## Scenario

Size first, then build your own order with that volume.

```csharp
double vol = await svc.CalcVolumeForRiskAsync("EURUSD", stopPoints: 200, riskMoney: 50);
double sl  = await svc.PriceFromOffsetPointsAsync("EURUSD", fromAsk: true, offsetPoints: -200);
await svc.BuyMarketAsync("EURUSD", vol, sl: sl);
```

## Notes

* Returns the symbol's **min volume** when inputs are non-positive or tick value is unavailable.
* For a one-call sized entry use [`BuyMarketByRiskAsync`](BuyMarketByRisk.md) / [`SellMarketByRiskAsync`](SellMarketByRisk.md).

## See also

* [`BuyMarketByRiskAsync`](BuyMarketByRisk.md) · [`NormalizeVolumeAsync`](../2.%20Normalization_Utils/NormalizeVolume.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Trading](../../MT4Service/Trading_Methods.md)
