# MT4Sugar · `NormalizePriceAsync`

> Round a raw price to the symbol's digits.

→ extension method on **[`MT4Service`](../../MT4Service/Symbol_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Rounds a price to the symbol's `Digits`. Brokers reject prices that don't sit on the tick grid, so run any **computed** entry/SL/TP through this before sending it.

## Signature

```csharp
Task<double> NormalizePriceAsync(string symbol, double price, DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `symbol` | `string` | — | Instrument, e.g. `"EURUSD"`. |
| `price` | `double` | — | The price to round. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`double` — the price rounded to the symbol's digits.

## Example

```csharp
double p = await svc.NormalizePriceAsync("EURUSD", 1.123456789); // -> 1.12346
```

## Scenario

Compute a stop, then round it to the tick grid before modifying — brokers reject off-tick prices.

```csharp
double rawSl = entry - 150 * point;                 // may land between ticks
double sl   = await svc.NormalizePriceAsync("EURUSD", rawSl);
await svc.ModifySlTpAsync(ticket, sl: sl);
```

## Notes

* **Prevents `invalid price` rejections** from arithmetic that lands between ticks.
* Pair with [`NormalizeVolumeAsync`](NormalizeVolume.md) to validate the other half of an order.

## See also

* [`PriceFromOffsetPointsAsync`](PriceFromOffsetPoints.md) — already returns a normalized price.
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Symbol](../../MT4Service/Symbol_Methods.md)
