# MT4Sugar · `NormalizeVolumeAsync`

> Clamp & snap a raw volume to the symbol's min/max/step → a broker-valid lot size.

→ extension method on **[`MT4Service`](../../MT4Service/Symbol_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Clamps a volume into `[VolumeMin … VolumeMax]` and snaps it onto `VolumeStep`. Hand-computed lot sizes rarely land on a valid step, so normalize before ordering.

## Signature

```csharp
Task<double> NormalizeVolumeAsync(string symbol, double volume, DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `symbol` | `string` | — | Instrument, e.g. `"EURUSD"`. |
| `volume` | `double` | — | The raw volume (lots) to clamp and snap. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`double` — a broker-valid volume in lots.

## Example

```csharp
double v = await svc.NormalizeVolumeAsync("EURUSD", 0.037); // -> 0.04
```

## Scenario

A lot from a sizing formula is rarely on the broker's step — snap it before ordering.

```csharp
double raw = equity * 0.01 / 1000;                  // e.g. 0.237
double vol = await svc.NormalizeVolumeAsync("EURUSD", raw);   // -> 0.23 on a 0.01 step
await svc.BuyMarketAsync("EURUSD", vol);
```

## Notes

* **Prevents `invalid volume` rejections.**
* The by-risk and market helpers already normalize internally — use this when you build the order yourself.

## See also

* [`CalcVolumeForRiskAsync`](../6.%20Market_ByRisk/CalcVolumeForRisk.md) — produces an already-normalized volume.
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Symbol](../../MT4Service/Symbol_Methods.md)
